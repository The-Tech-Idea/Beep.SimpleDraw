import {spawn} from 'node:child_process';
import readline from 'node:readline';
import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';

const [client, directory, evidence] = process.argv.slice(2);
assert.ok(['codex','claude'].includes(client));
const root = path.resolve(directory), work = path.resolve(evidence);
await fs.mkdir(work, {recursive:true});
const config = JSON.parse(await fs.readFile(path.join(root,'.mcp.json'),'utf8')).mcpServers.simpledraw;
const expand = value => value.replaceAll('${CLAUDE_PLUGIN_ROOT}',root);
const cwd = config.cwd ? path.resolve(root,config.cwd) : work;
const args = config.args.map(expand);
const dll = path.resolve(cwd,args[0]);
const serverHash = createHash('sha256').update(await fs.readFile(dll)).digest('hex');
const child = spawn(expand(config.command),[...args,'--workspace',path.join(work,'workspace')],{cwd,stdio:['pipe','pipe','pipe']});
let sequence=0, stderr='', badLine;
const pending=new Map();
child.stderr.on('data',b=>stderr+=b);
child.on('error',error=>{for(const p of pending.values())p.reject(error);pending.clear();});
readline.createInterface({input:child.stdout}).on('line',line=>{
    try {const r=JSON.parse(line);const p=pending.get(r.id);if(p){pending.delete(r.id);r.error?p.reject(new Error(JSON.stringify(r.error))):p.resolve(r.result);}}
    catch {badLine=line;}
});
const request=(method,params={})=>new Promise((resolve,reject)=>{
    const id=++sequence;
    const timer=setTimeout(()=>{pending.delete(id);reject(new Error(`Timeout ${method}: ${stderr.slice(-1500)}`));},15000);
    pending.set(id,{resolve:v=>{clearTimeout(timer);resolve(v);},reject:e=>{clearTimeout(timer);reject(e);}});
    child.stdin.write(JSON.stringify({jsonrpc:'2.0',id,method,params})+'\n');
});
const call=async(name,args={})=>{const r=await request('tools/call',{name,arguments:args});assert.equal(r.isError??false,false,JSON.stringify(r));return r;};
const data=async(name,args)=>JSON.parse((await call(name,args)).content.find(c=>c.type==='text').text);
try {
    await request('initialize',{protocolVersion:'2025-06-18',capabilities:{},clientInfo:{name:'Plugin package acceptance',version:'1.0'}});
    child.stdin.write(JSON.stringify({jsonrpc:'2.0',method:'notifications/initialized'})+'\n');
    assert.equal((await request('tools/list')).tools.length,31);
    const catalog=await data('list_components');assert.equal(catalog.total,249);assert.equal(catalog.families.length,16);
    const schema=await data('describe_component',{type:'Beep.Skia.Flowchart.ProcessNode'});assert.ok(schema.properties.length);
    let board=await data('create_drawing',{name:`${client} installed plugin acceptance`});
    board=await data('add_component',{drawingId:board.id,expectedRevision:board.revision,type:schema.type,x:0,y:0});
    const native=await data('export_drawing',{drawingId:board.id});assert.ok(native.nativeJson.length);
    const png=await data('export_drawing',{drawingId:board.id,format:'png'});
    assert.equal((await fs.readFile(png.path))[0],137);
    const rendered=await call('render_drawing',{drawingId:board.id});assert.ok(rendered.content.some(c=>c.type==='image'));
    const resources=await request('resources/list');assert.ok(resources.resources.length);
    assert.equal(badLine,undefined);
    const record={client,pluginRoot:root,configuredLauncher:true,serverHash,tools:31,components:249,families:16,propertySchema:true,nativeExport:true,png:true,inlineRender:true,resources:true,stdoutJsonOnly:true};
    await fs.writeFile(path.join(work,'acceptance.json'),JSON.stringify(record,null,2));
    console.log(JSON.stringify(record));
} finally {child.kill();await fs.writeFile(path.join(work,'stderr.log'),stderr);}
