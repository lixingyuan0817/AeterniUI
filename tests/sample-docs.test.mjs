// 示例工程的内容门禁。
//
// 示例页是组件库的可运行契约，但它的 API 表是手写的，而手写的表会漂移：审查发现
// 8 个页面登记的参数数量与组件的 `[Parameter]` 对不上（`VirtualList` 的整套选择 API
// 一个字都没写）。文档漂移用 `check-doc-drift.mjs` 拦，示例页的这一层此前没有门禁，
// 所以这里补上，并且只断言能从源码直接推出的事实：
//
//   1. 目录里的每个条目都有真正的页面文件，且路由与条目 id 一致（侧栏的“在 GitHub
//      编辑”链接就是按文件名拼的，文件名错了只会得到一个 404）。
//   2. 组件的公共表由 `scripts/generate-api-reference.mjs` 生成，页面不再手写；因此这里
//      断言的是“生成物覆盖了每个有组件的条目”，以及“页面没有退回手写表”。生成物本身是否
//      与源码一致由 CI 的 `--check` 负责——那需要重新生成，属于独立的一步。
//   3. 提供代码块的页面必须真的定义了该常量，且片段用到了它宣称记录的组件。
//   4. 生成物里的默认值与源码一致（自动属性的初始值曾经整批丢失，页面上就会写错行为）。
//   5. 示例页里的组件标签只能使用该组件真实的 `[Parameter]` 或 HTML 原生属性；Razor 的
//      `@on*` / `@bind-*` 等指令语法与泛型类型参数（`TItem`）按语法放行。
//   6. 多组件合页的 API 按组件类型分组，且分组正好覆盖页面的全部成员（模板与分组来自生成物
//      的 `Sections`，`ShowcaseApiTable` 按组渲染，不再是一张混表）。
//   7. 基类参数带支持矩阵：不渲染 DOM 或明确不消费该参数的组件会被标成「不支持 + 原因」，
//      矩阵从组件源码结构推导（`scripts/lib/showcase-support.mjs`），不是每个页面无条件列出。
//   8. API 表的编辑内容只有一个维护入口（`scripts/api-content.json` 的页内 descriptions /
//      extraRows / since）；拆成三个 JSON 的旧布局不再允许出现。
//   9. 搜索索引生成物按页面分组（页面字段只存一次），消费端摊平后顺序必须与目录一致。
//
// 有意不做的事：不校验参数说明的文字，也不校验示例的视觉结果——那属于浏览器检查。
import assert from 'node:assert/strict';
import { catalogEntries, pascal, read } from '../scripts/lib/showcase-catalog.mjs';
import { loadApiContent } from '../scripts/lib/showcase-api-content.mjs';
import { existsSync, readdirSync } from 'node:fs';
import { basename, join } from 'node:path';
import { test } from 'node:test';

const SAMPLE = 'src/AeterniUI.Sample';
const LIBRARY = 'src/AeterniUI';
const PAGES = `${SAMPLE}/Pages/Components`;

// AeterniComponent 的公共参数由 ShowcaseApi.CommonParameters 统一登记，
// 页面不必逐条重复。
const BASE_PARAMETERS = new Set([
    'Id', 'Disabled', 'Visible', 'Class', 'Style', 'Element', 'AdditionalAttributes', 'ElementChanged'
]);

// 内部渲染部件：delivered-features §24/§37 明确它们保留 Razor 类型只是为了内部组合，
// 已标记为非稳定公共 API，因此不在页面必须登记的公共面里。
const INTERNAL_PARTS = new Set(['DateCalendar', 'TimeOptionList']);

// 非组件页：安装指南、设计基础与对比实验。它们讲步骤、规格与配方，没有单一组件，因此
// 没有 demo 卡片，也没有生成式 API 表；但它们与组件页共用 `ComponentPage` 骨架（页头 →
// 何时使用 → 内容 → API → 相关组件 → 说明与边界），段齐全与"有可复制代码块"同样要求，
// 只有"每段 demo 都有自己的标题与代码"这条换成"至少一个 ShowcaseCode 代码块"。
const NON_COMPONENT_PAGES = new Set(['install', 'colors']);

/// 页面骨架的锚点（也是目录条目 id）。没有 `ComponentPage` 骨架的页面返回 null。
const pageIdOf = source => /AnchorId="([a-z0-9-]+)"/.exec(source)?.[1] ?? null;

/// 取页面里的开标签（引号与 `@(...)` 感知）：`For="() => Project.Name"` 里的 `>` 不是标签结束，
/// 表达式里的引号也不参与配对，所以不能用一个 `[^>]*>` 正则了事。
function openingTags(source, names) {
    const pattern = new RegExp(`<(${names.join('|')})\\b`, 'g');
    const tags = [];

    for (const match of source.matchAll(pattern)) {
        const start = match.index;
        let cursor = start;
        let quote = null;
        let depth = 0;

        while (cursor < source.length) {
            const character = source[cursor];
            if (depth > 0) {
                if (character === '(') depth += 1;
                else if (character === ')') depth -= 1;
            } else if (quote) {
                if (character === quote) quote = null;
            } else if (character === '"' || character === "'") {
                quote = character;
            } else if (character === '@' && source[cursor + 1] === '(') {
                depth = 1;
                cursor += 1;
            } else if (character === '>') {
                cursor += 1;
                break;
            }

            cursor += 1;
        }

        tags.push(source.slice(start, cursor));
    }

    return tags;
}

