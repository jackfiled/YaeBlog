---
title: 从.NET 6到.NET 8中的性能提升综述
date: 2024-08-31T18:51:06.9233598+08:00
tags:
- dotnet
- 技术笔记
- 编译原理
---


JIT编译就一定比AOT编译慢吗？

<!--more-->

长期以来，我们已经习惯了调侃Java较慢的运行速度，并将其原因归咎于Java使用了字节码加虚拟机的JIT编译方式。但是对于同样采用了这样方式的.NET，微软的开发人员却认为——"（虽然这样说很难让人信服，）但是许多人都认为托管应用程序的性能实际上超过了非托管应用程序。有许多原因使我们对此深信不疑。例如，当JIT编译器在运行时将IL代码编译成本地代码时，编译器对于执行环境的认识比非托管编译器更深刻。"（摘自Jeffrey Richter的《CLR via C#》）。

既然微软的开发人员对此深信不疑，同时在.NET Core之后，.NET的内部开发流程逐渐公开化，在[Github](https://github.com/dotnet/runtime)和[.NET 官方博客](https://devblogs.microsoft.com/dotnet/)上都能看到。那么我们就在本篇文章中梳理一下.NET平台从.NET 6到.NET 8三个版本中所有主要的性能提升，主要聚焦于JIT编译器、内存管理等少数几个部分。

## JIT

JIT（Just In Time，即时编译）编译器是运行时中的基础，负责将前端编译器生成的IL（Intermediate Language，就是一套微软规定的中间表示形式）转换为汇编语言，在AOT（Ahead Of Time，提前编译）编译时也是调用的该编译器。这里可以解释一下.NET代码执行的三种模型：

- JIT编译执行：最为“传统“的执行模型，所有的IL代码都需要在执行前通过JIT编译为本机代码再执行。
- 即时运行（ReadyToRun， R2R)：在程序编译阶段先调用JIT编译器将IL代码编译为本机代码，在程序运行时首先运行编译好的本机代码以提高应用启动的速度，在运行过程中再次调用JIT编译对热点代码进行优化编译。为了提高启动速度，.NET中的所有核心库都以R2R的形式提供，程序员可以自行决定编写的代码是否使用R2R的方式运行。
- 提前编译：在程序编译阶段直接调用JIT编译器将IL代码编译为本机代码，程序运行时就执行这一套代码。

### 分层编译、栈上替换和PGO

在.NET从6到8的版本演进过程中，最为重磅的性能更新莫过于在.NET 6便引入的动态PGO（dynamic Profile-Guided Optimization）在.NET 8中终于默认启用了。为了介绍动态PGO，我们必须首先理解JIT对于IL代码的分层编译机制。

#### 分层编译

在JIT编译器最初的设计模型中，每个方法只会被编译一次：每个方法只会在调用被编译为汇编代码，该代码被缓存起来以备下次调用。但是这种设计却导致许多矛盾：一个根本性的矛盾就是JIT编译花费在编译优化上的时间同从优化中能得到的效果之间的矛盾。在编译过程中对代码进行优化几乎是编译器工作过程中最耗时的部分，尤其是对于一个JIT编译器来说，编译的时间几乎直接决定了应用启动的时间，如果对一个方法进行优化需要耗费一秒钟的时间，但是仅能使该方法的运行时间从10毫秒下降到1毫秒，在该方法在运行过程只会调用一次的情况下，编译器引入该优化只会让程序的运行时间增加。因此，编译器必须要在程序运行时的效率和启动时间之间做取舍。尤其是考虑到程序的**空间局部性**原理：程序中的大多数函数只会在运行时被调用少数几次，对于这些函数在启动时耗费大量的优化时间是纯纯的浪费。

**分层编译**的引入从根本上解决了这个问题：该编译策略允许一个方法在运行时被编译多次。

在第一次调用时，方法会被编译到第0层（Tier 0）。在这个编译层级上只会应用少数的编译优化策略，这些编译优化策略被称为最小优化策略（Minimal Optimization，Min Opts）。需要指出的这些策略实际上也不少，包含了那些可以使JIT编译器更快运行的优化策略，例如可以生成更少量的本机代码。在优化的同时JIT编译器还会注入一些短短的代码片段（stub），这些代码片段使得运行时可以统计每个方法的调用次数。

运行时可以监控这些方法的调用次数，当某个方法的调用次数超过某个预先设定的阈值时，这个方法将被加入重新编译的队列。这次编译将会把方法编译到第1层（Tier 1），JIT编译器将会在编译的过程中应用所有可能的优化策略。在整个程序的运行过程中，只有少数被多次调用的方法会编译到第1层。同时编译器也可以通过收集方法在第0层的运行过程中的信息来进行第1层编译过程中的优化。例如对于`static readonly`类型的变量，当方法在第0层执行之后，这些类型的变量已经完成初始化且无法再发生更改，此时编译器就可以将这些变量当作是`const`类型的常量，将所有应用于常量的优化策略扩展到该类型的变量上进行应用。

