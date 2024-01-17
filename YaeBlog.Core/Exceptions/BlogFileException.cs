namespace YaeBlog.Core.Exceptions;

public class BlogFileException : Exception
{
    public BlogFileException() : base()
    {

    }

    public BlogFileException(string message) : base(message)
    {

    }

    public BlogFileException(string message, Exception innerException) : base(message, innerException)
    {

    }
}
