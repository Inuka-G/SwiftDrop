using System.ComponentModel.DataAnnotations;

namespace SwiftDrop.Identity.Api.Dtos;

public record SignupRequest(
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password,
    [Phone, StringLength(20)] string? PhoneNumber);
