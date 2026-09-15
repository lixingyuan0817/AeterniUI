#!/usr/bin/env node
// Guards the WCAG contrast floors of the brand palettes (design guidelines §5.4).
//
// The palette is a hue layer: every semantic brand alias (fill, hover, active,
// text, link) is a `var(--aeterni-brand-*)` in one of the light / dark / system
// dark theme blocks. That indirection means a single edited stop silently
// re-tints buttons, links and selected states everywhere, and nothing in the
// build notices when the result drops below 4.5:1 under white text — the failure
// only shows up as unreadable labels in a browser.
//
// This gate resolves the tokens for real instead of trusting the comments:
// it scans the token file, replays the cascade for a given <html> attribute
// state, follows `var()` chains to concrete colors, and measures each
// foreground/background pair the palette is actually used in. It also enforces
// the §5.4 accounting rule: a hue layer whose solid fill leaves less than a
// 1.1x margin must state that margin in its own comment block, so tightening a
// palette is always a deliberate, reviewed act rather than a side effect.
//
// Adding a hue layer: declare `[data-aeterni-brand="…"]` with the same ten
// stops and the gate picks it up automatically - no edit needed here.
//
// Usage: node scripts/check-contrast.mjs [path-to-css]
import { readFileSync } from 'node:fs';

const CSS_FILE = 'src/AeterniUI/wwwroot/css/aeterni_ui.css';
const target = process.argv[2] ?? CSS_FILE;
const STOPS = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900];
const TEXT_FLOOR = 4.5;      // body text and any label inside a filled control
const GRAPHIC_FLOOR = 3;     // icons, borders, focus rings (non-text contrast)
const ACCOUNTING_MARGIN = 1.1;
const DARK_MEDIA = 'prefers-color-scheme: dark';

/* --- a very small CSS scanner -------------------------------------------- */

function parseRules(css) {
    const rules = [];
    const stack = [];
    let buffer = '';
    let comment = '';
    let index = 0;

    while (index < css.length) {
        const ch = css[index];

        if (ch === '/' && css[index + 1] === '*') {
            const close = css.indexOf('*/', index + 2);
            comment = css.slice(index + 2, close < 0 ? css.length : close);
            index = close < 0 ? css.length : close + 2;
            continue;
        }

        if (ch === '{') {
            const parent = stack[stack.length - 1];
            const prelude = buffer.trim();
            buffer = '';
            stack.push({
                prelude,
                body: '',
                atRule: prelude.startsWith('@'),
                media: parent ? (parent.atRule ? parent.prelude : parent.media) : null,
                comment,
            });
            comment = '';
            index++;
            continue;
        }

        if (ch === '}') {
            const block = stack.pop();
            if (!block.atRule) {
                rules.push({
                    selectors: block.prelude.split(',').map(s => s.trim()).filter(Boolean),
                    declarations: parseDeclarations(block.body),
                    media: block.media,
                    comment: block.comment,
                });
            }
            index++;
            continue;
        }

        const frame = stack[stack.length - 1];
        if (!frame || frame.atRule) {
            buffer += ch;
        } else {
            frame.body += ch;
        }
        index++;
    }

    return rules;
}

// Declarations are split on ";" at paren depth 0 so values such as
// `color-mix(in srgb, x 5%, y)` stay in one piece.
function parseDeclarations(body) {
    const declarations = {};
    let depth = 0;
    let current = '';

    for (const ch of body) {
        if (ch === '(') depth++;
        else if (ch === ')') depth--;
        if (ch === ';' && depth === 0) {
            store(current);
            current = '';
            continue;
        }
        current += ch;
    }
    store(current);

    function store(text) {
        const colon = text.indexOf(':');
        if (colon < 0) return;
        const name = text.slice(0, colon).trim();
        const value = text.slice(colon + 1).trim();
        if (name.startsWith('--') && value) declarations[name] = value;
    }

    return declarations;
}

/* --- cascade + var() resolution ------------------------------------------ */

