---
title: 异构编程模型的昨天、今天与明天
date: 2024-10-16T13:34:49.0270134+08:00
tags: 
    - 编译原理
    - 组会汇报
---

随着摩尔定律的逐渐失效，将CPU和其他架构的计算设备集成在片上或者通过高速总线互联构建的异构系统成为了高性能计算的主流。但是在这种系统中，上层应用的设计与实现面临着异构系统中各个设备之间体系结构差异过大、缺乏良好的异构抽象以及统一的编程接口和应用程序的优化难度大等困难。

异构并行编程模型便是解决这些编程和执行效率问题的解决方案。

<!--more-->

## 异构并行编程模型概述

异构并行编程模型是沟通上层应用和下层异构系统之间的桥梁，其的设计需要处理好下面五个问题：任务划分、任务映射、数据分布、同步和通信。

### 异构并行编程模型面临的技术挑战

异构并行编程模型面临的技术挑战主要是由两方面带来的：首先异构架构本身为编程模型带来的挑战，其次是上层应用带来的挑战。

异构并行编程模型需要解决的一个重要问题就是为上层应用的程序员提供一个合理的硬件平台抽象，使得其在编程是可以充分释放异构资源带来的计算能力，同时不需要考虑复杂的硬件细节。但是异构系统中各个计算设备在内部体系结构、设备间互联架构上的复杂性和多样性使得异构并行编程模型在提供建立统一的平台抽象上遇到了巨大的困难。具体来说，主要体现下述三点。

首先是异构系统中各个设备之间的并行计算能力不同。在同构的并行计算系统中，比如多核CPU中，虽然同一CPU的不同核之间、同一核的不同SIMD部件之间可以承担不同粒度的并行计算任务，但是其并行计算的能力是完全相同的。但是在一个典型的异构计算系统，例如CPU、GPU和FPGA组成的异构系统，不同设备的微架构具有本质差异，其并行计算的模式和能力都完全不同，设备之间的特长也完全不同。这种设备之间并行计算能力的差异使得系统中的任务划分和任务映射不再是均一的，而是具有显著的特异性。这种特点虽然也有利于表达实际应用的特点，但是却给异构并行计算模型的设计带来了巨大的困难。

![9eb06d8be92ddef3db33e040163c67a7.png](./heterogeneous-programming-model/9eb06d8be92ddef3db33e040163c67a7.png "9eb06d8be92ddef3db33e040163c67a7.png")

其次是异构系统中加速设备数据分布可配置、设备间数据通信渠道多样性给数据分布和通信带来的困难。在同构并行系统中，CPU片内的存储是对于软件透明的缓存架构，在片外则是一个共享内存模型，因此在这类系统中，数据仅可能分布在片外的共享存储中，具有存储位置单一的特点，也不需要进行显式的通信操作。但是在异构系统中，不仅在单个加速设备内部可能有软件可分配的快速局部存储，设备之间的连接方式差异也很大。目前，大多个加速设备都是通过PCIe总线的方式同CPU进行连接，这使得加速设备无法通过和CPU相同的方式完成地址映射，存在某一设备无法访问另一设备片外存储的问题。这使得异构系统中数据可以分布在CPU、加速设备的片外存储和加速设备的片内多层次局部存储等多个位置，不仅使得编程模型的数据分布问题变得十分复杂，设备间的通信文件也可能需要显式进行。

![eab553f9e30d8d866a1ddd201b5e4c85.png](./heterogeneous-programming-model/eab553f9e30d8d866a1ddd201b5e4c85.png)

最后是异构系统中多层次数据共享和多范围同步操作带来的同步困难问题。这也可以认为是上个数据同步问题带来的后继问题：在异构系统中数据可能分布在不同位置的条件下，同步操作需要在众多的位置上保证共享数据的一致性，这使得同步操作的范围变得十分复杂。同时，在一些特定的加速设备中，例如GPU，可能还会有局部的硬件同步机制，这更加提高了在异构系统的同步操作的设计和实现难度。

上层应用带来的挑战主要集中在缺少良好的异构抽象和统一的编程接口上。例如在CPU上进行编程时通常使用Java、Python等高级语言，而在进行GPU编程时则使用各种C语言的变体，其中的核心计算函数（Kernel Function）则通常只支持一个C语言的子集，而FPGA这些硬件设备又需要使用硬件描述语言进行编程。

### 异构并行编程接口和编译/运行时支持机制

