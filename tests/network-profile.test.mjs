import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import http from 'node:http';
import {performance} from 'node:perf_hooks';
import {ResponseLink} from '../scripts/network-profile.mjs';

test('aggregate bandwidth reservations share one clock and do not accumulate idle burst credit',()=>{
  let now=100;
  const link=new ResponseLink({downloadMbps:8,responseDelayMs:40},{clock:()=>now});
  assert.equal(link.reserve(100000),200);assert.equal(link.reserve(200000),400);
  now=1000;assert.equal(link.reserve(100000),1100);
  for(const bytes of [0,-1,NaN,Infinity,1.5]) assert.throws(()=>link.reserve(bytes));
});
test('invalid or incomplete network profiles cannot silently choose a faster rate',()=>{
  for(const profile of [null,{}, {downloadMbps:0,responseDelayMs:40},{downloadMbps:Infinity,responseDelayMs:40},
    {downloadMbps:8,responseDelayMs:-1},{downloadMbps:8,responseDelayMs:1.5}]) assert.throws(()=>new ResponseLink(profile));
});
test('real concurrent responses preserve bytes and share the aggregate limit; HEAD has no body',async t=>{
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'simpledraw-link-')),file=path.join(root,'payload');
  const bytes=Buffer.alloc(200000,117);fs.writeFileSync(file,bytes);const transfers=[];
  const link=new ResponseLink({downloadMbps:8,responseDelayMs:40},{onTransfer:value=>transfers.push(value)});
  const server=http.createServer((request,response)=>{
    link.send(file,request,response,{'Content-Length':bytes.length}).catch(()=>response.destroy());
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  t.after(async()=>{await new Promise(resolve=>server.close(resolve));
    const resolved=path.resolve(root);assert.equal(path.dirname(resolved),path.resolve(os.tmpdir()));assert.ok(path.basename(resolved).startsWith('simpledraw-link-'));fs.rmSync(resolved,{recursive:true});});
  const request=(method='GET')=>new Promise((resolve,reject)=>{
    const req=http.request({hostname:'127.0.0.1',port:server.address().port,path:'/payload',method},response=>{
      const chunks=[];response.on('data',chunk=>chunks.push(chunk));response.on('end',()=>resolve(Buffer.concat(chunks)));
    });req.on('error',reject);req.end();
  });
  const started=performance.now(),bodies=await Promise.all([request(),request()]);
  assert.ok(performance.now()-started>=440,'400000 bytes at 1 MB/s plus 40 ms delay must not use two independent links.');
  assert.deepEqual(bodies,[bytes,bytes]);assert.equal(transfers.length,2);
  assert.ok(transfers.every(value=>value.completed&&value.bodyBytes===200000));
  assert.equal((await request('HEAD')).length,0);assert.equal(transfers.at(-1).bodyBytes,0);
  assert.equal(transfers.at(-1).method,'HEAD');
});
test('aborting a delayed response releases its wait without sending body bytes',async()=>{
  const controller=new AbortController(),link=new ResponseLink({downloadMbps:8,responseDelayMs:40});
  const wait=link.waitUntil(performance.now()+10000,controller.signal);controller.abort();
  await assert.rejects(wait,/Response closed/);
});
