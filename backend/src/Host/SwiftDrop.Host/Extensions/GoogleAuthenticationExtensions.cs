using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Host.Extensions;

public static class GoogleAuthenticationExtensions
{
    /// Adds "Login with Google" using OpenID Connect, the standard identity layer on top of OAuth 2.0.
    /// ASP.NET Core then handles the security checks from the design report for us:
    /// state (stops forged callbacks), PKCE (stops stolen codes), and the signed ID token with its nonce.
    /// If the Client ID or secret is missing, Google login is simply switched off, so teammates
    /// without the secrets can still run the app.
    public static WebApplicationBuilder AddGoogleLogin(this WebApplicationBuilder builder)
    {
        var google = builder.Configuration.GetSection(GoogleAuthOptions.SectionName).Get<GoogleAuthOptions>() ?? new();
        if (!google.IsConfigured) return builder;

        builder.Services.AddAuthentication()
            .AddCookie(GoogleAuthOptions.ExternalCookieScheme, options =>
            {
                options.Cookie.Name = "SwiftDrop.Google";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddOpenIdConnect(GoogleAuthOptions.Scheme, "Google", options =>
            {
                options.Authority = "https://accounts.google.com";
                options.ClientId = google.ClientId;
                options.ClientSecret = google.ClientSecret;
                options.CallbackPath = GoogleAuthOptions.CallbackPath;
                options.SignInScheme = GoogleAuthOptions.ExternalCookieScheme;

                options.ResponseType = OpenIdConnectResponseType.Code;   // Authorization Code flow
                options.UsePkce = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.MapInboundClaims = false;   // keep Google's claim names: sub, email, name, email_verified
                options.SaveTokens = false;         // we only need who the person is, not Google's tokens

                // Google sends the result back as a normal redirect (not a form POST), so the
                // Lax cookies below are still sent and the flow also works on http://localhost.
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                // Cancelling on Google's page, or any failure, goes back to the React app with a message
                // instead of showing a server error page.
                options.Events.OnRemoteFailure = context =>
                {
                    context.Response.Redirect(QueryHelpers.AddQueryString(
                        google.FrontendCallbackUrl, "error", "Google sign-in was cancelled or failed. Please try again."));
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });

        return builder;
    }
}
