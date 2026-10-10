#!/usr/bin/env node
/**
 * Generates `src/AeterniUI.Icons.FontAwesome/FontAwesomeIcons.cs` from the
 * official Font Awesome Free npm packages.
 *
 * The script keeps the adapter free of hand-copied SVG path data: it resolves
 * every icon in the manifest against the upstream package, fails loudly when a
 * name no longer exists, and rewrites the C# file from the authoritative
 * `svgPathData` values.
 *
 * Usage:
 *   node scripts/generate-fontawesome-icons.mjs                 # install/cached, then write
 *   node scripts/generate-fontawesome-icons.mjs --fa-dir <dir>  # reuse an existing @fortawesome install
 *   node scripts/generate-fontawesome-icons.mjs --check         # fail when the committed file is stale
 *
 * Requires network access on the first run (or a pre-populated --fa-dir).
 */

import { execFileSync } from 'node:child_process';
import { createRequire } from 'node:module';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

/** Pinned Font Awesome Free release used by the adapter. */
const FA_VERSION = '7.3.1';

/** Adapter style folders mapped to their npm package and Font Awesome prefix. */
const STYLES = {
    Solid: { package: 'free-solid-svg-icons', prefix: 'fas' },
};

/**
 * Curated icon set. Categories drive the generated `Categories` metadata that
 * the sample icon browser and documentation pickers consume; every name must
 * exist in Font Awesome Free {@link FA_VERSION}.
 */
