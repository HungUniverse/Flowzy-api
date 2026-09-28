// Local-only demo preparation. Never prints tokens or existing account passwords.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { execFileSync } = require('node:child_process');
const ROOT = path.resolve(__dirname, '..');
const OUT = path.join(ROOT, '.tools', 'local-demo-20260928');
const BASE = 'http://localhost:8080';
const FILE = 'C:/Users/lenovo/Downloads/SU26_EXE101 _ Group List (mentor).xlsx';
fs.mkdirSync(OUT, { recursive: true });
const statePath = path.join(OUT, 'state.json');
let state = fs.existsSync(statePath) ? JSON.parse(fs.readFileSync(statePath, 'utf8')) : {};
function save() { fs.writeFileSync(statePath, JSON.stringify(state, null, 2)); }
const delay = ms => new Promise(r => setTimeout(r, ms));
async function api(method, route, body, token) {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  if (body && !(body instanceof FormData)) headers['Content-Type'] = 'application/json';
  const response = await fetch(BASE + route, { method, headers, body: body instanceof FormData ? body : body === undefined ? undefined : JSON.stringify(body) });
  const data = await response.json();
  if (!response.ok) throw new Error(`${method} ${route}: ${response.status} ${data.message || JSON.stringify(data.errors || {})}`);
  return data.data;
}
async function login(email, password) { return (await api('POST', '/api/auth/login', { email, password })).accessToken; }
async function adminToken() {
  const cfg = JSON.parse(fs.readFileSync(path.join(ROOT, 'src/Flowzy.Api/appsettings.Development.json'), 'utf8'));
  return login(cfg.Admin.Email, cfg.Admin.Password);
}
async function once(key, work) {
  state.steps ||= {};
  if (Object.hasOwn(state.steps, key)) return state.steps[key];
  const value = await work(); state.steps[key] = value ?? true; save();
  console.log('Prepared: ' + key); return value;
}
async function seed(admin) {
  if (state.import?.status !== 'COMPLETED') throw new Error('Wait for completed import before seeding');
  const terms = await api('GET', '/api/terms/available', undefined, admin);
  if (!terms.some(t => t.code === 'SU26')) throw new Error('SU26 is not open');
  state.password ||= 'Flowzy-UAT-' + crypto.randomBytes(9).toString('base64url') + '!'; save();
  const actors = {}, tokens = {};
  for (const key of ['i1','i2','m1','m2','s1','s2','s3','s4','s5','s6','s7']) {
    const role = key[0] === 'i' ? 'INSTRUCTOR' : key[0] === 'm' ? 'MENTOR' : 'STUDENT';
    const email = `uat.${key}.0928a@example.com`, fullName = `UAT ${role} ${key.slice(1)}`, code = `UAT${key.toUpperCase()}0928A`;
    const profileKey = role.toLowerCase() + 'Profile';
    const profile = { fullName, [role.toLowerCase() + 'Code']: code };
    if (role === 'STUDENT') Object.assign(profile, { major: 'Software Engineering', cohort: 'UAT', className: 'UAT Local' });
    if (role === 'MENTOR') Object.assign(profile, { company: 'UAT Demo', expertise: 'Web applications; testing', yearsOfExperience: 5 });
    if (role === 'INSTRUCTOR') Object.assign(profile, { department: 'UAT Software', expertise: 'Project review' });
    const user = await once('account.' + key, async () => {
      const found = await api('GET', '/api/admin/users?search=' + encodeURIComponent(email), undefined, admin);
      if (found.content.some(u => u.email === email)) throw new Error('Existing account collision; no password/profile changes: ' + email);
      return api('POST', '/api/admin/users', { email, role, initialPassword: state.password, [profileKey]: profile }, admin);
    });
    await once('activate.' + key, () => api('PATCH', `/api/admin/users/${user.id}`, { email, status: 'ACTIVE', mustChangePassword: false, [profileKey]: profile }, admin));
    actors[key] = user; tokens[key] = await login(email, state.password);
  }
  fs.writeFileSync(path.join(OUT, 'test-accounts.local.json'), JSON.stringify({ warning: 'LOCAL UAT ONLY. Do not deploy these accounts/passwords.', password: state.password, accounts: Object.entries(actors).map(([key,u]) => ({ key, email: u.email, role: u.role, accountId: u.id })) }, null, 2));
  const groups = {};
  for (const [key, leader, member, suffix] of [['g1','s1','s2','A'],['g2','s3','s4','B'],['g3','s5',null,'C']]) {
    const group = await once('group.'+key, () => api('POST', '/api/groups', {
      term: 'SU26', courseCode: 'EXE101', name: 'UAT Flowzy Team '+suffix,
      projectName: 'UAT Campus Project '+suffix, ideaDescription: 'Dữ liệu giả phục vụ test local, không phải dự án sinh viên thật.', researchDomain: 'Software',
      targetGrade: 8, recruitmentNeeds: [{ role: 'SOFTWARE_DEVELOPER', quantity: 1 },{ role: 'UI_UX_DESIGNER', quantity: 1 }]
    }, tokens[leader]));
    groups[key] = group;
    if (member) {
      const invitation = await once('invitation.'+key, () => api('POST', `/api/groups/${group.id}/invitations`, { studentCodeOrEmail: actors[member].email, message: 'UAT: mời thành viên để test' }, tokens[leader]));
      await once('accept.'+key, () => api('POST', `/api/groups/invitations/${invitation.id}/accept`, undefined, tokens[member]));
    }
    // G3 is deliberately left unclaimed for instructor claim tests.
    if (key !== 'g3') await once('instructor.'+key, () => api('PATCH', `/api/groups/${group.id}/instructor`, { instructorId: actors.i1.id }, admin));
    await once('mentor.'+key, () => api('PATCH', `/api/groups/${group.id}/mentor`, { mentorId: actors.m1.id }, admin));
  }
  await once('pendingInvitation', () => api('POST', `/api/groups/${groups.g3.id}/invitations`, { studentCodeOrEmail: actors.s6.email, message: 'UAT: giữ PENDING để test Accept/Decline' }, tokens.s5));
  await once('pendingJoinRequest', () => api('POST', `/api/groups/${groups.g3.id}/join-requests`, { message: 'UAT: giữ PENDING để leader duyệt' }, tokens.s7));
  const domain = await once('domain', () => api('POST', '/api/admin/problem-domains', { code: 'UATD0928A', name: 'UAT Software', description: 'Danh mục giả cho kiểm thử local', status: 'ACTIVE' }, admin));
  for (const [key, title, difficulty, status] of [['p1','UAT Campus Booking','BEGINNER','ACTIVE'],['p2','UAT Smart Inventory','INTERMEDIATE','ACTIVE'],['p3','UAT Inactive Topic','ADVANCED','INACTIVE']]) {
    await once('problem.'+key, () => api('POST', '/api/admin/problems', { code: 'UAT0928'+key.toUpperCase(), title, domainCode: domain.code,
      statement: 'Bài toán giả: thiết kế ứng dụng quản lý, phân quyền và báo cáo phục vụ kiểm thử nghiệp vụ.', difficultyLevel: difficulty,
      expectedOutput: 'Prototype, source code, test report', status }, admin));
  }
  await once('selectedProblem', () => api('POST', `/api/groups/${groups.g1.id}/problems/select`, { problemId: state.steps['problem.p1'].id }, tokens.s1));
  await once('pendingProposal', () => api('POST', `/api/groups/${groups.g2.id}/problems/propose`, { title: 'UAT Proposal - chờ giảng viên duyệt', statement: 'Xây dựng ứng dụng đặt lịch phòng học và theo dõi sử dụng tài nguyên.', difficultyLevel: 'BEGINNER', domainCode: domain.code, expectedOutput: 'Prototype và báo cáo kiểm thử' }, tokens.s3));
  for (const [groupKey, leader, member] of [['g1','s1','s2'],['g2','s3','s4']]) {
    const gid = groups[groupKey].id;
    const board = await once('board.'+groupKey, () => api('POST', `/api/groups/${gid}/boards`, { name: 'UAT Sprint 1', description: 'Dữ liệu mẫu cho Kanban' }, tokens[leader]));
    for (const [index, status] of ['BACKLOG','TODO','IN_PROGRESS','REVIEW','DONE'].entries()) {
      const key = `${groupKey}.${index}`;
      const task = await once('task.'+key, () => api('POST', `/api/groups/${gid}/tasks`, {
        title: ['UAT Phân tích yêu cầu','UAT Thiết kế màn hình','UAT Xây dựng API','UAT Review mã nguồn','UAT Viết báo cáo'][index],
        description: 'Task giả phục vụ thao tác FE. Có thể sửa, kéo thả, bình luận và archive.', status: status === 'BACKLOG' ? 'BACKLOG' : 'TODO',
        priority: ['LOW','MEDIUM','HIGH','URGENT','MEDIUM'][index], dueAt: new Date(Date.now()+(index+1)*86400000).toISOString(),
        assigneeStudentIds: [actors[index%2 ? member : leader].studentProfile.id], boardId: board.id
      }, tokens[leader]));
      if (!['BACKLOG','TODO'].includes(status)) await once('move.'+key, () => api('PATCH', `/api/groups/${gid}/tasks/${task.id}/move`, { status, position: 0, version: task.version }, tokens[leader]));
      if (index === 2) {
        const item = await once('checklist1.'+key, () => api('POST', `/api/groups/${gid}/tasks/${task.id}/checklist-items`, { title: 'Kiểm tra trường bắt buộc' }, tokens[leader]));
        await once('checklistDone.'+key, () => api('PATCH', `/api/groups/${gid}/tasks/${task.id}/checklist-items/${item.id}`, { completed: true }, tokens[leader]));
        await once('checklist2.'+key, () => api('POST', `/api/groups/${gid}/tasks/${task.id}/checklist-items`, { title: 'Kiểm tra quyền truy cập' }, tokens[leader]));
        await once('comment.'+key, () => api('POST', `/api/groups/${gid}/tasks/${task.id}/comments`, { content: 'UAT: đã hoàn tất phần validation, cần kiểm tra thêm phân quyền.' }, tokens[member]));
      }
    }
  }
  for (const [key,title,weight,position] of [['ms1','UAT Prototype',40,1],['ms2','UAT Final',60,2]]) {
    await once(key, () => api('POST', '/api/course-milestones', { term: 'SU26', courseCode: 'EXE101', title, description: 'Milestone giả dành riêng cho nhóm do UAT instructor phụ trách.', weight, maxScore: 10, position, deadlineAt: new Date(Date.now()+position*7*86400000).toISOString() }, tokens.i1));
  }
  const gid = groups.g1.id;
  for (const milestone of ['ms1','ms2']) {
    const mid = state.steps[milestone].id;
    await once('contributions.g1.'+milestone, () => api('PUT', `/api/groups/${gid}/milestones/${mid}/contributions`, { items: [{ studentId: actors.s1.studentProfile.id, contributionPercent:100 },{ studentId: actors.s2.studentProfile.id, contributionPercent: milestone==='ms1'?80:100 }] }, tokens.s1));
    for (const actor of ['s1','s2']) await once('agree.'+milestone+'.'+actor, () => api('PUT', `/api/groups/${gid}/milestones/${mid}/contribution-agreement`, { decision:'AGREE' }, tokens[actor]));
  }
  await once('grade.ms1', () => api('PUT', `/api/instructor/milestones/${state.steps.ms1.id}/groups/${gid}/grade`, { score:8, feedback:'UAT: prototype đạt yêu cầu; đây là điểm giả để test hiển thị.' }, tokens.i1));
  const g2 = groups.g2.id, ms1 = state.steps.ms1.id;
  await once('contributions.g2', () => api('PUT', `/api/groups/${g2}/milestones/${ms1}/contributions`, { items:[{studentId:actors.s3.studentProfile.id,contributionPercent:100},{studentId:actors.s4.studentProfile.id,contributionPercent:70}] },tokens.s3));
  await once('changes.g2', () => api('PUT', `/api/groups/${g2}/milestones/${ms1}/contribution-agreement`, {decision:'REQUEST_CHANGES',reason:'UAT: cần leader điều chỉnh tỷ lệ đóng góp trước khi đồng thuận.'},tokens.s4));
  function atDay(offset,hour) { const d = new Date(Date.now()+offset*86400000); const date = new Date(d.getTime()+7*3600000).toISOString().slice(0,10); return `${date}T${String(hour).padStart(2,'0')}:00:00+07:00`; }
  for (const [key,hour] of [['slot1',9],['slot2',11],['slot3',14]]) {
    await once(key, () => api('POST','/api/mentor/availability',{startAt:atDay(1,hour),endAt:atDay(1,hour+1),meetLink:'https://meet.google.com/abc-defg-hij',note:'UAT: link định dạng mẫu, không phải phòng họp thật'},tokens.m1));
  }
  await once('meeting.future', () => api('POST',`/api/groups/${gid}/mentor/meetings`,{slotId:state.steps.slot1.id},tokens.s1));
  await once('meeting.past', () => api('POST',`/api/groups/${gid}/mentor/meetings`,{startAt:new Date(atDay(-1,9)).toISOString(),endAt:new Date(atDay(-1,10)).toISOString(),meetLink:'https://meet.google.com/abc-defg-hij',note:'UAT: cuộc họp đã qua, chờ student nộp minh chứng để hoàn tất'},tokens.m1));
  state.seedCompletedAt = new Date().toISOString(); save();
  console.log(JSON.stringify({ seedCompleted:true, groupIds:Object.fromEntries(Object.entries(groups).map(([k,g])=>[k,g.id])), credentialsFile:path.join(OUT,'test-accounts.local.json') }));
}
async function main() {
  const mode = process.argv[2];
  const admin = await adminToken();
  if (mode === 'seed') return seed(admin);
  if (mode === 'verify') {
    const checks=[];
    const check=(name,condition)=>{checks.push({name,pass:Boolean(condition)});if(!condition)throw new Error('Verification failed: '+name);};
    const tokens={};
    for(const key of ['i1','i2','m1','m2','s1','s2','s3','s4','s5','s6','s7']) {
      const user=state.steps['account.'+key]; tokens[key]=await login(user.email,state.password);
      const me=await api('GET','/api/auth/me',undefined,tokens[key]);check('Login '+key,me.email===user.email&&!me.mustChangePassword);
    }
    const g1=state.steps['group.g1'].id,g2=state.steps['group.g2'].id,g3=state.steps['group.g3'].id;
    for(const [gid,key,count] of [[g1,'s1',2],[g2,'s3',2],[g3,'s5',1]]) {
      const group=await api('GET',`/api/groups/${gid}`,undefined,tokens[key]);check('Members '+gid,group.members.length===count&&!group.studentReadOnly);
    }
    const instructorGroups=await api('GET','/api/groups/instructor/me',undefined,tokens.i1);check('I1 groups',instructorGroups.length===2&&instructorGroups.every(g=>[g1,g2].includes(g.id)));
    check('I2 unassigned',(await api('GET','/api/groups/instructor/me',undefined,tokens.i2)).length===0);
    check('M1 groups',(await api('GET','/api/groups/mentor/me',undefined,tokens.m1)).length===3);
    check('M2 unassigned',(await api('GET','/api/groups/mentor/me',undefined,tokens.m2)).length===0);
    for(const [key,actor] of [['g1','s1'],['g2','s3']]) {
      const board=await api('GET',`/api/groups/${state.steps['group.'+key].id}/board?boardId=${state.steps['board.'+key].id}`,undefined,tokens[actor]);
      check('Five task states '+key,board.activeTaskCount===5&&board.columns.filter(c=>c.tasks.length===1).length===5);
    }
    const matrix=await api('GET',`/api/groups/${g1}/grades`,undefined,tokens.s1);
    check('G1 MS1 graded',matrix.milestones.find(m=>m.milestoneId===state.steps.ms1.id).groupGrade.score===8);
    check('G1 MS2 ready',matrix.milestones.find(m=>m.milestoneId===state.steps.ms2.id).contributionAgreementStatus==='AGREED'&&!matrix.milestones.find(m=>m.milestoneId===state.steps.ms2.id).graded);
    check('G1 current weighted totals',matrix.members.find(m=>m.studentId===state.steps['account.s1'].studentProfile.id).totalScore===3.2&&matrix.members.find(m=>m.studentId===state.steps['account.s2'].studentProfile.id).totalScore===2.56);
    const second=await api('GET',`/api/groups/${g2}/grades`,undefined,tokens.s3);check('G2 changes requested',second.milestones.find(m=>m.milestoneId===state.steps.ms1.id).contributionAgreementStatus==='CHANGES_REQUESTED');
    const meetings=await api('GET',`/api/groups/${g1}/mentor/meetings`,undefined,tokens.s1);check('G1 two scheduled meetings',meetings.length===2&&meetings.every(m=>m.status==='SCHEDULED'));
    check('Two available slots',(await api('GET',`/api/groups/${g2}/mentor/availability`,undefined,tokens.s3)).length===2);
    check('S6 pending invitation',(await api('GET','/api/groups/invitations/me',undefined,tokens.s6)).some(i=>i.groupId===g3&&i.status==='PENDING'));
    check('S7 pending join',(await api('GET','/api/groups/join-requests/me',undefined,tokens.s7)).some(i=>i.groupId===g3&&i.status==='PENDING'));
    check('S2 has notifications',(await api('GET','/api/notifications?size=20',undefined,tokens.s2)).totalElements>0);
    check('Frontend login reachable',(await fetch('http://localhost:3000/login')).status===200);
    state.verification={at:new Date().toISOString(),checks};save();console.log(JSON.stringify({passed:checks.length,failed:0,groupIds:{g1,g2,g3},currentGrades:matrix.members.map(m=>({code:m.studentCode,total:m.totalScore,complete:m.complete}))}));return;
  }
  if (mode === 'remove-import-collisions') {
    const collisions = JSON.parse(execFileSync('docker',['exec','flowzy-api-postgres-1','psql','-U','flowzy','-d','flowzy','-tAc',
      "SELECT coalesce(json_agg(t),'[]') FROM (SELECT g.id AS group_id,s.id AS student_id,a.email FROM import_row_errors e JOIN students s ON lower(s.student_code)=lower(e.raw_data::jsonb->>'student_code') JOIN accounts a ON a.id=s.account_id JOIN student_group_members m ON m.student_id=s.id JOIN student_groups g ON g.id=m.group_id WHERE e.batch_id=3 AND e.error_code='ALREADY_EXISTS' AND g.import_batch_id=3 AND a.email LIKE '%.demo@example.com' AND lower(a.email)<>lower(e.raw_data::jsonb->>'email')) t"],{encoding:'utf8'}).trim());
    state.collisionRemovals ||= [];
    for(const collision of collisions) {
      const group = await api('GET',`/api/groups/${collision.group_id}`,undefined,admin);
      let replacementLeader = null;
      if(group.leader.id===collision.student_id) {
        // Match the importer's documented first-valid-member fallback; record that the source leader is unresolved.
        replacementLeader = group.members.find(m=>m.studentId!==collision.student_id && !m.email.endsWith('.demo@example.com'));
        if(!replacementLeader) throw new Error('No safe fallback leader for imported group '+group.id);
        await api('PATCH',`/api/groups/${group.id}/leader`,{studentId:replacementLeader.studentId},admin);
      }
      await api('DELETE',`/api/groups/${group.id}/members/${collision.student_id}`,undefined,admin);
      state.collisionRemovals.push({...collision,temporaryLeaderStudentId:replacementLeader?.studentId??null}); save();
    }
    console.log(JSON.stringify({removedWrongMemberships:state.collisionRemovals.length,accountRecordsDeleted:0,temporaryLeaderGroups:state.collisionRemovals.filter(x=>x.temporaryLeaderStudentId).map(x=>x.group_id)}));return;
  }
  if (mode === 'reconcile-mentors') {
    if (state.import?.status !== 'COMPLETED') throw new Error('Import not complete');
    const workbookGroups = JSON.parse(execFileSync('C:/Users/lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe', ['-X','utf8',path.join(__dirname,'Inspect-GroupImport.py')], {encoding:'utf8'}));
    const mentors = [];
    for (let page=0;;page++) { const p=await api('GET',`/api/admin/users?role=MENTOR&size=100&page=${page}`,undefined,admin); mentors.push(...p.content); if(!p.hasNext) break; }
    const importedIds = JSON.parse(execFileSync('docker',['exec','flowzy-api-postgres-1','psql','-U','flowzy','-d','flowzy','-tAc',"SELECT coalesce(json_agg(id),'[]') FROM student_groups WHERE import_batch_id=3"],{encoding:'utf8'}).trim());
    const groups = (await api('GET','/api/groups',undefined,admin)).filter(g=>importedIds.includes(g.id));
    const normal = value=>value.trim().replace(/\s+/g,' ').toLowerCase();
    const unresolved=[]; let corrected=0;
    for (const source of workbookGroups) {
      const matches=groups.filter(g=>g.term==='SU26' && g.groupNo===source.groupNo && normal(g.name)===normal(source.name));
      if(matches.length!==1 || source.mentorCodes.length!==1) { unresolved.push({groupNo:source.groupNo,reason:'Ambiguous source/group match'}); continue; }
      const group=matches[0], code=source.mentorCodes[0];
      const people=mentors.filter(m=>m.code?.toUpperCase()===code && m.status==='ACTIVE');
      if(people.length!==1) { unresolved.push({groupId:group.id,groupNo:source.groupNo,mentorCode:code,reason:'Mentor code does not match one active account'}); continue; }
      if(group.mentorAccountId===people[0].id) continue;
      // Never replace an already assigned different mentor without clarification.
      if(group.mentorAccountId) {unresolved.push({groupId:group.id,mentorCode:code,reason:'Existing assignment differs'}); continue;}
      await once('reconcileMentor.'+group.id,()=>api('PATCH',`/api/groups/${group.id}/mentor`,{mentorId:people[0].id},admin)); corrected++;
    }
    state.mentorReconciliation={corrected,unresolved}; save(); console.log(JSON.stringify(state.mentorReconciliation)); return;
  }
  if (mode === 'backup') {
    if (!state.backupId) { const job = await api('POST', '/api/admin/backups', undefined, admin); state.backupId = job.id; save(); }
    for (let i = 0; i < 60; i++) {
      const job = await api('GET', `/api/admin/backups/${state.backupId}`, undefined, admin);
      if (job.status === 'SUCCEEDED') {
        const response = await fetch(`${BASE}/api/admin/backups/${job.id}/download`, { headers: { Authorization: `Bearer ${admin}` } });
        if (!response.ok) throw new Error('Backup download failed');
        const dest = path.join(OUT, 'before-group-import.dump');
        fs.writeFileSync(dest, Buffer.from(await response.arrayBuffer()));
        state.backup = { id: job.id, file: dest, bytes: fs.statSync(dest).size }; save();
        console.log(JSON.stringify({ backup: state.backup })); return;
      }
      if (job.status === 'FAILED') throw new Error('Backup job failed: ' + JSON.stringify(job));
      await delay(1000);
    }
    throw new Error('Backup still pending; rerun backup mode');
  }
  if (mode === 'import') {
    if (!state.backup?.bytes) throw new Error('Run backup first');
    if (!state.importId) {
      const terms = await api('GET', '/api/terms/available', undefined, admin);
      if (terms.some(t => t.code !== 'SU26')) throw new Error('Another open term exists; do not close automatically');
      const form = new FormData();
      form.append('file', new Blob([fs.readFileSync(FILE)]), path.basename(FILE));
      const batch = await api('POST', '/api/imports/students', form, admin);
      state.importId = batch.batchId; state.sourceSha256 = crypto.createHash('sha256').update(fs.readFileSync(FILE)).digest('hex'); save();
      console.log(JSON.stringify({ queued: state.importId }));
    }
    for (let i = 0; i < 900; i++) {
      const batch = await api('GET', `/api/imports/${state.importId}`, undefined, admin);
      if (!['QUEUED', 'RUNNING'].includes(batch.status)) {
        const errors = []; let page = 0;
        while (true) {
          const result = await api('GET', `/api/imports/${state.importId}/errors?page=${page}&size=100`, undefined, admin);
          errors.push(...result.content); if (!result.hasNext) break; page++;
        }
        state.import = batch; state.importErrors = errors; save();
        console.log(JSON.stringify({ batch, errorCounts: errors.reduce((a,e) => { a[e.errorCode]=(a[e.errorCode]||0)+1; return a; },{}) })); return;
      }
      if (i % 15 === 0) console.log(JSON.stringify({ batchId: state.importId, status: batch.status, elapsedSeconds: i*2 }));
      await delay(2000);
    }
    throw new Error('Import still running; rerun import mode to resume polling without upload');
  }
  throw new Error('Supported modes: backup, import, seed, reconcile-mentors, remove-import-collisions, verify');
}
main().catch(e => { console.error(e.message); process.exitCode = 1; });
