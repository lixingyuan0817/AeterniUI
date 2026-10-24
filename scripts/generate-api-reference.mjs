#!/usr/bin/env node
// 生成示例工程的 API 表数据。
//
// 为什么生成而不是手写：示例页的参数表此前是手写的，审查实测 8 个页面与组件的公共
// `[Parameter]` 对不上（`VirtualList` 的整套选择 API 一个字都没写）。市面上的组件文档
// （Ant Design、Element Plus）都从类型定义生成 props 表，原因就是这个：手写的表会漂移，
// 而且漂移没有任何编译期信号。
//
// 这里沿用仓库既有的生成物约定（`generate-fontawesome-icons.mjs` + `--check`）：生成物
// 提交进仓库，CI 用 `--check` 拒绝过期产物，因此“改了组件忘了改文档”会在 CI 失败而不是
// 无声漂移。
//
// 解析策略是「读源码文本」而不是加载程序集：生成器要能在没有任何 .NET 环境的机器上跑
// （CI 的 Node 步骤、本地 `node scripts/...`），也因为 API 表要的是声明而不是运行时形状
// ——默认值、XML 文档说明这些只有在源码里才完整。
//
// 生成物分三块：
//   - ByPage / Contracts：页面主组件与模型、服务的成员行（原有结构，搜索索引也读它）；
//   - Sections：页面的二级分组。多组件合页（DatePicker / DateRangePicker、Tabs / Tab、
//     Dialog / Confirm / Alert / Toast……）按组件类型拆开，并给出每个组件的基类参数支持
//     矩阵（不生效的项 + 原因）。分组与矩阵的推导规则见 scripts/lib/showcase-support.mjs
//     与 scripts/api-groups.json。
//
// 用法：node scripts/generate-api-reference.mjs [--check]

import { writeFileSync, existsSync, readdirSync } from 'node:fs';
import { join, basename } from 'node:path';
import { catalogIds, pascal, read } from './lib/showcase-catalog.mjs';
import { deriveBaseParameterSupport } from './lib/showcase-support.mjs';

const LIBRARY = 'src/AeterniUI';
const SAMPLE = 'src/AeterniUI.Sample';
const OUTPUT = `${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`;

// 内部渲染部件：delivered-features §24/§37 明确它们保留 Razor 类型只为内部组合，已标记
// 为非稳定公共 API，因此不进示例页的公共表。
const INTERNAL_PARTS = new Set(['DateCalendar', 'TimeOptionList']);

// 基类参数的说明由 ShowcaseApi.CommonParameters 统一登记，页面不逐条重复；每个组件到底
// 支持哪几项由 scripts/lib/showcase-support.mjs 从源码结构推导（见生成物的 Sections）。
const BASE_PARAMETERS = new Set([
    'Id', 'Disabled', 'Visible', 'Class', 'Style', 'Element', 'AdditionalAttributes', 'ElementChanged'
]);

// 目录 id 与组件目录名不一致的例外。目录 id 由路由决定，目录名由类型名决定，两者只能
// 显式对齐；例外的存在本身就是为什么这里不做“猜名字”的推导。
const COMPONENT_DIRECTORY_OVERRIDES = {
    combobox: 'ComboBox'
};

// 不产出 API 表的页面：安装、设计基础与实验室页讲的是用法与 Token，没有单一组件。
const PAGES_WITHOUT_API = new Set(['install', 'colors', 'glass']);

// 成员首次出现的版本。人工维护、体量很小，只登记 release-history 明确记录了“新增公共成员”
// 的那几批；没有登记就显示为空，绝不猜测。
const SINCE_FILE = 'scripts/api-since.json';

// 说明文字与“生成器推不出来的行”是编辑内容：前者是散文（源码里没有 XML 注释），后者是模型、
// 服务与汇总行（`Value / StartDate / EndDate` 这种一条覆盖多名的写法）。它们从原来的手写表里
// 一次性抽出来，之后只在这两个文件里维护 —— 页面本身不再手写任何 API 行。
const DESCRIPTIONS_FILE = 'scripts/api-descriptions.json';
const EXTRA_ROWS_FILE = 'scripts/api-extra-rows.json';

