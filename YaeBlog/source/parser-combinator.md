---
title: 使用Parser Combinator编写编译器前端
tags:
  - 编译原理
  - C#
  - dotnet
---

在函数式编程思想的指导下，使用Parser Combinator编写编译器前端。

<!--more-->

在编译原理的课程上，我们往往会学习两种编写语法分析器的方式，分别是自顶向下的递归下降分析和`LL(1)`语法分析器和自底向上的`LR(1)`语法分析器。在课堂上，我们通常更加聚焦于学习`LL(1)`和`LR(1)`等几种给定BNF范式的语法就能自动生成表驱动分析器的分析技术。在实践中，以`Yacc`和`ANTLR`为代表语法分析器就是上述思想的经典实现。

但是如果我们调研实际中的编译器，会发现大多数的编译器都是使用递归下降的方式手动编写解析器，而不是使用各种的解析器生成工具。这往往是因为解析器生成器是难以扩展的，且生成的代码通常质量不高，在阅读和调试上都存在一系列的困难。但是手写解析器通常是一个非常枯燥的过程，写出来的代码也不能直观的表示语法。

因此我们希望能够有一种方法，在给定一种语言的语法之后就可以明确、简单地设计出该语言的词法分析器和语法分析器。上个世纪末出现了抽象能力极强函数式编程语言，有人注意到可以直接使用函数式编程语言的代码来表示各种文法的产生式，这类技术就被成为解析器组合子（Parser Combinator）。

## Parser Combinator初见

在没有学习编译原理的课程之前，如果我们需要编写一个识别文本的程序，我们往往会这样编写程序。例如识别一个布尔表达式：

```
bool_value := true | false;
```

我们会写出下面这种代码：

```csharp
bool ParserBoolValue(string input)
{
    if (string.StartsWith("true"))
    {
        return true;
    }
    
    if (string.StartsWith("false"))
    {
        return false;
    }
    
    throw new InvalidInputException();
}
```

这就是一个**解析器（Parser）**的原型实现：输入需要识别的字符串，输出识别的结果。在这个原型方法中，我们可以很容易发现两个问题：

1. 解析器并不会**消耗**输入的字符串，缺少一个读取字符串的游标。
2. 解析器输出结果的方式并不高明，输出的结果只能是解析成功情况的结果，解析失败的情况需要通过抛出一个自定义的异常`InvalidInputException`，但是考虑到一个解析器解析失败是非常常见的情况，在这里使用异常方式返回错误结果会导致非常严重的性能问题。

从上述两点出现，我们需要设计一个输入状态的抽象和一个解析结果的抽象才能设计出一个良好的解析器抽象。

首先是输入状态的抽象，这个输入状态需要具有一个*读取游标*的功能，可以读取当前值并向后移动，容易想到**链表**是一个非常适合该场景的数据结构（实际上在部分函数式编程语言中，列表的默认实现就是链表）。然后是解析结果的抽象，解析结果至少需要能够被表示两个状态——成功状态和失败状态。从面向对象设计的角度出发，我们可以设计一个解析结果基类，并分别实现一个成功解析结果基类和一个失败解析结果基类。但是从函数式编程的角度出发，我们可以设计一个类似于Rust中的枚举或者F#中的可区分联合之类的数据结构。（实际上目前C#的设计者正在设计并实现一个类似的东西[Discriminated Unions](https://github.com/dotnet/csharplang/issues/113)，毕竟在代码中需要返回成功或者失败结果的函数太常见了，而使用异常是一个非常昂贵的方式，使用`Try`和`out`关键词也不是一个非常优雅的方式。）

然后我们便可以设计出如下的解析器原型函数：

```csharp
public abstract class Parser<T>
{
    public ParseResult<T> Parse<TState>(TState state) where TState : IReadState<TState>;
}
```

上面解析器基类中，类上的泛型参数`T`表示该解析器最终解析结果的类型，解析函数`Parse`的泛型参数`TState`是实现了输入状态`IReadState`的类型，返回的类型`ParseResult`就是上文中提到的解析结果基类。