/// 开标签里是否有落单的双引号。属性值里出现未转义的 `"` 时 Razor 会把它截断，剩下的文本被当成
/// 属性名——编译期毫无提示，运行时整页渲染失败（实测：只渲染出第一段 demo，报 "does not have a
/// property matching the name …"）。`@(...)` 表达式里的引号是合法内容，先整体换成占位符，
/// 再把成对的 `name="value"` 删掉：剩下的任何双引号都是被截断的属性值留下的。
function unpairedQuotes(tag) {
    let stripped = '';
    let quote = null;
    let depth = 0;

    for (let cursor = 0; cursor < tag.length; cursor += 1) {
        const character = tag[cursor];
        if (depth > 0) {
            if (character === '(') depth += 1;
            else if (character === ')') {
                depth -= 1;
                if (depth === 0) stripped += '@()';
            }

            continue;
        }

        if (quote) {
            // `attr="@("...")"` 也是合法的表达式值：引号紧跟 `@(`，内层字符串的引号不参与配对。
            if (character === '@' && tag[cursor + 1] === '(') {
                depth = 1;
                cursor += 1;
                continue;
            }
            stripped += character;
            if (character === quote) quote = null;
            continue;
        }

        if (character === '"' || character === "'") {
            quote = character;
            stripped += character;
            continue;
        }

        if (character === '@' && tag[cursor + 1] === '(') {
            depth = 1;
            cursor += 1;
            continue;
        }

        stripped += character;
    }

    return stripped.replace(/[\w:.@-]+="[^"]*"/g, '').includes('"');
}

function pageNames(path) {
    const names = new Set();
    for (const match of read(path).matchAll(/new\("([^"]+)"/g)) {
        for (const part of match[1].split('/')) {
            const token = part.trim().split('.').pop().trim();
            if (/^\w+$/.test(token) || token.includes('*')) names.add(token);
        }
    }
    return names;
}

function componentParameters(directory) {
    const names = new Set();
    const dir = join(LIBRARY, 'Components', directory);
    for (const file of readdirSync(dir)) {
        if (!file.endsWith('.razor.cs')) continue;
        if (INTERNAL_PARTS.has(file.slice(0, -'.razor.cs'.length))) continue;

        const source = read(join(dir, file));
        for (const match of source.matchAll(/\[Parameter[^\]]*\]\s*public\s+[\w<>?,\[\]\.\s]+?\s+(\w+)\s*\{/g)) {
            names.add(match[1]);
        }
    }

    for (const name of BASE_PARAMETERS) names.delete(name);
    return names;
}

/// 页面允许用一条汇总行覆盖一族参数（`ValueChanged / *Changed`、`MinLength / MaxLength`）。
/// 通配符只在它自己的字面部分够长时才生效，避免一个裸 `*` 匹配掉所有参数。
function covered(token, listed) {
    if (listed.has(token)) return true;
    return [...listed].some(entry => {
        if (!entry.includes('*')) return false;
        const literals = entry.replace(/\*/g, '');
        if (literals.length < 3) return false;
        const pattern = new RegExp(`^${entry.split('*').map(part => part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('.*')}$`);
        return pattern.test(token);
    });
}

const entries = catalogEntries();
const componentDirs = new Set(readdirSync(join(LIBRARY, 'Components')).filter(name => name !== 'AeterniComponent.cs'));

test('目录里的每个条目都有页面文件，且路由与条目 id 一致', () => {
    assert.ok(entries.length >= 50, `只从目录里解析到 ${entries.length} 个条目，解析逻辑可能已失效`);

    for (const entry of entries) {
        const file = `${PAGES}/${entry.page ?? pascal(entry.id)}.razor`;
        assert.ok(existsSync(file), `目录条目 ${entry.id} 没有页面文件 ${file}`);

        const expectedRoute = entry.id === 'install' ? '/components' : `/components/${entry.id}`;
        assert.match(read(file), new RegExp(`@page "${expectedRoute}"`), `${file} 的路由与条目 id ${entry.id} 不一致`);
    }
});

test('每个有组件的条目都出现在生成的 API 表里，且页面不再手写表', () => {
    const generated = read(`${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`);
    const covered = new Set([...generated.matchAll(/\["([a-z0-9-]+)"\] = new ShowcaseApiMember\[\]/g)].map(match => match[1]));

    // 安装、设计基础与实验室页讲的是用法与 Token，没有单一组件，因此没有 API 表。
    // 反馈页记录的是服务与选项类型，没有对应组件目录，它的行全部来自编辑文件的额外行
    // （`scripts/api-content.json` → 生成器在成员为空时也为它产出条目）。这里不再把它
    // 当例外：例外曾经让它带着三张没人消费的数组通过门禁，页面上其实没有 API 区。
    const noComponent = NON_COMPONENT_PAGES;

    const missing = entries
        .filter(entry => !noComponent.has(entry.id) && (componentDirs.has(entry.page ?? pascal(entry.id)) || entry.id === 'feedback'))
        .filter(entry => !covered.has(entry.id))
        .map(entry => entry.id);
    assert.deepEqual(missing, [], `这些条目没有生成 API 表：${missing.join(', ')}`);

    const handWritten = [];
    for (const entry of entries) {
        if (noComponent.has(entry.id)) continue;
        const file = `${PAGES}/${entry.page ?? pascal(entry.id)}.razor`;
        if (!existsSync(file)) continue;
        if (/ShowcaseParameter\[\]/.test(read(file))) handWritten.push(entry.id);
    }
    assert.deepEqual(handWritten, [], `这些页面仍在手写 API 表：${handWritten.join(', ')}`);
});