// 多组件合页的二级分组与公共参数兜底。目录里只有一个组件的页保持单表；多个组件的页
// 自动按组件类型分组（有组件目录就能推出来），只有“与文件顺序不一致”或“没有组件目录”
// （反馈页的 Dialog / Confirm / Alert / Toast）才在这里登记。公共参数的支持矩阵优先从
// 源码结构推导，推不出来的个别情况用这个文件的 baseParameterOverrides 显式兜底。
const GROUPS_FILE = 'scripts/api-groups.json';
const groupConfig = existsSync(GROUPS_FILE)
    ? JSON.parse(read(GROUPS_FILE))
    : { pages: {}, baseParameterOverrides: {} };

const BASE_PARAMETER_OVERRIDES = groupConfig.baseParameterOverrides ?? {};

// 组件目录名只从一次目录列举里查，不用 existsSync：后者在大小写不敏感的文件系统上会
// 解析出实际上不存在（大小写不符）的目录，于是同一份源码在 Windows 与 Linux 上生成出
// 不同的页面集合，`--check` 在 CI 报过期却看不出差在哪。
const COMPONENT_DIRECTORIES = new Map(
    readdirSync(join(LIBRARY, 'Components'), { withFileTypes: true })
        .filter(entry => entry.isDirectory())
        .map(entry => [entry.name.toLowerCase(), entry.name])
);

/// 组件目录名：目录 id 的 Pascal 形式，或显式例外。
function componentDirectory(id) {
    const explicit = COMPONENT_DIRECTORY_OVERRIDES[id];
    if (explicit) {
        const found = COMPONENT_DIRECTORIES.get(explicit.toLowerCase());
        if (!found) throw new Error(`组件目录 ${explicit} 不存在（${id} 页的例外映射已过期）`);
        return found;
    }

    return COMPONENT_DIRECTORIES.get(pascal(id).toLowerCase()) ?? null;
}

function cleanDocComment(block) {
    if (!block) return '';

    const summary = /<summary>([\s\S]*?)<\/summary>/.exec(block)?.[1] ?? '';
    return summary
        .split('\n')
        .map(line => line.replace(/^\s*\/\/\/\s?/, '').trim())
        .join(' ')
        .replace(/<see cref="[^"]*?\.(\w+)"\s*\/>/g, '$1')
        .replace(/<see cref="(\w+)"\s*\/>/g, '$1')
        .replace(/<c>([^<]*)<\/c>/g, '$1')
        .replace(/<para>|<\/para>/g, ' ')
        .replace(/<[^>]+>/g, '')
        .replace(/\s+/g, ' ')
        .trim();
}

/// 跳过空白。参数声明可能跨行，读属性体与初始化表达式前都要先跳过换行。
function skipTrivia(source, start) {
    let cursor = start;
    while (cursor < source.length && /\s/.test(source[cursor])) cursor += 1;
    return cursor;
}

/// 跳过字符串字面量，返回结束引号之后的位置。默认值里只有简单字面量会进 API 表，但扫描
/// 属性体与初始化表达式时仍要跨过 `"…;…"`，否则字符串里的分号会被当成声明结束。
function skipStringLiteral(source, start) {
    // C# 11 原始字符串 `"""…"""`：以相同数量的引号结束，内部不转义。
    const quotes = /^"+/.exec(source.slice(start, start + 16))?.[0] ?? '"';
    if (quotes.length >= 3) {
        const closing = source.indexOf(quotes, start + quotes.length);
        return closing < 0 ? source.length : closing + quotes.length;
    }

    let cursor = start + 1;
    while (cursor < source.length) {
        if (source[cursor] === '\\') { cursor += 2; continue; }
        if (source[cursor] === '"') return cursor + 1;
        cursor += 1;
    }

    return cursor;
}

/// 跳转义字符串 `@"…"`（内部 `""` 表示一个引号），`quoteIndex` 指向开引号。
function skipVerbatimString(source, quoteIndex) {
    let cursor = quoteIndex + 1;
    while (cursor < source.length) {
        if (source[cursor] === '"') {
            if (source[cursor + 1] === '"') { cursor += 2; continue; }
            return cursor + 1;
        }
        cursor += 1;
    }

    return cursor;
}

