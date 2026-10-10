#!/usr/bin/env node
// 文档-实现漂移门禁：拦截「实现改了、文档没跟」这一类漂移。
//
// 第十轮全库审核（REV-141/142/144 等）确认：check-docs.sh 只校验必需文件、
// 文档版本头、锁定章节与契约命令的出现位置，**不校验任何成员名或参数清单**，
// 因此「源码已删除的成员仍在 delivered-features 里被写成可用」「文案表新增键未
// 登记」这两类漂移只能在人工审阅时发现。本脚本把这两类变成构建期失败。
//
// 规则：
//   1. 退役成员：`RETIRED` 中的名字不得在 delivered-features 里被表述为仍可用
//      （所在行必须带「移除」类标记；`status: 'pending'` 的条目只告警不失败，
//      在源码修复完成后再转 enforced）。上下文敏感的条目用 `sections` 限定章节，
//      避免误伤同名但合法的成员（例如 Input/Textarea 的 `OnChange`）。
//   2. 文案表键：`AeterniUITextOptions` 的每个 `public string` 键都必须出现在
//      delivered-features（§37 的覆写示例或组件章节）。新增文案键必须同批登记。
//
// 盲区（有意声明）：只读这两个文件——示例页参数表与 README 仍可能单独漂移。
//
// Usage: node scripts/check-doc-drift.mjs
import { readFileSync } from 'node:fs';

const FEATURES = 'docs/delivered-features.zh-CN.md';
const TEXT_OPTIONS = 'src/AeterniUI/Services/AeterniUITextOptions.cs';

// 「已移除」类标记：命中这些词的行允许提及退役成员（用于迁移说明）。
const REMOVAL_MARKERS = /已于[^，。；]*移除|已移除|已经移除/;

const RETIRED = [
    {
        token: 'OnChange',
        // Input/Textarea/Checkbox/Switch 有合法的 OnChange，只扫真正冲突的两章。
        sections: [/^## 17\. Rating$/, /^## 18\. ComboBox$/],
        replacedBy: 'ValueChanged / OnItemSelected',
        status: 'enforced',
    },
];

const features = readFileSync(FEATURES, 'utf8');
const lines = features.split('\n');
const problems = [];
const warnings = [];

const wordBoundary = name => new RegExp(`(?<![A-Za-z0-9_])${name}(?![A-Za-z0-9_])`);

// ---------- 规则 1：退役成员 ----------
for (const entry of RETIRED) {
    const tokenRe = wordBoundary(entry.token);
    const hits = [];

    if (entry.sections) {
        for (const section of entry.sections) {
            const start = lines.findIndex(l => section.test(l.trim()));
            if (start < 0) {
                problems.push(`找不到章节 ${section} —— 扫描范围已失效，请更新 RETIRED 中的 sections`);
                continue;
            }
            let end = lines.length;
            for (let i = start + 1; i < lines.length; i++) {
                if (/^## /.test(lines[i])) { end = i; break; }
            }
            for (let i = start; i < end; i++) {
                if (tokenRe.test(lines[i]) && !REMOVAL_MARKERS.test(lines[i])) hits.push(i + 1);
            }
        }
    } else {
        lines.forEach((line, i) => {
            if (tokenRe.test(line) && !REMOVAL_MARKERS.test(line)) hits.push(i + 1);
        });
    }

    for (const line of hits) {
        const message = `${FEATURES}:${line} 把已退役成员 \`${entry.token}\` 写成仍可用（替代：${entry.replacedBy}）`;
        if (entry.status === 'enforced') problems.push(message);
        else warnings.push(message);
    }
}

// ---------- 规则 2：文案表键覆盖 ----------
const source = readFileSync(TEXT_OPTIONS, 'utf8');
const keys = [...source.matchAll(/public\s+string\s+(\w+)\s*\{\s*get;\s*set;\s*\}/g)].map(m => m[1]);
if (keys.length < 60) {
    problems.push(`仅从 ${TEXT_OPTIONS} 提取到 ${keys.length} 个文案键（预期 ≥60）——提取逻辑可能已失效，门禁拒绝静默通过`);
}
for (const key of keys) {
    if (!wordBoundary(key).test(features)) {
        problems.push(`${FEATURES} 未登记文案表键 \`${key}\`（新增键必须同批登记，覆写示例见 §37）`);
    }
}

// ---------- 输出 ----------
if (warnings.length > 0) {
    console.warn('待修复（只告警，不失败）：');
    for (const warning of warnings) console.warn(`  ${warning}`);
}

if (problems.length > 0) {
    console.error('文档与实现的漂移：');
    for (const problem of problems) console.error(`  ${problem}`);
    process.exit(1);
}

const pending = warnings.length > 0 ? `, ${warnings.length} pending` : '';
console.log(`Doc drift check passed (${keys.length} text keys, ${RETIRED.length} retired entries${pending}).`);