test('卡片化页面：每段代码都声明了，且片段用到了本页记录的组件', () => {
    const files = readdirSync(PAGES).filter(name => name.endsWith('.razor'));

    // 一页可能记录多个组件（Radio 与 RadioGroup、List 与 ListItem……），因此"片段里出现的
    // 类型"取本页记录过的类型集合：主类型来自 ComponentName，其余来自携带类型前缀的记录行。
    const extras = loadApiContent().extraRows;
    const generated = read(`${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`);
    let cards = 0;

    for (const file of files) {
        const source = read(join(PAGES, file));
        if (!source.includes('<ComponentPage')) continue;

        const page = /AnchorId="([a-z0-9-]+)"/.exec(source)?.[1] ?? '';
        const name = /ComponentName="([^"]+)"/.exec(source)?.[1] ?? '';
        const documented = new Set([name.split(/[<(&（]/)[0].trim()]);

        // 一页可能记录多个组件（Radio 与 RadioGroup、List 与 ListItem），所以要把它记录过的
        // 类型都算进来：生成物里带类型前缀的成员名给出主目录中的类型，额外行给出模型与服务。
        const pageMembers = new RegExp(`\\["${page}"\\] = new ShowcaseApiMember\\[\\]\\r?\\n\\s*\\{([\\s\\S]*?)\\n\\s*\\},`)
            .exec(generated)?.[1] ?? '';
        for (const member of pageMembers.matchAll(/new\("([^"]+)"/g)) {
            const owner = member[1].split('.')[0];
            if (/^[A-Z]/.test(owner)) documented.add(owner);
        }

        for (const row of extras[page] ?? []) {
            for (const part of row.name.split('/')) {
                const owner = part.trim().split('.')[0];
                if (owner && /^[A-Z]/.test(owner)) documented.add(owner);
            }
        }

        // 片段与页面的相关性按"整页至少有一处"判定：一页可能同时给片段的形态与小节片段
        // （安装页的四步、色彩页的 CSS 与 C#），要求每一段都点名本页类型会逼出注水注释。
        // 每个片段本身仍必须真的声明过——那一条在上面单独断言。
        let mentionsOwner = false;
        for (const match of source.matchAll(/Code="@(\w+)"/g)) {
            const constant = match[1];
            // 用 indexOf 取原始字符串字面量，不走正则：跨语言的转义（\r?\n、[\s\S]）
            // 在生成测试文件时极易被吃掉，这一处已经被自己踩过一次。
            const marker = `private const string ${constant} = """`;
            const start = source.indexOf(marker);
            const end = start < 0 ? -1 : source.indexOf('"""', start + marker.length);
            const snippet = start < 0 || end < 0 ? undefined : source.slice(start + marker.length, end);
            assert.ok(snippet !== undefined, `${file} 引用了未声明的代码常量 ${constant}`);
            if ([...documented].some(type => snippet.includes(type))) mentionsOwner = true;
            cards += 1;
        }

        assert.ok(mentionsOwner,
            `${file} 的所有代码片段都没有出现本页记录的组件（${[...documented].join(' / ')}）`);
    }

    assert.ok(cards > 0, '示例工程里没有任何代码片段，说明本门禁已失效');
});