/// 跳过字符字面量 `'…'`：`'}'` 与 `'\''` 里的花括号、引号不参与结构配对。
function skipCharLiteral(source, start) {
    let cursor = start + 1;
    while (cursor < source.length) {
        if (source[cursor] === '\\') { cursor += 2; continue; }
        if (source[cursor] === "'") return cursor + 1;
        cursor += 1;
    }

    return cursor;
}

/// 从 `{` 开始按嵌套层级找到属性体结束的 `}`，返回右花括号之后的位置。
function skipPropertyBody(source, start) {
    let cursor = start;
    let depth = 0;
    while (cursor < source.length) {
        const character = source[cursor];
        if (character === '"') { cursor = skipStringLiteral(source, cursor); continue; }
        if (character === '@' && source[cursor + 1] === '"') { cursor = skipVerbatimString(source, cursor + 1); continue; }
        if (character === "'") { cursor = skipCharLiteral(source, cursor); continue; }
        if (character === '{') depth += 1;
        else if (character === '}') {
            depth -= 1;
            if (depth === 0) return cursor + 1;
        }
        cursor += 1;
    }

    return cursor;
}

/// 从 `=` 之后开始找初始化表达式结束的 `;`：括号/方括号/花括号与字符串都配对跳过，
/// 因此 `new(…) { … }`、`Array.Empty<int>()`、多行表达式都不会提前截断。
function endOfExpression(source, start) {
    let cursor = start;
    let depth = 0;
    while (cursor < source.length) {
        const character = source[cursor];
        if (character === '"') { cursor = skipStringLiteral(source, cursor); continue; }
        if (character === '@' && source[cursor + 1] === '"') { cursor = skipVerbatimString(source, cursor + 1); continue; }
        if (character === "'") { cursor = skipCharLiteral(source, cursor); continue; }
        if (character === '(' || character === '[' || character === '{') depth += 1;
        else if (character === ')' || character === ']' || character === '}') depth -= 1;
        else if (character === ';' && depth === 0) return cursor;
        cursor += 1;
    }

    return cursor;
}

/// 读出参数名之后的可选初始化表达式（`{ get; set; } = 99;` 里的 `99`）。
/// 表达式属性（`{ get => …; }`）没有初始化值，返回空串；复杂表达式原样返回，由
/// `simpleDefault` 决定是否展示。
function readInitializer(source, start) {
    let cursor = skipTrivia(source, start);

    if (source[cursor] === '{') {
        cursor = skipTrivia(source, skipPropertyBody(source, cursor));
    } else if (source[cursor] === '=' && source[cursor + 1] === '>') {
        return '';
    }

    if (source[cursor] !== '=' || source[cursor + 1] === '>') return '';

    return source.slice(cursor + 1, endOfExpression(source, cursor + 1)).trim();
}

/// 参数声明：`[Parameter]`（可带 CapturingUnmatchedValues）后跟属性声明。说明取该成员上方
/// 最近的 XML 文档注释——没有注释就是空说明，生成器不会替作者编。
///
/// 默认值不能用 `\{([^}]*)\}` 抓属性体来取：自动属性的初始值在右花括号**之后**
/// （`{ get; set; } = 99;`），属性体里根本没有 `=`；而表达式属性（`{ get => ...; }`）
/// 又没有初始化值。因此这里只匹配到参数名，再按 C# 结构继续读“可选的 `= 初始化表达式;`”，
/// 花括号、括号与字符串都按配对扫描，多行声明与 `new() { … }` 这类表达式不会越界。
function parseMembers(declaringType, source) {
    const members = [];
    const pattern = /((?:\s*\/\/\/[^\n]*\n)*)\s*\[Parameter[^\]]*\]\s*public\s+([\w<>?,\[\]\.\s]+?)\s+(\w+)\s*(?=[{=])/g;

    for (const match of source.matchAll(pattern)) {
        const [, doc, type, name] = match;
        if (BASE_PARAMETERS.has(name)) continue;

        const initializer = readInitializer(source, match.index + match[0].length);
        members.push({
            name,
            declaringType,
            type: type.replace(/\s+/g, ' ').trim(),
            defaultValue: simpleDefault(initializer),
            description: cleanDocComment(doc),
            kind: /EventCallback/.test(type) ? '事件回调' : '属性'
        });
    }

    return members;
}

