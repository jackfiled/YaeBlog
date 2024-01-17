using System.Collections.Concurrent;
using Markdig;
using Microsoft.Extensions.Logging;
using YaeBlog.Core.Exceptions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Services;

public class RendererService(ILogger<RendererService> logger,
    EssayScanService essayScanService,
    MarkdownPipeline markdownPipeline,
    EssayContentService essayContentService)
{
    public async Task RenderAsync()
    {
        List<BlogContent> contents = await essayScanService.ScanAsync();

        Parallel.ForEach(contents, content =>
        {
            logger.LogDebug("Render markdown file {}.", content.FileName);
            BlogEssay essay = new()
            {
                Title = content.FileName,
                PublishTime = DateTime.Now,
                HtmlContent = Markdown.ToHtml(content.FileContent, markdownPipeline)
            };

            if (!essayContentService.TryAdd(essay.Title, essay))
            {
                throw new BlogFileException(
                    $"There are two essays with the same name: '{content.FileName}'.");
            }
        });
    }
}