test('组件页的 demo 锚点唯一，每段都有自己的标题与代码', () => {
    const files = readdirSync(PAGES).filter(name => name.endsWith('.razor'));
    const migrated = [];

    for (const file of files) {
        const source = read(join(PAGES, file));
        if (!source.includes('<ComponentPage')) continue;
        if (NON_COMPONENT_PAGES.has(pageIdOf(source))) continue;
        migrated.push(file);

        const ids = [...source.matchAll(/<DemoCard\s+Id="([^"]+)"/g)].map(match => match[1]);
        assert.ok(ids.length >= 2, `${file} 至少要有两段 demo——卡片化的意义就在这里`);
        assert.equal(new Set(ids).size, ids.length, `${file} 的 demo 锚点必须唯一（右栏 TOC 与分享链接都指向它）`);

        for (const id of ids) {
            const start = source.indexOf(`<DemoCard Id="${id}"`);
            const block = source.slice(start, source.indexOf('</DemoCard>', start));
            assert.match(block, /Title="/, `${file} 的 ${id} 缺少标题`);
            assert.match(block, /Code="@\w+"/, `${file} 的 ${id} 缺少代码片段`);
        }
    }

    assert.ok(migrated.length > 0, '没有任何卡片化页面，说明本门禁已失效');
});

test('每个页面都有可复制代码块，且写了三段固定段', () => {
    const files = readdirSync(PAGES).filter(name => name.endsWith('.razor'));
    const withoutCode = [];
    const withoutSections = [];

    for (const file of files) {
        const source = read(join(PAGES, file));
        if (![...source.matchAll(/Code="@(\w+)"/g)].length) withoutCode.push(file);

        if (!source.includes('<ComponentPage')) continue;

        // 三段固定段：何时使用 / 相关组件 / 说明与边界。`ComponentPage` 只在参数非空时渲染它们，
        // 所以"页面写了段标记"就是"页面渲染了该段"——这条让"每页 01～05 段齐全"不再靠人工核对。
        const missing = ['<WhenToUse', 'Related="', '<Notes'].filter(marker => !source.includes(marker));
        if (missing.length > 0) withoutSections.push(`${file} 缺 ${missing.join(' / ')}`);
    }

    assert.deepEqual(withoutCode, [], `这些页面没有可复制代码块：${withoutCode.join(', ')}`);
    assert.deepEqual(withoutSections, [], `这些页面缺少固定段：${withoutSections.join('; ')}`);
});

test('非组件页（安装 / 设计基础 / 实验台）按同一模板组织', () => {
    const byId = new Map(entries.map(entry => [entry.id, entry]));

    for (const id of NON_COMPONENT_PAGES) {
        const entry = byId.get(id);
        assert.ok(entry, `目录里没有 ${id} 这个条目`);
        const file = `${PAGES}/${entry.page ?? pascal(entry.id)}.razor`;
        const source = read(file);

        assert.ok(source.includes('<ComponentPage'), `${file} 应与组件页共用页面骨架`);
        assert.equal(pageIdOf(source), id, `${file} 的骨架锚点应与目录 id 一致`);

        // 代码块用共享的 `ShowcaseCode`（带复制按钮），锚点唯一：复制按钮就是按它取文本的。
        const ids = [...source.matchAll(/<ShowcaseCode\s+Id="([^"]+)"/g)].map(match => match[1]);
        assert.ok(ids.length >= 2, `${file} 至少要有两个可复制代码块`);
        assert.equal(new Set(ids).size, ids.length, `${file} 的代码块锚点必须唯一`);
    }
});

test('开标签里的引号都成对（Razor 会把未转义的属性值截断）', () => {
    const files = readdirSync(PAGES).filter(name => name.endsWith('.razor'));
    const broken = [];

    for (const file of files) {
        const source = read(join(PAGES, file));
        for (const tag of openingTags(source, ['ComponentPage', 'DemoCard', 'ShowcaseCode'])) {
            if (unpairedQuotes(tag)) broken.push(`${file} 的 <${/^<(\w+)/.exec(tag)?.[1] ?? '?'}>`);
        }
    }

    assert.deepEqual(broken, [],
        `这些开标签里有未转义的引号，属性会被截断、整页渲染失败：${broken.join('; ')}`);
});

test('批次 7 骨架：版本入口与反馈闭环都存在', () => {
    const layout = read(`${SAMPLE}/Layout/MainLayout.razor`);
    assert.match(layout, /class="app-version"/, '页头应有版本入口');

    const page = read(`${SAMPLE}/Components/Showcase/ComponentPage.razor`);
    assert.ok(page.includes('<ShowcaseFeedback'), '组件页骨架末尾应带反馈闭环');
});

/// 递归收集目录下带指定后缀的文件。示例页按目录分层，扫描范围必须包含全部目录，
/// 不能只列一层后漏掉 `Layout/` 与 `Components/Showcase/`。
function filesUnder(root, suffix) {
    const files = [];
    for (const entry of readdirSync(root, { withFileTypes: true })) {
        const path = join(root, entry.name);
        if (entry.isDirectory()) files.push(...filesUnder(path, suffix));
        else if (entry.name.endsWith(suffix)) files.push(path);
    }
    return files;
}

/// 生成物里的字符串是 C# 字面量，反斜杠转义的引号要还原后再比较（`"320px"` 会写成 `\"320px\"`）。
const unescapeCSharp = text => text.replace(/\\"/g, '"').replace(/\\\\/g, '\\');

/// 生成物里某页的“成员名 → 默认值”。
function generatedDefaultValues(generated) {
    const byPage = new Map();

    for (const block of generated.matchAll(/\["([a-z0-9-]+)"\] = new ShowcaseApiMember\[\]((?:.|\n)*?)\r?\n\s*\},/g)) {
        const members = new Map();
        for (const row of block[2].matchAll(/new\("((?:[^"\\]|\\.)*)", "(?:[^"\\]|\\.)*", "((?:[^"\\]|\\.)*)"/g)) {
            members.set(unescapeCSharp(row[1]), unescapeCSharp(row[2]));
        }
        byPage.set(block[1], members);
    }

    return byPage;
}

/// 生成物里 ByPage（不含后面的 Sections / Contracts）的“页 → 成员名数组”。
function generatedByPageMemberNames(generated) {
    const boundary = generated.indexOf('Sections { get; }');
    const byPageSource = boundary < 0 ? generated : generated.slice(0, boundary);
    const byPage = new Map();

    for (const block of byPageSource.matchAll(/\["([a-z0-9-]+)"\] = new ShowcaseApiMember\[\]((?:.|\n)*?)\r?\n\s*\},/g)) {
        const members = [];
        for (const row of block[2].matchAll(/new\("((?:[^"\\]|\\.)*)",/g)) {
            members.push(unescapeCSharp(row[1]));
        }
        byPage.set(block[1], members);
    }

    return byPage;
}

/// 生成物里 `ShowcaseApiReference.Sections` 的结构：页 → [{ title, baseParameters, members }]。
/// `baseParameters === null` 表示该组不是库组件（模型 / 服务 / 共享约定），不列公共参数；
/// 空数组表示 8 个公共参数全部生效，非空数组只登记不生效的参数与原因。
function generatedSections(generated) {
    const sections = new Map();
    let page = null;
    let current = null;
    let mode = null;

    for (const line of generated.split(/\r?\n/)) {
        const header = /^\s*\["([a-z0-9-]+)"\] = new ShowcaseApiSection\[\]$/.exec(line);
        if (header) {
            page = header[1];
            sections.set(page, []);
            current = null;
            mode = null;
            continue;
        }

        if (page === null) continue;
        if (/^\s{12}\},$/.test(line)) {
            page = null;
            current = null;
            mode = null;
            continue;
        }

        const start = /^\s*new\("((?:[^"\\]|\\.)*)",$/.exec(line);
        if (start) {
            current = { title: unescapeCSharp(start[1]), baseParameters: null, members: [] };
            sections.get(page).push(current);
            mode = 'base';
            continue;
        }

        if (!current) continue;
        const trimmed = line.trim();
        if (trimmed === 'null,') {
            current.baseParameters = null;
            mode = 'members';
            continue;
        }

        if (trimmed.startsWith('Array.Empty<ShowcaseApiBaseParameter>()')) {
            current.baseParameters = [];
            mode = 'members';
            continue;
        }

        if (trimmed === 'new ShowcaseApiBaseParameter[]') {
            current.baseParameters = [];
            mode = 'base';
            continue;
        }

        if (trimmed === 'new[]') {
            mode = 'members';
            continue;
        }

        if (trimmed.startsWith('Array.Empty<string>()')) continue;

        const baseParameter = /^\s*new\("((?:[^"\\]|\\.)*)", "((?:[^"\\]|\\.)*)"\),$/.exec(line);
        if (mode === 'base' && baseParameter) {
            current.baseParameters.push({
                name: unescapeCSharp(baseParameter[1]),
                note: unescapeCSharp(baseParameter[2])
            });
            continue;
        }

        const member = /^\s*"((?:[^"\\]|\\.)*)",$/.exec(line);
        if (mode === 'members' && member) current.members.push(unescapeCSharp(member[1]));
    }

    return sections;
}

