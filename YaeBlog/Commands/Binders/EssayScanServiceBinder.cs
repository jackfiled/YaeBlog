using System.CommandLine.Binding;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Models;
using YaeBlog.Core.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace YaeBlog.Commands.Binders;

public sealed class EssayScanServiceBinder : BinderBase<IEssayScanService>
{
    protected override IEssayScanService GetBoundValue(BindingContext bindingContext)
    {
        bindingContext.AddService<IEssayScanService>(provider =>
        {
            DeserializerBuilder deserializerBuilder = new();
            deserializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance);
            deserializerBuilder.IgnoreUnmatchedProperties();

            SerializerBuilder serializerBuilder = new();
            serializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance);

            IOptions<BlogOptions> options = provider.GetRequiredService<IOptions<BlogOptions>>();
            ILogger<EssayScanService> logger = provider.GetRequiredService<ILogger<EssayScanService>>();

            return new EssayScanService(serializerBuilder.Build(), deserializerBuilder.Build(), options, logger);
        });

        return bindingContext.GetRequiredService<IEssayScanService>();
    }
}
