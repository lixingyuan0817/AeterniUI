#!/usr/bin/env node
// 滚动条统一门禁：凡是带 `overflow: auto|scroll` 的规则块，都必须自己声明滚动条样式。
//
// 为什么需要一条门禁：滚动条在视觉上"只有一个"，实现上却分散在十几份隔离样式里。漏掉一处不会
// 报错，只会让那个组件回落到浏览器默认外观——默认滑轨是实色（浅色主题下接近白），在玻璃面板、
// 卡片或弹层里就是一条常驻的白色竖条，而 `scrollbar-gutter: stable` 恰好让它一直占着位置。
// 同理，只写标准属性会漏掉 Safari 与旧 Chromium（它们读 ::-webkit-scrollbar），而只写
// ::-webkit-scrollbar 又会漏掉 Firefox（它只认标准属性），所以两类声明必须成对出现。
//
// 约定与 `aeterni_ui.css` 的 `--aeterni-scrollbar-*` 注释一致：
//   可见滚动条：块内 `scrollbar-width: thin` + `scrollbar-color: <thumb> <track>`；
//              另需该选择器的 `::-webkit-scrollbar`（按滚动主轴写 width 或 height）、
//              `::-webkit-scrollbar-thumb`、`::-webkit-scrollbar-track` 三条。
//   隐藏滚动条：块内 `scrollbar-width: none`，且 `::-webkit-scrollbar` 里 `display: none`。
//   滑轨必须透明：实色滑轨正是这一轮要修掉的东西。
//
// 用法：node scripts/check-scrollbars.mjs
import { readFileSync, readdirSync } from 'node:fs';
import { join, relative } from 'node:path';

const SKIP_DIRS = new Set(['.git', 'bin', 'obj', 'dist', 'node_modules', 'target', '.sample-publish', '.sample-publish']);

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

/// 规则块（不含嵌套）：选择器取 `{` 之前那一段的最后一行，属性体是块内文本。
function ruleBlocks(css) {
    const blocks = [];
    for (const match of css.matchAll(/([^{}]+)\{([^{}]*)\}/g)) {
        blocks.push({ selector: match[1].trim().split('\n').pop().trim(), body: match[2] });
    }
    return blocks;
}

const problems = [];
let containers = 0;
let sheets = 0;

for (const file of collectCss('.')) {
    const isComponentSheet = file.startsWith('src/AeterniUI/') && file.endsWith('.razor.css');

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

    if (!isComponentSheet) continue;

    const css = readFileSync(file, 'utf8');
    let touched = false;

    for (const block of ruleBlocks(css)) {
        if (!/overflow(-y|-x)?:\s*(auto|scroll)/.test(block.body)) continue;

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
    process.exit(1);
}

console.log(`Scrollbar check passed (${containers} scroll containers across ${sheets} component sheets).`);
