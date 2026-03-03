namespace YaeBlog.Tests;

public class DateTimeOffsetTests
{
    [Fact]
    public void DateTimeOffsetParseTest()
    {
        const string input = "2026-01-04T16:36:36.5629759+08:00";
        DateTimeOffset time = DateTimeOffset.Parse(input);

        Assert.Equal("2026年01月04日 16:36:36", time.ToString("yyyy年MM月dd日 HH:mm:ss"));
    }
}