在大多数情况下，使用分层编译可以使用程序同时获得良好的启动速度和运行效率，除了某些特定的情况。这些特定情况的一个典型例子就是运行时间非常长的方法：在上述的优化策略中只重视了调用次数非常多的方法，但是运行时间非常长的方法也对于效率有着非常明显的影响。而在分层编译的情况下，这些长运行时间但是少调用次数的函数将会只被编译到第0层，这会造成明显的性能下降。因此在.NET 7之前，所有含有回溯分支的方法都会直接编译到第1层。

.NET 7引入的栈上替换改进了这一点。

>这里可能有人会争论：对于少数运行时间长的方法在启动时多施加一些优化策略真的会导致明显的启动时间增加吗，有必要引入更复杂的策略针对这点蚊子腿进行优化吗？
>
>的确，对这点启动时间进行优化很可能是不明显的，但是别忘了编译器可以在第0层的运行过程中收集信息进行第1层的优化，这实际上也是动态PGO机制引入的基础之一。

#### 栈上替换

分层编译很好，除了在面对运行时间长的方法时。例如对于下面这个包含一万次循环的方法：

```csharp
class Program
{
    static void Main()
    {
        var sw = new System.Diagnostics.Stopwatch();
        while (true)
        {
            sw.Restart();
            for (int trial = 0; trial < 10_000; trial++)
            {
                int count = 0;
                for (int i = 0; i < char.MaxValue; i++)
                    if (IsAsciiDigit((char)i))
                        count++;
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
        }

        static bool IsAsciiDigit(char c) => (uint)(c - '0') <= 9;
    }
}
```

当在.NET 6平台运行时，我们可以比较在启用对方法的分层编译和不启用的情况下的性能对比。

| 编号 | 启用分层编译     | 不启用           |
| ---- | ---------------- | ---------------- |
| 1    | 00:00:01.2841397 | 00:00:00.5734352 |
| 2    | 00:00:01.2693485 | 00:00:00.5526667 |
| 3    | 00:00:01.2755646 | 00:00:00.5675267 |
| 4    | 00:00:01.2656678 | 00:00:00.5588724 |
| 5    | 00:00:01.2679925 | 00:00:00.5616028 |

栈上替换（On Stack Replacement）就是为了解决这个问题而引入的：没人规定说一个方法执行的本机代码只能在执行的间隙被替换，在执行的过程中也可以替换掉方法执行的本机代码，也就是当方法还在运行栈上时执行替换。在第0层编译时，编译器不仅可以为函数的调用生成统计调用次数的片段代码，也可以为循环的执行生成运行次数的片段代码。当运行时监控到某一个循环的执行次数超过设定的阈值时，编译器就可以将该方法编译到第1层，运行时将会把方法此时调用的所有寄存器和本地变量复制到一个新的方法调用中，而新的调用使用的本机代码已经是优化之后的本机代码了。

在分层编译和栈上替换的协作下，程序的启动实现和运行性能之前就可以达到一个较好的平衡了。当然，分层编译和栈上替换的能力并不仅限于优化应用的启动时间，在动态PGO中这两者将会发挥更大的作用。

#### 动态PGO

采样制导的优化（Profile-Guided Optimization）并不是一个新鲜的概念，在数十年前就出现，并在多种编程语言和运行时中得到的应用。PGO的一个典型工作流程一般如下：

1. 在插入一些特定指令的情况下构建应用程序；
2. 将应用程序放在典型的应用场景下进行运行，并通过这些特定指令收集运行的信息；
3. 在这些信息的指导下重新构建应用程序，得到针对运行场景的特定优化。

这种工作流程被称作是静态的PGO，这些工作流往往额外的应用知识、特定的工具和构建-上线流程的反复执行。

回到.NET的执行过程中，既然分层编译已经可以将程序生成为第0层和第1层两个版本，为什么不在第0层程序的运行过程中收集一些有用的信息输入到第1层的编译过程中呢，这样编译器还可以生成更加优化的第1层本机代码。这个过程中传统静态PGO流程中的构建-运行-再构建流程完全一致，不过现在优化的层级可以聚焦在方法上，而不是针对整个程序进行优化，以及最为重要的是，这一切都在程序运行的过程中由JIT编译器自动的进行，不需要任何额外的开发工作或者是针对性的构建流程。

在.NET 6到.NET 8整整三个大版本对于动态PGO的迭代过程中引入了大量的优化，这里仅能介绍一小部分。

首先是为了更好发挥动态PGO的性能，JIT编译器中为分层编译引入了更多的编译层数。需要引入更多编译层数的原因主要有两点。第一，插入各种采样的指令和代码是需要代价的，考虑到第0层编译的主要目标是为了降低编译的时间，提高应用的启动速度，在第0层编译过程中就不能插入太多的采样指令。因此编译器首先增加了一个新的编译层——采样第0层来解决这个问题。大部分的方法将在第一次运行时编译到缺少优化、缺少采样指令的第0层，在运行时发现该方法被调用了多次之后，JIT编译器将这个方法重新编译到采样第0层，再经过一系列的调用之后，JIT编译器将利用采样得到的信息对该方法重新进行编译并优化。第二，在原始编译器模型中使用即时运行（R2R）方法编译的代码不能参加到动态PGO中，尤其是考虑到几乎所有应用程序都会调用的核心库代码是采用R2R的方式进行运行的，如果这部分的代码不能参加动态PGO将不能够完全发挥动态PGO的效果，虽然核心库在提前编译的过程中会使用静态PGO进行一部分的优化。因此JIT编译器为R2R编译好的代码增加了一个新的编译器，在运行时发现这部分代码被调用多次之后将会被JIT编译器编译到含有优化和采样代码的采样第1层，随着调用次数的增加这部分的代码将可以利用采样得到的信息进行优化。下面这张图展现了不同编译方法在运行过程中可能达到的编译层级。

