using System.Text.Json;
using Xunit.Abstractions;

namespace YaeBlog.Tests;

public sealed class JsonSerializationTests(ITestOutputHelper outputHelper)
{
    private record JsonBody(long Number);

    [Fact]
    public void LongSerializeTest()
    {
        JsonBody body = new(long.MaxValue - 1);
        string output = JsonSerializer.Serialize(body);
        outputHelper.WriteLine(output);
    }
}
