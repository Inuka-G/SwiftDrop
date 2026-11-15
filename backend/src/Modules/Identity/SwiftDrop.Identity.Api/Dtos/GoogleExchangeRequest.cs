using System.ComponentModel.DataAnnotations;

namespace SwiftDrop.Identity.Api.Dtos;

/// The one-time code that the backend sent to the React app after Google sign-in.
public record GoogleExchangeRequest([Required, StringLength(100)] string Code);
