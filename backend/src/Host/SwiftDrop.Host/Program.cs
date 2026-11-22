
using SwiftDrop.AIAssistant.Api;
using SwiftDrop.Host.Extensions;
using SwiftDrop.Identity.Api;
using SwiftDrop.Dispatch.Api;
using SwiftDrop.Pricing.Api;
using SwiftDrop.Reporting.Api;
using SwiftDrop.SharedKernel.Middleware;
using SwiftDrop.Parcels.Api;
using SwiftDrop.Merchants.Api;
using SwiftDrop.Realtime.Api;
using SwiftDrop.Notifications.Api;
using SwiftDrop.Webhooks.Api;
using SwiftDrop.SharedKernel.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddJwtAuthentication();
builder.AddGoogleLogin();

builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
    allowedOrigins = ["http://localhost:5173", "http://127.0.0.1:5173"];

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()));
}

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddPricingModule(builder.Configuration);
builder.Services.AddParcelsModule(builder.Configuration);
builder.Services.AddDispatchModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddMerchantsModule(builder.Configuration);
builder.Services.AddAiAssistantModule(builder.Configuration);

builder.Services.AddRealtimeModule();

builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.PostConfigure<RabbitMqOptions>(options =>
    options.RequiredOutboxRoutes = [.. options.RequiredOutboxRoutes,
        SwiftDrop.Parcels.Contracts.Events.ParcelEventRoutingKeys.StatusChanged]);
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddWebhooksModule(builder.Configuration);

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
await app.InitializeNotificationsModuleAsync();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();

app.UseCors("Frontend");

app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));

app.UseCors(CorsExtensions.FrontendPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRealtimeEndpoints();
app.MapOpenApi();
app.MapGet("/", () => "Hello World!");

app.Run();

// Lets the integration tests start this app.
public partial class Program;