![image-20240828135354598](./dotnet-performance-8/image-20240828135354598.webp)

JIT编译器也在第0层编译的过程中引入了更多的优化。虽然第0层编译的目的是缩短编译的时间，但是许多的优化可以通过减少需要生成的代码数量来达到这个目的。常量折叠（Constant Folding）就是一个很好的例子。虽然这会让JIT编译器在第0层编译时花费更多的时间同运行时中的虚拟机交互来解析各种变量的类型，但是这可以大量的减少JIT编译器需要生成的代码量，尤其是对于下面这种涉及到类型判断的例子。

```csharp
MaybePrint(42.0);

static void MaybePrint<T>(T value)
{
    if (value is int)
    {
        Console.WriteLine(value);
    }
}
```

现在在第0层编译的过程中，JIT编译器可以发现`MaybePrint`方法在运行过程中不会运行任何实际的代码路径，因此可以直接优化掉这段代码。

```assembly
; Assembly listing for method Program:<<Main>$>g__MaybePrint|0_0[double](double) (Tier0)
; Emitting BLENDED_CODE for X64 with AVX - Windows
; Tier0 code
; rbp based frame
; partially interruptible

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       mov      rbp, rsp
       vmovsd   qword ptr [rbp+0x10], xmm0

G_M000_IG02:                ;; offset=0x0009

G_M000_IG03:                ;; offset=0x0009
       pop      rbp
       ret

; Total bytes of code 11
```

插入的采样代码片段也会造成一些性能上的问题。为了优化JIT编译器往往需要统计各种方法和分支的调用和运行次数，但是问题是这些统计调用次数的代码应该如何编写？尤其是考虑到代码片段是一个静态的“数据”，会在各种不同的运行线程之间共享，如何设计一个线程安全同时高效的统计方法？

最初的统计方式是设计一个朴素、没有线程同步的方法，例如`_branches[branchId]++`。虽然这种方法没有在运行时引入大量的同步开销，但是这也意味着在某个方法被多个线程同时调用时会损失掉大量的统计数据，这会造成一个本应该提前进入动态PGO的方法得到优化的时间严重滞后。这方面一个容易想到的方式是使用同步的方法进行统计，例如给数据加锁或者是使用原子指令（`Interlocked.Add`）。但是这种方式会严重的导致性能下降。为了解决这个问题，开发者们设计了一种非常巧妙的解决方法，这种方法的C#实现如下所示。

```csharp
static void Count(ref uint sharedCounter)
{
    uint currentCount = sharedCounter, delta = 1;
    if (currentCount > 0)
    {
        int logCount = 31 - (int)uint.LeadingZeroCount(currentCount);
        if (logCount >= 13)
        {
            delta = 1u << (logCount - 12);
            uint random = (uint)Random.Shared.NextInt64(0, uint.MaxValue + 1L);
            if ((random & (delta - 1)) != 0)
            {
                return;
            }
        }
    }

    Interlocked.Add(ref sharedCounter, delta);
}
```

在计数器的值没有超过8192时，计数逻辑直接使用原子指令进行统计。当计数器的数值超过8192之后，计数逻辑将采用一个随机的增加策略。首先按照50%的概率给计数器增加2，然后按照25%的概率增加4，然后按照12.5%的概率增加8，依次类推。随着计数器值的增加，但是需要调用原子指令的频率也就越低。

为了验证该计数逻辑的有效性，可以使用下面的代码进行验证。

```csharp
using System.Diagnostics;

uint counter = 0;
const int ItersPerThread = 1_000_000_00;

while (true)
{
    Run("Interlock", _ =>
    {
        for (int i = 0; i < ItersPerThread; i++) Interlocked.Increment(ref counter);
    });
    Run("Racy     ", _ =>
    {
        for (int i = 0; i < ItersPerThread; i++) counter++;
    });
    Run("Scalable ", _ =>
    {
        for (int i = 0; i < ItersPerThread; i++) Count(ref counter);
    });
    Console.WriteLine();
}

void Run(string name, Action<int> body)
{
    counter = 0;
    long start = Stopwatch.GetTimestamp();
    Parallel.For(0, Environment.ProcessorCount, body);
    long end = Stopwatch.GetTimestamp();
    Console.WriteLine(
        $"{name} => Expected: {Environment.ProcessorCount * ItersPerThread:N0}, Actual: {counter,13:N0}, Elapsed: {Stopwatch.GetElapsedTime(start, end).TotalMilliseconds}ms");
}
```

