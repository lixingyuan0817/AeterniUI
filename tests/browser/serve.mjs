// Minimal static server for the published sample, with the MIME types Blazor
// WASM needs and an SPA fallback so /components/... deep links resolve.
import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';

const root = process.argv[2];
const port = Number(process.argv[3] ?? 5099);

const TYPES = {
    '.html': 'text/html; charset=utf-8',
    '.js': 'text/javascript; charset=utf-8',
    '.mjs': 'text/javascript; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.json': 'application/json; charset=utf-8',
    '.wasm': 'application/wasm',
    '.dll': 'application/octet-stream',
    '.pdb': 'application/octet-stream',
    '.dat': 'application/octet-stream',
    '.blat': 'application/octet-stream',
    '.svg': 'image/svg+xml',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.jpeg': 'image/jpeg',
    '.ico': 'image/x-icon',
    '.woff': 'font/woff',
    '.woff2': 'font/woff2',
    '.ttf': 'font/ttf',
    '.map': 'application/json; charset=utf-8'
};

createServer(async (request, response) => {
    const url = new URL(request.url, 'http://localhost');
    let path = join(root, normalize(decodeURIComponent(url.pathname)).replace(/^(\.\.[/\\])+/, ''));
    try {
        const info = await stat(path).catch(() => null);
        if (!info || info.isDirectory()) {
            path = join(path, 'index.html');
        }
        const body = await readFile(path);
        response.writeHead(200, { 'Content-Type': TYPES[extname(path)] ?? 'application/octet-stream' });
        response.end(body);
    } catch {
        // SPA fallback: extensionless deep links get the app shell.
        try {
            const body = await readFile(join(root, 'index.html'));
            response.writeHead(200, { 'Content-Type': TYPES['.html'] });
            response.end(body);
        } catch {
            response.writeHead(404).end('not found');
        }
    }
}).listen(port, '127.0.0.1', () => console.log(`serving ${root} on http://127.0.0.1:${port}`));
