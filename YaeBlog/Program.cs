using System.CommandLine;
using YaeBlog.Commands;

RootCommand rootCommand = new("YaeBlog CLI");

rootCommand.AddServeCommand();
rootCommand.AddNewCommand();

await rootCommand.InvokeAsync(args);
