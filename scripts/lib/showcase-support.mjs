// 基类参数支持矩阵的推导：`AeterniComponent` 的 8 个公共参数里，哪些真正落到某个组件的
// 根元素上、哪些根本不生效。
//
// 为什么不能每个页面都无条件列出全部 8 个参数：`ThemeProvider`、`FormDialog` 不渲染 DOM，
// `Badge` 不读 `Disabled`，`Menu` / `Splitter` 之类把根元素引用绑在 `RenderedRootElement`
// 上而不是直接 `RootElement`。把这些结论写死在示例页里会立刻漂移，所以这里只从源码结构推导：
//
//   - 是否渲染根元素属性：标记了 `@attributes="BuildAttributes()"`，或把 5 个 DOM 参数
//     原样转发给子组件（ThemeSwitch / ThemeBrandSwitch 的写法）；
//   - `Class` / `Style` / `AdditionalAttributes`：覆盖了构建方法时是否调用 `base.*()`；
//   - `Disabled`：`SupportsDisabled => true`，或代码里真的读了基类 `Disabled`
//     （排除 `item.Disabled` 这类其它类型的同名成员与嵌套类型的声明）；
//   - `Element` / `ElementChanged`：根元素绑定 `@ref="RootElement"` / `"RenderedRootElement"`，
//     或把 `Element` / `ElementChanged` 转发给子组件。
//
// 推导不出来的个别情况由 `scripts/api-groups.json` 的 `baseParameterOverrides` 显式登记；
// 没登记就按源码结论走，不猜。
import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

/// 基类参数的唯一顺序（与 `ShowcaseApi.CommonParameters` 一致）。
export const BASE_PARAMETER_NAMES = Object.freeze([
    'Id', 'Disabled', 'Visible', 'Class', 'Style', 'Element', 'AdditionalAttributes', 'ElementChanged'
]);

const NOTE = Object.freeze({
    noDom: '该组件不渲染 DOM，此参数没有可作用的根元素。',
    attrsOverride: 'BuildAttributes 覆盖没有调用 base.BuildAttributes()，基类属性不会合并。',
    classOverride: 'BuildClass 覆盖没有调用 base.BuildClass()，消费方的 Class 会丢失。',
    styleOverride: 'BuildStyle 覆盖没有调用 base.BuildStyle()，消费方的 Style 会丢失。',
    disabled: '组件不读取基类 Disabled 参数，传入它不会改变任何行为。',
    element: '组件没有把根元素引用绑定到 DOM（没有 @ref="RootElement" 或转发的 Element）。',
    elementChanged: '根元素引用没有接到 DOM，ElementChanged 不会随根元素变化触发。'
});

const read = path => readFileSync(path, 'utf8').replace(/^\uFEFF/, '');

