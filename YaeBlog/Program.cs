using System.CommandLine;
using YaeBlog.Commands;

RootCommand rootCommand = new("YaeBlog CLI");

rootCommand.AddServeCommand();
rootCommand.AddNewCommand();
rootCommand.AddListCommand();
rootCommand.AddWatchCommand();
rootCommand.AddScanCommand();
rootCommand.AddPublishCommand();

await rootCommand.InvokeAsync(args);
