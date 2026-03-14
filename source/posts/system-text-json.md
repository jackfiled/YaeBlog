---
title: 使用System.Text.Json序列化和反序列化JSON
date: 2026-01-21T22:07:38.4297603+08:00
updateTime: 2026-01-21T22:07:38.4370636+08:00
tags:
- 技术笔记
- dotnet
---



如何使用`System.Text.Json`高效地序列化和反序列化JSON？

<!--more-->

### 序列化

序列化JSON几乎总是简单的，直接使用`JsonSerializer.Serialize`就可以序列化为字符串。

唯一需要注意的是，JSON理论上唯一的数字类型`number`默认是双精度浮点数，只能**精确地**表示53位（二进制）以下的整数。在对于`long`类型进行序列化时，虽然框架可以输出正确的数值，但是JavaScript中无法正确的解析。

```csharp
    [Fact]
    public void LongSerializeTest()
    {
        JsonBody body = new(long.MaxValue - 1);
        string output = JsonSerializer.Serialize(body);
        // Output: {"Number":9223372036854775806}
        outputHelper.WriteLine(output);
    }
```

上述的JSON字符串中在JavaScript中将会被解析为：

![image-20260120153508775](./system-text-json/image-20260120153508775.webp)

因此在需要传递大整数的时候最好使用`String`。

### 反序列化

而反序列化中需要考虑的东西就很多了。

#### 使用记录声明反序列化的对象

在`System.Text.Json`的早期版本中，无法将JSON反序列化为`record`这类关键词声明的不可变类型，因为当时库的逻辑是首先调用类型的公共无参数构造函数构造对象，再使用setter为需要反序列化的属性赋值。在后来的版本中，序列化程序可以直接调用类型的构造函数进行反序列化，这就为反序列化到`record`和`struct`提供了方便。

例如可以使用如下的代码快速地进行反序列化：

```csharp
    private record JsonBody(int Code, string Username);

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
```

但是这样进行反序列化有一个小小的坑，就是缺少对于空值的有效处理。例如对于下面的JSON，上面的代码都会正常地进行反序列化。

```csharp
    [Fact]
    public void DeserializeFromNonexistFieldTest()
    {
        const string input = """
                             {
                                "code": 111
                             }
                             """;

        JsonBody? body = JsonSerializer.Deserialize<JsonBody>(input, s_serializerOptions);
        Assert.NotNull(body);

        Assert.Equal(111, body.Code);
        Assert.Equal("", body.Username);
    }
```

```csharp
    [Fact]
    public void DeserializeFromNullValueTest()
    {
        const string input = """
                             {
                                "code": 111,
                                "username": null
                             }
                             """;

        JsonBody? body = JsonSerializer.Deserialize<JsonBody>(input, s_serializerOptions);
        Assert.NotNull(body);

        Assert.Equal(111, body.Code);
        Assert.Equal("", body.Username);
    }
```

但是对于返回结果的校验会发现`body.Username`实际上是一个空值。

![image-20260121221219618](./system-text-json/image-20260121221219618.webp)

幸好，在.NET 9中为`JsonSerializerOptions`添加了一个尊重可为空注释的选项`RespectNullableAnnotations`，将这个选项设置为`true`可以在**一定程度上**缓解这个问题。打开这个开关之后，对于`"username": null`的反序列化就会抛出异常了。

但是针对第一段JSON，也就是缺少了`username`字段的反序列化并不会报错，这就是反序列化的第二个坑，这里先按下不表。

因为在.NET运行时的设计初期并没有考虑空安全这一至关重要的特性，因此在IL中并没有针对引用类型的不可为空性的显式抽象（虽然后续的C#编译器会为所有不可为空的应用类型添加属性元数据）。所以，针对如下元素的不可为空约束是无效的：

1. 顶级类型；
2. 集合的元素类型；
3. 任何含有泛型的属性、字段和构造函数参数。

例如，针对下面这个反序列化代码并不会报错，需要程序员自行处理其中的空值：

```csharp
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
```

