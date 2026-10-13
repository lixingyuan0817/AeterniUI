#!/usr/bin/env node
// 生成示例文档的搜索索引：Cmd/Ctrl+K 命令面板的数据源。
//
// 索引 = 目录条目（页面本身）+ 页面上真实存在的段（何时使用、每张 demo 卡、参数与事件、
// 相关组件、说明与边界）。段从页面源码解析，锚点必须与 `ComponentPage` / `DemoCard` 渲染出来
// 的 id 完全一致：手写这份清单会在页面改名时静默留下死链，而「点搜索结果什么都不发生」属于
// 最难发现的一类缺陷。同理，只登记页面上真的存在的段——没有 `<WhenToUse>` 的页面不会出现在
// 「何时使用」里。
//
// 「参数与事件」段的存在与否由 `ShowcaseApiReference` 决定（页面只在生成物里有该页的键时才
// 渲染 API 段），所以这里读同一份生成物，而不是自己再推一遍「哪个 id 对应哪个组件目录」。
// 生成顺序因此是：generate-api-reference.mjs → generate-search-index.mjs。
//
// 生成物提交进仓库，CI 用 `--check` 拒绝过期产物（与 `generate-api-reference.mjs`、
// `generate-fontawesome-icons.mjs` 同一约定）。
//
// 用法：node scripts/generate-search-index.mjs [--check]

import { readFileSync, writeFileSync, existsSync } from 'node:fs';

const SAMPLE = 'src/AeterniUI.Sample';
const PAGES = `${SAMPLE}/Pages/Components`;
const CATALOG = `${SAMPLE}/Components/Showcase/ShowcaseCatalog.cs`;
const API_REFERENCE = `${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`;
const OUTPUT = `${SAMPLE}/Components/Showcase/ShowcaseSearchIndex.generated.cs`;

// 固定段的锚点后缀与显示标题：`ComponentPage` 用同样的方式推导自己的段标题
// （`{AnchorId}-when-title` 等），两处必须一致，否则目录与搜索结果会指向不同的 id。
const FIXED_SECTIONS = {
    when: { suffix: '-when-title', title: '何时使用' },
    api: { suffix: '-api-title', title: '参数与事件' },
    related: { suffix: '-related-title', title: '相关组件' },
    notes: { suffix: '-notes-title', title: '说明与边界' }
};

const read = path => readFileSync(path, 'utf8').replace(/^\uFEFF/, '');

const pascal = id => id.split('-').map(part => part[0].toUpperCase() + part.slice(1)).join('');

/// 目录条目：顺序、页面文件与分组名（= 页面上的 Category）的唯一事实源。
function catalogEntries() {
    const source = read(CATALOG);
    const entries = [];

    for (const group of source.matchAll(/new\("([^"]+)",\s*FontAwesomeIcons\.Solid\.\w+,\s*\[([\s\S]*?)\]\)/g)) {
        for (const item of group[2].matchAll(/new\("([a-z0-9-]+)",\s*"([^"]+)",\s*"([^"]+)"\)(\s*\{[^}]*\})?/g)) {
            const overrides = item[4] ?? '';
            entries.push({
                id: item[1],
                title: item[2],
                category: group[1],
                page: /Page\s*=\s*"([^"]+)"/.exec(overrides)?.[1] ?? pascal(item[1])
            });
        }
    }

    if (entries.length < 50) {
        throw new Error(`只从目录里解析到 ${entries.length} 个条目，解析逻辑可能已失效`);
    }

    return entries;
}

/// 页面 id → 已有生成式 API 表。页面上的「参数与事件」段正是按这份键集渲染的。
function pagesWithApi() {
    return new Set([...read(API_REFERENCE).matchAll(/\["([a-z0-9-]+)"\] = new ShowcaseApiMember\[\]/g)]
        .map(match => match[1]));
}

