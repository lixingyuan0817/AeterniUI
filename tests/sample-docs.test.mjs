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

    // 安装、设计基础与实验室页讲的是用法与 Token，没有单一组件；反馈页记录的是服务与选项
    // 类型，目前仍手写。两处例外都写在这里，而不是靠“没有断言”默认放过。
    const noComponent = new Set(['install', 'colors', 'glass', 'feedback']);

    const missing = entries
        .filter(entry => !noComponent.has(entry.id) && componentDirs.has(entry.page ?? pascal(entry.id)))
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

test('提供代码块的页面都定义了片段，且片段用到了它记录的组件', () => {
    const files = readdirSync(PAGES).filter(name => name.endsWith('.razor'));
    let withCode = 0;

    for (const file of files) {
        const source = read(join(PAGES, file));
        const usesCode = /Code="@UsageCode"/.test(source);
        const declares = /private const string UsageCode = """/.test(source);
        assert.equal(usesCode, declares, `${file} 的 Code 参数与 UsageCode 常量不匹配`);

        if (!usesCode) continue;
        withCode += 1;

        const name = /ComponentName="([^"<]+)"/.exec(source)?.[1] ?? '';
        const snippet = /private const string UsageCode = """\r?\n([\s\S]*?)""";/.exec(source)?.[1] ?? '';
        // 泛型名在属性里是 HTML 转义的（`MultiSelect&lt;TItem&gt;`），只留类型名本身。
        const type = name.split(/[<(&（]/)[0].trim();

        assert.ok(snippet.trim().length > 0, `${file} 的 UsageCode 是空的`);
        assert.ok(snippet.includes(type), `${file} 的用法片段没有出现 ${type}`);
    }

    assert.ok(withCode > 0, '示例工程里没有任何代码块，说明本门禁已失效');
});
