using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YaeBlog.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        public IServiceCollection AddMarkdig()
        {
            MarkdownPipelineBuilder builder = new();

            builder.UseAdvancedExtensions();

            collection.AddSingleton<MarkdownPipeline>(_ => builder.Build());

            return collection;
        }

        public IServiceCollection AddYamlParser()
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
}
