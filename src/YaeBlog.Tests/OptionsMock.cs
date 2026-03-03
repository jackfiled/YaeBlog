using Microsoft.Extensions.Options;
using Moq;
using YaeBlog.Models;

namespace YaeBlog.Tests;

public static class OptionsMock
{
    public static Mock<IOptions<BlogOptions>> CreateBlogOptionMock()
    {
        Mock<IOptions<BlogOptions>> mock = new();

        mock.SetupGet(o => o.Value)
            .Returns(new BlogOptions { Root = "source", Announcement = string.Empty, Links = [], StartYear = 2021 });

        return mock;
    }
}
