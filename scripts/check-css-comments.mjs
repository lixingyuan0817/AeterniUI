#!/usr/bin/env node
// Guards against a comment that closes early because its body contains a literal
// comment terminator.
//
// Prose such as "page-scoped by prefix (showcase-, preview- and control-)" that
// happens to spell a star right before a slash ends the comment at that point.
// Everything after it is then parsed as a selector, so the parser consumes input
// until the next "{", and the rule that follows the comment is discarded in
// full — silently. That is how `.showcase-layout` lost its `display: grid` and
// every token it declared, which collapsed the whole component page layout with
// no visible error.
//
// Detection: at top level (brace depth 0) a selector / at-rule prelude can never
// contain ";". If the text between a comment terminator and the next "{", "}" or
// ";" does not belong to a prelude, the comment ended early.
//
// Usage: node scripts/check-css-comments.mjs
import { readFileSync, readdirSync } from 'node:fs';
import { join, relative } from 'node:path';

const SKIP_DIRS = new Set(['.git', 'bin', 'obj', 'dist', 'node_modules', 'target', '.sample-publish']);

function collectCss(dir, found = []) {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
        if (entry.isDirectory()) {
            if (!SKIP_DIRS.has(entry.name)) {
                collectCss(join(dir, entry.name), found);
            }
        } else if (entry.name.endsWith('.css')) {
            found.push(join(dir, entry.name));
        }
    }
    return found;
}

const lineOf = (text, offset) => text.slice(0, offset).split('\n').length;

const problems = [];
const files = collectCss('.');

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
                // Advance past whitespace and any further comments to reach the
                // real prelude of the next rule.
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
    process.exit(1);
}

console.log(`CSS comment check passed (${files.length} files).`);
