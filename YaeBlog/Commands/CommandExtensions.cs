using System.CommandLine;
using YaeBlog.Commands.Binders;
using YaeBlog.Components;
using YaeBlog.Core.Extensions;
using YaeBlog.Core.Models;
using YaeBlog.Core.Services;

namespace YaeBlog.Commands;

public static class CommandExtensions
{
    public static void AddServeCommand(this RootCommand rootCommand)
    {
        Command serveCommand = new("serve", "Start http server.");
        rootCommand.AddCommand(serveCommand);

        serveCommand.SetHandler(async context =>
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddControllers();
            builder.Services.AddBlazorBootstrap();
            builder.AddYaeBlog();
            builder.AddServer();

            WebApplication application = builder.Build();

            application.UseStaticFiles();
            application.UseAntiforgery();
            application.UseYaeBlog();

            application.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            application.MapControllers();

            CancellationToken token = context.GetCancellationToken();
            await application.RunAsync(token);
        });
    }

    public static void AddWatchCommand(this RootCommand rootCommand)
    {
        Command command = new("watch", "Start a blog watcher that re-render when file changes.");
        rootCommand.AddCommand(command);

        command.SetHandler(async context =>
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddControllers();
            builder.Services.AddBlazorBootstrap();
            builder.AddYaeBlog();
            builder.AddWatcher();

            WebApplication application = builder.Build();

            application.UseStaticFiles();
            application.UseAntiforgery();
            application.UseYaeBlog();

            application.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            application.MapControllers();

            CancellationToken token = context.GetCancellationToken();
            await application.RunAsync(token);
        });
    }

    public static void AddNewCommand(this RootCommand rootCommand)
    {
        Command newCommand = new("new", "Create a new blog file and image directory.");
        rootCommand.AddCommand(newCommand);

        Argument<string> filenameArgument = new(name: "blog name", description: "The created blog filename.");
        newCommand.AddArgument(filenameArgument);

        newCommand.SetHandler(async (file, _, _, essayScanService) =>
            {
                BlogContents contents = await essayScanService.ScanContents();

                if (contents.Posts.Any(content => content.FileName == file))
                {
                    Console.WriteLine("There exists the same title blog in posts.");
                    return;
                }

                await essayScanService.SaveBlogContent(new BlogContent
                {
                    FileName = file,
                    FileContent = string.Empty,
                    Metadata = new MarkdownMetadata { Title = file, Date = DateTime.Now }
                });

                Console.WriteLine($"Created new blog '{file}.");
            }, filenameArgument, new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(),
            new EssayScanServiceBinder());
    }

    public static void AddListCommand(this RootCommand rootCommand)
    {
        Command command = new("list", "List all blogs");
        rootCommand.AddCommand(command);

        command.SetHandler(async (_, _, essyScanService) =>
        {
            BlogContents contents = await essyScanService.ScanContents();

            Console.WriteLine($"All {contents.Posts.Count} Posts:");
            foreach (BlogContent content in contents.Posts.OrderBy(x => x.FileName))
            {
                Console.WriteLine($" - {content.FileName}");
            }

            Console.WriteLine($"All {contents.Drafts.Count} Drafts:");
            foreach (BlogContent content in contents.Drafts.OrderBy(x => x.FileName))
            {
                Console.WriteLine($" - {content.FileName}");
            }
        }, new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(), new EssayScanServiceBinder());
    }

    public static void AddScanCommand(this RootCommand rootCommand)
    {
        Command command = new("scan", "Scan unused and not found images.");
        rootCommand.AddCommand(command);

        Option<bool> removeOption =
            new(name: "--rm", description: "Remove unused images.", getDefaultValue: () => false);
        command.AddOption(removeOption);

        command.SetHandler(async (_, _, essayScanService, removeOptionValue) =>
        {
            ImageScanResult result = await essayScanService.ScanImages();

            if (result.UnusedImages.Count != 0)
            {
                Console.WriteLine("Found unused images:");
                Console.WriteLine("HINT: use '--rm' to remove unused images.");
            }

            foreach (FileInfo image in result.UnusedImages)
            {
                Console.WriteLine($" - {image.FullName}");
            }

            if (removeOptionValue)
            {
                foreach (FileInfo image in result.UnusedImages)
                {
                    image.Delete();
                }
            }

            Console.WriteLine("Used not existed images:");

            foreach (FileInfo image in result.NotFoundImages)
            {
                Console.WriteLine($" - {image.FullName}");
            }
        }, new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(), new EssayScanServiceBinder(), removeOption);
    }

    public static void AddPublishCommand(this RootCommand rootCommand)
    {
        Command command = new("publish", "Publish a new blog file.");
        rootCommand.AddCommand(command);

        Argument<string> filenameArgument = new(name: "blog name", description: "The published blog filename.");
        command.AddArgument(filenameArgument);

        command.SetHandler(async (blogOptions, _, essayScanService, filename) =>
            {
                BlogContents contents = await essayScanService.ScanContents();

                BlogContent? content = (from blog in contents.Drafts
                    where blog.FileName == filename
                    select blog).FirstOrDefault();

                if (content is null)
                {
                    Console.WriteLine("Target blog does not exist.");
                    return;
                }

                // 将选中的博客文件复制到posts
                await essayScanService.SaveBlogContent(content, isDraft: false);

                // 复制图片文件夹
                DirectoryInfo sourceImageDirectory =
                    new(Path.Combine(blogOptions.Value.Root, "drafts", content.FileName));
                DirectoryInfo targetImageDirectory =
                    new(Path.Combine(blogOptions.Value.Root, "posts", content.FileName));

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
                FileInfo sourceBlogFile = new(Path.Combine(blogOptions.Value.Root, "drafts", content.FileName + ".md"));
                sourceBlogFile.Delete();
            }, new BlogOptionsBinder(),
            new LoggerBinder<EssayScanService>(), new EssayScanServiceBinder(), filenameArgument);
    }
}
