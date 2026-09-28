// Read-only consistency checks for the source-backed controller audit.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const {execFileSync} = require('child_process');
const root = path.resolve(__dirname, '..');
const source = JSON.parse(execFileSync(process.execPath, [path.join(__dirname, 'Read-ControllerAudit.cjs'), 'D:/f-spark/f-spark-api/src/main/java/com/fspark/api', 'all'], {cwd:root, encoding:'utf8', maxBuffer:8*1024*1024}));
const names = ['FSPARK_CONTROLLER_BUSINESS_SPEC.md','FSPARK_HTTP_CONTRACT_APPENDIX.md','FLOWZY_CONTROLLER_PARITY_AUDIT.md','CONTROLLER_DOCUMENTATION_REVIEW_2.md'];
const docs = names.map(name => ({name, file:path.join(root,'docs',name), text:fs.readFileSync(path.join(root,'docs',name),'utf8')}));
const expected = source.controllers.flatMap(c => c.operations.flatMap(o => o.routes)).sort();
const failures = [];
// Implementation changes are expected after the audit. Keep strict snapshot mode
// by default; this explicit option still verifies every Java hash and excerpt.
const allowRuntimeChanges = process.argv.includes('--allow-runtime-changes');
const runtimeChanges = [];
// Verify actual source excerpts, not only endpoint names. Normalize line endings
// only: parameter defaults, validation annotations and response bodies must match.
const normalize = text => text.replace(/\r\n/g, '\n').trim();
const appendix = normalize(docs[1].text);
let verifiedMethods = 0;
for (const [ci, controller] of source.controllers.entries()) {
  for (const [mi, operation] of controller.operations.entries()) {
    const anchor = `c${String(ci+1).padStart(2,'0')}-m${mi+1}`;
    const section = appendix.split(`<a id="${anchor}"></a>`)[1]?.split('<a id="')[0];
    const excerpt = section?.match(/```java\n([\s\S]*?)\n```/)?.[1];
    if (!excerpt || normalize(excerpt) !== normalize(operation.signature+'\n'+operation.body)) {
      failures.push('HTTP source excerpt differs: '+controller.name+'.'+operation.name+' ('+anchor+')');
    } else verifiedMethods++;
    if (operation.authorization && !section?.includes(normalize(operation.authorization))) {
      failures.push('Missing authorization annotation excerpt: '+anchor);
    }
  }
}
let verifiedDtos = 0;
for (const dto of source.dtos) {
  const section = appendix.split('### '+dto.name+'\n')[1];
  const excerpt = section?.match(/```java\n([\s\S]*?)\n```/)?.[1];
  if (!excerpt || normalize(excerpt) !== normalize(dto.source)) failures.push('DTO source excerpt differs: '+dto.name);
  else verifiedDtos++;
}
for (const doc of docs.slice(0,2)) {
  const actual = [...doc.text.matchAll(/^- `((?:GET|POST|PUT|PATCH|DELETE) \/[^`]+)`$/gm)].map(m=>m[1]).sort();
  if (JSON.stringify(actual)!==JSON.stringify(expected)) failures.push(doc.name+': route list differs from Java source');
}
for (const doc of docs) {
  for (const c of source.controllers) if(!doc.text.includes(c.name)) failures.push(doc.name+': missing '+c.name);
  for(const match of doc.text.matchAll(/\]\(([^)\r\n]+)\)/g)) {
    const target=match[1];
    if(!/^(?:[A-Z]:\/|#)/i.test(target)) continue;
    const [raw,anchor]=target.split('#');
    const line=raw.match(/:(\d+)$/)?.[1];
    const file=raw ? raw.replace(/:\d+$/,'') : doc.file;
    if(!fs.existsSync(file)){failures.push(doc.name+': missing link '+target);continue;}
    if(line || anchor) {
      const text=fs.readFileSync(file,'utf8');
      if(line && Number(line)>text.split('\n').length) failures.push(doc.name+': line outside file '+target);
      if(anchor && !text.includes('id="'+anchor+'"')) failures.push(doc.name+': missing explicit anchor '+target);
    }
  }
}
const manifest=JSON.parse(fs.readFileSync(path.join(root,'docs/controller-audit-source-manifest.json'),'utf8'));
for(const file of manifest.files) {
  if(!fs.existsSync(file.path)){failures.push('Missing snapshot source: '+file.path);continue;}
  const hash=crypto.createHash('sha256').update(fs.readFileSync(file.path)).digest('hex');
  if(hash!==file.sha256) {
    if(allowRuntimeChanges && file.path.endsWith('.cs')) runtimeChanges.push(file.path);
    else failures.push('Source changed since audit snapshot: '+file.path);
  }
}
const trx=fs.readFileSync(path.join(root,'tests/Flowzy.Tests/TestResults/controller-audit.trx'),'utf8');
const counters=trx.match(/<Counters\b[^>]+/)?.[0];
const stats=Object.fromEntries([...String(counters).matchAll(/(\w+)="(\d+)"/g)].map(m=>[m[1],Number(m[2])]));
if(stats.total!==66 || stats.passed!==66 || stats.failed!==0) failures.push('TRX counters differ from documented 66/0');
console.log(JSON.stringify({controllers:source.controllers.length,httpOperations:expected.length,verifiedMethodExcerpts:verifiedMethods,verifiedDtoExcerpts:verifiedDtos,javaFiles:manifest.files.filter(f=>f.path.endsWith('.java')).length,csharpFiles:manifest.files.filter(f=>f.path.endsWith('.cs')).length,documents:docs.map(d=>d.name),previousTestCounters:stats,allowRuntimeChanges,runtimeChanges,failures},null,2));
process.exitCode=failures.length?1:0;
