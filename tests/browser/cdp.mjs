// Minimal CDP client (no dependencies: Node 24 ships a global WebSocket).
export async function connect(cdpUrl = process.env.AETERNI_CDP_URL ?? 'http://127.0.0.1:9222') {
    const targets = await (await fetch(`${cdpUrl}/json/list`)).json();
    const page = targets.find(t => t.type === 'page');
    const socket = new WebSocket(page.webSocketDebuggerUrl);
    await new Promise((resolve, reject) => {
        socket.addEventListener('open', resolve, { once: true });
        socket.addEventListener('error', reject, { once: true });
    });

    let nextId = 1;
    const pending = new Map();
    const listeners = new Map();
    socket.addEventListener('message', event => {
        const message = JSON.parse(event.data);
        if (message.id && pending.has(message.id)) {
            const { resolve, reject } = pending.get(message.id);
            pending.delete(message.id);
            message.error ? reject(new Error(JSON.stringify(message.error))) : resolve(message.result);
            return;
        }
        for (const handler of listeners.get(message.method) ?? []) handler(message.params);
    });

    const send = (method, params = {}) => new Promise((resolve, reject) => {
        const id = nextId++;
        pending.set(id, { resolve, reject });
        socket.send(JSON.stringify({ id, method, params }));
    });

    const once = method => new Promise(resolve => {
        const handler = params => {
            listeners.get(method).delete(handler);
            resolve(params);
        };
        if (!listeners.has(method)) listeners.set(method, new Set());
        listeners.get(method).add(handler);
    });

    return { send, once, close: () => socket.close() };
}

export function makeHelpers({ send, once }) {
    async function evaluate(expression) {
        const { result, exceptionDetails } = await send('Runtime.evaluate', {
            expression, awaitPromise: true, returnByValue: true, userGesture: true
        });
        if (exceptionDetails) throw new Error(exceptionDetails.exception?.description ?? exceptionDetails.text);
        return result.value;
    }

    async function goto(url) {
        const loaded = once('Page.loadEventFired');
        await send('Page.navigate', { url });
        await loaded;
    }

    // Blazor WASM boots asynchronously; wait until the sample has rendered.
    async function waitForApp(timeoutMs = 60000) {
        const deadline = Date.now() + timeoutMs;
        while (Date.now() < deadline) {
            const ready = await evaluate(
                `document.querySelectorAll('[class*="aeterni-"]').length > 10`);
            if (ready) return true;
            await new Promise(r => setTimeout(r, 250));
        }
        throw new Error('the sample did not render in time');
    }

    // `key` moves focus and drives keydown listeners. It deliberately omits
    // `text`, so the key's *default action* does not fire.
    async function key(keyName, code, keyCode) {
        const base = { key: keyName, code, windowsVirtualKeyCode: keyCode, nativeVirtualKeyCode: keyCode };
        await send('Input.dispatchKeyEvent', { ...base, type: 'keyDown' });
        await send('Input.dispatchKeyEvent', { ...base, type: 'keyUp' });
    }

    // `press` additionally passes `text`, which is what makes the browser run the
    // default action — Enter on a focused button synthesises a click only with it.
    async function press(keyName, code, keyCode, text) {
        const base = { key: keyName, code, windowsVirtualKeyCode: keyCode, nativeVirtualKeyCode: keyCode };
        await send('Input.dispatchKeyEvent', { ...base, type: 'keyDown', text, unmodifiedText: text });
        await send('Input.dispatchKeyEvent', { ...base, type: 'keyUp' });
    }

    async function setReducedMotion(enabled) {
        await send('Emulation.setEmulatedMedia', {
            features: [{ name: 'prefers-reduced-motion', value: enabled ? 'reduce' : 'no-preference' }]
        });
    }

    return { evaluate, goto, waitForApp, once, key, press, setReducedMotion };
}