在设计完解析器之后，该谈一谈**组合子（Combinator）**了。实际上组合子就是将多个解析器组合到一起的一系列函数，输入一个或者多个解析器，输出一个合并之后的解析器。容易想到，各种解析器组合在一起的方式千千万万，但是实际上我们只需要实现一系列基本的组合子，就可以通过综合使用各种解析器和组合子将各种需要的解析器和组合子实现出现。实际上，这也是解析器组合子思想的集中体现，通过基础的“砖块”（解析器）和“水泥“（组合子）设计和实现各种构建，最终建造出宏伟的高楼。

基础解析器和组合子的选择因人而异，但是一个常见的组合是：

- 空解析器（empty)，一个总是成功并返回指定值的解析器。
- 短语解析器（term)，解析指定的短语并返回对应结果的解析器。
- 选择组合子（alternate），输入两个解析器，在相同的输入上执行并返回两者的结果。
- 连接组合子（sequence），输入两个解析器，依次应用这两个解析器并返回最终的结果。

好了，让我们来建设高楼吧！

## 实现一个C#的Parser Combinator库

## 解析器基类、输入状态接口和解析结果基类

在正式开始设计解析器和组合子之前，还请允许我再啰唆一下库中最为重要的那些接口和基类设计。

#### 输入状态接口

库中的输入状态接口详细定义如下：

```csharp
/// <summary>
/// 输入流的读取状态
/// </summary>
/// <typeparam name="TToken">输入流元素类型</typeparam>
public interface IReadState<out TToken>
{
    public TToken Current { get; }

    public bool HasValue { get; }
}

/// <summary>
/// 输入流的读取状态
/// </summary>
/// <typeparam name="TToken">输入流元素类型</typeparam>
/// <typeparam name="TState">下一个读取状态的类型</typeparam>
public interface IReadState<out TToken, TState> : IReadState<TToken>, IEquatable<TState>
    where TState : IReadState<TToken, TState>
{
    /// <summary>
    /// 下一个读取状态
    /// </summary>
    TState Next { get; }
}
```

这是一个**泛型**的链表接口定义，而且将链表的数据部分和指针部分分拆到两个接口中。分拆的好处在于在定义对于某一个特定输入状态的处理函数是可以使用`IReadState<TTokem>`接口而不用考虑下一个节点的指针类型。

同时这个输入状态接口没有限制输入类型，而是使用一个泛型类型`TToken`。这大大增加了解析器组合子库的泛用性，不仅可以用于处理各种字符串解析的场景，对于二进制比特流也可以进行解析。在实际的编译器设计过程中也可以首先针对`char`类型的输入流设计一个词法分析器，然后在设计一个针对词法令牌类型的输入流设计一个语法分析器。

#### 解析结果基类

```csharp
/// <summary>
/// 解析器结果
/// </summary>
/// <typeparam name="TToken">输入流类型</typeparam>
/// <typeparam name="T">实际结果类型</typeparam>
public abstract class ParseResult<TToken, T>
{
    /// <summary>
    /// 实际结果对象
    /// </summary>
    public abstract T Value { get; }

    /// <summary>
    /// 在当前结果上应用下一个解析器
    /// </summary>
    /// <param name="nextParser">下一个解析器的函数</param>
    /// <param name="continuation">处理解析结果的后继函数</param>
    /// <typeparam name="TNext">下一个解析器函数返回的解析结果类型</typeparam>
    /// <typeparam name="TResult">最终的解析结果类型</typeparam>
    /// <returns></returns>
    internal abstract ParseResult<TToken, TResult> Next<TNext, TResult>(Func<T, Parser<TToken, TNext>> nextParser,
        Func<ParseResult<TToken, TNext>, ParseResult<TToken, TResult>> continuation);

    /// <summary>
    /// 映射结果
    /// </summary>
    /// <param name="map">映射结果的函数</param>
    /// <typeparam name="TResult">映射结果函数返回解析结果的类型</typeparam>
    /// <returns>最终的解析结果</returns>
    public abstract ParseResult<TToken, TResult> Map<TResult>(Func<T, TResult> map);

    /// <summary>
    /// 在成功或者失败解析结果上应用不同的后继函数
    /// </summary>
    /// <param name="successfulHandler">在成功解析结果上应用的函数</param>
    /// <param name="failedHandler">在失败解析结构上应用的函数</param>
    /// <typeparam name="TResult">最后返回解析结果的类型</typeparam>
    /// <returns>最后的解析结果</returns>
    public abstract TResult CaseOf<TResult>(Func<SuccessfulResult<TToken, T>, TResult> successfulHandler,
        Func<FailedResult<TToken, T>, TResult> failedHandler);
}
```

