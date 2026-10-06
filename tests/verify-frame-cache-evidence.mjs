import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {verifyScenePhases} from '../src/Beep.SimpleDraw.Web/wwwroot/js/performance.js';

// Validate captured observations; this does not execute tests or browser interactions.
const root = new URL('../docs/evidence/',import.meta.url);
const bytes = name => fs.readFileSync(new URL(name,root));
const text = name => bytes(name).toString();
const json = name => JSON.parse(text(name));
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
function counters(name,total) {
    const tag=text(name).match(/<Counters\b[^>]*>/)?.[0]; assert.ok(tag,name);
    const values=Object.fromEntries([...tag.matchAll(/(\w+)="(\d+)"/g)].map(m=>[m[1],+m[2]]));
    for(const key of ['total','executed','passed']) assert.equal(values[key],total,name);
    for(const key of ['failed','error','timeout','aborted','notExecuted']) assert.equal(values[key],0,name);
    return values;
}
const release=counters('p6-frame-cache-release.trx',241);
const canonical=counters('p6-frame-cache-canonical.trx',241);
const sibling=counters('p6-frame-cache-sibling.trx',504);
const report=json('p6-frame-cache-ordinary-mixed-repeat.json');
assert.equal(report.error,null); assert.equal(report.runs.length,2);
assert.equal(report.environment.visibility,'visible'); assert.deepEqual(report.environment.viewport,[1280,720]);
assert.equal(report.environment.pixelRatio,1);
const timings=report.runs.map(run=>{
    assert.equal(run.objects,1000); assert.equal(run.committedEdits,140); assert.equal(run.warmup,20);
    assert.equal(run.samples,120); assert.equal(run.rawMs.length,120); assert.equal(run.profile,null);
    assert.ok(run.rawMs.every(ms=>Number.isFinite(ms)&&ms>=0));
    const sorted=[...run.rawMs].sort((a,b)=>a-b);
    assert.equal(run.medianMs,sorted[59]); assert.equal(run.p95Ms,sorted[113]); assert.equal(run.maxMs,sorted.at(-1));
    assert.equal(run.budgetMs,50); assert.equal(run.budgetPassed,true); assert.ok(run.p95Ms<=50);
    for(const key of ['x','y','selected','objects']) assert.equal(run.final[key],run.initial[key]);
    for(const key of ['revision','invalidationDispatches','paints']) assert.equal(run.final[key]-run.initial[key],140);
    assert.equal(run.final.selected,1); assert.equal(run.final.undoCount,100); assert.equal(run.final.maxHistoryDepth,100);
    return {medianMs:run.medianMs,p95Ms:run.p95Ms,maxMs:run.maxMs,budgetPassed:run.budgetPassed};
});
assert.deepEqual(json('p6-frame-cache-ordinary-mixed.json').runs[0],report.runs[0]);
const scheduling=json('p6-frame-cache-scheduling.json').schedulingChecks;
assert.equal(scheduling.length,1); assert.equal(scheduling[0].passed,true);
for(const key of ['paints','invalidationDispatches']) assert.equal(scheduling[0].final[key]-scheduling[0].initial[key],2);
for(const key of ['revision','undoCount','objects','selected','x','y']) assert.equal(scheduling[0].final[key],scheduling[0].initial[key]);
const profiled=json('p6-frame-cache-profile.json'); assert.equal(profiled.error,null); assert.equal(profiled.runs.length,1);
const probe=profiled.runs[0].profile; assert.equal(probe.scenePhases.rawFrames.length,120);
const contributorCounts=probe.scenePhases.rawFrames.map((frame,i)=>{
    verifyScenePhases(frame,probe.scenePaint.rawMs[i],1000);
    const groups=Object.values(frame.groups),attempts=groups.reduce((n,g)=>n+g.attempts,0),skips=groups.reduce((n,g)=>n+g.skippedAttempts,0);
    assert.ok(attempts>0&&attempts<1000&&skips>0); return {attempts,skips};
});
const seven=bytes('p6-publish-cache-guard-baseline.simpledraw.json');
for(const name of ['p6-frame-cache-normal.simpledraw.json','p6-frame-cache-offline.simpledraw.json']) assert.ok(seven.equals(bytes(name)),name);
const mixed=bytes('p6-aot-baseline.simpledraw.json'); assert.ok(mixed.equals(bytes('p6-frame-cache-mixed.simpledraw.json')));
for(const mode of ['normal','offline']) {
    assert.match(text(`p6-frame-cache-${mode}-width4.ax.txt`),/Stroke width 4, Value: 1/);
    assert.match(text(`p6-frame-cache-${mode}-undo.ax.txt`),/Stroke width 2, Value: 1/);
}
assert.match(text('p6-frame-cache-offline-restored.ax.txt'),/Restored from this device/);
assert.match(text('p6-frame-cache-offline-restored.ax.txt'),/Objects 7/);
assert.match(text('p6-frame-cache-keyboard-created.ax.txt'),/Objects 8/);
assert.match(text('p6-frame-cache-keyboard-undone.ax.txt'),/Objects 7/);
assert.deepEqual(json('p6-frame-cache-browser-logs.json'),[]);
assert.deepEqual(json('p6-frame-cache-normal-dom.json'),{url:'http://127.0.0.1:5242/draw/',diagnosticPanels:0});
const assets=json('p6-frame-cache-asset-report.json'),provenance=json('p6-frame-cache-provenance.json');
assert.equal(assets.offlineVersion,'99a7f9824c40819e'); assert.equal(assets.offlineAssets,134);
assert.deepEqual(assets.totals,{identity:29935561,br:8152461,gzip:10770663});
assert.equal(provenance.source.sourceFiles,210);
assert.equal(provenance.source.sourceSha256,'4f280f9375360e373833f1a97ccb98cf5159f484b767444dba20610c200d7993');
assert.match(text('p6-frame-cache-javascript-tests.log'),/pass 72/); assert.match(text('p6-frame-cache-javascript-tests.log'),/fail 0/);
assert.match(text('p6-frame-cache-archive.log'),/all 402 ZIP payload entries/);
for(const name of ['build.log','net8-host.log']) {
    assert.match(text(`p6-frame-cache-${name}`),/Build succeeded/);
    assert.match(text(`p6-frame-cache-${name}`),/0 Warning\(s\)/); assert.match(text(`p6-frame-cache-${name}`),/0 Error\(s\)/);
}
const summary={date:'2026-10-06',source:provenance.source,release,canonical,sibling,timings,
    profiling:{sceneMedianMs:probe.scenePaint.medianMs,sceneP95Ms:probe.scenePaint.p95Ms,
        minimumDrawAttempts:Math.min(...contributorCounts.map(c=>c.attempts)),maximumDrawAttempts:Math.max(...contributorCounts.map(c=>c.attempts)),
        minimumSkips:Math.min(...contributorCounts.map(c=>c.skips)),scope:'Optional CPU clear/scene scopes; excludes overlay/copy and is separate from presentation acceptance.'},
    native:{sevenBytes:seven.length,sevenSha256:hash(seven),mixedBytes:mixed.length,mixedSha256:hash(mixed),exactDownloads:true},
    assets:{offlineVersion:assets.offlineVersion,totals:assets.totals},scheduling,
    decision:'Automatic cache adopted through existing renderer/host; exact frame checks and repeated desktop mixed p95 pass. External device, startup/provider/public-host/CI and independent acceptance remain open.'};
fs.writeFileSync(new URL('p6-frame-cache-summary.json',root),JSON.stringify(summary,null,2)+'\n');
console.log(JSON.stringify({source:summary.source.sourceSha256,timings,profiling:summary.profiling,native:summary.native}));