运行得到的数据如下所示：

| 类型     | 期望数值      | 实际数值      | 运行时间     |
| -------- | ------------- | ------------- | ------------ |
| 原子指令 | 2,000,000,000 | 2,000,000,000 | 22241.9848ms |
| 朴素     | 2,000,000,000 | 220,525,235   | 277.3435ms   |
| 随机     | 2,000,000,000 | 2,024,587,268 | 527.5323ms   |

从数据上就可以发现，新方法可以在和朴素方法接近的运行时间下获得和使用原子指令接近的实际数值，而且运行时间会随着数值的增加进一步的减少，逐渐逼近朴素方法的运行时间。

如何准确而低成本的技术并不是采样过程中唯一的问题。另一个问题是如何统计在接口或者是虚拟方法调用时哪个类型是最可能被调用到的类型，如果JIT能够得到这种信息，就可以为该类型生成一条更加快速的调用路径。正如上一个算法所揭示的，准确统计每一个类型被调用的次数是非常昂贵的，因此在这里开发者引入了一种被称作蓄水池采样（Reservoir Sampling）的方法进行统计。例如对于一个含有60%的`'a'`、30%的`'b'`和10%的`‘c'`的字符序列，如何快速而准确的统计其中哪个字符出现的频率最高？利用蓄水池采样算法，可以写出如下的统计代码：

> 蓄水池采样算法设计的目的是为了解决这样一个问题：给出一个数据流，这个数据流的长度很大或者是未知，并且对于该数据流中的数据只能访问一次。请设计一个随机选择算法，使得数据里中所有数据被选中的概率相等。

```csharp
// Create random input for testing, with 60% a, 30% b, 10% c
char[] chars = new char[1_000_000];
Array.Fill(chars, 'a', 0, 600_000);
Array.Fill(chars, 'b', 600_000, 300_000);
Array.Fill(chars, 'c', 900_000, 100_000);
Random.Shared.Shuffle(chars);

for (int trial = 0; trial < 5; trial++)
{
    // Reservoir sampling
    char[] reservoir = new char[32]; // same reservoir size as the JIT
    int next = 0;
    for (int i = 0; i < reservoir.Length && next < chars.Length; i++, next++)
    {
        reservoir[i] = chars[i];
    }
    for (; next < chars.Length; next++)
    {
        int r = Random.Shared.Next(next + 1);
        if (r < reservoir.Length)
        {
            reservoir[r] = chars[next];
        }
    }

    // Print resulting percentages
    Console.WriteLine($"a: {reservoir.Count(c => c == 'a') * 100.0 / reservoir.Length}");
    Console.WriteLine($"b: {reservoir.Count(c => c == 'b') * 100.0 / reservoir.Length}");
    Console.WriteLine($"c: {reservoir.Count(c => c == 'c') * 100.0 / reservoir.Length}");
    Console.WriteLine();
}
```

程序的输出是5次次采样统计的结果：

![image-20240828155556375](./dotnet-performance-8/image-20240828155556375.webp)

需要指出的是，虽然在上面的代码中使用和运行时代码中一样的“蓄水池”大小，但是在运行时并没有提前获得所有需要统计的数据，调用的统计数据是由多个不同的运行线程同时写入蓄水池中的。从结果中可以看出，虽然数值上并不准确，但是该算法准确的统计出了各个字符的出现趋势。

在上述两个例子中，算法中都引入了随机数的概念进行统计，这就导致每次运行的结果都在一定程度上有着不同，同时这也会导致在每次程序运行的过程中，动态PGO所做的优化都会有轻微的不同。有的开发者可能会担心这些随机的引入是否会造成程序运行行为的不可确定性从而导致程序的调试变得困难，但是实际上在引入这些随机数之后这些代码路径已经就有一定的不确定性（例如那个朴素的调用次数统计算法），同时开发过程中已经有大量的数据证实这些代码的行为是总体上稳定且可重现的。

本篇文章中介绍动态PGO的部分就大致到这里，但是文章后续的部分中仍然可以在各个地方中看到动态PGO的身影，这也可以侧面看出动态PGO对于整个优化的巨大作用。

### 函数内联

函数内联是JIT编译器能完成的重要优化之一，其的运行逻辑是取消对于某个方法的直接调用，而是将该方法的执行代码直接插入到当前的控制流中。函数内联最显而易见的优化是减小了调用函数过程中压栈和弹栈带来的开销，但除了对于某些在热点路径上的小型方法，这点减少的开销实际上并不是函数内联实际上带来的主要优化。

函数内联带来的主要优化是其将被调用者的逻辑暴露给了调用者，或者反过来。例如，当调用者将一个常数作为参数传递给被调用的方法时，如果被调用的方法没有进行内联，对该方法进行编译时编译器就无从得知一个常数被传递了过来，但是如果该方法被内联了，进行编译的编译器就可以应用一切对于常数可以应用的优化，包括删除死代码、分支预测、常量折叠等等。

