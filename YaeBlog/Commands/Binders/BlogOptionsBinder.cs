using System.CommandLine.Binding;
using System.Text.Json;
using Microsoft.Extensions.Options;
using YaeBlog.Core.Models;

namespace YaeBlog.Commands.Binders;

public sealed class BlogOptionsBinder : BinderBase<IOptions<BlogOptions>>
{
    protected override IOptions<BlogOptions> GetBoundValue(BindingContext bindingContext)
    {
        bindingContext.AddService<IOptions<BlogOptions>>(_ =>
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

            return new OptionsWrapper<BlogOptions>(result);
        });

        return bindingContext.GetRequiredService<IOptions<BlogOptions>>();
    }
}
