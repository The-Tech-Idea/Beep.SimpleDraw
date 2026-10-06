import {test} from 'node:test';
import assert from 'node:assert/strict';
import {summarize,verifyEdits,verifyScenePhases,attach,paint,runEdits,runSchedulingCheck,detach} from '../src/Beep.SimpleDraw.Web/wwwroot/js/performance.js';
import {fixture} from '../scripts/performance-fixtures.mjs';
test('timing summaries require executed finite samples and use nearest-rank p95',()=>{
    const samples=Array.from({length:120},(_,i)=>120-i);
    assert.equal(summarize(samples).p95Ms,114);assert.equal(summarize(samples).medianMs,60);
    for(const values of [[],[NaN],[Infinity],[-1]]) assert.throws(()=>summarize(values));
});
test('fixture identities/groups are independent and 5000 remains an explicit refusal case',()=>{
    for(const count of [100,1000,2000,5000]) {
        const f=fixture(count);assert.equal(f.diagram.components.length,count);
        assert.equal(new Set(f.diagram.components.map(c=>c.id)).size,count);
        assert.ok(f.diagram.components.every(c=>c.propertyBag.GroupId==='00000000-0000-0000-0000-000000000000'));
    }
    assert.equal(new Set(fixture(100,true).diagram.components.map(c=>c.propertyBag.Kind)).size,7);
    assert.throws(()=>fixture(2001));
});
test('an executed edit run respects retained history capacity and refuses missing edits/geometry drift',()=>{
    const before={objects:1000,selected:1,x:30,y:30,undoCount:0,maxHistoryDepth:100,revision:1};
    const after={...before,undoCount:100,revision:141};
    verifyEdits(before,after,140);
    for(const bad of [{...after,revision:140},{...after,undoCount:0},{...after,x:31},{...after,selected:0},{...after,objects:999}]) assert.throws(()=>verifyEdits(before,bad,140));
    assert.throws(()=>verifyEdits({...before,maxHistoryDepth:0},after,140));
    verifyEdits({...before,maxHistoryDepth:0},{...after,undoCount:140},140);
});
test('queued presentation callbacks from a disposed editor cannot report a new editor startup',()=>{
    const saved={navigator:globalThis.navigator,performance:globalThis.performance};
    const frames=[];
    Object.defineProperty(globalThis,'navigator',{value:{userAgent:'harness',hardwareConcurrency:1},configurable:true});
    globalThis.performance={now:()=>321,getEntriesByType:()=>[]};globalThis.innerWidth=800;globalThis.innerHeight=600;globalThis.devicePixelRatio=1;
    globalThis.document={visibilityState:'visible'};globalThis.requestAnimationFrame=callback=>frames.push(callback);
    try {
        const old={textContent:''},next={textContent:''};attach(old,{});paint(true);detach();attach(next,{});
        while(frames.length) frames.shift()();
        assert.equal(JSON.parse(next.textContent).startup.usableMs,null);
        paint(true);while(frames.length) frames.shift()();
        assert.equal(JSON.parse(next.textContent).startup.usableMs,321);detach();
    } finally {
        Object.defineProperty(globalThis,'navigator',{value:saved.navigator,configurable:true});globalThis.performance=saved.performance;
        for(const key of ['innerWidth','innerHeight','devicePixelRatio','document','requestAnimationFrame']) delete globalThis[key];
    }
});

