using System.ComponentModel.DataAnnotations;

namespace YaeBlog.Models;

public class GiteaOptions
{
    public const string OptionName = "Gitea";

    [Required] public required string BaseAddress { get; init; }

    [Required] public required string ApiKey { get; init; }

    [Required] public required string HeatMapUsername { get; init; }
}
