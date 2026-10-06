import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {appRoot} from './pin-inputs.mjs';
import {hash,noticeFiles,verifyNoticeBundle} from './prepare-notices.mjs';
import {readManifest} from './release-assets.mjs';
import crypto from 'node:crypto';

export function classifyPayload(name, packages, sdkAssemblies, groups) {
    const sources=[];
    const add=(source,notices)=>{for(const id of notices) if(!groups.has(id)) throw new Error('Runtime source has no full notices: '+id);sources.push({source,noticeGroups:notices});};
    const mono='sdk:mono/10.0.12', ems='sdk:emscripten/3.1.56', skia='package:skiasharp.nativeassets.webassembly/4.150.1';
    if(/^_framework\/dotnet\.native\.[a-z0-9]{10}\.wasm$/i.test(name)) {
        // AOT places managed source/package code in this native payload as well. Cover every
        // resolved runtime candidate conservatively; trimming may remove individual assemblies.
        const notices=[...new Set([mono,ems,skia,'source:beep-skia',
            ...[...packages.values()].map(pkg=>'package:'+pkg.toLowerCase())])].sort();
        add('Linked browser native runtime and conservatively covered managed AOT inputs',notices);
        sources[0].scope='All resolved managed runtime candidates and reviewed Beep/application source; no assembly/function-level inclusion claim.';
    }
    else if(/^_framework\/.+\.[a-z0-9]{10}\.wasm$/i.test(name)) {
        const assembly=path.posix.basename(name).replace(/\.[a-z0-9]{10}\.wasm$/i,'.dll');
        const pkg=packages.get(assembly);
        if(pkg) add(pkg,['package:'+pkg.toLowerCase()]);
        else if(['Beep.Skia.dll','Beep.Skia.Model.dll','Beep.Skia.FlowChart.dll','Beep.Skia.Blazor.Controls.dll','Beep.Skia.UML.dll','Beep.Skia.ERD.dll','Beep.Skia.MindMap.dll','Beep.Skia.Network.dll','Beep.Skia.Business.dll','Beep.Skia.Cloud.dll','Beep.Skia.DFD.dll','Beep.Skia.ECAD.dll','Beep.Skia.ETL.dll','Beep.Skia.ML.dll','Beep.Skia.PM.dll','Beep.Skia.Security.dll','Beep.Skia.StateMachine.dll','Beep.Ski.Quantitative.dll','Beep.Skia.WellLogs.dll'].includes(assembly)) add('Beep.Skia reviewed source',['source:beep-skia']);
        else if(['Beep.SimpleDraw.Core.dll','Beep.SimpleDraw.Web.dll'].includes(assembly)) add('Beep.SimpleDraw application-owned source',[]);
        else if(sdkAssemblies.has(assembly)) add('Microsoft.NETCore.App.Runtime.Mono.browser-wasm/10.0.12',[mono]);
        else throw new Error('Published managed assembly has no reviewed source/notice mapping: '+assembly);
    } else if(/^_framework\/blazor\.webassembly\.[a-z0-9]{10}\.js$/i.test(name)) add('Microsoft.AspNetCore.Components.WebAssembly/10.0.12',['package:microsoft.aspnetcore.components.webassembly/10.0.12']);
    else if(/^_framework\/dotnet(?:\.(?:native|runtime))?\.[a-z0-9]{10}\.js$/i.test(name)) add('SDK browser runtime JavaScript',[mono,ems]);
    else if(/^_framework\/icudt_(?:CJK|EFIGS|no_CJK)\.[a-z0-9]{10}\.dat$/.test(name)) add('SDK ICU/Unicode globalization data',[mono]);
    else if(name.startsWith('_content/Beep.Skia.Blazor.Controls/')) add('Beep.Skia Blazor host source',['source:beep-skia']);
    else if(name.startsWith('_content/SkiaSharp.Views.Blazor/')) add('SkiaSharp.Views.Blazor/4.150.1',['package:skiasharp.views.blazor/4.150.1']);
    else if(['fonts/Kalam-Regular.ttf','fonts/OFL.txt'].includes(name)) add('Bundled Kalam Regular',['font:kalam']);
    else throw new Error('Published framework/content/font payload has no reviewed notice mapping: '+name);
    return sources;
}
export function verifyRuntimeNotices(wwwroot,sdkRoot,root=appRoot) {
    const bundle=verifyNoticeBundle(path.join(wwwroot,'licenses'),root,sdkRoot);
    const {manifest,files}=noticeFiles(root,sdkRoot), groups=new Map(manifest.groups.map(g=>[g.id,g]));
    const offline=readManifest(wwwroot), offlineEntries=new Map(offline.assets.map(a=>[a.url,a]));
    if(offlineEntries.size!==offline.assets.length) throw new Error('Duplicate offline notice/payload URL.');
    const verifyOffline=(name,bytes)=>{
        const record=offlineEntries.get(name);
        if(!record) throw new Error('Required notice/payload is absent from the offline manifest: '+name);
        const integrity='sha256-'+crypto.createHash('sha256').update(bytes).digest('base64');
        if(record.hash!==integrity) throw new Error('Offline notice/payload integrity mismatch: '+name);
    };
    for(const [name,bytes] of files) verifyOffline('licenses/'+name,bytes);
    // The notice page's offline download must retain both its handler and the
    // editor's shared byte-download helper, not only the visible notice texts.
    for(const name of ['js/notices.js','js/app.js']) {
        const original=fs.readFileSync(path.join(root,'src/Beep.SimpleDraw.Web/wwwroot',name));
        const published=fs.readFileSync(path.join(wwwroot,name));
        if(!published.equals(original)) throw new Error('Published notice download module differs from app source: '+name);
        verifyOffline(name,published);
    }
    if(hash(fs.readFileSync(path.join(wwwroot,'fonts/Kalam-Regular.ttf')))!==manifest.fontSha256) throw new Error('Published font differs from its licensed input.');
    if(!fs.readFileSync(path.join(wwwroot,'fonts/OFL.txt')).equals(fs.readFileSync(path.join(root,'src/Beep.SimpleDraw.Core/Fonts/OFL.txt')))) throw new Error('Published font license differs from its original.');
    const assets=JSON.parse(fs.readFileSync(path.join(root,'src/Beep.SimpleDraw.Web/obj/project.assets.json'),'utf8'));
    const target=assets.targets['net10.0/browser-wasm'];
    if(!target) throw new Error('Missing resolved browser-wasm runtime target.');
    const packages=new Map();
    for(const [key,pkg] of Object.entries(target)) if(pkg.type==='package') for(const item of Object.keys(pkg.runtime??{})) {
        if(!item.endsWith('.dll')) continue;
        const name=path.posix.basename(item), previous=packages.get(name);
        if(previous&&previous!==key) throw new Error('Ambiguous package runtime source: '+name);
        packages.set(name,key);
    }
    const sdkDirectory=path.join(sdkRoot,'packs/Microsoft.NETCore.App.Runtime.Mono.browser-wasm/10.0.12/runtimes/browser-wasm/lib/net10.0');
    const sdkAssemblies=new Set(fs.readdirSync(sdkDirectory).filter(n=>n.endsWith('.dll')));
    // CoreLib is a managed assembly carried by the SDK's native runtime directory.
    const coreLib='runtimes/browser-wasm/native/System.Private.CoreLib.dll';
    const runtimePack=path.join(sdkRoot,'packs/Microsoft.NETCore.App.Runtime.Mono.browser-wasm/10.0.12');
    const runtimeList=fs.readFileSync(path.join(runtimePack,'data/RuntimeList.xml'),'utf8');
    if(!runtimeList.includes('Path="'+coreLib+'" AssemblyName="System.Private.CoreLib"')||!fs.statSync(path.join(runtimePack,coreLib)).isFile())
        throw new Error('SDK CoreLib runtime input is missing or unrecognized.');
    sdkAssemblies.add('System.Private.CoreLib.dll');
    const payloads=[];
    for(const dir of ['_framework','_content','fonts']) {
        function walk(relative) {
            for(const e of fs.readdirSync(path.join(wwwroot,relative),{withFileTypes:true})) {
                const name=relative+'/'+e.name;
                if(e.isSymbolicLink()) throw new Error('Runtime payload symlinks are not supported.');
                if(e.isDirectory()) walk(name);
                else if(!name.endsWith('.br')&&!name.endsWith('.gz')) {
                    const bytes=fs.readFileSync(path.join(wwwroot,name));
                    verifyOffline(name,bytes);
                    payloads.push({path:name,bytes:bytes.length,sha256:hash(bytes),sources:classifyPayload(name,packages,sdkAssemblies,groups)});
                }
            }
        }
        walk(dir);
    }
    const used=[...new Set(payloads.flatMap(p=>p.sources.flatMap(s=>s.noticeGroups)))].sort();
    return {version:1,noticeInputsSha256:hash(fs.readFileSync(path.join(root,'dependencies/notices/inputs.lock.json'))),bundle,
        payloads:payloads.sort((a,b)=>a.path.localeCompare(b.path)),usedNoticeGroups:used,
        conservativeAdditionalNoticeGroups:[...groups.keys()].filter(id=>!used.includes(id)).sort(),
        scope:'Actual published framework/content/font paths and SHA256, mapped to resolved NuGet runtime item names, exact SDK runtime assembly names or reviewed source. Linked native vendor notices are retained in full; no subcomponent/function-level trimming claim. Independent rights/header acceptance remains open.'};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
    try {
        const [wwwroot,sdkRoot,output]=process.argv.slice(2);
        if(!wwwroot||!sdkRoot||!output) throw new Error('Use: node scripts/release-notices.mjs wwwroot dotnetRoot output.json');
        const result=verifyRuntimeNotices(wwwroot,sdkRoot);
        fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n');
        console.log(JSON.stringify({runtimePayloads:result.payloads.length,usedNoticeGroups:result.usedNoticeGroups.length,bundle:result.bundle}));
    } catch(error) {console.error(error.message);process.exitCode=1;}
}
