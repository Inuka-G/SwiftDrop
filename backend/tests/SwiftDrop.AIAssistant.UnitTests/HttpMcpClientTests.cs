using System.Net;
using System.Text;
using FluentAssertions;
using SwiftDrop.AIAssistant.Api.Clients;

namespace SwiftDrop.AIAssistant.UnitTests;

public sealed class HttpMcpClientTests
{
    [Fact]
    public async Task CallToolAsync_SendsJsonRpcAndReadsTextContent()
    {
        string? sentBody = null;
        var handler = new StubHandler(async request =>
        {
            sentBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"jsonrpc":"2.0","id":"1","result":{"content":[{"type":"text","text":"LKR 430.00"}],"isError":false}}
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/api/v1/ai-assistant/mcp")
        };
        var client = new HttpMcpClient(httpClient);

        var result = await client.CallToolAsync(
            "EstimateFee",
            "How much to send 2 kg to Colombo?");

        result.Should().Be("LKR 430.00");
        sentBody.Should().Contain("\"method\":\"tools/call\"");
        sentBody.Should().Contain("\"name\":\"EstimateFee\"");
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responseFactory(request);
    }
}
