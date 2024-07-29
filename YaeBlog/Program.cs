using YaeBlog.Components;
using YaeBlog.Core.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.Services.AddBlazorBootstrap();
builder.AddYaeBlog();

WebApplication application = builder.Build();

application.UseStaticFiles();
application.UseAntiforgery();
application.UseYaeBlog();

application.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
application.MapControllers();

await application.RunAsync();
