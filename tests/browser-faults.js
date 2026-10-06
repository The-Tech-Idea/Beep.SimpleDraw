// Test-owned visible controls only; never included in the portable artifact.
const parameters=new URLSearchParams(location.search);
const appPath=parameters.get('appPath')??'/';
const channel=parameters.get('channel')??'document';
if(!/^\/(?:[a-zA-Z0-9_-]+\/)*$/.test(appPath)||!['document','library'].includes(channel)) throw new Error('Invalid owned test storage target.');
const key=`beep.simpledraw:${appPath}:${channel}:1`,output=document.getElementById('status');
document.getElementById('target').textContent=`Owned test target: ${key}`;
document.getElementById('corrupt').onclick=()=>{localStorage.setItem(key,'{owned-test-corrupt');output.textContent='Corrupt test bytes stored.';};
document.getElementById('future').onclick=()=>{localStorage.setItem(key,JSON.stringify({version:99,revision:1,document:'future-test'}));output.textContent='Future test bytes stored.';};
document.getElementById('payload').disabled=channel!=='library';
document.getElementById('payload').onclick=()=>{localStorage.setItem(key,JSON.stringify({version:1,revision:7,document:JSON.stringify({format:'beep.simpledraw.library',version:99,entries:[]})}));output.textContent='Future library payload stored inside a readable envelope.';};
document.getElementById('quota').onclick=()=>{
    let size=512*1024,index=0,total=0;
    while(size>=1){try{localStorage.setItem('simpledraw-owned-quota:'+index,'x'.repeat(size));index++;total+=size;}catch{size=Math.floor(size/2);}}
    output.textContent='Quota filled with '+total+' test characters.';
};
document.getElementById('release').onclick=()=>{for(const k of Object.keys(localStorage))if(k.startsWith('simpledraw-owned-quota:'))localStorage.removeItem(k);output.textContent='Test quota padding released.';};
document.getElementById('read').onclick=()=>{output.textContent=localStorage.getItem(key)??'No record';};
document.getElementById('board').onclick=()=>{output.textContent=localStorage.getItem(`beep.simpledraw:${appPath}:document:1`)??'No record';};