按照这个逻辑分析，那么在编译的时候应该应内联尽内联，但是内联有可能会增加编译之后的指令条数。而指令条数的增加可能会造成指令缓存效率的下降——当需要读取内存的次数越多时，缓存的效率就会越低。例如考虑一个方法，这个方法在整个程序中被内联了100次，而这一百次都内联编译为一份不同的本机代码序列，这一百次调用就完全不能高效的利用指令缓存，而如果对于这个方法没有进行内联，这一百次调用都可以指向同一个内存地址，这就让指令缓存感到非常舒适。因此在JIT编译器编译一个方法时，如果编译器聪明到可以判断出内联之后编译得到的指令序列将少于直接调用得到的指令序列那么编译器就可以执行内联操作，反之编译器就需要衡量内联方法得到的吞吐量提高和增长的指令序列造成的运行效率了。

因此就需要JIT编译器合理的判断哪些方法在编译过程中需要进行内联，哪些方法在编译过程中进行内联。这方面编译器做出的主要更新是让内联更好的能够判断需要被内联方法的内容，尤其是在方法没有被分层编译或者是方法直接跳过了第0层编译的情况下。再考虑到在运行时库中引入的大量可以低成本调用的硬件加速指令方法，这些方法也可以有效的进行内联。

### 去虚拟化

在调用一个接口类型的变量上的方法时，运行时需要做的一个重要工作就是判断实际上应该调用哪个类型的对象上的方法，这在对于接口、虚拟成员方法、泛型方法和委托类型的调用上都是适用的。

因此JIT编译器引入一种被称为保险去虚拟化（Guarded Devirtualization，GDV）的机制进行优化，这种机制也是在动态PGO的帮助下引入的。具体地说，在运行时将会统计具体被调用的类型或者方法的频率，然后在进行优化编译时为最常出现的类型提供一条快速调用的路径。对于下面这种例子来说：

```csharp
public class Tests
{
    internal interface IValueProducer
    {
        int GetValue();
    }
    
    class Producer : IValueProducer
    {
        public int GetValue() => 42;
    }
    
    private IValueProducer _valueProducer;
    private int _factor = 2;

    public void Setup() => _valueProducer = new Producer42();

    public int GetValue() => _valueProducer.GetValue() * _factor;
}
```

对于其中的`GetValue`方法，在没有动态PGO和GDV的参与下，这个方法中将会被编译为一种普通的接口方法调用。但是在启用了动态PGO的环境下，编译器将会注意到对于`IValueProducer`最常见的实现是`Producer`，这样JIT编译器就可以为`Producer`生成一条快速路径，对应与下面的C#实现：

```csharp
int result = _valueProducer.GetType() == typeof(Producer) ?
    Unsafe.As<Producer>(_valueProducer).GetValue() :
    _valueProducer.GetValue();
return result * _factor;
```

.NET中实现的GDV优化可以支持生成多个GDV，也就是在进行接口调用同时为多个类型生成快速路径。但是这个默认的运行条件下是关闭，需要用户通过一个特定的环境变量进行设置`DOTNET_JitGuardedDevirutalizationMaxTypeChecks`。这一优化在使用AOT编译器直接编译到本机代码时还有一个非常有趣的效果，考虑到在进行AOT编译时会对程序集进行裁剪，也就是删除掉最终的应用程序中没有用到的类型，这就让编译器可以在编译时知道实现了某一特定接口的类型总共有哪些，并且在这些类型的数量较少时直接为这些类型都生成调用时的快速路径而完全避免在运行时进行判断。

在上文中已经提到GDV不仅可以在调用接口上定义方法时使用，也可以在调用委托的时候使用。这使用GDV在和循环克隆（Loop Cloning）等优化技术配合时能够发挥出更大的功能，例如对于下面这个例子：

```csharp
public class Tests
{
    private readonly Func<int, int> _func = i => i + 1;

    public int Sum() => Sum(_func);

    private static int Sum(Func<int, int> func)
    {
        int sum = 0;
        for (int i = 0; i < 10_000; i++)
        {
            sum += func(i);
        }

        return sum;
    }
}
```

在上面的示例代码的循环中调用了一个委托`func`，在动态PGO和GDV的参与下，编译器可以知道这个委托最常见的实现（其实是唯一的）是一个固定的Lambda函数（暂且称之为Known Lambda），因此编译器可以将`Sum`函数的编译器为如下的等价C#代码：

```csharp
private static int Sum(Func<int, int> func)
{
    int sum = 0;
    for (int i = 0; i < 10_000; i++)
    {
        sum += func.Method == KnownLambda ? i + 1 : func(i);
    }

    return sum;
}
```

> 这里需要注意的是，这些代码都是**等价**C#代码，实际上编译器并不是先编译为一种C#形式的代码，而是直接生成为汇编代码。

显然，在循环内部反复的进行一个相同的判断并不是一个理想的状态。因此在变量提升（hoisting）优化技术的帮助下，编译器可以将循环内部一个相同的判断提升到循环外部执行，这将产生如下的等价代码。

```csharp
private static int Sum(Func<int, int> func)
{
    int sum = 0;
    bool isAdd = func.Method == KnownLambda;
    for (int i = 0; i < 10_000; i++)
    {
        sum += isAdd ? i + 1 : func(i);
    }

    return sum;
}
```

