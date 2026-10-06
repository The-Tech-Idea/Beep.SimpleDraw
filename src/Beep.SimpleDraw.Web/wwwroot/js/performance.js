// Opt-in local diagnostics. Normal editing does not import this module.
// Reports are rendered in the DOM; no telemetry or external submission.
let output, reference, usable, pending, running = false, results = [], schedulingChecks = [], generation = 0, profiling = false;
const environment = () => ({userAgent:navigator.userAgent,viewport:[innerWidth,innerHeight],pixelRatio:devicePixelRatio,cores:navigator.hardwareConcurrency,visibility:document.visibilityState});
const resources = () => performance.getEntriesByType('resource').map(e => ({url:e.name,initiator:e.initiatorType,transferBytes:e.transferSize,encodedBytes:e.encodedBodySize,durationMs:e.duration}));
function render(error) {
    if (!output) return;
    const navigation=performance.getEntriesByType('navigation')[0];
    output.textContent=JSON.stringify({method:'normal Home editor / shared SKCanvasView; JS command start to second animation frame after its real paint callback',environment:environment(),startup:{usableMs:usable??null,navigationType:navigation?.type,serviceWorkerStartMs:navigation?.workerStart??0,navigationTransferBytes:navigation?.transferSize??0},runs:results,schedulingChecks,resources:resources(),error:error??null},null,2);
}
export function attach(element,dotnet,profile=false) { generation++;output=element;reference=dotnet;usable=undefined;results=[];schedulingChecks=[];profiling=profile;render(); }
export function paint(ready,scenePaintMs=null,scenePhases=null) {
    if (!output) return;
    const sample=pending;
    const instance=generation;
    if (usable!==undefined && (!sample||sample.paintQueued)) return;
    if (sample) {
        if(sample.remainingPaints&&--sample.remainingPaints>0) return;
        if(profiling&&(!Number.isFinite(scenePaintMs)||scenePaintMs<0)) {
            clearTimeout(sample.timer);pending=null;sample.reject(new Error('The profiled paint did not supply a valid shared scene CPU duration.'));return;
        }
        if(profiling) {
            try { verifyScenePhases(scenePhases,scenePaintMs); }
            catch(error) { clearTimeout(sample.timer);pending=null;sample.reject(error);return; }
        }
        sample.scenePaintMs=scenePaintMs;sample.scenePhases=scenePhases;sample.paintQueued=true;
    }
    requestAnimationFrame(()=>requestAnimationFrame(()=>{
        if (!output||instance!==generation) return;
        if (ready&&usable===undefined) { usable=performance.now();render(); }
        if (sample && pending===sample) { clearTimeout(sample.timer);pending=null;sample.resolve(performance.now()-sample.start); }
    }));
}
export function verifyScenePhases(phases,cpuMs,objects=null) {
    const names=['totalMs','preparationMs','gridMs','connectionsMs','adornersMs','staticMs'];
    if(!phases||names.some(key=>!Number.isFinite(phases[key])||phases[key]<0)
        ||phases.totalMs>cpuMs+.01||!phases.groups||typeof phases.groups!=='object'||Array.isArray(phases.groups))
        throw new Error('The profiled paint did not supply valid shared scene phase evidence.');
    const groups=Object.values(phases.groups);
    if(groups.some(group=>!group||!Number.isSafeInteger(group.attempts)||group.attempts<0
        ||!Number.isSafeInteger(group.skippedAttempts)||group.skippedAttempts<0||group.attempts+group.skippedAttempts<=0
        ||!Number.isFinite(group.milliseconds)||group.milliseconds<0)
        ||(objects!==null&&groups.reduce((sum,group)=>sum+group.attempts+group.skippedAttempts,0)!==objects))
        throw new Error('The profiled scene draw attempts do not match the measured board.');
    const accounted=names.slice(1).reduce((sum,key)=>sum+phases[key],0)+groups.reduce((sum,group)=>sum+group.milliseconds,0);
    if(accounted>phases.totalMs+.01) throw new Error('The profiled CPU phase scopes exceed the scene duration.');
}
export function summarize(samples) {
    if (!samples.length || samples.some(v=>!Number.isFinite(v)||v<0)) throw new Error('A performance result requires valid executed samples.');
    const sorted=[...samples].sort((a,b)=>a-b);
    return {samples:samples.length,medianMs:sorted[Math.ceil(sorted.length*.5)-1],p95Ms:sorted[Math.ceil(sorted.length*.95)-1],maxMs:sorted.at(-1),rawMs:samples};
}
export function verifyEdits(initial,final,count) {
    const expected=initial.maxHistoryDepth>0?Math.min(initial.maxHistoryDepth,initial.undoCount+count):initial.undoCount+count;
    if(final.objects!==initial.objects||final.selected!==1||final.x!==initial.x||final.y!==initial.y||final.revision-initial.revision!==count||final.undoCount!==expected)
        throw new Error('Measured edits did not execute/restore the expected bounded history and document revision path.');
}
export async function runEdits() {
    if (running) throw new Error('A benchmark is already running.');
    if (document.visibilityState!=='visible') throw new Error('Keep this benchmark tab visible.');
    running=true;
    const samples=[], commands=[], scenePaints=[], phaseFrames=[];
    try {
        const initial=await reference.invokeMethodAsync('BenchmarkState');
        if (initial.selected!==1) throw new Error('Select exactly one ungrouped object before running.');
        for(let i=0;i<140;i++) {
            if(document.visibilityState!=='visible') throw new Error('Benchmark tab became hidden.');
            await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
            let measured;
            const elapsed=new Promise((resolve,reject)=>{
                measured=pending={start:performance.now(),resolve,reject,paintQueued:false};
                pending.timer=setTimeout(()=>{pending=null;reject(new Error('No real canvas paint completed within 10 seconds.'));},10000);
            });
            const command=reference.invokeMethodAsync('BenchmarkEdit',i%2===0);
            const [commandMs,latency]=await Promise.all([profiling?command.then(()=>performance.now()-measured.start):command,elapsed]);
            if(profiling) verifyScenePhases(measured.scenePhases,measured.scenePaintMs,initial.objects);
            if(i>=20) {samples.push(latency);if(profiling){commands.push(commandMs);scenePaints.push(measured.scenePaintMs);phaseFrames.push(measured.scenePhases);}}
        }
        const final=await reference.invokeMethodAsync('BenchmarkState');
        verifyEdits(initial,final,140);
        const summary=summarize(samples);
        results.push({objects:initial.objects,warmup:20,edit:'alternating ArrowRight/ArrowLeft through Home.Shortcut; one selected object',committedEdits:140,initial,final,...summary,budgetMs:50,budgetPassed:summary.p95Ms<=50,
            profile:profiling?{command:summarize(commands),scenePaint:summarize(scenePaints),scenePhases:{rawFrames:phaseFrames,
                scope:'Disjoint CPU preparation, grid, component draw attempts grouped by sketch kind/type, connections, adorners and static scopes. Total includes unassigned transitions and probe overhead. Counts are attempts, not visible raster output.'},
                scope:'Command invocation completion and CPU clear plus DrawingManager.Draw before overlay. Durations may overlap; do not add them. Neither is a GPU presentation timestamp. Phase profiling adds instrumentation overhead; use ordinary runs for acceptance.'}:null});
        render();
    } catch(error) { if(pending) {clearTimeout(pending.timer);pending=null;} render(error.message);throw error; }
    finally {running=false;}
}
export async function runSchedulingCheck() {
    if(running||!output||!reference) throw new Error('A ready idle diagnostic editor is required.');
    if(document.visibilityState!=='visible') throw new Error('Keep the diagnostic tab visible.');
    running=true;
    try {
        await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
        const initial=await reference.invokeMethodAsync('BenchmarkState');
        const elapsed=new Promise((resolve,reject)=>{
            pending={start:performance.now(),resolve,reject,paintQueued:false,remainingPaints:2};
            pending.timer=setTimeout(()=>{pending=null;reject(new Error('The paint requested during painting did not complete.'));},10000);
        });
        const [,latency]=await Promise.all([reference.invokeMethodAsync('RequestRepaintDuringPaint'),elapsed]);
        const final=await reference.invokeMethodAsync('BenchmarkState');
        if(final.invalidationDispatches-initial.invalidationDispatches!==2||final.paints-initial.paints!==2
            ||['objects','selected','x','y','revision','undoCount'].some(key=>final[key]!==initial[key]))
            throw new Error('The two coalesced paint requests or unchanged editor state were not verified.');
        schedulingChecks.push({initial,final,presentationMs:latency,passed:true,
            scope:'Three initial invalidations and three during painting; exactly two host dispatches and completed paints. Host dispatches exclude vendor resize/DPI requests.'});
        render();
    } catch(error) {if(pending){clearTimeout(pending.timer);pending=null;}render(error.message);throw error;}
    finally {running=false;}
}
export function detach() {generation++;if(pending){clearTimeout(pending.timer);pending.reject(new Error('Benchmark disposed.'));pending=null;}output=null;reference=null;}