/// 同名参数可能由同一目录下的不同类型声明（`Tab` 与 `Tabs` 各有一个 `Value`，类型还不同）。
/// 这类歧义必须显式消解：以前只看谁最后被读到，于是生成物取决于文件系统的列举顺序，
/// Windows 与 Linux 会生成不同的表（本批被 CI 抓到过一次）。消解方式是双方都保留并加上
/// 类型名前缀，信息不丢，顺序也只由源码决定。
function qualify(members, pageId) {
    const byName = new Map();
    for (const member of members) {
        byName.set(member.name, [...(byName.get(member.name) ?? []), member]);
    }

    const collisions = [];
    const resolved = [];
    for (const [name, declarations] of byName) {
        if (declarations.length === 1) {
            resolved.push(declarations[0]);
            continue;
        }

        collisions.push(`${pageId}.${name}`);
        for (const declaration of declarations) {
            resolved.push({ ...declaration, name: `${declaration.declaringType}.${name}`, qualified: name });
        }
    }

    return { members: resolved, collisions };
}

/// 默认值只在字面量或简单成员访问时才写出来：`new(...)`、方法调用这类表达式对读者没有
/// 信息量，显示出来反而会让人以为默认值是可预测的。
function simpleDefault(initializer) {
    if (!initializer) return '';
    if (/^(true|false|null|\[\]|\[\.\.\.\]|default|string\.Empty|""|''|\d+(\.\d+)?|"[^"]*")$/.test(initializer)) {
        return initializer === 'string.Empty' ? '空字符串' : initializer;
    }
    if (/^[A-Z]\w*\.\w+$/.test(initializer)) return initializer;
    if (/^\[\s*\]$/.test(initializer)) return '空列表';
    return '';
}

const collisionWarnings = [];

const since = existsSync(SINCE_FILE)
    ? JSON.parse(read(SINCE_FILE))
    : { members: {} };

const descriptions = existsSync(DESCRIPTIONS_FILE)
    ? JSON.parse(read(DESCRIPTIONS_FILE)).members
    : {};

const extraRows = existsSync(EXTRA_ROWS_FILE)
    ? JSON.parse(read(EXTRA_ROWS_FILE)).rows
    : {};

const compareCodePoints = (left, right) => (left < right ? -1 : left > right ? 1 : 0);

/// 多组件合页的分组配置：字符串条目等价于 `{ title, owners: [title] }`。
/// 没有配置的页按组件目录自动分组，所以这里只登记顺序有讲究或没有组件目录的页。
function configuredGroups(pageId) {
    const configured = groupConfig.pages?.[pageId];
    if (!configured) return null;
    if (!Array.isArray(configured) || configured.length === 0) {
        throw new Error(`api-groups.json 的 ${pageId} 分组必须是数组`);
    }

    return configured.map(item => {
        const group = typeof item === 'string' ? { title: item, owners: [item] } : item;
        if (!group || typeof group.title !== 'string' || group.title.trim() === '') {
            throw new Error(`api-groups.json 的 ${pageId} 有缺少 title 的分组`);
        }

        const owners = group.owners ?? [group.title];
        if (!Array.isArray(owners) || owners.length === 0 || owners.some(owner => typeof owner !== 'string')) {
            throw new Error(`api-groups.json 的 ${pageId}.${group.title} 的 owners 必须是非空字符串数组`);
        }

        return { title: group.title, owners };
    });
}

/// 额外行（模型、服务与汇总写法）的归属标记：取名字开头的类型。
/// `DialogOptions.Title` → DialogOptions；`*Changed` 这类跨组件汇总行没有归属。
function extraRowOwner(name) {
    const match = /^([A-Z][A-Za-z0-9_]*)/.exec(name);
    return match ? match[1] : null;
}

