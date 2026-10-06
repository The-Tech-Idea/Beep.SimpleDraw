import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {appRoot,verifyInputs} from './pin-inputs.mjs';
import {verifyPackageArchives} from './pin-packages.mjs';

const [output,packagesPath]=process.argv.slice(2);
if(!output||!packagesPath) throw new Error('Use: node scripts/release-provenance.mjs releaseRoot packagesPath');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
function appSourceFiles() {
  const names=['global.json','NuGet.Config','Directory.Build.props','Beep.SimpleDraw.slnx','README.md','.gitignore'];
  function walk(relative) {
    for(const entry of fs.readdirSync(path.join(appRoot,relative),{withFileTypes:true})) {
      if(['bin','obj','artifacts','.git'].includes(entry.name)) continue;
      if(entry.isSymbolicLink()) throw new Error('App build source may not contain symlinks.');
      const name=relative+'/'+entry.name;
      if(entry.isDirectory()) walk(name);else names.push(name);
    }
  }
  for(const dir of ['src','tests','scripts','docs','plans','dependencies','.github']) walk(dir);
  return [...new Set(names)].sort().map(file=>({path:file,sha256:hash(path.join(appRoot,file))}));
}
const appProjects=['src/Beep.SimpleDraw.Core','src/Beep.SimpleDraw.Web','tests/Beep.SimpleDraw.Tests'];
const skiaRoot=path.resolve(appRoot,'../Beep.Skia');
const source=verifyInputs(skiaRoot);
const inputFiles=['global.json','NuGet.Config','dependencies/toolchain.lock.json','dependencies/beep-skia.lock.json','dependencies/beep-skia.patch','dependencies/nuget-archives.lock.json','dependencies/notices/inputs.lock.json',...appProjects.map(p=>p+'/packages.net10.lock.json')];
const inputs=inputFiles.map(file=>({path:file,sha256:hash(path.join(appRoot,file))}));
const packages=new Map();
verifyPackageArchives(packagesPath);
for(const project of [...appProjects.map(p=>path.join(appRoot,p)),...['Beep.Skia','Beep.Skia.Model','Beep.Skia.FlowChart','Beep.Skia.Blazor.Controls','Beep.Skia.UML','Beep.Skia.ERD','Beep.Skia.MindMap','Beep.Skia.Network','Beep.Skia.Business','Beep.Skia.Cloud','Beep.Skia.DFD','Beep.Skia.ECAD','Beep.Skia.ETL','Beep.Skia.ML','Beep.Skia.PM','Beep.Skia.Security','Beep.Skia.StateMachine','Beep.Ski.Quantitative','Beep.Skia.WellLogs'].map(p=>path.join(skiaRoot,p))]) {
  const lock=JSON.parse(fs.readFileSync(path.join(project,'packages.net10.lock.json'),'utf8'));
  for(const [framework,items] of Object.entries(lock.dependencies)) for(const [id,pkg] of Object.entries(items)) {
    if(pkg.type==='Project') continue;
    const name=id.toLowerCase(),version=pkg.resolved,archive=path.join(packagesPath,name,version,`${name}.${version}.nupkg`);
    const key=id+'/'+version;
    const previous=packages.get(key);
    if(previous&&previous.contentHash!==pkg.contentHash) throw new Error('Conflicting locked package hash: '+key);
    packages.set(key,{id,version,contentHash:pkg.contentHash,archiveSha256:hash(archive),frameworks:[...new Set([...(previous?.frameworks??[]),framework])].sort()});
  }
}
const assetReport=JSON.parse(fs.readFileSync(path.join(output,'asset-report.json'),'utf8'));
const appSource=appSourceFiles();
const provenance={version:1,createdUtc:new Date().toISOString(),source,appSource,appSourceSha256:crypto.createHash('sha256').update(JSON.stringify(appSource)).digest('hex'),toolchain:JSON.parse(fs.readFileSync(path.join(output,'toolchain.json'),'utf8')),inputs,packages:[...packages.values()].sort((a,b)=>(a.id+'/'+a.version).localeCompare(b.id+'/'+b.version)),assetReportSha256:hash(path.join(output,'asset-report.json')),runtimeNoticesSha256:hash(path.join(output,'runtime-notices.json')),basePath:assetReport.basePath,scope:'Pinned source and verified package/artifact/notice inputs. Does not assert hosted CI execution, device acceptance or bit-for-bit archive reproducibility.'};
fs.writeFileSync(path.join(output,'provenance.json'),JSON.stringify(provenance,null,2)+'\n');
console.log(JSON.stringify({packages:packages.size,sourceFiles:source.sourceFiles,basePath:assetReport.basePath}));
