---
title: .Net 8中我关心的新功能
date: 2023-11-22 21:07:03
tags:
  - 技术笔记
  - dotnet
---


## .NET

首先看看.NET 8的新增功能，参照官方文档中的[.NET 8的新增功能](https://learn.microsoft.com/zh-cn/dotnet/core/whats-new/dotnet-8)。

### System.Text.Json序列化和反序列化功能

#### 源生成器功能完善

在.NET 8中，使用源生成器的序列化程序的功能已经同使用反射的序列化程序的功能基本类似。

首先是万众瞩目的`AOT`支持中主要的缺憾——依赖反射运行的`JSON`序列化能力无法在`AOT`支持中运行。但是开发团队提供了一种基于编译器的魔法技术——源生成器（Source Generator），利用这种技术我们可以在编译期收集用来序列化和反序列化对象的信息。

因此，在以后我可能会尝试使用`AOT`技术开发几个命令行的小工具。

#### 接口层次结构

在序列化接口时会对其基接口的属性进行序列化。

```c#
IDerived value = new DerivedImplement { Base = 0, Derived = 1 };
JsonSerializer.Serialize(value); // {"Base":0,"Derived":1}

public interface IBase
{
    public int Base { get; set; }
}

public interface IDerived : IBase
{
    public int Derived { get; set; }
}

public class DerivedImplement : IDerived
{
    public int Base { get; set; }
    public int Derived { get; set; }
}
```

很难相信在序列化接口对象时不会自动序列化父接口中的属性，感觉这更像一个`bug`修复而不是一个特性引入。例如在上面的示例代码中，如果在.NET 7中运行会得到的结果是`{"Derived": 1}`.

另外我在使用`JSON`序列化时很少使用接口，因此我也没有遇到这个问题。

#### 命名策略

`JsonNamingPolicy`现在包括下划线命名法`snake_case`和连字符命名法`kebab-case`。

这个是真泪目，想到我最近还写了一堆这样的粪代码：

```c#
public class RoomEvent : SyncEvent, IComparable<RoomEvent>
{
    [JsonPropertyName("event_id")]
    public string? EventId { get; set; }

    [JsonPropertyName("origin_server_ts")]
    public long OriginTimeStamp { get; set; }

    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    public string? Sender { get; set; }

    [JsonPropertyName("state_key")]
    public string? StateKey { get; set; }

    public UnsignedData? Unsigned { get; set; }
    public int CompareTo(RoomEvent? other) =>
        OriginTimeStamp.CompareTo(other?.OriginTimeStamp);
}
```

要是我晚几个月写这个项目就能少写一堆`JsonPropertyName`属性注解。我当时看文档就觉得奇怪，开发团队提供了`JsonNamingPolicy`这个抽象层次却只提供了一个`CamelCase`实现。

#### 只读属性

现在可以反序列化到只读字段和属性。

现在可以在写很多不需要修改的序列化对象时少写一个`set`访问器了，比如很多访问`API`时的反序列化对象，`IDE`也能给我少报一个警告了。

### 新的随机方法

利用`System.Random.GetItems`方式可以从一个集合中随机选择一项输出。

利用这个新的`API`可以减少很多以前需要通过随机索引来从一个列表中获得一项随机输出的重复代码。

### 键化DI服务

在进行依赖注入时，可以使用一个字符串，也就是“键”，来获得同一个接口的不同实现。

这也许提供了一种更为灵活的依赖注入方法。例如我可以通过一个`API`请求的参数来调用同一个接口的不同依赖注入实现。另外我也没想到这个功能在.NET中没有提供，我之前在`Spring`中使用过类似的功能。

## C# 12

新的语法糖可能比框架提供的新功能更加常用，参照[C# 12中的新增功能](https://learn.microsoft.com/zh-cn/dotnet/csharp/whats-new/csharp-12)。

### 主构造函数

非常好语法糖，爱来自`kotlin`。

比如之前我的代码：

```csharp
public class KatheryneChatRobot : IChatRobot
{
    private readonly ILogger<KatheryneChatRobot> _logger;
    private readonly GrammarTree _grammarTree;

    private string _currentStage;

    public KatheryneChatRobot(GrammarTree grammarTree, ILogger<KatheryneChatRobot> logger, 
        string beginStage, string robotName)
    {
        _logger = logger;

        _grammarTree = grammarTree;
        _currentStage = beginStage;
    }
}
```

现在使用主构造函数就可以简化为：

```csharp
public class KatheryneChatRobot(GrammarTree grammarTree, ILogger<KatheryneChatRobot> logger,
        string beginStage, string robotName)
    : IChatRobot
{
    
}
```

不过这些对象没有用下划线开头还有点不习惯。

### 集合表达式

使用集合表达式创建二维数组。

```csharp
int[][] twoD = [[1, 2, 3], [4, 5, 6], [7, 8, 9]];

int[] row0 = [1, 2, 3];
int[] row1 = [4, 5, 6];
int[] row2 = [7, 8, 9];
int[][] twoDFromVariables = [row0, row1, row2];
```

~~C#的Python说宣告成立~~

还引入一个新的**展开运算符**`..`：

```csharp
int[] row0 = [1, 2, 3];
int[] row1 = [4, 5, 6];
int[] row2 = [7, 8, 9];
int[] single = [..row0, ..row1, ..row2];
```

## ASP.NET

参考官方文档中的[8.0版中的新增功能](https://learn.microsoft.com/zh-cn/aspnet/core/release-notes/aspnetcore-8.0?view=aspnetcore-8.0)。

### Blazor

作为在`.net conf 2023`上第一个推出介绍的功能，显然微软对于`blazor`功能还是非常重视的。

#### 新的呈现模式

在原来的`Blazor Server`和`Blazor WebAssembly`呈现模式的基础上，`ASP.NET 8`新增了两种呈现模式：

- 只生成静态HTML文档，不具有交互性的静态服务器呈现；
- 在应用启动时使用`Blazor Server`，在`Blazor WebAssembly`下载完成之后自动切换的自动模式。

静态服务器呈现是一种当页面不需要过多交互性是很好的节省资源的措施。同时，**增强的导航和表单处理**功能和**流式渲染**功能也增强了静态服务器渲染的可用性。增强的导航和表单处理功能可以拦截在静态页面发出的导航到新页面请求和提交表单请求，将原本需要整页刷新的请求修改为局部DOM变化。也就是说，我们可以在没有`websocket`连接的情况下使用`<a>`标签和`<form>`标签实现交互操作，这很好，有一种返璞归真的美。而流式渲染优化了在执行长时间异步加载操作时的用户体验，例如在页面执行数据库请求时首先展示包含占位符内容的页面主要框架，在请求执行完成之后将更新的内容修补到DOM中。例如对于如下的页面：

```html
@page "/weather"
@attribute [StreamRendering(true)]

@if (forecasts == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <table class="table">
        ...
        <tbody>
            @foreach (var forecast in forecasts)
            {
                <tr>
                    <td>@forecast.Date.ToShortDateString()</td>
                    <td>@forecast.TemperatureC</td>
                    <td>@forecast.TemperatureF</td>
                    <td>@forecast.Summary</td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private WeatherForecast[]? forecasts;

    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(500);
        forecasts = ...
    }
}
```

如果在不使用流式渲染的情况下，上述页面需要在

```csharp
await Task.Delay(500);
```

异步操作完成之后才能显示，而在启用流式渲染的情况下，上述页面可以显示一个加载中的安慰剂页面，在异步操作完成之后在通过动态更新DOM将新内容“修补”到页面中。

显然，静态服务器渲染是一个有点用但不多的功能，在特定场景下可能有着比较好的效果。但是在`ASP.NET 8`中还提供了一个新的特性，大大提高这些新的呈现模式的可用性。

> 自动模式因为我还没有实际用过，暂时不过多评价。

#### 将呈现模式应用到组件实例和组件定义

在之前的`Blazor`版本中，我们只能指定整个应用的呈现模式为`Blazor Sever`或者`Blazor WebAssembly`。而在现在我们可以在组件的抽象层级上指定组件的呈现呈现模式。例如，可以将不太需要交互功能的主页指定为静态服务器渲染模式将降低服务器资源占用，而保留其他页面的`Blazor Server`呈现模式。这就大大提高了新呈现模式使用的灵活性。

#### 新的应用模板

在添加了新的呈现模式之后，`Blazor`也修改了应用模板。移除原来的`Blazor Server`模板，用`Blazor Web`模板替代。在`Blazor Web`模板中可以使用上述的四种呈现模式。

#### 在ASP.NET Core 之外呈现Razor组件

现在可以直接将Razor组件直接渲染为HTML，这相当于可以将Razor语法作为一种HTML模板语言使用，感觉是一个非常有用的功能，可以用来开发一个基于Razor的静态网站渲染器。

#### 路由到命名元素

现在可以通过URL导航到页面上的指定元素。

原来这个功能是需要框架支持的吗，，，我还以为是浏览器自动实现的。

### 本机AOT

添加了对于本机`AOT`的支持，现在`gRPC`，最小API都可以使用本机`AOT`，直接编译为单个的二进制可执行文件。

感觉看上去是个挺有用的功能，以后和写`go`和`rust`的哥们交流也可以说，我们也可以编译出一个只依赖于`glibc`的二进制文件了。

不过目前看上去可用性并不大，毕竟光是一个`System.Text.Json`对于`AOT`的支持就花费了大量的时间，不敢想象在`ASP.NET`生态中大量依赖反射运行的库需要多少的时间来支持`AOT`编译。毕竟目前文档中对于最小API的描述仍然是**部分支持**。而且现在好不容易解决了`System.Text.Json`的支持问题，后面还有一个更加重量级的`Entity Framework Core`需要支持。

下面是最小API新引入的`webapiaot`模板中的示例程序：

```c#
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

var sampleTodos = new Todo[] {
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
```

可以看出相对于没有`AOT`支持的最小API应用多了不少内容，大部分都是为了支持`System.Text.Json`而引入的。上面的文件在我的电脑上会编译出一个`10M`左右的可执行文件。

使用`ab`对`todos/`终结点进行压力测试：

```shell
ab -n 10000 -c 1000 http://locallhost:5000/todos/
```

测试之后的结果的如图所示：

![image-20231122100930849](./whats-new-of-dotnet-8/image-20231122100930849.png)

直接使用运行时运行编译出来的`dll`文件：

![image-20231122101012416](./whats-new-of-dotnet-8/image-20231122101012416.png)

修改项目文件和源代码，取消`System.Text.Json`使用源生成器和对于`AOT`的支持。

修改之后的代码为：

```csharp
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var sampleTodos = new Todo[] {
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);
```

使用同样的命令测试得到的结果如图：

![image-20231122101142080](./whats-new-of-dotnet-8/image-20231122101142080.png)

可以发现几次测试的结果都比较接近，几乎可以认为`AOT`编译不会对性能产生明显的影响。虽然按照官方博客中给出的图片，`AOT`编译是有性能下降，应该是我这边测试工具的瓶颈。

![Before and After AOT](./whats-new-of-dotnet-8/AOTOptimizations4.png)

### Identity API 终结点

新的扩展方法，封装了常用的登录注册逻辑。添加了两个终结点：

- `/register`用来注册用户
- `/login`用来登录。

同时支持`Bearer`和Cookies两个认证模式。

