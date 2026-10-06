import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import {validateBasePath,finalizeAssets,verifyAssets,readManifest} from '../scripts/release-assets.mjs';

const sri=bytes=>'sha256-'+crypto.createHash('sha256').update(bytes).digest('base64');
function fixture(t) {
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'simpledraw-assets-'));
  t.after(()=>{
    const resolved=path.resolve(root);
    assert.equal(path.dirname(resolved),path.resolve(os.tmpdir()));
    assert.ok(path.basename(resolved).startsWith('simpledraw-assets-'));
    fs.rmSync(resolved,{recursive:true});
  });
  const write=(name,data)=>{
    const file=path.join(root,name);fs.mkdirSync(path.dirname(file),{recursive:true});fs.writeFileSync(file,data);
    fs.writeFileSync(file+'.br',zlib.brotliCompressSync(Buffer.from(data)));
    fs.writeFileSync(file+'.gz',zlib.gzipSync(Buffer.from(data)));
  };
  const runtime='export default {};';
  const imports={imports:{'./_framework/dotnet.js':'./_framework/dotnet.abcdefghij.js'},integrity:{'./_framework/dotnet.abcdefghij.js':sri(runtime)}};
  write('index.html',`<html><head><base href="/" /><link href="_framework/dotnet.abcdefghij.js" integrity="${sri(runtime)}" /><script type="importmap">${JSON.stringify(imports)}</script></head><body><script>navigator.serviceWorker.register('service-worker.js');</script></body></html>`);
  for(const [name,data] of Object.entries({'_framework/dotnet.abcdefghij.js':runtime,'_framework/dotnet.native.abcdefghij.wasm':Buffer.from([0,97,115,109]),'fonts/Kalam-Regular.ttf':'font fixture','fonts/OFL.txt':'license fixture','manifest.webmanifest':'{"start_url":"./"}','css/app.css':'body{}','js/documents.js':'export {};'})) write(name,data);
  write('service-worker.js','self.importScripts("./service-worker-assets.js");');
  const assets=['index.html','_framework/dotnet.abcdefghij.js','_framework/dotnet.native.abcdefghij.wasm','fonts/Kalam-Regular.ttf','fonts/OFL.txt','manifest.webmanifest','css/app.css','js/documents.js'].map(url=>({url,hash:sri(fs.readFileSync(path.join(root,url)))}));
  const manifest={version:'sdkversion',assets};
  const setManifest=()=>write('service-worker-assets.js','self.assetsManifest = '+JSON.stringify(manifest)+';\n');
  setManifest();
  return {root,write,manifest,setManifest};
}
test('base paths are canonical, scope-safe and slash terminated',()=>{
  for(const base of ['/','/draw/','/team_a/draw-2/']) assert.equal(validateBasePath(base),base);
  for(const base of ['','draw/','/draw','//evil/','/../','/a%2fb/','/a b/','https://example.com/','/a?b/','/a\\b/']) assert.throws(()=>validateBasePath(base));
});
test('subpath finalization updates only the entry hash and all its encodings; CSP hashes inline scripts',t=>{
  const f=fixture(t),before=f.manifest.assets.map(a=>({...a}));
  const report=finalizeAssets(f.root,'/team/draw/'),after=readManifest(f.root);
  assert.equal(report.basePath,'/team/draw/');assert.notEqual(after.version,'sdkversion');
  assert.notEqual(after.assets[0].hash,before[0].hash);assert.deepEqual(after.assets.slice(1),before.slice(1));
  assert.equal(verifyAssets(f.root,'/team/draw/').offlineVersion,after.version);
  assert.ok(report.csp.includes("'wasm-unsafe-eval'"));assert.ok(!report.csp.includes("'unsafe-eval'"));
  assert.ok(report.csp.includes("'"+sri("navigator.serviceWorker.register('service-worker.js');")+"'"));
  assert.deepEqual(report.budget.passed,true);
});
test('SDK generated worker encoding is regenerated from final PublishedContent',t=>{
  const f=fixture(t);fs.writeFileSync(path.join(f.root,'service-worker.js.br'),zlib.brotliCompressSync(Buffer.from('old development worker')));
  assert.throws(()=>verifyAssets(f.root),/Compressed asset mismatch/);
  assert.doesNotThrow(()=>finalizeAssets(f.root));
});
test('Windows CRLF import maps get parsed-content CSP hashes and a new canonical entry version',t=>{
  const f=fixture(t),file=path.join(f.root,'index.html');
  const text=fs.readFileSync(file,'utf8').replace('<script type="importmap">','<script type="importmap">\r\n').replace('</script></head>','\r\n</script></head>');
  f.write('index.html',text);f.manifest.assets[0].hash=sri(text);f.setManifest();
  const map=/<script type="importmap">([\s\S]*?)<\/script>/.exec(text)[1];
  const report=finalizeAssets(f.root);
  assert.ok(report.csp.includes("'"+sri(map.replace(/\r\n?/g,'\n'))+"'"));
  assert.ok(!fs.readFileSync(file,'utf8').includes('\r'));assert.notEqual(report.offlineVersion,'sdkversion');
  assert.doesNotThrow(()=>verifyAssets(f.root));
});
test('changed payload cannot be repaired by finalization or packaged under a new hash',t=>{
  const f=fixture(t);fs.writeFileSync(path.join(f.root,'fonts/Kalam-Regular.ttf'),'changed font');
  assert.throws(()=>finalizeAssets(f.root),/integrity mismatch before finalization/);
  assert.equal(readManifest(f.root).version,'sdkversion');
  assert.throws(()=>verifyAssets(f.root),/Offline integrity mismatch/);
});
test('stale gzip, orphan encodings and duplicate offline URLs are rejected',t=>{
  const f=fixture(t);const file=path.join(f.root,'css/app.css.gz'),original=fs.readFileSync(file);
  fs.writeFileSync(file,zlib.gzipSync(Buffer.from('old CSS')));assert.throws(()=>verifyAssets(f.root),/Compressed asset mismatch/);fs.writeFileSync(file,original);
  fs.writeFileSync(path.join(f.root,'orphan.txt.br'),zlib.brotliCompressSync(Buffer.from('orphan')));assert.throws(()=>verifyAssets(f.root),/Orphan compressed/);fs.unlinkSync(path.join(f.root,'orphan.txt.br'));
  f.manifest.assets.push(f.manifest.assets[0]);f.setManifest();assert.throws(()=>verifyAssets(f.root),/Duplicate offline/);
});
test('missing font and traversal URLs are rejected before reading outside publish',t=>{
  const f=fixture(t);const original=f.manifest.assets;
  f.manifest.assets=original.filter(a=>a.url!=='fonts/Kalam-Regular.ttf');f.setManifest();assert.throws(()=>verifyAssets(f.root),/Required offline asset missing/);
  f.manifest.assets=[...original,{url:'../outside',hash:'anything'}];f.setManifest();assert.throws(()=>verifyAssets(f.root),/Unsafe asset URL/);
});
test('external entry resources and incorrect import integrity cannot pass a valid offline hash',t=>{
  const f=fixture(t),file=path.join(f.root,'index.html'),original=fs.readFileSync(file,'utf8');
  const change=text=>{f.write('index.html',text);f.manifest.assets[0].hash=sri(text);f.setManifest();};
  change(original.replace('<body>','<body><script src="https://example.com/runtime.js"></script>'));assert.throws(()=>verifyAssets(f.root),/external runtime resource/);
  change(original.replace('"./_framework/dotnet.abcdefghij.js":"sha256-', '"./_framework/dotnet.abcdefghij.js":"sha256-bad'));
  assert.throws(()=>verifyAssets(f.root),/Import integrity mismatch/);
});
test('whole artifact compressed inventory enforces the 25 MiB release gate',t=>{
  const f=fixture(t);fs.writeFileSync(path.join(f.root,'uncompressed-extra.bin'),Buffer.alloc(25*1024*1024));
  assert.throws(()=>verifyAssets(f.root),/exceeds 25 MiB/);
});
