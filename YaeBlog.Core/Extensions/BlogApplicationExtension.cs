using Microsoft.Extensions.DependencyInjection;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Builder;
using YaeBlog.Core.Services;

namespace YaeBlog.Core.Extensions;

public static class BlogApplicationExtension
{
    public static void UsePreRenderProcessor<T>(this BlogApplication application)
        where T : IPreRenderProcessor
    {
        RendererService rendererService =
            application.Services.GetRequiredService<RendererService>();
        T preRenderProcessor =
            application.Services.GetRequiredService<T>();
        rendererService.AddPreRenderProcessor(preRenderProcessor);
    }

    public static void UsePostRenderProcessor<T>(this BlogApplication application)
        where T : IPostRenderProcessor
    {
        RendererService rendererService =
            application.Services.GetRequiredService<RendererService>();
        T postRenderProcessor =
            application.Services.GetRequiredService<T>();
        rendererService.AddPostRenderProcessor(postRenderProcessor);
    }
}
