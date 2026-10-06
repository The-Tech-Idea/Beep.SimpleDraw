import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {fileURLToPath} from 'node:url';
import {verifyAssets} from './release-assets.mjs';
import {ResponseLink} from './network-profile.mjs';

const mime={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.json':'application/json','.map':'application/json','.webmanifest':'application/manifest+json','.css':'text/css; charset=utf-8','.wasm':'application/wasm','.ttf':'font/ttf','.woff':'font/woff','.woff2':'font/woff2','.svg':'image/svg+xml','.png':'image/png','.jpg':'image/jpeg','.ico':'image/x-icon','.txt':'text/plain; charset=utf-8'};
export function createStaticServer(root,report,{networkProfile,onTransfer}={}) {
  const verified=verifyAssets(root,report.basePath);
  if(report.offlineVersion!==verified.offlineVersion||report.csp!==verified.csp) throw new Error('Hosting report differs from the verified artifact.');
  const base=report.basePath,absoluteRoot=fs.realpathSync(root);
  const link=networkProfile?new ResponseLink(networkProfile,{onTransfer}):null;
  return http.createServer((request,response)=>{
    const fail=(status,message)=>{response.writeHead(status,{'Content-Type':'text/plain; charset=utf-8','Cache-Control':'no-store','X-Content-Type-Options':'nosniff'});response.end(message);};
    if(!['GET','HEAD'].includes(request.method)) {response.setHeader('Allow','GET, HEAD');return fail(405,'Static host accepts GET and HEAD.');}
    let pathname;
    try {
      const raw=request.url.split('?')[0];
      if(/%2f|%5c/i.test(raw)) return fail(400,'Encoded path separators are not allowed.');
      pathname=decodeURIComponent(raw);
      if(pathname.includes('\\')||pathname.includes('\0')||pathname.split('/').some(p=>p==='.'||p==='..')) return fail(400,'Invalid static path.');
    } catch {return fail(400,'Invalid URL encoding.');}
    if(base!=='/'&&pathname===base.slice(0,-1)) {response.writeHead(308,{Location:base,'Cache-Control':'no-store'});return response.end();}
    if(!pathname.startsWith(base)) return fail(404,'Outside app scope.');
    let relative=pathname.slice(base.length)||'index.html';
    if(relative.endsWith('/')) relative+='index.html';
    let file=path.resolve(absoluteRoot,relative);
    if(!file.startsWith(absoluteRoot+path.sep)) return fail(400,'Invalid static path.');
    if(!fs.existsSync(file)||!fs.statSync(file).isFile()) {
      if(path.posix.basename(relative).includes('.')) return fail(404,'Static asset not found.');
      relative='index.html';file=path.join(absoluteRoot,relative);
    }
    if(!fs.realpathSync(file).startsWith(absoluteRoot+path.sep)) return fail(403,'Static symlink escapes its root.');
    const accept=(request.headers['accept-encoding']??'').split(',').map(value=>{
      const [name,...params]=value.trim().split(';');const quality=params.find(p=>p.trim().startsWith('q='));
      return {name:name.trim(),q:quality?Number(quality.trim().slice(2)):1};
    }).filter(item=>Number.isFinite(item.q)&&item.q>0&&item.q<=1);
    const variants=[['br','.br'],['gzip','.gz']].filter(([name,suffix])=>accept.some(item=>item.name===name)&&fs.existsSync(file+suffix));
    variants.sort((a,b)=>accept.find(v=>v.name===b[0]).q-accept.find(v=>v.name===a[0]).q);
    const encoded=variants[0];
    const immutable=/\.[a-z0-9]{10}\.[^.]+$/.test(relative);
    const headers={'Content-Type':mime[path.extname(relative)]??'application/octet-stream','Content-Length':fs.statSync(file+(encoded?.[1]??'')).size,'Cache-Control':immutable?'public, max-age=31536000, immutable':'no-cache','Vary':'Accept-Encoding','Content-Security-Policy':verified.csp,'X-Content-Type-Options':'nosniff','Referrer-Policy':'no-referrer','Cross-Origin-Opener-Policy':'same-origin'};
    if(encoded) headers['Content-Encoding']=encoded[0];
    if(link) {
      link.send(file+(encoded?.[1]??''),request,response,headers).catch(()=>response.destroy());
      return;
    }
    response.writeHead(200,headers);
    if(request.method==='HEAD') return response.end();
    const stream=fs.createReadStream(file+(encoded?.[1]??''));
    stream.on('error',()=>response.destroy());stream.pipe(response);
  });
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  try {
    const [root,reportPath,portText='5210']=process.argv.slice(2);
    const port=Number(portText);
    if(!root||!reportPath||!Number.isInteger(port)||port<1024||port>65535) throw new Error('Use: node scripts/serve-static.mjs wwwroot asset-report.json [port 1024..65535]');
    const report=JSON.parse(fs.readFileSync(reportPath,'utf8'));
    const server=createStaticServer(root,report);
    server.on('error',error=>{console.error(error.message);process.exitCode=1;});
    server.listen(port,'127.0.0.1',()=>console.log(`Verified static preview: http://127.0.0.1:${port}${report.basePath}`));
  } catch(error) {console.error(error.message);process.exitCode=1;}
}
