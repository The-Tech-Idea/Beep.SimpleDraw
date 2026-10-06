import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import crypto from 'node:crypto';
import {appRoot} from '../scripts/pin-inputs.mjs';
import {noticeFiles,verifyNoticeBundle} from '../scripts/prepare-notices.mjs';
import {classifyPayload,verifyRuntimeNotices} from '../scripts/release-notices.mjs';

const sdk='C:/Program Files/dotnet';
function fixture(t) {
    const base=fs.mkdtempSync(path.join(os.tmpdir(),'simpledraw-notices-')), root=path.join(base,'app'), wwwroot=path.join(base,'publish');
    t.after(()=>{const resolved=path.resolve(base);assert.equal(path.dirname(resolved),path.resolve(os.tmpdir()));assert.ok(path.basename(resolved).startsWith('simpledraw-notices-'));fs.rmSync(resolved,{recursive:true});});
    const copy=(from,to)=>{fs.mkdirSync(path.dirname(to),{recursive:true});fs.cpSync(from,to,{recursive:true});};
    for(const name of ['dependencies/notices','dependencies/nuget-archives.lock.json','dependencies/toolchain.lock.json','src/Beep.SimpleDraw.Web/packages.net10.lock.json','src/Beep.SimpleDraw.Core/Fonts','src/Beep.SimpleDraw.Web/obj/project.assets.json','src/Beep.SimpleDraw.Web/wwwroot/js']) copy(path.join(appRoot,name),path.join(root,name));
    copy(path.resolve(appRoot,'../Beep.Skia/LICENSE.txt'),path.join(base,'Beep.Skia/LICENSE.txt'));
    const {manifest,files}=noticeFiles(root,sdk), assets=[];
    const write=(name,bytes)=>{const file=path.join(wwwroot,name);fs.mkdirSync(path.dirname(file),{recursive:true});fs.writeFileSync(file,bytes);assets.push({url:name,hash:'sha256-'+crypto.createHash('sha256').update(bytes).digest('base64')});};
    for(const [name,bytes] of files) write('licenses/'+name,bytes);
    for(const name of ['js/notices.js','js/app.js']) write(name,fs.readFileSync(path.join(root,'src/Beep.SimpleDraw.Web/wwwroot',name)));
    for(const name of ['Kalam-Regular.ttf','OFL.txt']) write('fonts/'+name,fs.readFileSync(path.join(root,'src/Beep.SimpleDraw.Core/Fonts',name)));
    for(const name of ['_framework/dotnet.native.abcdefghij.wasm','_framework/System.Runtime.abcdefghij.wasm','_framework/System.Private.CoreLib.abcdefghij.wasm','_framework/Beep.Skia.abcdefghij.wasm','_framework/DataManagementModels.abcdefghij.wasm','_content/SkiaSharp.Views.Blazor/SKHtmlCanvas.js']) write(name,Buffer.from('fixture: '+name));
    const saveManifest=()=>fs.writeFileSync(path.join(wwwroot,'service-worker-assets.js'),'self.assetsManifest = '+JSON.stringify({version:'fixture',assets})+';\n');
    saveManifest(); return {root,wwwroot,manifest,files,assets,saveManifest};
}
test('original notice bundle preserves full native/runtime/font texts and rejects changed or missing distributed text',t=>{
    const f=fixture(t), result=verifyNoticeBundle(path.join(f.wwwroot,'licenses'),f.root,sdk);
    assert.equal(result.groups,45); assert.equal(result.originalTexts,21);
    const text=[...f.files].find(([name])=>name.startsWith('texts/'));
    fs.appendFileSync(path.join(f.wwwroot,'licenses',text[0]),'changed');
    assert.throws(()=>verifyNoticeBundle(path.join(f.wwwroot,'licenses'),f.root,sdk),/Published notice changed/);
    fs.unlinkSync(path.join(f.wwwroot,'licenses',text[0]));
    assert.throws(()=>verifyNoticeBundle(path.join(f.wwwroot,'licenses'),f.root,sdk),/inventory differs/);
});
test('notice capture refuses changed original material and a changed build-input lock',t=>{
    const f=fixture(t), source=path.join(f.root,'dependencies/notices',f.manifest.files[0].path);
    const original=fs.readFileSync(source); fs.appendFileSync(source,'changed');
    assert.throws(()=>noticeFiles(f.root,sdk),/Original notice input changed/);
    fs.writeFileSync(source,original); fs.appendFileSync(path.join(f.root,'src/Beep.SimpleDraw.Web/packages.net10.lock.json'),' ');
    assert.throws(()=>noticeFiles(f.root,sdk),/reviewed build input/);
});
test('actual published sources map packages, SDK, linked native inputs and source-owned modules distinctly',t=>{
    const f=fixture(t), result=verifyRuntimeNotices(f.wwwroot,sdk,f.root);
    const sources=new Map(result.payloads.map(p=>[p.path,p.sources]));
    assert.ok(sources.get('_framework/dotnet.native.abcdefghij.wasm')[0].noticeGroups.includes('package:skiasharp.nativeassets.webassembly/4.150.1'));
    assert.ok(sources.get('_framework/dotnet.native.abcdefghij.wasm')[0].noticeGroups.includes('package:thetechidea.beep.datamanagementmodels/3.1.1'));
    assert.ok(sources.get('_framework/dotnet.native.abcdefghij.wasm')[0].noticeGroups.includes('source:beep-skia'));
    assert.equal(sources.get('_framework/DataManagementModels.abcdefghij.wasm')[0].source,'TheTechIdea.Beep.DataManagementModels/3.1.1');
    assert.equal(sources.get('_framework/System.Runtime.abcdefghij.wasm')[0].source,'Microsoft.NETCore.App.Runtime.Mono.browser-wasm/10.0.12');
    assert.equal(sources.get('_framework/System.Private.CoreLib.abcdefghij.wasm')[0].source,'Microsoft.NETCore.App.Runtime.Mono.browser-wasm/10.0.12');
    assert.ok(result.conservativeAdditionalNoticeGroups.length>0);
});
test('new assemblies and unresolved notice groups cannot acquire attribution by a System prefix or package name',()=>{
    assert.throws(()=>classifyPayload('_framework/System.Unreviewed.abcdefghij.wasm',new Map(),new Set(),new Map()),/no reviewed source/);
    assert.throws(()=>classifyPayload('_framework/Example.abcdefghij.wasm',new Map([['Example.dll','Example/1.0.0']]),new Set(),new Map()),/no full notices/);
    assert.throws(()=>classifyPayload('_content/NewVendor/library.js',new Map(),new Set(),new Map()),/no reviewed notice mapping/);
});
test('native AOT candidate coverage refuses a managed dependency without full notices',()=>{
    const groups=new Map(['sdk:mono/10.0.12','sdk:emscripten/3.1.56',
        'package:skiasharp.nativeassets.webassembly/4.150.1','source:beep-skia'].map(id=>[id,{}]));
    assert.throws(()=>classifyPayload('_framework/dotnet.native.abcdefghij.wasm',
        new Map([['Unreviewed.dll','Unreviewed/1.0.0']]),new Set(),groups),/no full notices/);
    groups.set('package:system.formats.nrbf/10.0.12',{});
    const native=classifyPayload('_framework/dotnet.native.abcdefghij.wasm',
        new Map([['System.Formats.Nrbf.dll','System.Formats.Nrbf/10.0.12']]),new Set(),groups);
    assert.ok(native[0].noticeGroups.includes('package:system.formats.nrbf/10.0.12'));
});
test('full notices must be installed offline with their exact integrity hashes',t=>{
    const f=fixture(t); const record=f.assets.find(a=>a.url==='licenses/THIRD-PARTY-NOTICES.txt');
    record.hash='sha256-invalid'; f.saveManifest();
    assert.throws(()=>verifyRuntimeNotices(f.wwwroot,sdk,f.root),/Offline notice\/payload integrity mismatch/);
    f.assets.splice(f.assets.indexOf(record),1);f.saveManifest();
    assert.throws(()=>verifyRuntimeNotices(f.wwwroot,sdk,f.root),/absent from the offline manifest/);
});
test('the licensed font and its original license cannot silently change in the shipped artifact',t=>{
    const f=fixture(t);fs.appendFileSync(path.join(f.wwwroot,'fonts/Kalam-Regular.ttf'),'changed');
    assert.throws(()=>verifyRuntimeNotices(f.wwwroot,sdk,f.root),/font differs from its licensed input/);
});

