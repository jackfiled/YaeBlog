using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.Logging;
using YaeBlog.Core.Abstractions;
using YaeBlog.Core.Exceptions;
using YaeBlog.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace YaeBlog.Core.Services;

public partial class RendererService(
    ILogger<RendererService> logger,
    EssayScanService essayScanService,
    MarkdownPipeline markdownPipeline,
    IDeserializer yamlDeserializer,
    EssayContentService essayContentService)
{
    private readonly Stopwatch _stopwatch = new();

    private readonly List<IPreRenderProcessor> _preRenderProcessors = [];

    private readonly List<IPostRenderProcessor> _postRenderProcessors = [];

    public async Task RenderAsync()
    {
        _stopwatch.Start();
        logger.LogInformation("Render essays start.");

        List<BlogContent> contents = await essayScanService.ScanAsync();
        IEnumerable<BlogContent> preProcessedContents = await PreProcess(contents);

        List<BlogEssay> essays = [];
        await Task.Run(() =>
        {
            foreach (BlogContent content in preProcessedContents)
            {
                MarkdownMetadata? metadata = TryParseMetadata(content);
                uint wordCount = GetWordCount(content);
                BlogEssay essay = new()
                {
                    Title = metadata?.Title ?? content.FileName,
                    FileName = content.FileName,
                    Description = GetDescription(content),
                    WordCount = wordCount,
                    ReadTime = CalculateReadTime(wordCount),
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

        ConcurrentBag<BlogEssay> postProcessEssays = [];
        Parallel.ForEach(essays, essay =>
        {
            BlogEssay newEssay =
                essay.WithNewHtmlContent(Markdown.ToHtml(essay.HtmlContent, markdownPipeline));

            postProcessEssays.Add(newEssay);
            logger.LogDebug("Render markdown file {}.", newEssay);
        });

        await PostProcess(postProcessEssays);
        essayContentService.RefreshTags();

        _stopwatch.Stop();
        logger.LogInformation("Render finished, consuming {} s.",
            _stopwatch.Elapsed.ToString("s\\.fff"));
    }

    public void AddPreRenderProcessor(IPreRenderProcessor processor)
    {
        bool exist = _preRenderProcessors.Any(p => p.Name == processor.Name);

        if (exist)
        {
            throw new InvalidOperationException("There exists one pre-render processor " +
                                                $"with the same name: {processor.Name}.");
        }

        _preRenderProcessors.Add(processor);
    }

    public void AddPostRenderProcessor(IPostRenderProcessor processor)
    {
        bool exist = _postRenderProcessors.Any(p => p.Name == processor.Name);

        if (exist)
        {
            throw new InvalidCastException("There exists one post-render processor " +
                                           $"with the same name: {processor.Name}.");
        }

        _postRenderProcessors.Add(processor);
    }

    private async Task<IEnumerable<BlogContent>> PreProcess(IEnumerable<BlogContent> contents)
    {
        ConcurrentBag<BlogContent> processedContents = [];

        await Parallel.ForEachAsync(contents, async (content, _) =>
        {
            foreach (var processor in _preRenderProcessors)
            {
                content = await processor.ProcessAsync(content);
            }

            processedContents.Add(content);
        });

        return processedContents;
    }

    private async Task PostProcess(IEnumerable<BlogEssay> essays)
    {
        await Parallel.ForEachAsync(essays, async (essay, _) =>
        {
            foreach (IPostRenderProcessor processor in _postRenderProcessors)
            {
                essay = await processor.ProcessAsync(essay);
            }

            if (!essayContentService.TryAdd(essay))
            {
                throw new BlogFileException(
                    $"There are two essays with the same name: '{essay.FileName}'.");
            }
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

    [GeneratedRegex(@"(?<!\\)[^\#\*_\-\+\`{}\[\]!~]+")]
    private static partial Regex DescriptionPattern();

    private string GetDescription(BlogContent content)
    {
        const string delimiter = "<!--more-->";
        int pos = content.FileContent.IndexOf(delimiter, StringComparison.Ordinal);
        bool breakSentence = false;

        if (pos == -1)
        {
            // 自动截取前50个字符
            pos = content.FileContent.Length < 50 ? content.FileContent.Length : 50;
            breakSentence = true;
        }

        string rawContent = content.FileContent[..pos];
        MatchCollection matches = DescriptionPattern().Matches(rawContent);

        StringBuilder builder = new();
        foreach (Match match in matches)
        {
            builder.Append(match.Value);
        }

        if (breakSentence)
        {
            builder.Append("……");
        }

        string description = builder.ToString();

        logger.LogDebug("Description of {} is {}.", content.FileName,
            description);
        return description;
    }

    private uint GetWordCount(BlogContent content)
    {
        uint count = 0;

        foreach (char c in content.FileContent)
        {
            if (char.IsControl(c) || char.IsSymbol(c)
                                  || char.IsSeparator(c))
            {
                continue;
            }

            count++;
        }

        logger.LogDebug("Word count of {} is {}", content.FileName,
            count);
        return count;
    }

    private static string CalculateReadTime(uint wordCount)
    {
        // 据说语文教学大纲规定，中国高中问阅读现代文的速度是600字每分钟
        int second = (int)wordCount / 10;
        TimeSpan span = new TimeSpan(0, 0, second);

        return span.ToString("mm'分 'ss'秒'");
    }
}