#### 解析器基类

```csharp
/// <summary>
/// 解析器抽象基类
/// </summary>
/// <typeparam name="TToken">输入流类型</typeparam>
/// <typeparam name="T">解析结果类型</typeparam>
public abstract class Parser<TToken, T>
{
    /// <summary>
    /// 解析器运行函数
    /// </summary>
    /// <param name="state">解析的输入流状态</param>
    /// <param name="continuation">运行之后的后继函数</param>
    /// <typeparam name="TState">输入流状态类型</typeparam>
    /// <typeparam name="TResult">后继函数运行之后的解析结果类型</typeparam>
    /// <returns></returns>
    internal abstract ParseResult<TToken, TResult> Run<TState, TResult>(TState state,
        Func<ParseResult<TToken, T>, ParseResult<TToken, TResult>> continuation)
        where TState : IReadState<TToken, TState>;

    public ParseResult<TToken, T> Parse<TState>(TState state) where TState : IReadState<TToken, TState>
    {
        return Run(state);
    }

    private ParseResult<TToken, T> Run<TState>(TState state) where TState : IReadState<TToken, TState>
    {
        try
        {
            return Run(state, result => result);
        }
        catch (Exception e)
        {
            return ParseResultBuilder.Fail<TToken, TState, T>(e, state);
        }
    }

    public static Parser<TToken, T> operator |(Parser<TToken, T> a, Parser<TToken, T> b)
        => a.Alternative(b);
}
```

在解析器基类的设计上仍然使用了常见的CPS(Continuous Passing Style)设计范式，在解析器运行函数中需要传入一**后继函数**，对该解析器返回的解析结果进行进一个的处理之后再返回。在后续`sequence`类别的组合子设计中也将会利用这一点。

## 基础解析器和组合子的选择与设计

在设计解析器和组合子时，我们分成三类，分别是基础组合子（Basic），修改解析结果的解析器组合子（Modified Parser）和原组合子（Primitive Parser）。

基础组合子是将输入的一个或者多个解析器组合为新解析器的组合子，主要有选择组合子（Alternative Parser）、单子组合子（Bind Parser）、映射组合子（Map Parser）和下一个组合子（Next Parser）等。这里需要指出的是，上面选择的一些组合子理论上完全可以通过其他的组合子组合出来，但是有些非常常用的组合子使用其他组合子进行组合会导致运行时栈的嵌套深度过深，降低运行效率，因此选择这部分组合子单独实现可以在一定长度上提高整个库的运行效率。在库提供的若干个组合个中选择关键的组合子作为基础组合子进行实现也是一个效率的平衡。

这里贴一下两个重要但不复杂的基础组合子实现：选择组合子和单子组合子。

```csharp
/// <summary>
/// 选择解析器
/// 如果第一个不成功则调用第二个
/// </summary>
/// <param name="first">第一个解析器</param>
/// <param name="second">第二个解析器</param>
/// <typeparam name="TToken">输入流类型</typeparam>
/// <typeparam name="T">解析器结果类型</typeparam>
internal sealed class AlternativeParser<TToken, T>(Parser<TToken, T> first, Parser<TToken, T> second)
    : Parser<TToken, T>
{
    internal override ParseResult<TToken, TResult> Run<TState, TResult>(TState state,
        Func<ParseResult<TToken, T>, ParseResult<TToken, TResult>> continuation)
    {
        return first.Run(state, result => result.CaseOf(continuation, _ => second.Run(state, continuation)));
    }
}
```

```csharp
/// <summary>
/// 选择解析器
/// 如果第一个不成功则调用第二个
/// </summary>
/// <param name="first">第一个解析器</param>
/// <param name="second">第二个解析器</param>
/// <typeparam name="TToken">输入流类型</typeparam>
/// <typeparam name="T">解析器结果类型</typeparam>
internal sealed class AlternativeParser<TToken, T>(Parser<TToken, T> first, Parser<TToken, T> second)
    : Parser<TToken, T>
{
    internal override ParseResult<TToken, TResult> Run<TState, TResult>(TState state,
        Func<ParseResult<TToken, T>, ParseResult<TToken, TResult>> continuation)
    {
        return first.Run(state, result => result.CaseOf(continuation, _ => second.Run(state, continuation)));
    }
}
```

