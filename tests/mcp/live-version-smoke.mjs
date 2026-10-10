import {spawn} from 'node:child_process';
import readline from 'node:readline';
import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';

// Focused acceptance of a published MCP binary, using a separate fixture workspace.
const dll = path.resolve(process.argv[2]);
const root = path.resolve(process.argv[3]);
const origin = 'https://draw.thetechidea.app';
await fs.mkdir(root, {recursive:true});
const child = spawn('dotnet', [dll, '--workspace', path.join(root, 'workspace'), '--link-origin', origin], {stdio:['pipe','pipe','pipe']});
let stderr = '', sequence = 0, invalidLine;
const pending = new Map();
child.stderr.on('data', bytes => { stderr += bytes; });
readline.createInterface({input:child.stdout}).on('line', line => {
    let response;
    try { response = JSON.parse(line); }
    catch { invalidLine = line; return; }
    if (response.id !== undefined) pending.get(response.id)?.(response);
});
function request(method, params = {}) {
    return new Promise((resolve, reject) => {
        const id = ++sequence;
        const timer = setTimeout(() => { pending.delete(id); reject(new Error(`Timeout: ${method}`)); }, 15000);
        pending.set(id, response => {
            clearTimeout(timer); pending.delete(id);
            if (response.error) reject(new Error(JSON.stringify(response.error)));
            else resolve(response.result);
        });
        child.stdin.write(JSON.stringify({jsonrpc:'2.0', id, method, params}) + '\n');
    });
}
async function data(name, args = {}) {
    const result = await request('tools/call', {name, arguments:args});
    assert.equal(result.isError ?? false, false, JSON.stringify(result));
    return JSON.parse(result.content.find(block => block.type === 'text').text);
}
try {
    const init = await request('initialize', {protocolVersion:'2025-06-18', capabilities:{}, clientInfo:{name:'SimpleDraw version compatibility acceptance',version:'1.0'}});
    child.stdin.write(JSON.stringify({jsonrpc:'2.0',method:'notifications/initialized'}) + '\n');
    const manifest = await request('tools/list');
    const expected = JSON.parse(await fs.readFile(new URL('../../distribution/mcp-plugins/shared/tools.json',import.meta.url),'utf8')).tools;
    assert.deepEqual(manifest.tools.map(tool => tool.name).sort(), expected);
    const board = await data('create_drawing', {name:'Version compatibility fixture'});
    const emptyNative = (await data('export_drawing', {drawingId:board.id})).nativeJson;
    const tableBoard = await data('add_table', {drawingId:board.id,expectedRevision:board.revision,rows:2,columns:2,cells:[['Version 23','Live link'],['مخطط','Table']],rightToLeft:true});
    const tableId = tableBoard.selection[0];
    const tableBefore = await data('get_table',{drawingId:board.id,elementId:tableId});
    const native = (await data('export_drawing', {drawingId:board.id})).nativeJson;
    assert.equal(JSON.parse(native).version, 23);
    const imported = await data('import_drawing', {name:'Version 23 import fixture', nativeJson:native});
    assert.equal((await data('export_drawing', {drawingId:imported.id})).nativeJson, native);
    const legacy = JSON.parse(emptyNative); legacy.version = 20;
    const migrated = await data('import_drawing', {name:'Version 20 migration fixture', nativeJson:JSON.stringify(legacy)});
    assert.equal(JSON.parse((await data('export_drawing', {drawingId:migrated.id})).nativeJson).version, 23);
    const offer = await data('link_board', {drawingId:board.id});
    const base = new URL(offer.address);
    assert.equal(base.hostname, '127.0.0.1');
    assert.ok(['7248','17248','37248'].includes(base.port));
    const paired = await fetch(new URL('/live/pair',base), {method:'POST', headers:{Origin:origin,'Content-Type':'application/json'},body:JSON.stringify({code:offer.code}),signal:AbortSignal.timeout(15000)});
    assert.equal(paired.status,200);
    const session = await paired.json();
    assert.equal(JSON.parse(session.document).version,23);
    const headers = {Origin:origin,'Content-Type':'application/json',Authorization:`Bearer ${session.token}`};
    const push = async (document, revision) => fetch(new URL('/live/board',base), {method:'PUT',headers,body:JSON.stringify({baseRevision:revision,document}),signal:AbortSignal.timeout(15000)});
    const pushed = await push(native,session.revision);
    assert.equal(pushed.status,200);
    const changed = await pushed.json();
    assert.ok(changed.revision > session.revision);
    assert.deepEqual(await data('get_table',{drawingId:board.id,elementId:tableId}),tableBefore);
    const polled = await fetch(new URL(`/live/board?after=${session.revision}`,base), {headers,signal:AbortSignal.timeout(15000)});
    assert.equal(polled.status,200);
    assert.equal(JSON.parse((await polled.json()).document).version,23);
    const future = JSON.parse(native); future.version = 24;
    const refused = await push(JSON.stringify(future),changed.revision);
    assert.equal(refused.status,400);
    assert.match((await refused.json()).error,/version 1 through 23/);
    const stale = await push(native,session.revision);
    assert.equal(stale.status,409);
    const ended = await fetch(new URL('/live/session',base), {method:'DELETE',headers,signal:AbortSignal.timeout(15000)});
    assert.ok(ended.ok);
    assert.equal((await push(native,changed.revision)).status,401);
    assert.equal(invalidLine,undefined,'stdout must contain only JSON-RPC');
    const evidence = {protocolVersion:init.protocolVersion,tools:manifest.tools.length,nativeVersion:23,exactNativeImport:true,version20Migration:true,livePairVersion:23,livePushVersion23:true,livePollVersion23:true,tableAndArabicPreserved:true,futureVersionRefused:true,staleRevisionRefused:true,endedSessionRefused:true,stdoutJsonOnly:true};
    await fs.writeFile(path.join(root,'acceptance.json'),JSON.stringify(evidence,null,2)+'\n');
    console.log(JSON.stringify(evidence));
} finally {
    const exited = new Promise(resolve => child.once('exit',resolve));
    child.stdin.end();
    await Promise.race([exited,new Promise(resolve => setTimeout(resolve,3000))]);
    if (child.exitCode === null) child.kill();
    await fs.writeFile(path.join(root,'stderr.log'),stderr);
}
