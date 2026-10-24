#!/usr/bin/env node
// 生成示例工程的 API 表数据。
//
// 手写的参数表会漂移：审查实测 8 个页面与组件的 [Parameter] 对不上（VirtualList 的整套选择
// API 一个字都没写），且没有任何编译期信号。生成物提交进仓库，CI 用 `--check` 拒绝过期产物
// （与 generate-fontawesome-icons.mjs 同一约定）。解析读源码文本而不是加载程序集：CI 的 Node
// 步骤没有 .NET 环境，而默认值 / XML 说明只有源码里才完整。
//
// 生成物三块：ByPage / Contracts（成员行，搜索索引也读它）、Sections（多组件页按组件类型分组
// + 基类参数支持矩阵，推导见 scripts/lib/showcase-support.mjs 与 scripts/api-groups.json）。
//
// 用法：node scripts/generate-api-reference.mjs [--check]

import { writeFileSync, existsSync, readdirSync } from 'node:fs';
import { join, basename } from 'node:path';
import { catalogIds, pascal, read } from './lib/showcase-catalog.mjs';
import { deriveBaseParameterSupport } from './lib/showcase-support.mjs';
import { loadApiContent } from './lib/showcase-api-content.mjs';

const LIBRARY = 'src/AeterniUI';
const SAMPLE = 'src/AeterniUI.Sample';
const OUTPUT = `${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`;

// 内部渲染部件（delivered-features §24/§37 标记为非稳定公共 API），不进示例页的公共表。
const INTERNAL_PARTS = new Set(['DateCalendar', 'TimeOptionList']);

// 基类参数说明在 ShowcaseApi.CommonParameters；支持矩阵由 lib/showcase-support.mjs 推导。
const BASE_PARAMETERS = new Set([
    'Id', 'Disabled', 'Visible', 'Class', 'Style', 'Element', 'AdditionalAttributes', 'ElementChanged'
]);

// 目录 id 与组件目录名不一致的显式例外：不做“猜名字”的推导。
const COMPONENT_DIRECTORY_OVERRIDES = {
    combobox: 'ComboBox'
};

// 不产出 API 表的页面：安装、设计基础与实验室页讲的是用法与 Token，没有单一组件。
const PAGES_WITHOUT_API = new Set(['install', 'colors', 'glass']);

// 中文说明、推不出来的行与「自哪个版本」统一在 scripts/api-content.json（校验见 lib）。
const API_CONTENT_FILE = 'scripts/api-content.json';

// 多组件合页分组与公共参数兜底：单组件页保持单表；有组件目录的多组件页自动拆组，顺序有
// 讲究或没有组件目录的页（反馈页 Dialog / Confirm / Alert / Toast）在这里登记；矩阵推导
// 不出来时用 baseParameterOverrides 兜底。
const GROUPS_FILE = 'scripts/api-groups.json';
const groupConfig = existsSync(GROUPS_FILE)
    ? JSON.parse(read(GROUPS_FILE))
    : { pages: {}, baseParameterOverrides: {} };

const BASE_PARAMETER_OVERRIDES = groupConfig.baseParameterOverrides ?? {};

// 组件目录名从一次目录列举里查，不用 existsSync：大小写不敏感的文件系统会把大小写不符的
// 目录解析成存在，同一份源码在 Windows 与 Linux 上生成不同页面集合，`--check` 却看不出差。
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

/// 跳过字符串字面量：扫描属性体与初始化表达式时要跨过 `"…;…"`，否则分号会被当成声明结束。
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

/// 找初始化表达式结束的 `;`：括号/花括号/字符串配对跳过，`new(…) { … }` 与多行不会提前截断。
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

/// 读出参数名之后的可选初始化表达式（`{ get; set; } = 99;` 里的 `99`）；表达式属性没有值。
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

/// 参数声明：`[Parameter]` 后跟属性声明；说明取上方最近的 XML 注释（没有就留空，不替作者编）。
/// 默认值不能用 `\{([^}]*)\}` 抓属性体：自动属性的初始值在右花括号之后，表达式属性又没有
/// `=`。因此只匹配参数名，再按 C# 结构读可选的 `= 表达式;`（花括号/括号/字符串都配对扫描）。
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

