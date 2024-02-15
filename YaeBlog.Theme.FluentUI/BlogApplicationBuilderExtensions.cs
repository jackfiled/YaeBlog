using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using YaeBlog.Core.Builder;
using YaeBlog.Core.Extensions;

namespace YaeBlog.Theme.FluentUI;

public static class BlogApplicationBuilderExtensions
{
    public static void UseFluentTheme(this BlogApplicationBuilder builder)
    {
        builder.ConfigureWebApplicationBuilder(ConfigureWebApplicationBuilder);
        builder.ConfigureWebApplication(ConfigureWebApplication);
    }

    private static void ConfigureWebApplicationBuilder(WebApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents();
        builder.Services.AddFluentUIComponents();
    }

    private static void ConfigureWebApplication(WebApplication application)
    {
        application.UseStaticFiles();
        application.UseAntiforgery();
        application.UseStatusCodePagesWithRedirects("~/NotFound");
        application.MapRazorComponents<App>();
    }
}
