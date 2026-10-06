const hosts = new WeakMap();
export function attach(host, dotnet, options = {}) {
    detach(host);
    let active = null, last = null, queue = Promise.resolve(), failed = false;
    let activeType = null, pinch = null, promoted = false;
    const touches = new Map();
    const listeners = [];
    function send(payload) {
        queue = queue.then(() => failed ? undefined : dotnet.invokeMethodAsync("HandlePointer", payload))
            .catch(error => { failed = true; console.error("Beep.Skia pointer delivery failed", error);
                host.setAttribute("aria-label", "Drawing canvas: input failed. Reload the application."); });
    }
    function deliver(action, event) {
        const box = host.getBoundingClientRect();
        last = { action, x: event.clientX - box.left, y: event.clientY - box.top,
            button: event.button ?? 0, control: !!(event.ctrlKey || event.metaKey), shift: !!event.shiftKey,
            alt: !!event.altKey, delta: (event.deltaY ?? 0) * (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? box.height : 1),
            deltaX: (event.deltaX ?? 0) * (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? box.width : 1) };
        last.pointerId = event.pointerId ?? 0; last.pointerType = event.pointerType ?? 'mouse';
        last.pressure = Math.max(0,Math.min(1,event.pressure??0));
        if (action === "move" && event.getCoalescedEvents) {
            const samples = event.getCoalescedEvents();
            // Keep browser sample order in bounded batches instead of one interop call per sample.
            for (let i = 0; i < samples.length; i += 256) {
                const chunk = samples.slice(i, i + 256).map(p => ({ x: p.clientX - box.left, y: p.clientY - box.top }));
                if (i + 256 < samples.length) send({ ...last, x: chunk.at(-1).x, y: chunk.at(-1).y, samples: chunk });
                else last.samples = chunk;
            }
        }
        send(last);
    }
    function point(event) {
        const box = host.getBoundingClientRect(); return {x:event.clientX-box.left,y:event.clientY-box.top};
    }
    function pair() {
        const [a,b] = touches.values();
        return {x:(a.x+b.x)/2,y:(a.y+b.y)/2,distance:Math.max(1,Math.hypot(a.x-b.x,a.y-b.y))};
    }
    function release(id) { if (host.hasPointerCapture(id)) host.releasePointerCapture(id); }
    function cancelAll() {
        const id = active; active = null; activeType = null;
        if (id !== null && last) send({...last,action:'cancel',samples:[]});
        if (pinch) send({action:'pinchend',x:0,y:0});
        pinch = null; promoted = false;
        const ids = [...touches.keys()]; touches.clear();
        if (id !== null) release(id); ids.forEach(release);
    }
    function listen(target, name, callback, options) {
        target.addEventListener(name, callback, options);
        listeners.push(() => target.removeEventListener(name, callback, options));
    }
    listen(host, "pointerdown", event => {
        if (options.viewportGestures && event.pointerType === 'touch') {
            if (active === null && touches.size === 0 && !event.isPrimary) return;
            if (activeType === 'pen' || touches.size >= 2 || promoted) return;
            if (active !== null && activeType !== 'touch') return;
            touches.set(event.pointerId,point(event));
            if (touches.size === 2) {
                event.preventDefault(); host.setPointerCapture(event.pointerId);
                if (last) send({...last,action:'cancel',samples:[]});
                active = null; activeType = null; promoted = true; pinch = pair();
                send({action:'pinchstart',x:pinch.x,y:pinch.y,scale:1}); return;
            }
        }
        if (active !== null || !event.isPrimary || event.button > 1) return;
        active = event.pointerId;
        activeType = event.pointerType ?? 'mouse';
        host.focus({ preventScroll: true });
        host.setPointerCapture(active);
        event.preventDefault();
        deliver("down", event);
    });
    listen(host, "pointermove", event => {
        if (promoted && touches.has(event.pointerId)) {
            touches.set(event.pointerId,point(event));
            if (pinch && touches.size === 2) { const current = pair(); send({action:'pinchmove',x:current.x,y:current.y,scale:current.distance/pinch.distance}); }
            return;
        }
        if (touches.has(event.pointerId)) touches.set(event.pointerId,point(event));
        if (event.pointerId === active) deliver("move", event);
    });
    function finish(action, event) {
        if (promoted && touches.has(event.pointerId)) {
            touches.delete(event.pointerId);
            if (pinch) { send({action:'pinchend',x:0,y:0}); pinch = null; }
            if (touches.size === 0) promoted = false;
            release(event.pointerId); return;
        }
        if (event.pointerId !== active) return;
        const id = active; active = null; activeType = null; touches.delete(id);
        deliver(action, event);
        if (host.hasPointerCapture(id)) host.releasePointerCapture(id);
    }
    listen(host, "pointerup", event => finish("up", event));
    listen(host, "pointercancel", event => finish("cancel", event));
    listen(host, "lostpointercapture", event => finish("cancel", event));
    listen(window, "blur", cancelAll);
    listen(window, "resize", cancelAll);
    listen(host, "wheel", event => { event.preventDefault(); if (!promoted) deliver("wheel", event); }, { passive: false });
    listen(host, "contextmenu", event => event.preventDefault());
    listen(host, "dblclick", event => deliver("doubleclick", event));
    let observer;
    if (options.viewportGestures && typeof ResizeObserver !== 'undefined') {
        let previous;
        observer = new ResizeObserver(() => {
            const box = host.getBoundingClientRect();
            const size = `${box.width},${box.height}`;
            if (size === previous) return;
            previous = size; cancelAll(); send({action:'resize',x:0,y:0,width:box.width,height:box.height});
        }); observer.observe(host);
    }
    hosts.set(host, () => { listeners.forEach(remove => remove()); observer?.disconnect(); failed = true;
        cancelAll(); });
}
export function detach(host) { hosts.get(host)?.(); hosts.delete(host); }
