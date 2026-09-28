// Local UAT against the running Flowzy API. Uses only pre-existing UAT actors/groups.
// Intentionally never closes SU26, archives real students, or restores the database.
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const fixture = JSON.parse(fs.readFileSync(path.join(root, '.tools/local-demo-20260928/state.json'), 'utf8'));
const results = [];
const base = 'http://localhost:8080';
const id = name => fixture.steps[name].id;
const g1 = id('group.g1'), g2 = id('group.g2'), g3 = id('group.g3');
const p1 = id('problem.p1'), ms1 = id('ms1'), ms2 = id('ms2');
const accounts = Object.fromEntries(['i1','i2','m1','m2','s1','s2','s3','s4','s5','s6','s7'].map(k => [k, fixture.steps['account.' + k]]));
const tokens = {};
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
async function request(method, route, body, actor, binary = false) {
  const headers = actor ? {Authorization: `Bearer ${tokens[actor]}`} : {};
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const response = await fetch(base + route, {method, headers, body: body === undefined ? undefined : JSON.stringify(body)});
  if (binary) return {status: response.status, headers: response.headers, bytes: Buffer.from(await response.arrayBuffer())};
  const raw = await response.text();
  let json; try { json = JSON.parse(raw); } catch { json = {message: raw.slice(0, 300)}; }
  return {status: response.status, data: json.data, message: json.message, json};
}
function ensure(test, message) { if (!test) throw Error(message); }
function success(r, message) { ensure(r.status >= 200 && r.status < 300, `${message}: HTTP ${r.status} ${r.message || ''}`); return r.data; }
function rejected(r, message) { ensure(r.status >= 400 && r.status < 500, `${message}: expected 4xx, got ${r.status} ${r.message || ''}`); }
async function check(caseId, action) {
  try { const detail = await action(); results.push({caseId, status:'PASS', detail: detail || ''}); console.log(`PASS ${caseId}${detail ? ' '+detail : ''}`); return true; }
  catch (error) { results.push({caseId, status:'FAIL', detail: error.message}); console.log(`FAIL ${caseId} ${error.message}`); return false; }
}
function blocked(caseId, detail) { results.push({caseId, status:'BLOCKED', detail}); console.log(`BLOCKED ${caseId} ${detail}`); }
async function main() {
  const config = JSON.parse(fs.readFileSync(path.join(root, 'src/Flowzy.Api/appsettings.Development.json'), 'utf8'));
  tokens.admin = success(await request('POST','/api/auth/login',{email:config.Admin.Email,password:config.Admin.Password}), 'Admin login').accessToken;
  // I2 was INACTIVE before the first attempt in this run; restore that state at the end.
  let restoreI2Inactive = true;
  for (const actor of Object.keys(accounts)) {
    let login = await request('POST','/api/auth/login',{email:accounts[actor].email,password:fixture.password});
    if (actor === 's5' && login.status === 401) {
      blocked('SETUP-S5','The saved UAT password no longer authenticates S5; account/password is preserved.');
      continue;
    }
    if (actor === 'i2' && login.status === 401 && login.message === 'Account is not active') {
      const profile = accounts.i2.instructorProfile;
      const body = {email:accounts.i2.email,status:'ACTIVE',mustChangePassword:false,instructorProfile:{instructorCode:profile.instructorCode,fullName:profile.fullName,phone:profile.phone,department:profile.department,expertise:profile.expertise}};
      success(await request('PATCH',`/api/admin/users/${accounts.i2.id}`,body,'admin'),'Temporarily activate UAT I2');
      restoreI2Inactive = true;
      console.log('SETUP UAT I2 temporarily activated; original status INACTIVE will be restored');
      login = await request('POST','/api/auth/login',{email:accounts[actor].email,password:fixture.password});
    }
    tokens[actor] = success(login, 'Login '+actor).accessToken;
  }
  const term = success(await request('GET','/api/terms/available',undefined,'admin'),'Open term');
  ensure(term.some(x=>x.code==='SU26'), 'SU26 is not OPEN');

  // U10: proposal lifecycle and review scope.
  let proposal;
  await check('U10-H1/H2', async()=>{
    proposal = success(await request('POST',`/api/groups/${g1}/problems/propose`,{title:'UAT API Proposal Review',statement:'Bài kiểm tra API đề xuất và duyệt đề tài.',difficultyLevel:'BEGINNER',domainCode:'UATD0928A',expectedOutput:'UAT API result'},'s1'),'Propose');
    ensure(proposal.status==='PENDING_REVIEW','Proposal is not pending');
    const updated=success(await request('PUT',`/api/groups/${g1}/problems/proposals/${proposal.id}`,{title:'UAT API Proposal Review Updated',statement:'Nội dung sau khi sửa cho kiểm thử API.',difficultyLevel:'BEGINNER',domainCode:'UATD0928A',expectedOutput:'UAT API result'},'s1'),'Update proposal');
    ensure(updated.id===proposal.id && updated.title.includes('Updated'),'Proposal update created another item or lost title');
    return `proposal=${proposal.id}`;
  });
  if (proposal) {
    await check('U10-N1',async()=>{const pending=success(await request('GET','/api/instructor/problems/pending',undefined,'i2'),'I2 pending'); ensure(!pending.some(x=>x.id===proposal.id),'I2 saw G1 proposal'); rejected(await request('PATCH',`/api/instructor/problems/${proposal.id}/review`,{status:'APPROVED',comment:'out of scope'},'i2'),'I2 review');});
    await check('U10-H3',async()=>{const p=success(await request('PATCH',`/api/instructor/problems/${proposal.id}/review`,{status:'APPROVED',comment:'UAT API approved'},'i1'),'I1 approve'); ensure(p.status==='ACTIVE' && p.sourceType==='OFFICIAL','Approved proposal is not official ACTIVE');});
  }
  await check('U10-H4',async()=>{
    const p=success(await request('POST',`/api/groups/${g1}/problems/propose`,{title:'UAT API Reject',statement:'Bài kiểm tra lý do từ chối.',difficultyLevel:'BEGINNER',domainCode:'UATD0928A'},'s1'),'Reject proposal fixture');
    rejected(await request('PATCH',`/api/instructor/problems/${p.id}/review`,{status:'REJECTED',comment:''},'i1'),'Reject without reason');
    const reviewed=success(await request('PATCH',`/api/instructor/problems/${p.id}/review`,{status:'REJECTED',comment:'UAT: thiếu phạm vi nghiên cứu'},'i1'),'Reject with reason');
    ensure(reviewed.status==='REJECTED','Proposal not rejected'); return `proposal=${p.id}`;
  });
  await check('U10-H6',async()=>{
    const pending=fixture.steps.pendingProposal; const p=success(await request('PATCH',`/api/admin/problems/${pending.id}/review`,{status:'APPROVED',comment:'UAT admin review'},'admin'),'Admin review G2');
    ensure(p.status==='ACTIVE' && p.sourceType==='OFFICIAL','Admin approval not official ACTIVE'); return `proposal=${p.id}`;
  });
  await check('U10-restore-UAT-selection',async()=>{
    success(await request('POST',`/api/groups/${g1}/problems/select`,{problemId:p1},'s1'),'Restore G1 selection');
    const group=success(await request('GET',`/api/groups/${g1}`,undefined,'s1'),'Read G1 after restore');
    ensure(group.selectedProblem?.id===p1,'G1 original selection not restored');
  });

  // U11: a fresh task, versioned update, cross-member visibility, checklist, comments, archive/restore.
  let task;
  await check('U11-H2/H3',async()=>{
    task=success(await request('POST',`/api/groups/${g1}/tasks`,{title:'UAT API Task 0928',description:'Created by automated UAT',status:'TODO',priority:'HIGH',dueAt:new Date(Date.now()+5*86400000).toISOString(),assigneeStudentIds:[accounts.s2.studentProfile.id],boardId:id('board.g1')},'s1'),'Create task');
    ensure(task.assignees.some(a=>a.studentId===accounts.s2.studentProfile.id),'S2 not assigned');
    const mine=success(await request('GET',`/api/tasks/me?groupId=${g1}`,undefined,'s2'),'S2 tasks');
    ensure(mine.content.some(x=>x.id===task.id),'S2 does not see assigned task'); return `task=${task.id}`;
  });
  if (task) {
    await check('U11-H4/H5',async()=>{
      task=success(await request('PATCH',`/api/groups/${g1}/tasks/${task.id}`,{title:'UAT API Task Updated',priority:'URGENT',version:task.version},'s1'),'Update task');
      task=success(await request('PATCH',`/api/groups/${g1}/tasks/${task.id}/move`,{status:'IN_PROGRESS',position:0,version:task.version},'s1'),'Move task');
      const fresh=success(await request('GET',`/api/groups/${g1}/tasks/${task.id}`,undefined,'s2'),'Reload task');
      ensure(fresh.title==='UAT API Task Updated' && fresh.status==='IN_PROGRESS','Task changes not persisted');
    });
    await check('U11-H6/H7/H8',async()=>{
      const item=success(await request('POST',`/api/groups/${g1}/tasks/${task.id}/checklist-items`,{title:'UAT API checklist'},'s1'),'Add checklist');
      success(await request('PATCH',`/api/groups/${g1}/tasks/${task.id}/checklist-items/${item.id}`,{completed:true},'s2'),'S2 complete checklist');
      const c=success(await request('POST',`/api/groups/${g1}/tasks/${task.id}/comments`,{content:'UAT API comment from S2'},'s2'),'Comment');
      const comments=success(await request('GET',`/api/groups/${g1}/tasks/${task.id}/comments`,undefined,'s1'),'List comments');
      ensure(comments.content.some(x=>x.id===c.id),'Comment not visible to S1');
      const activities=success(await request('GET',`/api/groups/${g1}/tasks/${task.id}/activities`,undefined,'s1'),'Activities');
      ensure(activities.content.length>0,'No task activities');
      success(await request('DELETE',`/api/groups/${g1}/tasks/${task.id}/comments/${c.id}`,undefined,'s2'),'Delete own comment');
    });
    await check('U11-H10',async()=>{
      success(await request('DELETE',`/api/groups/${g1}/tasks/${task.id}`,undefined,'s1'),'Archive task');
      const archived=success(await request('GET',`/api/groups/${g1}/tasks/${task.id}`,undefined,'s1'),'Read archived');
      ensure(Boolean(archived.archivedAt),'Task not archived');
      const restored=success(await request('POST',`/api/groups/${g1}/tasks/${task.id}/restore`,undefined,'s1'),'Restore task');
      ensure(!restored.archivedAt,'Task not restored');
    });
  }
  await check('U11-N2',async()=>{const board=success(await request('GET',`/api/groups/${g2}/board?boardId=${id('board.g2')}`,undefined,'s3'),'G2 board'); ensure(!board.columns.some(c=>c.tasks.some(t=>t.id===task?.id)),'G1 task leaked to G2');});

  // U12/U13: availability and meetings, using only UAT groups.
  let slot;
  await check('U12-H1/H2',async()=>{
    const start=new Date(Date.now()+4*86400000); start.setUTCHours(3,0,0,0); const end=new Date(start.getTime()+3600000);
    slot=success(await request('POST','/api/mentor/availability',{startAt:start.toISOString(),endAt:end.toISOString(),meetLink:'https://meet.google.com/abc-defg-hij',note:'UAT API slot'},'m1'),'Create slot');
    ensure(slot.status==='AVAILABLE','Slot not available');
    slot=success(await request('PATCH',`/api/mentor/availability/${slot.id}`,{note:'UAT API slot updated'},'m1'),'Update slot');
    ensure(slot.note==='UAT API slot updated','Slot note not saved'); return `slot=${slot.id}`;
  });
  if(slot) {
    await check('U12-N1',async()=>{rejected(await request('POST','/api/mentor/availability',{startAt:slot.startAt,endAt:slot.endAt,meetLink:'https://meet.google.com/abc-defg-hij',note:'UAT overlapping slot'},'m1'),'Overlap');});
    await check('U12-H3',async()=>{
      const first=await request('POST',`/api/groups/${g2}/mentor/meetings`,{slotId:slot.id},'s3');
      const second=await request('POST',`/api/groups/${g2}/mentor/meetings`,{slotId:slot.id},'s3');
      const statuses=[first.status,second.status]; ensure(statuses.filter(s=>s<300).length===1 && statuses.filter(s=>s>=400 && s<500).length===1,`Expected exactly one booking: ${statuses}`);
    });
    blocked('U18-N4','Cross-group simultaneous booking needs G3 leader S5; saved UAT password no longer works.');
  }
  await check('U13-N1',async()=>{rejected(await request('POST',`/api/groups/${g1}/mentor/meetings`,{startAt:new Date(Date.now()+7*86400000).toISOString(),endAt:new Date(Date.now()+7*86400000+3600000).toISOString(),meetLink:'https://meet.google.com/abc-defg-hij'},'m1'),'G1 over quota');});
  await check('U13-H4',async()=>{
    const past=id('meeting.past'); const meeting=success(await request('PUT',`/api/groups/${g1}/mentor/meetings/${past}/evidence`,{imageUrl:'http://localhost:3000/logo.png'},'s2'),'Submit evidence');
    ensure(meeting.status==='COMPLETED' && meeting.evidenceSubmittedByStudentId===accounts.s2.studentProfile.id,'Meeting not completed by S2');
  });
  await check('U13-H5',async()=>{const file=await request('GET','/api/mentor/meeting-reports/export.xlsx?term=SU26',undefined,'m1',true); ensure(file.status===200 && file.bytes.length>100 && file.bytes.subarray(0,2).toString()==='PK','Mentor XLSX invalid'); return `bytes=${file.bytes.length}`;});

  // U14/U15: read milestone scope, grade agreed milestone, verify arithmetic and CSV.
  await check('U14-H1/H2',async()=>{const milestones=success(await request('GET','/api/course-milestones?term=SU26&courseCode=EXE101',undefined,'i1'),'List milestones'); ensure(milestones.some(x=>x.id===ms1&&x.weight===40)&&milestones.some(x=>x.id===ms2&&x.weight===60),'40/60 milestone setup wrong');});
  await check('U14-N1',async()=>{const milestones=success(await request('GET','/api/course-milestones?term=SU26&courseCode=EXE101',undefined,'i2'),'I2 milestones'); ensure(!milestones.some(x=>x.id===ms1),'I2 sees I1 milestone');});
  await check('U15-H5/H6',async()=>{
    const matrixBefore=success(await request('GET',`/api/groups/${g1}/grades`,undefined,'s1'),'Matrix before');
    const milestone=matrixBefore.milestones.find(x=>x.milestoneId===ms2);
    ensure(milestone?.contributionAgreementStatus==='AGREED','MS2 not agreed');
    if (!milestone.graded) success(await request('PUT',`/api/instructor/milestones/${ms2}/groups/${g1}/grade`,{score:9,feedback:'UAT API final grade'},'i1'),'Grade MS2');
    const matrix=success(await request('GET',`/api/groups/${g1}/grades`,undefined,'s2'),'Matrix after');
    const s1=matrix.members.find(x=>x.studentId===accounts.s1.studentProfile.id); const s2=matrix.members.find(x=>x.studentId===accounts.s2.studentProfile.id);
    ensure(s1?.totalScore===8.6 && s2?.totalScore===7.96,`Expected 8.6/7.96, got ${s1?.totalScore}/${s2?.totalScore}`); return `S1=${s1.totalScore}, S2=${s2.totalScore}`;
  });
  await check('U15-N2',async()=>{rejected(await request('PUT',`/api/groups/${g1}/milestones/${ms2}/contributions`,{items:[{studentId:accounts.s1.studentProfile.id,contributionPercent:90},{studentId:accounts.s2.studentProfile.id,contributionPercent:90}]},'s1'),'Edit graded contribution');});
  await check('U15-H7',async()=>{const file=await request('GET','/api/instructor/grades/export.csv?term=SU26&courseCode=EXE101',undefined,'i1',true); const csv=file.bytes.toString('utf8'); ensure(file.status===200 && csv.includes('UATS10928A') && csv.includes('UATS20928A') && csv.includes('8.6'),'Grade CSV incomplete'); return `bytes=${file.bytes.length}`;});

  // U16/U17/U18: notifications, dashboards, auth boundary, persisted reads.
  await check('U16-H2/H3',async()=>{
    const before=success(await request('GET','/api/notifications/unread-count',undefined,'s2'),'Unread count');
    const page=success(await request('GET','/api/notifications?size=50',undefined,'s2'),'Notifications');
    ensure(page.content.length>0,'S2 has no notifications');
    const target=page.content.find(x=>!x.read) || page.content[0];
    success(await request('PATCH',`/api/notifications/${target.id}/read`,undefined,'s2'),'Read one');
    success(await request('PATCH','/api/notifications/read-all',undefined,'s2'),'Read all');
    const after=success(await request('GET','/api/notifications/unread-count',undefined,'s2'),'Unread after');
    ensure(after===0,`Unread ${after} after read all`); return `before=${before}, after=${after}`;
  });
  await check('U16-N1',async()=>{const other=success(await request('GET','/api/notifications?size=100',undefined,'s3'),'S3 notifications'); const own=success(await request('GET','/api/notifications?size=100',undefined,'s2'),'S2 notifications'); ensure(!other.content.some(x=>own.content.some(y=>y.id===x.id)),'Notification leaked across students');});
  await check('U17-H1/H2/H3/H4',async()=>{
    for(const [who,route] of [['admin','/api/dashboard/admin/overview?term=SU26&courseCode=EXE101'],['s1','/api/dashboard/student/progress'],['i1','/api/dashboard/instructor/milestones?term=SU26&courseCode=EXE101'],['m1','/api/dashboard/mentor/groups'],['admin','/api/dashboard/tv-showcase/projects?term=SU26&courseCode=EXE101']]) success(await request('GET',route,undefined,who),'Dashboard '+who+' '+route);
  });
  await check('U18-N1/N2',async()=>{rejected(await request('GET','/api/admin/users',undefined,'s1'),'Student admin route'); rejected(await request('GET','/api/instructor/submissions',undefined,'s1'),'Student instructor route'); rejected(await request('GET',`/api/groups/${g1}/tasks/${id('task.g1.0')}`,undefined,'s3'),'Other-group task'); rejected(await request('GET','/api/admin/backups'),'Unauthenticated backup');});
  await check('U18-H1',async()=>{const stored=success(await request('GET',`/api/groups/${g1}/tasks/${task.id}`,undefined,'s2'),'Reload UAT task'); ensure(stored.title==='UAT API Task Updated' && !stored.archivedAt,'Task persistence failed');});

  // U19/U20 require closing the real SU26 term and archiving imported students.
  blocked('U19-H1..H7','SU26 contains 231 groups and 1270 students; closing it would affect imported data. Feedback writes require a CLOSED term.');
  blocked('U20-H1..H3','Archiving SU26 would deactivate imported student accounts; no isolated UAT term is available.');
  await check('U19-read-only',async()=>{const feedback=success(await request('GET','/api/feedback/me?term=SU26',undefined,'s1'),'Feedback pending'); ensure(Array.isArray(feedback),'Feedback list invalid');});

  // U21: create and download a backup; leave the existing schedule untouched.
  await check('U21-H3-read',async()=>{const schedule=success(await request('GET','/api/admin/backups/schedule',undefined,'admin'),'Backup schedule'); ensure(typeof schedule.enabled==='boolean','Schedule unavailable'); return `enabled=${schedule.enabled}`;});
  await check('U21-H1/H2',async()=>{
    let job=success(await request('POST','/api/admin/backups',undefined,'admin'),'Create backup');
    for(let i=0;i<25 && !['SUCCEEDED','FAILED'].includes(job.status);i++){await sleep(400);job=success(await request('GET',`/api/admin/backups/${job.id}`,undefined,'admin'),'Poll backup');}
    ensure(job.status==='SUCCEEDED',`Backup job ${job.id} ended ${job.status}: ${job.errorMessage||''}`);
    const file=await request('GET',`/api/admin/backups/${job.id}/download`,undefined,'admin',true);
    ensure(file.status===200 && file.bytes.length>10000 && (file.headers.get('content-disposition')||'').includes('.dump'),'Backup download invalid');
    const list=success(await request('GET','/api/admin/backups?status=SUCCEEDED',undefined,'admin'),'Backup list'); ensure(list.content.some(x=>x.id===job.id),'Backup not in history');
    return `job=${job.id}, bytes=${file.bytes.length}`;
  });
  blocked('U21-restore','Database restore requires a separate disposable PostgreSQL stack; current SU26 contains imported data.');

  // U22: legacy submission endpoints are read-only without a seeded legacy submission.
  await check('U22-H1/H2',async()=>{success(await request('GET',`/api/milestone-submissions/groups/${g1}`,undefined,'s1'),'Student legacy submissions'); success(await request('GET',`/api/instructor/submissions?term=SU26&courseCode=EXE101&groupId=${g1}`,undefined,'i1'),'Instructor legacy submissions');});
  blocked('U22-grade-detail','No legacy submission fixture exists; creation/grade legacy paths were removed in the Java oracle.');

  if (restoreI2Inactive) await check('RESTORE-I2',async()=>{
    const profile=accounts.i2.instructorProfile;
    const body={email:accounts.i2.email,status:'INACTIVE',mustChangePassword:false,instructorProfile:{instructorCode:profile.instructorCode,fullName:profile.fullName,phone:profile.phone,department:profile.department,expertise:profile.expertise}};
    const restored=success(await request('PATCH',`/api/admin/users/${accounts.i2.id}`,body,'admin'),'Restore I2 status');
    ensure(restored.status==='INACTIVE','I2 original status not restored');
  });

  const counts=Object.fromEntries(['PASS','FAIL','BLOCKED'].map(status=>[status,results.filter(x=>x.status===status).length]));
  console.log('SUMMARY '+JSON.stringify({counts,results}));
  if(counts.FAIL) process.exitCode=1;
}
main().catch(error=>{console.error('FATAL '+error.message);process.exitCode=2;});
