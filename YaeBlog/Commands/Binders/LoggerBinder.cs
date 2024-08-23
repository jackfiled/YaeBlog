using System.CommandLine.Binding;

namespace YaeBlog.Commands.Binders;

public sealed class LoggerBinder<T> : BinderBase<ILogger<T>>
{
    protected override ILogger<T> GetBoundValue(BindingContext bindingContext)
    {
        bindingContext.AddService(_ => LoggerFactory.Create(builder => builder.AddConsole()));
        bindingContext.AddService<ILogger<T>>(provider =>
        {
            ILoggerFactory factory = provider.GetRequiredService<ILoggerFactory>();
            return factory.CreateLogger<T>();
        });

        return bindingContext.GetRequiredService<ILogger<T>>();
    }
}
