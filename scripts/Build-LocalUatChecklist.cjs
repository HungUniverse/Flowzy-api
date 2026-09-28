// Read-only generator: emits Markdown to stdout. Does not call APIs or change data.
const fs = require('node:fs');
const cp = require('node:child_process');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const java = 'D:/f-spark/f-spark-api/src/main/java/com/fspark/api';
const audit = JSON.parse(cp.execFileSync(process.execPath,
  [path.join(__dirname, 'Read-ControllerAudit.cjs'), java, 'all'],
  {encoding:'utf8', maxBuffer:8e6}));
const spec = fs.readFileSync(path.join(root,'docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md'),'utf8').replace(/\r/g,'');
const openapi = JSON.parse(fs.readFileSync(path.join(root,'contracts/fspark-openapi.json'),'utf8'));
const schemas = openapi.components.schemas;
const flows = {
  AcademicTermController:'F04',AdminFeedbackController:'F15',AdminGroupController:'F05',
  AdminProblemController:'F07',AdminTermController:'F04, F15, F16',AdminUserController:'F02',
  AuthController:'F01',BackupController:'F17',CourseMilestoneController:'F10',DashboardController:'F14',
  FeedbackController:'F15',GroupController:'F05, F06, F10',GroupInvitationController:'F06',
  GroupJoinRequestController:'F06',GroupMeetingController:'F09',GroupProblemController:'F07',
  GroupRecruitmentRoleController:'F04',GroupTaskController:'F08',ImportController:'F03',
  InstructorGroupBoardController:'F05',InstructorProblemController:'F07',InstructorSubmissionController:'F12',
  MentorAvailabilityController:'F09',MentorMeetingReportController:'F09',MilestoneGradeController:'F12',
  MilestoneGradeMatrixController:'F11',MilestoneSubmissionController:'F12',NotificationController:'F13',
  ProblemController:'F07',ProblemCriteriaController:'F04, F07',ProblemDomainController:'F04, F07',
  ProblemImportController:'F03',ProfileController:'F01',StudentAccountImportController:'F03',
  StudentController:'F04',StudentGroupGradeController:'F12',TaskBoardController:'F08'
};
const esc = x => String(x).replace(/\|/g,'\\|').replace(/\s+/g,' ').trim();
const resolve = s => s?.$ref ? schemas[s.$ref.split('/').pop()] : s;
function type(s) {
  if (!s) return 'xem DTO';
  if (s.$ref) return s.$ref.split('/').pop();
  if (s.type==='array') return 'array<'+type(s.items)+'>';
  return (s.type||'object')+(s.format?' ('+s.format+')':'');
}
function params(op) {
  const multipart = op.signature.includes('MultipartFile');
  return [...op.signature.matchAll(/@(RequestParam|PathVariable|RequestPart)(?:\(([^)]*)\))?\s+([\w<>?]+)\s+(\w+)/g)].map(m=>{
    const args=m[2]||'';
    const name=args.match(/(?:value|name)\s*=\s*"([^"]+)"/)?.[1] || args.match(/^\s*"([^"]+)"/)?.[1] || m[4];
    const def=args.match(/defaultValue\s*=\s*"([^"]*)"/)?.[1];
    const required=!/required\s*=\s*false/.test(args)&&def===undefined;
    return {name,where:m[1]==='PathVariable'?'path':m[1]==='RequestPart'||multipart?'multipart':'query',type:m[3],required,def};
  });
}
function dataFields(dataType) {
  if(!dataType || dataType==='Void')return 'null';
  if(['Long','Integer','BigDecimal'].includes(dataType))return 'Giá trị số, không phải object.';
  const list=dataType.match(/^(List|PageResponse)<(.+)>$/);
  const name=list?list[2]:dataType;
  const s=schemas[name];
  if(!s?.properties)return 'Xem DTO/contract liên kết bên dưới; không suy đoán field.';
  let fields=Object.keys(s.properties).map(k=>'`'+k+'`').join(', ');
  if(list?.[1]==='List')return 'Mảng; mỗi phần tử: '+fields+'.';
  if(list?.[1]==='PageResponse')return 'PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: '+fields+'.';
  return fields+'.';
}
function response(op) {
  let status=200;
  if (/ResponseEntity\.accepted\(/.test(op.body)) status=202;
  if (/status\(HttpStatus\.CREATED\)/.test(op.body)) status=201;
  if (/status\(HttpStatus\.GONE\)/.test(op.body)) status=410;
  if (/status\(404\)/.test(op.body)) status=404;
  const binary=/ResponseEntity<(?:byte\[\]|Resource)>/.test(op.signature);
  let messages=[...op.body.matchAll(/APIResponse\.success\(\s*"([^"]+)"/g)].map(m=>m[1]);
  if(status===202) messages=[...op.body.matchAll(/new APIResponse<>\(\s*202,\s*"([^"]+)"/g)].map(m=>m[1]);
  if(status>=400) messages=[...op.body.matchAll(/APIResponse\.error\([\s\S]*?"([^"]+)"/g)].map(m=>m[1]);
  if(op.name==='updateGroupLock')messages=['Group locked successfully','Group unlocked successfully'];
  if(op.name==='listMySlots')messages=['Availability slots retrieved successfully'];
  if(!messages.length&&!binary)messages=['Success'];
  const code=status>=400?status:status===202?202:200;
  const dataType=op.signature.match(/ResponseEntity<APIResponse<(.+?)>>\s+\w+\s*\(/)?.[1];
  return {status,binary,messages,code,dataType};
}
const requestTypes=new Set();
let out='# Flowzy — checklist nghiệm thu từng controller và API\n\n';
out+='Ngày soạn: 2026-09-27. Đọc cùng [luồng chạy và dữ liệu mẫu](D:/Flowzy-api/docs/LOCAL_UAT_PLAYBOOK.md).\n\n';
out+='## Phạm vi và cách thực hiện\n\n';
out+='37 controller Java; 174 method đang hoạt động; 192 tổ hợp HTTP method–route tính riêng alias. Các checkbox đều **NOT RUN** khi phát hành. Đây là expected contract lấy từ Java/đặc tả, không phải response Flowzy đã được kiểm thử.\n\n';
out+='Với mỗi mục: (1) hoàn tất fixture và quyền trong luồng F tương ứng; (2) thay path ID/query/body bằng dữ liệu thật; (3) gọi API, ghi HTTP/code/message/data; (4) kiểm readback và side effect trong phần nghiệp vụ; (5) chạy nhánh lỗi đã mô tả và kiểm dữ liệu không đổi ngoài ý muốn; (6) ghi PASS/FAIL/BLOCKED. Alias ghi kết quả riêng, không đánh dấu cả cụm sau một request.\n\n';
out+='Body và query dùng đúng camelCase. Ô `mặc định` lấy từ annotation Java; service có thể bắt buộc query dù annotation cho optional, ví dụ student ungrouped/feedback export. Đừng suy quyền chỉ từ annotation: nguồn security/service và phần điều kiện chung ưu tiên. Các trường JSON trong phần cuối lấy từ snapshot schema để nhập liệu; DTO Java liên kết là chuẩn nếu khác.\n\n';
out+='Lỗi chung: token thiếu/hỏng/đã logout →401; sai role/resource →403 theo guard; validation →400; conflict/concurrency →409 khi được nêu; missing resource →404 theo thứ tự kiểm quyền. Không ép mọi lỗi nghiệp vụ vào cùng một mã. Mỗi lỗi phải đối chiếu message trong DTO/service tại link nguồn. File trả bytes và headers, không envelope.\n\n';
out+='**An toàn:** close term/archive/delete/reset-password chỉ trên fixture UAT. Restore chỉ trên stack disposable có DB/volume riêng. Legacy submissions/grades cần fixture riêng vì không có API tạo đang hoạt động. Google cần người dùng xác thực thật.\n\n';
out+='## Mục lục kiểm soát độ phủ\n\n| Mã | Controller | Luồng | HTTP routes | Đã pass / tổng |\n|---|---|---|---:|---|\n';
for(let i=0;i<audit.controllers.length;i++){
 const c=audit.controllers[i],n=c.operations.reduce((n,o)=>n+o.routes.length,0),id='C'+String(i+1).padStart(2,'0');
 out+=`| ${id} | [${c.name}](#${id.toLowerCase()}) | ${flows[c.name]} | ${n} | __ / ${n} |\n`;
}
let routeCount=0,methodCount=0;
for(let i=0;i<audit.controllers.length;i++){
 const c=audit.controllers[i],id='C'+String(i+1).padStart(2,'0');
 const start=spec.indexOf(`## ${i+1}. ${c.name}\n`);
 const next=spec.indexOf('\n## ',start+1);
 const section=spec.slice(start,next<0?spec.length:next);
 const preamble=section.slice(section.indexOf('\n')+1,section.indexOf('\n### ')).replace(/<a id="[^"]+"><\/a>/g,'').trim();
 out+=`\n<a id="${id.toLowerCase()}"></a>\n\n## ${id}. ${c.name}\n\nLuồng và fixture: **${flows[c.name]}** trong playbook.\n\n${preamble}\n`;
 for(let j=0;j<c.operations.length;j++){
  const op=c.operations[j],key=`${id}-${String(j+1).padStart(2,'0')}`,r=response(op);
  const marker=`### ${i+1}.${j+1}. ${op.name}`;
  const a=section.indexOf(marker); if(a<0)throw new Error('Missing spec '+key);
  const b=section.indexOf('\n### ',a+1);
  const business=section.slice(a+marker.length,b<0?section.length:b).replace(/^- `(?:GET|POST|PUT|PATCH|DELETE) [^`]+`\s*$/gm,'').replace(/<a id="[^"]+"><\/a>/g,'').trim();
  const ps=params(op);
  const bodyName=op.signature.match(/@RequestBody\s+(?:@Valid\s+)?(\w+)/)?.[1];
  if(bodyName)requestTypes.add(bodyName);
  out+=`\n### ${key}. ${op.name}\n\n`;
  for(let k=0;k<op.routes.length;k++){out+=`- [ ] ${key}${op.routes.length>1?'.'+(k+1):''}: \`${op.routes[k]}\` — kết quả: NOT RUN.\n`;routeCount++;}
  if(ps.length){out+='\n| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |\n|---|---|---|---|---|\n';for(const p of ps)out+=`| ${p.name} | ${p.where} | ${esc(p.type)} | ${p.required?'Có':'Không'} | ${p.def===undefined?'—':'`'+p.def+'`'} |\n`;}
  if(bodyName)out+=`\nBody JSON: [${bodyName}](#dto-${bodyName.toLowerCase()}); payload chính minh họa trong luồng ${flows[c.name]}.\n`;
  if(!bodyName&&!ps.some(p=>p.where==='multipart'))out+='\nBody: không cần JSON theo chữ ký controller.\n';
  out+=`\n**Response mong đợi:** HTTP **${r.status}**`;
  if(r.binary)out+='; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.';
  else out+=`; envelope code **${r.code}**; message ${r.messages.map(m=>'`'+m+'`').join(' hoặc ')}; data: \`${r.dataType||'xem contract'}\`.`;
  if(!r.binary)out+='\n\nCác field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): '+(r.status>=400?'null':dataFields(r.dataType));
  out+='\n\n**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**\n\n'+business+'\n';
  out+='\n- [ ] Happy path và readback/side effect đúng mô tả trên.\n- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.\n- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.\n';
  methodCount++;
 }
}
out+='\n## Body JSON theo DTO\n\nTrường bắt buộc dưới đây theo schema snapshot; các điều kiện chéo (profile theo role, slotId hoặc startAt/endAt, owner/group, version, kỳ OPEN) nằm trong playbook và nghiệp vụ từng API. Không gửi tên biến như S1_ID dưới dạng JSON number; thay bằng ID thực. Không dùng dữ liệu mẫu làm tài khoản thật.\n';
const visited=new Set();
function renderDto(name){
 if(visited.has(name))return;visited.add(name);
 const s=resolve(schemas[name]);
 const src=audit.dtos.find(d=>d.name===name)?.file;
 out+=`\n<a id="dto-${name.toLowerCase()}"></a>\n\n### ${name}\n\n`;
 if(src)out+=`[DTO Java](${src}).\n\n`;
 if(!s?.properties){out+='Schema không có trong snapshot. Mở DTO Java/phụ lục HTTP để nhập chính xác; không tự đoán field.\n';return;}
 out+='| Field | Kiểu | Required schema | Ràng buộc / enum |\n|---|---|---|---|\n';
 const nested=[];
 for(const [field,v] of Object.entries(s.properties)){
  const constraints=[];
  if(v.enum)constraints.push(v.enum.join(', '));
  for(const k of ['minimum','maximum','exclusiveMinimum','exclusiveMaximum','minLength','maxLength','minItems','maxItems','pattern','default'])if(v[k]!==undefined&&v[k]!==2147483647)constraints.push(k+'='+v[k]);
  let t=type(v);
  const ref=v.$ref||v.items?.$ref;
  if(ref){const n=ref.split('/').pop();nested.push(n);t=`[${t}](#dto-${n.toLowerCase()})`;}
  out+=`| ${field} | ${esc(t)} | ${(s.required||[]).includes(field)?'Có':'Không'} | ${esc(constraints.join('; ')||'—')} |\n`;
 }
 for(const n of nested)renderDto(n);
}
for(const name of [...requestTypes].sort())renderDto(name);
if(routeCount!==192||methodCount!==174||audit.controllers.length!==37)throw new Error('Coverage mismatch');
out+='\n## Tổng kết lượt test\n\n| Chỉ số | Kết quả người kiểm thử điền |\n|---|---|\n| HTTP routes đã chạy / 192 | __ |\n| PASS / FAIL / BLOCKED / NOT RUN | __ / __ / __ / __ |\n| Controller đã đủ positive + negative + scope + side effects / 37 | __ |\n| Restore chạy trên DB disposable | Có / Không / BLOCKED |\n| Google end-to-end | PASS / FAIL / BLOCKED |\n| STOMP nhận MESSAGE sau commit | PASS / FAIL / BLOCKED |\n| Legacy fixture đủ dữ liệu | Có / Không |\n\nKhông ký nghiệm thu controller còn case FAIL/BLOCKED hoặc thiếu alias chưa chạy.\n';
process.stdout.write(out);
