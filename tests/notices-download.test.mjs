import test from 'node:test';
import assert from 'node:assert/strict';
import {attachNoticeDownload} from '../src/Beep.SimpleDraw.Web/wwwroot/js/notices.js';

function fixture(t) {
    const originals = {fetch:globalThis.fetch, document:globalThis.document,
        create:URL.createObjectURL, revoke:URL.revokeObjectURL, timeout:globalThis.setTimeout};
    const blobs = [], links = [], revoked = [];
    globalThis.document = {body:{append(link){link.appended=true;}},createElement(tag){
        assert.equal(tag,'a'); const link={click(){this.clicked=true;},remove(){this.removed=true;}};
        links.push(link); return link;
    }};
    URL.createObjectURL = blob => {blobs.push(blob);return 'blob:notice-test';};
    URL.revokeObjectURL = url => revoked.push(url);
    globalThis.setTimeout = (callback, delay) => {assert.equal(delay,10000);callback();};
    t.after(()=>{globalThis.fetch=originals.fetch;globalThis.document=originals.document;
        URL.createObjectURL=originals.create;URL.revokeObjectURL=originals.revoke;globalThis.setTimeout=originals.timeout;});
    let click;
    const button={disabled:false,addEventListener(name,handler){assert.equal(name,'click');click=handler;}};
    const status={textContent:''};
    attachNoticeDownload(button,status,'https://example.test/draw/licenses/THIRD-PARTY-NOTICES.txt');
    return {button,status,blobs,links,revoked,click:()=>click()};
}

test('notice download preserves original bytes through the shared download helper, including BOM/CRLF/Unicode',async t=>{
    const f=fixture(t), original=new Uint8Array([239,187,191,65,13,10,194,169,10]);
    globalThis.fetch=async (url,options)=>{
        assert.equal(url,'https://example.test/draw/licenses/THIRD-PARTY-NOTICES.txt');
        assert.deepEqual(options,{mode:'same-origin',redirect:'error'});
        return new Response(original,{headers:{'Content-Type':'text/plain'}});
    };
    await f.click(); assert.equal(f.blobs.length,1);
    assert.deepEqual(new Uint8Array(await f.blobs[0].arrayBuffer()),original);
    assert.equal(f.blobs[0].type,'text/plain;charset=utf-8');
    assert.equal(f.links[0].download,'THIRD-PARTY-NOTICES.txt');
    assert.equal(f.links[0].clicked,true);assert.equal(f.links[0].removed,true);
    assert.deepEqual(f.revoked,['blob:notice-test']);assert.equal(f.button.disabled,false);
    assert.equal(f.status.textContent,'Notices download requested.');
});

test('missing notice file reports failure without downloading an HTML error and permits retry',async t=>{
    const f=fixture(t);
    globalThis.fetch=async()=>new Response('<h1>404</h1>',{status:404});
    await f.click();assert.equal(f.blobs.length,0);assert.match(f.status.textContent,/Could not download/);
    assert.equal(f.button.disabled,false);
    globalThis.fetch=async()=>new Response('complete notice');
    await f.click();assert.equal(f.blobs.length,1);
});

test('offline cache/read failures are visible and never create a partial download',async t=>{
    const f=fixture(t);
    for(const failure of [async()=>{throw new TypeError('network unavailable');},
        async()=>({ok:true,arrayBuffer:async()=>{throw new Error('interrupted read');}})]) {
        globalThis.fetch=failure;await f.click();assert.equal(f.blobs.length,0);
        assert.match(f.status.textContent,/Could not download/);assert.equal(f.button.disabled,false);
    }
});

test('repeat activation while the cached file is being read produces only one download',async t=>{
    const f=fixture(t);let resolve,fetches=0;
    globalThis.fetch=()=>{fetches++;return new Promise(r=>resolve=r);};
    const first=f.click();assert.equal(f.button.disabled,true);await f.click();assert.equal(fetches,1);
    resolve(new Response('notice'));await first;assert.equal(f.blobs.length,1);assert.equal(f.button.disabled,false);
});