// A context is one rendered `<html>` attribute state. `selectors` lists the
// selectors that match in that state; file order then decides which declaration
// wins, exactly as the browser does for same-specificity rules.
function resolveContext(rules, { brand, mode }) {
    const active = new Set([':root', `[data-aeterni-brand="${brand}"]`]);
    if (mode === 'light') active.add('[data-theme="light"]');
    else if (mode === 'dark') {
        active.add('[data-theme="dark"]');
        active.add('.aeterni-dark');
    }

    const declared = new Map();
    for (const rule of rules) {
        const inDarkMedia = rule.media !== null && rule.media.includes(DARK_MEDIA);
        if (rule.media !== null && !(mode === 'system-dark' && inDarkMedia)) continue;
        // The system-dark block is one media query that guards the root element
        // with `:root:not([data-theme]):not(...)`, so it never literally matches
        // the selector set; inside that query any `:root…` selector is the
        // palette we are checking.
        const matches = rule.selectors.some(selector =>
            active.has(selector) || (mode === 'system-dark' && inDarkMedia && selector.startsWith(':root')));
        if (!matches) continue;
        for (const [name, value] of Object.entries(rule.declarations)) declared.set(name, value);
    }

    const resolve = (value, seen = new Set()) => {
        const text = value.trim();
        const hex = text.match(/^#([0-9a-f]{3}|[0-9a-f]{6})$/i);
        if (hex) return parseHex(text);
        const fn = text.match(/^rgba?\(([^)]+)\)$/i);
        if (fn) {
            const [r, g, b, a = '1'] = fn[1].split(/[,\s/]+/).filter(Boolean).map(Number);
            return { r, g, b, a };
        }
        const ref = text.match(/^var\(\s*(--[\w-]+)\s*(?:,([\s\S]*))?\)$/);
        if (ref) {
            const name = ref[1];
            if (seen.has(name)) throw new Error(`cyclic var() reference at ${name}`);
            const declaredValue = declared.get(name);
            if (declaredValue === undefined) {
                if (ref[2] === undefined) throw new Error(`${name} is not declared in this context`);
                return resolve(ref[2], seen);
            }
            seen.add(name);
            return resolve(declaredValue, seen);
        }
        throw new Error(`unsupported color value ${JSON.stringify(text)}`);
    };

    const color = name => {
        const raw = declared.get(name);
        if (raw === undefined) throw new Error(`${name} is not declared in this context`);
        return resolve(raw);
    };

    return { color };
}

/* --- color math ---------------------------------------------------------- */

function parseHex(hex) {
    let body = hex.slice(1);
    if (body.length === 3) body = [...body].map(c => c + c).join('');
    return {
        r: parseInt(body.slice(0, 2), 16),
        g: parseInt(body.slice(2, 4), 16),
        b: parseInt(body.slice(4, 6), 16),
        a: 1,
    };
}

const channel = v => {
    const s = v / 255;
    return s <= 0.04045 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
};

const luminance = ({ r, g, b }) => 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);

// Translucent foregrounds are composited onto their background first, so a
// 78% ink token is measured as the pixel the user actually sees.
const composite = (fg, bg) => fg.a >= 1 ? fg : {
    r: fg.r * fg.a + bg.r * (1 - fg.a),
    g: fg.g * fg.a + bg.g * (1 - fg.a),
    b: fg.b * fg.a + bg.b * (1 - fg.a),
    a: 1,
};

function contrast(fg, bg) {
    const effective = composite(fg, bg);
    const [hi, lo] = [luminance(effective), luminance(bg)].sort((a, b) => b - a);
    return (hi + 0.05) / (lo + 0.05);
}

/* --- the pairs the palette is used in ------------------------------------ */

const PAIRS = [
    ['--aeterni-text-inverse', '--aeterni-color-brand-default', TEXT_FLOOR, 'label on a solid brand fill'],
    ['--aeterni-text-inverse', '--aeterni-color-brand-hover', TEXT_FLOOR, 'label on a hovered brand fill'],
    ['--aeterni-text-inverse', '--aeterni-color-brand-active', TEXT_FLOOR, 'label on a pressed brand fill'],
    ['--aeterni-color-brand-text', '--aeterni-bg-surface', TEXT_FLOOR, 'brand text on a surface'],
    ['--aeterni-color-brand-text', '--aeterni-bg-tertiary', TEXT_FLOOR, 'brand text on a tinted surface'],
    ['--aeterni-color-brand-text', '--aeterni-bg', TEXT_FLOOR, 'brand text on the page'],
    ['--aeterni-text-link', '--aeterni-bg-surface', TEXT_FLOOR, 'link on a surface'],
    ['--aeterni-text-link', '--aeterni-bg', TEXT_FLOOR, 'link on the page'],
    ['--aeterni-color-brand-default', '--aeterni-bg-surface', GRAPHIC_FLOOR, 'brand border or icon on a surface'],
    ['--aeterni-color-brand-default', '--aeterni-bg', GRAPHIC_FLOOR, 'brand border or icon on the page'],
    ['--aeterni-state-color-focus', '--aeterni-bg-surface', GRAPHIC_FLOOR, 'focus ring on a surface'],
];

/* --- run ----------------------------------------------------------------- */

const css = readFileSync(target, 'utf8');
const rules = parseRules(css);

