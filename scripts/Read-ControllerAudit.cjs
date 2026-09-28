// Read-only source inventory for the controller audit. Prints JSON; never alters the Java oracle.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const javaRoot = process.argv[2] || 'D:/f-spark/f-spark-api/src/main/java/com/fspark/api';
const normalize = p => p.replaceAll('\\', '/');
function walk(dir) { return fs.readdirSync(dir, {withFileTypes:true}).flatMap(e => ['bin','obj'].includes(e.name) ? [] : e.isDirectory() ? walk(path.join(dir,e.name)) : [path.join(dir,e.name)]); }
function mask(src) {
  return src.replace(/\/\*[\s\S]*?\*\/|\/\/[^\r\n]*|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'/g, m => m.startsWith('/') ? m.replace(/[^\r\n]/g,' ') : m);
}
// Separate delimiter matcher (do not decrement depth on unrelated characters).
function endAt(src,start,left,right) {
  let d=0,q=null,esc=false;
  for(let i=start;i<src.length;i++){const c=src[i];if(q){if(esc)esc=false;else if(c==='\\')esc=true;else if(c===q)q=null;continue;}if(c==='"'||c==="'"){q=c;continue;}if(c===left)d++;else if(c===right&&--d===0)return i;}
  throw new Error('Unclosed delimiter');
}
function routes(args) {
  const a=args.match(/(?:value|path)\s*=\s*(\{[^}]*\}|"(?:\\.|[^"\\])*")/s)?.[1] ?? args;
  const strings=[...a.matchAll(/"((?:\\.|[^"\\])*)"/g)].map(x=>x[1]);
  return strings.filter(x=>x.startsWith('/')).length ? strings.filter(x=>x.startsWith('/')) : [''];
}
const controllers=walk(path.join(javaRoot,'controllers')).filter(p=>p.endsWith('Controller.java')).sort().map(file=>{
  const raw=fs.readFileSync(file,'utf8'), src=mask(raw), classPos=src.indexOf('public class ');
  const header=src.slice(0,classPos), baseMatch=/@RequestMapping\s*(\([^]*?\))?\s*(?=@|public|\r?\n)/.exec(header);
  const bases=baseMatch?.[1] ? routes(baseMatch[1]) : [''];
  const ops=[];const re=/@(Get|Post|Put|Patch|Delete|Request)Mapping\b/g;re.lastIndex=classPos;
  let m;while((m=re.exec(src))){let cursor=re.lastIndex;while(/\s/.test(src[cursor]))cursor++;let args='';if(src[cursor]==='('){let end=endAt(src,cursor,'(',')');args=src.slice(cursor+1,end);cursor=end+1;}
    const signatureStart=src.indexOf('public ',cursor);if(signatureStart<0)throw new Error('No method');
    const nameMatch=/\b(\w+)\s*\(/.exec(src.slice(signatureStart)); const paren=signatureStart+nameMatch.index+nameMatch[0].lastIndexOf('(');const paramsEnd=endAt(src,paren,'(',')');
    const bodyStart=src.indexOf('{',paramsEnd),bodyEnd=endAt(src,bodyStart,'{','}');
    const methods=m[1]==='Request'?[...args.matchAll(/RequestMethod\.(\w+)/g)].map(x=>x[1]):[m[1].toUpperCase()];
    if(!methods.length)throw new Error('Missing methods '+file);
    const annotation=raw.slice(m.index,signatureStart);const body=raw.slice(bodyStart,bodyEnd+1);
    ops.push({name:nameMatch[1],line:raw.slice(0,m.index).split('\n').length,routes:bases.flatMap(b=>routes(args).flatMap(r=>methods.map(http=>http+' '+(b+r||'/')))),signature:raw.slice(signatureStart,bodyStart).trim(),authorization:(annotation.match(/@PreAuthorize\([^\n]+/g)||header.match(/@PreAuthorize\([^\n]+/g)||[]).join(' '),body,summary:annotation.match(/summary\s*=\s*"([^"]*)"/)?.[1]||'',calls:[...body.matchAll(/\b(\w+(?:Service|Repository))\.(\w+)\s*\(/g)].map(x=>x[1]+'.'+x[2])});
    re.lastIndex=bodyEnd+1;
  }
  return {name:path.basename(file,'.java'),file:normalize(file),operations:ops};
});
const dtoNames=new Set(controllers.flatMap(c=>c.operations.flatMap(o=>[...o.signature.matchAll(/\b([A-Z]\w*(?:Request|Dto))\b/g)].map(m=>m[1]))));
const dtos=walk(path.join(javaRoot,'dtos')).filter(p=>dtoNames.has(path.basename(p,'.java'))).map(p=>({file:normalize(p),name:path.basename(p,'.java'),source:fs.readFileSync(p,'utf8').split('\n').filter(l=>!/^import |^package /.test(l)).join('\n').trim()}));
const files=[...walk(javaRoot).filter(p=>p.endsWith('.java')),...walk('D:/Flowzy-api/src').filter(p=>p.endsWith('.cs'))].sort().map(p=>({path:normalize(p),sha256:crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')}));
const mode=process.argv[3]||'summary';
const operationCount=controllers.reduce((n,c)=>n+c.operations.reduce((n,o)=>n+o.routes.length,0),0);
if (mode === 'all') {
  console.log(JSON.stringify({controllers, dtos, operationCount}));
  process.exit(0);
}
console.log(JSON.stringify(mode==='summary'?{controllers:controllers.map(c=>({name:c.name,file:c.file,operations:c.operations.map(o=>({name:o.name,routes:o.routes,line:o.line,calls:o.calls}))})),operationCount}:mode==='files'?files:mode==='dto-list'?dtos.map(d=>({name:d.name,file:d.file})):mode==='dto-all'?dtos:mode.startsWith('dto:')?dtos.find(d=>d.name===mode.slice(4)):controllers.find(c=>c.name===mode)));
