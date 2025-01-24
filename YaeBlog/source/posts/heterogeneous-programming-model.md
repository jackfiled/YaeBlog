---
title: 异构编程模型的昨天、今天与明天
date: 2024-11-04T22:20:41.2571467+08:00
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

![9eb06d8be92ddef3db33e040163c67a7.png](./heterogeneous-programming-model/9eb06d8be92ddef3db33e040163c67a7.png)

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
inline std::vector<int> cpuMatrixMultiply(
    const std::vector<int>& a,
    const std::vector<int>& b)
{
    std::vector result(MATRIX_SIZE * MATRIX_SIZE, 0);

    for (int i = 0; i < MATRIX_SIZE; i++)
    {
        for (int j = 0; j < MATRIX_SIZE; j++)
        {
            int temp = 0;
            for (int k = 0; k < MATRIX_SIZE; k++)
            {
                // a[i][j] = a[i][k] * b[k][j] where k in (0..MATRIX_SIZE)
                temp += a[i * MATRIX_SIZE + k] * b[k * MATRIX_SIZE + j];
            }
            result[i * MATRIX_SIZE + j] = temp;
        }
    }

    return result;
}
```

### OpenMP

OpenMP是`Open MultiProcessing`的缩写，是一个使用编译器制导（Directives）来进行共享内存平行计算的框架，在C、C++和Fortran语言的并行编程中得到的了广泛的应用。OpenMP提供了一个简单而灵活的接口，让程序员能够充分释放多核和多处理器系统性能。

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
| SingleThread | 21685 ms | 1.00 |
| OpenMP       | 2268 ms  | 0.10 |

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

| 类型 | 运行时间 | 比率  |
| ---- | -------- | ----- |
| CPU  | 22059ms  | 1.000 |
| GPU  | 32ms     | 0.001 |

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
> 综上所述，可以认为此现象和异构CPU之间存在着明显的关联，但是缺乏直接证据。
>
> 在我们调整了矩阵的数据布局之后，这里提到的实验结果又发生了变化。上面的实验结果是使用二维数据存储矩阵得到的，而在修改为使用一维数组（也就是现在提供的代码）之后，相同的CPU计算代码的计算时间又没有产生明显的变化了。看来这个问题可能和数据布局、CPU缓存等问题相关。

### OpenCL

OpenCL是目前最为典型、发展最好的异构并行编程模型，毕竟其在官网的第一句话就是“为异构系统中并行编程的开放标准“。

![image-20241020142938110](./heterogeneous-programming-model/image-20241020142938110.png)

从上图的OpenCL工作原理中可以看出，OpenCL和CUDA类似，也采用了Device-Host类型的编程接口。主机代码通常通过普通的C/C++代码进行编写，编译之后在CPU上执行，而设备代码使用一个特定的C语言方言OpenCL C进行编写，这个方言针对并行编程进行了扩展，并提供了一系列封装好的数学计算函数。

设备代码上的编译方法有两种：在线编译和离线编译。其中在线编译就是指在程序运行时由对应设备厂商开发的OpenCL驱动将设备代码编译为在对应设备上运行的可执行代码，离线编译则有两种表现形式，第一种是在线编译的扩展版，由驱动编译得到的可执行程序可以通过API获取并保存下来，当下一需要在同一设备上调用时可以直接使用而不是再次编译，第二种则是完全独立的编译过程，在OpenCL程序运行之前使用单独的编译工具编译得到可执行文件。

![image-20241020155656219](./heterogeneous-programming-model/image-20241020155656219.png)

在提出离线编译之后，为了让驱动编译好的二进制文件可以在不同的设备之间复用，同时也是支持更为丰富的编译器生态系统，OpenCL的提出者Khronos设计了一种跨设备的、可迁移的中间表示形式[SPIRV](https://www.khronos.org/spir/)。这种中间形式的提出使得编程语言的提出者、编译器的开发人员可以直接将语言编译为`SPIRV`内核，这样就可以在任何支持`SPIRV`的OpenCL驱动上运行。下面将会介绍的`SYCL`和`Julia`语言都是基于`SPIRV`的中间语言进行构建的。`SPIRV`中间语言的提出也扩展了可以支持`OpenCL`的设备范围，现在已经有开发者和公司在探索将`SPIRV`编译到`Vulkan`、`DirectX`和`Metal`等传统意义上的图形API。

下面是一个使用OpenCL进行矩阵计算的例子。

```cpp
struct ComputationContext
{
    cl_platform_id platform;
    cl_device_id device;
};