/// 同名参数可能由同一目录的不同类型声明（`Tabs` 与 `Tab` 各有一个 `Value`）。以前按“谁最后
/// 被读到”消解，生成物取决于文件系统列举顺序（CI 抓到过 Windows/Linux 不一致）；现在加类型名
/// 前缀全保留，顺序只由源码决定。
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

/// 默认值只在字面量或简单成员访问时写出；`new(...)`、方法调用对读者没有信息量。
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

const { descriptions, extraRows, since } = loadApiContent(API_CONTENT_FILE);

const compareCodePoints = (left, right) => (left < right ? -1 : left > right ? 1 : 0);

/// 分组配置：字符串条目等价于 `{ title, owners: [title] }`；没配置的页按组件目录自动分组。
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

/// 额外行的归属标记：取名字开头的类型（`DialogOptions.Title` → DialogOptions）；`*Changed`
/// 这类跨组件汇总行没有归属。
function extraRowOwner(name) {
    const match = /^([A-Z][A-Za-z0-9_]*)/.exec(name);
    return match ? match[1] : null;
}

/// 把页面成员拆成二级分组：单组件页保持一张表；多组件页每个组件一组（目录顺序或显式配置），
/// 其它归属各自成组，无归属汇总行进「共享约定」。分完必须正好覆盖全部成员否则抛错（漏行会
/// 让 API 表静默丢内容）。
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

    // 单组件页（及无组件的反馈页兜底）保持一张表：成员按名字排序，额外行按编辑顺序在后。
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
        // 没有对应组件类型的页（反馈页记录 IDialogService 与各选项类型）仍可有 API 表：行全部
        // 来自编辑文件的额外行。以前直接跳过，页面手写数组成了死代码，门禁还把它当例外放过。
        if (!directory) {
            const extras = extraRows[id] ?? [];
            if (extras.length === 0) continue;

            entries.push({
                id,
                directory: null,
                members: [],
                extras,
                // 没有组件类型：分组只按额外行归属拆，不列基类参数（不属于这里记录的类型）。
                sections: buildSections({ pageId: id, componentTypes: [], members: [], emittedExtras: extras.filter(row => row.kind !== '接口') })
                    .map(section => ({ ...section, unsupportedBaseParameters: null }))
            });
            continue;
        }

        const dir = join(LIBRARY, 'Components', directory);
        // 文件名排序：同名成员谁先出现会影响合并，顺序必须由源码而非文件系统列举决定。
        const files = readdirSync(dir)
            .filter(file => file.endsWith('.razor.cs') && !INTERNAL_PARTS.has(basename(file, '.razor.cs')))
            .sort();

        const parsed = files.flatMap(file => parseMembers(basename(file, '.razor.cs'), read(join(dir, file))));
        const { members: qualified, collisions } = qualify(parsed, id);
        for (const collision of collisions) collisionWarnings.push(collision);

        // 歧义行在编辑文件里按带前缀的键存（`Radio.AriaLabel`），这里按同一把键回查。
        const extraDescriptions = new Map((extraRows[id] ?? []).map(row => [row.name, row.description]));

        const members = qualified.map(member => ({
            ...member,
            // 说明优先取编辑文件：源码 XML 注释只覆盖一部分，中文散文一直在这里维护。
            description: descriptions[id]?.[member.qualified ?? member.name]
                ?? descriptions[id]?.[member.name]
                ?? extraDescriptions.get(member.name)
                ?? member.description,
            // 歧义成员已展开成「声明类型.成员名」，再拼 `${directory}.` 会得到
            // 「DatePicker.DatePicker.ShowLunar」这类查不到的键，所以按「类型名.成员名」直查。
            since: since.members[`${directory}.${member.name}`] ?? since.members[member.name] ?? ''
        }));

        const extras = extraRows[id] ?? [];
        if (members.length === 0 && extras.length === 0) continue;

        // 排序必须是代码点序，不能用 localeCompare：ICU 规则随 Node 构建变化，生成物会在
        // CI 与本地不一致，`--check` 报“过期”却看不出差（CI 抓到过一次）。
        const sortedMembers = members.sort((left, right) => (left.name < right.name ? -1 : left.name > right.name ? 1 : 0));

        // 额外行排在组件成员之后。分组只关心非接口行（接口行由 Contracts 单独渲染），去重
        // 规则必须和 render() 一致，否则分组会引用不存在的成员。
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

// 歧义只在生成时报告：`--check` 成功时应安静，否则 CI 日志每次都有无关提示。
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