/// 把一个页面的成员拆成二级分组，返回 `{ title, componentType, memberNames }[]`。
/// - 单组件页：所有成员（含模型 / 服务额外行）保持一张表，不为了分组而分组；
/// - 多组件页：每个组件类型一组（目录文件顺序，或 api-groups.json 的显式顺序），
///   其它归属（ThemeService 这类服务）各自成组，无归属的汇总行进「共享约定」，最后兜底；
/// - 分完必须正好覆盖页面的全部成员：漏行或重复行会让 API 表静默丢内容，直接抛错。
function buildSections({ pageId, componentTypes, members, emittedExtras }) {
    const rowsByOwner = new Map();
    const sharedRows = [];
    const addRow = (owner, name) => {
        if (!owner) {
            sharedRows.push(name);
            return;
        }

        rowsByOwner.set(owner, [...(rowsByOwner.get(owner) ?? []), name]);
    };

    for (const member of members) addRow(member.declaringType, member.name);
    for (const row of emittedExtras) addRow(extraRowOwner(row.name), row.name);

    const config = configuredGroups(pageId);
    const singleComponent = componentTypes.length <= 1;

    // 单组件页（以及没有组件的反馈页兜底）保持一张表，成员顺序与原来的渲染一致：
    // 组件成员按名字排序，额外行按编辑文件顺序排在后面。
    if (!config && singleComponent) {
        const sorted = [...members.map(member => member.name), ...emittedExtras.map(row => row.name)];
        return [{ title: componentTypes[0] ?? '', componentType: componentTypes[0] ?? null, memberNames: sorted }];
    }

    const sections = [];
    const claimed = new Set();
    const titles = new Set();
    const push = (title, componentType, memberNames) => {
        if (config && titles.has(title)) {
            throw new Error(`api-groups.json 的 ${pageId} 有重复的分组标题 ${title}`);
        }

        titles.add(title);
        if (memberNames.length === 0 && !componentType) return;
        sections.push({ title, componentType, memberNames: memberNames.sort(compareCodePoints) });
    };

    if (config) {
        for (const group of config) {
            const memberNames = [];
            let componentType = null;
            for (const owner of group.owners) {
                if (claimed.has(owner)) {
                    throw new Error(`api-groups.json 的 ${pageId} 把 ${owner} 分到了多个分组`);
                }

                const owned = rowsByOwner.get(owner) ?? [];
                if (owned.length === 0 && !componentTypes.includes(owner)) {
                    throw new Error(`api-groups.json 的 ${pageId}.${group.title} 登记了没有成员的归属 ${owner}`);
                }

                claimed.add(owner);
                memberNames.push(...owned);
                if (componentTypes.includes(owner)) {
                    if (componentType && componentType !== owner) {
                        throw new Error(`api-groups.json 的 ${pageId}.${group.title} 不能合并多个组件类型`);
                    }

                    componentType = owner;
                }
            }

            push(group.title, componentType, memberNames);
        }
    }

    // 没有配置、或配置漏掉的组件按目录文件顺序各成一组：新增多组件目录不会退回混表。
    for (const type of componentTypes) {
        if (claimed.has(type)) continue;
        claimed.add(type);
        push(type, type, [...(rowsByOwner.get(type) ?? [])]);
    }

    // 其余归属（服务与模型）按名字排序各成一组；没有归属的汇总行放最后。
    for (const owner of [...rowsByOwner.keys()].filter(owner => !claimed.has(owner)).sort(compareCodePoints)) {
        claimed.add(owner);
        push(owner, null, [...(rowsByOwner.get(owner) ?? [])]);
    }

    if (sharedRows.length > 0) push('共享约定', null, [...sharedRows]);

    if (sections.length === 0) {
        sections.push({ title: '', componentType: null, memberNames: [] });
    }

    const assigned = sections.flatMap(section => section.memberNames);
    const expected = [...members.map(member => member.name), ...emittedExtras.map(row => row.name)];
    if (assigned.length !== expected.length || new Set(assigned).size !== expected.length) {
        throw new Error(`${pageId} 的 API 分组没有正好覆盖全部成员（分组 ${assigned.length} 行，成员 ${expected.length} 行）`);
    }

    return sections;
}