test('offline notice downloads require the exact handler and shared byte-download helper',t=>{
    const f=fixture(t);
    for(const name of ['js/notices.js','js/app.js']) {
        const record=f.assets.find(a=>a.url===name), original=record.hash;
        record.hash='sha256-invalid';f.saveManifest();
        assert.throws(()=>verifyRuntimeNotices(f.wwwroot,sdk,f.root),/Offline notice\/payload integrity mismatch/);
        record.hash=original;f.saveManifest();
        const file=path.join(f.wwwroot,name), bytes=fs.readFileSync(file);
        fs.appendFileSync(file,'changed');
        assert.throws(()=>verifyRuntimeNotices(f.wwwroot,sdk,f.root),/notice download module differs/);
        fs.writeFileSync(file,bytes);
        f.assets.splice(f.assets.indexOf(record),1);f.saveManifest();
        assert.throws(()=>verifyRuntimeNotices(f.wwwroot,sdk,f.root),/absent from the offline manifest/);
        f.assets.push(record);f.saveManifest();
    }
    const page=f.files.get('index.html').toString();
    assert.match(page,/<button type="button" id="download-notices">/);
    assert.match(page,/id="notices-download-status" role="status" aria-live="polite"/);
    assert.match(page,/<script type="module" src="\.\.\/js\/notices.js"><\/script>/);
});