const MANIFEST = {
    '基础操作': [
        'align-center', 'align-left', 'align-right', 'asterisk', 'ban', 'bolt', 'check', 'check-double',
        'circle-check', 'circle-exclamation', 'circle-info', 'circle-minus', 'circle-plus', 'circle-question',
        'circle-xmark', 'compass-drafting', 'copy', 'droplet', 'ellipsis', 'ellipsis-vertical', 'eraser', 'exclamation',
        'eye', 'eye-low-vision', 'eye-slash', 'filter', 'fire', 'flask', 'font', 'gear', 'gears', 'grip', 'grip-lines',
        'grip-vertical', 'hammer', 'heart', 'id-badge', 'id-card', 'info', 'italic', 'key', 'lightbulb', 'link',
        'link-slash', 'lock', 'magnifying-glass', 'magnifying-glass-minus', 'magnifying-glass-plus', 'minus',
        'paintbrush', 'palette', 'paperclip', 'pen', 'pen-to-square', 'plus', 'puzzle-piece', 'quote-left',
        'quote-right', 'ruler', 'ruler-combined', 'scissors', 'screwdriver-wrench', 'share-nodes', 'shield',
        'shield-halved', 'sliders', 'star', 'star-half-stroke', 'swatchbook', 'thumbs-down', 'thumbs-up', 'trash',
        'trash-can', 'triangle-exclamation', 'underline', 'unlock', 'user', 'user-check', 'user-gear', 'user-minus',
        'user-pen', 'user-plus', 'user-shield', 'user-xmark', 'users', 'wand-magic-sparkles', 'wrench', 'xmark',
    ],
    '导航与方向': [
        'angles-down', 'angles-left', 'angles-right', 'angles-up', 'arrow-down', 'arrow-down-up-across-line',
        'arrow-left', 'arrow-left-long', 'arrow-right', 'arrow-right-from-bracket', 'arrow-right-long',
        'arrow-right-to-bracket', 'arrow-up', 'arrow-up-right-from-square', 'arrows-rotate', 'arrows-up-down', 'bars',
        'bars-progress', 'bars-staggered', 'box-archive', 'caret-down', 'caret-left', 'caret-right', 'caret-up',
        'chevron-down', 'chevron-left', 'chevron-right', 'chevron-up', 'circle-chevron-down', 'circle-chevron-left',
        'circle-chevron-right', 'circle-chevron-up', 'circle-nodes', 'code-branch', 'code-commit', 'code-fork',
        'code-merge', 'code-pull-request', 'compass', 'cube', 'cubes', 'diagram-project', 'expand', 'flag',
        'flag-checkered', 'folder-tree', 'house', 'layer-group', 'list', 'list-check', 'list-ol', 'list-ul',
        'location-arrow', 'map', 'map-location-dot', 'network-wired', 'object-group', 'object-ungroup', 'road', 'route',
        'signs-post', 'sitemap', 'sort', 'sort-down', 'sort-up', 'table', 'table-cells', 'table-columns', 'table-list',
        'up-down-left-right',
    ],
    '表单与数据': [
        'address-card', 'bag-shopping', 'barcode', 'basket-shopping', 'battery-full', 'bell', 'bell-slash', 'bookmark',
        'box', 'box-open', 'boxes-stacked', 'calendar', 'calendar-check', 'calendar-days', 'calendar-plus',
        'calendar-xmark', 'camera', 'camera-retro', 'cart-plus', 'cart-shopping', 'chart-area', 'chart-column',
        'chart-line', 'chart-pie', 'chart-simple', 'clipboard', 'clipboard-check', 'clipboard-list',
        'clipboard-question', 'clock', 'cloud', 'cloud-arrow-down', 'cloud-arrow-up', 'comment', 'comment-dots',
        'comments', 'computer-mouse', 'credit-card', 'database', 'desktop', 'envelope', 'envelope-open', 'file',
        'file-arrow-down', 'file-arrow-up', 'file-code', 'file-csv', 'file-excel', 'file-image', 'file-lines',
        'file-pdf', 'file-word', 'file-zipper', 'film', 'folder', 'folder-minus', 'folder-open', 'folder-plus',
        'hard-drive', 'hourglass', 'hourglass-half', 'image', 'images', 'inbox', 'keyboard', 'laptop', 'memory',
        'message', 'microchip', 'mobile-screen', 'paper-plane', 'phone', 'photo-film', 'plug', 'print', 'qrcode',
        'server', 'signal', 'stopwatch', 'tablet', 'tag', 'tags', 'tower-broadcast', 'truck', 'truck-fast', 'upload',
        'download', 'video', 'warehouse', 'wifi',
    ],
    '状态与反馈': [
        'award', 'certificate', 'check-to-slot', 'circle-half-stroke', 'circle-notch', 'gift', 'hand-holding-heart',
        'handshake', 'medal', 'rotate', 'rotate-left', 'rotate-right', 'shield-heart', 'spinner', 'square-check',
        'square-minus', 'square-plus', 'square-xmark', 'ticket', 'trophy',
    ],
    '媒体与自然': [
        'backward', 'book', 'book-bookmark', 'book-open', 'circle-pause', 'circle-play', 'cloud-moon', 'cloud-sun',
        'earth-americas', 'flask-vial', 'forward', 'globe', 'graduation-cap', 'headphones', 'infinity', 'language',
        'leaf', 'microphone', 'moon', 'mountain', 'music', 'newspaper', 'pause', 'play', 'podcast', 'radio', 'rocket',
        'rss', 'scroll', 'seedling', 'share', 'stop', 'sun', 'tree', 'universal-access', 'volume-high', 'volume-xmark',
    ],
    '代码与开发': [
        'at', 'bolt-lightning', 'bug', 'bug-slash', 'code', 'code-compare', 'equals', 'hashtag', 'percent',
        'plus-minus', 'satellite-dish', 'shapes', 'square-binary', 'terminal', 'tower-cell',
    ],
    '业务与场景': [
        'briefcase', 'building', 'chart-gantt', 'city', 'coins', 'diagram-predecessor', 'diagram-successor', 'hand',
        'hands', 'hospital', 'hotel', 'industry', 'landmark', 'money-bill', 'receipt', 'scale-balanced', 'school',
        'store', 'user-graduate', 'user-tie', 'users-gear', 'wallet', 'wheelchair-move',
    ],
};

const REPO_ROOT = path.resolve(fileURLToPath(new URL('.', import.meta.url)), '..');const OUTPUT_FILE = path.join(
    REPO_ROOT,
    'src',
    'AeterniUI.Icons.FontAwesome',
    'FontAwesomeIcons.cs',
);

/** Property names that must hide an inherited <see cref="object"/> member. */
const HIDDEN_OBJECT_MEMBERS = new Set(['Equals', 'GetHashCode', 'GetType', 'MemberwiseClone', 'ReferenceEquals', 'ToString']);

/** Converts a Font Awesome kebab-case name into a PascalCase C# property name. */
function toPascalCase(name) {
    return name
        .split('-')
        .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
        .join('');
}