同时还有一个常用且复杂的组合子：修改组合子（Fix Parser）。这个组合子通过传入一个针对自己的修改函数获得一个新的解析器，在后面的组合子设计作为一个递归的实现出现。

```csharp
/// <summary>
/// 修正？解析器
/// 感觉是一种递归的高级实现？
///
/// </summary>
/// <typeparam name="TToken"></typeparam>
/// <typeparam name="T"></typeparam>
internal sealed class FixParser<TToken, T> : Parser<TToken, T>
{
    private readonly Parser<TToken, T> _parser;

    public FixParser(Func<Parser<TToken, T>, Parser<TToken, T>> func)
    {
        _parser = func(this);
    }

    internal override ParseResult<TToken, TResult> Run<TState, TResult>(TState state,
        Func<ParseResult<TToken, T>, ParseResult<TToken, TResult>> continuation)
        => _parser.Run(state, continuation);
}
```

>设计出这种组合子的人确实很牛逼，感觉可以加入“这种代码我一辈子也写不出来”系列。

修改组合子是在传入解析器的基础上修改解析器结果的一类组合子。在这类组合子中较为重要的有：向前看组合子（Look Ahead Parser）和翻转组合子（Reverse Parser）。

向前看组合子在解析成功之后不会将输入状态向下移动，以此来达到向前看的效果。

```csharp
/// <summary>
/// 向前看解析器
/// 使用传入的解析器向前解析
/// 但是返回的结果中输入流读取状态不前移
/// </summary>
/// <param name="parser">需要向前看的解析器</param>
/// <typeparam name="TToken">输入流令牌</typeparam>
/// <typeparam name="T">返回的解析结果类型</typeparam>
internal sealed class LookAheadParser<TToken, T>(Parser<TToken, T> parser) : ModifiedParser<TToken, T, T>(parser)
{
    protected override ParseResult<TToken, T> Succeed<TState>(TState state,
        SuccessfulResult<TToken, T> successfulResult)
        => ParseResultBuilder.Succeed<TToken, TState, T>(successfulResult.Value, state);

    protected override ParseResult<TToken, T> Fail<TState>(TState state, FailedResult<TToken, T> failedResult)
        => ParseResultBuilder.Fail<TToken, TState, T>($"Failed when looking ahead: {failedResult}", state);
}
```

翻转组合子负责翻转传入解析器的解析结果，常常和向前看组合子配合使用，达到向前看不到期望输入的效果。

```csharp
/// <summary>
/// 翻转结果的解析器
/// 当成功时失败
/// 当失败时返回指定的成功结果
/// </summary>
/// <param name="parser">上游解析器</param>
/// <param name="result">期望中的结果</param>
/// <typeparam name="TToken">输入流的类型</typeparam>
/// <typeparam name="TIntermediate">上游解析器结果类型</typeparam>
/// <typeparam name="T">最终的返回结果</typeparam>
internal sealed class ReverseParser<TToken, TIntermediate, T>(Parser<TToken, TIntermediate> parser, T result)
    : ModifiedParser<TToken, TIntermediate, T>(parser)
{
    protected override ParseResult<TToken, T> Succeed<TState>(TState state,
        SuccessfulResult<TToken, TIntermediate> successfulResult)
        => ParseResultBuilder.Fail<TToken, TState, T>($"Unexpected successful result: {successfulResult.Value}",
            state);

    protected override ParseResult<TToken, T> Fail<TState>(TState state,
        FailedResult<TToken, TIntermediate> failedResult)
        => ParseResultBuilder.Succeed<TToken, TState, T>(result, state);
}
```

元组合子理论上只有成功特定短语的`term`解析器和不识别任何内容直接成功的`empty`解析器两种，但是在这里我们还是额外多实现了一些，仍然是出现效率的考量。同时我们把短语解析器修改为了满足解析器（Satisfy Parser），通过传入一个判断谓词进行解析，提高了编写的灵活性。

