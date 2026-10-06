import test from 'node:test';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import {readFile} from 'node:fs/promises';
const source=await readFile(new URL('../src/Beep.SimpleDraw.Web/wwwroot/service-worker.published.js',import.meta.url),'utf8');
function setup(path='/draw/'){
    const handlers={}, entries=new Map(), removed=[], requests=[], clients=[], posted=[];let skipped=0;
    const self={registration:{scope:'https://example.test'+path},origin:'https://example.test',assetsManifest:{version:'abc',assets:[
        {url:'index.html',hash:'sha256-aa'},{url:'fonts/Kalam-Regular.ttf',hash:'sha256-bb'},
        {url:'_framework/native.wasm',hash:'sha256-cc'},{url:'service-worker.js',hash:'sha256-dd'}]},
        importScripts(){},addEventListener(name,fn){handlers[name]=fn;},clients:{async claim(){},async matchAll(){return clients;}},async skipWaiting(){skipped++;}};
    const cache={async addAll(items){requests.push(...items);},async match(key){return entries.get(typeof key==='string'?key:key.url);}};
    vm.runInNewContext(source,{self,URL,Request,console:{info(){}},caches:{async open(){return cache;},async keys(){return ['simpledraw-'+path+'-old','simpledraw-/other/-old'];},async delete(k){removed.push(k);}},fetch:async request=>({network:request.url})});
    return {handlers,entries,removed,requests,clients,posted,get skipped(){return skipped;},
        async install(){let result;handlers.install({waitUntil:p=>result=p});await result;},
        async activate(){let result;handlers.activate({waitUntil:p=>result=p});await result;},
        async message(){let result;handlers.message({data:{type:'ACTIVATE_IF_SINGLE_EDITOR'},source:{postMessage:m=>posted.push(m)},waitUntil:p=>result=p});await result;},
        async fetch(url,mode='navigate'){let result;handlers.fetch({request:{method:'GET',url,mode},respondWith:p=>result=p});return await result;}};
}
test('root and subpath install use integrity-scoped absolute requests including bundled font',async()=>{
    for(const path of ['/','/draw/']){const w=setup(path);await w.install();assert.equal(w.requests.length,3);
        assert.equal(w.requests[0].url,'https://example.test'+path+'index.html');assert.equal(w.requests[1].integrity,'sha256-bb');}
});
test('activation cleans only this app scope, leaving other installed apps intact',async()=>{
    const w=setup();await w.activate();assert.deepEqual(w.removed,['simpledraw-/draw/-old']);
});
test('update cannot activate while another editor is open',async()=>{
    const w=setup();w.clients.push({url:'https://example.test/draw/'},{url:'https://example.test/draw/?second'});await w.message();
    assert.equal(w.skipped,0);assert.equal(w.posted[0].type,'UPDATE_BLOCKED');w.clients.pop();await w.message();assert.equal(w.skipped,1);
});
test('offline navigation stays in app scope and uses cached index',async()=>{
    const w=setup();w.entries.set('https://example.test/draw/index.html',{offline:true});
    assert.equal((await w.fetch('https://example.test/draw/')).offline,true);
    assert.equal((await w.fetch('https://example.test/unrelated/')).network,'https://example.test/unrelated/');
    assert.equal((await w.fetch('https://example.test/draw/diagnostic.html')).network,'https://example.test/draw/diagnostic.html');
});
