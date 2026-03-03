using System.CommandLine.Binding;
using YaeBlog.Abstraction;
using YaeBlog.Services;

namespace YaeBlog.Commands.Binders;

public sealed class ImageCompressServiceBinder : BinderBase<ImageCompressService>
{
    protected override ImageCompressService GetBoundValue(BindingContext bindingContext)
    {
        bindingContext.AddService(provider =>
        {
            IEssayScanService essayScanService = provider.GetRequiredService<IEssayScanService>();
            ILogger<ImageCompressService> logger = provider.GetRequiredService<ILogger<ImageCompressService>>();

            return new ImageCompressService(essayScanService, logger);
        });

        return bindingContext.GetRequiredService<ImageCompressService>();
    }
}
