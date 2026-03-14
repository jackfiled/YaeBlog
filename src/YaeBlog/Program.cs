using YaeBlog.Components;
using YaeBlog.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();
builder.AddYaeBlog();
builder.AddYaeCommand(args);

WebApplication application = builder.Build();

application.MapStaticAssets();
application.UseAntiforgery();
application.UseYaeBlog();

application.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
application.MapControllers();

await application.RunAsync();