```csharp
/// <summary>
/// 满足指定条件即成功的解析器
/// </summary>
/// <param name="predicate">满足的条件谓词</param>
/// <typeparam name="TToken">输入流类型</typeparam>
internal sealed class SatisfyParser<TToken>(Func<TToken, bool> predicate) : PrimitiveParser<TToken, TToken>
{
    protected override ParseResult<TToken, TToken> Run<TState>(TState state)
    {
        return state.HasValue && predicate(state.Current)
            ? ParseResultBuilder.Succeed<TToken, TState, TToken>(state.Current, state.Next)
            : ParseResultBuilder.Fail<TToken, TState, TToken>(state);
    }
}
```

## 进阶组合子的设计和实现

目前的组合子库中大致一共实现了50个组合子，这里并不会解释涉及到的所有组合子，只列举一些我们实现过程中比较迷惑的组合子。

首先是顺序组合子（Sequence Parser）的实现，这个组合子输入一系列组合子，顺序应用所有的组合子之后输出结果。我们最开始的实现是：

```csharp
public static Parser<TToken, IEnumerable<T>> Sequence<TToken, T>(IEnumerable<Parser<TToken, T>> parsers)
        => parsers.Aggregate(Pure<TToken, IEnumerable<T>>([]),
            (result, parser) => result.Bind(
                x => parser.Map(x.Append)));
```

但是我们发现类似开源库对于这个组合子的实现是：

```csharp
public static Parser<TToken, IEnumerable<T>> Sequence<TToken, T>(IEnumerable<Parser<TToken, T>> parsers)
         => parsers.Reverse().Aggregate(Pure<TToken, IEnumerable<T>>([]),
             (next, parser) => parser.Bind(
                 x => next.Map(result => result.Prepend(x))));
```

造成上述实现差异的可能原因是闭包中捕获元素的不同：在我们的实现中传给`Map`函数的闭包需要捕获的变量`x`是`IEnumerable<T>`类型，在开源库中的需要捕获的变量就是`T`类型的，这可能在一定程度上造成我们实现的运行效率不如开源库的效率。

使用指定解析器识别零次到多次的组合子`Many`的实现是一个典型的递归实现：

```csharp
    private static Parser<TToken, IEnumerable<T>> ManyRecursively<TToken, T>(this Parser<TToken, T> parser,
        IEnumerable<T> result)
        => parser.Next(x => parser.ManyRecursively(result.Append(x)), result);

    public static Parser<TToken, IEnumerable<T>> Many<TToken, T>(this Parser<TToken, T> parser)
        => parser.ManyRecursively([]);
```

这里一个挺有趣的问题是，在使用`IEnumerable`作为目标类型是，`[]`将会被初始化为哪个类型？

