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
// 用法：node scripts/generate-api-reference.mjs [--check]

import { readFileSync, writeFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join, basename } from 'node:path';

const LIBRARY = 'src/AeterniUI';
const SAMPLE = 'src/AeterniUI.Sample';
const CATALOG = `${SAMPLE}/Components/Showcase/ShowcaseCatalog.cs`;
const OUTPUT = `${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`;

// 内部渲染部件：delivered-features §24/§37 明确它们保留 Razor 类型只为内部组合，已标记
// 为非稳定公共 API，因此不进示例页的公共表。
const INTERNAL_PARTS = new Set(['DateCalendar', 'TimeOptionList']);

// 基类参数由 ComponentPreview 的 CommonParameters 统一登记，页面不逐条重复。
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

const read = path => readFileSync(path, 'utf8').replace(/^\uFEFF/, '');

function pascal(parts) {
    return parts.split('-').map(part => part[0].toUpperCase() + part.slice(1)).join('');
}

function catalogIds() {
    const source = read(CATALOG);
    return [...source.matchAll(/new\("([a-z0-9-]+)",/g)].map(match => match[1]);
}

/// 组件目录名：目录 id 的 Pascal 形式，或显式例外。
function componentDirectory(id) {
    const explicit = COMPONENT_DIRECTORY_OVERRIDES[id];
    if (explicit) return explicit;
    const derived = pascal(id);
    return existsSync(join(LIBRARY, 'Components', derived)) ? derived : null;
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

/// 参数声明：`[Parameter]`（可带 CapturingUnmatchedValues）后跟属性声明。说明取该成员上方
/// 最近的 XML 文档注释——没有注释就是空说明，生成器不会替作者编。
function parseMembers(source) {
    const members = [];
    const pattern = /((?:\s*\/\/\/[^\n]*\n)*)\s*\[Parameter[^\]]*\]\s*public\s+([\w<>?,\[\]\.\s]+?)\s+(\w+)\s*\{([^}]*)\}/g;

    for (const match of source.matchAll(pattern)) {
        const [, doc, type, name, body] = match;
        if (BASE_PARAMETERS.has(name)) continue;

        const initializer = /=\s*([^;]+);/.exec(body)?.[1]?.trim() ?? '';
        members.push({
            name,
            type: type.replace(/\s+/g, ' ').trim(),
            defaultValue: simpleDefault(initializer),
            description: cleanDocComment(doc),
            kind: /EventCallback/.test(type) ? '事件回调' : '属性'
        });
    }

    return members;
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

const since = existsSync(SINCE_FILE)
    ? JSON.parse(read(SINCE_FILE))
    : { members: {} };

const descriptions = existsSync(DESCRIPTIONS_FILE)
    ? JSON.parse(read(DESCRIPTIONS_FILE)).members
    : {};

const extraRows = existsSync(EXTRA_ROWS_FILE)
    ? JSON.parse(read(EXTRA_ROWS_FILE)).rows
    : {};

function pageEntries() {
    const entries = [];
    for (const id of catalogIds()) {
        if (PAGES_WITHOUT_API.has(id)) continue;

        const directory = componentDirectory(id);
        if (!directory) continue;

        const dir = join(LIBRARY, 'Components', directory);
        const source = readdirSync(dir)
            .filter(file => file.endsWith('.razor.cs') && !INTERNAL_PARTS.has(basename(file, '.razor.cs')))
            .map(file => read(join(dir, file)))
            .join('\n');

        const members = parseMembers(source).map(member => ({
            ...member,
            // 说明优先取编辑文件：源码里的 XML 注释只覆盖了一部分成员，而中文散文一直是
            // 写在页面上的，抽出来之后就以这个文件为准。
            description: descriptions[id]?.[member.name] ?? member.description,
            since: since.members[`${directory}.${member.name}`] ?? ''
        }));

        if (members.length === 0) continue;

        entries.push({
            id,
            directory,
            // 排序必须是代码点序，不能用 localeCompare：ICU 的排序规则随 Node 构建变化，
            // 生成物会因此在 CI 与本地不一致，`--check` 报“过期”却看不出差在哪（本批真的
            // 被 CI 抓到过一次）。生成物的内容只能取决于源码。
            members: members.sort((left, right) => (left.name < right.name ? -1 : left.name > right.name ? 1 : 0)),
            // 额外行排在组件成员之后：它们记录的是同页的模型、服务与汇总写法，读者先看组件本身。
            extras: extraRows[id] ?? []
        });
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
        for (const member of [...entry.members, ...entry.extras.filter(row => row.kind !== '接口')]) {
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

const entries = pageEntries();
const generated = render(entries);

if (process.argv.includes('--check')) {
    const current = existsSync(OUTPUT) ? read(OUTPUT) : '';
    if (current !== generated) {
        console.error(`${OUTPUT} 已过期：请运行 node scripts/generate-api-reference.mjs 后提交生成物。`);
        process.exit(1);
    }

    console.log(`API reference is up to date (${entries.length} pages, ` +
        `${entries.reduce((total, entry) => total + entry.members.length, 0)} members).`);
} else {
    writeFileSync(OUTPUT, generated);
    console.log(`Wrote ${OUTPUT} (${entries.length} pages, ` +
        `${entries.reduce((total, entry) => total + entry.members.length, 0)} members).`);
}
