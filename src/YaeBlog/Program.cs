using YaeBlog.Components;
using YaeBlog.Extensions;
using YaeBlog.Services;

HostApplicationBuilder consoleBuilder = Host.CreateApplicationBuilder(args);

ConsoleInfoService consoleInfoService = consoleBuilder.AddYaeCommand(args);

IHost consoleApp = consoleBuilder.Build();
await consoleApp.RunAsync();

if (consoleInfoService.IsOneShotCommand)
{
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.AddYaeServer(consoleInfoService);

WebApplication application = builder.Build();

application.MapStaticAssets();
application.UseAntiforgery();
application.UseYaeBlog();

application.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
application.MapControllers();

await application.RunAsync();
