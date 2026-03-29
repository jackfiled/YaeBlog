using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using Microsoft.Extensions.Options;
using YaeBlog.Abstractions;
using YaeBlog.Core.Exceptions;
using YaeBlog.Abstractions.Models;

namespace YaeBlog.Services;

public sealed class YaeCommandService(
    string[] arguments,
    IEssayScanService essayScanService,
    ImageCompressService imageCompressService,
    ConsoleInfoService consoleInfoService,
    IHostApplicationLifetime hostApplicationLifetime,
    IOptions<BlogOptions> blogOptions,
    ILogger<YaeCommandService> logger)
    : BackgroundService
{
    private readonly BlogOptions _blogOptions = blogOptions.Value;
    private bool _oneShotCommandFlag = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RootCommand rootCommand = new("YaeBlog CLI");

        RegisterServeCommand(rootCommand);
        RegisterWatchCommand(rootCommand);

        RegisterNewCommand(rootCommand);
        RegisterUpdateCommand(rootCommand);
        RegisterScanCommand(rootCommand);
        RegisterPublishCommand(rootCommand);
        RegisterCompressCommand(rootCommand);

        // Shit code: wait for the application starting.
        // If the command service finished early before the application starting, there will be an ugly exception.
        await Task.Delay(500, stoppingToken);
        logger.LogInformation("Running YaeBlog Command.");
        int exitCode = await rootCommand.InvokeAsync(arguments);

        if (exitCode != 0)
        {
            throw new BlogCommandException($"YaeBlog command exited with no-zero code {exitCode}");
        }

        consoleInfoService.IsOneShotCommand = _oneShotCommandFlag;

        if (!consoleInfoService.IsOneShotCommand)
        {
            logger.LogInformation("Start YaeBlog command: {}", consoleInfoService.Command);
        }
        hostApplicationLifetime.StopApplication();
    }

    private void RegisterServeCommand(RootCommand rootCommand)
    {
        Command command = new("serve", "Start http server.");
        rootCommand.AddCommand(command);

        command.SetHandler(HandleServeCommand);

        // When invoking the root command without sub command, fallback to serve command.
        rootCommand.SetHandler(HandleServeCommand);
    }

    private Task HandleServeCommand(InvocationContext context)
    {
        _oneShotCommandFlag = false;
        consoleInfoService.Command = ServerCommand.Serve;

        return Task.CompletedTask;
    }

    private void RegisterWatchCommand(RootCommand rootCommand)
    {
        Command command = new("watch", "Start a blog watcher that re-render when file changes.");
        rootCommand.AddCommand(command);

        command.SetHandler(_ =>
        {
            _oneShotCommandFlag = false;
            consoleInfoService.Command = ServerCommand.Watch;
        });
    }

    private void RegisterNewCommand(RootCommand rootCommand)
    {
        Command command = new("new", "Create a new blog file and image directory.");
        rootCommand.AddCommand(command);

        Argument<string> filenameArgument = new(name: "blog name", description: "The created blog filename.");
        command.AddArgument(filenameArgument);

        command.SetHandler(HandleNewCommand, filenameArgument);
    }

    private async Task HandleNewCommand(string filename)
    {
        BlogContents contents = await essayScanService.ScanContents();

        if (contents.Posts.Any(content => content.BlogName == filename))
        {
            throw new BlogCommandException("There exits the same title blog in posts.");
        }

        await essayScanService.SaveBlogContent(new BlogContent(
            new FileInfo(Path.Combine(_blogOptions.Root, "drafts", filename + ".md")),
            new MarkdownMetadata
            {
                Title = filename,
                Date = DateTimeOffset.Now.ToString("o"),
                UpdateTime = DateTimeOffset.Now.ToString("o")
            },
            string.Empty, true, [], []
        ));

        logger.LogInformation("Create new blog '{}'", filename);
    }

    private void RegisterUpdateCommand(RootCommand rootCommand)
    {
        Command command = new("update", "Update the blog essay.");
        rootCommand.AddCommand(command);

        Argument<string> filenameArgument = new(name: "blog name", description: "The blog filename to update.");
        command.AddArgument(filenameArgument);

        command.SetHandler(HandleUpdateCommand, filenameArgument);
    }

    private async Task HandleUpdateCommand(string filename)
    {
        logger.LogInformation("The update command only considers published blogs.");
        BlogContents contents = await essayScanService.ScanContents();

        BlogContent? content = contents.Posts.FirstOrDefault(c => c.BlogName == filename);
        if (content is null)
        {
            throw new BlogCommandException($"Target essay {filename} is not exist.");
        }

        content.Metadata.UpdateTime = DateTimeOffset.Now.ToString("o");
        await essayScanService.SaveBlogContent(content, content.IsDraft);
        logger.LogInformation("Update time of essay '{}' updated.", content.BlogName);
    }

    private void RegisterScanCommand(RootCommand rootCommand)
    {
        Command command = new("scan", "Scan unused and not found images.");
        rootCommand.AddCommand(command);

        Option<bool> removeOption =
            new(name: "--rm", description: "Remove unused images.", getDefaultValue: () => false);
        command.AddOption(removeOption);

        command.SetHandler(HandleScanCommand, removeOption);
    }

    private async Task HandleScanCommand(bool removeUnusedImages)
    {
        BlogContents contents = await essayScanService.ScanContents();
        List<BlogImageInfo> unusedImages = (from content in contents
            from image in content.Images
            where image is { IsUsed: false }
            select image).ToList();

        if (unusedImages.Count != 0)
        {
            StringBuilder builder = new();
            builder.Append("Found unused images:").Append('\n');

            foreach (BlogImageInfo image in unusedImages)
            {
                builder.Append('\t').Append("- ").Append(image.File.FullName).Append('\n');
            }

            logger.LogInformation("{}", builder.ToString());
            logger.LogInformation("HINT: use '--rm' to remove unused images.");
        }

        if (removeUnusedImages)
        {
            foreach (BlogImageInfo image in unusedImages)
            {
                image.File.Delete();
            }
        }

        StringBuilder infoBuilder = new();
        infoBuilder.Append("Used not existed images:\n");

        bool flag = false;
        foreach (BlogContent content in contents)
        {
            foreach (FileInfo file in content.NotfoundImages)
            {
                flag = true;
                infoBuilder.Append('\t').Append("- ").Append(file.Name).Append(" in ").Append(content.BlogName)
                    .Append('\n');
            }
        }

        if (flag)
        {
            logger.LogInformation("{}", infoBuilder.ToString());
        }
    }

    private void RegisterPublishCommand(RootCommand rootCommand)
    {
        Command command = new("publish", "Publish a new blog file.");
        rootCommand.AddCommand(command);

        Argument<string> filenameArgument = new(name: "blog name", description: "The published blog filename.");
        command.AddArgument(filenameArgument);

        command.SetHandler(HandlePublishCommand, filenameArgument);
    }

    private async Task HandlePublishCommand(string filename)
    {
        BlogContents contents = await essayScanService.ScanContents();

        BlogContent? content = (from blog in contents.Drafts
            where blog.BlogName == filename
            select blog).FirstOrDefault();

        if (content is null)
        {
            throw new BlogCommandException("Target blog doest not exist.");
        }

        logger.LogInformation("Publish blog {}", content.BlogName);

        // 设置发布的时间
        content.Metadata.Date = DateTimeOffset.Now.ToString("o");
        content.Metadata.UpdateTime = DateTimeOffset.Now.ToString("o");

        // 将选中的博客文件复制到posts
        await essayScanService.SaveBlogContent(content, isDraft: false);

        // 复制图片文件夹
        DirectoryInfo sourceImageDirectory =
            new(Path.Combine(blogOptions.Value.Root, "drafts", content.BlogName));
        DirectoryInfo targetImageDirectory =
            new(Path.Combine(blogOptions.Value.Root, "posts", content.BlogName));

        if (sourceImageDirectory.Exists)
        {
            targetImageDirectory.Create();
            foreach (FileInfo file in sourceImageDirectory.EnumerateFiles())
            {
                file.CopyTo(Path.Combine(targetImageDirectory.FullName, file.Name), true);
            }

            sourceImageDirectory.Delete(true);
        }

        // 删除原始的文件
        FileInfo sourceBlogFile = new(Path.Combine(blogOptions.Value.Root, "drafts", content.BlogName + ".md"));
        sourceBlogFile.Delete();
    }

    private void RegisterCompressCommand(RootCommand rootCommand)
    {
        Command command = new("compress", "Compress png/jpeg image to webp image to reduce size.");
        rootCommand.Add(command);

        Option<bool> dryRunOption = new("--dry-run", description: "Dry run the compression task but not write.",
            getDefaultValue: () => false);
        command.AddOption(dryRunOption);

        command.SetHandler(async dryRun => { await imageCompressService.Compress(dryRun); }, dryRunOption);
    }
}
