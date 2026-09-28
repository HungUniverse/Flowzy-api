using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Groups;

public sealed class AdminGroupService(IAdminGroupRepository groups) : IAdminGroupService
{
    private static readonly IReadOnlyDictionary<string, RecruitmentRoleResponse> Roles = new RecruitmentRoleResponse[]
    {
        new("SOFTWARE_DEVELOPER", "TECHNOLOGY", "Lập trình phần mềm", "Software Developer"),
        new("WEB_MOBILE_DEVELOPER", "TECHNOLOGY", "Lập trình Web/Mobile", "Web/Mobile Developer"),
        new("AI_ML_ENGINEER", "TECHNOLOGY", "Kỹ sư AI/Machine Learning", "AI/ML Engineer"),
        new("DATA_ANALYST", "TECHNOLOGY", "Phân tích dữ liệu", "Data Analyst"),
        new("CYBERSECURITY_SPECIALIST", "TECHNOLOGY", "An toàn thông tin", "Cybersecurity Specialist"),
        new("CLOUD_DEVOPS_ENGINEER", "TECHNOLOGY", "Cloud/DevOps", "Cloud/DevOps Engineer"),
        new("SYSTEM_BUSINESS_ANALYST", "TECHNOLOGY", "Phân tích hệ thống/nghiệp vụ", "System/Business Analyst"),
        new("ROBOTICS_IOT_ENGINEER", "TECHNOLOGY", "Robot/IoT", "Robotics/IoT Engineer"),
        new("EMBEDDED_SEMICONDUCTOR_ENGINEER", "TECHNOLOGY", "Hệ thống nhúng/vi mạch", "Embedded/Semiconductor Engineer"),
        new("AUTOMOTIVE_TECH_ENGINEER", "TECHNOLOGY", "Công nghệ ô tô số", "Automotive Tech Engineer"),
        new("UI_UX_DESIGNER", "DESIGN", "Thiết kế UI/UX", "UI/UX Designer"),
        new("GRAPHIC_DESIGNER", "DESIGN", "Thiết kế đồ họa", "Graphic Designer"),
        new("MULTIMEDIA_DESIGNER", "DESIGN", "Thiết kế đa phương tiện", "Multimedia Designer"),
        new("PRODUCT_MANAGER", "BUSINESS", "Quản lý sản phẩm", "Product Manager"),
        new("BUSINESS_DEVELOPMENT", "BUSINESS", "Phát triển kinh doanh", "Business Development"),
        new("MARKETING_SPECIALIST", "BUSINESS", "Marketing", "Marketing Specialist"),
        new("E_COMMERCE_SPECIALIST", "BUSINESS", "Thương mại điện tử", "E-Commerce Specialist"),
        new("FINANCE_FINTECH_SPECIALIST", "BUSINESS", "Tài chính/Fintech", "Finance/Fintech Specialist"),
        new("LOGISTICS_SUPPLY_CHAIN_SPECIALIST", "BUSINESS", "Logistics/Chuỗi cung ứng", "Logistics/Supply Chain Specialist"),
        new("CUSTOMER_EXPERIENCE_SPECIALIST", "BUSINESS", "Trải nghiệm khách hàng", "Customer Experience Specialist"),
        new("CONTENT_CREATOR", "COMMUNICATION", "Sáng tạo nội dung", "Content Creator"),
        new("PUBLIC_RELATIONS_SPECIALIST", "COMMUNICATION", "Quan hệ công chúng", "Public Relations Specialist"),
        new("BRAND_COMMUNICATION_SPECIALIST", "COMMUNICATION", "Truyền thông thương hiệu", "Brand Communication Specialist"),
        new("EVENT_MANAGER", "COMMUNICATION", "Tổ chức sự kiện", "Event Manager"),
        new("TRANSLATOR_LOCALIZATION_SPECIALIST", "LANGUAGE_LEGAL", "Biên phiên dịch/Bản địa hóa", "Translator/Localization Specialist"),
        new("LEGAL_COMPLIANCE_SPECIALIST", "LANGUAGE_LEGAL", "Pháp lý/Tuân thủ", "Legal/Compliance Specialist")
    }.ToDictionary(x => x.Code);

    public async Task<PageResponse<GroupSummaryResponse>> SearchAsync(int page, int size, string? search,
        string? status, CancellationToken cancellationToken = default)
    {
        if (page < 0) throw new BadRequestException("Page index must be zero or greater");
        if (size is < 1 or > 100) throw new BadRequestException("Page size must be between 1 and 100");
        var statusFilter = ParseStatus(status);
        var result = await groups.SearchAsync(page, size, search, statusFilter, cancellationToken);
        return PageResponse<GroupSummaryResponse>.Create(result.Items.Select(Map).ToList(), page, size, result.Total);
    }

    private static string? ParseStatus(string? status)
    {
        var normalized = (status ?? "ACTIVE").Trim().ToUpperInvariant();
        if (normalized == "ALL") return null;
        if (normalized is not ("ACTIVE" or "INACTIVE"))
            throw new BadRequestException("Status must be ACTIVE, INACTIVE, or ALL");
        return normalized;
    }

    private static GroupSummaryResponse Map(StudentGroup group)
    {
        var needs = group.GroupRecruitmentNeeds.Select(need =>
        {
            Roles.TryGetValue(need.Role, out var role);
            return new GroupRecruitmentNeedResponse(need.Role, role?.Category, role?.DisplayNameVi,
                role?.DisplayNameEn, need.Quantity);
        }).ToList();
        var selected = group.SelectedProblem is null ? null : new SelectedProblemSummaryResponse(
            group.SelectedProblem.Id, group.SelectedProblem.Code, group.SelectedProblem.Title,
            group.SelectedProblem.SourceType, group.SelectedProblem.Status);
        var termStatus = group.TermNavigation?.Status ?? "OPEN";
        return new GroupSummaryResponse(group.Id, group.Term, termStatus, group.TermNavigation?.ClosedAt,
            termStatus == "CLOSED", group.CourseCode, group.GroupNo, group.Name, group.ProjectName,
            group.LeaderStudent?.FullName, group.StudentGroupMembers.Count, group.RequiredGpa, group.TargetGrade,
            group.Status, group.MentorId, group.Mentor?.AccountId, group.Mentor?.MentorCode,
            group.Mentor?.FullName, group.InstructorId, group.Instructor?.AccountId,
            group.Instructor?.InstructorCode, group.Instructor?.FullName, group.IsLocked, selected, needs);
    }
}