/** Emits the property declaration prefix, adding `new` when a name hides object. */
function toPropertyModifiers(name) {
    return HIDDEN_OBJECT_MEMBERS.has(toPascalCase(name)) ? 'public static new' : 'public static';
}

/** Escapes a string for use inside a C# string literal. */
function toCSharpLiteral(value) {
    return value.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

/** Resolves the `@fortawesome` install directory, installing it on first use. */
function resolveFontAwesomeRoot(explicitDir) {
    if (explicitDir) {
        if (!existsSync(path.join(explicitDir, 'free-solid-svg-icons', 'package.json'))) {
            throw new Error(`--fa-dir does not look like an @fortawesome install: ${explicitDir}`);
        }
        return explicitDir;
    }

    const cacheDir = path.join(tmpdir(), `aeterni-fontawesome-${FA_VERSION}`);
    const scoped = path.join(cacheDir, 'node_modules', '@fortawesome', 'free-solid-svg-icons', 'package.json');
    if (existsSync(scoped)) {
        const installed = JSON.parse(readFileSync(scoped, 'utf8'));
        if (installed.version === FA_VERSION) {
            return path.join(cacheDir, 'node_modules', '@fortawesome');
        }
    }

    mkdirSync(cacheDir, { recursive: true });
    const packages = Object.values(STYLES).map(
        (style) => `@fortawesome/${style.package}@${FA_VERSION}`,
    );
    console.log(`Installing ${packages.join(', ')} into ${cacheDir}`);
    execFileSync(
        process.platform === 'win32' ? 'npm.cmd' : 'npm',
        ['install', '--no-save', '--no-audit', '--no-fund', '--loglevel', 'error', ...packages],
        { cwd: cacheDir, stdio: 'inherit' },
    );
    return path.join(cacheDir, 'node_modules', '@fortawesome');
}

/** Loads one icon definition from the installed Font Awesome packages. */
function loadIcon(requireFn, fontAwesomeRoot, style, name) {
    const camelCase = name.split('-').map((part, index) => (index === 0 ? part : part.charAt(0).toUpperCase() + part.slice(1))).join('');
    const modulePath = path.join(fontAwesomeRoot, style.package, `fa${camelCase}.js`);
    if (!existsSync(modulePath)) {
        throw new Error(`Unknown Font Awesome icon "${name}" (${style.package}@${FA_VERSION})`);
    }

    const loaded = requireFn(modulePath);
    const [width, height, , , svgPathData] = loaded.definition.icon;
    if (loaded.definition.prefix !== style.prefix || loaded.definition.iconName !== name) {
        throw new Error(
            `"${name}" resolves to ${loaded.definition.prefix}:${loaded.definition.iconName}; use the canonical name.`,
        );
    }

    const paths = Array.isArray(svgPathData) ? svgPathData : [svgPathData];
    return { name, width, height, paths };
}

/** Builds the generated C# source for the resolved icons. */
function buildSource(categories) {
    const lines = [];
    lines.push('// <auto-generated>');
    lines.push(`//     Generated by scripts/generate-fontawesome-icons.mjs from Font Awesome Free ${FA_VERSION}.`);
    lines.push('//     Do not edit this file by hand: update the manifest in the script and regenerate.');
    lines.push('//     Icons: CC BY 4.0 (https://creativecommons.org/licenses/by/4.0/); see THIRD-PARTY-NOTICES.md.');
    lines.push('// </auto-generated>');
    lines.push('#nullable enable');
    lines.push('using System.Diagnostics.CodeAnalysis;');
    lines.push('using AeterniUI.Icons;');
    lines.push('');
    lines.push('namespace AeterniUI.Icons.FontAwesome;');
    lines.push('');
    lines.push('/// <summary>');
    lines.push(`/// Selected Font Awesome Free ${FA_VERSION} Classic Solid icon definitions.`);
    lines.push('/// </summary>');
    lines.push('public static class FontAwesomeIcons');
    lines.push('{');
    lines.push('    /// <summary>Font Awesome Free Classic Solid (<c>fas</c>) definitions.</summary>');
    lines.push('    public static class Solid');
    lines.push('    {');

    const all = [];
    for (const [category, icons] of categories) {
        lines.push(`        // ${category}`);
        for (const icon of icons) {
            all.push(icon.name);
            const paths = icon.paths.length === 1
                ? `"${toCSharpLiteral(icon.paths[0])}"`
                : `[${icon.paths.map((entry) => `"${toCSharpLiteral(entry)}"`).join(', ')}]`;
            lines.push(`        /// <summary>Font Awesome Free icon <c>${icon.name}</c>.</summary>`);
            lines.push(`        ${toPropertyModifiers(icon.name)} IconDefinition ${toPascalCase(icon.name)} { get; } = new(`);
            lines.push(`            "${icon.name}",`);
            lines.push(`            ${icon.width},`);
            lines.push(`            ${icon.height},`);
            lines.push(`            ${paths});`);
            lines.push('');
        }
    }
    lines.pop();

    lines.push('    }');
    lines.push('');
    lines.push('    /// <summary>Icon definitions grouped by usage, for pickers and documentation.</summary>');
    lines.push('    public static IReadOnlyList<IconCategory> Categories { get; } =');
    lines.push('    [');
    for (const [category, icons] of categories) {
        lines.push(`        new("${category}",`);
        lines.push('        [');
        for (const icon of icons) {
            lines.push(`            Solid.${toPascalCase(icon.name)},`);
        }
        lines.push('        ]),');
    }
    lines.push('    ];');
    lines.push('');
    lines.push('    private static readonly Dictionary<string, IconDefinition> Lookup = Categories');
    lines.push('        .SelectMany(category => category.Icons)');
    lines.push('        .ToDictionary(icon => icon.Name, StringComparer.OrdinalIgnoreCase);');
    lines.push('');
    lines.push('    /// <summary>');
    lines.push('    /// Resolves a Font Awesome icon name such as <c>arrow-right</c> to its definition.');
    lines.push('    /// </summary>');
    lines.push('    public static bool TryGet(string? name, [NotNullWhen(true)] out IconDefinition? definition)');
    lines.push('    {');
    lines.push('        if (string.IsNullOrWhiteSpace(name))');
    lines.push('        {');
    lines.push('            definition = null;');
    lines.push('            return false;');
    lines.push('        }');
    lines.push('');
    lines.push('        return Lookup.TryGetValue(name.Trim(), out definition);');
    lines.push('    }');
    lines.push('}');
    lines.push('');
    lines.push('/// <summary>A named group of icon definitions.</summary>');
    lines.push('public sealed record IconCategory(string Name, IReadOnlyList<IconDefinition> Icons);');
    lines.push('');

    return { source: lines.join('\n'), count: all.length };
}

function main() {
    const args = process.argv.slice(2);
    const checkOnly = args.includes('--check');
    const dirIndex = args.indexOf('--fa-dir');
    const explicitDir = dirIndex >= 0 ? args[dirIndex + 1] : undefined;

    const fontAwesomeRoot = resolveFontAwesomeRoot(explicitDir);
    const requireFn = createRequire(path.join(fontAwesomeRoot, '..', 'noop.cjs'));

    const seen = new Set();
    const problems = [];
    const categories = Object.entries(MANIFEST).map(([category, names]) => {
        const icons = [];
        for (const name of [...names].sort()) {
            if (seen.has(name)) {
                problems.push(`"${name}" is listed in more than one category.`);
                continue;
            }
            seen.add(name);
            try {
                icons.push(loadIcon(requireFn, fontAwesomeRoot, STYLES.Solid, name));
            } catch (error) {
                problems.push(error.message);
            }
        }
        return [category, icons];
    });

    if (problems.length > 0) {
        console.error(`Icon manifest problems (${problems.length}):`);
        for (const problem of problems) {
            console.error(`  - ${problem}`);
        }
        process.exit(1);
    }

    const { source, count } = buildSource(categories);

    if (checkOnly) {
        const current = existsSync(OUTPUT_FILE) ? readFileSync(OUTPUT_FILE, 'utf8') : '';
        if (current.replace(/\r\n/g, '\n').trimEnd() !== source.trimEnd()) {
            console.error('FontAwesomeIcons.cs is out of date; run node scripts/generate-fontawesome-icons.mjs');
            process.exit(1);
        }
        console.log(`FontAwesomeIcons.cs is up to date (${count} icons).`);
        return;
    }

    writeFileSync(OUTPUT_FILE, source, 'utf8');
    console.log(`Wrote ${path.relative(REPO_ROOT, OUTPUT_FILE)} with ${count} Font Awesome Free ${FA_VERSION} icons in ${categories.length} categories.`);
}

main();
