---
title: async/await究竟是如何工作的？
tags:
  - dotnet
  - 技术笔记
  - 译文
---

### 译者按

如何正确而快速的编写异步运行的代码一直是软件工程界的难题，而C#提出的`async/await`范式无疑是探索道路上的先行者。本篇文章便是翻译自.NET开发者博客上一篇名为“How async/await really works in C#”的文章，希望能够让读者在阅读之后明白`async/await`编程范式的前世今生和`.NET`实现方式。另外，.Net开发者中文博客也翻译了[这篇文章](https://devblogs.microsoft.com/dotnet-ch/async-await%e5%9c%a8-c%e8%af%ad%e8%a8%80%e4%b8%ad%e6%98%af%e5%a6%82%e4%bd%95%e5%b7%a5%e4%bd%9c%e7%9a%84/)，一并供读者参考。

---

数周前，[.NET开发者博客](https://devblogs.microsoft.com/dotnet/)发布了一篇题为[什么是.NET，为什么你应该选择.NET](https://devblogs.microsoft.com/dotnet/why-dotnet/)的文章。文章中从宏观上概览了整个`dotnet`生态系统，总结了系统中的各个部分和其中的设计决定；文章还承诺在未来推出一系列的深度文章介绍涉及到的方方面面。这篇文章便是这系列文章中的第一篇，深入介绍C#和.NET中`async/await`的历史、设计决定和实现细节。

对于`async/await`的支持大约在十年前就提供了。在这段时间里，`async/await`语法大幅改变了编写可扩展.NET代码的方式，同时该语法使得在不了解`async/await`工作原理的情况下使用它提供的功能编写异步代码也是十分容易和常见的。以下面的**同步**方法为例：（因为这个方法的调用者在整个操作完成之前、将控制权返回给它之前都不能进行任何操作，所以这个方法被称为**同步**）

```csharp
// 将数据同步地从源复制到目的地
public void CopyStreamToStream(Stream source, Stream destination)
{
    var buffer = new byte[0x1000];
    int numRead;
    while ((numRead = source.Read(buffer, 0, buffer.Length)) != 0)
    {
        destination.Write(buffer, 0, numRead);
    }
}
```

在这个方法的基础上，你只需要修改几个关键词、改变几个方法的名称，就可以得到一个**异步**的方法（因为这个方法将很快，往往实在所有的工作完成之前，就会将控制权返回给它的调用者，所以被称作异步方法）。

```csharp
// 将数据异步地从源复制到目的地
public async Task CopyStreamToStreamAsync(Stream source, Stream destination)
{
    var buffer = new byte[0x1000];
    int numRead;
    while ((numRead = await source.ReadAsync(buffer, 0, buffer.Length)) != 0)
    {
        await destination.WriteAsync(buffer, 0, numRead);
    }
}
```

有着几乎相同的语法，类似的控制流结构，但是现在这个方法在执行过程中不会阻塞，有着完全不同的底层执行模型，而且C#编译器和核心库帮你完成所有这些复杂的工作。

尽管在不了解底层原理的基础上使用这类技术是十分普遍的，但是我们坚持认为了解这些事务的运行原理将会帮助我们更好的利用它们。之于`async/await`，了解这些原理将在你需要深入探究时十分有用，例如当你需要调试一段错误的代码或者优化某段正确运行代码的运行效率时。在这篇文章中，我们将深入了解`async/await`具体如何在语言、编译器和库层面运行，然后你将更好地利用这些优秀的设计。

为了更好的理解这一切，我们将回到没有`async/await`的时代，看看在没有它们的情况下最优秀的异步代码是如何编写的。平心而论，这些代码看上去并不好。

### 原初的历史

回到.NET框架1.0时代，当时流行的异步编程范式是**异步编程模型**，“Asynchronous Programming Model”，也被称作`APM`范式、`Being/End`范式或者`IAsyncResult`范式。从宏观上来看，这种范式是相当简单的。例如对于一个同步操作`DoStuff`：

```csharp
class Handler
{
    public int DoStuff(string arg);
}
```

在这种编程模型下会存在两个相关的方法：一个`BeginStuff`方法和一个`EndStuff`方法：

```csharp
class Handler
{
    public int DoStuff(string arg);

    public IAsyncResult BeginDoStuff(string arg, AsyncCallback? callback, object? state);
    public int EndDoStuff(IAsyncResult asyncResult);
}
```

`BeginStuff`方法首先会接受所有`DoStuff`方法会接受的参数，同时其会接受一个`AsyncCallback`回调和一个**不透明**的状态对象`state`，而且这两个参数都可以为空。这个“开始”方法将负责异步操作的初始化，而且如果提供了回调函数，这个函数还会负责在异步操作完成之后调用这个回调函数，因此这个回调函数也常常被称为初始化操作的“下一步”。开始方法还会负责构建一个实现了`IAsyncResult`接口的对象，这个对象中的`AsyncState`属性由可选的`state`参数提供：

```csharp
namespace System
{
    public interface IAsyncResult
    {
        object? AsyncState { get; }
        WaitHandle AsyncWaitHandle { get; }
        bool IsCompleted { get; }
        bool CompletedSynchronously { get; }
    }

    public delegate void AsyncCallback(IAsyncResult ar);
}
```

这个`IAsynResult`实例将会被开始方法返回，在调用`AsyncCallback`时这个实例也会被传递过去。当准备好使用该异步操作的结果时，调用者也会将这个`IAsyncResult`实例传递给结束方法，同时结束方法也会负责保证这个异步操作完成，如果没有完成该方法就会阻塞代码的运行直到完成。结束方法会返回异步操作的结果，异步操作过程中引发的各种错误和异常也会通过该方法传递出来。因此，对于下面这种同步的操作：

```csharp
try
{
    int i = handler.DoStuff(arg); 
    Use(i);
}
catch (Exception e)
{
    ... // 在这里处理DoStuff方法和Use方法中引发的各种异常
}
```

可以使用开始/结束方法改写为异步运行的形式：

```csharp
try
{
    handler.BeginDoStuff(arg, iar =>
    {
        try
        {
            Handler handler = (Handler)iar.AsyncState!;
            int i = handler.EndDoStuff(iar);
            Use(i);
        }
        catch (Exception e2)
        {
            ... // 处理从EndDoStuff方法和Use方法中引发的各种异常
        }
    }, handler);
}
catch (Exception e)
{
    ... // 处理从同步调用BeginDoStuff方法引发的各种异常
}
```

对于熟悉使用含有回调`API`语言的开发者来说，这样的代码应该会显得相当眼熟。

但是事情在这里变得更加复杂了。例如，这段代码存在“栈堆积”`stack dive`的问题。栈堆积就是代码在重复的调用方法中使得栈越来越深，直到发生栈溢出的现象。如果“异步”操作同步完成，开始方法将会使同步的调用回调方法，这就意味着对于开始方法的调用就会直接调用回调方法。同时考虑到“异步”方法同步完成却是一种非常常见的现象，它们只是承诺会异步的完成操作而不是只被允许异步的完成。例如一个对于某个网络操作的异步操作，比如读取一个套接字，如果你只需要从一次操作中读取少量的数据，例如在一次回答中只需要读取少量响应头的数据，你可能会直接读取大量数据存储在缓冲区中。相比于每次使用都使用系统调用但是只读取少量的数据，你一次读取了大量数据在缓冲区中，并在缓冲区失效之前都是从缓冲区中读取，这样就减少了需要调用昂贵的系统调用来和套接字交互的次数。像这样的缓冲区可能在你进行任何异步调用之后存在，例如第一次操作异步的完成对于缓冲区的填充，之后的若干次“异步”操作都不需要同I/O进行任何交互而直接通过与缓冲区的同步交互完成，直到缓冲区失效之后再次异步的填充缓冲区。因此当开始方法进行上述的一次调用时，开始方法会发现操作同步地完成了，因此开始方法同步地调用回调方法。此时，你有一个调用了开始方法的栈帧和一个调用了回调方法的栈帧。想想看如果回调方法再次调用了开始方法会发生什么？如果开始方法和回调方法都是被同步调用的，现在你就会在站上得到多个重复的栈帧，如此重复下去直到将栈上的空间耗尽。

这并不是杞人忧天，使用下面这段代码就可以很容易的复现这个问题：

```csharp
using System.Net;
using System.Net.Sockets;

using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
listener.Listen();

using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
client.Connect(listener.LocalEndPoint!);

using Socket server = listener.Accept();
_ = server.SendAsync(new byte[100_000]);

var mres = new ManualResetEventSlim();
byte[] buffer = new byte[1];

var stream = new NetworkStream(client);

void ReadAgain()
{
    stream.BeginRead(buffer, 0, 1, iar =>
    {
        if (stream.EndRead(iar) != 0)
        {
            ReadAgain(); // uh oh!
        }
        else
        {
            mres.Set();
        }
    }, null);
};
ReadAgain();

mres.Wait();
```

在代码中我们建立一个简单的客户端套接字和一个简单的服务端套接字并让它们连接。服务端会向客户端发送十万字节的信息，而客户端会使用开始/结束方法尝试去“异步的”接收这些信息（需要注意这样做是十分低效的，在教学实例之外的地方都不应该这样编写代码）。传递给`BeingRead`的回调函数通过调用`EndRead`方法停止读取，如果在读取过程中读取到数据（意味着还没有读取完成），就通过对于本地方法`ReadAgain`的递归调用来再次调用`BeingRead`方法继续读取。值得指出的是，在.NET Core中套接字操作比原来在.NET Framework中的版本快上许多，同时如果操作系统可以同步的完成这些操作，那么.NET Core中的操作也会同步完成（需要注意操作系统内核也有一个缓冲区来完成套接字接收操作）。因此，运行这段代码就会出现栈溢出。

鉴于这个问题非常容易出现，因此`APM`模型中内建了缓解这个问题的方法。容易想到有两种方法可以缓解这个问题：

1. 不允许`AsyncCallback`被同步调用。如果该回调方法始终都是被异步调用的，即使操作是异步完成的，栈堆叠的方法也就不存在了。但是这样做会降低性能，因为同步完成的操作（或者快到难以注意到的操作）是相当的常见的，强制这些操作的回调排队完成会增加相当可观的开销。
2. 引入一个机制让调用者而不是回调函数在工作异步完成时完成剩余的工作。在这种情况下，我们就避免了引入额外的栈帧，在不增加栈深度的情况下完成了余下的工作。

`APM`模型使用了第二种方法。为了实现这个方法，`IAsyncResult`接口提供了另外两个成员：`IsCompleted`和`CompletedSynchronusly`。`IsCompeleted`成员告诉我们操作是否完成，在程序中可以反复检查这个成员直到它从`false`变成`true`。相对的，`CompletedSynchronously`在运行过程中不会变化，（或者它存在一个还未被发现的`bug`会导致这个值变化，笑），这个值的主要作用是判断后续的工作是应该由开始方法的调用者还是`AsyncCallback`来进行。如果`CompletedSynchronously`的值是`false`，说明这个操作是异步进行的，所有后续的工作应该由回调函数来进行处理；毕竟，如果工作是异步完成的，开始方法的调用者不能知道工作是何时完成的（如果开始方法的调用者调用了结束方法，那么结束方法就会阻塞直到工作完成）。反之，如果`CompletedSynchronously`的值是`true`，如果此时使用回调方法处理后续的工作就会引发栈堆叠问题，因为此时回调方法会在栈上比开始它更低的位置上进行后续的操作。因此任何在意栈堆叠问题的实现需要关注`CompletedSynchronously`的值，当为真的时候，让开始方法的调用者处理后续的工作，而回调方法在此时不应处理任何工作。这也是为什么`CompletedSynchronously`的值不能改变——开始方法的调用者和回调方法需要相同的值来保证后续工作在任何情况下都进行且只进行一次。

因此我们之前的`DoStuff`实例就需要被修改为：

```csharp
try
{
    IAsyncResult ar = handler.BeginDoStuff(arg, iar =>
    {
        if (!iar.CompletedSynchronously)
        {
            try
            {
                Handler handler = (Handler)iar.AsyncState!;
                int i = handler.EndDoStuff(iar);
                Use(i);
            }
            catch (Exception e2)
            {
                ... // handle exceptions from EndDoStuff and Use
            }
        }
    }, handler);
    if (ar.CompletedSynchronously)
    {
        int i = handler.EndDoStuff(ar);
        Use(i);
    }
}
catch (Exception e)
{
    ... // handle exceptions that emerge synchronously from BeginDoStuff and possibly EndDoStuff/Use
}
```

这里的代码已经~~显得冗长~~，而且我们还只研究了如何使用这种范式，还没有涉及如何实现这种范式。尽管大部分的开发者并不需要在这些子调用（例如实现`Socket.BeginReceive/EndReceive`这些方法去和操作系统交互），但是很多开发者需要组合这些操作（从一个“较大的”的异步操作调用多个异步操作），而这不仅需要使用其他的开始/结束方法，还需要自行实现你自己的开始/结束方法，这样你才能在其他的地方使用这个操作。同时，你还会注意到在上述的`DoStuff`范例中没有任何的控制流代码。如果需要引入一些控制流代码——即使是一个简单的循环——这也会立刻变成~~抖M才会编写的代码~~，同时也给无数的博客作者提供水`CSDN`的好题材。

所以让我们现在就来写一篇`CSDN`，给出一个完成的实例。在文章的开头我展示了一个`CopyStreamToStream`方法，这个方式会将一个流中的数据复制到另外一个流中（就是`Stream.CopyTo`方法所完成的工作，但是为了说明，让我们假设这个方法并不存在）：

```csharp
public void CopyStreamToStream(Stream source, Stream destination)
{
    var buffer = new byte[0x1000];
    int numRead;
    while ((numRead = source.Read(buffer, 0, buffer.Length)) != 0)
    {
        destination.Write(buffer, 0, numRead);
    }
}
```

直白的说，我们只需要不停的从一个流中读取数据然后写入到另外一个流中，直到我们没法从第一个流中读取到任何数据。现在让我们使用`APM`模型使用这个操作的异步模式吧：

```csharp
public IAsyncResult BeginCopyStreamToStream(
    Stream source, Stream destination,
    AsyncCallback callback, object state)
{
    var ar = new MyAsyncResult(state);
    var buffer = new byte[0x1000];

    Action<IAsyncResult?> readWriteLoop = null!;
    readWriteLoop = iar =>
    {
        try
        {
            for (bool isRead = iar == null; ; isRead = !isRead)
            {
                if (isRead)
                {
                    iar = source.BeginRead(buffer, 0, buffer.Length, static readResult =>
                    {
                        if (!readResult.CompletedSynchronously)
                        {
                            ((Action<IAsyncResult?>)readResult.AsyncState!)(readResult);
                        }
                    }, readWriteLoop);

                    if (!iar.CompletedSynchronously)
                    {
                        return;
                    }
                }
                else
                {
                    int numRead = source.EndRead(iar!);
                    if (numRead == 0)
                    {
                        ar.Complete(null);
                        callback?.Invoke(ar);
                        return;
                    }

                    iar = destination.BeginWrite(buffer, 0, numRead, writeResult =>
                    {
                        if (!writeResult.CompletedSynchronously)
                        {
                            try
                            {
                                destination.EndWrite(writeResult);
                                readWriteLoop(null);
                            }
                            catch (Exception e2)
                            {
                                ar.Complete(e);
                                callback?.Invoke(ar);
                            }
                        }
                    }, null);

                    if (!iar.CompletedSynchronously)
                    {
                        return;
                    }

                    destination.EndWrite(iar);
                }
            }
        }
        catch (Exception e)
        {
            ar.Complete(e);
            callback?.Invoke(ar);
        }
    };

    readWriteLoop(null);

    return ar;
}

public void EndCopyStreamToStream(IAsyncResult asyncResult)
{
    if (asyncResult is not MyAsyncResult ar)
    {
        throw new ArgumentException(null, nameof(asyncResult));
    }

    ar.Wait();
}

private sealed class MyAsyncResult : IAsyncResult
{
    private bool _completed;
    private int _completedSynchronously;
    private ManualResetEvent? _event;
    private Exception? _error;

    public MyAsyncResult(object? state) => AsyncState = state;

    public object? AsyncState { get; }

    public void Complete(Exception? error)
    {
        lock (this)
        {
            _completed = true;
            _error = error;
            _event?.Set();
        }
    }

    public void Wait()
    {
        WaitHandle? h = null;
        lock (this)
        {
            if (_completed)
            {
                if (_error is not null)
                {
                    throw _error;
                }
                return;
            }

            h = _event ??= new ManualResetEvent(false);
        }

        h.WaitOne();
        if (_error is not null)
        {
            throw _error;
        }
    }

    public WaitHandle AsyncWaitHandle
    {
        get
        {
            lock (this)
            {
                return _event ??= new ManualResetEvent(_completed);
            }
        }
    }

    public bool CompletedSynchronously
    {
        get
        {
            lock (this)
            {
                if (_completedSynchronously == 0)
                {
                    _completedSynchronously = _completed ? 1 : -1;
                }

                return _completedSynchronously == 1;
            }
        }
    }

    public bool IsCompleted
    {
        get
        {
            lock (this)
            {
                return _completed;
            }
        }
    }
}
```

~~Yowsers~~。即使写完了这些繁文缛节，这实际上仍然不是一个完美的实现。例如，`IAsyncResult`的实现会在每次操作时上锁，而不是在任何可能的时候都使用无锁的实现；异常也是以原始的模型存储，如果使用`ExceptionDispatchInfo`可以让异常在传播的过程中含有调用栈的信息，在每次操作中都分配了大量的空间来存储变量（例如在每次`BeingWrite`调用时都会分配一片空间来存储委托），如此等等。现在想象这就是你每次编写方法时需要做的工作，每次当你需要编写一个可重用的异步方法来使用另外一个异步方法时，你需要自己完成上述所有的工作。而且如果你需要编写使用多个不同的`IAsyncResult`的可重用代码——就像在`async/await`范式中`Task.WhenAll`所完成的那样，难度又上升了一个等级；每个不同操作都会实现并暴露针对相关的`API`，这让编写一套逻辑代码并简单的复用它们也变得不可能（尽管一些库作者可能会通过提供一层针对回调方法的新抽象来方便开发者编写需要访问暴露`API`的回调方法）。

上述这些复杂性也说明只有很少的一部分人尝试过这样编写代码，而且对于这些人来说，`bug`也往往如影随形。而且这并不是一个`APM`范式的黑点，这是所有使用基于回调的异步方法都具有的缺点。我们已经十分习惯现代语言都有的控制流结构所带来的强大和便利，因此使用会破坏这种结构的基于回调的异步方式会带来大量的复杂性也是可以理解的。同时，也没有任何主流的语言提供了更好的替代。

我们需要一种更好的办法，一个既继承了我们在`APM`范式中所学习到所有经验也规避了其所有的各种缺点的方式。一个有趣的点是，`APM`范式只是一种编程范式，运行时、核心库和编译器在使用或者实现这种范式的过程中没有提供任何协助。

### 基于事件的异步范式

在.NET Framework 2.0中提供了一系列的`API`来实现一种不同的异步编程范式，当时设想这种范式的主要应用场景是客户端应用程序。这种基于事件的异步范式，也被称作`EAP`范式，也是以提供一系列成员的方式提供的，包含一个用于初始化异步操作的方式和一个监听异步操作是否完成的事件。因此上述的`DoStuff`示例可能会暴露如下的一系列成员：

```csharp
class Handler
{
    public int DoStuff(string arg);

    public void DoStuffAsync(string arg, object? userToken);
    public event DoStuffEventHandler? DoStuffCompleted;
}

public delegate void DoStuffEventHandler(object sender, DoStuffEventArgs e);

public class DoStuffEventArgs : AsyncCompletedEventArgs
{
    public DoStuffEventArgs(int result, Exception? error, bool canceled, object? userToken) :
        base(error, canceled, usertoken) => Result = result;

    public int Result { get; }
}
```

首先通过`DoStuffCompleted`事件注册需要在完成异步操作时进行的工作然后调用`DoStuff`方法，这个方法将初始化异步操作，一旦异步操作完成，`DoStuffCompleted`事件将会被调用者引发。已经注册的回调方法可以运行剩余的工作，例如验证提供的`userToken`是否是期望的`userToken`，同时我们可以注册多个回调方法在异步操作完成的时候运行。

这个范式确实让一系列用例的编写更好编写，同时也让一系列用例变得更加复杂（例如上述的`CopyStreamToStream`例子）。这种范式的影响范围并不大，只在一次.NET Framework的更新中引入便匆匆地消失了，除了留下了一系列为了支持这种范式而实现的`API`，例如：

```csharp
class Handler
{
    public int DoStuff(string arg);

    public void DoStuffAsync(string arg, object? userToken);
    public event DoStuffEventHandler? DoStuffCompleted;
}

public delegate void DoStuffEventHandler(object sender, DoStuffEventArgs e);

public class DoStuffEventArgs : AsyncCompletedEventArgs
{
    public DoStuffEventArgs(int result, Exception? error, bool canceled, object? userToken) :
        base(error, canceled, usertoken) => Result = result;

    public int Result { get; }
}
```

但是这种编程范式确实在`APM`范式所没有注意到的地方前进了一大步，并且这一点还保留到了我们今天所介绍的模型中：[同步上下文](https://github.com/dotnet/runtime/blob/967a59712996c2cdb8ce2f65fb3167afbd8b01f3/src/libraries/System.Private.CoreLib/src/System/Threading/SynchronizationContext.cs#L6) (`SynchronizationContext`)。

同步上下文作为一个对于通用调度器的实现，也是在.NET Framework中引入的。在实践中，同步上下文最常用的方法是`Post`，这个方法将一个工作实现传递给上下文所代表的一种调度器。举例来说，一个基础的同步上下文实现是一个线程池`ThreadPool`，因此`Post`方法的典型实现就是`ThreadPool.QueueUserWorkItem`方法，这个方法将让线程池在池中任意的线程上以指定的状态调用指定的委托。然而，同步上下文的巧妙之处不仅在于提供了对于不同调度器的支持，而是提供了一种针对不同的应用模型使用不同调度方法的抽象能力。

考虑像Windows Forms之类的`UI`框架。对于大多数工作在Windows上的`UI`框架来说，控件往往关联到一个特定的线程，这个线程负责运行一个消息管理中心，这个中心用来运行那些需要同控件交互的工作：只有这个控件有能力来修改控件，任何其他试图同控件进行交互的线程都需要发送消息到这个消息控制中心。Windows Forms通过一系列方法来实现这一点，例如`Control.BeingInvoke`，这类方法将会把提供的委托和参数传递给同这个控件相关联的线程来运行。你可以写出如下的代码：

```csharp
private void button1_Click(object sender, EventArgs e)
{
    ThreadPool.QueueUserWorkItem(_ =>
    {
        string message = ComputeMessage();
        button1.BeginInvoke(() =>
        {
            button1.Text = message;
        });
    });
}
```

这段代码首先将`ComputeMessage`方法交给线程池中的一个线程运行（这样可以保证该方法在运行时`UI`界面不会卡死），当上述工作完成之后，再将一个更新`button1`标签的委托传递给关联到`button1`的线程运行。简单而易于理解。在`WPF`框架中也是类似的逻辑，使用一个被称为`Dispatcher`的类型：

```csharp
private void button1_Click(object sender, RoutedEventArgs e)
{
    ThreadPool.QueueUserWorkItem(_ =>
    {
        string message = ComputeMessage();
        button1.Dispatcher.InvokeAsync(() =>
        {
            button1.Content = message;
        });
    });
}
```

`.NET MAUI`亦然。但是如果我想将这部分的逻辑封装到一个独立的辅助函数中，例如下面这种：

```csharp
// 调用ComputeMessage然后触发更新逻辑
internal static void ComputeMessageAndInvokeUpdate(Action<string> update) { ... }
```

这样我就可以直接：

```csharp
private void button1_Click(object sender, EventArgs e)
{
    ComputeMessageAndInvokeUpdate(message => button1.Text = message);
}
```

但是`ComputerMessageAndInvokeUpdate`应该如何实现才能适配各种类型的应用程序呢？难道需要硬编码所有可能涉及的`UI`框架吗？这就是`SynchronizationContext`大显神威的地方，我们可以这样实现这个方法：

```csharp
internal static void ComputeMessageAndInvokeUpdate(Action<string> update)
{
    SynchronizationContext? sc = SynchronizationContext.Current;
    ThreadPool.QueueUserWorkItem(_ =>
    {
        string message = ComputeMessage();
        if (sc is not null)
        {
            sc.Post(_ => update(message), null);
        }
        else
        {
            update(message);
        }
    });
}
```

在这个实现中将`SynchronizationContext`作为同`UI`进行交互的调度器之抽象。任何应用程序模型都需要保证在`SynchronizationContext.Current`属性上注册一个继承了`SynchronizationContext`的类，这个就会完成调度相关的工作。例如在`Windows Forms`中：

```csharp
public sealed class WindowsFormsSynchronizationContext : SynchronizationContext, IDisposable
{
    public override void Post(SendOrPostCallback d, object? state) =>
        _controlToSendTo?.BeginInvoke(d, new object?[] { state });
    ...
}
```

在`WPF`中有：

```
public sealed class DispatcherSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, Object state) =>
        _dispatcher.BeginInvoke(_priority, d, state);
    ...
}
```

`ASP.NET`*曾经*也有过一个实现，尽管Web框架实际上并不关心是哪个线程在运行指定的工作，但是非常关心指定工作和那个请求相关，因此该实现主要负责保证多个线程不会在同时访问同一个`HttpContext`。

```csharp
internal sealed class AspNetSynchronizationContext : AspNetSynchronizationContextBase
{
    public override void Post(SendOrPostCallback callback, Object state) =>
        _state.Helper.QueueAsynchronous(() => callback(state));
    ...
}
```

这个概念也并不局限于像上面的主流应用程序模型。例如在[xunit](https://github.com/xunit/xunit)，一个流行的单元测试框架（`.NET`核心代码仓库也使用了）中也实现了需要自定义的`SynchronizationContext`。例如限制同步运行单元测试时同时运行单元测试数量就可以用`SynchroniaztionContext`实现：

```
public class MaxConcurrencySyncContext : SynchronizationContext, IDisposable
{
    public override void Post(SendOrPostCallback d, object? state)
    {
        var context = ExecutionContext.Capture();
        workQueue.Enqueue((d, state, context));
        workReady.Set();
    }
}
```

`MaxConcurrentSyncContext`中的`Post`方法只是将需要完成的工作压入其内部的工作队列中，这样就能够控制同时多少工作能够并行的运行。

那么同步上下文这个概念时如何同基于事件的异步范式关联起来的呢？`EAP`范式和同步上下文都是在同一时间引入的，而`EAP`范式要求当异步操作启动的时候，完成事件需要由当前`SynchronizationContext`进行调度。为了简化这个过程（可能反而引入多余的复杂性），在`System.ComponentModel`命名控件中引入了一些帮助程序，具体来说是`AsyncOperation`和`AsyncOperationManager`。其中前者是一个由用户提供的状态对象和捕获到的`SynchronizationContext`组成的元组，后者是一个捕获`SynchronizationContext`和创建`AsyncOperation`对象的工厂类。`EAP`范式会在实现中使用上述帮助类，例如`Ping.SendAsync`会首先调用`AsyncOperationManager.CreateOperationi`来捕获同步上下文，然后当异步操作完成的时候调用`AsyncOperation.PostOperationCompleted`方法来调用捕获到的`SynchronizationContext.Post`方法。

`SynchronizationContext`还提供了其他一些后面会用到的小工具。这个类暴露了`OperationStarted`和`OperationCompleted`两个方法。这个虚方法在基类中的实现都是空的，并不完成任何工作。但是继承其的实现可能会重载这些来了解运行中的操作。`EAP`的实现就会在每个操作开始和结束的时候调用`OperationStarted`和`OperationCompleted`，来方便可能存在的同步上下文跟踪工作的进度。鉴于在`EAP`范式中启动异步操作的方法往往不会返回任何东西，不能指望可以获得任何帮助你跟踪工作进度的东西，因而可能获得工作进度的同步上下文就显得很有价值了。

综上所说，我们需要一些比`APM`编程范式更好的东西，而`EAP`范式引入了一些新的东西，但是没有解决我们面对的核心问题，我们仍然需要一些更好的东西。

### 进入Task时代

在.NET Framework 4.0中引入了`System.Threading.Tasks.Task`类型。当时`Task`类型还只代表某些异步操作的最终完成（在其他编程框架中可能成称为`promise`或者`future`）。当一个操作开始时，创建一个`Task`来表示这个操作，当这个操作完成之后，操作的结果就会被保存在这个`Task`中。简单而明确。但是`Task`相较于`IAsyncResult`提供的重要特点是其蕴含了一个任务在持续运行的状态。这个特点让你能够随意找到一个`Task`，让它在异步操作完成的时候异步的通知你，而不用你关注任务当前是处在已经完成、没有完成、正在完成等各种状态。为什么这点非常重要？首先想想`APM`范式中存在的两个主要问题：

1. 你需要对每个操作实现一个自定义的`IAsycResult`实现：库中没有任何内置开箱即用的`IAsycResult`实现。
2. 你需要在调用开始方法之前就知道在操作结束的时候需要做什么。这让编写使用任意异步操作的组合代码或者通用运行时非常困难。

相对的，`Task`提供了一个通用的接口让你在启动一个异步操作之后“接触”这个操作，还提供了针对“持续”的抽象，这样你就不需要为启动异步操作的方法提供一个持续性。任何需要进行异步操作的人都可以产生一个`Task`，任何人需要使用异步操作的人都可以使用一个`Task`，在这个过程中不用自定义任何东西，`Task`成为了沟通异步操作的生产者和消费者之间最重要的桥梁。这一点大大改变了.NET框架。

现在让我们深入理解`Task`所带来的重要意义。与其直接去研究错综复杂的`Task`源代码，我们将尝试去实现一个`Task`的简单版本。这不会是一个完善的实现，只会完成基础的功能来让我们更好的理解什么是`Task`，即一个负责协调设置和存储完成信号的数据结构。

开始时`Task`中只有很少的字段：

```csharp
class MyTask
{
    private bool _completed;
    private Exception? _error;
    private Action<MyTask>? _continuation;
    private ExecutionContext? _ec;
    ...
}
```

我们首先需要一个字段告诉我们任务是否完成`_completed`，一个字段存储造成任务执行失败的错误`_error`；如果我们需要实现一个泛型的`MyTask<TResult>`，还需要一个`private TResult _result`字段来存储操作运行完成之后的结果。到目前为止的实现和`IAsyncResult`相关的实现非常类似（当然这不是一个巧合）。`_continuation`字段时实现中最重要的字段。在这个简单的实现中，我们只支持一个简单的后续过程，在真正的`Task`实现中是一个`object`类型的字段，这样既可以是一个独立的后续过程，也可以是一个后续过程的列表。这个委托会在任务完成的时候调用。

让我们继续深入。如上所述，`Task`相较于之前的异步执行模型一个基础的优势是在异步操作开始之后再提供后续需要完成的工作。因此我们需要一个方法来实现这个功能：

```csharp
public void ContinueWith(Action<MyTask> action)
{
    lock (this)
    {
        if (_completed)
        {
            ThreadPool.QueueUserWorkItem(_ => action(this));
        }
        else if (_continuation is not null)
        {
            throw new InvalidOperationException("Unlike Task, this implementation only supports a single continuation.");
        }
        else
        {
            _continuation = action;
            _ec = ExecutionContext.Capture();
        }
    }
}
```

如果在调用`ContinueWith`的时候异步操作已经完成，那么就直接将该委托的执行加入执行队列。反之，这个方法就会将存储这个委托，当异步任务完成的时候进行执行（这个方法同时也存储一个被称为`ExecutionContext`的对象，会在后续调用委托的涉及到，我们后续会继续介绍）。

然后我们需要能够在异步过程完成的时候标记任务已经完成。我们将添加两个方法，一个负责标记任务成功完成，一个负责标记任务报错退出。

```csharp
public void SetResult() => Complete(null);

public void SetException(Exception error) => Complete(error);

private void Complete(Exception? error)
{
    lock (this)
    {
        if (_completed)
        {
            throw new InvalidOperationException("Already completed");
        }

        _error = error;
        _completed = true;

        if (_continuation is not null)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (_ec is not null)
                {
                    ExecutionContext.Run(_ec, _ => _continuation(this), null);
                }
                else
                {
                    _continuation(this);
                }
            });
        }
    }
}
```

我们会存储任何的错误、标记任务已经完成，如果已经注册的任何的后续过程，我们也会引发其进行执行。

最后我们还需要一个方法将在工作中发生的任何传递出来，（如果是泛型类型，还需要将执行结果返回），为了方便某些特定的场景，我们将允许这个方法阻塞直到异步操作完成（通过调用`ContinueWith`注册一个`ManualResetEventSlim`实现）。

```csharp
public void Wait()
{
    ManualResetEventSlim? mres = null;
    lock (this)
    {
        if (!_completed)
        {
            mres = new ManualResetEventSlim();
            ContinueWith(_ => mres.Set());
        }
    }

    mres?.Wait();
    if (_error is not null)
    {
        ExceptionDispatchInfo.Throw(_error);
    }
}
```

这就是一个基础的`Task`实现。当然需要指出的是实际的`Task`会复杂很多：

- 支持设置任意数量的后续工作；
- 支持配置其的工作行为（例如配置后续工作是应该进入工作队列等待执行还是作为任务完成的一部分同步被调用）；
- 支持存储多个错误；
- 支持取消异步操作；
- 一系列的帮助函数（例如`Task.Run`创建一个代表在线程池上运行委托的`Task`）。

但是这些内容中没有什么奥秘，核心工作原理和我们自行实现的是一样的。

你可以会注意到我们自行实现的`MyTask`直接公开了`SetResult/SetException`方法，而`Task`没有；这是因为`Task`是以`internal`声明了上述两个方法，同时`System.Threading.Tasks.TaskCompletionSource`类型负责作为一个独立的`Task`生产者和管理任务的完成。这样做的目的并不是出于技术目的，只是将负责控制完成的方法从消费`Task`的方法中分离出来。这样你就可以通过保留`TaskCompletionSource`对象来控制`Task`的完成，不必担心你创建的`Task`在你不知道的地方被完成。（`CancellationToken`和`CanellationTokenSource`也是处于同样的设计考虑，`CancellationToken`是一个包装`CancellationTokenSource`的结构，只暴露了和接受消费信号相关的结构而缺少产生一个取消信号的能力，这样就限制只有`CancellationToeknSource`可以产生取消信号。）

当前我们也可以像`Task`一样为我们自己的`MyTask`添加各种工具函数。例如我们添加一个`MyTask.WhenAll`：

```csharp
public static MyTask WhenAll(MyTask t1, MyTask t2)
{
    var t = new MyTask();

    int remaining = 2;
    Exception? e = null;

    Action<MyTask> continuation = completed =>
    {
        e ??= completed._error; // just store a single exception for simplicity
        if (Interlocked.Decrement(ref remaining) == 0)
        {
            if (e is not null) t.SetException(e);
            else t.SetResult();
        }
    };

    t1.ContinueWith(continuation);
    t2.ContinueWith(continuation);

    return t;
}
```

然后是一个`MyTask.Run`的示例：

```csharp
public static MyTask Run(Action action)
{
    var t = new MyTask();

    ThreadPool.QueueUserWorkItem(_ =>
    {
        try
        {
            action();
            t.SetResult();
        }
        catch (Exception e)
        {
            t.SetException(e);
        }
    });

    return t;
}
```

还有一个简单的`MyTask.Delay`：

```csharp
public static MyTask Delay(TimeSpan delay)
{
    var t = new MyTask();

    var timer = new Timer(_ => t.SetResult());
    timer.Change(delay, Timeout.InfiniteTimeSpan);

    return t;
}
```

在`Task`横空出世之后，之前的所有异步编程范式都成为了过去式。任何使用过去的编程范式暴露的异步`API`，现在都提供了返回`Task`的方法。

### 添加Value Task

直到现在，`Task`都是.NET异步编程中的主力军，在每次新版本发布或者社区发布的新`API`都会返回`Task`或者`Task<TResult>`。但是，`Task`是一个类，而每次创建一个类是都需要分配一次内存。在大多数情况下，为一个会长期存在的异步操作进行一次内存分配时无关紧要的，并不会操作明显的性能影响。但是正如之前所说的，同步完成的异步操作十分创建。例如，`Stream.ReadAsync`会返回一个`Task<int>`，但是如果是在一个类似与`BufferedStream`的实现上调用该方法，那么你的调用由很大概率就会是同步完成的，因为大多数读取只需要从内存中的缓冲区中读取数据而不需要通过系统调用访问`I/O`。在这种情况下还需要分配一个额外的对象显然是不划算的（而且在`APM`范式中也存在这个问题）。对于返回非泛型类型的方法来说，还可以通过返回一个预先分配的已完成单例来缓解这个问题，而且`Task`也提供了一个`Task.CompletedTask`。但是对于泛型的`Task<TResult>`则不行，因为不可能针对每个不同的`TResult`都创建一个对应的单例。那么我们可以如何让这个同步操作更快呢？

我们可以试图缓存一个常见的`Task<TResult>`。例如`Task<bool>`就非常的常见，而且也只存在两种需要缓存的情况：当结果为真时的一个对象和结果为假时的一个对象。同样的，尽管我们可能不想尝试（也不太可能）去缓存数亿个`Task<int>`对象以覆盖所有可能出现的值，但是鉴于很小的`Int32`值时非常常见的，我们可以尝试去缓存给一些较小的结果，例如从-1到8的结果。 而且对于其他任意的类型来说，`default`就是一个常常出现的值，因此缓存一个结果是`default(TResult)`的`Task`。而且	在最近的.NET版本中添加了一个称作`Task.FromResult`辅助函数，该函数就会完成与上述类似的工作，如果存在可以重复使用的`Task<Result>`单例就返回该单例，反之再创建一个新的`Task`对象。对于其他常常出现的值也也可以设计方法进行缓存。还是以`Stream.ReadAsync`为例子，这个方法常常会在同一个流上调用多次，而且每次读取的值都是允许读取的字节数量`count`。再考虑到使用者往往只需要读取到这个`count`值，因此`Stream.ReadAsync`操作常常会重复返回有着相同`int`值的`Task`对象。为了避免在这种情况下重复的内存分配，许多`Stream`的实现（例如`MemoryStream`）会缓存上一次成功缓存的`Task<int>`对象，如果下一次读取仍然是同步返回的且返回了相同的数值，该方法就会返回上一次读取创建的`Task<int>`对象。但是仍然会存在许多无法覆盖的其他情况，能不能找到一种更加优雅的解决方案来来避免在异步操作同步完成的时候避免创建新的对象，尤其是在性能非常重要的场景下。

这就是`ValueTask<TResult>`诞生的背景（[这篇博客](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/)详细测试了`ValueTask<TResult>`的性能）。`ValueTask<TResult>`在诞生之初是`TResult`和`Task<TResult>`的歧视性联合。在这些争论尘埃落定之后，`ValueTask<TResult>`便不是一个立刻可以返回的结果就是一个对未来结果的承诺：

```csharp
public readonly struct ValueTask<TResult>
{
   private readonly Task<TResult>? _task;
   private readonly TResult _result;
   ...
}
```

一个方法可以通过返回`ValueTask<TResult>`来避免在`TResult`已知的情况下创建新的`Task<Result>`对象，当然返回的类型会更大、返回的结果更加不直接。

当然，实际应用中也存在对性能需求相当高的场合，甚至你会想在操作异步完成的时候也避免`Task<TResult>`对象的分配。例如`Socket`作为整个网络栈的最底层，对于网络中的大多数服务来说`SendAsync`和`ReceiveAsync`都是绝对的热点代码路径，不论是同步操作还是异步操作都是非常常见的（鉴于内核中的缓存，大多数发送请求都会同步完成，部分接受请求会同步完成）。因此对于像`Socket`这类的工具，如果我们可以在异步我弄成和同步完成的情况下都实现无内存分配的调用是十分有意义的。

这就是`System.Threading.Tasks.Sources.IValueTaskSource<TResult>`产生的背景：

```csharp
public interface IValueTaskSource<out TResult>
{
    ValueTaskSourceStatus GetStatus(short token);
    void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags);
    TResult GetResult(short token);
}
```

该接口允许自行为`ValueTask<TResult>`实现一个“背后“的对象，并且让这个对象提供了获得操作结构的`GetResult`方法和设置操作后续工作的`OnCompleted`。在这个接口出现之后，`ValueTask<TResult>`也小小修改了定义，`Task<TResult>? _task`字段被一个`object? _obj`字段替换了：

```csharp
public readonly struct ValueTask<TResult>
{
   private readonly object? _obj;
   private readonly TResult _result;
   ...
}
```

现在`_obj`字段就可以存储一个`IValueTaskSource<TReuslt>`对象了。而且相较于`Task<TResult>`在完成之后就只能保持完成的状态，不能变回未完成的状态，`IValueTaskSource<TResult>`的实现有着完全的控制权，可以在已完成和未完成的状态之间双向变化。但是`ValueTask<TResult>`要求一个特定的实例只能被使用一次，不能观察到这个实例在使用之后的任何变化，这也是分析规则[CA2012](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2012)存在的意义。这就让让类似于`Socket`的工具为重复的调用建立一个`IValueTaskSource<TResult>`对象池。从实现上来说，`Socket`会至多缓存两个类似的实例，一个用于读取操作一个用于写入操作，因为在99.999%的情况下同时只会有一个发送请求和一个接受请求。

值得说明的是我只提到了`ValueTask<TResult>`却没有提到`ValueTask`。因为如果只是为了在操作同步完成的时候避免内存分配，非泛型类型的`ValueTask`指挥提供很少的性能提升，因为在同样的条件下可以使用`Task.CompletedTask`。但是如果要考虑在异步完成的时候通过缓存对象避免内存分配，非泛型类型也有作用。因而，在引入`IValueTaskSource<TResult>`的同时，`IValueTaskSource`和`ValueTask`也被引入了。

到目前我们，我们已经可以利用`Task`，`Task<TResult>`，`ValueTask`，`ValueTask<TResult>`表示各种各样的异步操作，并注册在操作完成之前和之后注册后续的操作。

但是这些后续操作仍然是回调方法，我们仍然陷入了基于回调的异步控制流程。该怎么办？

### 迭代器成为大救星

解决方案的先声实际上在`Task`诞生之前就出现了，在C# 2.0引入迭代器语法的时候。

你可能会问，迭代器就是`IEnumerable<T>`吗？这是其中的一个。迭代器是一个让编译器将你编写的方法自动实现`IEnumerable<T>`或者`IEnumertor<T>`的语法。例如我可以用迭代器语法编写一个产生斐波那契数列的可遍历对象：

```csharp
public static IEnumerable<int> Fib()
{
    int prev = 0, next = 1;
    yield return prev;
    yield return next;

    while (true)
    {
        int sum = prev + next;
        yield return sum;
        prev = next;
        next = sum;
    }
}
```

这个方法可以直接用`foreach`遍历，也可以和`System.Linq.Enumerable`中提供的各种方法组合，也可以直接用一个`IEnumerator<T>`对象遍历。

```csharp
foreach (int i in Fib())
{
    if (i > 100) break;
    Console.Write($"{i} ");
}
```

```csharp
foreach (int i in Fib().Take(12))
{
    Console.Write($"{i} ");
}
```

```csharp
using IEnumerator<int> e = Fib().GetEnumerator();
while (e.MoveNext())
{
    int i = e.Current;
    if (i > 100) break;
    Console.Write($"{i} ");
}
```

