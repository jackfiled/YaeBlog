using System.Text.Json;
using Xunit.Abstractions;

namespace YaeBlog.Tests;

public sealed class JsonDeserializationTests(ITestOutputHelper outputHelper)
{
    private record JsonBody(int Code, string Username);

    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    [Fact]
    public void DeserializeTest()
    {
        const string input = """
                             {
                                "code": 111,
                                "username": "ricardo"
                             }
                             """;

        JsonBody? body = JsonSerializer.Deserialize<JsonBody>(input, s_serializerOptions);
        Assert.NotNull(body);

        Assert.Equal(111, body.Code);
        Assert.Equal("ricardo", body.Username);
    }

    [Fact]
    public void DeserializeFromNonexistFieldTest()
    {
        const string input = """
                             {
                                "code": 111
                             }
                             """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonBody>(input, s_serializerOptions));
    }

    [Fact]
    public void DeserializeFromNullValueTest()
    {
        const string input = """
                             {
                                "code": 111,
                                "username": null
                             }
                             """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonBody>(input, s_serializerOptions));
    }

    [Fact]
    public void DeserializeFromUndefinedValueTest()
    {
        const string input = """
                             {
                                "code": 111,
                                "username": undefined
                             }
                             """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonBody>(input, s_serializerOptions));
    }

    private record JsonListBody(List<string> Names);

    [Fact]
    public void DeserializeListTest()
    {
        const string input = """
                             {
                                "names": [
                                    "1",
                                    null,
                                    "2"
                                ]
                             }
                             """;

        JsonListBody? body = JsonSerializer.Deserialize<JsonListBody>(input, s_serializerOptions);
        Assert.NotNull(body);

        foreach ((int i, string value) in body.Names.Index())
        {
            outputHelper.WriteLine($"{i} is null? {value is null}");
        }
    }

    private struct JsonStruct
    {
        public int Id { get; }

        public string Name { get; }

        public JsonStruct(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    [Fact]
    public void DeserializeToStructTest()
    {
        const string input = """
                             {
                                "Id": 1,
                                "Name": "ricardo"
                             }
                             """;

        JsonStruct r = JsonSerializer.Deserialize<JsonStruct>(input, s_serializerOptions);
        Assert.Equal(0, r.Id);
        Assert.Null(r.Name);
    }

    private readonly struct JsonReadonlyStruct
    {
        public int Id { get; }

        public string Name { get; }

        public JsonReadonlyStruct(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    [Fact]
    public void DeserializeToReadonlyStructTest()
    {
        const string input = """
                             {
                                "Id": 1,
                                "Name": "ricardo"
                             }
                             """;

        JsonReadonlyStruct r = JsonSerializer.Deserialize<JsonReadonlyStruct>(input, s_serializerOptions);
        Assert.Equal(0, r.Id);
        Assert.Null(r.Name);
    }

    private record struct JsonRecordStruct(int Id, string Name);

    [Fact]
    public void DeserializeToRecordStructTest()
    {
        const string input = """
                             {
                                "Id": 1,
                                "Name": "ricardo"
                             }
                             """;

        JsonRecordStruct r = JsonSerializer.Deserialize<JsonRecordStruct>(input, s_serializerOptions);
        Assert.Equal(1, r.Id);
        Assert.Equal("ricardo", r.Name);
    }

    private readonly record struct JsonReadonlyRecordStruct(int Id, string Name);

    [Fact]
    public void DeserializeToReadonlyRecordStructTest()
    {
        const string input = """
                             {
                                "Id": 1,
                                "Name": "ricardo"
                             }
                             """;

        JsonReadonlyRecordStruct r = JsonSerializer.Deserialize<JsonReadonlyRecordStruct>(input, s_serializerOptions);
        Assert.Equal(1, r.Id);
        Assert.Equal("ricardo", r.Name);
    }
}
