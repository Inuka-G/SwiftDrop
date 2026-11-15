using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Business.Services;

namespace SwiftDrop.Merchants.Api.Controllers;

[ApiController]
[Route("api/v1/oauth")]
[Produces("application/json")]
public sealed class OAuthController(IMerchantService merchantService) : ControllerBase
{
    /// <summary>
    /// OAuth 2.0 token endpoint supporting client_credentials grant type per RFC 6749.
    /// Accepts credentials via HTTP Basic Auth header, application/x-www-form-urlencoded body, or JSON body.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType<OAuthTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Token(CancellationToken ct)
    {
        string? grantType = null;
        string? clientId = null;
        string? clientSecret = null;
        string? scope = null;

        // 1. Check HTTP Basic Authentication Header (Authorization: Basic base64(client_id:client_secret))
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var encoded = headerValue["Basic ".Length..].Trim();
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                    var parts = decoded.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        clientId = parts[0];
                        clientSecret = parts[1];
                    }
                }
                catch
                {
                    // Ignore parse error, missing/invalid credentials will fail below
                }
            }
        }

        // 2. Check Form urlencoded
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(ct);
            grantType = form["grant_type"].ToString();
            if (string.IsNullOrEmpty(clientId))
                clientId = form["client_id"].ToString();
            if (string.IsNullOrEmpty(clientSecret))
                clientSecret = form["client_secret"].ToString();
            scope = form["scope"].ToString();
        }
        else if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            // 3. Check JSON body
            var jsonRequest = await Request.ReadFromJsonAsync<OAuthTokenRequest>(ct);
            if (jsonRequest is not null)
            {
                grantType ??= jsonRequest.GrantType;
                clientId ??= jsonRequest.ClientId;
                clientSecret ??= jsonRequest.ClientSecret;
                scope ??= jsonRequest.Scope;
            }
        }

        var request = new OAuthTokenRequest
        {
            GrantType = grantType,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Scope = scope
        };

        var response = await merchantService.ExchangeClientCredentialsAsync(request, ct);
        return Ok(response);
    }
}