/// 页面里真实存在的段，按阅读顺序：何时使用 → demo 卡 → API → 相关组件 → 说明与边界。
/// 非组件页（安装、设计基础、实验室）没有这个骨架，返回空数组，只登记页面条目本身。
function sectionsOf(entry, apiPages) {
    const source = read(`${PAGES}/${entry.page}.razor`);
    const componentPage = /<ComponentPage\b[^>]*>/.exec(source);
    if (!componentPage) {
        return [];
    }

    const opening = componentPage[0];
    const anchor = /AnchorId="([^"]+)"/.exec(opening)?.[1] ?? '';
    const category = /Category="([^"]+)"/.exec(opening)?.[1] ?? '';
    if (anchor !== entry.id || category !== entry.category) {
        throw new Error(`${entry.page}.razor 的 AnchorId/Category 与目录不一致：` +
            `AnchorId=${anchor}（目录 ${entry.id}）、Category=${category}（目录 ${entry.category}）`);
    }

    const sections = [];
    if (/<WhenToUse\b/.test(source)) {
        sections.push({ id: `${anchor}${FIXED_SECTIONS.when.suffix}`, title: FIXED_SECTIONS.when.title });
    }

    for (const card of source.matchAll(/<DemoCard\b[^>]*>/gs)) {
        const id = /Id="([^"]+)"/.exec(card[0])?.[1];
        const title = /Title="([^"]+)"/.exec(card[0])?.[1];
        if (!id || !title) {
            throw new Error(`${entry.page}.razor 里有缺少字面 Id/Title 的 DemoCard：${card[0].slice(0, 80)}`);
        }

        sections.push({ id, title });
    }

    if (apiPages.has(entry.id)) {
        sections.push({ id: `${anchor}${FIXED_SECTIONS.api.suffix}`, title: FIXED_SECTIONS.api.title });
    }

    if (/\bRelated="/.test(opening)) {
        sections.push({ id: `${anchor}${FIXED_SECTIONS.related.suffix}`, title: FIXED_SECTIONS.related.title });
    }

    if (/<Notes\b/.test(source)) {
        sections.push({ id: `${anchor}${FIXED_SECTIONS.notes.suffix}`, title: FIXED_SECTIONS.notes.title });
    }

    return sections;
}

/// 页面条目的标题：组件页以自己的 `Title` 为准（它就是页面上的 h1），非组件页退到目录标题。
/// 锚点取页面自己给出的落地 id——组件页是 `AnchorId`（`<article id="@AnchorId">`），
/// 其余页面是其首个带 id 的元素，因此每条搜索结果都落到真实存在的元素上。
function pageTitleOf(entry) {
    const source = read(`${PAGES}/${entry.page}.razor`);
    const opening = /<ComponentPage\b[^>]*>/.exec(source);
    if (opening) {
        return /Title="([^"]+)"/.exec(opening[0])?.[1] ?? entry.title;
    }

    const anchor = /id="([^"]+)"/.exec(source)?.[1];
    if (!anchor) {
        throw new Error(`${entry.page}.razor 既没有组件页骨架，也没有带 id 的元素，页面条目无法落地`);
    }

    return entry.title;
}

/// 页面条目的锚点。非组件页没有 `AnchorId`，用它自己的落地 id（首个带 id 的元素），
/// 否则「安装」这类条目会指向一个不存在的锚点。
function pageAnchorOf(entry) {
    const source = read(`${PAGES}/${entry.page}.razor`);
    const opening = /<ComponentPage\b[^>]*>/.exec(source);
    return opening ? entry.id : /id="([^"]+)"/.exec(source)?.[1];
}

const apiPages = pagesWithApi();
const entries = [];

for (const catalogEntry of catalogEntries()) {
    const pageTitle = pageTitleOf(catalogEntry);
    const anchor = pageAnchorOf(catalogEntry);
    entries.push({
        pageId: catalogEntry.id,
        pageTitle,
        category: catalogEntry.category,
        sectionId: anchor,
        sectionTitle: pageTitle
    });

    for (const section of sectionsOf(catalogEntry, apiPages)) {
        entries.push({
            pageId: catalogEntry.id,
            pageTitle,
            category: catalogEntry.category,
            sectionId: section.id,
            sectionTitle: section.title
        });
    }
}

