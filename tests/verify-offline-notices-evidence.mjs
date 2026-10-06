import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';

// Validate retained build/UI/file observations; this does not execute a browser.
const root=new URL('../docs/evidence/',import.meta.url);
const bytes=name=>fs.readFileSync(new URL(name,root));
const text=name=>bytes(name).toString();
const json=name=>JSON.parse(text(name));
const current=suffix=>'p6-offline-notices-'+suffix;
const hash=value=>crypto.createHash('sha256').update(value).digest('hex');
const trx=text(current('release.trx'));
const tag=trx.match(/<Counters\b[^>]*>/)?.[0];assert.ok(tag);
const release=Object.fromEntries([...tag.matchAll(/(\w+)="(\d+)"/g)].map(m=>[m[1],+m[2]]));
for(const key of ['total','executed','passed']) assert.equal(release[key],289);
for(const key of ['failed','error','timeout','aborted','notExecuted']) assert.equal(release[key],0);
assert.equal([...trx.matchAll(/exact pixels=True; footprint contained=True/g)].length,528);
assert.match(text(current('javascript-tests.log')),/pass 77/);assert.match(text(current('javascript-tests.log')),/fail 0/);
assert.match(text(current('build.log')),/Build succeeded/);
assert.match(text(current('build.log')),/0 Warning\(s\)/);assert.match(text(current('build.log')),/0 Error\(s\)/);
assert.match(text(current('archive.log')),/all 405 ZIP payload entries/);
const assets=json(current('asset-report.json')),provenance=json(current('provenance.json'));
assert.equal(assets.offlineAssets,135);assert.equal(assets.offlineVersion,'ea9b4cfb39543ad5');
assert.deepEqual(assets.totals,{identity:29937314,br:8152713,gzip:10771371});
assert.deepEqual(provenance.source,json('p6-session-overlays-provenance.json').source);
assert.equal(provenance.source.sourceSha256,'4f280f9375360e373833f1a97ccb98cf5159f484b767444dba20610c200d7993');
const runtime=json(current('runtime-notices.json')),oldRuntime=json('p6-session-overlays-runtime-notices.json');
assert.equal(runtime.payloads.length,101);assert.deepEqual(runtime.payloads,oldRuntime.payloads);
assert.equal(runtime.bundle.groups,45);assert.equal(runtime.bundle.originalTexts,21);assert.equal(runtime.bundle.files,24);
const equivalence=json(current('payload-equivalence.json'));
assert.equal(equivalence.previousFiles,402);assert.equal(equivalence.currentFiles,405);assert.equal(equivalence.unchanged,393);
const expectedChanges=['js/notices.js','licenses/index.html','service-worker-assets.js','service-worker.js']
    .flatMap(name=>[name,name+'.br',name+'.gz']).sort();
assert.deepEqual(equivalence.changes.map(c=>c.path).sort(),expectedChanges);
const originals=json(current('originals.json'));assert.equal(originals.unchangedFiles,23);
const noticeFiles=['online','offline','offline-reload'].map(mode=>{
    const data=bytes(current(mode+'.txt')),record=json(current(mode+'-file.json'));
    assert.equal(data.length,523698);assert.equal(hash(data),'f129e7d9309501566e8ec45c9bf215feda8c926defa3235c1be41032c600e851');
    assert.ok(data.equals(bytes('p6-current-notices-online.txt')));
    assert.equal(record.bytes,data.length);assert.equal(record.sha256,hash(data));assert.equal(record.exactPublishedBytes,true);
    assert.equal(record.downloadEventObserved,false);assert.ok(Number.isFinite(record.lastWriteTimeUnix));
    return {mode,...record};
});
for(const [suffix,index] of [['server-stopped.json',1],['reload-server-stopped.json',2]]) {
    const stopped=json(current(suffix));assert.equal(stopped.port,5243);assert.equal(stopped.activeListeners,0);
    assert.ok(noticeFiles[index].lastWriteTimeUnix*1000>Date.parse(stopped.checkedAt));
}
for(const suffix of ['online-direct.ax.txt','offline.ax.txt','offline-reload.ax.txt']) {
    const state=text(current(suffix));assert.match(state,/button Download all software notices/);
    assert.match(state,/Notices download requested/);assert.match(state,/Read all software notices/);
    assert.equal([...state.matchAll(/heading .+, Value: 2/g)].length,45);
}
assert.equal(json(current('offline-download.json')).status,'not-observed');
assert.equal(json(current('online-download.json')).status,'not-observed');
assert.deepEqual(json(current('browser-logs.json')),[]);assert.deepEqual(json(current('editor-logs.json')),[]);
const navigation=json(current('help-navigation.json'));
assert.ok(navigation.some(t=>t.title==='SimpleDraw — software notices'&&t.url==='http://127.0.0.1:5243/draw/licenses/index.html'));
assert.match(text(current('update-ready.ax.txt')),/button Update app/);
assert.match(text(current('update-blocked.ax.txt')),/Close other editor tabs before updating/);
for(const suffix of ['update-restored.ax.txt','editor-offline.ax.txt']) {
    assert.match(text(current(suffix)),/Restored from this device/);assert.match(text(current(suffix)),/Objects 7/);
    assert.doesNotMatch(text(current(suffix)),/button Update app/);
}
assert.match(text(current('normal-width4.ax.txt')),/Stroke width 4, Value: 1/);
assert.match(text(current('normal-undo.ax.txt')),/Stroke width 2, Value: 1/);
const seven=bytes('p6-publish-cache-guard-baseline.simpledraw.json');
for(const suffix of ['normal.simpledraw.json','editor-offline.simpledraw.json']) assert.ok(seven.equals(bytes(current(suffix))));
const summary={date:'2026-10-06',source:provenance.source,release,javascriptTests:77,exactOverlayFrames:528,
    assets:{offlineVersion:assets.offlineVersion,totals:assets.totals,offlineAssets:135,zipEntries:405},
    notices:{files:noticeFiles,originalFilesUnchanged:23,groups:45,originalTexts:21,
        completionEvidence:'Actual fresh files retained after UI clicks; download-event observers timed out.',
        browserScope:'Directly controlled IAB tab online, fresh offline navigation and stopped-server reload. Help opened a real popup; popup button reported a request but its completion remains unverified.'},
    unchangedEditor:{runtimePayloads:101,unchangedPayloadFiles:393,nativeBytes:seven.length,nativeSha256:hash(seven),
        priorMixedP95Ms:[30.5,30.2],newBenchmarkRun:false,scope:'Prior presentation evidence applies to byte-identical editor/runtime/stylesheet/bridge payloads; no fresh timing or new engine/device certification claimed.'},
    update:{anotherClientRefusal:true,activationAndExactNativeRestoration:true},
    decision:'Cached byte retrieval and shared download helper close the directly controlled IAB online/offline notice file gate. Popup download completion and independent rights/header/guide, devices, intended host/provider/cold-process, hosted CI/install and acceptance remain open.'};
fs.writeFileSync(new URL(current('summary.json'),root),JSON.stringify(summary,null,2)+'\n');
console.log(JSON.stringify({release:release.passed,javascriptTests:77,noticeSha256:noticeFiles[0].sha256,
    onlineOfflineExact:true,unchangedRuntimePayloads:101,offlineVersion:assets.offlineVersion}));
