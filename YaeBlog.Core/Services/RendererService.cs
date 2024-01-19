using Markdig;
using Microsoft.Extensions.Logging;
using YaeBlog.Core.Exceptions;
using YaeBlog.Core.Models;
using YamlDotNet.Serialization;

namespace YaeBlog.Core.Services;

public class RendererService(ILogger<RendererService> logger,
    EssayScanService essayScanService,
    MarkdownPipeline markdownPipeline,
    IDeserializer yamlDeserializer,
    EssayContentService essayContentService)
{
    public async Task RenderAsync()
    {
        List<BlogContent> contents = await essayScanService.ScanAsync();

        Parallel.ForEach(contents, content =>
        {
            MarkdownMetadata? metadata = TryParseMetadata(content);

            BlogEssay essay = new()
            {
                Title = metadata?.Title ?? content.FileName,
                PublishTime = metadata?.Date ?? DateTime.Now,
                HtmlContent = Markdown.ToHtml(content.FileContent, markdownPipeline)
            };
            if (metadata is not null)
            {
                essay.Tags.AddRange(essay.Tags);
            }

            if (!essayContentService.TryAdd(essay.Title, essay))
            {
                throw new BlogFileException(
                    $"There are two essays with the same name: '{content.FileName}'.");
            }
            logger.LogDebug("Render markdown file {}.", essay);
            logger.LogDebug("{}", essay.HtmlContent);
        });
    }

    private MarkdownMetadata? TryParseMetadata(BlogContent content)
    {
        string fileContent = content.FileContent.Trim();

        if (!fileContent.StartsWith("---"))
        {
            return null;
        }

        // 移除起始的---
        fileContent = fileContent[3..];

        int lastPos = fileContent.IndexOf("---", StringComparison.Ordinal);
        if (lastPos is -1 or 0)
        {
            return null;
        }

        string yamlContent = fileContent[..lastPos];
        MarkdownMetadata metadata = yamlDeserializer.Deserialize<MarkdownMetadata>(yamlContent);
        logger.LogDebug("Title: {}, Publish Date: {}.",
            metadata.Title, metadata.Date);

        // 返回去掉元数据之后的文本
        lastPos += 3;
        content.FileContent = fileContent[lastPos..];

        return null;
    }
}
