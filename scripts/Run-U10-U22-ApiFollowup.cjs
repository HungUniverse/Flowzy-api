// Additional non-destructive U10-U22 API checks on the local UAT fixtures.
const fs=require('node:fs');
const s=JSON.parse(fs.readFileSync('.tools/local-demo-20260928/state.json','utf8'));
const cfg=JSON.parse(fs.readFileSync('src/Flowzy.Api/appsettings.Development.json','utf8'));
const g1=s.steps['group.g1'].id,g2=s.steps['group.g2'].id;
const tokens={},results=[];
async function call(method,path,body,actor){
 const headers=actor?{Authorization:'Bearer '+tokens[actor]}:{};
 if(body!==undefined)headers['Content-Type']='application/json';
 const r=await fetch('http://localhost:8080'+path,{method,headers,body:body===undefined?undefined:JSON.stringify(body)});
 let j;try{j=await r.json()}catch{j={}}return {status:r.status,data:j.data,message:j.message};
}
const ok=(r,label)=>{if(r.status<200||r.status>=300)throw Error(`${label}: HTTP ${r.status} ${r.message||''}`);return r.data};
const bad=(r,label)=>{if(r.status<400||r.status>=500)throw Error(`${label}: expected 4xx, got ${r.status}`)};
const need=(v,label)=>{if(!v)throw Error(label)};
async function test(id,fn){try{const note=await fn();results.push({id,status:'PASS',note:note||''});console.log('PASS',id,note||'')}catch(e){results.push({id,status:'FAIL',note:e.message});console.log('FAIL',id,e.message)}}
async function main(){
 for(const key of ['s1','s2','s3','m1','i1']){const a=s.steps['account.'+key];tokens[key]=ok(await call('POST','/api/auth/login',{email:a.email,password:s.password}),'Login '+key).accessToken}
 tokens.admin=ok(await call('POST','/api/auth/login',{email:cfg.Admin.Email,password:cfg.Admin.Password}),'Admin login').accessToken;
 if(process.argv.includes('--extra')||process.argv.includes('--race')){await extra();const summary={counts:Object.fromEntries(['PASS','FAIL'].map(status=>[status,results.filter(x=>x.status===status).length])),results};console.log('SUMMARY '+JSON.stringify(summary));if(summary.counts.FAIL)process.exitCode=1;return}
 await test('U10-N2-reviewed-immutable',async()=>{
   const approved=8;
   bad(await call('PUT',`/api/groups/${g1}/problems/proposals/${approved}`,{title:'Should not save',statement:'Should not save',difficultyLevel:'BEGINNER',domainCode:'UATD0928A'},'s1'),'Edit approved proposal');
   bad(await call('DELETE',`/api/groups/${g1}/problems/proposals/${approved}`,undefined,'s1'),'Delete approved proposal');
   const p=ok(await call('GET',`/api/problems/${approved}`,undefined,'s1'),'Read approved');need(p.status==='ACTIVE','Approved problem changed');
 });
 await test('U11-N1-validation',async()=>{
   bad(await call('POST',`/api/groups/${g1}/tasks`,{title:'',status:'TODO',boardId:s.steps['board.g1'].id},'s1'),'Blank task title');
   bad(await call('POST',`/api/groups/${g1}/tasks`,{title:'UAT Invalid Done',status:'DONE',boardId:s.steps['board.g1'].id},'s1'),'Create directly Done');
 });
 await test('U18-N5-optimistic-task-version',async()=>{
   const tid=11,path=`/api/groups/${g1}/tasks/${tid}`;
   const before=ok(await call('GET',path,undefined,'s1'),'Read task');
   const newer=ok(await call('PATCH',path,{description:'UAT API optimistic version A',version:before.version},'s1'),'Save version A');
   bad(await call('PATCH',path,{description:'UAT API stale version B',version:before.version},'s2'),'Save stale version B');
   const after=ok(await call('GET',path,undefined,'s2'),'Reload after conflict');
   need(after.description==='UAT API optimistic version A'&&after.version===newer.version,'Stale write overwrote new version');
 });
 await test('U12-N1-invalid-link',async()=>{
   const d=new Date(Date.now()+8*86400000);d.setUTCHours(3,0,0,0);
   bad(await call('POST','/api/mentor/availability',{startAt:d.toISOString(),endAt:new Date(d.getTime()+3600000).toISOString(),meetLink:'https://example.com/not-a-meet'},'m1'),'Invalid Meet link');
 });
 await test('U12-H4-cancel-own-slot',async()=>{
   const d=new Date(Date.now()+9*86400000);d.setUTCHours(3,0,0,0);
   const slot=ok(await call('POST','/api/mentor/availability',{startAt:d.toISOString(),endAt:new Date(d.getTime()+3600000).toISOString(),meetLink:'https://meet.google.com/abc-defg-hij',note:'UAT cancel-only slot'},'m1'),'Create cancel slot');
   ok(await call('DELETE',`/api/mentor/availability/${slot.id}`,undefined,'m1'),'Cancel slot');
   const all=ok(await call('GET','/api/mentor/availability',undefined,'m1'),'Read slots');
   need(all.some(x=>x.id===slot.id&&x.status==='CANCELED'),'Slot not marked CANCELED');return `slot=${slot.id}`;
 });
 await test('U13-H1/H2/H3-direct-meeting',async()=>{
   const d=new Date(Date.now()+10*86400000);d.setUTCHours(4,0,0,0);
   let meeting=ok(await call('POST',`/api/groups/${g2}/mentor/meetings`,{startAt:d.toISOString(),endAt:new Date(d.getTime()+3600000).toISOString(),meetLink:'https://meet.google.com/abc-defg-hij',note:'UAT API direct meeting'},'m1'),'Create direct meeting');
   meeting=ok(await call('PATCH',`/api/groups/${g2}/mentor/meetings/${meeting.id}`,{startAt:d.toISOString(),endAt:new Date(d.getTime()+3600000).toISOString(),meetLink:'https://meet.google.com/abc-defg-hij',note:'UAT API direct updated'},'m1'),'Edit meeting');
   need(meeting.note==='UAT API direct updated','Meeting note not saved');
   meeting=ok(await call('PATCH',`/api/groups/${g2}/mentor/meetings/${meeting.id}/cancel`,{reason:'UAT API cancel after edit'},'m1'),'Cancel meeting');
   need(meeting.status==='CANCELED','Meeting not canceled');return `meeting=${meeting.id}`;
 });
 await test('U14-N2-validation',async()=>{
   bad(await call('POST','/api/course-milestones',{term:'SU26',courseCode:'EXE101',title:'',weight:5,maxScore:10,position:100},'i1'),'Blank milestone title');
   bad(await call('POST','/api/course-milestones',{term:'SU26',courseCode:'EXE101',title:'UAT invalid max',weight:5,maxScore:0,position:100},'i1'),'Zero max score');
 });
 await test('U15-N3-grade-range',async()=>{
   bad(await call('PUT',`/api/instructor/milestones/${s.steps.ms2.id}/groups/${g1}/grade`,{score:11,feedback:'Invalid'},'i1'),'Over-max score');
   const matrix=ok(await call('GET',`/api/groups/${g1}/grades`,undefined,'s1'),'Read matrix');
   need(matrix.milestones.find(x=>x.milestoneId===s.steps.ms2.id)?.groupGrade?.score===9,'Valid grade changed');
 });
 await test('U16-N1-notification-access',async()=>{
   const n=ok(await call('GET','/api/notifications?size=50',undefined,'s2'),'S2 list').content[0];need(n,'No S2 notification');
   bad(await call('PATCH',`/api/notifications/${n.id}/read`,undefined,'s3'),'S3 read S2 notification');
 });
 await test('U17-all-dashboard-routes',async()=>{
   for(const route of ['/api/dashboard/admin/groups','/api/dashboard/admin/projects','/api/dashboard/admin/mentors','/api/dashboard/admin/timeline','/api/dashboard/admin/execution-status','/api/dashboard/tv-showcase/recruitments?term=SU26&courseCode=EXE101'])ok(await call('GET',route,undefined,'admin'),'Admin/TV '+route);
   for(const route of ['/api/dashboard/student/groups','/api/dashboard/student/projects',`/api/dashboard/student/milestones?groupId=${g1}`])ok(await call('GET',route,undefined,'s1'),'Student '+route);
   ok(await call('GET','/api/dashboard/mentor/meetings',undefined,'m1'),'Mentor meetings dashboard');
 });
 await test('U19-open-term-feedback-gate',async()=>{
   const list=ok(await call('GET','/api/feedback/me?term=SU26',undefined,'s1'),'Feedback list');need(list.length>0,'No feedback fixture');
   const pending=list.find(x=>x.status==='PENDING');need(pending,'No PENDING feedback');
   bad(await call('PUT',`/api/feedback/${pending.id}`,{rating:5,comment:'UAT should be blocked before close'},'s1'),'Submit while OPEN');
   const after=ok(await call('GET','/api/feedback/me?term=SU26',undefined,'s1'),'Feedback after');need(after.find(x=>x.id===pending.id)?.status==='PENDING','Feedback changed while term OPEN');
 });
 await test('U21-N1-schedule-validation',async()=>{
   const before=ok(await call('GET','/api/admin/backups/schedule',undefined,'admin'),'Schedule before');
   bad(await call('PUT','/api/admin/backups/schedule',{enabled:false,cronExpression:before.cronExpression,timezone:before.timezone,retentionDays:0},'admin'),'Retention zero');
   const after=ok(await call('GET','/api/admin/backups/schedule',undefined,'admin'),'Schedule after');need(after.retentionDays===before.retentionDays&&after.enabled===before.enabled,'Invalid schedule overwrote valid settings');
 });
 const summary={counts:Object.fromEntries(['PASS','FAIL'].map(status=>[status,results.filter(x=>x.status===status).length])),results};console.log('SUMMARY '+JSON.stringify(summary));if(summary.counts.FAIL)process.exitCode=1;
}
async function extra(){
 if(!process.argv.includes('--race')){
 await test('U10-H5-pending-delete',async()=>{
   const original=s.steps.pendingProposal.id;
   const p=ok(await call('POST',`/api/groups/${g2}/problems/propose`,{title:'UAT API Delete Pending',statement:'Proposal chỉ dùng để kiểm tra xóa.',difficultyLevel:'BEGINNER',domainCode:'UATD0928A'},'s3'),'Create pending');
   need(p.status==='PENDING_REVIEW','New proposal not pending');
   ok(await call('DELETE',`/api/groups/${g2}/problems/proposals/${p.id}`,undefined,'s3'),'Delete pending');
   const list=ok(await call('GET',`/api/groups/${g2}/problems/proposals`,undefined,'s3'),'List proposals');
   need(!list.some(x=>x.id===p.id),'Deleted pending still listed');
   ok(await call('POST',`/api/groups/${g2}/problems/select`,{problemId:original},'s3'),'Restore selected problem');
   return `deleted=${p.id}`;
 });
 await test('U11-H1-board-create',async()=>{
   const board=ok(await call('POST',`/api/groups/${g1}/boards`,{name:'UAT API Extra Board',description:'API create/read UAT'},'s1'),'Create board');
   const list=ok(await call('GET',`/api/groups/${g1}/boards`,undefined,'s1'),'List boards');
   need(list.some(x=>x.id===board.id),'Created board missing');
   ok(await call('DELETE',`/api/groups/${g1}/boards/${board.id}`,undefined,'s1'),'Delete empty extra board');
   return `board=${board.id}`;
 });
 await test('U14-H1/H3-extra-milestone',async()=>{
   const milestone=ok(await call('POST','/api/course-milestones',{term:'SU26',courseCode:'EXE101',title:'UAT API Extra Milestone',description:'Supplemental UAT only',weight:0,maxScore:10,position:99,deadlineAt:new Date(Date.now()+30*86400000).toISOString()},'i1'),'Create milestone');
   const updated=ok(await call('PATCH',`/api/course-milestones/${milestone.id}`,{title:'UAT API Extra Milestone',description:'UAT edited description',weight:0,maxScore:10,position:99},'i1'),'Edit milestone');
   need(updated.description==='UAT edited description','Milestone edit not saved');
   ok(await call('DELETE',`/api/course-milestones/${milestone.id}`,undefined,'i1'),'Archive extra milestone');
   return `milestone=${milestone.id}`;
 });
 await test('U21-H3-schedule-save-restore',async()=>{
   const before=ok(await call('GET','/api/admin/backups/schedule',undefined,'admin'),'Schedule before');
   const edited=ok(await call('PUT','/api/admin/backups/schedule',{enabled:false,cronExpression:before.cronExpression,timezone:before.timezone,retentionDays:before.retentionDays+1},'admin'),'Save schedule');
   need(edited.retentionDays===before.retentionDays+1,'Edited retention not persisted');
   const restored=ok(await call('PUT','/api/admin/backups/schedule',{enabled:before.enabled,cronExpression:before.cronExpression,timezone:before.timezone,retentionDays:before.retentionDays},'admin'),'Restore schedule');
   need(restored.retentionDays===before.retentionDays&&restored.enabled===before.enabled,'Original schedule not restored');
 });
 }
 await test('U18-N3-concurrent-instructor-claim',async()=>{
   const i2=s.steps['account.i2'];const profile=i2.instructorProfile;
   const body=status=>({email:i2.email,status,mustChangePassword:false,instructorProfile:{instructorCode:profile.instructorCode,fullName:profile.fullName,phone:profile.phone,department:profile.department,expertise:profile.expertise}});
   let assigned=false, activated=false, originalInstructorAccountId=null;
   try{
     const before=ok(await call('GET',`/api/groups/${s.steps['group.g3'].id}`,undefined,'admin'),'G3 before');
     originalInstructorAccountId=before.instructorAccountId;
     if(before.instructorId)ok(await call('DELETE',`/api/groups/${s.steps['group.g3'].id}/instructor`,undefined,'admin'),'Temporarily unassign G3');
     ok(await call('PATCH',`/api/admin/users/${i2.id}`,body('ACTIVE'),'admin'),'Activate I2 temporarily');activated=true;
     tokens.i2=ok(await call('POST','/api/auth/login',{email:i2.email,password:s.password}),'I2 login').accessToken;
     const [a,b]=await Promise.all([call('POST',`/api/instructor/groups/${s.steps['group.g3'].id}/claim`,undefined,'i1'),call('POST',`/api/instructor/groups/${s.steps['group.g3'].id}/claim`,undefined,'i2')]);
     assigned=a.status<300||b.status<300;
     need([a,b].filter(x=>x.status<300).length===1 && [a,b].filter(x=>x.status>=400&&x.status<500).length===1,`Expected one winner: ${a.status}/${b.status}`);
     const after=ok(await call('GET',`/api/groups/${s.steps['group.g3'].id}`,undefined,'admin'),'G3 after');
     need(Boolean(after.instructorId),'No instructor assigned after race');
     return `claim=${a.status}/${b.status}`;
   }finally{
     if(assigned)ok(await call('DELETE',`/api/groups/${s.steps['group.g3'].id}/instructor`,undefined,'admin'),'Restore G3 unassigned');
     if(originalInstructorAccountId)ok(await call('PATCH',`/api/groups/${s.steps['group.g3'].id}/instructor`,{instructorId:originalInstructorAccountId},'admin'),'Restore G3 original instructor');
     if(activated)ok(await call('PATCH',`/api/admin/users/${i2.id}`,body('INACTIVE'),'admin'),'Restore I2 inactive');
   }
 });
}
main().catch(e=>{console.error('FATAL '+e.message);process.exitCode=2});
