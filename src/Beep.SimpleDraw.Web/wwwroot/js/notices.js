import {downloadBytes} from './app.js';

// Fetch participates in the installed offline cache; the shared Blob download
// does not require the browser's download navigation to reach a live server.
export function attachNoticeDownload(button, status, source) {
    button.addEventListener('click', async () => {
        if (button.disabled) return;
        button.disabled = true;
        status.textContent = 'Preparing notices…';
        try {
            const response = await fetch(source, {mode:'same-origin', redirect:'error'});
            if (!response.ok) throw new Error('Notice file unavailable');
            const contents = new Uint8Array(await response.arrayBuffer());
            downloadBytes('THIRD-PARTY-NOTICES.txt', contents, 'text/plain;charset=utf-8');
            status.textContent = 'Notices download requested.';
        } catch {
            status.textContent = 'Could not download notices. Try again or read the original texts below.';
        } finally {
            button.disabled = false;
        }
    });
}

if (typeof document !== 'undefined') {
    const button = document.getElementById('download-notices');
    const status = document.getElementById('notices-download-status');
    if (button && status) attachNoticeDownload(button, status,
        new URL('THIRD-PARTY-NOTICES.txt', document.baseURI).href);
}
