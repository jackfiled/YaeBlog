﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Services;

public class WebApplicationHostedService : IHostedService
{
    private readonly WebApplicationBuilder _websiteBuilder = WebApplication.CreateBuilder();

    private readonly List<Action<WebApplication>> _webApplicationConfigurations;

    private readonly IOptions<BlogOptions> _options;

    private Website? _currentWebsite;

    public WebApplicationHostedService(List<Action<WebApplicationBuilder>> webApplicationBuilderConfigurations,
        List<Action<WebApplication>> webApplicationConfigurations,
        IServiceProvider hostServiceProvider)
    {
        _webApplicationConfigurations = webApplicationConfigurations;
        _options = hostServiceProvider.GetRequiredService<IOptions<BlogOptions>>();

        foreach (Action<WebApplicationBuilder> configure in webApplicationBuilderConfigurations)
        {
            configure(_websiteBuilder);
        }

        AddHostServices(hostServiceProvider);
    }

    public async Task BuildWebsite()
    {
        if (_currentWebsite is not null)
        {
            await _currentWebsite.ShutdownAsync(new CancellationToken());
        }

        WebApplication application = _websiteBuilder.Build();
        application.UsePathBase("/" + _options.Value.SubPath);
        foreach (Action<WebApplication> configure in _webApplicationConfigurations)
        {
            configure(application);
        }
        IHostLifetime websiteLifetime = application.Services.GetRequiredService<IHostLifetime>();
        _currentWebsite = new Website(application, websiteLifetime);
    }

    public Task RunAsync()
    {
        if (_currentWebsite is not null)
        {
            return _currentWebsite.RunAsync();
        }

        throw new InvalidOperationException("Website has not been built.");
    }

    public Task ShutdownAsync()
    {
        if (_currentWebsite is { Running: true })
        {
            return _currentWebsite.ShutdownAsync(new CancellationToken());
        }

        throw new InvalidOperationException("Website is not running.");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await BuildWebsite();
        _ = RunAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_currentWebsite is { Running: true })
        {
            return _currentWebsite.ShutdownAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    private void AddHostServices(IServiceProvider provider)
    {
        _websiteBuilder.Services.AddSingleton<EssayContentService>(_ =>
            provider.GetRequiredService<EssayContentService>());
        _websiteBuilder.Services.AddTransient<BlogOptions>(_ =>
            provider.GetRequiredService<IOptions<BlogOptions>>().Value);
    }


    private class Website(WebApplication application, IHostLifetime websiteLifetime)
    {
        public bool Running { get; private set; }

        public Task RunAsync()
        {
            Running = true;
            return application.RunAsync();
        }

        public async Task ShutdownAsync(CancellationToken cancellationToken)
        {
            if (!Running)
            {
                await websiteLifetime.StopAsync(cancellationToken);
            }

            Running = false;
        }
    }
}
