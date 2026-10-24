// 示例组件目录的解析：`ShowcaseCatalog.cs` 是侧栏、路由、API 表与搜索索引的共同事实源。
//
// 三个消费方（`generate-api-reference.mjs`、`generate-search-index.mjs`、
// `tests/sample-docs.test.mjs`）此前各写一份正则与 Pascal 推导，改目录格式时要同步三处；
// 这里收成一份，解析失败统一抛错，避免某个脚本悄悄解析出空目录却继续生成。
import { readFileSync } from 'node:fs';

export const CATALOG = 'src/AeterniUI.Sample/Components/Showcase/ShowcaseCatalog.cs';

export const read = path => readFileSync(path, 'utf8').replace(/^\uFEFF/, '');

export const pascal = id => id.split('-').map(part => part[0].toUpperCase() + part.slice(1)).join('');

/// 目录条目 id，只用于 API 表页面的枚举。
export function catalogIds() {
    const source = read(CATALOG);
    return [...source.matchAll(/new\("([a-z0-9-]+)",/g)].map(match => match[1]);
}

/// 目录条目：顺序、页面文件与分组名（= 页面上的 Category）。
export function catalogEntries() {
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
