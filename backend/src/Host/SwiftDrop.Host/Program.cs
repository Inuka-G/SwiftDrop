using SwiftDrop.Pricing.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddPricingModule(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
await app.InitializePricingModuleAsync();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
app.MapControllers();
app.MapOpenApi();
app.MapGet("/", () => "Hello World!");

app.Run();
