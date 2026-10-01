using System.Text;
using Core.Application.Cattle;
using Core.Application.Security;
using Infrastructure;
using Infrastructure.Cattle;
using Infrastructure.Security;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Presentation.API.Authorization;
using Presentation.API.Components;
using Presentation.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IAnimalTagNormalizer, AnimalTagNormalizer>();
builder.Services.AddTransient<IAnimalRegistrationValidator, AnimalRegistrationValidator>();
builder.Services.AddScoped<ICattleCatalogService, CattleCatalogService>();
builder.Services.AddScoped<ICattleRepository, InMemoryCattleRepository>();
// The in-memory store keeps cattle records available across HTTP requests.
builder.Services.AddSingleton<InMemoryCattleStore>();

var app = builder.Build();

if (args.Contains("--seed"))
{
    await app.Services.SeedDatabaseAsync();
    return;
}

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

app.UseAuthentication();
app.UseAuthorization();

var storageOptions = app.Services.GetRequiredService<IOptions<StorageOptions>>().Value;
var uploadsRoot = Path.GetFullPath(storageOptions.RootPath);
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = storageOptions.RequestPath
});

app.UseAntiforgery();
app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
