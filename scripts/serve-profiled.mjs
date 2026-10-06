import fs from 'node:fs';
import path from 'node:path';
import {createStaticServer} from './serve-static.mjs';

try {
  const [root,reportPath,portText,profilePath,evidencePath]=process.argv.slice(2);
  const port=Number(portText);
  if(!root||!reportPath||!profilePath||!evidencePath||!Number.isInteger(port)||port<1024||port>65535)
    throw new Error('Use: node scripts/serve-profiled.mjs wwwroot asset-report.json port profile.json absent-evidence.jsonl');
  const report=JSON.parse(fs.readFileSync(reportPath,'utf8')),profile=JSON.parse(fs.readFileSync(profilePath,'utf8'));
  const evidence=path.resolve(evidencePath);
  const server=createStaticServer(root,report,{networkProfile:profile,onTransfer:transfer=>fs.appendFileSync(evidence,JSON.stringify({kind:'response',...transfer})+'\n')});
  fs.writeFileSync(evidence,JSON.stringify({kind:'profile',profile,offlineVersion:report.offlineVersion,basePath:report.basePath,
    createdAt:new Date().toISOString(),scope:'Local aggregate encoded-response-body pacing plus per-response delay. Does not model DNS, TLS, HTTP header bytes, upload, packet loss, provider/browser throttling or public hosting. Refused paths retain the ordinary host error response.'})+'\n',{flag:'wx'});
  server.on('error',error=>{console.error(error.message);process.exitCode=1;});
  server.listen(port,'127.0.0.1',()=>console.log(`Profiled verified preview: http://127.0.0.1:${port}${report.basePath}; ${profile.downloadMbps} Mbps encoded bodies, ${profile.responseDelayMs} ms response delay`));
} catch(error) {console.error(error.message);process.exitCode=1;}