static std::unique_ptr<ComputationContext> selectDevice()
{
    cl_uint platformCount;
    checkOpenCLError(clGetPlatformIDs(0, nullptr, &platformCount));
    std::cout << "Platform count: " << platformCount << std::endl;

    std::vector<cl_platform_id> platforms(platformCount);
    checkOpenCLError(clGetPlatformIDs(platformCount, platforms.data(), nullptr));

    std::unique_ptr<ComputationContext> selectedDevice = nullptr;

    for (const auto& platform : platforms)
    {
        cl_uint deviceCount = 0;
        checkOpenCLError(clGetDeviceIDs(platform, CL_DEVICE_TYPE_ALL, 0, nullptr, &deviceCount));

        std::vector<cl_device_id> devices(deviceCount);
        checkOpenCLError(clGetDeviceIDs(platform, CL_DEVICE_TYPE_ALL, deviceCount, devices.data(), nullptr));

        for (const auto& device : devices)
        {
            size_t deviceNameLength;
            checkOpenCLError(clGetDeviceInfo(device, CL_DEVICE_NAME, 0, nullptr, &deviceNameLength));

            std::vector<char> deviceNameArray(deviceNameLength);
            checkOpenCLError(
                clGetDeviceInfo(device, CL_DEVICE_NAME, deviceNameLength, deviceNameArray.data(), nullptr));

            std::string deviceName(deviceNameArray.data(), deviceNameArray.size() - 1);

            std::cout << "Found device: " << deviceName << std::endl;

            if (deviceName.find("4060") != std::string::npos)
            {
                std::cout << "Select device '" << deviceName << "' as runner." << std::endl;
                selectedDevice = std::make_unique<ComputationContext>();
                selectedDevice->platform = platform;
                selectedDevice->device = device;
            }
            else
            {
                clReleaseDevice(device);
            }
        }
    }

    if (selectedDevice == nullptr)
    {
        std::cout << "Failed to find the target device." << std::endl;
        std::exit(EXIT_FAILURE);
    }

    return selectedDevice;
}