我使用[sharplab](https://sharplab.io/#v2:D4AQTAjAsAUCAMACEEAsBuWsQGZlkQGFEBvWRC5PEVRAWQAoBKU8y9lHAHgEsA7AC4A+RAEMATuNEBPRAF5EAbQgAaMCpwBdTDHYBfWHqA==)进行了一波实验，发现C#编译器会默认实现为一个数组类型。

```csharp
public class C {
    public void M() {
        IEnumerable<int> array = [1,2,3];
    }
}
```



![image-20240813214315576](./parser-combinator/image-20240813214315576.png)

跳过组合子的实现则是使用我们之前提过的修改组合子（Fix Parser）进行的。

```csharp	
public static Parser<TToken, Unit> SkipMany<TToken, TIgnore>(this Parser<TToken, TIgnore> parser)
        => Fix<TToken, Unit>(self => parser.Next(_ => self, Unit.Instance));
```

实际上这段Magic Code也可以使用显式递归函数的方式实现为：

```csharp
public static Parser<TToken, Unit> SkipManyRecursively<TToken, TIgnore>(this Parser<TToken, TIgnore> parser)
	=> parser.Next(_ => parser.SkipManyRecursively(), Unit.Instance);
```

在跳过直到组合子中也是使用修改组合子（Fix Parser）实现的：

```csharp
public static Parser<TToken, T> SkipTill<TToken, TIgnore, T>(this Parser<TToken, TIgnore> parser,
        Parser<TToken, T> terminator)
        => Fix<TToken, T>(self => terminator | parser.Right(self));
```

在这段代码中使用到一个非常有趣的组合子 Right。这个组合子和它的孪生兄弟Left其实都是单子组合子的封装：

```csharp
public static Parser<TToken, TLeft> Left<TToken, TLeft, TRight>(this Parser<TToken, TLeft> left,
        Parser<TToken, TRight> right)
        => left.Bind(right.Map);

public static Parser<TToken, TRight> Right<TToken, TLeft, TRight>(this Parser<TToken, TLeft> left,
        Parser<TToken, TRight> right)
        => left.Bind(_ => right)
```

实际的作用是Left 组合子返回左侧解析器返回的结果作为最终结果，Right 组合子返回右侧解析器返回的结果作为最终结果。

最后一个有趣的组合子是引用组合子（Quote Parser），这个组合子输入三个解析器，负责解析由左解析器和右解析器限定范围内的所有元素。非常适合与解析各种封闭范围的元素，例如字符串和注释。

```csharp
public static Parser<TToken, IEnumerable<T>> Quote<TToken, T, TLeft, TRight>(this Parser<TToken, T> parser,
        Parser<TToken, TLeft> left, Parser<TToken, TRight> right)
        => left.Right(parser.ManyTill(right))
```

## Pascal词法分析器实战

在准备好丰富的砖瓦之后，我们可以先尝试盖一栋小楼，编写一个可以解析Pascal-S语言词法的词法分析器。

Pascal-S语言的词法约定如下所示：

![image-20240813220521028](./parser-combinator/image-20240813220521028.png)

![image-20240813220530717](./parser-combinator/image-20240813220530717.png)

据此，我们可以开始编写对应的词法分析器。首先给出一个词法令牌的规定，将词法令牌分类为：

- 关键词、
- 整型常数、
- 浮点常数、
- 操作符、
- 分隔符、
- 标识符、
- 字符、
- 字符串。

使用枚举表示出上述的种类，将词法令牌类实现为：

```csharp
public sealed class LexicalToken(LexicalTokenType type, string literalValue) : IEquatable<LexicalToken>
{
    public LexicalTokenType TokenType { get; } = type;

    public string LiteralValue { get; } = literalValue;

    public bool Equals(LexicalToken? other) =>
        other is not null && TokenType == other.TokenType && LiteralValue == other.LiteralValue;

    public override bool Equals(object? obj) => obj is LexicalToken other && Equals(other);

    public override int GetHashCode() => TokenType.GetHashCode() ^ LiteralValue.GetHashCode();

    public override string ToString() => $"<{TokenType}>'{LiteralValue}'";
}
```

对于不同的词法令牌种类实现对应的解析器。

首先是识别关键词的解析器：

```csharp
public static Parser<char, LexicalToken> KeywordParser()
    {
        return from value in Choice(StringIgnoreCase("program"),
                StringIgnoreCase("const"),
                StringIgnoreCase("var"),
                StringIgnoreCase("procedure"),
                StringIgnoreCase("function"),
                StringIgnoreCase("begin"),
                StringIgnoreCase("end"),
                StringIgnoreCase("array"),
                StringIgnoreCase("of"),
                StringIgnoreCase("if"),
                StringIgnoreCase("then"),
                StringIgnoreCase("else"),
                StringIgnoreCase("for"),
                StringIgnoreCase("to"),
                StringIgnoreCase("do"),
                StringIgnoreCase("integer"),
                StringIgnoreCase("real"),
                StringIgnoreCase("boolean"),
                StringIgnoreCase("char"),
                StringIgnoreCase("divide"),
                StringIgnoreCase("not"),
                StringIgnoreCase("mod"),
                StringIgnoreCase("and"),
                StringIgnoreCase("or"),
                StringIgnoreCase("true"),
                StringIgnoreCase("false"),
                StringIgnoreCase("while"))
            from _ in (AsciiLetter() | AsciiDigit() | Char('_')).LookAhead().Not()
            select new LexicalToken(LexicalTokenType.Keyword, value);
    }
```

考虑到在Pascal中关键词不区分大小，使用`StringIgnoreCase`作为定义关键词的解析器组合子，同时向前看识别下一个字符不是任何的字母、数字或者下划线——虽然在词法定义中说下一个字符应该是空格或者换行符，但是考虑到起始和结束关键词以及空格和换行符的统一处理，这里就采用了识别下一个字符不是字母、数字和下划线的方法。

然后是分隔符和运算法的解析器：

```csharp
public static Parser<char, LexicalToken> DelimiterParser()
    {
        Parser<char, LexicalToken> semicolonParser = from token in Char(':')
            from _ in Char('=').LookAhead().Not()
            select new LexicalToken(LexicalTokenType.Delimiter, token.ToString());
        Parser<char, LexicalToken> periodParser = from token in Char('.')
            from _ in Char('.').LookAhead().Not()
            select new LexicalToken(LexicalTokenType.Delimiter, ".");

        Parser<char, LexicalToken> singleCharTokenParser = from token in Choice(
                String(","),
                String(";"),
                String("("),
                String(")"),
                String("["),
                String("]"),
                String(".."))
            select new LexicalToken(LexicalTokenType.Delimiter, token);

        return singleCharTokenParser | semicolonParser | periodParser;
    }

public static Parser<char, LexicalToken> OperatorParser()
    {
        Parser<char, LexicalToken> lessParser = from token in Char('<')
            from _ in Char('=').LookAhead().Not()
            select new LexicalToken(LexicalTokenType.Operator, "<");

        Parser<char, LexicalToken> greaterParser = from token in Char('>')
            from _ in Char('=').LookAhead().Not()
            select new LexicalToken(LexicalTokenType.Operator, ">");

        Parser<char, LexicalToken> otherParsers = from token in Choice(
                String("="),
                String("!="),
                String("<="),
                String(">="),
                String("+"),
                String("-"),
                String("*"),
                String("/"),
                String(":="))
            select new LexicalToken(LexicalTokenType.Operator, token);

        return otherParsers | lessParser | greaterParser;
    }
```

这两个解析器的编写主要是需要注意前缀相同符号的处理，比如冒号和赋值号、点和两个点、大于和大于等于以及小于和小于等于几个符号。

常数的识别就按照表达式编写就好：

```csharp
public static Parser<char, LexicalToken> ConstIntegerParser()
    {
        return from nums in AsciiDigit().Many1()
            from _ in Char('.').LookAhead().Not()
            select new LexicalToken(LexicalTokenType.ConstInteger, new string(nums.ToArray()));
    }

public static Parser<char, LexicalToken> ConstFloatParser()
    {
        return from integer in AsciiDigit().Many1()
            from _ in Char('.')
            from fraction in AsciiDigit().Many1()
            select new LexicalToken(LexicalTokenType.ConstFloat,
                new string(integer.ToArray()) + '.' + new string(fraction.ToArray()));
    }
```

标识符的识别和常数的识别类似：

```csharp
public static Parser<char, LexicalToken> IdentifierParser()
    {
        return from first in AsciiLetter() | Char('_')
            from second in (AsciiLetter() | AsciiDigit() | Char('_')).Many()
            select new LexicalToken(LexicalTokenType.Identifier, first + new string(second.ToArray()));
    }
```

注释的识别和字符串的识别使用我们前文中提到的引用组合子编写非常的方便：

```csharp
public static Parser<char, Unit> CommentParser()
    {
        return Any<char>().Quote(Char('{'), Char('}')).Map(_ => Unit.Instance);
    }

public static Parser<char, LexicalToken> CharParser()
    {
        return from str in Any<char>().Quote(Char('\'')).Map(x => new string(x.ToArray()))
            select str.Length <= 1
                ? new LexicalToken(LexicalTokenType.Character, str)
                : new LexicalToken(LexicalTokenType.String, str);
    }
```

将注释、空格和换行符都作为无用的符号聚合到同一个解析器中：

```csharp
public static Parser<char, Unit> JunkParser()
    {
        return Space().Map(_ => Unit.Instance) | LineBreak().Map(_ => Unit.Instance) | CommentParser();
    }
```

最终将上述解析器组合到一起就构成了完整的词法分析器：

```csharp
public static Parser<char, IEnumerable<LexicalToken>> PascalParser()
    {
        return JunkParser().SkipTill(Choice(KeywordParser(),
            DelimiterParser(),
            OperatorParser(),
            ConstIntegerParser(),
            ConstFloatParser(),
            CharParser(),
            IdentifierParser())).Many();
    }
```

​		
