import {spawn} from 'node:child_process';
import http from 'node:http';
import path from 'node:path';
import fs from 'node:fs/promises';
import assert from 'node:assert/strict';
const dll=path.resolve(process.argv[2]??'artifacts/mcp-local-v1/Beep.SimpleDraw.Mcp.dll');
const port=5256;const url=`http://127.0.0.1:${port}/mcp`;
const child=spawn('dotnet',[dll,'--http','--port',String(port),'--workspace',path.resolve('docs/evidence/mcp-http-workspace')],{stdio:['ignore','pipe','pipe']});
let stderr='';child.stderr.on('data',b=>stderr+=b);let seq=0;
const headers={'content-type':'application/json',accept:'application/json, text/event-stream','MCP-Protocol-Version':'2025-06-18'};
async function rpc(method,params) {const response=await fetch(url,{method:'POST',headers,body:JSON.stringify({jsonrpc:'2.0',id:++seq,method,params})});assert.equal(response.status,200,await response.clone().text());const raw=await response.text();const result=raw.startsWith('event:')||raw.startsWith('data:') ? JSON.parse(raw.split('\n').find(l=>l.startsWith('data:')).slice(5)) : JSON.parse(raw);assert.equal(result.error,undefined);return result.result;}
try {
    for(let i=0;i<100;i++){try{await fetch(url);break;}catch(e){if(i===99)throw e;await new Promise(r=>setTimeout(r,100));}}
    const init=await rpc('initialize',{protocolVersion:'2025-06-18',capabilities:{},clientInfo:{name:'SimpleDraw HTTP acceptance',version:'1'}});
    const tools=await rpc('tools/list',{});assert.equal(tools.tools.length,31);
    const result=await rpc('tools/call',{name:'list_components',arguments:{offset:200}});assert.equal(result.isError??false,false);const catalogue=JSON.parse(result.content[0].text);assert.equal(catalogue.total,249);assert.equal(catalogue.components.length,49);
    const foreignOrigin=await fetch(url,{method:'POST',headers:{...headers,origin:'https://unrelated.example'},body:JSON.stringify({jsonrpc:'2.0',id:100,method:'tools/list',params:{}})});assert.equal(foreignOrigin.status,403);
    const foreignHost=await new Promise((resolve,reject)=>{const req=http.request(url,{method:'POST',headers:{...headers,Host:'unrelated.example'}},res=>{res.resume();res.on('end',()=>resolve(res.statusCode));});req.on('error',reject);req.end('{}');});assert.equal(foreignHost,403);
    const malformed=await fetch(url,{method:'POST',headers,body:'{}'});assert.equal(malformed.status,400);
    const evidence={endpoint:url,protocolVersion:init.protocolVersion,tools:tools.tools.length,catalogTotal:catalogue.total,foreignOriginRejected:foreignOrigin.status,foreignHostRejected:foreignHost,malformedJsonRejected:malformed.status};await fs.writeFile('docs/evidence/mcp-http-acceptance.json',JSON.stringify(evidence,null,2));console.log(JSON.stringify(evidence));
} finally {child.kill();await fs.writeFile('docs/evidence/mcp-http-stderr.log',stderr);}
