// Loopback test wrapper: keep the verified shipped runtime byte-for-byte intact.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {createStaticServer} from '../scripts/serve-static.mjs';
const [root,reportPath,portText]=process.argv.slice(2),port=Number(portText);
if(!root||!reportPath||!Number.isInteger(port)||port<1024||port>65535) throw new Error('Use: node tests/serve-browser-faults.mjs wwwroot report.json port');
const server=createStaticServer(root,JSON.parse(fs.readFileSync(reportPath,'utf8')));
const runtime=server.listeners('request')[0];
server.removeListener('request',runtime);
const directory=path.dirname(fileURLToPath(import.meta.url));
server.on('request',(request,response)=>{
  const pathname=request.url.split('?')[0];
  const files={'/__owned-tests/storage/':'browser-faults.html','/__owned-tests/storage/browser-faults.js':'browser-faults.js'};
  if(!Object.hasOwn(files,pathname)) return runtime(request,response);
  if(!['GET','HEAD'].includes(request.method)) {response.writeHead(405);return response.end();}
  const content=fs.readFileSync(path.join(directory,files[pathname]));
  response.writeHead(200,{'Content-Type':pathname.endsWith('.js')?'text/javascript; charset=utf-8':'text/html; charset=utf-8','Content-Length':content.length,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff','Content-Security-Policy':"default-src 'none'; script-src 'self'; base-uri 'none'; frame-ancestors 'none'"});
  response.end(request.method==='HEAD'?undefined:content);
});
server.listen(port,'127.0.0.1',()=>console.log(`Owned fault harness: http://127.0.0.1:${port}/__owned-tests/storage/?appPath=/draw/&channel=library`));