std::vector<int> clCalculateMatrix(const std::vector<int>& a,
                                   const std::vector<int>& b)
{
    cl_int error;

    const std::unique_ptr<ComputationContext> computationContext = selectDevice();
    // A key-value list ends with 0
    // See also https://www.khronos.org/registry/OpenCL/specs/3.0-unified/html/OpenCL_API.html#context-properties-table
    std::array<cl_context_properties, 3> properties = {
        CL_CONTEXT_PLATFORM,
        reinterpret_cast<cl_context_properties>(computationContext->platform),
        0
    };

    cl_context context = clCreateContext(properties.data(), 1, &computationContext->device, nullptr, nullptr,
                                         &error);
    checkOpenCLError(error);
    cl_command_queue queue = clCreateCommandQueueWithProperties(context, computationContext->device, nullptr,
                                                                &error);
    checkOpenCLError(error);

    std::vector result(MATRIX_SIZE * MATRIX_SIZE, 0);
    constexpr size_t matrixSize = MATRIX_SIZE * MATRIX_SIZE * sizeof(int);

    cl_mem deviceA = clCreateBuffer(context, CL_MEM_READ_ONLY, matrixSize, nullptr, &error);
    checkOpenCLError(error);
    cl_mem deviceB = clCreateBuffer(context, CL_MEM_READ_ONLY, matrixSize, nullptr, &error);
    checkOpenCLError(error);
    cl_mem deviceC = clCreateBuffer(context, CL_MEM_READ_WRITE, matrixSize, nullptr, &error);
    checkOpenCLError(error);

    checkOpenCLError(
        clEnqueueWriteBuffer(queue, deviceA, CL_TRUE, 0, matrixSize, a.data(), 0, nullptr,
            nullptr));
    checkOpenCLError(
        clEnqueueWriteBuffer(queue, deviceB, CL_TRUE, 0, matrixSize, b.data(), 0, nullptr,
            nullptr));
    // Copy result to erase the previous result
    checkOpenCLError(
        clEnqueueWriteBuffer(queue, deviceC, CL_TRUE, 0, matrixSize, result.data(), 0,
            nullptr, nullptr
        ));

    auto source = R"(
#define MATRIX_SIZE 2048

__kernel void calculate(const __global int* a, const __global int* b, __global int* c)
{
    const int x = get_global_id(0);
    const int y = get_global_id(1);

    int result = 0;
    for (int i = 0; i < MATRIX_SIZE; i++)
    {
        result += a[x * MATRIX_SIZE + i] * b[i * MATRIX_SIZE + y];
    }

    c[x * MATRIX_SIZE + y] = result;
})";

    cl_program program = clCreateProgramWithSource(context, 1, &source, nullptr, &error);
    checkOpenCLError(error);
    checkOpenCLError(clBuildProgram(program, 0, nullptr, "", nullptr, nullptr));

    size_t messageSize;
    checkOpenCLError(
        clGetProgramBuildInfo(program, computationContext->device, CL_PROGRAM_BUILD_LOG, 0, nullptr, &messageSize));
    std::vector<char> messageArray(messageSize);
    checkOpenCLError(
        clGetProgramBuildInfo(program, computationContext->device, CL_PROGRAM_BUILD_LOG, messageSize, messageArray.data(
        ), nullptr));
    std::string message(messageArray.data(), messageSize - 1);
    std::cout << "Build log: " << message << std::endl;

    cl_kernel kernel = clCreateKernel(program, "calculate", &error);
    checkOpenCLError(error);

    checkOpenCLError(clSetKernelArg(kernel, 0, sizeof(cl_mem), &deviceA));
    checkOpenCLError(clSetKernelArg(kernel, 1, sizeof(cl_mem), &deviceB));
    checkOpenCLError(clSetKernelArg(kernel, 2, sizeof(cl_mem), &deviceC));

    cl_event event;
    constexpr std::size_t globalSize[2] = {MATRIX_SIZE, MATRIX_SIZE};
    checkOpenCLError(clEnqueueNDRangeKernel(queue, kernel, 2, nullptr,
        globalSize, nullptr, 0, nullptr, &event));

    checkOpenCLError(clWaitForEvents(1, &event));

    checkOpenCLError(
        clEnqueueReadBuffer(queue, deviceC, CL_TRUE, 0, matrixSize, result.data(), 0,
            nullptr, nullptr));

    clReleaseMemObject(deviceA);
    clReleaseMemObject(deviceB);
    clReleaseMemObject(deviceC);

    clReleaseKernel(kernel);
    clReleaseProgram(program);
    clReleaseCommandQueue(queue);
    clReleaseContext(context);
    clReleaseDevice(computationContext->device);
    return result;
}
```

从上面的代码中可以看出两点：

- OpenCL的编程比CUDA的更为繁琐，因为OpenCL支持的设备种类更多，在主机代码上还需要多出一块选择运行设备的代码；
- OpenCL在主机代码和核函数的解耦更为彻底，核函数直接以字符串的形式存在于主机代码中，而各个厂商提供的驱动才是真正的编译器。

测试的运行结果如下：

| 类型                          | 运行时间 | 比率 |
| ----------------------------- | -------- | ---- |
| NVIDIA 4060 Ti OpenCL         | 173ms    | 0.01 |
| Intel UHD Graphics 770 OpenCL | 1020ms   | 0.04 |
| CPU                           | 21255ms  | 1.00 |

### SYCL

SYCL是一个使用标准C++编写在各种异构计算设备上运行核函数的抽象层，并提供了一套新的API来查找各种设备并管理这些设备上的内存资源和代码执行。这个标准是开发、无版税、跨平台的抽象标准。同时也是因为这是一个**标准**，因此需要寻找支持这个标准的编译器才能使用这个标准。按照官网上的说明，我们选择了两个看上去还在活跃开发的项目，Intel的[oneAPI](https://www.intel.com/content/www/us/en/developer/tools/oneapi/overview.html)和开源的[AdaptiveCpp](https://github.com/AdaptiveCpp/AdaptiveCpp)进行调研，考虑到在后文中还将继续介绍oneAPI相关的工作，因此这里将重点放在AdaptiveCpp上。

AdaptiveCpp由四个部分组成，分别在不同的C++命名空间中提供。

- SYCL Interface：实现了SYCL标准中规定的各种类和函数，是实际上同用户交互的接口。这些接口实际上可以仍然可以分成主机API和核函数库两个部分。主机API是普通的C++代码，负责任务调度、任务管理和平台射别管理等。核函数库包括了这种在编写核函数时可以使用的类和函数，这些接口暴露一些后端特定的功能，其中的一些甚至需要使用后端特定的方言来编写，例如CUDA。

- AdaptiveCpp Runtime：运行时实际上实现了设备调度、任务图管理和执行、数据管理、后端管理、任务调度和同步等等功能，运行时负责同各种支持后端的运行时交互来实现上述的功能。

  ![image-20241029123308139](./heterogeneous-programming-model/image-20241029123308139.png)

- Compiler：考虑到在用户编写的代码中可能使用一些特定后端的方言，因此普通的C++编译器无法正常编译所有的用户代码。因此用户代码的编译是通过一个名为`acpp`的Python脚本驱动的，这个脚本将各个后端的不同编译器暴露为一个统一的编程接口。

- Glue：将上述的各个部分连接在一起的胶水代码。一种典型的胶水代码是内核函数的启动代码`kernel launcher`，由于启动器中往往涉及到一些后端特定的方言，例如CUDA中的`<<<>>>`或者OpenMP中的各种`pragma`，因此这些代码通常需要使用特定的编译器进行编译，所以这些胶水代码直接以头文件的方式提供，以方便在编译时被特定的编译器处理。这些胶水代码将会把核函数包裹为一个合法的C++函数对象，这样运行时就可以获得这个函数对象并控制代码在设备上的运行。

AdaptiveCpp同时支持多种不同的编译流程。

1. 一种通用的一遍编译流程，将核函数编译到一种统一的中间表示形式，这种中间表示形式将在运行时被编译到特定的后端架构上。这种编译流程提供了高度的可移植性和较快的编译速度。这种编译设施支持的后端有：通过`PTX`在NVIDIA的GPU上运行，通过`amdgcn`在AMD的GPU上运行，通过`SPIR-V`在Intel的GPU上运行，通过`SPIR-V`在任何支持OpenCL驱动的设备上运行，也可以通过LLVM直接在CPU上运行。
2. 一种为互操作性优化的多遍编译流程，在这个流程中AdaptiveCpp将聚合现有的各种LLVM/Clang的编译工具链，使得用户可以在单个代码文件中混合编写SYCL和各种特定的编程模型，例如CUDA和HIP。使用这个编译流程的好处有亮点：（1）在这种编译流程中可以直接在SYCL代码使用各个特定编译模型中提供最新设备内部优化（Intrinsics），不用等待SYCL标准的支持；（2）在这种编译流程中可以使用各个厂商提供的优化模板库，例如`rocPRIM`和`CUB`。这种编译流程是提供聚合`CUDA`的clang前端和`ROCm`的clang前端来实现的。
3. 一种只将AdaptiveCpp作为函数使用的编程流程。在这种情况AdaptiveCpp作为一个三方库被引入其他的编译器编译流程中。

第一种通用的编译流程显然是泛用性最广的一种编译流程，同时也是AdaptiveCpp推荐的编译流程。

![image-20241029163654675](./heterogeneous-programming-model/image-20241029163654675.png)

下面是一段使用SYCL进行矩阵乘法加速的代码：

```cpp
struct CustomDeviceSelector
{
    explicit CustomDeviceSelector(std::string vendorName) : _vendorName(std::move(vendorName))
    {
    }

