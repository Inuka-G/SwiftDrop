using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.AIAssistant.Business.Services;
using SwiftDrop.AIAssistant.Contracts;

namespace SwiftDrop.AIAssistant.Api.Controllers;

[ApiController]
[Route("api/v1/ai-assistant")]
public sealed class AiAssistantController(IAiAssistantService assistant) : ControllerBase
{
    [HttpPost("chat")]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Chat(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await assistant.ChatAsync(request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}

