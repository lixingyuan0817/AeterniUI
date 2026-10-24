#!/usr/bin/env node
// Guards the one duplication the theme layer still carries: the explicit dark
// theme block (`[data-theme="dark"]`) and the `prefers-color-scheme: dark`
// fallback declare the same token set twice. They cannot simply be merged —
// the explicit block has to win once a theme attribute is set, while the media
// block has to paint the right colours before any script runs, so a dark-mode
// visitor never gets a light flash. The duplication is therefore deliberate, and
// the risk is drift: a token added or retuned in one block and forgotten in the
// other silently produces two different dark themes.
//
// This gate parses both blocks, compares every custom-property declaration
// token by token, and fails when a token is missing on either side or its value
// differs. It also fails when one block declares the same token twice with two
// different values (the browser would silently keep the later one).
//
// It does not compare selectors or non-token properties such as `color-scheme`:
// the two blocks match different selectors on purpose, and `color-scheme` is not
// a design token. Everything the page actually paints through is a custom
// property, so that is the contract checked here.
//
// Usage: node scripts/check-theme-parity.mjs [path-to-css]
import { readFileSync } from 'node:fs';

const CSS_FILE = 'src/AeterniUI/wwwroot/css/aeterni_ui.css';
const target = process.argv[2] ?? CSS_FILE;
const DARK_MEDIA = 'prefers-color-scheme: dark';
const DARK_SELECTORS = new Set(['[data-theme="dark"]', '[data-aeterni-mode="dark"]', '.aeterni-dark']);

/* --- a very small CSS scanner -------------------------------------------- */

const css = readFileSync(target, 'utf8');

function lineAt(offset) {
    let line = 1;
    for (let index = 0; index < offset && index < css.length; index++) {
        if (css[index] === '\n') line++;
    }
    return line;
}

// Returns every style rule with its selector list, declaration body, byte range
// and enclosing media query (null when top level). Comments are skipped, so a
// token mentioned in a comment never counts as a declaration.
function parseRules(source) {
    const rules = [];
    const stack = [];
    let buffer = '';
    let index = 0;

    while (index < source.length) {
        const ch = source[index];

        if (ch === '/' && source[index + 1] === '*') {
            const close = source.indexOf('*/', index + 2);
            index = close < 0 ? source.length : close + 2;
            continue;
        }

        if (ch === '{') {
            const prelude = buffer.trim();
            buffer = '';
            const parent = stack[stack.length - 1];
            stack.push({
                prelude,
                body: '',
                bodyStart: index + 1,
                atRule: prelude.startsWith('@'),
                media: parent ? (parent.atRule ? parent.prelude : parent.media) : null,
            });
            index++;
            continue;
        }

        if (ch === '}') {
            const block = stack.pop();
            if (block && !block.atRule) {
                rules.push({
                    selectors: block.prelude.split(',').map(selector => selector.trim()).filter(Boolean),
                    body: block.body,
                    bodyStart: block.bodyStart,
                    media: block.media,
                });
            }
            index++;
            continue;
        }

        const frame = stack[stack.length - 1];
        if (!frame || frame.atRule) buffer += ch;
        else frame.body += ch;
        index++;
    }

    return rules;
}

// Declarations are split on ";" at paren depth 0 so values such as
// `color-mix(in srgb, x 5%, y)` stay in one piece. Only custom properties are
// returned: they are the token contract.
function parseDeclarations(rule) {
    const declarations = [];
    let depth = 0;
    let current = '';
    let start = rule.bodyStart;

    const store = (text, offset) => {
        const colon = text.indexOf(':');
        if (colon < 0) return;
        const name = text.slice(0, colon).trim();
        const value = text.slice(colon + 1).trim().replace(/\s+/g, ' ');
        if (!name.startsWith('--') || !value) return;
        const firstContent = text.search(/\S/);
        declarations.push({
            name,
            value,
            line: lineAt(offset + Math.max(0, firstContent)),
        });
    };

    for (let index = 0; index < rule.body.length; index++) {
        const ch = rule.body[index];
        if (ch === '(') depth++;
        else if (ch === ')') depth--;

        if (ch === ';' && depth === 0) {
            store(current, start);
            current = '';
            start = rule.bodyStart + index + 1;
            continue;
        }
        current += ch;
    }
    store(current, start);

    return declarations;
}

/* --- collect the two dark-theme blocks ----------------------------------- */

const rules = parseRules(css);
const explicit = new Map();
const fallback = new Map();
const duplicates = [];

function collect(map, rule, label) {
    for (const declaration of parseDeclarations(rule)) {
        const previous = map.get(declaration.name);
        if (previous && previous.value !== declaration.value) {
            duplicates.push(
                `${declaration.name} is declared twice with different values in the ${label}\n` +
                `    line ${previous.line}: ${previous.value}\n` +
                `    line ${declaration.line}: ${declaration.value}`);
        }
        map.set(declaration.name, declaration);
    }
}

for (const rule of rules) {
    if (rule.media !== null) {
        if (rule.media.includes(DARK_MEDIA)) collect(fallback, rule, '@media (prefers-color-scheme: dark) fallback');
        continue;
    }
    if (rule.selectors.some(selector => DARK_SELECTORS.has(selector))) {
        collect(explicit, rule, 'explicit dark theme block');
    }
}

/* --- compare ------------------------------------------------------------- */

const problems = [...duplicates];

if (explicit.size === 0) {
    problems.push(`no custom properties found in the explicit dark theme block (${DARK_SELECTORS.size} known selectors)`);
}
if (fallback.size === 0) {
    problems.push(`no custom properties found in the ${DARK_MEDIA} fallback`);
}

for (const [name, declaration] of explicit) {
    const other = fallback.get(name);
    if (!other) {
        problems.push(
            `missing from the @media fallback: ${name}\n` +
            `    explicit dark block line ${declaration.line}: ${declaration.value}`);
        continue;
    }
    if (other.value !== declaration.value) {
        problems.push(
            `value mismatch for ${name}\n` +
            `    explicit dark block line ${declaration.line}: ${declaration.value}\n` +
            `    @media fallback line ${other.line}: ${other.value}`);
    }
}

for (const [name, declaration] of fallback) {
    if (!explicit.has(name)) {
        problems.push(
            `missing from the explicit dark theme block: ${name}\n` +
            `    @media fallback line ${declaration.line}: ${declaration.value}`);
    }
}

if (problems.length > 0) {
    console.error('Dark theme blocks have drifted apart. Both must declare the same tokens:');
    for (const problem of problems) console.error(`  ${problem}`);
    console.error(
        `\nFix: add or retune the token in both blocks of ${target} ` +
        '(the explicit [data-theme="dark"] block and the @media (prefers-color-scheme: dark) fallback).');
    process.exitCode = 1;
} else {
    console.log(
        `Dark theme parity check passed (${explicit.size} tokens identical in the explicit block ` +
        `and the ${DARK_MEDIA} fallback).`);
}