运行的输出结果提示第二个元素为空：

![image-20260120172747047](./system-text-json/image-20260120172747047.webp)

#### 需要才是需要，不为空并不一定不为空

在默认的反序列化行为中，如果反序列化对象的某一个属性并不在输入的JSON对象中，反序列化器并不为报错而是直接设置为null，这显然会给破环空安全的假定，即使打开了尊重空值注释也是这样。这在.NET文档中被称为**缺失值和空值**：

- **显式空值null**将会在`RespectNullableAnnontations=true`的情况下引发异常；
- **缺少的属性**不会引发任何异常，即使对应的属性被声明为不可为空。

为了让序列化程序确保缺少属性时会报错，需要将这个属性声明为**需要的**。这一点可以通过C#的`required`关键词或者`[Required]`属性来实现。

而且，这两种属性对于C#语言和序列化程序来说是正交的，即：

1. 可以有一个可以为空的必需属性：

   ```csharp
   MyPoco poco = new() { Value = null }; // No compiler warnings.
   
   class MyPoco
   {
       public required string? Value { get; set; }
   }
   ```

2. 可以有一个不可为空的可选属性：

   ```csharp
   class MyPoco
   {
       public string Value { get; set; } = "default";
   }
   ```

但是对于`record`类型来说，前者在语义上是冗余的，语法上是错误的，后者则对于程序员带来了额外的心智负担，需要手动给每一个字段加上一个额外的注解。

考虑到序列化程序也支持使用有参数的公共构造函数，上面这两个属性对于构造函数的参数来说也是成立的：

```csharp
record MyPoco(
    string RequiredNonNullable,
    string? RequiredNullable,
    string OptionalNonNullable = "default",
    string? OptionalNullable = "default"
    );
```

不过在.NET 9之前，所有构造函数的参数都被序列化程序认为是可选的。在.NET 9之后，`JsonSerializerOptions`添加了一个尊重必须构造函数参数的选项（别忘了对于`record`这类不可变对象的反序列化是通过构造函数来实现的）`RespectRequiredConstructorParameters`。在打开这个选项之后，针对缺少属性的反序列化就会正常报错了。

```csharp
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

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
```

#### 反序列化为结构

结构作为值类型，虽然在函数之间传递时需要被拷贝而带来了额外的性能开销，但是也因为这一点而可以被直接分配在栈上，给GC带来的压力较小。因此在部分需要极端性能优化的场景可以直接针对`struct`进行反序列化。

`struct`的反序列化也是通过构造函数来实现的，序列化程序遵循如下的规则来选择构造函数：

1. 对于类，如果唯一的构造函数是参数化构造函数，则选择这一构造函数；
2. 对于结构或者具有多个构造函数的类，需要使用`[JsonConstructor]`手动指定需要使用的构造函数，否则**只会**使用公共无参构造函数（如果存在）。

因此，如果需要针对不可变的结构进行反序列化，需要加上`[JsonConstructor]`注解。例如，针对下面的代码，如果不加上注解，反序列化又会静默地失败。

```csharp
	private struct JsonStruct
    {
        public int Id { get; }

        public string Name { get; }

        [JsonConstructor]
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
        Assert.Equal(1, r.Id);
    }
```

为了简化语法，不可变的结构可以使用`readonly record struct`语法来替代：

```csharp
    private readonly record struct JsonRecordStruct(int Id, string Name);

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
```

不过这里有一个很奇怪的点，使用`readonly record struct`语法之后就不需要`[JsonConstructor]`了。

可以实验一下是`readonly`还是`record`发挥了作用。

在仅仅添加了`readonly`的情况下，反序列化不会成功：

```csharp
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
```

而在仅仅加上`record`的情况下，序列化程序就可以选择正确的构造函数了：

```csharp
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
```

> 不过这样说来`readonly record struct`中的`readonly`似乎是冗余的？
>
> 原来，`record struct`声明的对象是可变的。详见文档中对于[不可变性](https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/builtin-types/record#immutability)的描述。



