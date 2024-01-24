using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Exceptions;
using YaeBlog.Core.Models;

namespace YaeBlog.Core.Services;

public class EssayScanService(
    IOptions<BlogOptions> blogOptions,
    ILogger<EssayContentService> logger)
{
    private readonly BlogOptions _blogOptions = blogOptions.Value;

    public async Task<List<BlogContent>> ScanAsync()
    {
        string root = Path.Combine(Environment.CurrentDirectory, _blogOptions.Root);
        DirectoryInfo rootDirectory = new(root);

        if (!rootDirectory.Exists)
        {
            throw new BlogFileException($"'{root}' is not a directory.");
        }

        List<FileInfo> markdownFiles = [];

        await Task.Run(() =>
        {
            foreach (FileInfo fileInfo in rootDirectory.EnumerateFiles())
            {
                if (fileInfo.Extension != ".md")
                {
                    continue;
                }

                logger.LogDebug("Scan markdown file: {}.", fileInfo.Name);
                markdownFiles.Add(fileInfo);
            }
        });

        ConcurrentBag<BlogContent> contents = [];

        await Parallel.ForEachAsync(markdownFiles, async (info, token) =>
        {
            StreamReader reader = new(info.OpenRead());

            BlogContent content = new()
            {
                FileName = info.Name.Split('.')[0], FileContent = await reader.ReadToEndAsync(token)
            };

            contents.Add(content);
        });

        return contents.ToList();
    }
}
