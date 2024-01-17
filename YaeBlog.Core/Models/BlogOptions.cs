using Microsoft.Extensions.Options;

namespace YaeBlog.Core.Models;

public class BlogOptions
{
    public const string OptionName = "Blog";

    public required string Root { get; set; }
}
