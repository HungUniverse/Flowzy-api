using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Service.Groups;

public sealed class GroupOperationsService(FlowzyDbContext db, IGroupTaskService tasks) : IGroupOperationsService
{
    private static DateTime Now=>DateTime.UtcNow;
    public async Task<IReadOnlyList<GroupSummaryResponse>> ListAsync(string? search,string? status,string? neededRole,string? category,CancellationToken ct)
    {var q=Groups();if(!string.IsNullOrWhiteSpace(search)){var s=search.Trim().ToLower();q=q.Where(x=>x.Name.ToLower().Contains(s)||(x.ProjectName!=null&&x.ProjectName.ToLower().Contains(s))||x.GroupNo.ToLower().Contains(s));}if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.Status==status.ToUpper());if(!string.IsNullOrWhiteSpace(neededRole))q=q.Where(x=>x.GroupRecruitmentNeeds.Any(n=>n.Role==neededRole.ToUpper()));return (await q.OrderByDescending(x=>x.CreatedAt).ToListAsync(ct)).Select(Summary).ToList();}
    public async Task<PageResponse<GroupSummaryResponse>> DiscoverAsync(int page,int size,string? name,decimal? gpa,string? role,string email,CancellationToken ct)
    {if(page<0)throw new BadRequestException("Page index must be zero or greater");if(size is <1 or >100)throw new BadRequestException("Page size must be between 1 and 100");if(gpa is <0 or >4)throw new BadRequestException("Student GPA must be between 0.00 and 4.00");await Student(email,ct);var q=Groups().Where(x=>x.Status=="ACTIVE"&&x.TermNavigation.Status=="OPEN");if(!string.IsNullOrWhiteSpace(name)){var n=name.ToLower();q=q.Where(x=>x.Name.ToLower().Contains(n));}if(gpa is not null)q=q.Where(x=>x.RequiredGpa==null||x.RequiredGpa<=gpa);if(!string.IsNullOrWhiteSpace(role))q=q.Where(x=>x.GroupRecruitmentNeeds.Any(n=>n.Role==role.ToUpper()));var total=await q.CountAsync(ct);var rows=await q.OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Skip(page*size).Take(size).ToListAsync(ct);return PageResponse<GroupSummaryResponse>.Create(rows.Select(Summary).ToList(),page,size,total);}
    public async Task<object> DetailAsync(long id,CancellationToken ct)=>Detail(await Group(id,ct));
    public async Task<object> CreateAsync(CreateGroupRequest r,string email,CancellationToken ct)
    {var s=await Student(email,ct);var term=r.Term.Trim().ToUpper();var course=r.CourseCode.Trim().ToUpper();var at=await db.AcademicTerms.FirstOrDefaultAsync(x=>x.Code==term,ct)??throw new BadRequestException("Academic term does not exist");if(at.Status!="OPEN")throw new BadRequestException("Academic term is closed");var name=Name(r.Name);if(await db.StudentGroups.AnyAsync(x=>x.Term==term&&x.Name.ToLower()==name.ToLower(),ct))throw new ConflictException("A group with this name already exists in the selected term");if(await db.StudentGroupMembers.AnyAsync(x=>x.StudentId==s.Id&&x.Group.Term==term&&x.Group.CourseCode==course,ct))throw new BadRequestException("Student is already a member of a group in the same term and course");ValidateScores(r.RequiredGpa,r.TargetGrade);var used=await db.StudentGroups.Where(x=>x.Term==term&&x.CourseCode==course).Select(x=>x.GroupNo).ToListAsync(ct);var no=1;while(used.Contains(no.ToString(System.Globalization.CultureInfo.InvariantCulture)))no++;var now=Now;var g=new StudentGroup{Term=term,CourseCode=course,GroupNo=no.ToString(System.Globalization.CultureInfo.InvariantCulture),Name=name,ProjectName=r.ProjectName,IdeaDescription=r.IdeaDescription,ResearchDomain=r.ResearchDomain,LeaderStudentId=s.Id,RequiredGpa=r.RequiredGpa,TargetGrade=r.TargetGrade,Status="ACTIVE",CreatedAt=now,UpdatedAt=now};db.StudentGroups.Add(g);await db.SaveChangesAsync(ct);db.StudentGroupMembers.Add(new StudentGroupMember{GroupId=g.Id,StudentId=s.Id,MemberRole="LEADER",JoinedAt=now});db.TaskBoards.Add(new TaskBoard{GroupId=g.Id,Name="Default",Description="Default Board",Position=0,DefaultBoard=true,CreatedByStudentId=s.Id,CreatedAt=now,UpdatedAt=now});ReplaceNeeds(g,r.RecruitmentNeeds,now);await db.SaveChangesAsync(ct);return Detail(await Group(g.Id,ct));}
    public async Task<object> UpdateAsync(long id,UpdateGroupRequest r,string email,CancellationToken ct){var g=await Group(id,ct);await LeaderOrAdmin(g,email,ct);Writable(g);ValidateScores(r.RequiredGpa,r.TargetGrade);if(r.Name is not null){var n=Name(r.Name);if(await db.StudentGroups.AnyAsync(x=>x.Id!=id&&x.Term==g.Term&&x.Name.ToLower()==n.ToLower(),ct))throw new ConflictException("A group with this name already exists in the selected term");g.Name=n;}if(r.ProjectName is not null)g.ProjectName=r.ProjectName;if(r.IdeaDescription is not null)g.IdeaDescription=r.IdeaDescription;if(r.ResearchDomain is not null)g.ResearchDomain=r.ResearchDomain;if(r.RequiredGpa is not null)g.RequiredGpa=r.RequiredGpa;if(r.TargetGrade is not null)g.TargetGrade=r.TargetGrade;if(r.RecruitmentNeeds is not null){db.GroupRecruitmentNeeds.RemoveRange(g.GroupRecruitmentNeeds);ReplaceNeeds(g,r.RecruitmentNeeds,Now);}g.UpdatedAt=Now;await db.SaveChangesAsync(ct);return Detail(await Group(id,ct));}
    public Task<object> CriteriaAsync(long id,UpdateGroupCriteriaRequest r,string email,CancellationToken ct)=>UpdateAsync(id,new(null,null,null,null,r.RequiredGpa,r.TargetGrade,r.RecruitmentNeeds),email,ct);
    public async Task<IReadOnlyList<GroupSummaryResponse>> MineAsync(string kind,string email,string? term,string? course,CancellationToken ct){IQueryable<StudentGroup> q=Groups();if(kind=="student"){var s=await Student(email,ct);q=q.Where(x=>x.StudentGroupMembers.Any(m=>m.StudentId==s.Id));}else if(kind=="mentor"){var a=await Account(email,ct);var m=await db.Mentors.FirstOrDefaultAsync(x=>x.AccountId==a.Id,ct)??throw new NotFoundException($"Mentor profile not found for email: {email}");q=q.Where(x=>x.MentorId==m.Id);}else{var a=await Account(email,ct);var i=await db.Instructors.FirstOrDefaultAsync(x=>x.AccountId==a.Id,ct)??throw new NotFoundException($"Instructor profile not found for email: {email}");q=q.Where(x=>x.InstructorId==i.Id);if(!string.IsNullOrWhiteSpace(term))q=q.Where(x=>x.Term==term);if(!string.IsNullOrWhiteSpace(course))q=q.Where(x=>x.CourseCode==course);}return (await q.OrderByDescending(x=>x.CreatedAt).ToListAsync(ct)).Select(Summary).ToList();}
    public async Task RemoveAsync(long gid, long sid, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var g = await LockMembershipGroup(gid, ct); Unlocked(g);
        var caller = await Account(email, ct);
        if (caller.Role != "ADMIN")
        {
            Writable(g);
            var student = await db.Students.FirstOrDefaultAsync(x => x.AccountId == caller.Id, ct);
            if (student is null || g.LeaderStudentId != student.Id) throw new ForbiddenException("Only group leader or admin can remove member");
        }
        var member = await db.StudentGroupMembers.Include(x => x.Student).FirstOrDefaultAsync(x => x.GroupId == gid && x.StudentId == sid, ct)
            ?? throw new BadRequestException("Student is not a member of this group");
        if (g.LeaderStudentId == sid) throw new BadRequestException("Leader cannot be removed. Transfer leadership first");
        await tasks.HandleMemberRemoval(gid, sid, ct);
        db.StudentGroupMembers.Remove(member); await db.SaveChangesAsync(ct); await Deactivate(g, ct);
        await MembershipNotification(member.Student.AccountId, g, sid, "Removed from Group", $"You have been removed from group {g.Name}.", false, ct);
        await tx.CommitAsync(ct);
    }

    public async Task LeaveAsync(long gid, string email, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var student = await Student(email, ct); var g = await LockMembershipGroup(gid, ct); Writable(g); Unlocked(g);
        var member = await db.StudentGroupMembers.FirstOrDefaultAsync(x => x.GroupId == gid && x.StudentId == student.Id, ct)
            ?? throw new BadRequestException("You are not a member of this group");
        var isLeader = g.LeaderStudentId == student.Id;
        if (isLeader && await db.StudentGroupMembers.CountAsync(x => x.GroupId == gid, ct) > 1)
            throw new BadRequestException("Group leader cannot leave the group. Transfer leadership first");
        await tasks.HandleMemberRemoval(gid, student.Id, ct);
        db.StudentGroupMembers.Remove(member); await db.SaveChangesAsync(ct); await Deactivate(g, ct);
        if (!isLeader && g.LeaderStudentId is not null)
        {
            var accountId = await db.Students.Where(x => x.Id == g.LeaderStudentId).Select(x => x.AccountId).SingleAsync(ct);
            await MembershipNotification(accountId, g, student.Id, "Member Left Group", $"{student.FullName} has left group {g.Name}.", true, ct);
        }
        await tx.CommitAsync(ct);
    }

    private async Task<StudentGroup> LockMembershipGroup(long id, CancellationToken ct)
    {
        var group = await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={id} FOR UPDATE").FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Group not found with id: {id}");
        await db.Entry(group).Reference(x => x.TermNavigation).LoadAsync(ct); return group;
    }

    private async Task MembershipNotification(long recipient, StudentGroup group, long studentId, string title, string body, bool leaving, CancellationToken ct)
    {
        var key = $"GROUP_MEMBER_REMOVED:{group.Id}:{studentId}";
        if (!await db.Accounts.AnyAsync(x => x.Id == recipient && x.Status == "ACTIVE", ct) ||
            await db.Notifications.AnyAsync(x => x.RecipientId == recipient && x.EventKey == key, ct)) return;
        var parameters = new Dictionary<string, string> { ["groupId"] = group.Id.ToString() };
        if (leaving) parameters["studentId"] = studentId.ToString();
        db.Notifications.Add(new Notification { RecipientId = recipient, Type = "GROUP_MEMBER_REMOVED", Title = title, Body = body,
            ActionKey = "OPEN_GROUP", ActionParams = System.Text.Json.JsonSerializer.Serialize(parameters), EntityType = "StudentGroup",
            EntityId = group.Id.ToString(), EventKey = key, CreatedAt = Now, UpdatedAt = Now });
        await db.SaveChangesAsync(ct);
    }
    public async Task TransferAsync(long gid,long sid,string email,CancellationToken ct){var g=await Group(gid,ct);await LeaderOrAdmin(g,email,ct);Writable(g);var target=await db.StudentGroupMembers.FirstOrDefaultAsync(x=>x.GroupId==gid&&x.StudentId==sid,ct)??throw new BadRequestException("New leader must be a member of the group");if(g.LeaderStudentId==sid)return;var old=await db.StudentGroupMembers.FirstOrDefaultAsync(x=>x.GroupId==gid&&x.StudentId==g.LeaderStudentId,ct);if(old is not null)old.MemberRole="MEMBER";target.MemberRole="LEADER";g.LeaderStudentId=sid;g.UpdatedAt=Now;await db.SaveChangesAsync(ct);}
    public async Task<object> AssignAsync(long gid,string kind,long? accountId,CancellationToken ct){var g=await Group(gid,ct);if(accountId is not null){if(g.Status!="ACTIVE"||g.StudentGroupMembers.Count==0)throw new ConflictException("Inactive groups or groups without members cannot receive assignments");var a=await db.Accounts.FindAsync([accountId.Value],ct)??throw new NotFoundException($"{kind} account not found with id: {accountId}");if(a.Role!=kind.ToUpper())throw new BadRequestException($"Account is not an {kind}: {accountId}");if(kind=="instructor")g.InstructorId=(await db.Instructors.FirstOrDefaultAsync(x=>x.AccountId==a.Id,ct)??throw new NotFoundException("Instructor profile not found")).Id;else g.MentorId=(await db.Mentors.FirstOrDefaultAsync(x=>x.AccountId==a.Id,ct)??throw new NotFoundException("Mentor profile not found")).Id;}else if(kind=="instructor")g.InstructorId=null;else g.MentorId=null;g.UpdatedAt=Now;await db.SaveChangesAsync(ct);return Detail(await Group(gid,ct));}
    public async Task<object> LockAsync(long gid,bool value,string email,CancellationToken ct){var g=await Group(gid,ct);var s=await Student(email,ct);Writable(g);if(g.LeaderStudentId!=s.Id)throw new ForbiddenException("Only the group leader can lock or unlock the group");g.IsLocked=value;g.UpdatedAt=Now;await db.SaveChangesAsync(ct);return Detail(await Group(gid,ct));}

    private IQueryable<StudentGroup> Groups()=>db.StudentGroups.AsNoTracking().Include(x=>x.TermNavigation).Include(x=>x.LeaderStudent).ThenInclude(x=>x!.Account).Include(x=>x.Mentor).ThenInclude(x=>x!.Account).Include(x=>x.Instructor).ThenInclude(x=>x!.Account).Include(x=>x.SelectedProblem).Include(x=>x.StudentGroupMembers).ThenInclude(x=>x.Student).ThenInclude(x=>x.Account).Include(x=>x.GroupRecruitmentNeeds);
    private async Task<StudentGroup> Group(long id,CancellationToken ct)=>await Groups().AsTracking().FirstOrDefaultAsync(x=>x.Id==id,ct)??throw new NotFoundException($"Group not found with id: {id}");
    private async Task<Account> Account(string email,CancellationToken ct)=>await db.Accounts.FirstOrDefaultAsync(x=>x.Email.ToLower()==email.ToLower(),ct)??throw new UnauthorizedException("User account not found");
    private async Task<Student> Student(string email,CancellationToken ct){var a=await Account(email,ct);if(a.Role!="STUDENT")throw new ForbiddenException("Student access required");return await db.Students.FirstOrDefaultAsync(x=>x.AccountId==a.Id,ct)??throw new NotFoundException($"Student profile not found for email: {email}");}
    private async Task LeaderOrAdmin(StudentGroup g,string email,CancellationToken ct){var a=await Account(email,ct);if(a.Role=="ADMIN")return;var s=await Student(email,ct);if(g.LeaderStudentId!=s.Id)throw new ForbiddenException("Only group leader or admin can update group");}
    private static void Writable(StudentGroup g) => StudentTermWriteGuard.RequireWritable(g);
    private static void Unlocked(StudentGroup g){if(g.IsLocked)throw new ConflictException("Group membership is locked");}
    private async Task Deactivate(StudentGroup g,CancellationToken ct){if(!await db.StudentGroupMembers.AnyAsync(x=>x.GroupId==g.Id,ct)){g.LeaderStudentId=null;g.Status="INACTIVE";g.IsLocked=false;g.UpdatedAt=Now;await db.SaveChangesAsync(ct);}}
    private static string Name(string n)=>string.IsNullOrWhiteSpace(n)?throw new BadRequestException("Group name is required"):string.Join(' ',n.Trim().Split(' ',StringSplitOptions.RemoveEmptyEntries));private static void ValidateScores(decimal? g,decimal? t){if(g is <0 or >4)throw new BadRequestException("Required GPA must be between 0.00 and 4.00");if(t is <0 or >10)throw new BadRequestException("Target grade must be between 0.0 and 10.0");}
    private void ReplaceNeeds(StudentGroup g,IReadOnlyList<RecruitmentNeedRequest>? needs,DateTime now){if(needs is null)return;foreach(var n in needs){if(n.Quantity<1)throw new BadRequestException("Recruitment quantity must be greater than zero");var role=n.Role.Trim().ToUpper();if(g.GroupRecruitmentNeeds.Any(x=>x.Role==role))throw new BadRequestException("Duplicate recruitment role: "+role);g.GroupRecruitmentNeeds.Add(new GroupRecruitmentNeed{Role=role,Quantity=n.Quantity,CreatedAt=now,UpdatedAt=now});}}
    private static GroupSummaryResponse Summary(StudentGroup g)=>new(g.Id,g.Term,g.TermNavigation.Status,g.TermNavigation.ClosedAt,g.TermNavigation.Status=="CLOSED",g.CourseCode,g.GroupNo,g.Name,g.ProjectName,g.LeaderStudent?.FullName,g.StudentGroupMembers.Count,g.RequiredGpa,g.TargetGrade,g.Status,g.MentorId,g.Mentor?.AccountId,g.Mentor?.MentorCode,g.Mentor?.FullName,g.InstructorId,g.Instructor?.AccountId,g.Instructor?.InstructorCode,g.Instructor?.FullName,g.IsLocked,g.SelectedProblem is null?null:new(g.SelectedProblem.Id,g.SelectedProblem.Code,g.SelectedProblem.Title,g.SelectedProblem.SourceType,g.SelectedProblem.Status),g.GroupRecruitmentNeeds.Select(x=>new GroupRecruitmentNeedResponse(x.Role,null,null,null,x.Quantity)).ToList());
    private static object Detail(StudentGroup g)=>new{id=g.Id,term=g.Term,termStatus=g.TermNavigation.Status,termClosedAt=g.TermNavigation.ClosedAt,studentReadOnly=g.TermNavigation.Status=="CLOSED",courseCode=g.CourseCode,groupNo=g.GroupNo,name=g.Name,projectName=g.ProjectName,ideaDescription=g.IdeaDescription,researchDomain=g.ResearchDomain,leader=g.LeaderStudent is null?null:new{g.LeaderStudent.Id,g.LeaderStudent.StudentCode,g.LeaderStudent.FullName,email=g.LeaderStudent.Account.Email,g.LeaderStudent.Phone,g.LeaderStudent.DateOfBirth,g.LeaderStudent.Gender,g.LeaderStudent.Address,g.LeaderStudent.Major,g.LeaderStudent.Cohort,g.LeaderStudent.ClassName,g.LeaderStudent.Status},requiredGpa=g.RequiredGpa,targetGrade=g.TargetGrade,status=g.Status,mentor=g.Mentor is null?null:new{g.Mentor.Id,g.Mentor.MentorCode,g.Mentor.FullName,email=g.Mentor.Account.Email,g.Mentor.Phone,g.Mentor.JobTitle,g.Mentor.Company,g.Mentor.Expertise,g.Mentor.YearsOfExperience,g.Mentor.LinkedinUrl,g.Mentor.Status},mentorAccountId=g.Mentor?.AccountId,instructorId=g.InstructorId,instructorAccountId=g.Instructor?.AccountId,instructorCode=g.Instructor?.InstructorCode,instructorName=g.Instructor?.FullName,isLock=g.IsLocked,members=g.StudentGroupMembers.Select(x=>new{studentId=x.StudentId,x.Student.StudentCode,x.Student.FullName,email=x.Student.Account.Email,role=x.MemberRole}),selectedProblem=g.SelectedProblem is null?null:new{g.SelectedProblem.Id,g.SelectedProblem.Code,g.SelectedProblem.Title,g.SelectedProblem.SourceType,g.SelectedProblem.Status},recruitmentNeeds=g.GroupRecruitmentNeeds.Select(x=>new{x.Role,category=(string?)null,displayNameVi=(string?)null,displayNameEn=(string?)null,x.Quantity})};
}
