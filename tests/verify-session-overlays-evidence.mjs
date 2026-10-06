import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

// Verify retained observations. This is not a test runner or browser automation.
const root = new URL('../docs/evidence/', import.meta.url);
const bytes = name => fs.readFileSync(new URL(name, root));
const text = name => bytes(name).toString();
const json = name => JSON.parse(text(name));
const current = suffix => `p6-session-overlays-${suffix}`;
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
function counters(name, total) {
    const tag = text(name).match(/<Counters\b[^>]*>/)?.[0]; assert.ok(tag, name);
    const values = Object.fromEntries([...tag.matchAll(/(\w+)="(\d+)"/g)].map(m => [m[1], +m[2]]));
    for (const key of ['total', 'executed', 'passed']) assert.equal(values[key], total, name);
    for (const key of ['failed', 'error', 'timeout', 'aborted', 'notExecuted']) assert.equal(values[key], 0, name);
    return values;
}
const release = counters(current('release.trx'), 289);
const trx = text(current('release.trx'));
const overlayCases = [...trx.matchAll(/<UnitTestResult\b[^>]*testName="Beep.SimpleDraw.Tests.DrawingOverlayTests\.[^>]*>/g)];
assert.equal(overlayCases.length, 48);
assert.ok(overlayCases.every(m => m[0].includes('outcome="Passed"')));
const frames = [...trx.matchAll(/overlay frame=\d+; zoom=([\d.]+); dpi=([\d.]+); css=(\d+x\d+); exact pixels=True; footprint contained=True/g)];
assert.equal(frames.length, 528);
const coverage = {cases: overlayCases.length, exactFrames: frames.length,
    zooms: [...new Set(frames.map(m => +m[1]))].sort((a,b) => a-b),
    deviceTransforms: [...new Set(frames.map(m => +m[2]))].sort((a,b) => a-b),
    cssSizes: [...new Set(frames.map(m => m[3]))].sort()};
assert.deepEqual(coverage.zooms, [.41, .875, 1.75]);
assert.deepEqual(coverage.deviceTransforms, [1.25, 2]);
assert.deepEqual(coverage.cssSizes, ['320x360', '640x480']);
const report = json(current('ordinary-mixed-repeat.json'));
assert.equal(report.error, null); assert.equal(report.runs.length, 2);
assert.equal(report.environment.visibility, 'visible');
assert.deepEqual(report.environment.viewport, [1280,720]); assert.equal(report.environment.pixelRatio, 1);
const timings = report.runs.map(run => {
    assert.equal(run.objects, 1000); assert.equal(run.committedEdits, 140);
    assert.equal(run.warmup, 20); assert.equal(run.samples, 120); assert.equal(run.rawMs.length, 120);
    assert.equal(run.profile, null); assert.ok(run.rawMs.every(ms => Number.isFinite(ms) && ms >= 0));
    const sorted = [...run.rawMs].sort((a,b) => a-b);
    assert.equal(run.medianMs, sorted[59]); assert.equal(run.p95Ms, sorted[113]); assert.equal(run.maxMs, sorted.at(-1));
    assert.equal(run.budgetMs, 50); assert.equal(run.budgetPassed, true); assert.ok(run.p95Ms <= 50);
    for (const key of ['x','y','selected','objects']) assert.equal(run.final[key], run.initial[key]);
    for (const key of ['revision','invalidationDispatches','paints']) assert.equal(run.final[key]-run.initial[key], 140);
    assert.equal(run.final.selected, 1); assert.equal(run.final.undoCount, 100); assert.equal(run.final.maxHistoryDepth, 100);
    return {medianMs:run.medianMs, p95Ms:run.p95Ms, maxMs:run.maxMs, budgetPassed:run.budgetPassed};
});
assert.deepEqual(json(current('ordinary-mixed.json')).runs[0], report.runs[0]);
const scheduling = json(current('scheduling.json')).schedulingChecks;
assert.equal(scheduling.length, 1); assert.equal(scheduling[0].passed, true);
for (const key of ['paints','invalidationDispatches']) assert.equal(scheduling[0].final[key]-scheduling[0].initial[key], 2);
for (const key of ['revision','undoCount','objects','selected','x','y']) assert.equal(scheduling[0].final[key], scheduling[0].initial[key]);
const seven = bytes('p6-publish-cache-guard-baseline.simpledraw.json');
for (const suffix of ['normal.simpledraw.json','offline.simpledraw.json','responsive-restored.simpledraw.json'])
    assert.ok(seven.equals(bytes(current(suffix))), suffix);
const mixed = bytes('p6-aot-baseline.simpledraw.json'); assert.ok(mixed.equals(bytes(current('mixed.simpledraw.json'))));
for (const mode of ['normal','offline']) {
    assert.match(text(current(`${mode}-width4.ax.txt`)), /Stroke width 4, Value: 1/);
    assert.match(text(current(`${mode}-undo.ax.txt`)), /Stroke width 2, Value: 1/);
}
assert.match(text(current('offline-restored.ax.txt')), /Restored from this device/);
assert.match(text(current('offline-restored.ax.txt')), /Objects 7/);
const layout = json(current('320-layout.json'));
assert.deepEqual(layout.viewport, {height:720, pixelRatio:1, width:320}); assert.equal(layout.documentWidth, 320);
assert.equal(layout.tools.length, 11); assert.ok(layout.tools.every(t => t.width === 44 && t.height === 44));
const original = JSON.parse(seven), moved = json(current('320-moved.simpledraw.json'));
const shapes = original.diagram.components;
const width = Math.max(...shapes.map(s => s.x+s.width))-Math.min(...shapes.map(s => s.x));
const height = Math.max(...shapes.map(s => s.y+s.height))-Math.min(...shapes.map(s => s.y));
const zoom = Math.min((layout.canvas[0].cssWidth-48)/width, (layout.canvas[0].cssHeight-260)/height);
const ellipse = shapes.find(s => s.name === 'Ellipse 3'), changed = moved.diagram.components.find(s => s.id === ellipse.id);
const actualDelta = [changed.x-ellipse.x, changed.y-ellipse.y], expectedDelta = [10/zoom, 5/zoom];
actualDelta.forEach((delta,i) => assert.ok(Math.abs(delta-expectedDelta[i]) < .0001));
changed.x = ellipse.x; changed.y = ellipse.y; assert.deepEqual(moved, original);
assert.match(text(current('320-resized.ax.txt')), /Width, Value: 240/); assert.match(text(current('320-resized.ax.txt')), /Height, Value: 108/);
assert.match(text(current('320-undo.ax.txt')), /Width, Value: 200/); assert.match(text(current('320-undo.ax.txt')), /Height, Value: 90/);
const help = json(current('640-help-layout.json'));
assert.deepEqual(help, {clientHeight:258, documentWidth:640, height:260, scrollHeight:500, viewport:[640,360], width:370});
assert.match(text(current('640-focus-return.ax.txt')), /The focused UI element is \d+ button Keyboard shortcuts/);
const stopped = json(current('server-stopped.json')); assert.equal(stopped.port, 5243); assert.equal(stopped.activeListeners, 0);
assert.deepEqual(json(current('browser-logs.json')), []);
assert.deepEqual(json(current('normal-dom.json')), {url:'http://127.0.0.1:5243/draw/', diagnosticPanels:0});
const assets = json(current('asset-report.json')), provenance = json(current('provenance.json'));
assert.equal(assets.offlineVersion, '00ef50ab8e91f107'); assert.equal(assets.offlineAssets, 134);
assert.deepEqual(assets.totals, {identity:29935561, br:8152076, gzip:10770530});
assert.equal(provenance.source.sourceFiles, 210);
assert.equal(provenance.source.sourceSha256, '4f280f9375360e373833f1a97ccb98cf5159f484b767444dba20610c200d7993');
assert.deepEqual(provenance.source, json('p6-frame-cache-provenance.json').source);
const sibling = counters('p6-frame-cache-sibling.trx', 504);
for (const name of [current('build.log'), 'p6-frame-cache-net8-host.log']) {
    assert.match(text(name), /Build succeeded/); assert.match(text(name), /0 Warning\(s\)/); assert.match(text(name), /0 Error\(s\)/);
}
assert.match(text(current('javascript-tests.log')), /pass 72/); assert.match(text(current('javascript-tests.log')), /fail 0/);
assert.match(text(current('archive.log')), /all 402 ZIP payload entries/);
const summary = {date:'2026-10-06', source:provenance.source, release, coverage, timings, scheduling,
    unchangedSiblingInputs:{sibling, evidence:'p6-frame-cache-sibling.trx', net8:'p6-frame-cache-net8-host.log', freshRun:false},
    native:{sevenBytes:seven.length, sevenSha256:hash(seven), mixedBytes:mixed.length, mixedSha256:hash(mixed), exactDownloads:true},
    responsive:{layout, help, pointer:{cssDelta:[10,5], zoom, actualDelta, expectedDelta, tolerance:.0001, otherFieldsExact:true}},
    offline:{stopped}, assets:{offlineVersion:assets.offlineVersion, totals:assets.totals},
    decision:'Production session overlays are covered by exact native frames and repeated browser release gates. Native device transforms do not certify physical browser DPI/zoom. External device, screen-reader, intended host/provider/cold-process/CI/install and independent acceptance remain open.'};
fs.writeFileSync(new URL(current('summary.json'), root), JSON.stringify(summary,null,2)+'\n');
console.log(JSON.stringify({coverage,timings,native:summary.native,pointer:summary.responsive.pointer}));
