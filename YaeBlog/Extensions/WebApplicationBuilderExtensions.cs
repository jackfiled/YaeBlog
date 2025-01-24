using AngleSharp;
using Microsoft.Extensions.Options;
using YaeBlog.Abstraction;
using YaeBlog.Services;
using YaeBlog.Models;
using YaeBlog.Processors;

namespace YaeBlog.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddYaeBlog(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<BlogOptions>(builder.Configuration.GetSection(BlogOptions.OptionName));

        builder.Services.AddHttpClient();

        builder.Services.AddMarkdig();
        builder.Services.AddYamlParser();
        builder.Services.AddSingleton<AngleSharp.IConfiguration>(_ => Configuration.Default);
        builder.Services.AddSingleton<IEssayScanService, EssayScanService>();
        builder.Services.AddSingleton<RendererService>();
        builder.Services.AddSingleton<IEssayContentService, EssayContentService>();
        builder.Services.AddTransient<ImagePostRenderProcessor>();
        builder.Services.AddTransient<HeadlinePostRenderProcessor>();
        builder.Services.AddTransient<EssayStylesPostRenderProcessor>();
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
