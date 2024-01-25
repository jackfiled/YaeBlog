﻿using Markdig;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YaeBlog.Core.Builder;
using YaeBlog.Core.Models;
using YaeBlog.Core.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YaeBlog.Core.Extensions;

public static class BlogApplicationBuilderExtension
{
    internal static void ConfigureDefaultBlogApplicationBuilder(this BlogApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.Configure<BlogOptions>(
            builder.Configuration.GetSection(BlogOptions.OptionName));

        builder.YamlDeserializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance);
        builder.YamlDeserializerBuilder.IgnoreUnmatchedProperties();

        builder.Services.AddSingleton<MarkdownPipeline>(
            _ => builder.MarkdigPipelineBuilder.Build());
        builder.Services.AddSingleton<IDeserializer>(
            _ => builder.YamlDeserializerBuilder.Build());

        builder.Services.AddHostedService<BlogHostedService>();
        builder.Services.AddSingleton<EssayScanService>();
        builder.Services.AddSingleton<RendererService>();
        builder.Services.AddSingleton<EssayContentService>();

        builder.Services.AddHostedService<WebApplicationHostedService>((provider)
            => new WebApplicationHostedService(builder.WebApplicationBuilderConfigurations,
                builder.WebApplicationConfigurations, provider));
    }

    public static void ConfigureWebApplicationBuilder(this BlogApplicationBuilder builder,
        Action<WebApplicationBuilder> configureWebApplicationBuilder)
    {
        builder.WebApplicationBuilderConfigurations.Add(configureWebApplicationBuilder);
    }

    public static void ConfigureWebApplication(this BlogApplicationBuilder builder,
        Action<WebApplication> configureWebApplication)
    {
        builder.WebApplicationConfigurations.Add(configureWebApplication);
    }
}
