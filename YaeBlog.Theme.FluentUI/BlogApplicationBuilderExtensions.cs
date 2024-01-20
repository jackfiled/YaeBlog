using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using YaeBlog.Core.Builder;
using YaeBlog.Core.Extensions;

namespace YaeBlog.Theme.FluentUI;

public static class BlogApplicationBuilderExtensions
{
    public static void UseFluentTheme(this BlogApplicationBuilder builder)
    {
        builder.ConfigureWebApplication(ConfigureWebApplicationBuilder, ConfigureWebApplication);
    }

    private static void ConfigureWebApplicationBuilder(WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents();
    }

    private static void ConfigureWebApplication(WebApplication application)
    {
        application.UseStaticFiles();
        application.UseAntiforgery();
        application.MapRazorComponents<App>();
    }
}
