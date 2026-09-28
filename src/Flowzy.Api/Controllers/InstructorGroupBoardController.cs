using Flowzy.Service.Contracts;using Flowzy.Service.Groups;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace Flowzy.Api.Controllers;
[ApiController,Authorize(Roles="INSTRUCTOR"),Route("api/instructor/groups")]
public sealed class InstructorGroupBoardController(IInstructorBoardService service):ControllerBase{private string Email=>User.Identity!.Name!;
[HttpGet("board")]public async Task<ActionResult<ApiResponse<InstructorBoardResponse>>>Get([FromQuery]int page=0,[FromQuery]int size=12,[FromQuery]string? term=null,[FromQuery]string? courseCode=null,[FromQuery]string? search=null,[FromQuery]string assignment="ALL",CancellationToken ct=default)=>Ok(ApiResponse<InstructorBoardResponse>.Success(await service.Get(Email,page,size,term,courseCode,search,assignment,ct),"Instructor group board retrieved successfully"));
[HttpPost("{groupId:long}/claim")]public async Task<ActionResult<ApiResponse<InstructorBoardItem>>>Claim(long groupId,CancellationToken ct)=>Ok(ApiResponse<InstructorBoardItem>.Success(await service.Claim(groupId,Email,ct),"Group claimed successfully"));}
