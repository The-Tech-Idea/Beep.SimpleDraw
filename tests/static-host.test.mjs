import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import http from 'node:http';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import {createStaticServer} from '../scripts/serve-static.mjs';
import {finalizeAssets} from '../scripts/release-assets.mjs';

test('verified static host enforces scope, MIME, compression, cache and CSP',async t=>{
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'simpledraw-host-'));
  const sri=bytes=>'sha256-'+crypto.createHash('sha256').update(bytes).digest('base64');
  const files={'index.html':'<base href="/" /><script type="importmap">{"imports":{},"integrity":{}}</script><script>void 0;</script>','_framework/dotnet.native.abcdefghij.wasm':Buffer.from([0,97,115,109]),'fonts/Kalam-Regular.ttf':'font fixture','fonts/OFL.txt':'license fixture','manifest.webmanifest':'{}','css/app.css':'body{}','js/documents.js':'export {};','service-worker.js':'self.importScripts("./service-worker-assets.js");'};
  for(const [name,data] of Object.entries(files)) {
    const file=path.join(root,name);fs.mkdirSync(path.dirname(file),{recursive:true});fs.writeFileSync(file,data);
    fs.writeFileSync(file+'.br',zlib.brotliCompressSync(Buffer.from(data)));fs.writeFileSync(file+'.gz',zlib.gzipSync(Buffer.from(data)));
  }
  fs.writeFileSync(path.join(root,'service-worker-assets.js'),'self.assetsManifest = '+JSON.stringify({version:'fixture',assets:Object.keys(files).filter(url=>url!=='service-worker.js').map(url=>({url,hash:sri(files[url])}))})+';');
  const report=finalizeAssets(root,'/draw/'),server=createStaticServer(root,report);
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  t.after(async()=>{
    await new Promise(resolve=>server.close(resolve));
    const resolved=path.resolve(root);assert.equal(path.dirname(resolved),path.resolve(os.tmpdir()));assert.ok(path.basename(resolved).startsWith('simpledraw-host-'));fs.rmSync(resolved,{recursive:true});
  });
  const request=(url,headers={},method='GET')=>new Promise((resolve,reject)=>{
    const req=http.request({hostname:'127.0.0.1',port:server.address().port,path:url,headers,method},response=>{
      const chunks=[];response.on('data',chunk=>chunks.push(chunk));response.on('end',()=>resolve({status:response.statusCode,headers:response.headers,body:Buffer.concat(chunks)}));
    });req.on('error',reject);req.end();
  });
  await t.test('Brotli native payload uses WASM MIME, immutable fingerprint cache and the measured CSP',async()=>{
    const result=await request('/draw/_framework/dotnet.native.abcdefghij.wasm',{'Accept-Encoding':'br, gzip'});
    assert.equal(result.status,200);assert.equal(result.headers['content-type'],'application/wasm');assert.equal(result.headers['content-encoding'],'br');
    assert.deepEqual(zlib.brotliDecompressSync(result.body),files['_framework/dotnet.native.abcdefghij.wasm']);
    assert.ok(result.headers['cache-control'].includes('immutable'));assert.equal(result.headers['content-security-policy'],report.csp);assert.equal(result.headers['x-content-type-options'],'nosniff');
  });
  await t.test('gzip preference and q=0 are honored; unfingerprinted CSS revalidates',async()=>{
    const result=await request('/draw/css/app.css',{'Accept-Encoding':'br;q=0, gzip;q=1'});
    assert.equal(result.headers['content-encoding'],'gzip');assert.equal(zlib.gunzipSync(result.body).toString(),files['css/app.css']);assert.equal(result.headers['cache-control'],'no-cache');
  });
  await t.test('HEAD supplies identity metadata with no body and no content encoding',async()=>{
    const result=await request('/draw/fonts/Kalam-Regular.ttf',{},'HEAD');assert.equal(result.status,200);assert.equal(result.body.length,0);assert.equal(Number(result.headers['content-length']),files['fonts/Kalam-Regular.ttf'].length);assert.equal(result.headers['content-encoding'],undefined);
  });
  await t.test('root redirect and scoped SPA fallback do not expose outside paths',async()=>{
    assert.equal((await request('/draw')).headers.location,'/draw/');assert.equal((await request('/elsewhere')).status,404);
    const result=await request('/draw/scene/123');assert.equal(result.status,200);assert.ok(result.body.toString().includes('<base href="/draw/" />'));assert.equal(result.headers['cache-control'],'no-cache');
  });
  await t.test('missing assets, encoded separators, traversal and write methods are rejected',async()=>{
    assert.equal((await request('/draw/missing.js')).status,404);
    for(const url of ['/draw/../global.json','/draw/%2e%2e/global.json','/draw/css%2fapp.css','/draw/%ZZ']) assert.equal((await request(url)).status,400);
    const result=await request('/draw/',{},'POST');assert.equal(result.status,405);assert.equal(result.headers.allow,'GET, HEAD');
  });
  await t.test('worker and manifest always revalidate; mismatched hosting evidence refuses startup',async()=>{
    for(const name of ['service-worker.js','service-worker-assets.js','index.html']) assert.equal((await request('/draw/'+name)).headers['cache-control'],'no-cache');
    assert.throws(()=>createStaticServer(root,{...report,csp:"default-src *"}),/report differs/);
  });
});