// 生成器的默认值来自属性体之后的 `= 初始化表达式;`。只抓 `{ get; set; }` 的话，自动属性的
// 初始值整批丢失（Badge.Max、Skeleton.Lines、VirtualList.Height 都曾经是空的），页面上的
// 「超过 99 显示 99+」「单行骨架」「默认 320px 视口」这些说明就会与真实行为悄悄分叉。
// 默认值字符串一律以生成物里的写法为准（`"320px"` 带引号是 simpleDefault 的既有策略）。
test('生成的 API 表保留了参数默认值（自动属性的初始化值不可丢失）', () => {
    const generated = read(`${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`);
    const defaults = generatedDefaultValues(generated);

    const expectations = [
        ['badge', 'Max', '99'],
        ['skeleton', 'Lines', '1'],
        ['virtual-list', 'Height', '"320px"']
    ];

    for (const [page, member, expected] of expectations) {
        const actual = defaults.get(page)?.get(member);
        assert.notEqual(actual, undefined, `${page} 页的生成物里没有 ${member}，生成器解析可能已失效`);
        assert.equal(actual, expected,
            `${page} 页的 ${member} 默认值应为 ${expected}，生成物里是 ${JSON.stringify(actual)}。` +
            '先确认 parseMembers 读到了属性体之后的 `= 初始化表达式;`，再运行 node scripts/generate-api-reference.mjs 重新生成。');
    }
});

// 多组件合页（DatePicker / DateRangePicker、Tabs / Tab、List / ListItem、Dialog / Confirm /
// Alert / Toast……）此前是一张混表：同名成员只能靠 `Type.Member` 前缀区分，读者无法判断某个
// 参数属于哪个组件。生成物现在给出组件级分组；这里断言分组顺序，以及分组正好覆盖全部成员
// （漏行或重复行会让 API 表静默丢内容）。
test('多组件合页的 API 按组件分组，且分组正好覆盖生成物里的每个成员', () => {
    const generated = read(`${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`);
    const sections = generatedSections(generated);
    const byPage = generatedByPageMemberNames(generated);
    assert.ok(byPage.size > 50, `生成物里只解析到 ${byPage.size} 个页面，解析逻辑可能已失效`);

    for (const [page, members] of byPage) {
        const pageSections = sections.get(page);
        assert.ok(pageSections?.length, `${page} 页没有生成 API 分组（需运行 node scripts/generate-api-reference.mjs）`);
        const assigned = pageSections.flatMap(section => section.members);
        assert.deepEqual([...assigned].sort(), [...members].sort(),
            `${page} 页的 API 分组没有正好覆盖 ByPage 的全部成员`);
    }

    // 多组件页的组标题与顺序：有组件目录的按目录顺序，顺序有讲究的登记在
    // scripts/api-groups.json（反馈页没有组件目录，也在那里登记）。
    const componentGroups = {
        'date-picker': ['DatePicker', 'DateRangePicker', '共享约定'],
        'list': ['List', 'ListItem'],
        'popup': ['PopupHost', 'Popover'],
        'radio': ['Radio', 'RadioGroup'],
        'tabs': ['Tabs', 'Tab'],
        'theme': ['ThemeProvider', 'ThemeSwitch', 'ThemeBrandSwitch', 'ThemeService'],
        'toolbar': ['Toolbar', 'ToolbarGroup'],
        'feedback': ['Dialog', 'Confirm', 'Alert', 'Toast']
    };

    for (const [page, titles] of Object.entries(componentGroups)) {
        assert.deepEqual((sections.get(page) ?? []).map(section => section.title), titles,
            `${page} 页的 API 分组标题 / 顺序不对`);
    }

    for (const [page, pageSections] of sections) {
        if (componentGroups[page]) continue;
        assert.equal(pageSections.length, 1, `${page} 页只有一个组件，不应拆成多个分组`);
    }
});