异构并行编程接口是编程模型暴露给程序员使用的界面，它既需要为程序员提供合理的异构架构抽象，使程序员可以对异构计算资源加以合理利用，又需要保证接口的易用性，避免程序员陷入复杂的硬件细节中。编译/运行时系统是异构并行编程模型的软件工具层，它将程序员编写的加速器代码编译为可执行文件，并通过运行时系统完成任务的加速执行。

在任务划分、任务映射、数据分布、通信和同步这五个关键任务中，程序员往往只需要关注所编写应用程序的特点，因此显示的任务划分机制对应程序员来说可能是必不可少的，而其他的数据分布、通信和同步等任务只会加剧程序员开发应用程序的负担，但是这些任务通过接口暴露出来也为后续进行深度优化提供了空间。异构编译/运行时支持机制的主要任务就是保障任务映射，即明确任务将具体在哪个设备或者计算单元上执行，以何种顺序执行，同时在当程序员没有显式处理数据分布、通信和同步问题时进行自动处理并进行全系统级别的优化。

## 异构并行编程接口的研究

异构并行编程接口一般可以划分成两类：新设计的异构编程语言和现有语言的异构并行扩展。对于现有语言进行的异构并行扩展一般通过库（Library）或者是制导（Directive）的方法进行。

从异构并行编程接口的功能角度上来说也可以分成两类：有些接口屏蔽了较多的异构并行编程细节，通常仅给程序员提供显式异构任务划分的机制，而数据分布和通信、同步等的工作由运行时系统负责完成，也有些接口将多数异构系统的硬件细节通过上述机制暴露给程序员使用，这在给编程带来更大自由度的同时带来了使用上的困难。

![83ee1d254d638536d0fb4197ff63e758.png](./heterogeneous-programming-model/83ee1d254d638536d0fb4197ff63e758.png)

### 异构任务划分机制研究

在同构的并行编程语言中，并行编程接口需要提供一种面向单一设备的并行任务划分机制，这种并行任务划分机制有**任务并行**和**数据并行**等。数据并行指的是对源集合或者数组的元素同时执行相同操作的场景，一个数据并行的典型例子如下面计算两个矩阵的乘积：

```csharp
	static void MultiplyMatricesParallel(double[,] matA, double[,] matB, double[,] result)
    {
        int matACols = matA.GetLength(1);
        int matBCols = matB.GetLength(1);
        int matARows = matA.GetLength(0);

        // A basic matrix multiplication.
        // Parallelize the outer loop to partition the source array by rows.
        Parallel.For(0, matARows, i =>
        {
            for (int j = 0; j < matBCols; j++)
            {
                double temp = 0;
                for (int k = 0; k < matACols; k++)
                {
                    temp += matA[i, k] * matB[k, j];
                }
                result[i, j] = temp;
            }
        }); // Parallel.For
    }
```

任务并行的概念一般是指一个或者多个独立的任务同时运行，是一种比数据并行更高的抽象层级。

```csharp
public class Result
{
   public static void Main()
   {
        Task<Double>[] taskArray = { Task<Double>.Factory.StartNew(() => DoComputation(1.0)),
                                     Task<Double>.Factory.StartNew(() => DoComputation(100.0)),
                                     Task<Double>.Factory.StartNew(() => DoComputation(1000.0)) };

        var results = new Double[taskArray.Length];
        Double sum = 0;

        for (int i = 0; i < taskArray.Length; i++) {
            results[i] = taskArray[i].Result;
            Console.Write("{0:N1} {1}", results[i],
                              i == taskArray.Length - 1 ? "= " : "+ ");
            sum += results[i];
        }
        Console.WriteLine("{0:N1}", sum);
   }

   private static Double DoComputation(Double start)
   {
      Double sum = 0;
      for (var value = start; value <= start + 10; value += .1)
         sum += value;

      return sum;
   }
}
```

不论是高级或者是低级的异构并行编程接口都需要提供一种异构并行任务的划分机制。同传统的同构并行编程接口只需要提供面向单一设备的并行任务划分机制不同，异构并行编程接口还需要提供描述任务在不同设备间分配的机制。因此，异构并行编程接口的任务划分机制需要包括两个维度：异构特征描述和并行性表达两个维度。

