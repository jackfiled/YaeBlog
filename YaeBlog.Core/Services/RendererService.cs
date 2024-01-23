using System.Diagnostics;
using Markdig;
using Microsoft.Extensions.Logging;
using YaeBlog.Core.Exceptions;
using YaeBlog.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YaeBlog.Core.Services;

public class RendererService(ILogger<RendererService> logger,
    EssayScanService essayScanService,
    MarkdownPipeline markdownPipeline,
    IDeserializer yamlDeserializer,
    EssayContentService essayContentService)
{
    private readonly Stopwatch _stopwatch = new();

    public async Task RenderAsync()
    {
        _stopwatch.Start();
        logger.LogInformation("Render essays start.");

        List<BlogContent> contents = await essayScanService.ScanAsync();

        List<BlogEssay> essays = [];

        await Task.Run(() =>
        {
            foreach (BlogContent content in contents)
            {
                MarkdownMetadata? metadata = TryParseMetadata(content);
                BlogEssay essay = new()
                {
                    Title = metadata?.Title ?? content.FileName,
                    FileName = content.FileName,
                    PublishTime = metadata?.Date ?? DateTime.Now,
                    HtmlContent = content.FileContent
                };

                if (metadata?.Tags is not null)
                {
                    essay.Tags.AddRange(metadata.Tags);
                }
                essays.Add(essay);
            }
        });

        Parallel.ForEach(essays, essay =>
        {

            BlogEssay newEssay = new()
            {
                Title = essay.Title,
                FileName = essay.FileName,
                PublishTime = essay.PublishTime,
                HtmlContent = Markdown.ToHtml(essay.HtmlContent, markdownPipeline)
            };
            newEssay.Tags.AddRange(essay.Tags);

            if (!essayContentService.TryAdd(newEssay.FileName, newEssay))
            {
                throw new BlogFileException(
                    $"There are two essays with the same name: '{newEssay.FileName}'.");
            }
            logger.LogDebug("Render markdown file {}.", newEssay);
        });

        _stopwatch.Stop();
        logger.LogInformation("Render finished, consuming {} s.",
            _stopwatch.Elapsed.ToString("s\\.fff"));
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
        // 返回去掉元数据之后的文本
        lastPos += 3;
        content.FileContent = fileContent[lastPos..];

        try
        {
            MarkdownMetadata metadata =
                yamlDeserializer.Deserialize<MarkdownMetadata>(yamlContent);
            logger.LogDebug("Title: {}, Publish Date: {}.",
                metadata.Title, metadata.Date);

            return metadata;
        }
        catch (YamlException e)
        {
            logger.LogWarning("Failed to parse '{}' metadata: {}", yamlContent, e);
            return null;
        }
    }
}
