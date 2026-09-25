using Presentation.API.Components;
using Core.Application.Assets;
using Infrastructure.Assets;
using Presentation.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();

// Stateless normalization is transient; the use case is scoped to a request.
// The thread-safe in-memory store must survive across requests.
builder.Services.AddTransient<IAssetTagNormalizer, AssetTagNormalizer>();
builder.Services.AddScoped<IAssetCatalogService, AssetCatalogService>();
builder.Services.AddSingleton<IAssetRepository, InMemoryAssetRepository>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