这还不是优化的极限，注意到在每个循环中还有个重复的三元表达式，这个的结果在各次循环之前也应该是稳定的，因此在循环克隆优化的指导下，编译器将生成如下的等价代码。

```csharp
private static int Sum(Func<int, int> func)
{
    int sum = 0;
    if (func.Method == KnownLambda)
    {
        for (int i = 0; i < 10_000; i++)
        {
            sum += i + 1;
        }
    }
    else
    {
        for (int i = 0; i < 10_000; i++)
        {
            sum += func(i);
        }
    }
    return sum;
}
```

这可以说，在动态PGO和GDV优化策略的加持下，一些“传统的”优化策略又被编译器榨出了新的潜能，从实际的跑分上也可以验证这惊人的优化。

| 方法 | 条件             | 平均运行时间 |
| ---- | ---------------- | ------------ |
| Sum  | 开启动态PGO和GDV | 2.320us      |
| Sum  | 关闭动态PGO和GDV | 16.546us     |

### 分支

分支代码几乎是所有的代码片段中都会涉及到的模式，包括各种循环、判断和三元表达式种种。但是考虑到现代处理器都是多发射的超标量流水线处理器，而各种分支代码往往会打断这些高速运行的流水线，尽管处理器的设计者会通过分支预测器等技术进行猜测，而且往往还猜得很准，但是如果预测出错就需要清空流水线重新运行。因此如何减少代码中的分支是编译器优化的重要课题。

删除重复的分支判断是一个常见的分支优化，尤其常见与用户代码和库代码进行交互的过程中。例如对于下面这个例子：

```csharp
	public ReadOnlySpan<char> SliceOrDefault(ReadOnlySpan<char> span, int i)
    {
        if ((uint)i < (uint)span.Length)
        {
            return span.Slice(i);
        }

        return default;
    }
```

这段代码中首先判断索引起始的位置是否小于切片的长度再调用对应的切片方法，但是在`ReadOnlySpan<char>.Slice`的源代码中还有一个几乎一致的判断：

```csharp
		public ReadOnlySpan<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException();
 
            return new ReadOnlySpan<T>(ref Unsafe.Add(ref _reference, (nint)(uint)start /* force zero-extension */), _length - start);
        }
```

这就让生成的本机代码中出现两个冗余的判断。编译器可以针对这种冗余的判断进行检查并删除这些重复判断。这种类似的分支删除后面还会在“消除边界检查”章节中提到。

灵活的应用各种位运算也是一种常见的分支优化策略。例如对于下面这种对于一个有符号整数的判断`i >= o && j >= 0`可以直接被优化为`(i | j) >= 0`，通过引入一个位运算就减少了一个分支判断。除了灵活的应用位运算之外，使用指令集提供的各种条件移动指令也是一种有效的分支优化策略，比如`x86/64`指令集中提供的`cmov`指令和`arm`指令集中提供的`csel`指令，这些指令都将一个条件判断封装到一条指令中。

C#编译器也可以在分支消除中贡献一份属于自己的力量。考虑.NET中非常常见的一个类型`System.Boolean`，在使用中这个类型是一个两值类型，有且仅有两个取值`true`和`false`。但是实际上在运行时中会使用一个字节大小的空间来存储一个类型，这意味实际上该类型有着256个取值，并且将0视为`false`，将`[1,255]`视为`true`。当然开发者可以使用`unsafe`代码绕过一些编译器的限制，但是“普通的”的开发者和核心库都只会给这个类型的赋予0或者1两个值。因此，在设计一类特殊的算法——无分支判断算法时，开发者可能会写出如下的代码：

```csharp					
static int ConditionalSelect(bool condition, int whenTrue, int whenFalse) =>
    (whenTrue  *  condition) +
    (whenFalse * !condition);
```

但是上述的代码并不能被C#接受，因为C#编译器限制不能让布尔类型参加运算，因此这类算法的开发者不得不因此引入两个多余的分支判断：

```csharp
static int ConditionalSelect(bool condition, int whenTrue, int whenFalse) =>
    (whenTrue  * (condition ? 1 : 0)) +
    (whenFalse * (condition ? 0 : 1));
```

但是现在C#编译器可以消除掉这两个多余的分支判断，因为在.NET世界中编译器可以确保布尔变量的取值只能有1或者0两种情况。

#### 消除边界检查

.NET提供的一种特性就是运行时安全，这其中重要的一点就是对于数组、字符串和切片在运行时进行边界检查。但是这些边界检查就会在实际生成的代码中生成大量的分支判断，这会导致程序运行的效率严重下降。因此如何让编译器在能够保证访问安全的情况下消除掉部分不必要的边界检查是编译器优化中的一个重要课题。

例如在一个常用数据结构——哈希表中，通常的实现是计算键的哈希值，并利用该哈希值作为下标在数组中获得存储的对象。考虑到哈希值是一个`int`类型的变量，但是哈希表中很少需要存储高达21亿对象，因此往往需要对哈希值取模之后再作为数组的下标，此时取模的值常常就是数组的长度。也就是说，在这种情况下对于数组的访问是不可能出现越界的情况下。因此编译器可以为类似与如下的代码取消访问数组时的边界检查：