/// 只去掉整行注释与块注释：属性值里的 `//`（例如 URL）不能被当成注释吃掉。
export function stripComments(source) {
    return source
        .replace(/\/\*[\s\S]*?\*\//g, ' ')
        .split('\n')
        .map(line => (/^\s*\/\//.test(line) ? '' : line))
        .join('\n');
}

/// 覆盖方法是否调用基类实现。没有覆盖视为沿用基类（支持）；覆盖了但没调用 base 的参数会丢失。
/// 只取这一个方法的表达式体 / 花括号体，避免把相邻方法里的 base 调用算进来。
function overrideCallsBase(code, method) {
    const signature = new RegExp(`override\\s+[\\w<>?,\\[\\]\\.\\s]+?\\s+${method}\\(\\)`);
    const match = signature.exec(code);
    if (!match) return true;

    const rest = code.slice(match.index + match[0].length);
    const brace = rest.indexOf('{');
    const semicolon = rest.indexOf(';');
    let body = rest;
    if (brace >= 0 && (semicolon < 0 || brace < semicolon)) {
        let depth = 0;
        body = rest.slice(0, brace);
        for (let cursor = brace; cursor < rest.length; cursor += 1) {
            if (rest[cursor] === '{') depth += 1;
            else if (rest[cursor] === '}') {
                depth -= 1;
                if (depth === 0) {
                    body = rest.slice(0, cursor + 1);
                    break;
                }
            }
        }
    } else if (semicolon >= 0) {
        body = rest.slice(0, semicolon + 1);
    }

    return body.includes(`base.${method}()`);
}

/// 组件代码是否读取基类 `Disabled`。排除“看起来像但不是基类参数”的写法：
///   - `item.Disabled` / `FormField?.Disabled`（其它类型上的同名成员）：前面有 `.`；
///   - `IsDisabled` / `SupportsDisabled`（不同标识符）：前面有单词字符；
///   - 嵌套类型自己声明 `bool Disabled`（record 参数、属性）：整行是声明；
///   - 属性模式与命名参数里的 `Disabled:`（例如 `SelectedTab is { Disabled: false }`）：
///     那是别的类型上的属性名，先把名字部分去掉；`Disabled: Disabled` 的右值仍会被算进来。
function usesBaseDisabled(code) {
    return code.split('\n').some(line => {
        if (/^\s*(?:public|private|protected|internal)\s+(?:readonly\s+)?bool\s+Disabled\b/.test(line)) return false;
        if (/\bbool\s+Disabled\b\s*[,)=]/.test(line)) return false;
        return /(?<![.\w])Disabled\b/.test(line.replace(/\bDisabled\s*:/g, ''));
    });
}

/// 推导一个组件不支持哪些基类参数。返回 `{ name, note }[]`（顺序固定）；
/// 空数组表示 8 个参数都落在根元素上。
/// `overrides` 来自 `api-groups.json` 的 `baseParameterOverrides[组件类型名]`：
/// 值为说明字符串时强制登记为不支持，值为 null 时强制视为支持（两者都以源码为准的兜底）。
export function deriveBaseParameterSupport({ type, directory, overrides = {} }) {
    const razorPath = join(directory, `${type}.razor`);
    const codePath = join(directory, `${type}.razor.cs`);
    if (!existsSync(razorPath) || !existsSync(codePath)) {
        throw new Error(`推导 ${type} 的公共参数支持矩阵失败：缺少 ${razorPath} 或 ${codePath}`);
    }

    const markup = stripComments(read(razorPath));
    const code = stripComments(read(codePath));

    // 根元素属性：直接渲染 BuildAttributes()，或把 5 个 DOM 参数原样转发给子组件。
    const rendersAttributes = /@attributes\s*=\s*["']?BuildAttributes\(\)/.test(markup);
    const forwardsDom = ['Id', 'Class', 'Style', 'Visible', 'AdditionalAttributes']
        .every(name => new RegExp(`${name}\\s*=\\s*"@${name}"`).test(markup));
    const hasDomRoot = rendersAttributes || forwardsDom;

    const attributesMerged = hasDomRoot && overrideCallsBase(code, 'BuildAttributes');
    const classMerged = attributesMerged && overrideCallsBase(code, 'BuildClass');
    const styleMerged = attributesMerged && overrideCallsBase(code, 'BuildStyle');

    const rootRef = /@ref\s*=\s*"(?:Rendered)?RootElement"/.test(markup);
    const forwardsElement = /Element\s*=\s*"@Element"/.test(markup);
    const forwardsElementChanged = /ElementChanged\s*=\s*"[^"]+"/.test(markup);
    const elementWired = hasDomRoot && (rootRef || forwardsElement);
    const elementChangedWired = hasDomRoot && (rootRef || (forwardsElement && forwardsElementChanged));

    const disabledRead = /SupportsDisabled\s*=>\s*true/.test(code) || usesBaseDisabled(code);

    // 先看有没有 DOM 根元素：没有根元素时所有 DOM 参数都是「不渲染」，而不是「覆盖写坏了」；
    // 有根元素时再看构建方法有没有调用 base（覆盖了却不调 base 才会丢消费方的值）。
    const domNote = supported => {
        if (!hasDomRoot) return NOTE.noDom;
        return supported ? null : NOTE.attrsOverride;
    };

    const rows = [
        ['Id', domNote(attributesMerged)],
        ['Disabled', disabledRead ? null : (hasDomRoot ? NOTE.disabled : NOTE.noDom)],
        ['Visible', domNote(attributesMerged)],
        ['Class', !hasDomRoot ? NOTE.noDom : (attributesMerged ? (classMerged ? null : NOTE.classOverride) : NOTE.attrsOverride)],
        ['Style', !hasDomRoot ? NOTE.noDom : (attributesMerged ? (styleMerged ? null : NOTE.styleOverride) : NOTE.attrsOverride)],
        ['Element', !hasDomRoot ? NOTE.noDom : (elementWired ? null : NOTE.element)],
        ['AdditionalAttributes', domNote(attributesMerged)],
        ['ElementChanged', !hasDomRoot ? NOTE.noDom : (elementChangedWired ? null : NOTE.elementChanged)]
    ];

    const unsupported = rows
        .filter(([, reason]) => reason !== null)
        .map(([name, reason]) => ({ name, note: reason }));

    const explicit = overrides[type] ?? {};
    for (const [name, value] of Object.entries(explicit)) {
        if (!BASE_PARAMETER_NAMES.includes(name)) {
            throw new Error(`baseParameterOverrides.${type} 登记了不存在的基类参数 ${name}`);
        }

        const index = unsupported.findIndex(row => row.name === name);
        if (value === null) {
            if (index >= 0) unsupported.splice(index, 1);
            continue;
        }

        if (typeof value !== 'string' || value.trim() === '') {
            throw new Error(`baseParameterOverrides.${type}.${name} 必须是说明字符串或 null`);
        }

        if (index >= 0) unsupported[index].note = value;
        else unsupported.push({ name, note: value });
    }

    const order = new Map(BASE_PARAMETER_NAMES.map((name, index) => [name, index]));
    return unsupported.sort((left, right) => order.get(left.name) - order.get(right.name));
}
