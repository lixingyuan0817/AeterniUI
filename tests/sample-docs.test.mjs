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
//
// 有意不做的事：不校验参数说明的文字，也不校验示例的视觉结果——那属于浏览器检查。
import assert from 'node:assert/strict';
import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { join } from 'node:path';
import { test } from 'node:test';

const SAMPLE = 'src/AeterniUI.Sample';
const LIBRARY = 'src/AeterniUI';
const PAGES = `${SAMPLE}/Pages/Components`;
const CATALOG = `${SAMPLE}/Components/Showcase/ShowcaseCatalog.cs`;

// AeterniComponent 的公共参数由 ComponentPreview 的 CommonParameters 统一登记，
// 页面不必逐条重复。
const BASE_PARAMETERS = new Set([
    'Id', 'Disabled', 'Visible', 'Class', 'Style', 'Element', 'AdditionalAttributes', 'ElementChanged'
]);

// 内部渲染部件：delivered-features §24/§37 明确它们保留 Razor 类型只是为了内部组合，
// 已标记为非稳定公共 API，因此不在页面必须登记的公共面里。
const INTERNAL_PARTS = new Set(['DateCalendar', 'TimeOptionList']);

const read = path => readFileSync(path, 'utf8').replace(/^\uFEFF/, '');

function catalogEntries() {
    const source = read(CATALOG);
    const entries = [];
    const pattern = /new\("([a-z0-9-]+)",\s*"([^"]+)",\s*"([^"]+)"\)(\s*\{[^}]*\})?/g;
    for (const match of source.matchAll(pattern)) {
        const overrides = match[4] ?? '';
        const page = /Page\s*=\s*"([^"]+)"/.exec(overrides)?.[1];
        entries.push({ id: match[1], label: match[2], page });
    }
    return entries;
}

const pascal = id => id.split('-').map(part => part[0].toUpperCase() + part.slice(1)).join('');

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
    // （`scripts/api-extra-rows.json` → 生成器在成员为空时也为它产出条目）。这里不再把它
    // 当例外：例外曾经让它带着三张没人消费的数组通过门禁，页面上其实没有 API 区。
    const noComponent = new Set(['install', 'colors', 'glass']);

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
        if (/Showcase(Parameter|Event|Contract)\[\]/.test(read(file))) handWritten.push(entry.id);
    }
    assert.deepEqual(handWritten, [], `这些页面仍在手写 API 表：${handWritten.join(', ')}`);
});

test('卡片化页面：每段代码都声明了，且片段用到了本页记录的组件', () => {
    const files = readdirSync(PAGES).filter(name => name.endsWith('.razor'));

    // 一页可能记录多个组件（Radio 与 RadioGroup、List 与 ListItem……），因此"片段里出现的
    // 类型"取本页记录过的类型集合：主类型来自 ComponentName，其余来自携带类型前缀的记录行。
    const extras = JSON.parse(read('scripts/api-extra-rows.json')).rows;
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

        for (const match of source.matchAll(/Code="@(\w+)"/g)) {
            const constant = match[1];
            // 用 indexOf 取原始字符串字面量，不走正则：跨语言的转义（\r?\n、[\s\S]）
            // 在生成测试文件时极易被吃掉，这一处已经被自己踩过一次。
            const marker = `private const string ${constant} = """`;
            const start = source.indexOf(marker);
            const end = start < 0 ? -1 : source.indexOf('"""', start + marker.length);
            const snippet = start < 0 || end < 0 ? undefined : source.slice(start + marker.length, end);
            assert.ok(snippet !== undefined, `${file} 引用了未声明的代码常量 ${constant}`);
            assert.ok([...documented].some(type => snippet.includes(type)),
                `${file} 的 ${constant} 片段没有出现本页记录的组件（${[...documented].join(' / ')}）`);
            cards += 1;
        }
    }

    assert.ok(cards > 0, '示例工程里没有任何代码片段，说明本门禁已失效');
});

test('卡片化的页面：demo 锚点唯一，每段都有自己的标题与代码', () => {
    const files = readdirSync(PAGES).filter(name => name.endsWith('.razor'));
    const migrated = [];

    for (const file of files) {
        const source = read(join(PAGES, file));
        if (!source.includes('<ComponentPage')) continue;
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
