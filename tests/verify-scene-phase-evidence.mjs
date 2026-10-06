import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

// Independent validation of retained observations; does not simulate browser execution.
const root=path.resolve('docs/evidence');
const bytes=name=>fs.readFileSync(path.join(root,name));
const json=name=>JSON.parse(bytes(name));
const percentile=(values,p)=>[...values].sort((a,b)=>a-b)[Math.ceil(values.length*p)-1];
const summary=values=>({medianMs:percentile(values,.5),p95Ms:percentile(values,.95),maxMs:Math.max(...values)});
const baseline=bytes('p6-aot-baseline.simpledraw.json');
const expectedGroups={};
for(const component of JSON.parse(baseline).diagram.components) {
    const key='Sketch.'+component.propertyBag.Kind;
    expectedGroups[key]=(expectedGroups[key]??0)+1;
}
function verify(report,profiled) {
    assert.equal(report.error,null); assert.equal(report.runs.length,2);
    assert.equal(report.environment.visibility,'visible');
    assert.deepEqual(report.environment.viewport,[1280,720]);
    assert.equal(report.environment.pixelRatio,1);
    return report.runs.map(run=>{
        assert.equal(run.objects,1000); assert.equal(run.committedEdits,140);
        assert.equal(run.warmup,20); assert.equal(run.rawMs.length,120);
        assert.ok(run.rawMs.every(ms=>Number.isFinite(ms)&&ms>=0));
        assert.equal(run.p95Ms,percentile(run.rawMs,.95));
        assert.equal(run.budgetMs,50); assert.equal(run.budgetPassed,run.p95Ms<=50);
        assert.equal(run.final.revision-run.initial.revision,140);
        assert.equal(run.final.invalidationDispatches-run.initial.invalidationDispatches,140);
        assert.equal(run.final.paints-run.initial.paints,140);
        assert.equal(run.initial.selected,1); assert.equal(run.final.selected,1);
        assert.equal(run.final.x,run.initial.x); assert.equal(run.final.y,run.initial.y);
        assert.equal(run.initial.objects,1000); assert.equal(run.final.objects,1000);
        assert.equal(run.final.undoCount,100);
        if(!profiled) { assert.equal(run.profile,null); return summary(run.rawMs); }
        const frames=run.profile.scenePhases.rawFrames;
        assert.equal(frames.length,120); assert.equal(run.profile.scenePaint.rawMs.length,120);
        const phaseNames=['preparationMs','gridMs','connectionsMs','adornersMs','staticMs'];
        for(let i=0;i<frames.length;i++) {
            const frame=frames[i];
            for(const key of ['totalMs',...phaseNames]) assert.ok(Number.isFinite(frame[key])&&frame[key]>=0);
            assert.ok(frame.totalMs<=run.profile.scenePaint.rawMs[i]+.01);
            assert.deepEqual(Object.fromEntries(Object.entries(frame.groups).map(([key,g])=>[key,g.attempts])),expectedGroups);
            assert.ok(Object.values(frame.groups).every(g=>Number.isFinite(g.milliseconds)&&g.milliseconds>=0));
            const accounted=phaseNames.reduce((sum,key)=>sum+frame[key],0)+Object.values(frame.groups).reduce((sum,g)=>sum+g.milliseconds,0);
            assert.ok(accounted<=frame.totalMs+.01);
        }
        const groups=Object.fromEntries(Object.keys(expectedGroups).map(key=>[key,
            {attemptsPerFrame:expectedGroups[key],...summary(frames.map(frame=>frame.groups[key].milliseconds))}]));
        assert.ok(Object.entries(groups).every(([key,g])=>key==='Sketch.Text'||g.medianMs<groups['Sketch.Text'].medianMs));
        return {presentation:summary(run.rawMs),scene:summary(run.profile.scenePaint.rawMs),
            preparation:summary(frames.map(frame=>frame.preparationMs)),groups};
    });
}
const profiled=verify(json('p6-phase-profile-1000-mixed-repeat.json'),true);
const ordinary=verify(json('p6-phase-profile-ordinary-1000-mixed-repeat.json'),false);
const control=verify(json('p6-phase-profile-control-1000-mixed-repeat.json'),false);
for(const name of ['p6-phase-profile-roundtrip.simpledraw.json','p6-phase-profile-ordinary-roundtrip.simpledraw.json']) assert.ok(baseline.equals(bytes(name)));
const seven=bytes('p6-publish-cache-guard-baseline.simpledraw.json');
for(const name of ['p6-phase-profile-normal-roundtrip.simpledraw.json','p6-phase-profile-offline.simpledraw.json']) assert.ok(seven.equals(bytes(name)));
assert.deepEqual(json('p6-phase-profile-browser-logs.json'),[]);
assert.deepEqual(json('p6-phase-profile-control-browser-logs.json'),[]);
assert.deepEqual(json('p6-phase-profile-normal-dom.json'),{url:'http://127.0.0.1:5238/draw/',diagnosticPanels:0});
assert.match(bytes('p6-phase-profile-offline-ui.txt').toString(),/Restored from this device/);
assert.match(bytes('p6-phase-profile-offline-ui.txt').toString(),/Objects 7/);
assert.match(bytes('p6-phase-profile-normal-edit-ui.txt').toString(),/Stroke width 4, Value: 1/);
const assets=json('p6-phase-profile-asset-report.json');
assert.equal(assets.offlineVersion,'cdf98b860103a5e6'); assert.equal(assets.offlineAssets,134);
assert.deepEqual(assets.totals,{identity:29918043,br:8145810,gzip:10763262});
for(const [name,count] of [['p6-phase-profile-tests.trx',191],['p6-phase-profile-sibling-tests.trx',504]]) {
    const counters=bytes(name).toString().match(/<Counters\b[^>]*>/)?.[0]; assert.ok(counters);
    for(const key of ['total','executed','passed']) assert.ok(counters.includes(`${key}="${count}"`));
    assert.ok(counters.includes('failed="0"'));
}
assert.match(bytes('p6-phase-profile-javascript-tests.log').toString(),/pass 72/);
assert.match(bytes('p6-phase-profile-archive.log').toString(),/all 402 ZIP payload entries/);
assert.match(bytes('p6-phase-profile-net8-host.log').toString(),/Build succeeded\./);
assert.match(bytes('p6-phase-profile-net8-host.log').toString(),/0 Warning\(s\)/);
assert.match(bytes('p6-phase-profile-net8-host.log').toString(),/0 Error\(s\)/);
const result={profiled,ordinary,unchangedControl:control,
    finding:'Text is the largest attempted component CPU paint group in both instrumented runs; text replay/raster work is the next investigation target.',
    native:{thousandBytes:baseline.length,thousandSha256:crypto.createHash('sha256').update(baseline).digest('hex'),
        sevenBytes:seven.length,sevenSha256:crypto.createHash('sha256').update(seven).digest('hex'),exactDownloads:true},
    scope:'Desktop IAB Chromium; profiled measurements include probe overhead. Ordinary/control timings do not establish a causal performance improvement or regression. Mixed p95 and external acceptance remain open.'};
fs.writeFileSync(path.join(root,'p6-phase-profile-browser-summary.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({finding:result.finding,profiled:profiled.map(r=>({scene:r.scene,text:r.groups['Sketch.Text']})),ordinary,control}));