一种典型异构任务划分机制是由`BrookGPU`编程语言提出的。该编程语言采用特殊函数`kernel`标记需要在GPU上执行的代码段，`kernel`函数必须作用在流上。这个流（Stream）在并行性表达方面表达了细粒度的数据并行。后面的OpenCL和CUDA在C语言的基础上提供了异构扩展，这种扩展的任务划分机制和`BrookGPU`的十分类似。但是OpenCL和CUDA在并行行表达的层面上支持了SPMD计算模型，这个`BrookGPU`编程语言采用的流式编程模型不同。OpenCL在数据并行之外还提供了任务并行的机制。

`Lime`则是一门完全新的异构并行编程语言，通过语言结构为程序提供了丰富的操作符用于任务的划分。同时在异构特征描述方面，`Lime`也没有任何显式的接口，同`BrookGPU`等一系列需要手动指定设备代码段的编程模型完全不同，这也是因为`Lime`采用了基于任务的并行划分方式。同时在任务并行之外，`Lime`也通过`MapReduce`操作符提供了中粒度的数据并行机制。

`Merge`还是一门新的异构并行编程语言，基于Intel提出的异构多核多线程系统编程环境`EXOCHI`。在并行性表达上，`Merge`使用`MapReduce`思想。而在异构特征描述方面，`Merge`则提供了一种成为平台变体（Target Variant）的机制，程序员需要为异构系统中的不同设备提供不同版本的代码实现。

### 异构数据分布和通信机制

异构数据分布和通信机制主要分成显式和隐式两种，其中`OpenCL/CUDA`等使用了显式的数据分布的通信机制，为程序员提供了丰富的异构数据分布与通信接口。而`Lime`和`Merge`等语言则使用了隐式机制，运行时系统代为完成这部分的工作。

采用显示异步数据分布和通信机制的主要问题是普通程序员一般无法充分利用这些接口获得性能上的提升。这通常使用因为加速设备通常采用了大量的硬件加速机制，例如GPU的全局内存访存合并机制，这使得程序员如果没有为数据分配合理的存储位置或者设定足够多的线程，会使得加速的效果大打折扣。因此出现了针对这类显式控制语言的优化方法，例如`CUDA-lite`，这个运行时允许程序元在CUDA程序中加入简单的制导语句，数据分布的相关工作使用`CUDA-lite`的运行时系统完成，降低了CUDA程序的编写难度。

![628804b3fe95d39013ff55ae84516d14.png](./heterogeneous-programming-model/Screenshot_20241016_214139.png)

总结一下，为了解决异构系统带来的问题，异构并行编程接口具有如下三个特点：
- 异构任务划分机制在传统并行编程模型的基础上增加了"异构特征描述"的维度，用于描述任务在不同设备上的分配情况；
- 异构数据分布和通知机制在传统并行编程模型的基础上增加了"设备内数据多层分布"和"设备间显式通信"接口；
- 异构同步机制在传统并行编程模型的基础上增加了"设备间同步"的机制。

## 异步编译/运行时的研究

### 异构任务映射机制

异构编程/运行时系统的任务映射机制主要有两种：一类是直接映射，即独立完成并行任务向异构平台映射的工作，另一种是间接映射，即需要借助其他异构编译和运行时系统协助来完成部分任务映射工作。直接映射系统一般在运行时系统中实现，而间接映射通过源到源变换和是运行时分析相结合的方式实现。

![](./heterogeneous-programming-model/Screenshot_20241016_214939.png)

### 异构编译/运行时优化

与同构平台类似，异构编译/运行时优化有两条路径：

- 平台相关的优化，其核心是挖掘系统的硬件优势；
- 应用导向的优化，其核心是实施特定领域的优化并解决应用的输入敏感问题。

在平台优化上，异构系统通常具有复杂且多变的硬件结构, 因此程序员仅负责编写正确实现程序功能的代码、由编译/运行时系统完成面向加速设备结构特点的优化是比较合理的方式, 这样也有利于程序在不同异构系统中获得良好的性能。

## 异构并行编程模型的研究方向

- 面向普通用户的异构并行编程接口
- 面向多种加速设备的异构编译/运行时优化
- 面向异构集群的异构并行编程模型

## 异构并行编程模型调研

为了调研各个异构并行编程模型的不同，使用不同的编程模型实现一个通用矩阵乘法算法，并通过计算`2048*2048`大小的矩阵乘法时间来比较各个模型的加速效果。

辅助计算的`Calculator`类如下所示：