function pageEntries() {
    const entries = [];
    for (const id of catalogIds()) {
        if (PAGES_WITHOUT_API.has(id)) continue;

        const directory = componentDirectory(id);
        // 没有对应组件类型的页（反馈页记录的是 IDialogService 与各个选项类型）仍然可以
        // 有 API 表：它的行全部来自编辑文件的额外行。以前这里直接跳过，于是"页面不再手写
        // 表"的规则和"这类页只有手写表"的事实撞在一起——卡片化重构把页面的手写数组换成
        // 没人消费的死代码，页面上没有 API 区，门禁却把它当例外放过了。
        if (!directory) {
            const extras = extraRows[id] ?? [];
            if (extras.length === 0) continue;

            entries.push({
                id,
                directory: null,
                members: [],
                extras,
                // 没有组件类型，公共参数支持矩阵无处可推：分组只按额外行的归属标记拆，
                // 不列基类参数（它们不属于这里记录的服务与选项类型）。
                sections: buildSections({ pageId: id, componentTypes: [], members: [], emittedExtras: extras.filter(row => row.kind !== '接口') })
                    .map(section => ({ ...section, unsupportedBaseParameters: null }))
            });
            continue;
        }

        const dir = join(LIBRARY, 'Components', directory);
        // 文件名排序：解析结果随后按名字排序，但同名成员谁先出现也会影响合并，所以顺序
        // 必须由源码决定，而不是由文件系统的列举顺序决定。
        const files = readdirSync(dir)
            .filter(file => file.endsWith('.razor.cs') && !INTERNAL_PARTS.has(basename(file, '.razor.cs')))
            .sort();

        const parsed = files.flatMap(file => parseMembers(basename(file, '.razor.cs'), read(join(dir, file))));
        const { members: qualified, collisions } = qualify(parsed, id);
        for (const collision of collisions) collisionWarnings.push(collision);

        // 同名歧义行在抽说明时是按带前缀的名字存进编辑文件的（`Radio.AriaLabel`），
        // 因为原来的手写表就是这么写的；生成器这边也要按同一把键回查，否则这些行会
        // 只剩源码里通常为空的 XML 说明。
        const extraDescriptions = new Map((extraRows[id] ?? []).map(row => [row.name, row.description]));

        const members = qualified.map(member => ({
            ...member,
            // 说明优先取编辑文件：源码里的 XML 注释只覆盖了一部分成员，而中文散文一直是
            // 写在页面上的，抽出来之后就以这个文件为准。
            description: descriptions[id]?.[member.qualified ?? member.name]
                ?? descriptions[id]?.[member.name]
                ?? extraDescriptions.get(member.name)
                ?? member.description,
            // 页面内同名成员在 qualify() 里已经展开成「声明类型.成员名」。此时
            // `${directory}.${name}` 会变成「DatePicker.DatePicker.ShowLunar」这类
            // 重复前缀，永远查不到；再按 api-since 约定的「类型名.成员名」直查一次。
            since: since.members[`${directory}.${member.name}`] ?? since.members[member.name] ?? ''
        }));

        const extras = extraRows[id] ?? [];
        if (members.length === 0 && extras.length === 0) continue;

        // 排序必须是代码点序，不能用 localeCompare：ICU 的排序规则随 Node 构建变化，
        // 生成物会因此在 CI 与本地不一致，`--check` 报“过期”却看不出差在哪（本批真的
        // 被 CI 抓到过一次）。生成物的内容只能取决于源码。
        const sortedMembers = members.sort((left, right) => (left.name < right.name ? -1 : left.name > right.name ? 1 : 0));

        // 额外行排在组件成员之后：它们记录的是同页的模型、服务与汇总写法，读者先看组件本身。
        // 分组只关心非接口行（接口行由 Contracts 单独渲染），重名行与成员去重后的规则必须
        // 和 render() 一致，否则分组会引用一行根本不存在的成员。
        const emitted = new Set(sortedMembers.map(member => member.name));
        const emittedExtras = extras.filter(row => row.kind !== '接口' && !emitted.has(row.name));
        const componentTypes = files.map(file => basename(file, '.razor.cs'));

        const sections = buildSections({ pageId: id, componentTypes, members: sortedMembers, emittedExtras }).map(section => ({
            ...section,
            // 只有真正的库组件才谈得上基类参数：模型 / 服务 / 汇总行分组不列公共参数。
            unsupportedBaseParameters: section.componentType
                ? deriveBaseParameterSupport({
                    type: section.componentType,
                    directory: dir,
                    overrides: BASE_PARAMETER_OVERRIDES
                })
                : null
        }));

        entries.push({
            id,
            directory,
            members: sortedMembers,
            extras,
            sections
        });
    }

    const generatedPages = new Set(entries.map(entry => entry.id));
    for (const pageId of Object.keys(groupConfig.pages ?? {})) {
        if (!generatedPages.has(pageId)) {
            throw new Error(`api-groups.json 登记了没有生成 API 表的页面 ${pageId}`);
        }
    }

    return entries;
}