function editHarness(t,profile,cpuMilliseconds,schedule) {
    const values={navigator:{userAgent:'test',hardwareConcurrency:1},document:{visibilityState:'visible'},innerWidth:1280,innerHeight:720,devicePixelRatio:1,
        requestAnimationFrame:callback=>queueMicrotask(callback)};
    const saved=new Map(Object.keys(values).map(key=>[key,Object.getOwnPropertyDescriptor(globalThis,key)]));
    for(const [key,value] of Object.entries(values)) Object.defineProperty(globalThis,key,{value,configurable:true,writable:true});
    t.after(()=>{detach();for(const [key,descriptor] of saved) {if(descriptor) Object.defineProperty(globalThis,key,descriptor);else delete globalThis[key];}});
    const output={textContent:''},state={objects:1000,selected:1,x:30,y:30,revision:0,undoCount:0,maxHistoryDepth:100,invalidationDispatches:0,paints:0};
    attach(output,{async invokeMethodAsync(method,right) {
        if(method==='BenchmarkState') return {...state};
        if(method==='RequestRepaintDuringPaint') return schedule(state,output);
        assert.equal(method,'BenchmarkEdit');state.x+=right?1:-1;state.revision++;state.undoCount=Math.min(100,state.undoCount+1);
        paint(true,cpuMilliseconds,cpuMilliseconds===null?null:{totalMs:10,preparationMs:1,gridMs:0,connectionsMs:0,adornersMs:1,staticMs:0,
            groups:{'Sketch.Text':{attempts:1000,milliseconds:7,skippedAttempts:0}}});
    }},profile);
    return {output,state};
}
for(const profile of [false,true]) test(`ordinary edit driver retains presentation samples with profiling ${profile}`,async t=>{
    const h=editHarness(t,profile,profile?12.5:null);await runEdits();
    const report=JSON.parse(h.output.textContent),run=report.runs[0];
    assert.equal(report.error,null);assert.equal(run.samples,120);assert.equal(h.state.revision,140);assert.equal(h.state.x,30);
    if(profile) {
        assert.equal(run.profile.scenePaint.samples,120);assert.equal(run.profile.scenePaint.p95Ms,12.5);
        assert.equal(run.profile.command.samples,120);assert.ok(run.profile.command.rawMs.every(v=>Number.isFinite(v)&&v>=0));
        assert.equal(run.profile.scenePhases.rawFrames.length,120);
        assert.equal(run.profile.scenePhases.rawFrames[0].groups['Sketch.Text'].attempts,1000);
        assert.match(run.profile.scope,/may overlap/);
    } else assert.equal(run.profile,null);
});
test('CPU phase evidence refuses missing scopes, impossible totals and mismatched draw attempts',()=>{
    const valid={totalMs:10,preparationMs:1,gridMs:0,connectionsMs:0,adornersMs:1,staticMs:0,
        groups:{'Sketch.Text':{attempts:143,milliseconds:4,skippedAttempts:0},'Sketch.Rectangle':{attempts:857,milliseconds:3,skippedAttempts:0}}};
    verifyScenePhases(valid,12.5,1000);
    verifyScenePhases({...valid,groups:{text:{attempts:0,skippedAttempts:1000,milliseconds:0}}},12.5,1000);
    for(const bad of [null,{...valid,totalMs:20},{...valid,preparationMs:NaN},
        {...valid,groups:[]},{...valid,groups:{text:{attempts:1000,milliseconds:12,skippedAttempts:0}}},
        {...valid,groups:{text:{attempts:999,milliseconds:7,skippedAttempts:0}}},
        {...valid,groups:{text:{attempts:1000.5,milliseconds:7,skippedAttempts:0}}},
        {...valid,groups:{text:{attempts:1000,milliseconds:7}}},
        {...valid,groups:{text:{attempts:1000,skippedAttempts:-1,milliseconds:7}}},
        {...valid,groups:{text:{attempts:999,skippedAttempts:.5,milliseconds:7}}}]) assert.throws(()=>verifyScenePhases(bad,12.5,1000));
});
test('profiled edit refuses absent CPU paint evidence rather than inventing a timing',async t=>{
    const h=editHarness(t,true,null);await assert.rejects(runEdits(),/valid shared scene CPU duration/);
    const report=JSON.parse(h.output.textContent);assert.equal(report.runs.length,0);assert.match(report.error,/valid shared scene CPU duration/);
    assert.equal(h.state.revision,1);
});
test('scheduling driver waits for the second real paint and verifies two dispatches without edits',async t=>{
    let firstPaintPending;
    const h=editHarness(t,false,null,async state=>{
        state.invalidationDispatches++;state.paints++;paint(true);
        await Promise.resolve();await Promise.resolve();
        firstPaintPending=JSON.parse(h.output.textContent).schedulingChecks.length===0;
        state.invalidationDispatches++;state.paints++;paint(true);
    });
    await runSchedulingCheck();
    const report=JSON.parse(h.output.textContent),check=report.schedulingChecks[0];
    assert.equal(firstPaintPending,true);assert.equal(check.passed,true);assert.equal(report.error,null);
    assert.equal(check.final.invalidationDispatches-check.initial.invalidationDispatches,2);
    assert.equal(check.final.paints-check.initial.paints,2);assert.equal(h.state.revision,0);
});
test('scheduling driver refuses redundant host dispatches and editor changes',async t=>{
    for(const mutate of [state=>state.invalidationDispatches++,state=>state.revision++]) {
        const h=editHarness(t,false,null,state=>{
            state.invalidationDispatches+=2;state.paints+=2;mutate(state);paint(true);paint(true);
        });
        await assert.rejects(runSchedulingCheck(),/unchanged editor state/);
        const report=JSON.parse(h.output.textContent);assert.equal(report.schedulingChecks.length,0);assert.match(report.error,/not verified/);
        detach();
    }
});
test('disposing between scheduling paints rejects the unfinished check',async t=>{
    editHarness(t,false,null,state=>{state.invalidationDispatches++;state.paints++;paint(true);detach();});
    await assert.rejects(runSchedulingCheck(),/Benchmark disposed/);
});
