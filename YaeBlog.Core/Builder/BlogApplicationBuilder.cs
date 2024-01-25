using Markdig;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YaeBlog.Core.Extensions;
using YamlDotNet.Serialization;

namespace YaeBlog.Core.Builder;

public sealed class BlogApplicationBuilder : IHostApplicationBuilder
{
    private readonly HostApplicationBuilder _hostApplicationBuilder;

    internal List<Action<WebApplicationBuilder>> WebApplicationBuilderConfigurations { get; } = [];

    internal List<Action<WebApplication>> WebApplicationConfigurations { get; } = [];

    public MarkdownPipelineBuilder MarkdigPipelineBuilder { get; }

    public DeserializerBuilder YamlDeserializerBuilder { get; }

    internal BlogApplicationBuilder(BlogApplicationOptions options)
    {
        ConfigurationManager configuration = new();
        MarkdigPipelineBuilder = new MarkdownPipelineBuilder();
        YamlDeserializerBuilder = new DeserializerBuilder();

        _hostApplicationBuilder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = options.Args, Configuration = configuration
        });
    }

    public BlogApplication Build()
    {
        this.ConfigureDefaultBlogApplicationBuilder();
        BlogApplication application = new(_hostApplicationBuilder.Build());
        application.ConfigureDefaultBlogApplication();
        return application;
    }

    public void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull
        => _hostApplicationBuilder.ConfigureContainer(factory, configure);

    public IDictionary<object, object> Properties
        => (_hostApplicationBuilder as IHostApplicationBuilder).Properties;

    public IHostEnvironment Environment => _hostApplicationBuilder.Environment;

    public IConfigurationManager Configuration => _hostApplicationBuilder.Configuration;

    public ILoggingBuilder Logging => _hostApplicationBuilder.Logging;

    public IMetricsBuilder Metrics => _hostApplicationBuilder.Metrics;

    public IServiceCollection Services => _hostApplicationBuilder.Services;
}