function escape(value) {
    return value.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

function render(entries) {
    const lines = [];
    lines.push('// <auto-generated />');
    lines.push('// 由 scripts/generate-api-reference.mjs 生成，请勿手工编辑。');
    lines.push('// 每个组件的公共参数与事件回调都从源码的 [Parameter] 声明与 XML 文档注释提取，');
    lines.push('// 多组件合页按组件类型分组；基类参数的支持矩阵从组件源码结构推导（scripts/lib/showcase-support.mjs）。');
    lines.push('// CI 用 `node scripts/generate-api-reference.mjs --check` 拒绝过期产物。');
    lines.push('');
    lines.push('using System;');
    lines.push('using System.Collections.Generic;');
    lines.push('');
    lines.push('namespace AeterniUI.Sample.Components.Showcase;');
    lines.push('');
    lines.push('public static class ShowcaseApiReference');
    lines.push('{');
    lines.push('    /// <summary>Page id → 该页主组件的公共成员。</summary>');
    lines.push('    public static IReadOnlyDictionary<string, IReadOnlyList<ShowcaseApiMember>> ByPage { get; } =');
    lines.push('        new Dictionary<string, IReadOnlyList<ShowcaseApiMember>>(StringComparer.Ordinal)');
    lines.push('        {');

    for (const entry of entries) {
        lines.push(`            ["${entry.id}"] = new ShowcaseApiMember[]`);
        lines.push('            {');
        const emitted = new Set(entry.members.map(member => member.name));
        for (const member of [...entry.members, ...entry.extras.filter(row => row.kind !== '接口' && !emitted.has(row.name))]) {
            const parts = [
                `"${escape(member.name ?? '')}"`,
                `"${escape(member.type ?? '')}"`,
                `"${escape(member.defaultValue ?? member.default ?? '')}"`,
                `"${escape(member.description ?? '')}"`,
                `"${member.kind ?? '属性'}"`,
                `"${escape(member.since ?? '')}"`
            ];
            lines.push(`                new(${parts.join(', ')}),`);
        }
        lines.push('            },');
    }

    lines.push('        };');
    lines.push('    /// <summary>页面 id → 该页 API 的二级分组（多组件合页按组件类型分组）。</summary>');
    lines.push('    /// <remarks>');
    lines.push('    /// 组的顺序就是页面上的阅读顺序。UnsupportedBaseParameters 为 null 表示该组不是库组件');
    lines.push('    /// （模型 / 服务 / 共享约定），不列公共参数；为空数组表示 8 个公共参数都落到根元素上；');
    lines.push('    /// 非空时只登记不生效的参数与原因。');
    lines.push('    /// </remarks>');
    lines.push('    public static IReadOnlyDictionary<string, IReadOnlyList<ShowcaseApiSection>> Sections { get; } =');
    lines.push('        new Dictionary<string, IReadOnlyList<ShowcaseApiSection>>(StringComparer.Ordinal)');
    lines.push('        {');
    for (const entry of entries) {
        lines.push(`            ["${entry.id}"] = new ShowcaseApiSection[]`);
        lines.push('            {');
        for (const section of entry.sections) {
            const unsupported = section.unsupportedBaseParameters;
            const baseParameters = unsupported === null
                ? 'null'
                : unsupported.length === 0
                    ? 'Array.Empty<ShowcaseApiBaseParameter>()'
                    : `new ShowcaseApiBaseParameter[]\n                    {\n${unsupported
                        .map(parameter => `                        new("${escape(parameter.name)}", "${escape(parameter.note)}"),`)
                        .join('\n')}\n                    }`;
            const members = section.memberNames.length === 0
                ? 'Array.Empty<string>()'
                : `new[]\n                    {\n${section.memberNames
                    .map(name => `                        "${escape(name)}",`)
                    .join('\n')}\n                    }`;

            lines.push(`                new("${escape(section.title)}",`);
            lines.push(`                    ${baseParameters},`);
            lines.push(`                    ${members}),`);
        }
        lines.push('            },');
    }
    lines.push('        };');
    lines.push('    /// <summary>页面 id → 该页记录的接口 / 服务行（模型与服务方法）。</summary>');
    lines.push('    public static IReadOnlyDictionary<string, IReadOnlyList<ShowcaseApiMember>> Contracts { get; } =');
    lines.push('        new Dictionary<string, IReadOnlyList<ShowcaseApiMember>>(StringComparer.Ordinal)');
    lines.push('        {');
    for (const entry of entries) {
        const contracts = entry.extras.filter(row => row.kind === '接口');
        if (contracts.length === 0) continue;
        lines.push(`            ["${entry.id}"] = new ShowcaseApiMember[]`);
        lines.push('            {');
        for (const row of contracts) {
            const parts = [
                `"${escape(row.name ?? '')}"`,
                `"${escape(row.type ?? '')}"`,
                `"${escape(row.default ?? '')}"`,
                `"${escape(row.description ?? '')}"`,
                '"接口"',
                '""'
            ];
            lines.push(`                new(${parts.join(', ')}),`);
        }
        lines.push('            },');
    }
    lines.push('        };');
    lines.push('}');
    lines.push('');

    return lines.join('\n');
}

/// 只说“过期”会让人无从下手：列出前若干处差异，并单独指出行尾差异——那是平台差异
/// （检出时的 CRLF）而不是内容差异，两者要分清。
function reportDifference(current, generated) {
    const currentLines = current.split(/\r?\n/);
    const generatedLines = generated.split('\n');
    console.error(`  行数：仓库 ${currentLines.length}，生成 ${generatedLines.length}` +
        (current.includes('\r\n') ? '；仓库文件使用 CRLF，生成器输出 LF' : ''));

    let shown = 0;
    for (let index = 0; index < Math.max(currentLines.length, generatedLines.length) && shown < 10; index++) {
        if (currentLines[index] === generatedLines[index]) continue;
        console.error(`  L${index + 1}\n    仓库：${currentLines[index] ?? '<缺行>'}\n    生成：${generatedLines[index] ?? '<缺行>'}`);
        shown++;
    }

    if (shown === 0) console.error('  逐行比较没有差异，差异只可能来自行尾或末尾换行。');
}

const entries = pageEntries();
const generated = render(entries);

const totalRows = entries.reduce((total, entry) => total + entry.members.length + entry.extras.length, 0);

// 歧义只在生成时报告：`--check` 是门禁，成功时应当安静，否则 CI 日志里每次都会出现
// 一长串与本次改动无关的提示。
if (collisionWarnings.length > 0 && !process.argv.includes('--check')) {
    console.log(`同名参数已按类型前缀展开：${collisionWarnings.join(', ')}`);
}

if (process.argv.includes('--check')) {
    const current = existsSync(OUTPUT) ? read(OUTPUT) : '';
    if (current !== generated) {
        console.error(`${OUTPUT} 已过期：请运行 node scripts/generate-api-reference.mjs 后提交生成物。`);
        reportDifference(current, generated);
        process.exit(1);
    }

    console.log(`API reference is up to date (${entries.length} pages, ${totalRows} rows).`);
} else {
    writeFileSync(OUTPUT, generated);
    console.log(`Wrote ${OUTPUT} (${entries.length} pages, ${totalRows} rows).`);
}
