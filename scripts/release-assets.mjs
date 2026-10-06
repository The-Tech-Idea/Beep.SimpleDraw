import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import zlib from 'node:zlib';
import {fileURLToPath} from 'node:url';

const sha256 = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const integrity = bytes => 'sha256-'+crypto.createHash('sha256').update(bytes).digest('base64');
export function validateBasePath(base) {
  if (typeof base !== 'string' || !/^\/(?:[A-Za-z0-9_-]+\/)*$/.test(base)) throw new Error('Base path must be / or slash-terminated ASCII segments, such as /draw/.');
  return base;
}
function assetPath(root, name) {
  if (typeof name !== 'string' || !name || name.includes('\\') || name.includes('%') || name.split('/').some(p=>p==='..'||p==='.'||!p)) throw new Error(`Unsafe asset URL: ${name}`);
  const file = path.resolve(root,name);
  if (!file.startsWith(path.resolve(root)+path.sep)) throw new Error(`Asset URL escapes publish: ${name}`);
  return file;
}
export function readManifest(root) {
  const text = fs.readFileSync(path.join(root,'service-worker-assets.js'),'utf8');
  const match = /^self\.assetsManifest\s*=\s*([\s\S]+);\s*$/.exec(text);
  if (!match) throw new Error('Unexpected offline manifest syntax.');
  const manifest = JSON.parse(match[1]);
  if (!Array.isArray(manifest.assets) || typeof manifest.version !== 'string') throw new Error('Invalid offline manifest.');
  return manifest;
}
function writeEncoded(file, bytes) {
  fs.writeFileSync(file,bytes);
  // Publish provides these variants. Regenerate them whenever their identity file changes.
  for (const [ext, encode] of [['.br',zlib.brotliCompressSync],['.gz',zlib.gzipSync]]) {
    if (fs.existsSync(file+ext)) fs.writeFileSync(file+ext,encode(bytes));
  }
}
export function finalizeAssets(root, basePath='/') {
  validateBasePath(basePath);
  const manifest = readManifest(root);
  // Check the SDK payload before touching it. Finalization is not an integrity repair tool.
  for(const asset of manifest.assets) {
    if(asset.hash!==integrity(fs.readFileSync(assetPath(root,asset.url)))) throw new Error(`Offline integrity mismatch before finalization: ${asset.url}`);
  }
  const indexPath = path.join(root,'index.html');
  const index = fs.readFileSync(indexPath,'utf8');
  if ((index.match(/<base href="[^"]*"\s*\/>/g)||[]).length !== 1) throw new Error('Expected one published base href.');
  // HTML parsing normalizes CR/CRLF to LF. Emit canonical LF so inline CSP hashes
  // match the parsed script bytes on Windows and other build hosts.
  writeEncoded(indexPath,Buffer.from(index.replace(/\r\n?/g,'\n').replace(/<base href="[^"]*"\s*\/>/,`<base href="${basePath}" />`)));
  if (!manifest.assets.some(a=>a.url==='index.html')) throw new Error('Offline manifest has no entry document.');
  manifest.assets = manifest.assets.map(asset=>asset.url==='index.html'?{...asset,hash:integrity(fs.readFileSync(indexPath))}:asset);
  // Version comes from all payload hashes including the edited entry document. No timestamp.
  manifest.version = sha256(JSON.stringify(manifest.assets)).slice(0,16);
  writeEncoded(path.join(root,'service-worker-assets.js'),Buffer.from('self.assetsManifest = '+JSON.stringify(manifest,null,2)+';\n'));
  // The SDK copies PublishedContent to service-worker.js after the compression target.
  // Re-encode this generated worker from its final identity file, which is outside the asset list.
  const worker=path.join(root,'service-worker.js');
  writeEncoded(worker,fs.readFileSync(worker));
  return verifyAssets(root,basePath);
}
function filesIn(root) {
  const files=[];
  function walk(dir) {
    for(const entry of fs.readdirSync(dir,{withFileTypes:true})) {
      const full=path.join(dir,entry.name);
      if(entry.isSymbolicLink()) throw new Error('Publish may not contain symlinks.');
      if(entry.isDirectory()) walk(full); else files.push(path.relative(root,full).split(path.sep).join('/'));
    }
  }
  walk(root); return files.sort();
}
export function verifyAssets(root, basePath='/') {
  validateBasePath(basePath);
  const manifest = readManifest(root), seen = new Set();
  for(const asset of manifest.assets) {
    if(seen.has(asset.url)) throw new Error(`Duplicate offline asset: ${asset.url}`);
    seen.add(asset.url);
    if(asset.hash !== integrity(fs.readFileSync(assetPath(root,asset.url)))) throw new Error(`Offline integrity mismatch: ${asset.url}`);
  }
  const names=filesIn(root), records=[];
  for(const name of names.filter(name=>!name.endsWith('.br')&&!name.endsWith('.gz'))) {
    const file=assetPath(root,name), bytes=fs.readFileSync(file);
    const sizes={identity:bytes.length,br:bytes.length,gzip:bytes.length};
    for(const [ext,decode,key] of [['.br',zlib.brotliDecompressSync,'br'],['.gz',zlib.gunzipSync,'gzip']]) {
      if(fs.existsSync(file+ext)) {
        const encoded=fs.readFileSync(file+ext);
        if(!decode(encoded).equals(bytes)) throw new Error(`Compressed asset mismatch: ${name+ext}`);
        sizes[key]=encoded.length;
      }
    }
    records.push({path:name,sha256:sha256(bytes),...sizes});
  }
  for (const encoded of names.filter(name=>name.endsWith('.br')||name.endsWith('.gz'))) {
    if(!fs.existsSync(assetPath(root,encoded.slice(0,-3)))) throw new Error(`Orphan compressed asset: ${encoded}`);
  }
  for(const required of ['index.html','fonts/Kalam-Regular.ttf','fonts/OFL.txt','manifest.webmanifest','css/app.css','js/documents.js']) {
    if(!seen.has(required)) throw new Error(`Required offline asset missing: ${required}`);
  }
  if(!records.some(r=>/^_framework\/dotnet\.native\.[^.]+\.wasm$/.test(r.path))) throw new Error('Missing native-linked WASM payload.');
  const index = fs.readFileSync(path.join(root,'index.html'),'utf8');
  if(!index.includes(`<base href="${basePath}" />`)) throw new Error('Published base path differs from its release input.');
  const refs=[...index.matchAll(/(?:src|href)="([^"]+)"/g)].map(m=>m[1]);
  if(refs.some(ref=>/^(?:https?:|\/\/)/i.test(ref))) throw new Error('Entry document contains an external runtime resource.');
  const imports = JSON.parse(/<script type="importmap">([\s\S]*?)<\/script>/.exec(index)?.[1]??'null');
  if(!imports?.imports || !imports?.integrity) throw new Error('Missing resolved import map.');
  for(const url of Object.values(imports.imports)) {
    if(typeof url!=='string'||!url.startsWith('./')) throw new Error('External or unresolved import map target.');
    if(imports.integrity[url]!==integrity(fs.readFileSync(assetPath(root,url.slice(2))))) throw new Error(`Import integrity mismatch: ${url}`);
  }
  for(const tag of index.matchAll(/<link\b[^>]*\bintegrity="([^"]+)"[^>]*>/g)) {
    const href=/href="([^"]+)"/.exec(tag[0])?.[1];
    if(!href||tag[1]!==integrity(fs.readFileSync(assetPath(root,href)))) throw new Error('Preload integrity mismatch.');
  }
  const inline=[...index.matchAll(/<script([^>]*)>([\s\S]*?)<\/script>/g)].filter(m=>!m[1].includes('src=')).map(m=>`'${integrity(Buffer.from(m[2].replace(/\r\n?/g,'\n')))}'`);
  const csp=`default-src 'self'; script-src 'self' 'wasm-unsafe-eval' ${inline.join(' ')}; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self'; connect-src 'self'; worker-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'`;
  const totals=records.reduce((sum,r)=>({identity:sum.identity+r.identity,br:sum.br+r.br,gzip:sum.gzip+r.gzip}),{identity:0,br:0,gzip:0});
  if(totals.br>25*1024*1024) throw new Error(`Whole artifact Brotli inventory exceeds 25 MiB: ${totals.br}`);
  return {version:1,basePath,offlineVersion:manifest.version,offlineAssets:seen.size,files:records,totals,csp,budget:{compressedInventoryLimit:25*1024*1024,passed:true,scope:'Whole wwwroot inventory using .br when present, identity otherwise. Not an observed cold network transfer.'}};
}
if(process.argv[1] && path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  try {
    const [operation,root,basePath='/',output]=process.argv.slice(2);
    if(!root||!['finalize','verify'].includes(operation)) throw new Error('Use: node scripts/release-assets.mjs finalize|verify wwwroot [basePath] [report.json]');
    const report=operation==='finalize'?finalizeAssets(root,basePath):verifyAssets(root,basePath);
    if(output) fs.writeFileSync(output,JSON.stringify(report,null,2)+'\n');
    console.log(JSON.stringify({basePath:report.basePath,offlineVersion:report.offlineVersion,offlineAssets:report.offlineAssets,totals:report.totals,budget:report.budget}));
  } catch(error) {console.error(error.message);process.exitCode=1;}
}
