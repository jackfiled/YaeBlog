using AngleSharp;
using Microsoft.Extensions.Options;
using YaeBlog.Abstraction;
using YaeBlog.Services;
using YaeBlog.Models;
using YaeBlog.Processors;

namespace YaeBlog.Extensions;

public static class HostApplicationBuilderExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public ConsoleInfoService AddYaeCommand(string[] arguments)
        {
            builder.AddCommonServices();

            builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

            builder.Services.AddTransient<ImageCompressService>();
            builder.Services.AddHostedService<YaeCommandService>(provider =>
            {
                IEssayScanService essayScanService = provider.GetRequiredService<IEssayScanService>();
                ImageCompressService imageCompressService = provider.GetRequiredService<ImageCompressService>();
                ConsoleInfoService consoleInfoService = provider.GetRequiredService<ConsoleInfoService>();
                IOptions<BlogOptions> blogOptions = provider.GetRequiredService<IOptions<BlogOptions>>();
                ILogger<YaeCommandService> logger = provider.GetRequiredService<ILogger<YaeCommandService>>();
                IHostApplicationLifetime hostApplicationLifetime =
                    provider.GetRequiredService<IHostApplicationLifetime>();

                return new YaeCommandService(arguments, essayScanService, imageCompressService, consoleInfoService,
                    hostApplicationLifetime, blogOptions, logger);
            });

            ConsoleInfoService infoService = new();
            builder.Services.AddSingleton<ConsoleInfoService>(_ => infoService);

            return infoService;
        }

        private void AddCommonServices()
        {
            builder.Services.AddHttpClient()
                .AddMarkdig()
                .AddYamlParser();

            builder.ConfigureOptions<BlogOptions>(BlogOptions.OptionName)
                .ConfigureOptions<GiteaOptions>(GiteaOptions.OptionName);

            builder.Services.AddSingleton<IEssayScanService, EssayScanService>();
        }

        private IHostApplicationBuilder ConfigureOptions<T>(string optionSectionName) where T : class
        {
            builder.Services
                .AddOptions<T>()
                .Bind(builder.Configuration.GetSection(optionSectionName))
                .ValidateDataAnnotations();
            return builder;
        }
    }

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddYaeServer(ConsoleInfoService consoleInfoService)
        {
            builder.AddCommonServices();

            builder.Services.AddSingleton<AngleSharp.IConfiguration>(_ => Configuration.Default)
                .AddSingleton<ConsoleInfoService>(_ => consoleInfoService)
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

            builder.Services.AddHostedService<StartServerService>();

            return builder;
        }
    }
}
