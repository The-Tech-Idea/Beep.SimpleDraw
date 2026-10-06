import test from 'node:test';
import assert from 'node:assert/strict';
import {attach,paint,run,detach} from '../src/Beep.SimpleDraw.Web/wwwroot/js/rendererStress.js';

function harness(t,objects=5000) {
    const keys=['navigator','document','innerWidth','innerHeight','devicePixelRatio','requestAnimationFrame'];
    const saved=new Map(keys.map(key=>[key,Object.getOwnPropertyDescriptor(globalThis,key)]));
    const values={navigator:{userAgent:'test',hardwareConcurrency:1},document:{visibilityState:'visible'},innerWidth:1280,innerHeight:720,devicePixelRatio:1,
        requestAnimationFrame:callback=>queueMicrotask(callback)};
    for(const key of keys) Object.defineProperty(globalThis,key,{value:values[key],configurable:true,writable:true});
    t.after(()=>{detach();for(const [key,descriptor] of saved) {if(descriptor) Object.defineProperty(globalThis,key,descriptor);else delete globalThis[key];}});
    const output={textContent:''}, state={objects,selected:1,x:30,y:30,revision:0,undoCount:0,maxHistoryDepth:100,workload:'test workload'}, reference={
        async invokeMethodAsync(method,right) {
            if(method==='StressState') return {...state};
            assert.equal(method,'StressEdit');state.x+=right?1:-1;state.revision++;state.undoCount=Math.min(100,state.undoCount+1);
            paint(1200,440);
        }
    };
    attach(output,reference);return {output,state};
}

test('renderer stress requires actual 5000 edits/paints and reports no product budget',async t=>{
    const h=harness(t);await run();const report=JSON.parse(h.output.textContent),result=report.runs[0];
    assert.equal(h.state.revision,140);assert.equal(result.samples,120);assert.equal(result.observedPaints,140);
    assert.equal(result.final.x,result.initial.x);assert.equal(result.final.undoCount,100);
    assert.equal(result.budgetMs,null);assert.equal(result.budgetPassed,null);
    assert.deepEqual(report.environment.canvas,[1200,440]);
});
test('stress refuses an ordinary product count or hidden tab before reporting success',async t=>{
    const h=harness(t,1000);await assert.rejects(run(),/exactly 5000/);assert.equal(JSON.parse(h.output.textContent).runs.length,0);
    document.visibilityState='hidden';await assert.rejects(run(),/visible/);assert.equal(h.state.revision,0);
});
test('closing a pending stress paint cancels measurement and old frames cannot resolve a new harness',async t=>{
    const h=harness(t), frames=[];
    globalThis.requestAnimationFrame=callback=>frames.push(callback);
    const running=run();
    for(let i=0;i<20&&h.state.revision===0;i++) {if(frames.length) frames.shift()();await Promise.resolve();}
    assert.equal(h.state.revision,1);
    detach();while(frames.length) frames.shift()();
    await assert.rejects(running,/disposed/);
    const next={textContent:''};attach(next,{});while(frames.length) frames.shift()();
    assert.equal(JSON.parse(next.textContent).runs.length,0);
    assert.equal(h.state.revision<=1,true);
});
