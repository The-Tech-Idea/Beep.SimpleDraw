import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
const source = await readFile(new URL('../src/Beep.SimpleDraw.Web/wwwroot/js/documents.js',import.meta.url),'utf8');
const channelModule = await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));
const {createDocumentStore} = channelModule;
function environment() {
    const map = new Map(), held = new Set(); let fail = false;
    return {key:'test',storage:{getItem:k=>map.get(k)??null,setItem(k,v){if(fail)throw new Error('QuotaExceededError');map.set(k,v);}},
        locks:{async request(key,options,callback){if(held.has(key))return callback(null);held.add(key);try{return await callback({name:key});}finally{held.delete(key);}}},
        map,setFail(value){fail=value;}};
}
test('only one writer; closing releases ownership and restores its latest successful revision',async()=>{
    const env=environment(), a=createDocumentStore(env), b=createDocumentStore(env);
    assert.equal((await a.open()).status,'empty'); assert.equal(a.save('{"native":true}',12).status,'saved');
    const conflict=await b.open();assert.equal(conflict.status,'conflict');assert.equal(conflict.document,'{"native":true}');
    assert.equal(b.save('file-only',13).status,'conflict'); a.close(); await new Promise(setImmediate);
    const loaded=await b.open(); assert.equal(loaded.revision,12); assert.equal(loaded.document,'{"native":true}'); b.close();
});
test('quota failure preserves the last successful bytes and allows retry',async()=>{
    const env=environment(), store=createDocumentStore(env); await store.open(); store.save('first',1); const bytes=env.map.get('test');
    env.setFail(true); assert.equal(store.save('second',2).status,'failed'); assert.equal(env.map.get('test'),bytes);
    env.setFail(false); assert.equal(store.save('second',2).revision,2);store.close();
});
test('corrupt and future records preserve original bytes until an explicit recovery replacement',async()=>{
    for(const raw of ['{invalid',JSON.stringify({version:2,revision:1,document:'future'}),'null',JSON.stringify({version:1,revision:-1,document:'bad'})]){
        const env=environment();env.map.set('test',raw);const store=createDocumentStore(env);
        const result=await store.open();assert.equal(result.status,'recovery');assert.equal(result.raw,raw);
        assert.equal(store.save('empty',1).status,'recovery');assert.equal(env.map.get('test'),raw);
        assert.equal(store.save('recovered',1,true).status,'saved');store.close();
    }
});
test('compare-before-write and external notifications block silent replacement',async()=>{
    const env=environment(), events=[];const store=createDocumentStore({...env,notify:result=>events.push(result)});await store.open();store.save('owned',1);
    env.map.set('test','external');assert.equal(store.save('mine',2).status,'conflict');assert.equal(env.map.get('test'),'external');
    store.externalChange({key:'test',newValue:'external'});assert.equal(events[0].status,'conflict');store.close();
});
test('unavailable coordination cannot claim a local save',async()=>{
    const env=environment();const store=createDocumentStore({...env,locks:null});assert.equal((await store.open()).status,'unavailable');
    assert.equal(store.save('mine',1).status,'conflict');assert.equal(env.map.size,0);store.close();
});
test('invalid revisions and oversized records never change stored bytes',async()=>{
    const env=environment(), store=createDocumentStore(env);await store.open();
    assert.equal(store.save('mine',NaN).status,'failed');assert.equal(store.save('x'.repeat(3*1024*1024),1).status,'failed');assert.equal(env.map.size,0);store.close();
});

test('document/library channels isolate keys, locks, guards, notifications and disposal',async()=>{
    const env=environment(), listeners=new Map(), notified=[];
    globalThis.document={baseURI:'https://owned.invalid/portable/'};
    globalThis.localStorage=env.storage;
    Object.defineProperty(globalThis,'navigator',{configurable:true,value:{locks:env.locks}});
    globalThis.window={addEventListener(name,fn){if(!listeners.has(name))listeners.set(name,new Set());listeners.get(name).add(fn);},removeEventListener(name,fn){listeners.get(name)?.delete(fn);}};
    const reference={async invokeMethodAsync(...args){notified.push(args);}};
    assert.equal((await channelModule.connect(reference)).status,'empty');
    assert.equal((await channelModule.connectLibrary(reference)).status,'empty');
    channelModule.save('board',1);channelModule.saveLibrary('library',4);
    assert.equal(JSON.parse(env.map.get('beep.simpledraw:/portable/:document:1')).document,'board');
    assert.equal(JSON.parse(env.map.get('beep.simpledraw:/portable/:library:1')).document,'library');
    channelModule.guard(false);channelModule.guardLibrary(true);
    let blocked=0;for(const callback of listeners.get('beforeunload')) callback({preventDefault(){blocked++;}});assert.equal(blocked,1);
    for(const callback of listeners.get('storage')) callback({key:'beep.simpledraw:/portable/:library:1',newValue:'external'});
    assert.equal(notified.length,1); assert.equal(notified[0][0],'LibraryChanged');
    channelModule.disconnectLibrary(); assert.equal(channelModule.save('board-next',2).status,'saved');
    blocked=0;for(const callback of listeners.get('beforeunload')) callback({preventDefault(){blocked++;}});assert.equal(blocked,0);
    channelModule.disconnect(); assert.equal([...listeners.values()].reduce((n,set)=>n+set.size,0),0);
    await new Promise(setImmediate);
});
