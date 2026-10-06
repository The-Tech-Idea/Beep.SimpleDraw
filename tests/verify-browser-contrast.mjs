// Recheck actual browser-computed colors retained through CUA; no CSS simulation.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
const root=path.resolve('docs/evidence');
const json=name=>JSON.parse(fs.readFileSync(path.join(root,name),'utf8'));
const read=name=>fs.readFileSync(path.join(root,name),'utf8');
function luminance(rgb) {
  const channels=rgb.map(v=>v/255).map(v=>v<=.04045?v/12.92:((v+.055)/1.055)**2.4);
  return channels[0]*.2126+channels[1]*.7152+channels[2]*.0722;
}
function color(css) {
  const matched=/^rgb\((\d+), (\d+), (\d+)\)$/.exec(css);
  assert.ok(matched,`Expected an opaque rendered color: ${css}`);
  return matched.slice(1).map(Number);
}
function ratio(a,b) {const x=luminance(color(a)),y=luminance(color(b));return (Math.max(x,y)+.05)/(Math.min(x,y)+.05);}
assert.equal(ratio('rgb(0, 0, 0)','rgb(255, 255, 255)'),21);
const before=json('p4-rendered-contrast-before.json'),after=json('p4-rendered-contrast-after.json');
const countBefore=json('p4-rendered-count-contrast-before.json')[0],countAfter=json('p4-rendered-count-contrast-after.json')[0];
const selectBefore=json('p4-rendered-select-contrast-before.json')[0],selectAfter=json('p4-rendered-select-contrast-after.json')[0];
const environment=json('p4-contrast-browser-environment.json');
const find=(items,label)=>{const result=items.find(item=>item.label===label);assert.ok(result,`Missing rendered control: ${label}`);return result;};
const checks=[];
function check(name,oldColor,newColor,adjacent,threshold) {
  const oldRatio=ratio(oldColor,adjacent),newRatio=ratio(newColor,adjacent);
  assert.ok(oldRatio<threshold,`${name}: baseline finding was not reproduced`);
  assert.ok(newRatio>=threshold,`${name}: contrast below ${threshold}`);
  checks.push({name,before:oldRatio,after:newRatio,threshold,adjacent});
}
for(const label of ['Width','Height']) check(label+' input boundary',find(before,label).border,find(after,label).border,find(after,label).background,3);
const widthBefore=find(before,'Stroke width 2'),widthAfter=find(after,'Stroke width 2');
assert.equal(widthAfter.pressed,'true');assert.equal(widthAfter.borderWidth,'2px');
check('Selected width boundary against control',widthBefore.border,widthAfter.border,widthAfter.background,3);
check('Selected width boundary against panel',widthBefore.border,widthAfter.border,environment.panelBackground,3);
const canvasBefore=find(before,'Drawing canvas'),canvasAfter=find(after,'Drawing canvas');
assert.equal(canvasAfter.outlineStyle,'solid');assert.equal(canvasAfter.outlineWidth,'3px');assert.equal(canvasAfter.outlineOffset,'-4px');
check('Keyboard canvas focus',canvasBefore.outline,canvasAfter.outline,environment.paperBackground,3);
check('Object count text',countBefore.color,countAfter.color,countAfter.background,4.5);
check('Text size select boundary',selectBefore.border,selectAfter.border,selectAfter.background,3);
assert.equal(read('p4-contrast-baseline.simpledraw.json'),read('p4-contrast-roundtrip.simpledraw.json'));
assert.deepEqual(json('p4-contrast-browser-logs.json'),[]);
assert.match(read('p4-contrast-width-four-ui.txt'),/Stroke width 4, Value: 1/);
assert.match(read('p4-contrast-width-undo-ui.txt'),/Stroke width 2, Value: 1/);
const narrow=json('p4-contrast-narrow.json');
assert.equal(narrow.width,320);assert.equal(narrow.scrollWidth,320);assert.equal(narrow.tools.length,11);
assert.ok(narrow.tools.every(tool=>tool.width>=44&&tool.height>=44));
assert.equal(narrow.selectedWidth.borderWidth,'2px');
const delta=json('p4-contrast-runtime-delta.json');
assert.equal(delta.oldCount,402);assert.equal(delta.newCount,402);assert.deepEqual(delta.removed,[]);
assert.equal(delta.changed.length,9);
assert.ok(delta.changed.every(item=>/^(?:css\/app\.css|service-worker-assets\.js|service-worker\.js)(?:\.br|\.gz)?$/.test(item.path)));
const assets=json('p4-contrast-asset-report.json');
assert.equal(assets.offlineAssets,134);
assert.match(read('p4-contrast-offline-reload.txt'),/Objects 7/);
assert.match(read('p4-contrast-offline-reload.txt'),/Restored from this device/);
assert.deepEqual(json('p4-contrast-offline-colors.json'),{countColor:'rgb(71, 85, 105)',selectedWidth:'rgb(109, 40, 217)'});
assert.deepEqual(json('p4-contrast-offline-logs.json'),[]);
const result={date:'2026-10-05',runtime:'artifacts/p4-contrast-final/publish/wwwroot',offlineVersion:assets.offlineVersion,environment,checks,documentBytesIdentical:true,narrowTools44px:true,narrowHorizontalOverflow:false,serverStoppedRestoresSevenAndCorrectedCss:true,runtimeDelta:'Only CSS and generated worker metadata plus their encodings changed; all native/managed/other runtime bytes identical.',browserWarningsAndErrors:0,scope:'Targeted active-control contrast and desktop keyboard/history/file/offline smoke; not axe, full WCAG certification, physical DPI/zoom or screen-reader acceptance.'};
fs.writeFileSync(path.join(root,'p4-contrast-summary.json'),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({checks:checks.map(({name,before,after,threshold})=>({name,before,after,threshold})),documentBytesIdentical:true}));
