using System.CommandLine;
using YaeBlog.Components;
using YaeBlog.Core.Extensions;

namespace YaeBlog.Commands;

public static class CommandExtensions
{
    public static Command AddServeCommand(this RootCommand rootCommand)
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

        return rootCommand;
    }

    public static Command AddNewCommand(this RootCommand rootCommand)
    {
        Command newCommand = new("new", "Create a new blog file and image directory.");
        rootCommand.AddCommand(newCommand);

        Argument<string> filenameArgument = new(name: "blog name", description: "The created blog filename.");
        newCommand.AddArgument(filenameArgument);

        newCommand.SetHandler(async (file, blogOptions) =>
        {
            string fileWithExtension;
            if (file.EndsWith(".md"))
            {
                fileWithExtension = file;
                file = fileWithExtension[..fileWithExtension.LastIndexOf('.')];
            }
            else
            {
                fileWithExtension = file + ".md";
            }

            DirectoryInfo rootDir = new(Path.Combine(Environment.CurrentDirectory, blogOptions.Root));
            if (!rootDir.Exists)
            {
                throw new InvalidOperationException($"Blog source directory '{blogOptions.Root} doesn't exist.");
            }

            if (rootDir.EnumerateFiles().Any(f => f.Name == fileWithExtension))
            {
                throw new InvalidOperationException($"Target blog '{file}' has been created!");
            }

            FileInfo newBlogFile = new(Path.Combine(rootDir.FullName, fileWithExtension));
            await using StreamWriter newStream = newBlogFile.CreateText();

            await newStream.WriteAsync($"""
                                        ---
                                        title: {file}
                                        tags:
                                        ---
                                        <!--more-->
                                        """);

            Console.WriteLine($"Created new blog '{file}.");
        }, filenameArgument, new BlogOptionsBinder());


        return newCommand;
    }
}