// 基类参数（Id / Disabled / Visible / Class / Style / Element / AdditionalAttributes /
// ElementChanged）不是每个组件都真正消费：ThemeProvider / FormDialog 不渲染 DOM，Badge 不读
// Disabled，Menu 把根元素绑定成 RenderedRootElement。矩阵从源码结构推导，生成物只登记
// 「不生效 + 原因」；下面既校验结构，也锁定几条能从源码直接核对的代表性事实。
test('基类参数支持矩阵：只列真正生效的公共参数，不生效的带原因', () => {
    const generated = read(`${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`);
    const sections = generatedSections(generated);
    const componentTypes = new Set(filesUnder(join(LIBRARY, 'Components'), '.razor.cs')
        .map(file => basename(file, '.razor.cs')));
    const allBaseParameters = [...BASE_PARAMETERS];
    const domlessComponents = new Set(['ThemeProvider', 'FormDialog']);
    const sectionByKey = new Map();

    let componentSections = 0;
    for (const [page, pageSections] of sections) {
        for (const section of pageSections) {
            sectionByKey.set(`${page}:${section.title}`, section);

            if (section.baseParameters === null) {
                assert.ok(!componentTypes.has(section.title),
                    `${page}.${section.title} 是库组件，却没有基类参数支持矩阵`);
                continue;
            }

            componentSections += 1;
            assert.ok(componentTypes.has(section.title),
                `${page}.${section.title} 不是库组件，却生成了基类参数支持矩阵`);

            const names = section.baseParameters.map(parameter => parameter.name);
            assert.equal(new Set(names).size, names.length, `${page}.${section.title} 的基类参数重复`);
            for (const parameter of section.baseParameters) {
                assert.ok(BASE_PARAMETERS.has(parameter.name),
                    `${page}.${section.title} 登记了不存在的基类参数 ${parameter.name}`);
                assert.ok(parameter.note.trim().length > 0, `${page}.${section.title}.${parameter.name} 缺少不生效的原因`);
            }

            if (domlessComponents.has(section.title)) {
                assert.deepEqual([...names].sort(), [...allBaseParameters].sort(),
                    `${section.title} 不渲染 DOM，8 个基类参数都应标为不生效`);
                assert.ok(section.baseParameters.every(parameter => parameter.note.includes('不渲染 DOM')),
                    `${section.title} 的不生效原因应说明不渲染 DOM`);
            }
        }
    }

    assert.ok(componentSections > 50, `只解析到 ${componentSections} 个组件分组，解析逻辑可能已失效`);

    // 源码结构 → 矩阵的抽查。Badge / PopupHost 不读 Disabled；Tabs 的 Disabled 是每个 Tab
    // 自己的参数；Menu 用 RenderedRootElement 绑定根元素，所以 Element / ElementChanged 生效。
    const facts = [
        ['badge', 'Badge', ['Disabled']],
        ['button', 'Button', []],
        ['form-dialog', 'FormDialog', allBaseParameters],
        ['menu', 'Menu', []],
        ['popup', 'PopupHost', ['Disabled']],
        ['tabs', 'Tab', []],
        ['tabs', 'Tabs', ['Disabled']],
        ['theme', 'ThemeProvider', allBaseParameters],
        ['theme', 'ThemeSwitch', []],
        ['theme', 'ThemeBrandSwitch', []]
    ];

    for (const [page, title, expected] of facts) {
        const section = sectionByKey.get(`${page}:${title}`);
        assert.ok(section, `${page} 页缺少 ${title} 分组`);
        assert.deepEqual(section.baseParameters?.map(parameter => parameter.name).sort() ?? null,
            [...expected].sort(), `${page}.${title} 的基类参数支持矩阵与源码结构不符`);
    }

    // 不是库组件的分组（模型、服务、共享约定）不列公共参数：反馈页以前无条件列出 8 个
    // 基类参数，但它记录的是 IDialogService 与各选项类型，参数根本无处生效。
    for (const [page, title] of [['feedback', 'Dialog'], ['date-picker', '共享约定'], ['theme', 'ThemeService']]) {
        assert.equal(sectionByKey.get(`${page}:${title}`)?.baseParameters, null,
            `${page}.${title} 不是库组件，不应列公共参数`);
    }
});

// API 表的编辑内容（中文说明、生成器推不出来的行、自哪个版本）原先拆在三个 JSON 里：
// 同一条成员要改两处，某个文件漂移也只会在生成物 --check 时才暴露。现在统一为
// scripts/api-content.json 的「页 → descriptions / extraRows / since」，读取与校验只有
// scripts/lib/showcase-api-content.mjs 一个入口。
test('API 表的编辑内容只有一个维护入口', () => {
    assert.ok(existsSync('scripts/api-content.json'), '合并后的编辑源 scripts/api-content.json 不存在');

    for (const legacy of ['scripts/api-descriptions.json', 'scripts/api-extra-rows.json', 'scripts/api-since.json']) {
        assert.ok(!existsSync(legacy), `${legacy} 应已并入 scripts/api-content.json，不能再有第二份编辑源`);
    }

    const content = loadApiContent();
    assert.ok(Object.keys(content.descriptions).length >= 50,
        `合并源只解析到 ${Object.keys(content.descriptions).length} 页说明，读取入口可能已失效`);
    assert.ok(Object.keys(content.extraRows).length >= 20,
        `合并源只解析到 ${Object.keys(content.extraRows).length} 页额外行，读取入口可能已失效`);
    assert.ok(Object.keys(content.since.members).length >= 100,
        `合并源只解析到 ${Object.keys(content.since.members).length} 条 since，读取入口可能已失效`);

    // 抽样一条中文说明：确认生成器确实消费了合并源，而不是退回到源码里的英文 XML 注释。
    const generated = read(`${SAMPLE}/Components/Showcase/ShowcaseApiReference.generated.cs`);
    assert.ok(generated.includes('按钮视觉变体：Solid、Outline、Soft、Ghost、Link。'),
        '生成物应包含合并编辑源里的中文说明');
});

