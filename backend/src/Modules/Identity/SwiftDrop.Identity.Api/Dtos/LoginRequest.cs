using System.ComponentModel.DataAnnotations;

namespace SwiftDrop.Identity.Api.Dtos;

/// Username holds either a username (such as "admin") or an email address.
/// RememberMe = false makes the refresh-token cookie end when the browser closes.
public record LoginRequest(
    [Required, StringLength(254)] string Username,
    [Required, StringLength(100)] string Password,
    bool RememberMe = true);
