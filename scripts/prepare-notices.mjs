import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {appRoot} from './pin-inputs.mjs';

export const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const escape = text => String(text ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
export function noticeFiles(root=appRoot, sdkRoot) {
    const dir=path.join(root,'dependencies/notices'), manifest=JSON.parse(fs.readFileSync(path.join(dir,'inputs.lock.json'),'utf8'));
    if(manifest.version!==1) throw new Error('Unknown notice-input version.');
    for(const [property,file] of [['packageArchiveLockSha256','dependencies/nuget-archives.lock.json'],['webLockSha256','src/Beep.SimpleDraw.Web/packages.net10.lock.json'],['toolchainLockSha256','dependencies/toolchain.lock.json'],['fontSha256','src/Beep.SimpleDraw.Core/Fonts/Kalam-Regular.ttf']])
        if(hash(fs.readFileSync(path.join(root,file)))!==manifest[property]) throw new Error('Notice inputs differ from the reviewed build input: '+file);
    const files=new Map(), ids=new Set();
    for(const item of manifest.files) {
        if(!/^texts\/[a-f0-9]{64}\.txt$/.test(item.path)||files.has(item.path)||!item.origins?.length) throw new Error('Invalid/duplicate notice input path.');
        const file=path.join(dir,item.path);
        if(fs.lstatSync(file).isSymbolicLink()) throw new Error('Notice input symlinks are not supported.');
        const bytes=fs.readFileSync(file);
        if(hash(bytes)!==item.sha256||bytes.length!==item.bytes) throw new Error('Original notice input changed: '+item.path);
        new TextDecoder('utf-8',{fatal:true}).decode(bytes);
        files.set(item.path,bytes);
        for(const origin of item.origins) {
            if(origin.kind==='sdk-pack'&&sdkRoot) {
                if(!/^[A-Za-z0-9._-]+$/.test(origin.pack)||!/^\d+\.\d+\.\d+$/.test(origin.version)||origin.path.split('/').some(p=>!p||p==='..'||p==='.'||p.includes('\\')))
                    throw new Error('Unsafe SDK notice source.');
                if(hash(fs.readFileSync(path.join(sdkRoot,'packs',origin.pack,origin.version,origin.path)))!==item.sha256)
                    throw new Error('Installed SDK notice differs from its reviewed copy: '+origin.pack+'/'+origin.path);
            }
            if(origin.kind==='beep-source') {
                if(origin.path!=='LICENSE.txt'||origin.baseCommit!=='19bb026ba403ff91bd1a2ce324000ddce07cd26d') throw new Error('Unknown sibling source license input.');
                if(hash(fs.readFileSync(path.resolve(root,'../Beep.Skia',origin.path)))!==item.sha256) throw new Error('Sibling source license differs from its notice copy.');
            }
            if(origin.kind==='font-source') {
                if(origin.path!=='Fonts/OFL.txt'||hash(fs.readFileSync(path.join(root,'src/Beep.SimpleDraw.Core',origin.path)))!==item.sha256)
                    throw new Error('Original bundled font license differs from its notice copy.');
            }
        }
    }
    for(const name of fs.readdirSync(path.join(dir,'texts'))) if(!files.has('texts/'+name)) throw new Error('Unreviewed extra original notice file: '+name);
    for(const group of manifest.groups) {
        if(!group.id||ids.has(group.id)||!group.files?.length||group.files.some(f=>!files.has(f))) throw new Error('Missing/duplicate notice group or full text.');
        ids.add(group.id);
    }
    for(const required of ['sdk:mono/10.0.12','sdk:emscripten/3.1.56','source:beep-skia','font:kalam','package:skiasharp.nativeassets.webassembly/4.150.1'])
        if(!ids.has(required)) throw new Error('Missing linked runtime notice group: '+required);
    const intro='Beep.SimpleDraw — software notices\n\nOriginal full vendor license/notice texts are retained below. Attribution covers the Web dependency candidates and linked SDK/native inputs conservatively; additional notices may describe material not included after trimming/linking. No original license text is translated.\n';
    const headers=manifest.groups.map(g=>`\n${g.title}\nLicense: ${g.license}\n${g.authors?'Authors: '+g.authors+'\n':''}${g.copyright?'Copyright: '+g.copyright+'\n':''}Original text references: ${g.files.join(', ')}\n`).join('');
    const allTexts=[...files].map(([name,bytes])=>'\n'+'='.repeat(80)+'\n'+name+'\n'+'='.repeat(80)+'\n'+new TextDecoder('utf-8',{fatal:true}).decode(bytes)+'\n').join('');
    const sections=manifest.groups.map(g=>`<section><h2>${escape(g.title)}</h2><p>${escape(g.license)}</p>${g.authors?'<p>'+escape(g.authors)+'</p>':''}${g.copyright?'<p>'+escape(g.copyright)+'</p>':''}<ul>${g.files.map((f,i)=>`<li><a href="${f}">Original license or notice text ${i+1}</a></li>`).join('')}</ul></section>`).join('\n');
    const html=`<!doctype html>\n<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>SimpleDraw — software notices</title><style>body{max-width:850px;margin:40px auto;padding:0 24px;font:17px/1.6 system-ui,sans-serif;color:#24324a;background:#fcfcf9}a{color:#6335bf}a:focus-visible,button:focus-visible{outline:3px solid #6335bf;outline-offset:4px}button{font:inherit;min-height:44px;padding:8px 16px;cursor:pointer}section{border-top:1px solid #ddd;padding-top:12px;margin-top:24px}h2{font-size:1.1em}li{margin:8px 0}</style></head><body><main><h1>Software notices</h1><p>SimpleDraw includes third-party software and the Kalam font. Original license and notice texts are available below. Vendor collections are retained in full and may include notices for additional components.</p><p><button type="button" id="download-notices">Download all software notices</button></p><p id="notices-download-status" role="status" aria-live="polite"></p><p><a href="THIRD-PARTY-NOTICES.txt">Read all software notices</a></p>${sections}</main><script type="module" src="../js/notices.js"></script></body></html>\n`;
    files.set('index.html',Buffer.from(html)); files.set('THIRD-PARTY-NOTICES.txt',Buffer.from(intro+headers+allTexts));
    files.set('notices.json',Buffer.from(JSON.stringify({version:1,groups:manifest.groups,files:manifest.files,scope:manifest.scope},null,2)+'\n'));
    return {manifest,files};
}
export function verifyNoticeBundle(directory, root=appRoot, sdkRoot) {
    const {manifest,files}=noticeFiles(root,sdkRoot);
    const actual=[];
    function walk(dir,prefix='') {
        for(const e of fs.readdirSync(dir,{withFileTypes:true})) {
            if(e.isSymbolicLink()) throw new Error('Published notices may not contain symlinks.');
            const relative=prefix+e.name;
            if(e.isDirectory()) walk(path.join(dir,e.name),relative+'/');else if(!relative.endsWith('.br')&&!relative.endsWith('.gz')) actual.push(relative);
        }
    }
    walk(directory);
    if(JSON.stringify(actual.sort())!==JSON.stringify([...files.keys()].sort())) throw new Error('Published notice inventory differs from its reviewed inputs.');
    for(const [name,bytes] of files) if(!fs.readFileSync(path.join(directory,name)).equals(bytes)) throw new Error('Published notice changed: '+name);
    return {groups:manifest.groups.length,originalTexts:manifest.files.length,files:files.size};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
    try {
        const [operation,sdkRoot]=process.argv.slice(2), directory=path.join(appRoot,'src/Beep.SimpleDraw.Web/wwwroot/licenses');
        if(operation==='write') {
            if(fs.existsSync(directory)) throw new Error('Generated notice directory must be absent. Review changes before replacing an existing bundle.');
            const {files}=noticeFiles(appRoot,sdkRoot);
            for(const [name,bytes] of files) {const target=path.join(directory,name);fs.mkdirSync(path.dirname(target),{recursive:true});fs.writeFileSync(target,bytes);}
        } else if(operation!=='verify') throw new Error('Use: node scripts/prepare-notices.mjs write|verify [dotnetRoot]');
        console.log(JSON.stringify(verifyNoticeBundle(directory,appRoot,sdkRoot)));
    } catch(error) {console.error(error.message);process.exitCode=1;}
}
