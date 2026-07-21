using YaeBlog.Typst;

namespace YaeBlog.Tests;

public class CallRustTests
{
    [Fact]
    public void ProcessStringTest()
    {
        RustString test1 = RustCaller.ProcessString("123");
        Assert.NotNull(test1.Value);
        Assert.Equal("Process 123", test1.Value);

        RustString test2 = RustCaller.ProcessString("世界");
        Assert.NotNull(test2.Value);
        Assert.Equal("Process 世界", test2.Value);
    }
}