```cpp
#define MATRIX_SIZE 2048
#include <chrono>
#include <functional>
#include <iostream>
#include <random>

class Calculator
{
public:
    static void validate_matrix(const std::vector<std::vector<int>>& a, const std::vector<std::vector<int>>& b)
    {
        for (int i = 0; i < MATRIX_SIZE; i++)
        {
            for (int j = 0; j < MATRIX_SIZE; j++)
            {
                if (a[i][j] != b[i][j])
                {
                    std::cout << "Two matrix must be the same." << std::endl;
                }
            }
        }
    }

    std::vector<std::vector<int>> calculate(const std::string& method,
                                            const std::function<std::vector<std::vector<int>>(
                                                const std::vector<std::vector<int>>&,
                                                const std::vector<std::vector<int>>&)>& calculator) const
    {
        std::cout << "Calculator '" << method << "' start." << std::endl;
        const auto start_time = std::chrono::high_resolution_clock::now();
        const auto result = calculator(a, b);
        const auto end_time = std::chrono::high_resolution_clock::now();
        const auto span = end_time - start_time;

        std::cout << "Calculator '" << method << "' end, time is " << std::chrono::duration_cast<
            std::chrono::milliseconds>(span).count() << " ms." << std::endl;

        return result;
    }
private:
    std::vector<std::vector<int>> a = initialize_matrix();
    std::vector<std::vector<int>> b = initialize_matrix();

    static std::vector<std::vector<int>> initialize_matrix()
    {
        std::vector<std::vector<int>> matrix;
        std::random_device seed;
        std::ranlux48 engine(seed());
        std::uniform_int_distribution distribute(0, 102400);

        for (int i = 0; i < MATRIX_SIZE; i++)
        {
            std::vector row(MATRIX_SIZE, 0);
            for (int j = 0; j < MATRIX_SIZE; j++)
            {
                row[j] = distribute(engine);
            }

            matrix.emplace_back(row);
        }

        return matrix;
    }
}
```

作为对比，一个使用CPU单线程计算的例子如下：

```cpp
std::vector<std::vector<int>> matrix_multiply(
    const std::vector<std::vector<int>>& a,
    const std::vector<std::vector<int>>& b)
{
    std::vector result(MATRIX_SIZE, std::vector(MATRIX_SIZE, 0));

    for (int i = 0; i < MATRIX_SIZE; i++)
    {
        for (int j = 0; j < MATRIX_SIZE; j++)
        {
            int temp = 0;
            for (int k = 0; k < MATRIX_SIZE; k++)
            {
                temp += a[i][k] * b[k][j];
            }
            result[i][j] = temp;
        }
    }

    return result;
}
```

### OpenMP

OpenMP是`Opem MultiProcessing`的缩写，是一个使用编译器制导（Directives）来进行共享内存平行计算的框架，在C、C++和Fortran语言的并行编程中得到的了广泛的应用。OpenMP提供了一个简单而灵活的接口，让程序员能够充分释放多核和多处理器系统性能。

OpenMP从上面的介绍来看似乎并不是一个严格的异步并行编程模型，但是第一，OpenMP作为一个经典的并行编程框架，研究价值还是非常高的，其次在一些较新的OpenMP版本中其宣称也能利用NVIDIA GPU进行加速，似乎也能算是一个异构并行编程模型。

使用OpenMP进行并行加速的代码如下：

```C++
std::vector<std::vector<int>> omp_matrix_multiply(
    const std::vector<std::vector<int>>& a,
    const std::vector<std::vector<int>>& b)
{
    std::vector result(MATRIX_SIZE, std::vector(MATRIX_SIZE, 0));

#pragma omp parallel for shared(a, b, result) default(none)
    for (int i = 0; i < MATRIX_SIZE; i++)
    {
        for (int j = 0; j < MATRIX_SIZE; j++)
        {
            int temp = 0;
            for (int k = 0; k < MATRIX_SIZE; k++)
            {
                temp += a[i][k] * b[k][j];
            }
            result[i][j] = temp;
        }
    }

    return result;
}
```

加速的结果如下：

| 运行方法     | 运行时间 | 比率 |
| ------------ | -------- | ---- |
| SingleThread | 76823 ms | 1.00 |
| OpenMP       | 8324 ms  | 0.10 |

### CUDA

CUDA是NVIDIA公司设计的一套GPU加速应用程序的编程框架，是将NVIDIA GPU作为GPGPU使用的官方解决方案。

