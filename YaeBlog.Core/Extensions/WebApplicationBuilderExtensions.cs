using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using YaeBlog.Core.Models;
using YaeBlog.Core.Processors;
using YaeBlog.Core.Services;

namespace YaeBlog.Core.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddYaeBlog(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<BlogOptions>(builder.Configuration.GetSection(BlogOptions.OptionName));

        builder.Services.AddHttpClient();
        builder.Services.AddFluentUIComponents();

        builder.Services.AddMarkdig();
        builder.Services.AddYamlParser();
        builder.Services.AddSingleton<EssayScanService>();
        builder.Services.AddSingleton<RendererService>();
        builder.Services.AddSingleton<EssayContentService>();
        builder.Services.AddTransient<ImagePostRenderProcessor>();
        builder.Services.AddTransient<BlogOptions>(provider =>
            provider.GetRequiredService<IOptions<BlogOptions>>().Value);

        builder.Services.AddHostedService<BlogHostedService>();

        return builder;
    }
}