// 搜索索引生成物按页面分组：页标题与分类只存一次，段只存 id 与标题。消费端
// ShowcaseSearchModel 摊平后必须保持旧扁平序列的顺序——页面按目录顺序，页内第一段是页面
// 本身，其余按阅读顺序；查询排序与键盘下标都依赖这个稳定顺序。
test('搜索索引生成物按页面分组，扁平化后与目录顺序一致', () => {
    const generated = read(`${SAMPLE}/Components/Showcase/ShowcaseSearchIndex.generated.cs`);
    assert.match(generated, /IReadOnlyList<ShowcaseSearchPage> Pages \{ get; \}/, '搜索索引应按页面分组');
    assert.doesNotMatch(generated, /IReadOnlyList<ShowcaseSearchEntry> Entries/, '旧的扁平 Entries 生成物应已替换');

    const pages = [];
    let current = null;
    for (const line of generated.split(/\r?\n/)) {
        const header = /^\s*new\("((?:[^"\\]|\\.)*)", "((?:[^"\\]|\\.)*)", "((?:[^"\\]|\\.)*)",$/.exec(line);
        if (header) {
            current = {
                pageId: unescapeCSharp(header[1]),
                pageTitle: unescapeCSharp(header[2]),
                category: unescapeCSharp(header[3]),
                sections: []
            };
            pages.push(current);
            continue;
        }

        if (!current) continue;
        for (const section of line.matchAll(/new\("((?:[^"\\]|\\.)*)", "((?:[^"\\]|\\.)*)"\)/g)) {
            current.sections.push({ id: unescapeCSharp(section[1]), title: unescapeCSharp(section[2]) });
        }
    }

    assert.equal(pages.length, entries.length,
        `分组页面数 ${pages.length} 与目录条目数 ${entries.length} 不一致（需运行 node scripts/generate-search-index.mjs）`);
    assert.deepEqual(pages.map(page => page.pageId), entries.map(entry => entry.id),
        '页面顺序必须与目录顺序一致');

    let sectionCount = 0;
    for (const page of pages) {
        assert.ok(page.sections.length >= 1, `${page.pageId} 至少要有「页面本身」这一段`);
        assert.deepEqual(page.sections[0], { id: page.pageId, title: page.pageTitle },
            `${page.pageId} 的第一段应是页面本身`);
        assert.ok(page.category.length > 0, `${page.pageId} 缺少分类`);
        const ids = page.sections.map(section => section.id);
        assert.equal(new Set(ids).size, ids.length, `${page.pageId} 的段锚点重复`);
        sectionCount += page.sections.length;
    }

    assert.ok(sectionCount > entries.length, '除页面本身外还应登记页内段');

    // 抽查阅读顺序：按钮页是 何时使用 → demo 卡 → 参数与事件 → 相关组件 → 说明与边界。
    const button = pages.find(page => page.pageId === 'button');
    assert.ok(button, '搜索索引缺少 button 页');
    assert.equal(button.sections[1].title, '何时使用');
    assert.equal(button.sections.at(-1).title, '说明与边界');

    // 段行只带两个参数；页字段重复出现在段行就说明分组退化回了扁平结构。
    const twoFieldSection = /new\("(?:[^"\\]|\\.)*", "(?:[^"\\]|\\.)*"\)/g;
    for (const line of generated.split(/\r?\n/)) {
        if (!/^\s{16}new\(/.test(line)) continue;
        const leftover = line.replace(twoFieldSection, '');
        assert.match(leftover, /^[\s,]*$/, `段行不应重复页字段：${line.trim()}`);
    }
});

// 与生成器一致的 `[Parameter]` 解析：参数名在类型之后、属性体之前，与初始化值无关。
function componentParameterContract(source) {
    const parameters = new Set(BASE_PARAMETERS);
    for (const match of source.matchAll(/\[Parameter[^\]]*\]\s*public\s+[\w<>?,\[\]\.\s]+?\s+(\w+)\s*(?:\{|=>)/g)) {
        parameters.add(match[1]);
    }

    // `<Autocomplete TItem="string">` 的 TItem 是泛型类型参数而不是 [Parameter]，从类声明解析。
    const genericParameters = new Set();
    for (const match of source.matchAll(/public\s+(?:sealed\s+)?partial\s+class\s+\w+\s*<([^>]+)>/g)) {
        for (const part of match[1].split(',')) genericParameters.add(part.trim().replace(/\s.*$/, ''));
    }

    return { parameters, genericParameters };
}

/// 库组件名 → 允许的属性。只收 `.razor.cs` 里的组件类型；示例工程自己的 Showcase* 部件
/// 与 `AeterniIcons` 之类的静态类型不在这个集合里，因此不会被误判。
function componentContracts() {
    const contracts = new Map();
    for (const file of filesUnder(join(LIBRARY, 'Components'), '.razor.cs')) {
        contracts.set(basename(file, '.razor.cs'), componentParameterContract(read(file)));
    }
    return contracts;
}

// 组件通过 AdditionalAttributes 把未匹配属性透传给根元素，所以 HTML 原生属性是合法的。
// 这里刻意保持小写精确匹配：大小写不敏感会把 `Color` 误放行成 HTML 的 `color`——而
// Button 根本没有 Color 参数（已知错误写法 `<Button Color="…">`）。
const HTML_ATTRIBUTES = new Set(`
accept accesskey action allow allowfullscreen alt async autocomplete autocapitalize
autofocus autoplay charset checked cite class cols colspan content contenteditable
controls crossorigin datetime decoding defer dir dirname disabled download draggable
enctype enterkeyhint fetchpriority for form formaction formenctype formmethod
formnovalidate formtarget headers height hidden high href hreflang http-equiv id
inert inputmode integrity is ismap itemid itemprop itemref itemscope itemtype kind
label lang list loading loop low max maxlength media method min minlength multiple
muted name nonce novalidate open optimum part pattern ping placeholder playsinline
popover poster preload readonly referrerpolicy rel required reversed role rows
rowspan sandbox scope selected slot span spellcheck src srcdoc srclang srcset start
step style summary tabindex target title translate type usemap value width wrap
`.split(/\s+/).filter(Boolean));

// Razor 自身的语法：`Context` 是模板上下文（`<ItemTemplate Context="row">`），
// `@bind-*` / `@on*` / `@ref` / `@key` / `@attributes` 等以 `@` 开头的属性整类放行。
const RAZOR_ATTRIBUTES = new Set(['Context']);

