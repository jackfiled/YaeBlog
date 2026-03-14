namespace YaeBlog.Core.Exceptions;

public class BlogCommandException : Exception
{
    public BlogCommandException(string message) : base(message)
    {
    }

    public BlogCommandException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