CUDA的异构编程接口是经典的Device-Host两元结构，程序员需要编写两部分代码，Device代码是实际运行在GPU上的逻辑部分，而Host代码则负责将数据从内存中复制到GPU上的显存和复制回来等准备工作，并负责以特定的参数调用GPU上的Device代码。

一个使用GPU的矩阵乘法程序如下所示：

```c++
template <typename T>
void check(T result, char const* const func, const char* const file, int const line)
{
    if (result)
    {
        std::cerr << "CUDA error at " << file << ":" << line << "code = " << result << "(" << cudaGetErrorString(result)
            << ") '" << func << "'" << std::endl;
        exit(EXIT_FAILURE);
    }
}

#define checkCudaErrors(val) check((val), #val, __FILE__, __LINE__)

__global__ void cudaMatrixMultiply(const int* a, const int* b, int* c)
{
    const int totalSize = MATRIX_SIZE * MATRIX_SIZE;
    int threadId = threadIdx.x + blockIdx.x * blockDim.x;

    while (threadId < totalSize)
    {
        const int x = threadId / MATRIX_SIZE;
        const int y = threadId % MATRIX_SIZE;

        int result = 0;

        for (int i = 0; i < MATRIX_SIZE; i++)
        {
            result += a[x * MATRIX_SIZE + i] * b[i * MATRIX_SIZE + y];
        }

        c[MATRIX_SIZE * x + y] = result;
        threadId += gridDim.x * blockDim.x;
    }

    __syncthreads();
}

std::vector<std::vector<int>> cudaCalculateMatrix(const std::vector<std::vector<int>>& a,
                                                  const std::vector<std::vector<int>>& b)
{
    constexpr unsigned int matrixSize = sizeof(int) * MATRIX_SIZE * MATRIX_SIZE;

    // 在host上为a, b, c分配空间
    int *hostA, *hostB, *hostC;
    checkCudaErrors(cudaMallocHost(&hostA, matrixSize));
    checkCudaErrors(cudaMallocHost(&hostB, matrixSize));
    checkCudaErrors(cudaMallocHost(&hostC, matrixSize));

    // 将数据复制到host上
    for (int i = 0; i < MATRIX_SIZE; i++)
    {
        for (int j = 0; j < MATRIX_SIZE; j++)
        {
            hostA[i * MATRIX_SIZE + j] = a[i][j];
            hostB[i * MATRIX_SIZE + j] = b[i][j];
        }
    }

    // 在device上分配空间
    int *deviceA, *deviceB, *deviceC;
    checkCudaErrors(cudaMalloc(reinterpret_cast<void**>(&deviceA), matrixSize));
    checkCudaErrors(cudaMalloc(reinterpret_cast<void**>(&deviceB), matrixSize));
    checkCudaErrors(cudaMalloc(reinterpret_cast<void**>(&deviceC), matrixSize));

    cudaStream_t stream;

    checkCudaErrors(cudaStreamCreateWithFlags(&stream, cudaStreamNonBlocking));

    // 将数据从host复制到device
    checkCudaErrors(cudaMemcpyAsync(deviceA, hostA, matrixSize, cudaMemcpyHostToDevice, stream));
    checkCudaErrors(cudaMemcpyAsync(deviceB, hostB, matrixSize, cudaMemcpyHostToDevice, stream));

    constexpr int threadSize = 32 * 32;
    constexpr int grid = MATRIX_SIZE * MATRIX_SIZE / threadSize;

    cudaEvent_t start, stop;
    cudaEventCreate(&start);
    cudaEventCreate(&stop);

    cudaStreamSynchronize(stream);
    cudaEventRecord(start, stream);

    cudaMatrixMultiply<<<grid, threadSize, 0, stream>>>(deviceA, deviceB, deviceC);

    cudaEventRecord(stop, stream);
    cudaEventSynchronize(stop);

    float cudaRunTime = 0;
    cudaEventElapsedTime(&cudaRunTime, start, stop);
    std::cout << "CUDA actual run time is " << cudaRunTime << " ms" << std::endl;

    // 将数据从device复制到host
    cudaMemcpyAsync(hostC, deviceC, matrixSize, cudaMemcpyDeviceToHost, stream);
    cudaStreamSynchronize(stream);

    std::vector<std::vector<int>> result;

    for (int i = 0; i < MATRIX_SIZE; i++)
    {
        std::vector<int> row;
        for (int j = 0; j < MATRIX_SIZE; j++)
        {
            row.emplace_back(hostC[i * MATRIX_SIZE + j]);
        }
        result.emplace_back(row);
    }

    // 释放内存
    cudaFreeHost(hostA);
    cudaFreeHost(hostB);
    cudaFreeHost(hostC);
    cudaFree(deviceA);
    cudaFree(deviceB);
    cudaFree(deviceC);
    cudaEventDestroy(start);
    cudaEventDestroy(stop);
    cudaStreamDestroy(stream);

    return result;
}
```

