#!/usr/bin/env node
// 两个 CSS 结构门禁共用同一次遍历：
//   1) 注释提前闭合 —— 注释体里出现字面量 `*/` 会在那里结束注释，后面的规则被当成选择器吞掉；
//   2) 滚动条覆盖 —— 带 `overflow: auto|scroll` 的规则块必须自己声明滚动条样式（成对的标准属性
//      与 `::-webkit-scrollbar`），否则 Safari/Firefox 之一会回落到默认滚动条。
//
// 为什么合并：两条规则扫的是同一批 `.razor.css`，此前是两个脚本各写一份 `collectCss` 与
// `SKIP_DIRS`，重复的遍历与跳过列表已经漂移过一次（重复的 `.sample-publish`）。合并后规则仍是
// 两条独立门禁，可用 `--only` 单独运行；默认全跑。
//
// 用法：
//   node scripts/check-css.mjs
//   node scripts/check-css.mjs --only=comments
//   node scripts/check-css.mjs --only=scrollbars
import { readFileSync, readdirSync } from 'node:fs';
import { join, relative } from 'node:path';

const SKIP_DIRS = new Set(['.git', 'bin', 'obj', 'dist', 'node_modules', 'target', '.sample-publish']);

function collectCss(dir, found = []) {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
        if (entry.isDirectory()) {
            if (!SKIP_DIRS.has(entry.name)) collectCss(join(dir, entry.name), found);
        } else if (entry.name.endsWith('.css')) {
            found.push(relative('.', join(dir, entry.name)).replace(/\\/g, '/'));
        }
    }
    return found;
}

const lineOf = (text, offset) => text.slice(0, offset).split('\n').length;

/** 注释提前闭合：顶层注释结束符到下一个 `{`/`}`/`;` 之间出现 `;`，说明注释被截断。 */
function checkComments(files) {
    const problems = [];

    for (const file of files) {
        const css = readFileSync(file, 'utf8');
        let index = 0;
        let depth = 0;

        while (index < css.length) {
            if (css.startsWith('/*', index)) {
                const open = index;
                const close = css.indexOf('*/', open + 2);
                if (close < 0) {
                    problems.push(`${file}:${lineOf(css, open)} unterminated comment`);
                    break;
                }

                if (depth === 0) {
                    // 跳过空白与连续注释，定位下一条规则真正的前导文本。
                    let cursor = close + 2;
                    for (;;) {
                        while (cursor < css.length && /\s/.test(css[cursor])) cursor++;
                        if (css.startsWith('/*', cursor)) {
                            const nested = css.indexOf('*/', cursor + 2);
                            if (nested < 0) break;
                            cursor = nested + 2;
                            continue;
                        }
                        break;
                    }

                    const stop = css.slice(cursor).search(/[{};]/);
                    const end = stop < 0 ? css.length : cursor + stop;
                    if (css[end] === ';' || css.slice(cursor, end).includes(';')) {
                        const leaked = css.slice(cursor, Math.min(css.length, cursor + 90)).trim().replace(/\s+/g, ' ');
                        problems.push(
                            `${file}:${lineOf(css, open)} comment closes early, so the next rule is discarded\n` +
                            `    leaked text: ${JSON.stringify(leaked)}`);
                    }
                }

                index = close + 2;
                continue;
            }

            if (css[index] === '{') depth++;
            else if (css[index] === '}') depth = Math.max(0, depth - 1);
            index++;
        }
    }

    if (problems.length > 0) {
        console.error('CSS comments must not contain a literal comment terminator:');
        for (const problem of problems) console.error(`  ${problem}`);
        return false;
    }

    console.log(`CSS comment check passed (${files.length} files).`);
    return true;
}

/** 规则块（不含嵌套）：选择器取 `{` 之前那一段的最后一行，属性体是块内文本。 */
function ruleBlocks(css) {
    const blocks = [];
    for (const match of css.matchAll(/([^{}]+)\{([^{}]*)\}/g)) {
        blocks.push({ selector: match[1].trim().split('\n').pop().trim(), body: match[2] });
    }
    return blocks;
}