    int operator()(const sycl::device& d) const
    {
        int deviceRating = 0;

        if (d.is_gpu() && d.get_info<sycl::info::device::name>().find(_vendorName) != std::string::npos)
        {
            deviceRating = 3;
        }
        else if (d.is_cpu())
        {
            deviceRating = 1;
        }

        return deviceRating;
    }

private:
    std::string _vendorName;
};

static std::vector<int> syclCalculateMatrix(const std::vector<int>& a, const std::vector<int>& b,
                                            const std::string& hint)
{
    const CustomDeviceSelector selector(hint);
    sycl::queue queue(selector);

    const std::string deviceName = queue.get_device().get_info<sycl::info::device::name>();
    std::cout << "Select device: " << deviceName << std::endl;

    std::vector result(MATRIX_SIZE * MATRIX_SIZE, 0);

    sycl::buffer aBuffer(a);
    sycl::buffer bBuffer(b);
    sycl::buffer resultBuffer(result);

    queue.submit([&](sycl::handler& h)
    {
        const sycl::accessor aBufferAccessor(aBuffer, h, sycl::read_only);
        const sycl::accessor bBufferAccessor(bBuffer, h, sycl::read_only);
        const sycl::accessor resultBufferAccessor(resultBuffer, h, sycl::write_only);

        h.parallel_for(sycl::nd_range<2>({MATRIX_SIZE, MATRIX_SIZE}, {16, 16}), [=](const sycl::nd_item<2>& item)
        {
            const size_t x = item.get_global_id(0);
            const size_t y = item.get_global_id(1);

            int temp = 0;
            for (size_t k = 0; k < MATRIX_SIZE; ++k)
            {
                temp += aBufferAccessor[x * MATRIX_SIZE + k] * bBufferAccessor[k * MATRIX_SIZE + y];
            }
            resultBufferAccessor[x * MATRIX_SIZE + y] = temp;
        });
    });

    sycl::host_accessor resultHostAccessor(resultBuffer, sycl::read_only);

    for (size_t i = 0; i < MATRIX_SIZE; ++i)
    {
        for (size_t j = 0; j < MATRIX_SIZE; ++j)
        {
            result[i * MATRIX_SIZE + j] = resultHostAccessor[i * MATRIX_SIZE + j];
        }
    }

    return result;
}
```

测试之后的运行结果如下所示：

| 类型                        | 运行时间 | 比率  |
| --------------------------- | -------- | ----- |
| Intel UHD Graphics 770 SYCL | 488ms    | 0.023 |
| NVIDIA 4060 Ti SYCL         | 180ms    | 0.008 |
| OpenMP SYCL                 | 1591ms   | 0.076 |
| CPU                         | 20930ms  | 1.000 |

### OpenACC

OpenACC是一个通过编译器制导来在代码中表达并行性并利用并行编译器为多个并行加速器生成代码的编程模型。为了保证OpenACC可以适配于各种计算架构的加速设备，OpenACC设计了一个各种并行层次和有着不同速度和寻址方式内存的编程模型。同时OpenACC主要的功能即是支持同时将计算和数据卸载到一个加速设备上，考虑到加速设备可能有着同宿主设备完全不同的内存架构，OpenACC编译器和运行时将会自动分析代码并负责加速器上内存的管理和加速器和主机之间的数据传输。

作为一个高等级、平台独立的加速器编程框架，使用OpenACC进行开发能够使开发人员将一个源代码编译到一系列设备上运行并实现一个相对较好的性能，但是这个简易性和移植性也在一定程度上造成使用OpenACC编程无法完全利用加速设备上的算力。

OpenACC是作为一个标准的形式提供的，实现了该标准的编译器有：

| 编译器名称                                                   | 情况                                                         |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| NVIDIA HPC SDK                                               | 支持在NVIDIA GPU和多核CPU上的OpenACC并行编程                 |
| Sourcery CodeBench Lite                                      | OpenACC官网上说支持针对AMD GPU的编译，但是官网页面似乎改版了，没有找到相关的内容 |
| GCC 12                                                       | 支持到OpenACC 2.6                                            |
| [Omni Compiler Project](https://github.com/omni-compiler/omni-compiler) | 源到源编译器，将带有制导的源代码翻译到带有运行时调用的平台代码，近两年没有活跃开发 |
| [OpenUH](https://github.com/uhhpctools/openuh)               | 项目开发者在7年前的最后一次提交了中删除了README中有关OpenACC的内容 |
| [OpenArc](https://csmd.ornl.gov/project/openarc-open-accelerator-research-compiler) | 是学术界出品的还在活跃开发的编译器，看上去还做了不少工作的样子，就是OpenACC官网上的链接已经失效了找起来比较麻烦，而且宣称是一个开源编译器，但是获取源代码和二进制文件需要联系他们（美国橡树岭国家实验室）创建账户，这看去对于我们这些Foreign Adversary有些抽象了。 |

在试验OpenACC时遇到了巨大的困难，不论是使用gcc还是NVIDIA HPC SDK都没有办法实现明显的并行编程加速，多次实验之后都没有找到的问题的所在。这里还是贴一下实验的代码和实验的数据。

实验中编写的OpenACC加速代码如下：

```cpp
static std::vector<int> OpenACCCpuCalculateMatrix(const std::vector<int>& a, const std::vector<int>& b)
{
    constexpr int length = MATRIX_SIZE * MATRIX_SIZE;

    const auto aBuffer = new int[length];
    const auto bBuffer = new int[length];
    const auto cBuffer = new int[length];

    for (int i = 0; i < length; i++)
    {
        aBuffer[i] = a[i];
        bBuffer[i] = b[i];
        cBuffer[i] = 0;
    }

#pragma acc enter data copyin(aBuffer[0:length], bBuffer[0:length])
#pragma acc enter data create(bBuffer[0:length])
#pragma acc data present(aBuffer[0:length], bBuffer[0:length], cBuffer[0:length])
    {
#pragma acc kernels loop independent
        for (int i = 0; i < MATRIX_SIZE; i++)
        {
#pragma acc loop independent
            for (int j = 0; j < MATRIX_SIZE; j++)
            {
                int temp = 0;
#pragma acc loop independent reduction(+:temp)
                for (int k = 0; k < MATRIX_SIZE; k++)
                {
                    temp += aBuffer[i * MATRIX_SIZE + k] * bBuffer[k * MATRIX_SIZE + j];
                }
                cBuffer[i * MATRIX_SIZE + j] = temp;
            }
        }
    }
#pragma acc exit data copyout(cBuffer[0:length])
#pragma acc exit data delete(aBuffer[0:length], bBuffer[0:length])

    std::vector result(MATRIX_SIZE * MATRIX_SIZE, 0);

    for (int i = 0; i < length; ++i)
    {
        result[i] = cBuffer[i];
    }

    delete[] aBuffer;
    delete[] bBuffer;
    delete[] cBuffer;

    return result;
}
```

实验中使用分别使用`NVIDIA HPC SDK`和`GCC`编译运行的结果如下：

| 编译器         | 类型    | 运行时间 |
| -------------- | ------- | -------- |
| NVIDIA HPC SDK | OpenACC | 19315ms  |
| NVIDIA HPC SDK | CPU     | 22942ms  |
| GCC            | OpenACC | 19999ms  |
| GCC            | CPU     | 22623ms  |

### oneAPI

oneAPI是Intel公司提出的一套异构并行编程框架，该框架致力于达成如下几个目标：（1）定义一个跨架构、跨制造商的统一开放软件平台；（2）允许同一套代码可以在不同硬件制造商和加速技术的硬件上运行；（3）提供一套覆盖多个编程领域的库API。为了实现这些目标，oneAPI同上文中已经提到过的开放编程标准SYCL紧密合作，oneAPI也提供了一个SYCL的编译器和运行时；同时oneAPI也提供了一系列API库，包括`oneDPL`、`oneDNN`、`oneTBB`和`oneMKL`等。

![image-20241103162259981](./heterogeneous-programming-model/image-20241103162259981.png)

我对于oneAPI的理解就是Intel用来对标NVIDIA的CUDA的一套高性能编程工具箱。首先为了和NVIDIA完全闭源的CUDA形成鲜明的对比，Intel选择了OpenCL合作同时开发SYCL，当时也有可能是Intel知道自己的显卡技不如人，如果不兼容市面上其他的部件是没有出路的，同时为了和CUDA丰富的生态竞争，Intel再开发并开源了一系列的`oneXXX`。

这里我就把上面SYCL写的例子用Intel提供的`DPC++`编译运行一下，看看在效率上会不会有所变化。

| 类型                          | 运行时间 | 比率  |
| ----------------------------- | -------- | ----- |
| Intel UHD Graphics 770 oneAPI | 429ms    | 0.023 |
| NVIDIA 4060 Ti oneAPI         | 191ms    | 0.010 |
| Intel i5-13600K oneAPI        | 198ms    | 0.011 |
| CPU                           | 18643ms  | 1.000 |

在显卡上的计算时间没有明显的变化，但是我们Intel的编译器却在选择到使用Intel CPU进行计算时展现了不俗的实力。


## 参考文献

1. 刘颖,吕方,王蕾,陈莉,崔慧敏,冯晓兵.异构并行编程模型研究与进展.软件学报,2014,25(7):1459-1475. [http://www.jos.org.cn/1000-9825/4608.htm](http://www.jos.org.cn/1000-9825/4608.htm)
2. AdaptiveCpp官方文档. [https://adaptivecpp.github.io/AdaptiveCpp/](https://adaptivecpp.github.io/AdaptiveCpp/)
3. Exploring the performance of SGEMM in OpenCL on NVIDIA GPUs. [https://github.com/CNugteren/myGEMM](https://github.com/CNugteren/myGEMM)
4. OpenACC Programming and Best Practices Guide. [https://openacc-best-practices-guide.readthedocs.io/en/latest/01-Introduction.html](https://openacc-best-practices-guide.readthedocs.io/en/latest/01-Introduction.html)
5. oneAPI What is it?. [https://www.intel.com/content/www/us/en/developer/articles/technical/oneapi-what-is-it.html](https://www.intel.com/content/www/us/en/developer/articles/technical/oneapi-what-is-it.html)

