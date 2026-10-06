import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import {appRoot} from './pin-inputs.mjs';
const digest=(bytes,algorithm='sha256',encoding='hex')=>crypto.createHash(algorithm).update(bytes).digest(encoding);
export function lockedPackages(root=appRoot) {
  const projects=['src/Beep.SimpleDraw.Core','src/Beep.SimpleDraw.Web','tests/Beep.SimpleDraw.Tests'].map(p=>path.join(root,p));
  projects.push(...['Beep.Skia','Beep.Skia.Model','Beep.Skia.FlowChart','Beep.Skia.Blazor.Controls','Beep.Skia.UML','Beep.Skia.ERD','Beep.Skia.MindMap','Beep.Skia.Network','Beep.Skia.Business','Beep.Skia.Cloud','Beep.Skia.DFD','Beep.Skia.ECAD','Beep.Skia.ETL','Beep.Skia.ML','Beep.Skia.PM','Beep.Skia.Security','Beep.Skia.StateMachine','Beep.Ski.Quantitative','Beep.Skia.WellLogs'].map(p=>path.resolve(root,'../Beep.Skia',p)));
  const packages=new Map();
  for(const project of projects) {
    const lock=JSON.parse(fs.readFileSync(path.join(project,'packages.net10.lock.json'),'utf8'));
    for(const items of Object.values(lock.dependencies)) for(const [id,pkg] of Object.entries(items)) {
      if(pkg.type==='Project') continue;
      const key=id.toLowerCase()+'/'+pkg.resolved;
      if(packages.has(key)&&packages.get(key).contentHash!==pkg.contentHash) throw new Error('Conflicting package content hashes: '+key);
      packages.set(key,{id,version:pkg.resolved,contentHash:pkg.contentHash});
    }
  }
  return [...packages.values()].sort((a,b)=>(a.id.toLowerCase()+'/'+a.version).localeCompare(b.id.toLowerCase()+'/'+b.version));
}
export function verifyPackageArchives(packageRoot, root=appRoot) {
  const expected=JSON.parse(fs.readFileSync(path.join(root,'dependencies/nuget-archives.lock.json'),'utf8'));
  if(expected.version!==1||JSON.stringify(expected.packages.map(({archiveSha256,...pkg})=>pkg))!==JSON.stringify(lockedPackages(root))) throw new Error('Package archive pin does not match all application and Beep.Skia NuGet locks. Review and refresh the archive pin explicitly.');
  for(const pkg of expected.packages) {
    const name=pkg.id.toLowerCase(),dir=path.join(packageRoot,name,pkg.version),archive=path.join(dir,`${name}.${pkg.version}.nupkg`);
    const bytes=fs.readFileSync(archive);
    if(digest(bytes)!==pkg.archiveSha256) throw new Error('Package archive SHA256 differs from its pin: '+pkg.id+'/'+pkg.version);
    const metadata=JSON.parse(fs.readFileSync(path.join(dir,'.nupkg.metadata'),'utf8'));
    if(metadata.contentHash!==pkg.contentHash) throw new Error('Package metadata differs from the NuGet lock: '+pkg.id);
    // Signed NuGet contentHash excludes signing metadata; raw archive SHA512 is a separate value.
    if(fs.readFileSync(archive+'.sha512','utf8').trim()!==digest(bytes,'sha512','base64')) throw new Error('Package archive SHA512 differs from its extraction record: '+pkg.id);
  }
  return expected.packages;
}
function refresh(packageRoot) {
  const packages=lockedPackages().map(pkg=>({...pkg,archiveSha256:digest(fs.readFileSync(path.join(packageRoot,pkg.id.toLowerCase(),pkg.version,`${pkg.id.toLowerCase()}.${pkg.version}.nupkg`)))}));
  fs.writeFileSync(path.join(appRoot,'dependencies/nuget-archives.lock.json'),JSON.stringify({version:1,packages},null,2)+'\n');
  return verifyPackageArchives(packageRoot);
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  try {
    const [operation,packageRoot]=process.argv.slice(2);
    if(!packageRoot||!['verify','refresh'].includes(operation)) throw new Error('Use: node scripts/pin-packages.mjs verify|refresh packagesPath');
    const packages=operation==='refresh'?refresh(packageRoot):verifyPackageArchives(packageRoot);
    console.log(JSON.stringify({verifiedArchives:packages.length}));
  } catch(error) {console.error(error.message);process.exitCode=1;}
}
