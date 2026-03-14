using AngleSharp;
using Microsoft.Extensions.Options;
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
                .AddTransient<BlogChangeWatcher>()
                .AddTransient<BlogHotReloadService>()
                .AddSingleton<GitHeapMapService>();

            return builder;
        }

        public WebApplicationBuilder AddYaeCommand(string[] arguments)
        {
            builder.Services.AddHostedService<YaeCommandService>(provider =>
            {
                IEssayScanService essayScanService = provider.GetRequiredService<IEssayScanService>();
                IOptions<BlogOptions> blogOptions = provider.GetRequiredService<IOptions<BlogOptions>>();
                ILogger<YaeCommandService> logger = provider.GetRequiredService<ILogger<YaeCommandService>>();
                IHostApplicationLifetime applicationLifetime = provider.GetRequiredService<IHostApplicationLifetime>();

                return new YaeCommandService(arguments, essayScanService, provider, blogOptions, logger,
                    applicationLifetime);
            });

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