/// 只保留 Razor 标记，抹掉 `@code` / `@functions` 块、Razor 注释与 HTML 注释：代码片段是
/// 原始字符串里的展示文本，不是真实渲染的标签。替换成等长空白并保留换行，行号因此不变。
function markupOnly(source) {
    const codeStart = /(^|\n)@(code|functions)\b/.exec(source);
    const markup = codeStart ? source.slice(0, codeStart.index) : source;
    const blank = text => text.replace(/[^\n]/g, ' ');
    return markup
        .replace(/@\*[\s\S]*?\*@/g, blank)
        .replace(/<!--[\s\S]*?-->/g, blank);
}

/// 扫描开标签（引号与 `@(...)` 感知，`For="() => Project.Name"` 里的 `>` 不是标签结束）。
/// 只返回组件名命中库组件表的标签，HTML 元素与示例工程自有的 Showcase* 部件都跳过。
function componentTags(source, contracts) {
    const tags = [];
    let scannedUntil = 0;

    for (const match of source.matchAll(/<([A-Za-z_][\w.]*)/g)) {
        const start = match.index;
        // 属性值里的 `<...>` 是文本而不是嵌套标记：已经扫过的开标签内部不再重复识别。
        if (start < scannedUntil || !contracts.has(match[1])) continue;

        let cursor = start + match[0].length;
        let quote = null;
        let depth = 0;

        for (; cursor < source.length; cursor += 1) {
            const character = source[cursor];
            if (depth > 0) {
                if (character === '(') depth += 1;
                else if (character === ')') depth -= 1;
            } else if (quote) {
                if (character === '@' && source[cursor + 1] === '(') { depth = 1; cursor += 1; }
                else if (character === quote) quote = null;
            } else if (character === '"' || character === "'") quote = character;
            else if (character === '@' && source[cursor + 1] === '(') { depth = 1; cursor += 1; }
            else if (character === '>') { cursor += 1; break; }
        }

        tags.push({
            name: match[1],
            line: source.slice(0, start).split('\n').length,
            text: source.slice(start, cursor)
        });
        scannedUntil = cursor;
    }
    return tags;
}

/// 开标签里的属性名。值可以是引号字符串（内含 `@(...)` 表达式）或不带引号的单值；
/// 布尔属性没有 `=` 也算一个属性名。
function tagAttributeNames(tag) {
    const names = [];
    let cursor = /^<[A-Za-z_][\w.]*/.exec(tag)?.[0].length ?? 1;

    while (cursor < tag.length) {
        while (cursor < tag.length && /\s/.test(tag[cursor])) cursor += 1;
        if (cursor >= tag.length || tag[cursor] === '>' || tag[cursor] === '/') break;

        const start = cursor;
        while (cursor < tag.length && /[A-Za-z0-9_:.@-]/.test(tag[cursor])) cursor += 1;
        if (cursor === start) { cursor += 1; continue; }
        names.push(tag.slice(start, cursor));

        while (cursor < tag.length && /\s/.test(tag[cursor])) cursor += 1;
        if (tag[cursor] !== '=') continue;

        cursor += 1;
        while (cursor < tag.length && /\s/.test(tag[cursor])) cursor += 1;
        if (tag[cursor] !== '"' && tag[cursor] !== "'") {
            while (cursor < tag.length && !/[\s>/]/.test(tag[cursor])) cursor += 1;
            continue;
        }

        const quote = tag[cursor];
        cursor += 1;
        let depth = 0;
        for (; cursor < tag.length; cursor += 1) {
            const character = tag[cursor];
            if (depth > 0) {
                if (character === '(') depth += 1;
                else if (character === ')') depth -= 1;
            } else if (character === '@' && tag[cursor + 1] === '(') depth = 1;
            else if (character === quote) { cursor += 1; break; }
        }
    }

    return names;
}

function allowedComponentAttribute(contract, attribute) {
    if (attribute.startsWith('@')) return true;
    if (/^aria-/.test(attribute) || /^data-/.test(attribute) || /^on[a-z]+$/.test(attribute)) return true;
    if (HTML_ATTRIBUTES.has(attribute)) return true;
    if (RAZOR_ATTRIBUTES.has(attribute)) return true;
    return contract.genericParameters.has(attribute) || contract.parameters.has(attribute);
}

// 示例页是公共 API 的第一读者。这里从组件源码取每个 `[Parameter]`，再逐个校验示例里的
// 组件标签：属性名既不是该组件的参数、也不是 HTML 原生/事件/ARIA 属性时失败。它能抓到
// “页面写着 `AriaLabel` 但组件没有这个参数”“把 `Intent` 写成 `Color`”这类编译期其实
// 会报错、但人工审查容易漏掉的漂移——报错信息直接给 页面:行 与属性名。
test('示例页的组件标签只使用该组件的 [Parameter] 或 HTML 原生属性', () => {
    const contracts = componentContracts();
    assert.ok(contracts.size > 50, `只解析到 ${contracts.size} 个组件类型，解析逻辑可能已失效`);

    const roots = [
        join(SAMPLE, 'Pages', 'Components'),
        join(SAMPLE, 'Components', 'Showcase'),
        join(SAMPLE, 'Layout')
    ];

    const violations = [];
    let tagCount = 0;

    for (const root of roots) {
        for (const file of filesUnder(root, '.razor')) {
            const source = markupOnly(read(file));
            for (const tag of componentTags(source, contracts)) {
                tagCount += 1;
                const contract = contracts.get(tag.name);
                for (const attribute of tagAttributeNames(tag.text)) {
                    if (!allowedComponentAttribute(contract, attribute)) {
                        violations.push(`${file}:${tag.line} <${tag.name}> ${attribute}`);
                    }
                }
            }
        }
    }

    assert.ok(tagCount > 100, `只扫描到 ${tagCount} 个组件标签，扫描逻辑可能已失效`);
    assert.deepEqual(violations, [],
        `这些组件标签用了组件没有声明的属性（页面:行 标签 属性）：${violations.join('; ')}`);
});
