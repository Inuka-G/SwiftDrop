using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SwiftDrop.AIAssistant.Api.Configuration;
using SwiftDrop.AIAssistant.Business.Services;

namespace SwiftDrop.AIAssistant.Api.Clients;

internal sealed class OpenAiCompatibleLlmClient(
    HttpClient httpClient,
    IOptions<AiOptions> options) : ILlmClient
{
    private readonly AiOptions _options = options.Value;

    public async Task<string> CompleteAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return "The AI service is ready, but its API key has not been configured yet. " +
                   "You can still ask me to track a parcel or estimate a delivery fee.";
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.Model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are SwiftDrop's concise parcel delivery assistant. " +
                              "Never invent parcel status or delivery prices. " +
                              "Ask the user for missing details when needed."
                },
                new { role = "user", content = message }
            },
            temperature = 0.2
        });

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "The AI provider is taking too long to respond. Please try again shortly.";
        }

        using (response)
        {
            if ((int)response.StatusCode is 429 or 503)
            {
                return "The AI provider is temporarily busy. Please try again shortly.";
            }

            response.EnsureSuccessStatusCode();

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return string.IsNullOrWhiteSpace(content)
                ? "The AI provider returned an empty response."
                : content.Trim();
        }
    }
}

