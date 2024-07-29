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
        builder.Services.AddSingleton<EssayScanService>();
        builder.Services.AddSingleton<RendererService>();
        builder.Services.AddSingleton<EssayContentService>();
        builder.Services.AddSingleton<IEssayContentService, EssayContentService>(provider =>
            provider.GetRequiredService<EssayContentService>());
        builder.Services.AddSingleton<TableOfContentService>();
        builder.Services.AddSingleton<ITableOfContentService, TableOfContentService>(provider =>
            provider.GetRequiredService<TableOfContentService>());
        builder.Services.AddTransient<ImagePostRenderProcessor>();
        builder.Services.AddTransient<CodeBlockPostRenderProcessor>();
        builder.Services.AddTransient<TablePostRenderProcessor>();
        builder.Services.AddTransient<HeadlinePostRenderProcessor>();
        builder.Services.AddTransient<BlogOptions>(provider =>
            provider.GetRequiredService<IOptions<BlogOptions>>().Value);

        builder.Services.AddHostedService<BlogHostedService>();

        return builder;
    }
}
