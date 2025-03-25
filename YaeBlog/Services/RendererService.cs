using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using YaeBlog.Abstraction;
using YaeBlog.Core.Exceptions;
using YaeBlog.Models;

namespace YaeBlog.Services;

public partial class RendererService(
    ILogger<RendererService> logger,
    IEssayScanService essayScanService,
    MarkdownPipeline markdownPipeline,
    IEssayContentService essayContentService)
{
    private readonly Stopwatch _stopwatch = new();

    private readonly List<IPreRenderProcessor> _preRenderProcessors = [];

    private readonly List<IPostRenderProcessor> _postRenderProcessors = [];

    public async Task RenderAsync(bool includeDrafts = false)
    {
        _stopwatch.Start();
        logger.LogInformation("Render essays start.");

        BlogContents contents = await essayScanService.ScanContents();
        List<BlogContent> posts = contents.Posts.ToList();
        if (includeDrafts)
        {
            posts.AddRange(contents.Drafts);
        }

        IEnumerable<BlogContent> preProcessedContents = await PreProcess(posts);

        List<BlogEssay> essays = [];
        foreach (BlogContent content in preProcessedContents)
        {
            uint wordCount = GetWordCount(content);
            BlogEssay essay = new()
            {
                Title = content.Metadata.Title ?? content.BlogName,
                FileName = content.BlogName,
                IsDraft = content.IsDraft,
                Description = GetDescription(content),
                WordCount = wordCount,
                ReadTime = CalculateReadTime(wordCount),
                PublishTime = content.Metadata.Date ?? DateTime.Now,
                HtmlContent = content.Content
            };

            if (content.Metadata.Tags is not null)
            {
                essay.Tags.AddRange(content.Metadata.Tags);
            }

            essays.Add(essay);
        }

        ConcurrentBag<BlogEssay> postProcessEssays = [];
        Parallel.ForEach(essays, essay =>
        {
            BlogEssay newEssay =
                essay.WithNewHtmlContent(Markdown.ToHtml(essay.HtmlContent, markdownPipeline));

            postProcessEssays.Add(newEssay);
            logger.LogDebug("Render markdown file {}.", newEssay);
        });

        IEnumerable<BlogEssay> postProcessedEssays = await PostProcess(postProcessEssays);

        foreach (BlogEssay essay in postProcessedEssays)
        {
            if (!essayContentService.TryAdd(essay))
            {
                throw new BlogFileException($"There are at least two essays with filename '{essay.FileName}'.");
            }
        }

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

    private async Task<IEnumerable<BlogEssay>> PostProcess(IEnumerable<BlogEssay> essays)
    {
        ConcurrentBag<BlogEssay> processedContents = [];

        await Parallel.ForEachAsync(essays, async (essay, _) =>
        {
            foreach (IPostRenderProcessor processor in _postRenderProcessors)
            {
                essay = await processor.ProcessAsync(essay);
            }

            processedContents.Add(essay);
        });

        List<BlogEssay> result = processedContents.ToList();
        result.Sort();

        return result;
    }

    [GeneratedRegex(@"(?<!\\)[^\#\*_\-\+\`{}\[\]!~]+")]
    // private static partial Regex DescriptionPattern();
    private static partial Regex DescriptionPattern { get; }

    private string GetDescription(BlogContent content)
    {
        const string delimiter = "<!--more-->";
        int pos = content.Content.IndexOf(delimiter, StringComparison.Ordinal);
        bool breakSentence = false;

        if (pos == -1)
        {
            // 自动截取前50个字符
            pos = content.Content.Length < 50 ? content.Content.Length : 50;
            breakSentence = true;
        }

        string rawContent = content.Content[..pos];
        MatchCollection matches = DescriptionPattern.Matches(rawContent);

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

        logger.LogDebug("Description of {} is {}.", content.BlogName,
            description);
        return description;
    }

    private uint GetWordCount(BlogContent content)
    {
        int count = (from c in content.Content
            where char.IsLetterOrDigit(c)
            select c).Count();

        logger.LogDebug("Word count of {} is {}", content.BlogName,
            count);
        return (uint)count;
    }

    private static string CalculateReadTime(uint wordCount)
    {
        // 据说语文教学大纲规定，中国高中生阅读现代文的速度是600字每分钟
        int second = (int)wordCount / 10;
        TimeSpan span = new(0, 0, second);

        return span.ToString("mm'分 'ss'秒'");
    }
}