加速的结果如下所示：

| 类型 | 运行时间 | 加速比 |
| ---- | -------- | ------ |
| CPU  | 35245ms  | 1.00   |
| GPU  | 320ms    | 0.01   |

需要注意的是，上面编写的CUDA代码还没有完全利用GPU的并行计算能力。

> 这里我遇到的一个非常奇怪的问题是，相同的CPU计算代码，在运行完OpenMP测试之后再运行就会比在CUDA运行之后再运行慢上一倍，而且可复现性极高。这里我给出一个典型的运行时间比较：CUDA计算的时间是323毫秒，CUDA之后的CPU计算时间是38602毫秒，OpenMP的计算时间是8721毫秒，OpenMP之后的计算时间是76598毫秒。
>
> 针对这个比较奇怪的情况我觉得可以做出三个猜想：
>
> - 考虑到我使用的CPU是Intel的i7-13600K，这是一个有性能核和效率核组成的大小核异构系统，可能在两次计算的过程中调度到了不同的核上；
> - 在进行CUDA计算的过程中提高了缓存的亲和性；
> - 在测试中没有设计热身（Warm up）的过程，而在CUDA计算的过程中部分起到了这个作用。
>
> 针对上面三个猜测做个两个实验：
>
> - 首先是换了一台没有大小核异构设计的计算机进行实验，发现这下两次使用CPU计算的时间差异不大；
> - 加上了热身的阶段之后，计算时间没有发生明显的变化。
>
> 综上所述，可以认为此现象和异构CPU之间存在这明显的关联，但是缺乏直接证据。

### OpenCL

OpenCL是目前最为典型、发展最好的异构并行编程模型，毕竟其在官网的第一句话就是“为异构系统中并行编程的开放标准“。

![image-20241020142938110](./heterogeneous-programming-model/image-20241020142938110.png)

从上图的OpenCL工作原理中可以看出，OpenCL和CUDA类似，也采用了Device-Host类型的编程接口。主机代码通常通过普通的C/C++代码进行编写，编译之后在CPU上执行，而设备代码使用一个特定的C语言方言OpenCL C进行编写，这个方言针对并行编程进行了扩展，并提供了一系列封装好的数学计算函数。

设备代码上的编译方法有两种：在线编译和离线编译。其中在线编译就是指在程序运行时由对应设备厂商开发的OpenCL驱动将设备代码编译为在对应设备上运行的可执行代码，离线编译则有两种表现形式，第一种是在线编译的扩展版，由驱动编译得到的可执行程序可以通过API获取并保存下来，当下一需要在同一设备上调用时可以直接使用而不是再次编译，第二种则是完全独立的编译过程，在OpenCL程序运行之前使用单独的编译工具编译得到可执行文件。

![image-20241020155656219](./heterogeneous-programming-model/image-20241020155656219.png)

在提出离线编译之后，为了让驱动编译好的二进制文件可以在不同的设备之间复用，同时也是支持更为丰富的编译器生态系统，OpenCL的提出者Khronos设计了一种跨设备的、可迁移的中间表示形式[SPIRV](https://www.khronos.org/spir/)。这种中间形式的提出使得编程语言的提出者、编译器的开发人员可以直接将语言编译为`SPIRV`内核，这样就可以在任何支持`SPIRV`的OpenCL驱动上运行。下面将会介绍的`SYCL`和`Julia`语言都是基于`SPIRV`的中间语言进行构建的。`SPIRV`中间语言的提出也扩展了可以支持`OpenCL`的设备范围，现在已经有开发者和公司在探索将`SPIRV`编译到`Vulkan`、`DirectX`和`Metal`等传统意义上的图形API。

下面是一个使用OpenCL进行矩阵计算的例子。



### SYCL



### OpenAcc



### Triton



### oneAPI



### Julia






## 参考文献

1. 刘颖,吕方,王蕾,陈莉,崔慧敏,冯晓兵.异构并行编程模型研究与进展.软件学报,2014,25(7):1459-1475. http://www.jos.org.cn/1000-9825/4608.htm
2. 

