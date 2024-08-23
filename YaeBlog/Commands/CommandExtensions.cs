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
        rootCommand.Add(command);

        command.SetHandler(async (_, _, essyScanService) =>
        {
            BlogContents contents = await essyScanService.ScanContents();

            Console.WriteLine($"All {contents.Posts.Count} Posts:");
            foreach (BlogContent content in contents.Posts)
            {
                Console.WriteLine($" - {content.FileName}");
            }

            Console.WriteLine($"All {contents.Drafts.Count} Drafts:");
            foreach (BlogContent content in contents.Drafts)
            {
                Console.WriteLine($" - {content.FileName}");
            }
        }, new BlogOptionsBinder(), new LoggerBinder<EssayScanService>(), new EssayScanServiceBinder());
    }
}
