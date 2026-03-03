using AngleSharp;
using YaeBlog.Abstraction;
using YaeBlog.Services;
using YaeBlog.Models;
using YaeBlog.Processors;

namespace YaeBlog.Extensions;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddYaeBlog()
        {
            builder.ConfigureOptions<BlogOptions>(BlogOptions.OptionName)
                .ConfigureOptions<GiteaOptions>(GiteaOptions.OptionName);

            builder.Services.AddHttpClient()
                .AddMarkdig()
                .AddYamlParser();

            builder.Services.AddSingleton<AngleSharp.IConfiguration>(_ => Configuration.Default)
                .AddSingleton<IEssayScanService, EssayScanService>()
                .AddSingleton<RendererService>()
                .AddSingleton<IEssayContentService, EssayContentService>()
                .AddTransient<ImagePostRenderProcessor>()
                .AddTransient<HeadlinePostRenderProcessor>()
                .AddTransient<EssayStylesPostRenderProcessor>()
                .AddTransient<GiteaFetchService>()
                .AddSingleton<GitHeapMapService>();

            return builder;
        }

        public WebApplicationBuilder AddServer()
        {
            builder.Services.AddHostedService<BlogHostedService>();

            return builder;
        }

        public WebApplicationBuilder AddWatcher()
        {
            builder.Services.AddTransient<BlogChangeWatcher>();
            builder.Services.AddHostedService<BlogHotReloadService>();

            return builder;
        }

        private WebApplicationBuilder ConfigureOptions<T>(string optionSectionName) where T : class
        {
            builder.Services
                .AddOptions<T>()
                .Bind(builder.Configuration.GetSection(optionSectionName))
                .ValidateDataAnnotations();
            return builder;
        }
    }
}