```csharp
public class Tests
{
    private readonly int[] _array = new int[7];

    public int GetBucket() => GetBucket(_array, 42);

    private static int GetBucket(int[] buckets, int hashcode) =>
        buckets[(uint)hashcode % buckets.Length];
}
```

同样的，对于下面这些代码，编译器也可以取消访问数组时的边界检查：

```csharp
public class Tests
{
    private readonly string _s = "\"Hello, World!\"";

    public bool IsQuoted() => IsQuoted(_s);

    private static bool IsQuoted(string s) =>
        s.Length >= 2 && s[0] == '"' && s[^1] == '"';
}
```

### 常量折叠

常量折叠（Constant Folding）同样是一个编译器在生成代码时可以进行的重要优化，这让编译器在计算在编译器时就可以确定的值，而不是让他们留到运行时进行。最朴素的常量折叠——例如计算一个数学表达式的值——在这里不在赘述。在上面介绍函数内联时也涉及到了常量折叠的内容，分层编译的引入也会使得常量折叠的应用范围变广，这些都不在这里重复。

进行常量折叠优化时一个重要的问题是“教会”编译器哪些变量是常量。这方面编译器得到的提升有：

- 可以将一个字面值字符串的长度视为一个常数；
- 在进行空安全的检查时字面值字符串是必定不为空的；
- 编译器在编译时除了可以进行一些简单的数学运算，现在整个`System.Math`命名空间中提供的算法都可以在编译时进行运算；
- `static readonly`类型的字符串和数组长度被视为一个常数；
- `obj.GetType()`现在在JIT编译器明确了解类型的情况下可以被替换为一个常量；
- `DateTime`等时间类型初始化时可以在编译期计算内存存储的时间。例如对于`new DateTime(2023, 9, 1)`将会直接被编译到`new DateTime(0x8DBAA7E629B4000)`。

上述这些并不能完全覆盖在.NET 6到.NET 8三个大版本之中引入的所有JIT编译器优化，但是从中也可以一窥编译器优化的精巧之处。首先，编译器的优化并不是一个个独立优化策略的组合，而且各种优化策略的有机组合。方法的内联就是一个典型例子，通过将被调用方法的内容暴露给调用者（或者反过来）让其他的各种优化策略发挥更大的作用。其次，JIT编译器在编译优化方面可以发挥更伟大的作用。通过在程序运行时对于运行环境和程序本身有着更加深刻的理解，JIT编译器可以在运行时发挥出更高的性能。

## 内存管理

.NET中的垃圾回收器（GC）负责管理应用程序的内存分配和释放。每当有对象新建时，运行时都会将从托管堆为对象分配内存，主要托管堆中还有地址空间，运行时就会从托管堆为对象分配内存。不过内存并不是无限的，垃圾回收器就负责执行垃圾回收来释放一些内存。垃圾回收器的优化引擎会根据所执行的分配来确定执行收回的最佳时机。

.NET中内存管理中的一个显著变更为将内存的抽象从段（Segment）修改为区域（Region）。段和区域之前最明显的区别是大小，段是较大的内存——在64位的机器上一个段的大小万网是1GB、2GB或者是4GB，而区域是非常小的单元，在默认情况下只有4MB的大小。从宏观上来说，之前的GC是为每个代的堆维持一个GB级别的内存范围，而现在GC则是维持了许多个较小的内存区域，这些内存区域可以被分配给各个代的堆（或者其他可能涉及的堆）使用。

垃圾回收器中还有两个引人注意的特性增加。第一个是动态的代提升和下降（Dynamic Promotion and Demotion，`DPAD`），第二个是动态适应应用程序大小（Dynamic Adaptive To Application Size，`DATAS`）。`DPAD`特性允许GC在工作的过程中动态的设置一个区域的代数，例如直接将一个可能存活时间非常长的对象配置为第2代，而这在之前的GC模型中需要通过两次垃圾回收才能实现。而第二个特性`DATAS`旨在适应应用程序的内存要求，即应用程序堆的大小和长期数据大小大致成正比，即使在不同规格的计算机上执行相同的工作时，运行时中堆的大小也是类似的。相比如下，传统的服务器模式下的GC旨在提高程序的吞吐量，允许内存的分配量基于吞吐量而不是应用程序的大小。`DATAS`对于各种突发类型的工作负载是非常有利的，同时通过允许堆大小按照工作负载的要求进行调整，这将让一些内存首先的环境直接受益。

### 无垃圾回收的堆

在程序中大量会涉及到使用常量字符串的情形，例如下面这个例子：

```csharp
public class Tests
{
    public string GetPrefix() => "https://";
}
```

在.NET 7平台上这个方法会被JIT编译器编译之后得到下面这段本机代码：

```assembly
; Tests.GetPrefix()
       mov       rax,126A7C01498
       mov       rax,[rax]
       ret
; Total bytes of code 14
```