const hueLayers = new Map();
for (const rule of rules) {
    const selector = rule.selectors.find(s => /^\[data-aeterni-brand=".+?"\]$/.test(s));
    if (!selector || !rule.declarations['--aeterni-brand-500']) continue;
    hueLayers.set(selector.match(/"(.*)"/)[1], { selector, rule });
}

const problems = [];
if (!hueLayers.size) problems.push(`no [data-aeterni-brand="…"] hue layer found in ${target}`);

for (const [brand, layer] of hueLayers) {
    const declaredStops = Object.keys(layer.rule.declarations)
        .filter(name => name.startsWith('--aeterni-brand-'))
        .map(name => Number(name.replace('--aeterni-brand-', '')));
    const missing = STOPS.filter(stop => !declaredStops.includes(stop));
    const extra = declaredStops.filter(stop => !STOPS.includes(stop));
    if (missing.length) problems.push(`${brand}: missing stops ${missing.join(', ')}`);
    if (extra.length) problems.push(`${brand}: unexpected stops ${extra.join(', ')}`);
}

const rows = [];
for (const [brand] of hueLayers) {
    for (const mode of ['light', 'dark', 'system-dark']) {
        let context;
        try {
            context = resolveContext(rules, { brand, mode });
        } catch (error) {
            problems.push(`::error::${brand}/${mode}: ${error.message}`);
            continue;
        }

        for (const [fgToken, bgToken, floor, usage] of PAIRS) {
            try {
                const bg = context.color(bgToken);
                const ratio = contrast(context.color(fgToken), bg);
                const margin = ratio / floor;
                const pass = ratio >= floor;
                rows.push({ brand, mode, fgToken, bgToken, usage, ratio, floor, margin, pass });
                if (!pass) {
                    problems.push(
                        `::error::${brand}/${mode}: ${fgToken} on ${bgToken} is ${ratio.toFixed(2)}:1, ` +
                        `below the ${floor}:1 floor (${usage})`);
                }
            } catch (error) {
                problems.push(`::error::${brand}/${mode}: ${fgToken} on ${bgToken} - ${error.message}`);
            }
        }
    }
}

// §5.4 accounting: the 500 stop carries the solid fill, so its margin against
// the inverse text is the palette's real headroom. When that margin is thin the
// hue layer's own comment must name it, otherwise a later edit can spend it
// without anyone noticing that the palette had none left.
for (const [brand, layer] of hueLayers) {
    const fill = rows.find(r => r.brand === brand && r.mode === 'light'
        && r.fgToken === '--aeterni-text-inverse' && r.bgToken === '--aeterni-color-brand-default');
    if (!fill) continue;

    if (fill.margin < ACCOUNTING_MARGIN) {
        const quoted = `${fill.ratio.toFixed(2)}:1`;
        if (!layer.rule.comment.includes(quoted)) {
            problems.push(
                `::error::${layer.selector}: the solid fill leaves only a ${fill.margin.toFixed(3)}x margin ` +
                `(${quoted} against the inverse text), below the ${ACCOUNTING_MARGIN}x accounting line, but the ` +
                `comment above the hue layer does not state "${quoted}". Either loosen the anchor or record the ` +
                `trade-off in that comment.`);
        }
    }
}

const tag = ({ brand, mode }) => `${brand}/${mode}`.padEnd(18);
for (const brand of hueLayers.keys()) {
    for (const mode of ['light', 'dark', 'system-dark']) {
        const group = rows.filter(r => r.brand === brand && r.mode === mode);
        if (!group.length) continue;
        console.log(`\n${brand} / ${mode}`);
        for (const row of group) {
            console.log(
                `  ${row.pass ? 'ok  ' : 'FAIL'} ${row.ratio.toFixed(2).padStart(6)}:1  ` +
                `floor ${row.floor}  margin ${row.margin.toFixed(3)}  ` +
                `${row.fgToken} on ${row.bgToken}`);
        }
    }
}

const tightest = rows.filter(r => r.pass).sort((a, b) => a.margin - b.margin)[0];
if (tightest) {
    console.log(`\ntightest gate: ${tightest.ratio.toFixed(2)}:1 (margin ${tightest.margin.toFixed(3)}) - ` +
        `${tag(tightest)} ${tightest.fgToken} on ${tightest.bgToken}`);
}

if (problems.length) {
    console.error(`\nContrast check failed (${problems.length} problem${problems.length === 1 ? '' : 's'}):`);
    for (const problem of problems) console.error(`  ${problem}`);
    process.exit(1);
}

console.log(`\nContrast check passed: ${rows.length} gates across ` +
    `${hueLayers.size} hue layer${hueLayers.size === 1 ? '' : 's'} x 3 mode states.`);
