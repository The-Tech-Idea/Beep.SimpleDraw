import test from "node:test";
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
const source = await readFile(new URL("../../Beep.Skia/Beep.Skia.Blazor.Controls/wwwroot/skiaHost.js", import.meta.url), "utf8");
const bridge = await import("data:text/javascript;base64," + Buffer.from(source).toString("base64"));

class Target {
    listeners = new Map(); captures = new Set(); attributes = new Map();
    get capture() { return this.captures.size ? [...this.captures][0] : null; }
    addEventListener(name, fn) { (this.listeners.get(name) ?? this.listeners.set(name, []).get(name)).push(fn); }
    removeEventListener(name, fn) { this.listeners.set(name, (this.listeners.get(name) ?? []).filter(x => x !== fn)); }
    emit(name, values = {}) { for (const fn of this.listeners.get(name) ?? []) fn({
        clientX: 140, clientY: 90, pointerId: 1, isPrimary: true, button: 0,
        preventDefault() {}, ...values }); }
    getBoundingClientRect() { return { left: 40, top: 20, width: 320, height: 600 }; }
    setPointerCapture(id) { this.captures.add(id); }
    hasPointerCapture(id) { return this.captures.has(id); }
    releasePointerCapture(id) { this.captures.delete(id); }
    focus() {}
    setAttribute(name, value) { this.attributes.set(name, value); }
}
const flush = () => new Promise(resolve => setImmediate(resolve));
function setup(options = {}) {
    globalThis.window = new Target();
    const host = new Target(), events = [];
    bridge.attach(host, { async invokeMethodAsync(name, event) { assert.equal(name, "HandlePointer"); events.push(event); } }, options);
    return { host, events };
}
test("CSS coordinates and queued down/move/up preserve pointer order", async () => {
    const { host, events } = setup();
    host.emit("pointerdown"); host.emit("pointermove", { clientX: 200 }); host.emit("pointerup", { clientX: 210 });
    await flush();
    assert.deepEqual(events.map(e => e.action), ["down", "move", "up"]);
    assert.equal(events[0].x, 100); assert.equal(events[0].y, 70); assert.equal(events[2].x, 170);
    assert.equal(host.capture, null); bridge.detach(host);
});
test("secondary pointers do not steal the primary gesture", async () => {
    const { host, events } = setup();
    host.emit("pointerdown"); host.emit("pointerdown", { pointerId: 2, isPrimary: false });
    host.emit("pointermove", { pointerId: 2 }); host.emit("pointerup", { pointerId: 2 });
    host.emit("pointerup"); await flush();
    assert.deepEqual(events.map(e => e.action), ["down", "up"]); bridge.detach(host);
});
test("lost capture and window blur cancel once", async () => {
    const { host, events } = setup();
    host.emit("pointerdown"); host.emit("lostpointercapture"); host.emit("pointerup");
    await flush(); assert.deepEqual(events.map(e => e.action), ["down", "cancel"]);
    host.emit("pointerdown"); window.emit("blur"); await flush();
    assert.deepEqual(events.map(e => e.action), ["down", "cancel", "down", "cancel"]); bridge.detach(host);
});
test("detach removes browser and global listeners", () => {
    const { host } = setup(); bridge.detach(host);
    assert.equal([...host.listeners.values()].flat().length, 0);
    assert.equal([...window.listeners.values()].flat().length, 0);
});
test("wheel direction and modifiers are passed without DPR multiplication", async () => {
    const { host, events } = setup();
    host.emit("wheel", { deltaY: -100, ctrlKey: true, shiftKey: true });
    await flush(); assert.equal(events[0].delta, -100); assert.equal(events[0].control, true);
    assert.equal(events[0].shift, true); assert.equal(events[0].x, 100); bridge.detach(host);
});
test("trackpad horizontal scrolling and Command zoom reach the same host contract", async () => {
    const { host, events } = setup();
    host.emit("wheel", { deltaX: 24, deltaY: 32, metaKey: true }); await flush();
    assert.equal(events[0].deltaX,24); assert.equal(events[0].delta,32);
    assert.equal(events[0].control,true); bridge.detach(host);
});
test("line and page wheel units normalize to CSS pixels on both axes", async () => {
    const { host, events } = setup();
    host.emit("wheel", { deltaX: 2, deltaY: 3, deltaMode: 1 });
    host.emit("wheel", { deltaX: 1, deltaY: -1, deltaMode: 2 }); await flush();
    assert.equal(events[0].deltaX,32); assert.equal(events[0].delta,48);
    assert.equal(events[1].deltaX,320); assert.equal(events[1].delta,-600); bridge.detach(host);
});
test("coalesced pen samples preserve order in bounded interop batches", async () => {
    const { host, events } = setup();
    const samples = Array.from({ length: 600 }, (_, i) => ({ clientX: 40 + i, clientY: 20 + i / 2 }));
    host.emit("pointerdown"); host.emit("pointermove", { clientX: 640, clientY: 320, getCoalescedEvents: () => samples });
    host.emit("pointerup", { clientX: 640, clientY: 320 }); await flush();
    const moves = events.filter(e => e.action === "move");
    assert.deepEqual(moves.map(e => e.samples.length), [256, 256, 88]);
    assert.deepEqual(moves.flatMap(e => e.samples).map(p => p.x), samples.map(p => p.clientX - 40));
    assert.equal(moves[0].x, 255); assert.equal(moves.at(-1).x, 600);
    assert.equal(events.at(-1).action, "up"); bridge.detach(host);
});
test("double click reaches the host without changing pointer capture", async () => {
    const { host, events } = setup(); host.emit("dblclick", { clientX: 220, clientY: 120 }); await flush();
    assert.equal(events[0].action, "doubleclick"); assert.equal(events[0].x, 180); assert.equal(events[0].y, 100);
    assert.equal(host.capture, null); bridge.detach(host);
});

