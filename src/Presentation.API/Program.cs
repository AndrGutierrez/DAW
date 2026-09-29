using Presentation.API.Components;
using Core.Application.Cattle;
using Infrastructure.Cattle;
using Presentation.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();

builder.Services.AddSingleton<IAnimalTagNormalizer, AnimalTagNormalizer>();
builder.Services.AddTransient<IAnimalRegistrationValidator, AnimalRegistrationValidator>();
builder.Services.AddScoped<ICattleCatalogService, CattleCatalogService>();
builder.Services.AddScoped<ICattleRepository, InMemoryCattleRepository>();
// The temporary store survives across HTTP requests until Phase 2 adds persistence.
builder.Services.AddSingleton<InMemoryCattleStore>();

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
