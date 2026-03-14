using YaeBlog.Abstraction;
using YaeBlog.Processors;
using YaeBlog.Services;

namespace YaeBlog.Extensions;

public static class WebApplicationExtensions
{
    public static void UseYaeBlog(this WebApplication application)
    {
        application.UsePostRenderProcessor<ImagePostRenderProcessor>();
        application.UsePostRenderProcessor<HeadlinePostRenderProcessor>();
        application.UsePostRenderProcessor<EssayStylesPostRenderProcessor>();
    }

    private static void UsePreRenderProcessor<T>(this WebApplication application) where T : IPreRenderProcessor
    {
        RendererService rendererService = application.Services.GetRequiredService<RendererService>();
        T preRenderProcessor = application.Services.GetRequiredService<T>();

        rendererService.AddPreRenderProcessor(preRenderProcessor);
    }

    private static void UsePostRenderProcessor<T>(this WebApplication application) where T : IPostRenderProcessor
    {
        RendererService rendererService = application.Services.GetRequiredService<RendererService>();
        T postRenderProcessor = application.Services.GetRequiredService<T>();

        rendererService.AddPostRenderProcessor(postRenderProcessor);
    }
}
