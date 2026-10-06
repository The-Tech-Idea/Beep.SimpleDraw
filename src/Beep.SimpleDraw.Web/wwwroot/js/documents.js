const maxRecordBytes = 3 * 1024 * 1024;
export function createDocumentStore({storage, locks, key, label = 'drawing', notify = () => {}}) {
    let writer = false, release, lastRaw = null, blocked = false;
    const result = (status, error = null, extra = {}) => ({status, error, ...extra});
    function read() {
        try {
            lastRaw = storage.getItem(key);
            if (lastRaw === null) { blocked = false; return result('empty', null, {revision:0}); }
            if (new TextEncoder().encode(lastRaw).length > maxRecordBytes) throw new Error('Stored record exceeds 3 MiB.');
            const record = JSON.parse(lastRaw);
            if (Object.keys(record).sort().join(',') !== 'document,revision,version' || record.version !== 1 ||
                !Number.isSafeInteger(record.revision) || record.revision < 0 || typeof record.document !== 'string')
                throw new Error('Unsupported local record.');
            blocked = false; return result('loaded', null, {revision:record.revision, document:record.document, raw:lastRaw});
        } catch (error) {
            blocked = true; return result(lastRaw === null ? 'unavailable' : 'recovery', error.message, {raw:lastRaw});
        }
    }
    async function open() {
        if (!locks) return result('unavailable', `Safe local saving requires Web Locks over HTTPS or localhost. Download a file to keep this ${label}.`);
        if (writer) return read();
        return new Promise(resolve => {
            locks.request(key + ':writer', {ifAvailable:true}, async lock => {
                if (!lock) {
                    const snapshot = read();
                    resolve(result('conflict', `Another tab owns this local ${label}. Close it and retry, or edit a file-only copy.`,
                        {document:snapshot.document,revision:snapshot.revision??0})); return;
                }
                writer = true;
                const held = new Promise(done => { release = done; });
                resolve(read()); await held; writer = false;
            }).catch(error => resolve(result('unavailable', error.message)));
        });
    }
    function save(document, revision, replaceRecovery = false) {
        if (!writer) return result('conflict', 'This tab is not the local writer. Download a file to keep your work.');
        if (blocked && !replaceRecovery) return result('recovery', `Recover or replace the unreadable local ${label} first.`, {raw:lastRaw});
        try {
            if (storage.getItem(key) !== lastRaw) { blocked = true; return result('conflict', 'Local storage changed outside this writer. Your edits remain in this tab.'); }
            if (typeof document !== 'string' || !Number.isSafeInteger(revision) || revision < 0) throw new Error('Invalid save revision.');
            const serialized = JSON.stringify({version:1, revision, document});
            if (new TextEncoder().encode(serialized).length > maxRecordBytes) throw new Error('Local record exceeds 3 MiB. Download a file instead.');
            storage.setItem(key, serialized); lastRaw = serialized; blocked = false;
            return result('saved', null, {revision});
        } catch (error) { return result('failed', 'Local save failed: ' + error.message + ' Download a file to keep your work.'); }
    }
    function externalChange(event) {
        if (event.key === key && writer && event.newValue !== lastRaw) {
            blocked = true; notify(result('conflict', 'Local storage changed in another context. Autosave is paused; download your edits.'));
        }
    }
    function close() { release?.(); release = null; writer = false; }
    return {open, save, externalChange, close};
}
// One storage policy, isolated lifetimes/keys for the document and opt-in library.
const channels = new Map();
async function connectChannel(name, dotnet) {
    disconnectChannel(name);
    const channel = {unsafe:false}; channels.set(name,channel);
    const method = name === 'document' ? 'StorageChanged' : 'LibraryChanged';
    const notify = result => dotnet.invokeMethodAsync(method,result).catch(error=>console.error('Storage notification failed',error));
    try {
        const key = 'beep.simpledraw:' + new URL(document.baseURI).pathname + ':' + name + ':1';
        channel.store = createDocumentStore({storage:localStorage, locks:navigator.locks,key,label:name==='document'?'drawing':'library',notify});
        channel.changed = event => channel.store?.externalChange(event);
        channel.beforeUnload = event => { if (channel.unsafe) { event.preventDefault(); event.returnValue=''; } };
        channel.restored = event => { if(event.persisted) notify({status:'conflict',error:'This page returned from browser history. Download pending edits, then retry the local writer.'}); };
        channel.hidden = () => disconnectChannel(name);
        window.addEventListener('storage',channel.changed); window.addEventListener('beforeunload',channel.beforeUnload);
        window.addEventListener('pageshow',channel.restored); window.addEventListener('pagehide',channel.hidden,{once:true});
        return await channel.store.open();
    } catch(error) { return {status:'unavailable',error:'Local storage is unavailable: '+error.message}; }
}
function saveChannel(name,document,revision,replaceRecovery) {
    return channels.get(name)?.store?.save(document,revision,replaceRecovery) ?? {status:'conflict',error:'Local writer was released. Download edits and reopen this app.'};
}
function retryChannel(name) { return channels.get(name)?.store?.open() ?? {status:'conflict',error:'Local writer was released. Download edits and reopen this app.'}; }
function guardChannel(name,value) { const channel=channels.get(name); if(channel) channel.unsafe=value; }
function disconnectChannel(name) {
    const channel=channels.get(name); if(!channel) return;
    channel.store?.close();
    for(const [event,property] of [['storage','changed'],['beforeunload','beforeUnload'],['pageshow','restored'],['pagehide','hidden']])
        if(channel[property]) window.removeEventListener(event,channel[property]);
    channels.delete(name);
}
export function connect(dotnet) { return connectChannel('document',dotnet); }
export function save(document,revision,replaceRecovery) { return saveChannel('document',document,revision,replaceRecovery); }
export function retry() { return retryChannel('document'); }
export function guard(value) { guardChannel('document',value); }
export function disconnect() { disconnectChannel('document'); }
export function connectLibrary(dotnet) { return connectChannel('library',dotnet); }
export function saveLibrary(document,revision,replaceRecovery) { return saveChannel('library',document,revision,replaceRecovery); }
export function retryLibrary() { return retryChannel('library'); }
export function guardLibrary(value) { guardChannel('library',value); }
export function disconnectLibrary() { disconnectChannel('library'); }
