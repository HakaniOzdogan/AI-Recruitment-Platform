using IkOtomasyon.Api.Authorization;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Middleware;
using IkOtomasyon.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IkOtomasyon.Api.Controllers;

[ApiController]
[Authorize]
[Route("cv")]
public class CvController : ControllerBase
{
    private readonly CvWorkflowService _cvWorkflow;

    public CvController(CvWorkflowService cvWorkflow)
    {
        _cvWorkflow = cvWorkflow;
    }

    [HttpPost("{cvDocumentId:guid}/parse")]
    [RequirePermission(PermissionKeys.CandidateManage)]
    public async Task<ActionResult<ParseCvResponse>> Parse(Guid cvDocumentId, [FromQuery] bool force = false, CancellationToken ct = default)
    {
        try
        {
            var response = await _cvWorkflow.ParseCvAsync(cvDocumentId, force, ct);
            HttpContext.SetAuditInfo(AuditActions.CvParse, "CvDocument", cvDocumentId.ToString());
            return Ok(response);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
