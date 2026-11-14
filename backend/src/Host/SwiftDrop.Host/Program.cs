using SwiftDrop.AIAssistant.Api;
using SwiftDrop.Host.Extensions;
using SwiftDrop.Identity.Api;
using SwiftDrop.Dispatch.Api;
using SwiftDrop.Pricing.Api;
using SwiftDrop.Reporting.Api;
using SwiftDrop.SharedKernel.Middleware;
using SwiftDrop.Parcels.Api;
using SwiftDrop.Merchants.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddJwtAuthentication();   // must come before the modules: it makes sure the signing key exists
builder.AddGoogleLogin();          // "Login with Google"; switched off when the Google client secret is not set
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddPricingModule(builder.Configuration);
builder.Services.AddParcelsModule(builder.Configuration);
builder.Services.AddDispatchModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddMerchantsModule(builder.Configuration);
builder.Services.AddAiAssistantModule(builder.Configuration);
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();
var app = builder.Build();
await app.InitializeIdentityModuleAsync();
await app.InitializePricingModuleAsync();
await app.InitializeParcelsModuleAsync();
await app.InitializeDispatchModuleAsync();
await app.InitializeReportingModuleAsync();
await app.InitializeMerchantsModuleAsync();
app.UseMiddleware<ExceptionHandlingMiddleware>();   // first, so it catches errors from everything after it
app.UseStatusCodePages();                           // gives empty 401/403/404 responses the same JSON error format
app.UseCors("Frontend");
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
app.UseAuthentication();                            // who are you? (reads the JWT)
app.UseAuthorization();                             // are you allowed? (checks [Authorize])
app.MapControllers();
app.MapOpenApi();
app.MapGet("/", () => "Hello World!");

app.Run();

// Lets the integration tests start this app.
public partial class Program;
