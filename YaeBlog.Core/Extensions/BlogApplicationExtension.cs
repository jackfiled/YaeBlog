using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YaeBlog.Core.Builder;
using YaeBlog.Core.Models;
using YaeBlog.Core.Services;

namespace YaeBlog.Core.Extensions;

internal static class BlogApplicationExtension
{
    public static BlogApplicationBuilder ConfigureBlogApplication(this BlogApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.Configure<BlogOptions>(
            builder.Configuration.GetSection(BlogOptions.OptionName));

        builder.Services.AddHostedService<BlogHostedService>();
        builder.Services.AddSingleton<EssayScanService>();
        builder.Services.AddSingleton<RendererService>();
        builder.Services.AddSingleton<MarkdownPipeline>(
            _ => builder.MarkdigPipelineBuilder.Build());
        builder.Services.AddSingleton<EssayContentService>();

        return builder;
    }
}
