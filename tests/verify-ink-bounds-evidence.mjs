import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

// Checks retained observations only; it does not execute native tests or browser actions.
const root = new URL('../docs/evidence/', import.meta.url);
const bytes = name => fs.readFileSync(new URL(name, root));
const text = name => bytes(name).toString();
const json = name => JSON.parse(text(name));
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
function counters(name, expected) {
  const tag = text(name).match(/<Counters\b[^>]*>/)?.[0]; assert.ok(tag, name);
  const values = Object.fromEntries([...tag.matchAll(/(\w+)="(\d+)"/g)].map(m => [m[1], +m[2]]));
  for (const key of ['total','executed','passed']) assert.equal(values[key], expected, `${name}: ${key}`);
  for (const key of ['failed','error','timeout','aborted','notExecuted']) assert.equal(values[key], 0, `${name}: ${key}`);
  return values;
}
const release = counters('p6-lazy-ink-bounds-release.trx',214);
const canonical = counters('p6-lazy-ink-bounds-canonical.trx',214);
const sibling = counters('p6-lazy-ink-bounds-sibling.trx',504);
const prototype = counters('p6-lazy-ink-contributors.trx',12);
const frames = [...text('p6-lazy-ink-contributors.trx').matchAll(/zoom=([\d.]+); grid=(True|False); isolated overlap=(True|False); step=(\d+); changed channels=(\d+); omitted=(\d+)/g)]
  .map(m=>({zoom:+m[1],grid:m[2]==='True',isolatedOverlap:m[3]==='True',step:+m[4],changedChannels:+m[5],minimumOmitted:+m[6]}));
assert.equal(frames.length,96);
assert.ok(frames.every(f=>f.changedChannels===0 && f.minimumOmitted===80));
for(const zoom of [.41,.875,1.75]) for(const grid of [false,true]) for(const isolatedOverlap of [false,true])
  assert.deepEqual(frames.filter(f=>f.zoom===zoom&&f.grid===grid&&f.isolatedOverlap===isolatedOverlap).map(f=>f.step).sort(),[0,1,2,3,4,5,6,7]);
function timing(name) {
  const report=json(name); assert.equal(report.error,null); assert.equal(report.runs.length,2);
  assert.equal(report.environment.visibility,'visible'); assert.deepEqual(report.environment.viewport,[1280,720]); assert.equal(report.environment.pixelRatio,1);
  return report.runs.map(run=>{
    assert.equal(run.objects,1000); assert.equal(run.committedEdits,140); assert.equal(run.samples,120); assert.equal(run.warmup,20);
    assert.equal(run.rawMs.length,120); assert.ok(run.rawMs.every(ms=>Number.isFinite(ms)&&ms>=0));
    const sorted=[...run.rawMs].sort((a,b)=>a-b);
    assert.equal(run.medianMs,sorted[Math.ceil(sorted.length*.5)-1]); assert.equal(run.p95Ms,sorted[Math.ceil(sorted.length*.95)-1]); assert.equal(run.maxMs,sorted.at(-1));
    assert.equal(run.budgetMs,50); assert.equal(run.budgetPassed,run.p95Ms<=50); assert.equal(run.profile,null);
    for(const key of ['x','y']) assert.equal(run.final[key],run.initial[key]);
    for(const key of ['revision','invalidationDispatches','paints']) assert.equal(run.final[key]-run.initial[key],140);
    assert.equal(run.initial.selected,1); assert.equal(run.final.selected,1); assert.equal(run.final.undoCount,100);
    assert.equal(run.initial.objects,1000); assert.equal(run.final.objects,1000);
    return {medianMs:run.medianMs,p95Ms:run.p95Ms,maxMs:run.maxMs,budgetPassed:run.budgetPassed};
  });
}
const eagerTiming=timing('p6-ink-bounds-ordinary-mixed-repeat.json');
const lazyTiming=timing('p6-lazy-ink-bounds-ordinary-mixed-repeat.json');
const seven=bytes('p6-publish-cache-guard-baseline.simpledraw.json');
for(const name of ['p6-lazy-ink-bounds-normal.simpledraw.json','p6-lazy-ink-bounds-offline.simpledraw.json']) assert.ok(seven.equals(bytes(name)),name);
const mixed=bytes('p6-aot-baseline.simpledraw.json'); assert.ok(mixed.equals(bytes('p6-lazy-ink-bounds-mixed.simpledraw.json')));
assert.match(text('p6-lazy-ink-bounds-normal-width4-ui.txt'),/Stroke width 4, Value: 1/);
assert.match(text('p6-lazy-ink-bounds-normal-ui.txt'),/Stroke width 2, Value: 1/);
assert.match(text('p6-lazy-ink-bounds-offline-width4-ui.txt'),/Stroke width 4, Value: 1/);
assert.match(text('p6-lazy-ink-bounds-offline-undo-ui.txt'),/Stroke width 2, Value: 1/);
assert.match(text('p6-lazy-ink-bounds-offline-ui.txt'),/Restored from this device/); assert.match(text('p6-lazy-ink-bounds-offline-ui.txt'),/Objects 7/);
assert.deepEqual(json('p6-lazy-ink-bounds-final-browser-logs.json'),[]);
assert.deepEqual(json('p6-lazy-ink-bounds-normal-dom.json'),{url:'http://127.0.0.1:5241/draw/',diagnosticPanels:0});
const assets=json('p6-lazy-ink-bounds-asset-report.json');
assert.equal(assets.offlineVersion,'f6a3c3533a8708b2'); assert.equal(assets.offlineAssets,134);
assert.deepEqual(assets.totals,{identity:29919195,br:8146854,gzip:10763876});
assert.match(text('p6-lazy-ink-bounds-javascript-tests.log'),/pass 72/); assert.match(text('p6-lazy-ink-bounds-javascript-tests.log'),/fail 0/);
assert.match(text('p6-lazy-ink-bounds-archive.log'),/all 402 ZIP payload entries/);
assert.match(text('p6-lazy-ink-bounds-build.log'),/0 Warning\(s\)/);
assert.match(text('p6-lazy-ink-bounds-net8-host.log'),/Build succeeded\./); assert.match(text('p6-lazy-ink-bounds-net8-host.log'),/0 Warning\(s\)/); assert.match(text('p6-lazy-ink-bounds-net8-host.log'),/0 Error\(s\)/);
const provenance=json('p6-lazy-ink-bounds-provenance.json');
assert.equal(provenance.source.sourceFiles,209); assert.equal(provenance.source.sourceSha256,'5a26224ac9a5b78412e781486e919290fea54da87606f3585c0282c802969b6b');
const result={date:'2026-10-06',source:provenance.source.sourceSha256,release,canonical,sibling,prototype,
  nativePrototype:{frames:frames.length,exactPixels:true,sourceSha256:hash(bytes('p6-lazy-ink-contributors.cs')),scope:'Fixed damage on sketch leaves; full canvas raster and regional byte copy. No automatic history/overlay/viewport damage detector.'},
  native:{sevenBytes:seven.length,sevenSha256:hash(seven),mixedBytes:mixed.length,mixedSha256:hash(mixed),exactDownloads:true},
  eagerTiming,lazyTiming,assets:{offlineVersion:assets.offlineVersion,totals:assets.totals},
  decision:'Ink bounds API adopted as a foundation; automatic damage repaint remains unimplemented; mixed p95 and external acceptance remain open. These timings do not establish causal gain or regression.'};
fs.writeFileSync(new URL('p6-lazy-ink-bounds-summary.json',root),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({prototype:result.nativePrototype,lazyTiming,native:result.native,decision:result.decision}));
