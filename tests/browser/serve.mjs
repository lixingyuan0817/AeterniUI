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

// A browser that aborts a navigation while a response is in flight raises EPIPE /
// write-after-end on the response. In Node 24 that rejection became an unhandled
// rejection that killed the server mid-suite, leaving verify.mjs waiting on a dead
// endpoint. Every failure is now contained per request instead of taking the
// process down.
const server = createServer((request, response) => {
    request.on('error', () => {});
    response.on('error', () => {});

    const url = new URL(request.url, 'http://localhost');
    let path = join(root, normalize(decodeURIComponent(url.pathname)).replace(/^(\.\.[/\\])+/, ''));

    const send = (status, type, body) => {
        if (response.writableEnded || response.destroyed) return;
        response.writeHead(status, { 'Content-Type': type });
        response.end(body);
    };

    const fallback = async () => {
        try {
            send(200, TYPES['.html'], await readFile(join(root, 'index.html')));
        } catch {
            send(404, 'text/plain; charset=utf-8', 'not found');
        }
    };

    (async () => {
        try {
            const info = await stat(path).catch(() => null);
            if (!info || info.isDirectory()) {
                path = join(path, 'index.html');
            }
            send(200, TYPES[extname(path)] ?? 'application/octet-stream', await readFile(path));
        } catch {
            // SPA fallback: extensionless deep links get the app shell.
            await fallback();
        }
    })().catch(fallback);
});

server.on('clientError', (error, socket) => {
    if (socket.writable) socket.end('HTTP/1.1 400 Bad Request\r\n\r\n');
    else socket.destroy();
});

server.listen(port, '127.0.0.1', () => console.log(`serving ${root} on http://127.0.0.1:${port}`));