function escape(value) {
    return value.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

function render(list) {
    const lines = [];
    lines.push('// <auto-generated />');
    lines.push('// 由 scripts/generate-search-index.mjs 生成，请勿手工编辑。');
    lines.push('// 索引 = 目录条目（页面本身）+ 页面上真实存在的段；段锚点与 ComponentPage / DemoCard');
    lines.push('// 渲染出来的 id 一致。「参数与事件」段的存在与否取自 ShowcaseApiReference 生成物，因此');
    lines.push('// 生成顺序是先 `node scripts/generate-api-reference.mjs` 再运行本脚本；CI 用');
    lines.push('// `node scripts/generate-search-index.mjs --check` 拒绝过期产物。');
    lines.push('');
    lines.push('using System;');
    lines.push('using System.Collections.Generic;');
    lines.push('');
    lines.push('namespace AeterniUI.Sample.Components.Showcase;');
    lines.push('');
    lines.push('public static class ShowcaseSearchIndex');
    lines.push('{');
    lines.push('    /// <summary>页面与页内段的扁平索引，顺序即目录顺序（每页先页面本身，再页内各段）。</summary>');
    lines.push('    public static IReadOnlyList<ShowcaseSearchEntry> Entries { get; } =');
    lines.push('    [');

    let previousPage = null;
    for (const entry of list) {
        if (previousPage !== null && entry.pageId !== previousPage) {
            lines.push('');
        }

        previousPage = entry.pageId;
        const parts = [
            `"${escape(entry.pageId)}"`,
            `"${escape(entry.pageTitle)}"`,
            `"${escape(entry.category)}"`,
            `"${escape(entry.sectionId)}"`,
            `"${escape(entry.sectionTitle)}"`
        ];
        lines.push(`        new(${parts.join(', ')}),`);
    }

    lines.push('    ];');
    lines.push('}');
    lines.push('');

    return lines.join('\n');
}

const generated = render(entries);

/// 只说“过期”会让人无从下手：列出前若干处差异，并单独指出行尾差异——那是平台差异
/// （检出时的 CRLF）而不是内容差异。与 generate-api-reference.mjs 的诊断保持一致。
function reportDifference(current, expected) {
    const currentLines = current.split(/\r?\n/);
    const expectedLines = expected.split('\n');
    console.error(`  行数：仓库 ${currentLines.length}，生成 ${expectedLines.length}` +
        (current.includes('\r\n') ? '；仓库文件使用 CRLF，生成器输出 LF' : ''));

    let shown = 0;
    for (let index = 0; index < Math.max(currentLines.length, expectedLines.length) && shown < 10; index++) {
        if (currentLines[index] === expectedLines[index]) continue;
        console.error(`  L${index + 1}\n    仓库：${currentLines[index] ?? '<缺行>'}\n    生成：${expectedLines[index] ?? '<缺行>'}`);
        shown++;
    }

    if (shown === 0) console.error('  逐行比较没有差异，差异只可能来自行尾或末尾换行。');
}

if (process.argv.includes('--check')) {
    const current = existsSync(OUTPUT) ? read(OUTPUT) : '';
    if (current !== generated) {
        console.error(`${OUTPUT} 已过期：请运行 node scripts/generate-search-index.mjs 后提交生成物。`);
        reportDifference(current, generated);
        process.exit(1);
    }

    const pages = new Set(entries.map(entry => entry.pageId));
    console.log(`Search index is up to date (${entries.length} entries across ${pages.size} pages).`);
} else {
    writeFileSync(OUTPUT, generated);
    const pages = new Set(entries.map(entry => entry.pageId));
    console.log(`Wrote ${OUTPUT} (${entries.length} entries across ${pages.size} pages).`);
}
