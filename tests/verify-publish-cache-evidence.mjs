import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

// Validate retained observations; this does not drive or simulate a browser.
const root=path.resolve('docs/evidence');
const file=name=>fs.readFileSync(path.join(root,name));
const json=name=>JSON.parse(file(name));
assert.equal(json('p6-aot-cache-regression-setup.json').knownBadCacheReused,true);
assert.ok(json('p6-aot-cached-restore-logs.json').some(entry=>
    entry.level==='error' && entry.message.includes('[MONO] * Assertion: should not be reached')));
const comparison=json('p6-publish-cache-guard-runtime-equivalence.json');
assert.equal(comparison.byteIdentical,true);
assert.equal(comparison.baselineFiles,402);
assert.equal(comparison.guardedFiles,402);
for(const key of ['missing','added','changed']) assert.deepEqual(comparison[key],[]);
assert.equal(Object.keys(comparison.sha256).length,402);
const assets=json('p6-publish-cache-guard-asset-report.json');
assert.equal(assets.offlineVersion,'b8c46ebd9ba44fe5');
assert.equal(assets.offlineAssets,134);
assert.deepEqual(assets.totals,{identity:29913096,br:8144713,gzip:10761219});
const toolchain=json('p6-publish-cache-guard-toolchain.json');
assert.equal(toolchain.runAOTCompilation,'');
assert.equal(toolchain.nativeSkiaVerified,true);
const counters=file('p6-publish-cache-guard-tests.trx').toString().match(/<Counters\b[^>]*>/)?.[0];
assert.ok(counters);
for(const key of ['total','executed','passed']) assert.ok(counters.includes(`${key}="188"`));
assert.ok(counters.includes('failed="0"'));
const js=file('p6-publish-cache-guard-javascript-tests.log').toString();
assert.match(js,/tests 71/); assert.match(js,/pass 71/); assert.match(js,/fail 0/);
assert.match(file('p6-publish-cache-guard-archive.log').toString(),/all 402 ZIP payload entries/);
assert.deepEqual(json('p6-publish-cache-guard-browser-logs.json'),[]);
const edit=file('p6-publish-cache-guard-edit-ui.txt').toString();
assert.match(edit,/Stroke width 4, Value: 1/);
const offline=file('p6-publish-cache-guard-offline-ui.txt').toString();
assert.match(offline,/Objects 7/);
assert.match(offline,/Restored from this device/);
const baseline=file('p6-publish-cache-guard-baseline.simpledraw.json');
assert.ok(baseline.equals(file('p6-publish-cache-guard-roundtrip.simpledraw.json')));
assert.ok(baseline.equals(file('p6-publish-cache-guard-offline.simpledraw.json')));
assert.equal(JSON.parse(baseline).diagram.components.length,7);
const summary={
    decision:'Replayed failing post-AOT cache is corrected by fresh Web publish intermediates.',
    automated:{csharp:188,javascript:71,offlineAssets:134,zipEntries:402},
    allPublishedFilesByteIdentical:true,
    browser:{origin:'http://127.0.0.1:5237/draw/',startup:true,importedObjects:7,
        selectedWidthEditAndUndo:true,serverStoppedReload:true,warningErrorCount:0},
    native:{bytes:baseline.length,sha256:crypto.createHash('sha256').update(baseline).digest('hex'),
        baselineEditUndoOfflineDownloadsByteIdentical:true},
    limitation:'Captured desktop workflow and reproduced cache defect only; mixed p95 and external acceptance remain open.'
};
fs.writeFileSync(path.join(root,'p6-publish-cache-guard-browser-summary.json'),JSON.stringify(summary,null,2)+'\n');
console.log(JSON.stringify(summary));
