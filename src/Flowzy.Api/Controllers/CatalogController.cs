using Flowzy.Service.Contracts;
using Flowzy.Service.Platform;
using Flowzy.Service.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flowzy.Api.Controllers;

[ApiController, Authorize]
public sealed class CatalogController(IPlatformService service) : ControllerBase
{
    [HttpGet("api/terms/available")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AcademicTermResponse>>>> Terms(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<AcademicTermResponse>>.Success(await service.GetAvailableTermsAsync(ct), "Available academic terms retrieved successfully"));

    [HttpGet("api/problem-domains")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProblemDomainResponse>>>> Domains([FromQuery] string? search, [FromQuery] string? status, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<ProblemDomainResponse>>.Success(await service.GetProblemDomainsAsync(search, QueryEnum(status, "status", ProblemStatuses), ct), "Problem domains retrieved successfully"));

    [HttpGet("api/problem-evaluation-criteria")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProblemCriterionResponse>>>> Criteria(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<ProblemCriterionResponse>>.Success(await service.GetActiveCriteriaAsync(ct), "Active evaluation criteria retrieved successfully"));

    [HttpGet("api/problems")]
    public async Task<ActionResult<ApiResponse<PageResponse<ProblemSummaryResponse>>>> Problems([FromQuery] int page = 0,
        [FromQuery] int size = 10, [FromQuery] string? search = null, [FromQuery] string? domainCode = null,
        [FromQuery] string? difficulty = null, [FromQuery] string? expectedOutput = null, [FromQuery] string? sourceType = null,
        [FromQuery] string? status = null, CancellationToken ct = default) =>
        Ok(ApiResponse<PageResponse<ProblemSummaryResponse>>.Success(
            await service.SearchProblemsAsync(page, size, search, domainCode,
                QueryEnum(difficulty, "difficulty", ["BEGINNER", "INTERMEDIATE", "ADVANCED"]), expectedOutput,
                QueryEnum(sourceType, "sourceType", ["OFFICIAL", "SELF_PROPOSED"]), QueryEnum(status, "status", ProblemStatuses), ct),
            "Problems retrieved successfully"));

    [HttpGet("api/problems/{id:long}")]
    public async Task<ActionResult<ApiResponse<ProblemDetailResponse>>> Problem(long id, CancellationToken ct) =>
        Ok(ApiResponse<ProblemDetailResponse>.Success(await service.GetProblemAsync(id, ct), "Problem details retrieved successfully"));

    [HttpGet("api/group-recruitment-roles")]
    public ActionResult<ApiResponse<IReadOnlyList<RecruitmentRoleResponse>>> RecruitmentRoles() =>
        Ok(ApiResponse<IReadOnlyList<RecruitmentRoleResponse>>.Success(RecruitmentRolesData, "Recruitment roles retrieved successfully"));

    private static readonly string[] ProblemStatuses = ["ACTIVE", "INACTIVE", "PENDING_REVIEW", "APPROVED", "REJECTED", "ARCHIVED"];

    private static string? QueryEnum(string? value, string parameter, string[] allowed)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var normalized = value.Trim();
        if (!allowed.Contains(normalized, StringComparer.Ordinal)) throw new BadRequestException("Invalid parameter format: " + parameter);
        return normalized;
    }

    private static readonly RecruitmentRoleResponse[] RecruitmentRolesData =
    [
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
    ];
}