test('two touches cancel the preview and promote to anchored pinch; remaining finger never draws', async () => {
    const {host,events} = setup({viewportGestures:true});
    host.emit('pointerdown',{pointerType:'touch',clientX:100,clientY:100});
    host.emit('pointermove',{pointerType:'touch',clientX:120,clientY:100});
    host.emit('pointerdown',{pointerId:2,pointerType:'touch',isPrimary:false,clientX:220,clientY:100});
    assert.equal(host.captures.size,2);
    host.emit('pointermove',{pointerId:2,pointerType:'touch',clientX:320,clientY:100});
    host.emit('pointerdown',{pointerId:3,pointerType:'touch',isPrimary:false});
    host.emit('pointerup',{pointerId:2,pointerType:'touch'});
    host.emit('pointermove',{pointerType:'touch'}); host.emit('pointerup',{pointerType:'touch'});
    await flush(); assert.deepEqual(events.map(e=>e.action),['down','move','cancel','pinchstart','pinchmove','pinchend']);
    assert.equal(events[3].x,130); assert.equal(events[3].y,80); assert.equal(events[4].scale,2);
    assert.equal(events[4].x,180); assert.equal(host.captures.size,0);
    host.emit('pointerdown',{pointerType:'touch'}); host.emit('pointerup',{pointerType:'touch'});
    await flush(); assert.deepEqual(events.slice(-2).map(e=>e.action),['down','up']); bridge.detach(host);
});
test('pen metadata is clamped and incidental palm touch cannot change an active pen stroke', async () => {
    const {host,events} = setup({viewportGestures:true});
    host.emit('pointerdown',{pointerType:'pen',pressure:2,pointerId:8});
    host.emit('pointerdown',{pointerType:'touch',pointerId:9});
    host.emit('pointerup',{pointerType:'touch',pointerId:9});
    host.emit('pointerup',{pointerType:'pen',pointerId:8}); await flush();
    assert.deepEqual(events.map(e=>e.action),['down','up']); assert.equal(events[0].pressure,1);
    assert.equal(events[0].pointerType,'pen'); assert.equal(events[0].pointerId,8); bridge.detach(host);
});
test('orientation and observed CSS size cancel previews once and remove observer on detach', async () => {
    let observer;
    globalThis.ResizeObserver = class { constructor(callback) { this.callback=callback; observer=this; } observe(){} disconnect(){this.disconnected=true;} };
    const {host,events} = setup({viewportGestures:true}); observer.callback(); await flush();
    assert.equal(events[0].action,'resize'); assert.equal(events[0].width,320); assert.equal(events[0].height,600);
    observer.callback(); host.emit('pointerdown'); window.emit('resize'); window.emit('blur'); await flush();
    assert.deepEqual(events.map(e=>e.action),['resize','down','cancel']);
    bridge.detach(host); assert.equal(observer.disconnected,true); delete globalThis.ResizeObserver;
});
