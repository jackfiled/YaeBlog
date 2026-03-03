namespace YaeBlog.Core.Exceptions;

public class GiteaFetchException : Exception
{
    public GiteaFetchException() : base()
    {
    }

    public GiteaFetchException(string message) : base(message)
    {
    }

    public GiteaFetchException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
