using AngleSharp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Abstractions;
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

        builder.Services.AddMarkdig();
        builder.Services.AddYamlParser();
        builder.Services.AddSingleton<IConfiguration>(_ => Configuration.Default);
        builder.Services.AddSingleton<IEssayScanService, EssayScanService>();
        builder.Services.AddSingleton<RendererService>();
        builder.Services.AddSingleton<IEssayContentService, EssayContentService>();
        builder.Services.AddTransient<ImagePostRenderProcessor>();
        builder.Services.AddTransient<CodeBlockPostRenderProcessor>();
        builder.Services.AddTransient<TablePostRenderProcessor>();
        builder.Services.AddTransient<HeadlinePostRenderProcessor>();
        builder.Services.AddTransient<BlogOptions>(provider =>
            provider.GetRequiredService<IOptions<BlogOptions>>().Value);

        return builder;
    }

    public static WebApplicationBuilder AddServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<BlogHostedService>();

        return builder;
    }

    public static WebApplicationBuilder AddWatcher(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<BlogChangeWatcher>();
        builder.Services.AddHostedService<BlogHotReloadService>();

        return builder;
    }
}
