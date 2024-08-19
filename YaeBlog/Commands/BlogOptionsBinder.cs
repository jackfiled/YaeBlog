using System.CommandLine.Binding;
using System.Text.Json;
using YaeBlog.Core.Models;

namespace YaeBlog.Commands;

public sealed class BlogOptionsBinder : BinderBase<BlogOptions>
{
    protected override BlogOptions GetBoundValue(BindingContext bindingContext)
    {
        FileInfo settings = new(Path.Combine(Environment.CurrentDirectory, "appsettings.json"));
        if (!settings.Exists)
        {
            throw new InvalidOperationException("Failed to load YaeBlog configurations.");
        }

        using StreamReader reader = settings.OpenText();
        using JsonDocument document = JsonDocument.Parse(reader.ReadToEnd());
        JsonElement root = document.RootElement;
        JsonElement optionSection = root.GetProperty(BlogOptions.OptionName);

        BlogOptions? result = optionSection.Deserialize<BlogOptions>();
        if (result is null)
        {
            throw new InvalidOperationException("Failed to load YaeBlog configuration in appsettings.json.");
        }

        return result;
    }
}
