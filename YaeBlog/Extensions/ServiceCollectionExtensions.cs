using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YaeBlog.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMarkdig(this IServiceCollection collection)
    {
        MarkdownPipelineBuilder builder = new();

        builder.UseAdvancedExtensions();

        collection.AddSingleton<MarkdownPipeline>(_ => builder.Build());

        return collection;
    }

    public static IServiceCollection AddYamlParser(this IServiceCollection collection)
    {
        DeserializerBuilder deserializerBuilder = new();
        deserializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance);
        deserializerBuilder.IgnoreUnmatchedProperties();
        collection.AddSingleton(deserializerBuilder.Build());

        SerializerBuilder serializerBuilder = new();
        serializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance);
        collection.AddSingleton(serializerBuilder.Build());

        return collection;
    }
}