在这段代码中使用了两个`mov`指令，其中第一个指令加载存储这个字符串对象地址的地址，第二个读取该地址。从这段本机代码可以看见，尽管已经是在处理一个常量的字符串，但是编译器和运行时仍然需要为这个字符串在堆上分配一个`string`对象：因为一个在堆上分配的对象在GC的控制下会在内存中发生移动，编译器就不能为这个对象使用一个固定的内存地址，需用从一个指定的地址读取该对象所在的地址。如果能让这个常量字符串分配在不会移动的内存区域中就能从编译器和GC两个方面上提高程序运行的效率。

为了优化这种生成周期和程序一致对象的内存管理，.NET 8中引入了一个新的堆——没有内存管理的堆。JIT编译器将会保证这些常量类型的对象将会被分配在这个堆中，这种没有GC管理的堆也意味着JIT编译器可以为这些对象使用一个固定的内存地址，在使用时避免掉了一次内存读取。

![Heaps where .NET Objects Live](./dotnet-performance-8/HeapsWhereNetObjectsLive.webp)

将上述提高的示例代码使用.NET 8版本进行编译得到的代码如下，从中也可以看出JIT编译器生成的代码只有一条`mov`指令，避免了一次内存访问。

```assembly
; Tests.GetPrefix()
       mov       rax,227814EAEA8
       ret
; Total bytes of code 11
```

这个没有内存管理的堆引入还可以让其他的类型受益。例如对于`typeof(T)`返回的类型对象，容易想到一个程序集中所有类型对象的生命周期应该是和程序一致的，因此也可以在这个堆上分配所有这些类型对象。`Array.Empty<T>`也可以利用类似的思路分配在这个堆上。

### 值类型

因为可以避免在堆上分配内存，值类型已经在.NET的高性能代码中得到了广泛的应用，虽然频繁的内存拷贝可能带来额外的性能开销。因此编译器对于值类型的各种优化就显得至关重要。

这部分优化中一个引人注目的点是值类型的“推广”（promotion）这里的推广意味着将一个结果划分为组成它的各种字段来区别对待。可以利用下面这个示例代码进行理解：

```csharp
public class Tests
{
    private ParsedStat _stat;

    [Benchmark]
    public ulong GetTime()
    {
        ParsedStat stat = _stat;
        return stat.utime + stat.stime;
    }

    internal struct ParsedStat
    {
        internal int pid;
        internal string comm;
        internal char state;
        internal int ppid;
        internal int session;
        internal ulong utime;
        internal ulong stime;
        internal long nice;
        internal ulong starttime;
        internal ulong vsize;
        internal long rss;
        internal ulong rsslim;
    }
}
```

在这段代码中有一个较大的结构类型，其的大小是80个字节。在没有启用推广的条件下进行运行，`GetTime`方法编译得到的本机代码如下所示。在汇编代码中将下载栈上分配一片88字节的空间，再将整个结构体直接复制到当前方法的栈上，在复制完成之后计算两个字段的和并返回。

```assembly
; Tests.GetTime()
       push      rdi
       push      rsi
       sub       rsp,58
       lea       rsi,[rcx+8]
       lea       rdi,[rsp+8]
       mov       ecx,0A
       rep movsq
       mov       rax,[rsp+10]
       add       rax,[rsp+18]
       add       rsp,58
       pop       rsi
       pop       rdi
       ret
; Total bytes of code 40
```

而在打开推广的情况下运行得到的本机代码如下所示：

```assembly
; Tests.GetTime()
       add       rcx,8
       mov       rax,[rcx+8]
       mov       rcx,[rcx+10]
       add       rax,rcx
       ret
; Total bytes of code 16
```

在这段汇编代码中，JIT编译器只复制了两个需要使用的字段到当前方法的栈上，这就大幅减少了值类型在方法调用之前产生内存复制开销。

## 还有更多……

行文至此，本篇已经字数超过一万字了，毫无疑问这将成为博客历史上最长的一篇文章。在这点字数中我们还只是**简略**的介绍了一下.NET平台过去的几个版本中涉及到的优化，还主要聚焦于JIT编译器和内存管理的部分，在这两个部分之后还有一个线程管理部分也是影响性能的关键组件，同时.NET还提供了一个由数千个API组成的运行库，这些类型中无论是基元类型还是泛型集合类型都获得了若干提升，这些部分共同组成了这几个版本的性能奇迹。

本篇文章中的主要内容来自于.NET运行时仓库中的[Book of the Runtime](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/README.md)和微软开发者博客上的[Performance Improvements in .NET 6](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-6/)、[Performance Improvements in .NET 7](https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/)和[Performance Improvements in .NET 8](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8/#whats-next)等几篇文章，上述没有覆盖到的内容推荐读者这些文章。同时算算时间，.NET 9版本引入的性能提升文章应该也要发布了。

回到文章最开始时的问题：JIT编译就一定比AOT编译慢吗？从启动速度上来说，JIT编译当然是完败AOT编译，但是在程序长时间运行，各项设备（JIT编译器、运行时和GC等）预热完成之后，则是鹿死谁手，犹未可知了。

