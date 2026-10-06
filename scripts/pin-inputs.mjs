import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

export const appRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
export const sourceRoots = ['Beep.Skia','Beep.Skia.Model','Beep.Skia.FlowChart','Beep.Skia.Blazor.Controls','Beep.Skia.UML','Beep.Skia.ERD','Beep.Skia.MindMap','Beep.Skia.Network','Beep.Skia.Business','Beep.Skia.Cloud','Beep.Skia.DFD','Beep.Skia.ECAD','Beep.Skia.ETL','Beep.Skia.ML','Beep.Skia.PM','Beep.Skia.Security','Beep.Skia.StateMachine','Beep.Ski.Quantitative','Beep.Skia.WellLogs'];
const fixedFiles = ['Directory.Build.props','README.md','LICENSE.txt','Beep.Skia.Tests/FlowchartSimulatorTests.cs','Beep.Skia.Tests/FlowchartCodeGeneratorTests.cs'];
const textExtensions = new Set(['.cs','.csproj','.props','.targets','.razor','.js','.json','.md','.txt','.xml','.resx','.css','.svg','.html']);
const packageInputs = [
  {id:'TheTechIdea.Beep.DataManagementModels',version:'3.1.1',sha256:'5a9ac5f7fbf9581d99a7ce871ddb36ff62f6b0b79fe6ec110dce4d75aa52f46f',repository:'https://github.com/The-Tech-Idea/BeepDM',commit:'f305eaf64037392fa4b907aa69f64f5d641c69d4'},
  {id:'TheTechIdea.Beep.Vis.Modules',version:'2.0.44',sha256:'0e2c8c5809846dca31b6fa3c07f6487165337e3e21a002d0e60124731c13e718',repository:'https://github.com/The-Tech-Idea/BeepEnterprize.winform',commit:'9b62321ff640ebcc8bb6cca1f83e675a29a92215'}
];
const digest = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const safePath = (root, relative) => {
  if (!relative || relative.includes('\\') || relative.split('/').some(p=>p==='..'||p==='.'||!p)) throw new Error(`Invalid input path: ${relative}`);
  const full = path.resolve(root, relative);
  if (!full.startsWith(path.resolve(root)+path.sep)) throw new Error(`Input escapes its root: ${relative}`);
  return full;
};
function contentHash(file, mode) {
  const bytes = fs.readFileSync(file);
  return digest(mode === 'text-lf' ? new TextDecoder('utf-8',{fatal:true}).decode(bytes).replaceAll('\r\n','\n') : bytes);
}
function inventory(root) {
  const files = [...fixedFiles];
  const walk = relative => {
    for (const entry of fs.readdirSync(safePath(root, relative),{withFileTypes:true})) {
      if (['bin','obj','.git'].includes(entry.name) || entry.name==='packages.lock.json') continue;
      const name = relative+'/'+entry.name;
      if (entry.isSymbolicLink()) throw new Error(`Source symlink is not a pinned build input: ${name}`);
      if (entry.isDirectory()) walk(name); else files.push(name);
    }
  };
  sourceRoots.forEach(walk);
  return [...new Set(files)].sort().map(name=>{
    const mode = textExtensions.has(path.extname(name)) ? 'text-lf' : 'binary';
    return {path:name,mode,sha256:contentHash(safePath(root,name),mode)};
  });
}
export function verifyInputs(skiaRoot, root = appRoot) {
  const lock = JSON.parse(fs.readFileSync(path.join(root,'dependencies/beep-skia.lock.json'),'utf8'));
  if (lock.version !== 1 || lock.baseCommit !== '19bb026ba403ff91bd1a2ce324000ddce07cd26d') throw new Error('Unknown Beep source lock. Review its base commit before changing the verifier.');
  if (digest(fs.readFileSync(path.join(root,'dependencies/beep-skia.patch'))) !== lock.patchSha256) throw new Error('Beep patch hash differs from the lock.');
  for (const pkg of packageInputs) {
    const name = `${pkg.id.toLowerCase()}.${pkg.version}.nupkg`;
    if (digest(fs.readFileSync(path.join(root,'dependencies/packages',name))) !== pkg.sha256) throw new Error(`Pinned package changed: ${name}`);
  }
  const actual = inventory(skiaRoot), expected = lock.files;
  if (JSON.stringify(actual) !== JSON.stringify(expected)) {
    const differences = actual.filter(f=>!expected.some(e=>e.path===f.path&&e.mode===f.mode&&e.sha256===f.sha256)).map(f=>f.path);
    const missing = expected.filter(e=>!actual.some(f=>f.path===e.path)).map(e=>e.path);
    throw new Error(`Beep source differs from the reviewed lock. Changed/new: ${differences.slice(0,8).join(', ')}; missing: ${missing.slice(0,8).join(', ')}. Existing source has not been changed.`);
  }
  for (const name of lock.addedFiles) {
    const source = safePath(path.join(root,'dependencies/beep-skia-added'),name);
    const expectedFile = expected.find(f=>f.path===name);
    if (!expectedFile || contentHash(source,expectedFile.mode)!==expectedFile.sha256) throw new Error(`Pinned added source changed: ${name}`);
  }
  return {baseCommit:lock.baseCommit,sourceFiles:actual.length,sourceSha256:digest(JSON.stringify(actual)),packages:packageInputs};
}
function refresh(skiaRoot) {
  const baseCommit = '19bb026ba403ff91bd1a2ce324000ddce07cd26d';
  const head = execFileSync('git',['-C',skiaRoot,'rev-parse','HEAD'],{encoding:'utf8'}).trim();
  if (head !== baseCommit) throw new Error('Refresh requires the reviewed base commit; do not silently rebase the source pin.');
  for (const dir of sourceRoots) if (!fs.existsSync(path.join(skiaRoot,dir,'packages.net10.lock.json'))) throw new Error(`Missing successful net10 NuGet lock: ${dir}`);
  const files = inventory(skiaRoot);
  const tracked = new Set(execFileSync('git',['-C',skiaRoot,'ls-files','-z'],{encoding:'utf8'}).split('\0'));
  const addedFiles = files.filter(f=>!tracked.has(f.path)).map(f=>f.path);
  const patch = execFileSync('git',['-C',skiaRoot,'diff','--binary',baseCommit,'--',...sourceRoots,...fixedFiles],{maxBuffer:20*1024*1024});
  const dependencyDir = path.join(appRoot,'dependencies'); fs.mkdirSync(dependencyDir,{recursive:true});
  fs.writeFileSync(path.join(dependencyDir,'beep-skia.patch'),patch);
  for (const name of addedFiles) {
    const destination = safePath(path.join(dependencyDir,'beep-skia-added'),name); fs.mkdirSync(path.dirname(destination),{recursive:true});
    fs.copyFileSync(safePath(skiaRoot,name),destination);
  }
  const lock = {version:1,repository:'https://github.com/The-Tech-Idea/Beep.Skia.git',baseCommit,patchSha256:digest(patch),sourceRoots,fixedFiles,addedFiles,files};
  fs.writeFileSync(path.join(dependencyDir,'beep-skia.lock.json'),JSON.stringify(lock,null,2)+'\n');
  fs.writeFileSync(path.join(dependencyDir,'toolchain.lock.json'),JSON.stringify({version:1,sdk:'10.0.401',node:'24.14.0',wasmManifest:'10.0.112',wasmPack:'10.0.12',emscripten:'3.1.56',emscriptenPack:'10.0.12',configuration:'Release',framework:'net10.0',nugetLockFile:'packages.net10.lock.json',packages:packageInputs},null,2)+'\n');
  return verifyInputs(skiaRoot);
}
if (process.argv[1] && path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  try {
    const operation = process.argv[2], skiaRoot = path.resolve(process.argv[3] ?? path.join(appRoot,'../Beep.Skia'));
    const result = operation==='refresh' ? refresh(skiaRoot) : operation==='verify' ? verifyInputs(skiaRoot) : (()=>{throw new Error('Use: node scripts/pin-inputs.mjs verify|refresh [Beep.Skia path]');})();
    console.log(JSON.stringify(result));
  } catch (error) { console.error(error.message); process.exitCode=1; }
}
