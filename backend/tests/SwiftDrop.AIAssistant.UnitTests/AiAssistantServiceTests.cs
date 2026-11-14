using FluentAssertions;
using Moq;
using SwiftDrop.AIAssistant.Business.Services;
using SwiftDrop.AIAssistant.Contracts;

namespace SwiftDrop.AIAssistant.UnitTests;

public sealed class AiAssistantServiceTests
{
    [Fact]
    public async Task ChatAsync_RejectsEmptyMessage()
    {
        var service = CreateService(out _, out _);

        var action = () => service.ChatAsync(new ChatRequest("  "));

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ChatAsync_UsesMatchingTool()
    {
        var service = CreateService(out var llm, out var mcpClient);
        mcpClient.Setup(client => client.CallToolAsync(
                "TrackParcel", "track SD-1234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync("In Transit");

        var result = await service.ChatAsync(new ChatRequest("track SD-1234567"));

        result.Message.Should().Be("In Transit");
        result.ToolsUsed.Should().ContainSingle().Which.Should().Be("TrackParcel");
        llm.Verify(client => client.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChatAsync_UsesLlmWhenNoToolMatches()
    {
        var service = CreateService(out var llm, out var mcpClient);
        llm.Setup(client => client.CompleteAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello from SwiftDrop");

        var result = await service.ChatAsync(new ChatRequest("hello"));

        result.Message.Should().Be("Hello from SwiftDrop");
        result.ToolsUsed.Should().BeEmpty();
        mcpClient.Verify(client => client.CallToolAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static AiAssistantService CreateService(
        out Mock<ILlmClient> llm,
        out Mock<IMcpClient> mcpClient)
    {
        llm = new Mock<ILlmClient>();
        mcpClient = new Mock<IMcpClient>();
        return new AiAssistantService(llm.Object, mcpClient.Object);
    }
}
