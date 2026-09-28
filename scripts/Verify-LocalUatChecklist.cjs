// Documentation-only coverage checks. No HTTP requests and no database writes.
const fs = require('node:fs');
const cp = require('node:child_process');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const source = JSON.parse(cp.execFileSync(process.execPath,
  [path.join(__dirname, 'Read-ControllerAudit.cjs'), 'D:/f-spark/f-spark-api/src/main/java/com/fspark/api', 'summary'],
  {encoding:'utf8', maxBuffer:8e6}));
const doc = fs.readFileSync(path.join(root, 'docs/LOCAL_UAT_API_CHECKLIST.md'), 'utf8').replace(/\r/g,'');
const guide = fs.readFileSync(path.join(root, 'docs/LOCAL_UAT_PLAYBOOK.md'), 'utf8').replace(/\r/g,'');
const actual = [...doc.matchAll(/^- \[ \] C\d\d-\d\d(?:\.\d+)?: `([^`]+)`/gm)].map(m=>m[1]);
const expected = source.controllers.flatMap(c=>c.operations.flatMap(o=>o.routes));
const missing = expected.filter(r=>!actual.includes(r));
const extra = actual.filter(r=>!expected.includes(r));
const anchors = new Set([...doc.matchAll(/<a id="([^"]+)">/g)].map(m=>m[1]));
const broken = [...doc.matchAll(/\]\(#([^)]*)\)/g)].map(m=>m[1]).filter(x=>!anchors.has(x));
const normalize = route => route.replace(/\{[^}]+\}/g,'{id}').split('?')[0];
const known = new Set(expected.map(normalize));
const guideExplicitRoutes = [...guide.matchAll(/\b(GET|POST|PUT|PATCH|DELETE) `(\/api\/[^`]+)`/g)]
  .map(m=>m[1]+' '+m[2]);
const patterns = expected.map(r=>new RegExp('^'+r.split(/(\{[^}]+\})/).map(p=>p.startsWith('{')?'[^/]+':p.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')).join('')+'$'));
const unknownGuideRoutes = guideExplicitRoutes.filter(r=>!known.has(normalize(r)) && !patterns.some(p=>p.test(r.split('?')[0])));
const generated = cp.execFileSync(process.execPath,[path.join(__dirname,'Build-LocalUatChecklist.cjs')],{encoding:'utf8',maxBuffer:8e6}).replace(/\r/g,'');
const result = {
  controllers:source.controllers.length,
  controllerSections:(doc.match(/^## C\d\d\./gm)||[]).length,
  methods:(doc.match(/^### C\d\d-\d\d\./gm)||[]).length,
  expectedRoutes:expected.length,actualRoutes:actual.length,
  uniqueRoutes:new Set(actual).size,
  flows:(guide.match(/^## F\d\d\./gm)||[]).length,
  missing,extra,brokenAnchors:[...new Set(broken)],unknownGuideRoutes,
  generatorMatches:generated.trimEnd()===doc.trimEnd()
};
console.log(JSON.stringify(result,null,2));
if(missing.length||extra.length||broken.length||unknownGuideRoutes.length||!result.generatorMatches||result.controllerSections!==37||result.methods!==174||result.actualRoutes!==192||result.flows!==18)process.exitCode=1;
