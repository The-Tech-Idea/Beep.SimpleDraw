import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
const source = await readFile(new URL('../src/Beep.SimpleDraw.Web/wwwroot/js/app.js',import.meta.url),'utf8');
const ui = await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));

function setup() {
    const listeners = new Map(), calls = [];
    globalThis.window = {
        addEventListener(name,fn){listeners.set(name,fn);}, removeEventListener(name){listeners.delete(name);}
    };
    const state = {dialog:null};
    const element = (tag, extra={}) => ({tag,isConnected:true,inert:false,
        closest(selector){
            if(selector==='[inert]') return this.inert ? this : null;
            if(selector==='.drawing-surface') return this.canvas ? this : null;
            return selector.split(',').map(s=>s.trim()).includes(this.tag) ? this : null;
        }, getClientRects(){return [1];},
        focus(){document.activeElement=this; listeners.get('focusin')?.({target:this});}, ...extra
    });
    const canvas = element('div',{canvas:true}), invoker = element('button');
    globalThis.document = {
        activeElement:invoker,
        querySelectorAll(){return state.dialog ? [state.dialog] : [];}, querySelector(){return canvas;}
    };
    ui.attachShortcuts({async invokeMethodAsync(...args){calls.push(args);}}); invoker.focus();
    const key = (key, options={}) => {
        const event = {key,target:document.activeElement,prevented:false,preventDefault(){this.prevented=true;},...options};
        listeners.get('keydown')(event); return event;
    };
    const open = kind => {
        const first = element('textarea'), last = element('button');
        state.dialog = element('section',{dataset:{modalKind:kind},querySelector(){return null;},
            querySelectorAll(){return [first,last];}, contains(node){return [this,first,last].includes(node);}});
        ui.syncModal(); return {first,last};
    };
    return {state,element,canvas,invoker,key,open,calls,listeners};
}

test('modal wraps forward/reverse Tab, redirects outside focus, and restores invoker',()=>{
    const f=setup(), {first,last}=f.open('text'); assert.equal(document.activeElement,first);
    assert.equal(f.key('Tab',{shiftKey:true}).prevented,true); assert.equal(document.activeElement,last);
    assert.equal(f.key('Tab').prevented,true); assert.equal(document.activeElement,first);
    f.canvas.focus(); assert.equal(document.activeElement,first);
    f.state.dialog=null; ui.syncModal(); assert.equal(document.activeElement,f.invoker); ui.detachShortcuts();
    assert.equal(f.listeners.size,0);
});
test('modal handles only its own close/commit commands and preserves IME composition',()=>{
    const f=setup(); f.open('text');
    f.key('Enter',{ctrlKey:true,isComposing:true}); f.key('Escape',{isComposing:true}); f.key('Process');
    f.key('r'); f.key('s',{ctrlKey:true}); assert.deepEqual(f.calls,[]);
    assert.equal(f.key('Enter',{ctrlKey:true}).prevented,true); f.key('Escape');
    assert.deepEqual(f.calls,[['TextShortcut',true],['CloseModal','text']]); ui.detachShortcuts();
});
test('native inputs and buttons keep typing/arrows/Enter; canvas alone receives draw/move commands',()=>{
    const f=setup(); f.element('input').focus(); f.key('r'); f.key('ArrowUp'); f.key('s',{ctrlKey:true});
    f.invoker.focus(); assert.equal(f.key('Enter').prevented,false); assert.equal(f.key('ArrowDown').prevented,false);
    assert.deepEqual(f.calls,[]); f.canvas.focus(); f.key('Enter'); f.key('ArrowRight',{shiftKey:true});
    assert.deepEqual(f.calls,[['Shortcut','Enter',undefined,undefined],['Shortcut','ArrowRight',undefined,true]]);
    f.key('r',{altKey:true}); assert.equal(f.calls.length,2); ui.detachShortcuts();
});
test('removed invoker returns to canvas and help cannot commit text',()=>{
    const f=setup(); f.open('help'); f.key('Enter',{ctrlKey:true}); assert.deepEqual(f.calls,[]);
    f.invoker.isConnected=false; f.state.dialog=null; ui.syncModal(); assert.equal(document.activeElement,f.canvas);
    ui.detachShortcuts();
});
