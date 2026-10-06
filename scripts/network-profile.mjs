import fs from 'node:fs';
import {performance} from 'node:perf_hooks';

// A local delivery model, not browser throttling or a real broadband provider.
// One shared reservation clock paces encoded bodies across every response.
export class ResponseLink {
  constructor(profile,{clock=()=>performance.now(),onTransfer=()=>{}}={}) {
    if(!profile||!Number.isFinite(profile.downloadMbps)||profile.downloadMbps<1||profile.downloadMbps>10000
      ||!Number.isInteger(profile.responseDelayMs)||profile.responseDelayMs<0||profile.responseDelayMs>5000)
      throw new Error('Network profile requires downloadMbps 1..10000 and responseDelayMs 0..5000.');
    this.profile={downloadMbps:profile.downloadMbps,responseDelayMs:profile.responseDelayMs};
    this.bytesPerSecond=profile.downloadMbps*1_000_000/8;
    this.clock=clock;this.onTransfer=onTransfer;this.nextFinish=0;
  }
  reserve(bytes) {
    if(!Number.isSafeInteger(bytes)||bytes<=0) throw new Error('A bandwidth reservation requires positive bytes.');
    this.nextFinish=Math.max(this.clock(),this.nextFinish)+bytes*1000/this.bytesPerSecond;
    return this.nextFinish;
  }
  async waitUntil(deadline,signal) {
    while(this.clock()<deadline) {
      if(signal.aborted) throw new Error('Response closed.');
      await new Promise((resolve,reject)=>{
        const close=()=>{clearTimeout(timer);signal.removeEventListener('abort',close);reject(new Error('Response closed.'));};
        const timer=setTimeout(()=>{signal.removeEventListener('abort',close);resolve();},Math.ceil(deadline-this.clock()));
        signal.addEventListener('abort',close,{once:true});
      });
    }
    if(signal.aborted) throw new Error('Response closed.');
  }
  async send(file,request,response,headers) {
    const receivedAtMs=this.clock(),controller=new AbortController();
    let stream,bodyBytes=0,completed=false;
    const close=()=>{controller.abort();stream?.destroy();};
    response.once('close',close);
    try {
      await this.waitUntil(receivedAtMs+this.profile.responseDelayMs,controller.signal);
      response.writeHead(200,headers);
      if(request.method!=='HEAD') {
        stream=fs.createReadStream(file,{highWaterMark:16*1024});
        for await(const chunk of stream) {
          await this.waitUntil(this.reserve(chunk.length),controller.signal);
          if(!response.write(chunk)) await new Promise((resolve,reject)=>{
            const drain=()=>{response.off('close',closed);resolve();};
            const closed=()=>{response.off('drain',drain);reject(new Error('Response closed.'));};
            response.once('drain',drain);response.once('close',closed);
          });
          bodyBytes+=chunk.length;
        }
      }
      await new Promise((resolve,reject)=>{
        const closed=()=>{response.off('finish',finish);reject(new Error('Response closed.'));};
        const finish=()=>{response.off('close',closed);resolve();};
        response.once('finish',finish);response.once('close',closed);response.end();
      });
      completed=true;
    } finally {
      response.off('close',close);stream?.destroy();
      this.onTransfer({path:request.url,method:request.method,status:response.statusCode,
        encoding:headers['Content-Encoding']??'identity',bodyBytes,contentLength:Number(headers['Content-Length']),
        receivedAtMs,finishedAtMs:this.clock(),completed});
    }
  }
}
