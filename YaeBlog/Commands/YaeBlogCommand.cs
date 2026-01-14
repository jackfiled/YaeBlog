using System.CommandLine;
using Microsoft.Extensions.Options;
using YaeBlog.Abstraction;
using YaeBlog.Commands.Binders;
using YaeBlog.Components;
using YaeBlog.Extensions;
using YaeBlog.Models;
using YaeBlog.Services;

namespace YaeBlog.Commands;

public sealed class YaeBlogCommand
{
    private readonly RootCommand _rootCommand = new("YaeBlog Cli");

    public YaeBlogCommand()
    {
        AddServeCommand(_rootCommand);
        AddWatchCommand(_rootCommand);
        AddListCommand(_rootCommand);
        AddNewCommand(_rootCommand);
        AddUpdateCommand(_rootCommand);
        AddPublishCommand(_rootCommand);
        AddScanCommand(_rootCommand);
        AddCompressCommand(_rootCommand);
    }

    public Task<int> RunAsync(string[] args)
    {
        return _rootCommand.InvokeAsync(args);
    }

    private static void AddServeCommand(RootCommand rootCommand)
    {
        Command serveCommand = new("serve", "Start http server.");
        rootCommand.AddCommand(serveCommand);

        serveCommand.SetHandler(async context =>
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddControllers();
            builder.AddYaeBlog();
            builder.AddServer();

            WebApplication application = builder.Build();

            application.MapStaticAssets();
            application.UseAntiforgery();
            application.UseYaeBlog();

            application.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            application.MapControllers();

            CancellationToken token = context.GetCancellationToken();
            await application.RunAsync(token);
        });
    }

    private static void AddWatchCommand(RootCommand rootCommand)
    {
        Command command = new("watch", "Start a blog watcher that re-render when file changes.");
        rootCommand.AddCommand(command);

        command.SetHandler(async context =>
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddControllers();
            builder.AddYaeBlog();
            builder.AddWatcher();

            WebApplication application = builder.Build();

            application.MapStaticAssets();
            application.UseAntiforgery();
            application.UseYaeBlog();

            application.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            application.MapControllers();

            CancellationToken token = context.GetCancellationToken();
            await application.RunAsync(token);
        });
    }

    private static void AddNewCommand(RootCommand rootCommand)
    {
        Command newCommand = new("new", "Create a new blog file and image directory.");
        rootCommand.AddCommand(newCommand);

        Argument<string> filenameArgument = new(name: "blog name", description: "The created blog filename.");
        newCommand.AddArgument(filenameArgument);

        newCommand.SetHandler(async (file, blogOption, _, essayScanService) =>
            {
                BlogContents contents = await essayScanService.ScanContents();

                if (contents.Posts.Any(content => content.BlogName == file))
                {
                    Console.WriteLine("There exists the same title blog in posts.");
                    return;
                }

                await essayScanService.SaveBlogContent(new BlogContent(
                    new FileInfo(Path.Combine(blogOption.Value.Root, "drafts", file + ".md")),
                    new MarkdownMetadata
                    {
                        Title = file,
                        Date = DateTimeOffset.Now.ToString("o"),
                        UpdateTime = DateTimeOffset.Now.ToString("o")
                    },
                    string.Empty, true, [], []));

                Console.WriteLine($"Created new blog '{file}.");
            }, filenameArgument, new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(),
            new EssayScanServiceBinder());
    }

    private static void AddUpdateCommand(RootCommand rootCommand)
    {
        Command newCommand = new("update", "Update the blog essay.");
        rootCommand.AddCommand(newCommand);

        Argument<string> filenameArgument = new(name: "blog name", description: "The blog filename to update.");
        newCommand.AddArgument(filenameArgument);

        newCommand.SetHandler(async (file, _, _, essayScanService) =>
            {
                Console.WriteLine("HINT: The update command only consider published blogs.");
                BlogContents contents = await essayScanService.ScanContents();

                BlogContent? content = contents.Posts.FirstOrDefault(c => c.BlogName == file);
                if (content is null)
                {
                    Console.WriteLine($"Target essay {file} is not exist.");
                    return;
                }

                content.Metadata.UpdateTime = DateTimeOffset.Now.ToString("o");
                await essayScanService.SaveBlogContent(content, content.IsDraft);
            }, filenameArgument,
            new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(), new EssayScanServiceBinder());
    }

    private static void AddListCommand(RootCommand rootCommand)
    {
        Command command = new("list", "List all blogs");
        rootCommand.AddCommand(command);

        command.SetHandler(async (_, _, essyScanService) =>
        {
            BlogContents contents = await essyScanService.ScanContents();

            Console.WriteLine($"All {contents.Posts.Count} Posts:");
            foreach (BlogContent content in contents.Posts.OrderBy(x => x.BlogName))
            {
                Console.WriteLine($" - {content.BlogName}");
            }

            Console.WriteLine($"All {contents.Drafts.Count} Drafts:");
            foreach (BlogContent content in contents.Drafts.OrderBy(x => x.BlogName))
            {
                Console.WriteLine($" - {content.BlogName}");
            }
        }, new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(), new EssayScanServiceBinder());
    }

    private static void AddScanCommand(RootCommand rootCommand)
    {
        Command command = new("scan", "Scan unused and not found images.");
        rootCommand.AddCommand(command);

        Option<bool> removeOption =
            new(name: "--rm", description: "Remove unused images.", getDefaultValue: () => false);
        command.AddOption(removeOption);

        command.SetHandler(async (_, _, essayScanService, removeOptionValue) =>
        {
            BlogContents contents = await essayScanService.ScanContents();
            List<BlogImageInfo> unusedImages = (from content in contents
                from image in content.Images
                where image is { IsUsed: false }
                select image).ToList();

            if (unusedImages.Count != 0)
            {
                Console.WriteLine("Found unused images:");
                Console.WriteLine("HINT: use '--rm' to remove unused images.");
            }

            foreach (BlogImageInfo image in unusedImages)
            {
                Console.WriteLine($" - {image.File.FullName}");
            }

            if (removeOptionValue)
            {
                foreach (BlogImageInfo image in unusedImages)
                {
                    image.File.Delete();
                }
            }

            Console.WriteLine("Used not existed images:");

            foreach (BlogContent content in contents)
            {
                foreach (FileInfo file in content.NotfoundImages)
                {
                    Console.WriteLine($"- {file.Name} in {content.BlogName}");
                }
            }
        }, new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(), new EssayScanServiceBinder(), removeOption);
    }

    private static void AddPublishCommand(RootCommand rootCommand)
    {
        Command command = new("publish", "Publish a new blog file.");
        rootCommand.AddCommand(command);

        Argument<string> filenameArgument = new(name: "blog name", description: "The published blog filename.");
        command.AddArgument(filenameArgument);

        command.SetHandler(async (blogOptions, _, essayScanService, filename) =>
            {
                BlogContents contents = await essayScanService.ScanContents();

                BlogContent? content = (from blog in contents.Drafts
                    where blog.BlogName == filename
                    select blog).FirstOrDefault();

                if (content is null)
                {
                    Console.WriteLine("Target blog does not exist.");
                    return;
                }

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
            }, new BlogOptionsBinder(),
            new LoggerBinder<EssayScanService>(), new EssayScanServiceBinder(), filenameArgument);
    }

    private static void AddCompressCommand(RootCommand rootCommand)
    {
        Command command = new("compress", "Compress png/jpeg image to webp image to reduce size.");
        rootCommand.Add(command);

        Option<bool> dryRunOption = new("--dry-run", description: "Dry run the compression task but not write.",
            getDefaultValue: () => false);
        command.AddOption(dryRunOption);

        command.SetHandler(ImageCommandHandler,
            new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(), new LoggerBinder<ImageCompressService>(),
            new EssayScanServiceBinder(), new ImageCompressServiceBinder(), dryRunOption);
    }

    private static async Task ImageCommandHandler(IOptions<BlogOptions> _, ILogger<EssayScanService> _1,
        ILogger<ImageCompressService> _2,
        IEssayScanService _3, ImageCompressService imageCompressService, bool dryRun)
    {
        await imageCompressService.Compress(dryRun);
    }
}
