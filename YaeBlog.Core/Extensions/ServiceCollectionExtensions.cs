using Markdig;
using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YaeBlog.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarkdig(this IServiceCollection collection)
    {
        MarkdownPipelineBuilder builder = new();

        collection.AddSingleton<MarkdownPipeline>(_ => builder.Build());

        return collection;
    }

    public static IServiceCollection AddYamlParser(this IServiceCollection collection)
    {
        DeserializerBuilder builder = new();

        builder.WithNamingConvention(CamelCaseNamingConvention.Instance);
        builder.IgnoreUnmatchedProperties();

        collection.AddSingleton<IDeserializer>(_ => builder.Build());

        return collection;
    }
}
