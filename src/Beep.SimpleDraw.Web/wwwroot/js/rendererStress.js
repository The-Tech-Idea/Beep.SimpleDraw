import {summarize,verifyEdits} from './performance.js';
let output, reference, pending, running=false, generation=0, paints=0, size=[0,0], results=[];
function render(error=null) {
    if(output) output.textContent=JSON.stringify({scope:'Separate shared-renderer stress; not a native product document or autosave benchmark',
        method:'DrawingManager.MoveSelectedComponents shared history; JS command start to second animation frame after actual SkiaHostComponent paint',
        environment:{userAgent:navigator.userAgent,viewport:[innerWidth,innerHeight],canvas:size,pixelRatio:devicePixelRatio,cores:navigator.hardwareConcurrency,visibility:document.visibilityState},
        paints,runs:results,error},null,2);
}
export function attach(element,dotnet) {generation++;output=element;reference=dotnet;results=[];paints=0;render();}
export function paint(width,height) {
    paints++;size=[width,height];
    const sample=pending, instance=generation;
    if(!sample||sample.queued) return;
    sample.queued=true;
    requestAnimationFrame(()=>requestAnimationFrame(()=>{
        if(output&&instance===generation&&pending===sample) {clearTimeout(sample.timer);pending=null;sample.resolve(performance.now()-sample.start);}
    }));
}
export async function run() {
    if(running||!output||!reference) throw new Error('A ready, idle stress harness is required.');
    if(document.visibilityState!=='visible') throw new Error('Keep the stress tab visible.');
    running=true;
    const samples=[], instance=generation;
    try {
        // Let the newly mounted host finish its initial size observation and frame.
        await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
        if(instance!==generation) throw new Error('Renderer stress disposed.');
        const initial=await reference.invokeMethodAsync('StressState'), startPaints=paints;
        if(initial.objects!==5000||initial.selected!==1) throw new Error('Stress requires exactly 5000 objects and one active selection.');
        for(let i=0;i<140;i++) {
            if(instance!==generation||document.visibilityState!=='visible') throw new Error('Stress harness was closed or hidden.');
            await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
            if(instance!==generation) throw new Error('Renderer stress disposed.');
            const elapsed=new Promise((resolve,reject)=>{
                pending={start:performance.now(),resolve,reject,queued:false};
                pending.timer=setTimeout(()=>{pending=null;reject(new Error('No actual stress paint within 10 seconds.'));},10000);
            });
            const [,latency]=await Promise.all([reference.invokeMethodAsync('StressEdit',i%2===0),elapsed]);
            if(i>=20) samples.push(latency);
        }
        const final=await reference.invokeMethodAsync('StressState');
        verifyEdits(initial,final,140);
        if(paints-startPaints<140) throw new Error('Fewer actual paints than stress edits.');
        results.push({objects:5000,workload:initial.workload,warmup:20,committedEdits:140,observedPaints:paints-startPaints,initial,final,...summarize(samples),
            budgetMs:null,budgetPassed:null,note:'5000 is a separate stability workload; the 50 ms product gate applies to 1000 objects.'});
        render();
    } catch(error) {if(pending){clearTimeout(pending.timer);pending=null;}render(error.message);throw error;}
    finally {running=false;}
}
export function detach() {generation++;if(pending){clearTimeout(pending.timer);pending.reject(new Error('Renderer stress disposed.'));pending=null;}output=null;reference=null;}