/** 滚动条覆盖：每个可滚动容器都要写标准属性与 WebKit 伪元素两类声明。 */
function checkScrollbars(files) {
    const problems = [];
    let containers = 0;
    let sheets = 0;

    for (const file of files) {
        if (file === 'src/AeterniUI/wwwroot/css/aeterni_ui.css') {
            const tokens = readFileSync(file, 'utf8');
            if (!/--aeterni-scrollbar-track:\s*transparent/.test(tokens)) {
                problems.push(`${file}: 滑轨必须保持透明（--aeterni-scrollbar-track: transparent），实色滑轨就是这一轮要修掉的白色竖条`);
            }
            continue;
        }

        // 示例站是宿主：颜色写在根元素上（继承），宽度必须对元素声明（scrollbar-width 不继承）。
        if (file === 'src/AeterniUI.Sample/wwwroot/css/showcase.css') {
            const host = readFileSync(file, 'utf8');
            if (!/:root\s*\{[^}]*scrollbar-color/s.test(host)) {
                problems.push(`${file}: 宿主级的滚动条颜色不见了（:root 上的 scrollbar-color）`);
            }
            if (!/scrollbar-width\s*:/.test(host)) {
                problems.push(`${file}: 缺少 scrollbar-width 声明（它不继承，只写 :root 改不到子容器）`);
            }
            continue;
        }

        if (!(file.startsWith('src/AeterniUI/') && file.endsWith('.razor.css'))) continue;

        const css = readFileSync(file, 'utf8');
        let touched = false;

        for (const block of ruleBlocks(css)) {
            // `overflow: hidden auto` 是双值简写（x=hidden, y=auto），第二个值才是纵向滚动。
            if (!/overflow(-y|-x)?:\s*(auto|scroll)|overflow:\s*\S+\s+(auto|scroll)/.test(block.body)) continue;

            containers += 1;
            touched = true;

            const hidden = /scrollbar-width:\s*none/.test(block.body);
            const webkit = new RegExp(`${block.selector.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}::-webkit-scrollbar\\s*\\{([^}]*)\\}`).exec(css);

            if (!/scrollbar-width:/.test(block.body)) {
                problems.push(`${file}: ${block.selector} 有滚动却没有 scrollbar-width`);
            }

            if (hidden) {
                if (!webkit || !/display:\s*none/.test(webkit[1])) {
                    problems.push(`${file}: ${block.selector} 声明了隐藏滚动条（scrollbar-width: none），但缺少 ::-webkit-scrollbar { display: none }，Safari 上仍会画出滚动条`);
                }
            } else {
                if (!/scrollbar-color:/.test(block.body)) {
                    problems.push(`${file}: ${block.selector} 有滚动却没有 scrollbar-color`);
                }
                if (!webkit) {
                    problems.push(`${file}: ${block.selector} 缺少 ::-webkit-scrollbar 规则，Safari 与旧 Chromium 会回落到默认滚动条`);
                }
            }
        }

        if (touched) sheets += 1;
    }

    if (problems.length > 0) {
        console.error('Scrollbars must be declared per scroll container (see --aeterni-scrollbar-*):');
        for (const problem of problems) console.error(`  ${problem}`);
        return false;
    }

    console.log(`Scrollbar check passed (${containers} scroll containers across ${sheets} component sheets).`);
    return true;
}

const onlyArgument = process.argv.find(argument => argument.startsWith('--only='));
const only = onlyArgument?.slice('--only='.length);
if (only !== undefined && only !== 'comments' && only !== 'scrollbars') {
    console.error(`Unknown --only value: ${only}. Use comments or scrollbars.`);
    process.exit(1);
}

const files = collectCss('.');
let ok = true;
if (only !== 'scrollbars') ok = checkComments(files) && ok;
if (only !== 'comments') ok = checkScrollbars(files) && ok;

process.exit(ok ? 0 : 1);
