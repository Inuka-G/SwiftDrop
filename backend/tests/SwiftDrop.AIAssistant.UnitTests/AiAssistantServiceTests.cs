using FluentAssertions;
using Moq;
using SwiftDrop.AIAssistant.Business.Services;
using SwiftDrop.AIAssistant.Business.Tools;
using SwiftDrop.AIAssistant.Contracts;

namespace SwiftDrop.AIAssistant.UnitTests;

public sealed class AiAssistantServiceTests
{
    [Fact]
    public async Task ChatAsync_RejectsEmptyMessage()
    {
        var service = CreateService(out _, []);

        var action = () => service.ChatAsync(new ChatRequest("  "));

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ChatAsync_UsesMatchingTool()
    {
        var tool = new Mock<IAssistantTool>();
        tool.SetupGet(candidate => candidate.Name).Returns("TrackParcel");
        tool.Setup(candidate => candidate.CanHandle(It.IsAny<string>())).Returns(true);
        tool.Setup(candidate => candidate.ExecuteAsync("track SD-1234567", It.IsAny<CancellationToken>()))
            .ReturnsAsync("In Transit");
        var service = CreateService(out var llm, [tool.Object]);

        var result = await service.ChatAsync(new ChatRequest("track SD-1234567"));

        result.Message.Should().Be("In Transit");
        result.ToolsUsed.Should().ContainSingle().Which.Should().Be("TrackParcel");
        llm.Verify(client => client.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChatAsync_UsesLlmWhenNoToolMatches()
    {
        var service = CreateService(out var llm, []);
        llm.Setup(client => client.CompleteAsync("hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello from SwiftDrop");

        var result = await service.ChatAsync(new ChatRequest("hello"));

        result.Message.Should().Be("Hello from SwiftDrop");
        result.ToolsUsed.Should().BeEmpty();
    }

    private static AiAssistantService CreateService(
        out Mock<ILlmClient> llm,
        IReadOnlyList<IAssistantTool> tools)
    {
        llm = new Mock<ILlmClient>();
        return new AiAssistantService(llm.Object, tools);
    }
}
