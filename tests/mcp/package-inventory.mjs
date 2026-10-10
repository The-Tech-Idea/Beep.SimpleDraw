import fs from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';

const [directory, evidence] = process.argv.slice(2);
const root = path.resolve(directory);
const manifest = JSON.parse(await fs.readFile(path.join(root,'manifest.json'),'utf8'));
async function inventory(directory, prefix='') {
    const entries = await fs.readdir(directory,{withFileTypes:true});
    const files=[];
    for(const entry of entries) {
        assert.equal(entry.isSymbolicLink(),false,'No symlinks in a release');
        const relative=prefix+entry.name;
        if(entry.isDirectory()) files.push(...await inventory(path.join(directory,entry.name),relative+'/'));
        else files.push(relative);
    }
    return files;
}
const actual=(await inventory(root)).sort();
assert.deepEqual(actual,[...manifest.files.map(f=>f.path),'manifest.json'].sort());
const largeFiles=[];
for(const item of manifest.files) {
    assert.equal(path.posix.isAbsolute(item.path),false);
    assert.ok(!item.path.split('/').includes('..'));
    const bytes=await fs.readFile(path.join(root,item.path));
    assert.equal(bytes.length,item.bytes,item.path);
    assert.equal(createHash('sha256').update(bytes).digest('hex'),item.sha256,item.path);
    assert.ok(!/\.(pdb|log|exe)$|appsettings|private\.json/i.test(item.path),item.path);
    if(item.path.includes('/server/')) {
        if(item.path.endsWith('.json')) assert.ok(['Beep.SimpleDraw.Mcp.deps.json','Beep.SimpleDraw.Mcp.runtimeconfig.json','mcp-tool-manifest.json','mcp-component-schemas.json'].includes(path.basename(item.path)));
        if(item.path.includes('/runtimes/')) assert.match(item.path,/\/runtimes\/(win|win-x64)\//);
    }
    if(bytes.length>=5*1024*1024) largeFiles.push({path:item.path,bytes:bytes.length});
}
for(const client of ['codex','claude']) {
    const plugin=path.join(root,'plugins',`simpledraw-${client}`);
    const metadata=JSON.parse(await fs.readFile(path.join(plugin,client==='codex'?'.codex-plugin':'.claude-plugin','plugin.json'),'utf8'));
    assert.equal(metadata.name,'simpledraw');assert.equal(metadata.version,manifest.version);
    assert.equal(metadata.license,'Apache-2.0');
    for(const icon of ['simpledraw-icon.png','simpledraw-icon-dark.png']) {
        const bytes=await fs.readFile(path.join(plugin,'assets',icon));
        assert.equal(bytes.subarray(0,8).toString('hex'),'89504e470d0a1a0a');
        assert.equal(bytes.readUInt32BE(16),bytes.readUInt32BE(20));
        assert.ok(bytes.readUInt32BE(16)>=256&&bytes.readUInt32BE(16)<=4096);
        assert.ok(bytes.length<=5*1024*1024);
    }
    assert.equal(await fs.readFile(path.join(plugin,'LICENSE.txt'),'utf8'),await fs.readFile(path.join(root,'LICENSE.txt'),'utf8'));
    const tools=JSON.parse(await fs.readFile(path.join(plugin,'server','mcp-tool-manifest.json'),'utf8')).tools;
    assert.equal(tools.length,69);
    for(const tool of tools) for(const hint of ['readOnlyHint','openWorldHint','destructiveHint']) assert.equal(typeof tool.annotations?.[hint],'boolean');
    const noticeAudit=JSON.parse(await fs.readFile(path.join(plugin,'notices','audit.json'),'utf8'));
    assert.equal(noticeAudit.packages.length,16);
    for(const file of noticeAudit.files) assert.equal(createHash('sha256').update(await fs.readFile(path.join(plugin,'notices',file.path))).digest('hex'),file.sha256);
}
const report={version:manifest.version,runtime:manifest.runtime,files:actual.length,hashesVerified:true,iconsVerified:true,
    explicitAnnotations:true,noticesVerified:true,privateSettingsAndDebugSymbolsExcluded:true,
    officialAnthropicFileSizeGate:largeFiles.length?'Blocked':'Passed',filesAtLeast5MiB:largeFiles};
await fs.writeFile(evidence,JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report));
