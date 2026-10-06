let shortcuts, focused, lastFocused, modal, returnFocus;
const tabbables = 'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),summary,[tabindex="0"]';
function modalElements(container) { return [...container.querySelectorAll(tabbables)].filter(el=>!el.closest('[inert]') && el.getClientRects().length); }
export function syncModal() {
    const next = [...document.querySelectorAll('[aria-modal="true"]')].at(-1) ?? null;
    if (next === modal) return;
    if (!next) {
        modal = null;
        if (returnFocus?.isConnected && !returnFocus.closest('[inert]')) returnFocus.focus({preventScroll:true});
        else focusCanvas();
        returnFocus = null; return;
    }
    if (!modal) returnFocus = lastFocused;
    modal = next;
    (next.querySelector('[data-initial-focus]') ?? modalElements(next)[0] ?? next).focus({preventScroll:true});
}
export function focusCanvas() { document.querySelector('.drawing-surface')?.focus({preventScroll:true}); }
export function attachShortcuts(dotnet) {
    detachShortcuts();
    shortcuts = event => {
        if (event.isComposing || event.key === 'Process') return;
        if (modal) {
            if (event.key === 'Tab') {
                const list = modalElements(modal), index = list.indexOf(document.activeElement);
                if (!list.length) { event.preventDefault(); modal.focus(); return; }
                if (event.shiftKey && index <= 0) { event.preventDefault(); list.at(-1).focus(); }
                else if (!event.shiftKey && (index < 0 || index === list.length-1)) { event.preventDefault(); list[0].focus(); }
            }
            if (event.key === 'Escape') {
                event.preventDefault(); dotnet.invokeMethodAsync('CloseModal',modal.dataset.modalKind).catch(error=>console.error('Dialog close failed',error)); return;
            }
            if (modal.dataset.modalKind === 'text' && event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
                event.preventDefault(); dotnet.invokeMethodAsync('TextShortcut',true).catch(error=>console.error('Text command failed',error));
            }
            return;
        }
        if (event.target.closest("textarea") && (event.key === "Escape" || (event.key === "Enter" && (event.ctrlKey || event.metaKey)))) {
            event.preventDefault(); dotnet.invokeMethodAsync("TextShortcut", event.key === "Enter").catch(error => console.error("Text command failed", error)); return;
        }
        if (event.target.closest("input, textarea, select, [contenteditable=true]")) return;
        const control = event.ctrlKey || event.metaKey;
        const key = control ? event.key.toLowerCase() : event.key;
        const onCanvas = !!event.target.closest('.drawing-surface');
        if ((key === 'Enter' || key.startsWith('Arrow')) && !onCanvas) return;
        const handled = control ? ["z", "y", "s", "d", "g", "a"].includes(key) :
            ["v", "r", "o", "d", "h", "l", "a", "p", "e", "t", "V", "R", "O", "D", "H", "L", "A", "P", "E", "T", "Delete", "Backspace", "Escape",
                "ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Enter", "?"].includes(key);
        if (!handled || event.altKey) return;
        event.preventDefault();
        dotnet.invokeMethodAsync("Shortcut", key, control, event.shiftKey).catch(error => console.error("Shortcut failed", error));
    };
    focused = event => {
        if (modal && !modal.contains(event.target)) { (modalElements(modal)[0] ?? modal).focus({preventScroll:true}); return; }
        if (!modal) lastFocused = event.target;
    };
    window.addEventListener("keydown", shortcuts);
    window.addEventListener('focusin',focused);
}
export function detachShortcuts() {
    if (shortcuts) window.removeEventListener("keydown", shortcuts);
    if (focused) window.removeEventListener('focusin',focused);
    shortcuts = focused = modal = returnFocus = lastFocused = null;
}
export function download(name, contents, mime = 'application/json') {
    downloadBlob(name, new Blob([contents], {type:mime}));
}
export function downloadBytes(name, contents, mime) { downloadBlob(name, new Blob([contents],{type:mime})); }
function downloadBlob(name, blob) {
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url; link.download = name; document.body.append(link); link.click(); link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 10000);
}
let updateRegistration;
export async function watchUpdates(dotnet) {
    if (!navigator.serviceWorker) return;
    try {
        updateRegistration = await navigator.serviceWorker.getRegistration(new URL(document.baseURI).href);
        if (!updateRegistration) return;
        const report = () => { if (updateRegistration.waiting && navigator.serviceWorker.controller) dotnet.invokeMethodAsync('UpdateReady'); };
        navigator.serviceWorker.addEventListener('message', event => {
            if (event.data?.type === 'UPDATE_BLOCKED') dotnet.invokeMethodAsync('UpdateBlocked',event.data.reason);
        });
        report();
        updateRegistration.addEventListener('updatefound', () => {
            const worker = updateRegistration.installing;
            worker?.addEventListener('statechange', report);
        });
    } catch (error) { console.warn('Offline update check failed', error); }
}
export function applyUpdate() {
    if (!updateRegistration?.waiting) return;
    // Other open editor tabs block activation; never interrupt their dirty edits.
    navigator.serviceWorker.addEventListener('controllerchange', () => location.reload(), {once:true});
    updateRegistration.waiting.postMessage({type:'ACTIVATE_IF_SINGLE_EDITOR'});
}
