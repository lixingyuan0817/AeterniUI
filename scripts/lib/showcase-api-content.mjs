// API 表编辑内容的唯一读取入口。
//
// 说明文字、生成器推不出来的行（模型 / 服务 / 汇总写法）与「自哪个版本」原先分在
// api-descriptions / api-extra-rows / api-since 三个文件里：同一条成员要改两处、三处，
// 而且三个文件各自漂移也只会在生成物 --check 时才暴露。现在合并成 `api-content.json`
// 的「页 → { descriptions, extraRows, since }」，读取与校验都收在这里：
//   - 页 id 必须存在于目录（拼错页名不会静默丢内容）；
//   - 只允许三个字段（拼错字段名不会静默忽略）；
//   - since 键必须是「声明类型.成员」且版本形如 x.y.z，跨页重复直接报错；
//   - extraRows 每行必须有 name，kind 只能是 属性 / 事件回调 / 接口。
import { catalogEntries, read } from './showcase-catalog.mjs';

export const API_CONTENT_FILE = 'scripts/api-content.json';

const PAGE_FIELDS = new Set(['descriptions', 'extraRows', 'since']);
const ROW_FIELDS = new Set(['name', 'type', 'description', 'default', 'kind', 'since']);
const KINDS = new Set(['属性', '事件回调', '接口']);

/// 读取并校验合并后的编辑源，返回生成器使用的旧形状：
/// `descriptions[page][member]`、`extraRows[page][]`、`since.members[Type.Member]`。
export function loadApiContent(file = API_CONTENT_FILE) {
    const content = JSON.parse(read(file));
    const knownPages = new Set(catalogEntries().map(entry => entry.id));
    const descriptions = {};
    const extraRows = {};
    const sinceMembers = {};

    for (const [pageId, page] of Object.entries(content.pages ?? {})) {
        if (!knownPages.has(pageId)) {
            throw new Error(`${file}：页 ${pageId} 不在 ShowcaseCatalog 里`);
        }

        for (const field of Object.keys(page)) {
            if (!PAGE_FIELDS.has(field)) {
                throw new Error(`${file}：${pageId} 有未知字段 ${field}（只允许 ${[...PAGE_FIELDS].join(' / ')}）`);
            }
        }

        if (page.descriptions) {
            for (const [name, description] of Object.entries(page.descriptions)) {
                if (typeof description !== 'string' || description.trim() === '') {
                    throw new Error(`${file}：${pageId}.descriptions.${name} 必须是非空说明文字`);
                }
            }

            descriptions[pageId] = page.descriptions;
        }

        if (page.extraRows) {
            if (!Array.isArray(page.extraRows)) {
                throw new Error(`${file}：${pageId}.extraRows 必须是数组`);
            }

            for (const row of page.extraRows) {
                if (typeof row?.name !== 'string' || row.name.trim() === '') {
                    throw new Error(`${file}：${pageId}.extraRows 有一行缺少 name`);
                }

                for (const field of Object.keys(row)) {
                    if (!ROW_FIELDS.has(field)) {
                        throw new Error(`${file}：${pageId} 的额外行 ${row.name} 有未知字段 ${field}`);
                    }
                }

                if (row.kind !== undefined && !KINDS.has(row.kind)) {
                    throw new Error(`${file}：${pageId} 的额外行 ${row.name} 的 kind 非法（${row.kind}）`);
                }

                if (row.since !== undefined && !/^\d+\.\d+\.\d+$/.test(row.since)) {
                    throw new Error(`${file}：${pageId} 的额外行 ${row.name} 的 since 非法（${row.since}）`);
                }
            }

            extraRows[pageId] = page.extraRows;
        }

        for (const [key, version] of Object.entries(page.since ?? {})) {
            if (!/^[A-Za-z_]\w*(?:<[^>]+>)?\.\w+$/.test(key)) {
                throw new Error(`${file}：${pageId}.since 的键 ${key} 必须是「声明类型.成员」`);
            }

            if (!/^\d+\.\d+\.\d+$/.test(version)) {
                throw new Error(`${file}：${pageId}.since.${key} 的版本号非法（${version}）`);
            }

            if (sinceMembers[key] !== undefined) {
                throw new Error(`${file}：${key} 在多个页面的 since 里重复`);
            }

            sinceMembers[key] = version;
        }
    }

    return { descriptions, extraRows, since: { members: sinceMembers } };
}
