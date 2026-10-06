import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';

const evidence=path.resolve('docs/evidence');
const read=name=>JSON.parse(fs.readFileSync(path.join(evidence,name),'utf8'));
const baseline=read('p6-aot-baseline-performance.json');
const candidate=read('p6-aot-1000-mixed-repeat.json');
function verify(report,expectedRuns) {
    assert.equal(report.error,null);
    assert.equal(report.environment.visibility,'visible');
    assert.deepEqual(report.environment.viewport,[1280,720]);
    assert.equal(report.environment.pixelRatio,1);
    assert.equal(report.runs.length,expectedRuns);
    return report.runs.map(run=>{
        assert.equal(run.objects,1000);
        assert.equal(run.committedEdits,140);
        assert.equal(run.warmup,20);
        assert.equal(run.rawMs.length,120);
        assert.ok(run.rawMs.every(n=>Number.isFinite(n)&&n>=0));
        const sorted=[...run.rawMs].sort((a,b)=>a-b);
        const p95=sorted[Math.ceil(.95*sorted.length)-1];
        assert.equal(run.p95Ms,p95);
        assert.equal(run.budgetMs,50);
        assert.equal(run.budgetPassed,p95<=50);
        assert.equal(run.initial.selected,1);
        assert.equal(run.final.selected,1);
        assert.equal(run.initial.objects,1000);
        assert.equal(run.final.objects,1000);
        assert.equal(run.final.x,run.initial.x);
        assert.equal(run.final.y,run.initial.y);
        assert.equal(run.final.revision-run.initial.revision,140);
        assert.equal(run.final.invalidationDispatches-run.initial.invalidationDispatches,140);
        assert.equal(run.final.paints-run.initial.paints,140);
        assert.equal(run.final.undoCount,100);
        return {medianMs:run.medianMs,p95Ms:p95,maxMs:run.maxMs,budgetPassed:run.budgetPassed};
    });
}
const before=verify(baseline,1), after=verify(candidate,2);
assert.ok(after.every(run=>!run.budgetPassed),'This rejection evidence requires both observed failures.');
const original=fs.readFileSync(path.join(evidence,'p6-aot-baseline.simpledraw.json'));
const roundtrip=fs.readFileSync(path.join(evidence,'p6-aot-roundtrip.simpledraw.json'));
assert.ok(original.equals(roundtrip));
assert.equal(JSON.parse(roundtrip).diagram.components.length,1000);
const assets=read('p6-aot-asset-report.json');
assert.equal(assets.offlineVersion,'7e72b91b309ca7b5');
assert.equal(assets.totals.br,13945104);
const summary={decision:'AOT rejected and its production flag/package reverted: both measured p95 runs exceed 50 ms.',
    baseline:before,aot:after,nativeRoundtrip:{byteIdentical:true,bytes:original.length,
        sha256:crypto.createHash('sha256').update(original).digest('hex')},
    candidate:{offlineVersion:assets.offlineVersion,brotliBytes:assets.totals.br},
    limitations:['Desktop IAB Chromium only; no physical-device or other-engine claim.',
        'Local navigation startup is not a shaped-network or cold-process measurement.',
        'AOT/native correctness and green build gates do not establish editing performance.']};
fs.writeFileSync(path.join(evidence,'p6-aot-browser-summary.json'),JSON.stringify(summary,null,2)+'\n');
console.log(JSON.stringify(summary));
