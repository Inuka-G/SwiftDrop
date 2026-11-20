using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SwiftDrop.AIAssistant.Business.Tools;

namespace SwiftDrop.AIAssistant.Api.Controllers;

[ApiController]
[Route("api/v1/ai-assistant/mcp")]
public sealed class McpController(IEnumerable<IAssistantTool> tools) : ControllerBase
{
    private readonly IReadOnlyList<IAssistantTool> _tools = tools.ToList();

    [HttpGet("tools")]
    public IActionResult ListTools() => Ok(_tools.Select(tool => new
    {
        tool.Name,
        tool.Description
    }));

    [HttpPost]
    public async Task<IActionResult> Handle(
        McpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.JsonRpc != "2.0")
        {
            return Ok(Error(request.Id, -32600, "Invalid JSON-RPC request."));
        }

        if (request.Method == "tools/list")
        {
            var result = new
            {
                tools = _tools.Select(tool => new
                {
                    name = tool.Name,
                    description = tool.Description,
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            message = new { type = "string" }
                        },
                        required = new[] { "message" }
                    }
                })
            };

            return Ok(Success(request.Id, result));
        }

        if (request.Method == "tools/call")
        {
            var name = ReadString(request.Params, "name");
            var message = request.Params.TryGetProperty("arguments", out var arguments)
                ? ReadString(arguments, "message")
                : null;
            var tool = _tools.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

            if (tool is null)
            {
                return Ok(Error(request.Id, -32602, $"Unknown tool '{name}'."));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return Ok(Error(request.Id, -32602, "The message argument is required."));
            }

            var output = await tool.ExecuteAsync(message, cancellationToken);
            return Ok(Success(request.Id, new
            {
                content = new[] { new { type = "text", text = output } },
                isError = false
            }));
        }

        return Ok(Error(request.Id, -32601, $"Method '{request.Method}' was not found."));
    }

    [HttpPost("tools/{name}")]
    public async Task<IActionResult> CallTool(
        string name,
        ToolCallRequest request,
        CancellationToken cancellationToken)
    {
        var tool = _tools.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

        if (tool is null)
        {
            return NotFound(new { error = $"Unknown tool '{name}'." });
        }

        var result = await tool.ExecuteAsync(request.Message, cancellationToken);
        return Ok(new { result });
    }

    public sealed record ToolCallRequest(string Message);

    public sealed record McpRequest(
        [property: System.Text.Json.Serialization.JsonPropertyName("jsonrpc")] string JsonRpc,
        JsonElement Id,
        string Method,
        JsonElement Params);

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;

    private static object Success(JsonElement id, object result) => new
    {
        jsonrpc = "2.0",
        id,
        result
    };

    private static object Error(JsonElement id, int code, string message) => new
    {
        jsonrpc = "2.0",
        id,
        error = new { code, message }
    };
}

