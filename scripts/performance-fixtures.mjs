import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const template=JSON.parse(fs.readFileSync(path.join(root,'docs/evidence/p2-final-browser-roundtrip.simpledraw.json'),'utf8'));
const unique=[...new Map(template.diagram.components.map(c=>[c.propertyBag.Kind,c])).values()];
const rectangle=unique.find(c=>c.propertyBag.Kind==='Rectangle');
const id=key=>{const h=crypto.createHash('sha256').update(key).digest('hex').slice(0,32);return `${h.slice(0,8)}-${h.slice(8,12)}-${h.slice(12,16)}-${h.slice(16,20)}-${h.slice(20)}`;};
export function fixture(count,mixed=false) {
    if (![100,1000,2000,5000].includes(count)) throw new Error('Use the declared performance/boundary counts.');
    const columns=Math.ceil(Math.sqrt(count)), cell=900/columns;
    const components=Array.from({length:count},(_,i)=>{
        const c=structuredClone(mixed?unique[i%unique.length]:rectangle);
        c.id=id(`${count}:${mixed}:${i}`);c.name=`${c.propertyBag.Kind} ${i+1}`;
        c.x=30+i%columns*cell;c.y=30+Math.floor(i/columns)*cell;
        if(c.propertyBag.Kind!=='Text') {c.width=cell*.7;c.height=cell*.5;}
        c.propertyBag.GroupId='00000000-0000-0000-0000-000000000000';
        c.propertyBag.SketchSeed=String(i+1);c.inPointIds=[];c.outPointIds=[];
        return c;
    });
    return {format:'beep.simpledraw',version:6,diagram:{schemaVersion:3,themeName:'Light',components,lines:[]}};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
    const target=path.resolve(process.argv[2]??path.join(root,'artifacts/performance-fixtures'));
    if(fs.existsSync(target)) throw new Error('Fixture output must be absent.');
    fs.mkdirSync(target,{recursive:true});
    const report=[];
    for(const count of [100,1000,2000,5000]) for(const mixed of count===5000?[false]:[false,true]) {
        const json=JSON.stringify(fixture(count,mixed)),name=`${count}-${mixed?'mixed':'rectangles'}.simpledraw.json`;
        fs.writeFileSync(path.join(target,name),json);
        report.push({name,objects:count,mixed,bytes:Buffer.byteLength(json),sha256:crypto.createHash('sha256').update(json).digest('hex'),scope:count===5000?'native refusal fixture; shared-renderer stress is separate':'normal product board'});
    }
    fs.writeFileSync(path.join(target,'inventory.json'),JSON.stringify(report,null,2)+'\n');
    console.log(JSON.stringify(report));
}
