import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {appRoot,verifyInputs} from '../scripts/pin-inputs.mjs';

test('reviewed inputs refuse drift without changing an existing checkout',async t=>{
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'simpledraw-pins-'));
  t.after(()=>{
    const resolved=path.resolve(root);
    assert.equal(path.dirname(resolved),path.resolve(os.tmpdir()));
    assert.ok(path.basename(resolved).startsWith('simpledraw-pins-'));
    fs.rmSync(resolved,{recursive:true});
  });
  const app=path.join(root,'Beep.SimpleDraw'),skia=path.join(root,'Beep.Skia');
  fs.cpSync(path.join(appRoot,'dependencies'),path.join(app,'dependencies'),{recursive:true});
  const lock=JSON.parse(fs.readFileSync(path.join(app,'dependencies/beep-skia.lock.json'),'utf8'));
  for(const entry of lock.files) {
    const target=path.join(skia,entry.path);fs.mkdirSync(path.dirname(target),{recursive:true});
    fs.copyFileSync(path.resolve(appRoot,'../Beep.Skia',entry.path),target);
  }
  await t.test('complete copied input matches all pinned source hashes',()=>{
    assert.equal(verifyInputs(skia,app).sourceFiles,lock.files.length);
  });
  await t.test('source mutation is refused and preserved for its owner',()=>{
    const file=path.join(skia,lock.files.find(f=>f.path.endsWith('.cs')).path),original=fs.readFileSync(file);
    try {fs.appendFileSync(file,'\n// deliberate drift\n');assert.throws(()=>verifyInputs(skia,app),/source differs/);assert.ok(fs.readFileSync(file,'utf8').includes('deliberate drift'));} finally {fs.writeFileSync(file,original);}
  });
  await t.test('unexpected source is refused and never cleaned',()=>{
    const file=path.join(skia,'Beep.Skia','Unexpected.cs');
    try {fs.writeFileSync(file,'// extra source');assert.throws(()=>verifyInputs(skia,app),/source differs/);assert.ok(fs.existsSync(file));} finally {fs.unlinkSync(file);}
  });
  await t.test('owner archive replacement is refused',()=>{
    const file=path.join(app,'dependencies/packages/thetechidea.beep.vis.modules.2.0.44.nupkg'),original=fs.readFileSync(file);
    try {fs.appendFileSync(file,'changed');assert.throws(()=>verifyInputs(skia,app),/Pinned package changed/);} finally {fs.writeFileSync(file,original);}
  });
  await t.test('tracked patch and newly added source copies are independently pinned',()=>{
    const patch=path.join(app,'dependencies/beep-skia.patch'),original=fs.readFileSync(patch);
    try {fs.appendFileSync(patch,'changed');assert.throws(()=>verifyInputs(skia,app),/patch hash differs/);} finally {fs.writeFileSync(patch,original);}
    const added=path.join(app,'dependencies/beep-skia-added',lock.addedFiles[0]),bytes=fs.readFileSync(added);
    try {fs.appendFileSync(added,'changed');assert.throws(()=>verifyInputs(skia,app),/Pinned added source changed/);} finally {fs.writeFileSync(added,bytes);}
  });
});
