---
title: High Performance Computing 25 SP NVIDIA
date: 2025-08-31T13:50:42.8639950+08:00
tags:
- 高性能计算
- 学习资料
---



Fxxk you, NVIDIA!

<!--more-->

CPU/GPU Parallelism:

Moore's Law gives you more and more transistors:

- CPU strategy: make the workload (one compute thread) run as fast as possible.
- GPU strategy: make the workload (as many threads as possible) run as fast as possible.

GPU Architecture:

- Massively Parallel
- Power Efficient
- Memory Bandwidth
- Commercially Viable Parallelism
- Not dependent on large caches for performance

![image-20250424192311202](./hpc-2025-cuda/image-20250424192311202.webp)

## Nvidia GPU Generations

- 2006: G80-based GeForce 8800
- 2008: GT200-based GeForce GTX 280
- 2010: Fermi
- 2012: Kepler
- 2014: Maxwell
- 2016: Pascal
- 2017: Volta
- 2021: Ampere
- 2022: Hopper
- 2024: Blackwell

#### 2006: G80 Terminology

SP: Streaming Processor, scalar ALU for a single CUDA thread

SPA: Stream Processor Array

SM: Streaming Multiprocessor, containing of 8 SP

TPC: Texture Processor Cluster: 2 SM + TEX

![image-20250424192825010](./hpc-2025-cuda/image-20250424192825010.webp)

Design goal: performance per millimeter

For GPUs, performance is throughput, so hide latency with computation not cache.

So this is single instruction multiple thread (SIMT).

**Thread Life Cycle**:

Grid is launched on the SPA and thread blocks are serially distributed to all the SM.

![image-20250424193125125](./hpc-2025-cuda/image-20250424193125125.webp)

**SIMT Thread Execution**:

Groups of 32 threads formed into warps. Threads in the same wraps always executing same instructions. And some threads may become inactive when code path diverges so the hardware **automatically Handles divergence**.

Warps are the primitive unit of scheduling.

> SIMT execution is an implementation choice. As sharing control logic leaves more space for ALUs.

**SM Warp Scheduling**:

SM hardware implements zero-overhead warp scheduling:

- Warps whose next instruction has its operands ready for consumption are eligible for execution.
- Eligible warps are selected for execution on a prioritized scheduling policy.

> If 4 clock cycles needed to dispatch the same instructions for all threads in a warp, and one global memory access is needed for every 4 instructions and memory latency is 200 cycles. So there should be 200 / (4 * 4) =12.5 (13) warps to fully tolerate the memory latency

The SM warp scheduling use scoreboard and similar things.

**Granularity Consideration**:

Consider that int the G80 GPU, one SM can run 768 threads and 8 thread blocks, which is the best tiles to matrix multiplication: 16 * 16 = 256 and in one SM there can be 3 thread block which fully use the threads.

### 2008: GT200 Architecture

![image-20250424195111341](./hpc-2025-cuda/image-20250424195111341.webp)

### 2010: Fermi GF100 GPU

**Fermi SM**:

![image-20250424195221886](./hpc-2025-cuda/image-20250424195221886.webp)

There are 32 cores per SM and 512 cores in total, and introduce 64KB configureable L1/ shared memory.

Decouple internal execution resource and dual issue pipelines to select two warps.

And in Fermi, the debut the Parallel Thread eXecution(PTX) 2.0 ISA.

### 2012 Kepler GK 110

![image-20250424200022880](./hpc-2025-cuda/image-20250424200022880.webp)

### 2014 Maxwell

4 GPCs and 16 SMM.

![image-20250424200330783](./hpc-2025-cuda/image-20250424200330783.webp)

### 2016 Pascal

No thing to pay attention to.

### 2017 Volta

First introduce the tensor core, which is the ASIC to calculate matrix multiplication.

### 2021 Ampere

The GA100 SM:

![image-20250508183446257](./hpc-2025-cuda/image-20250508183446257.webp)

### 2022 Hopper

Introduce the GH200 Grace Hopper Superchip:

![image-20250508183528381](./hpc-2025-cuda/image-20250508183528381.webp)

A system contains a CPU and GPU which is linked by a NVLink technology.

And this system can scale out for machine learning.

![image-20250508183724162](./hpc-2025-cuda/image-20250508183724162.webp)

Memory access across the NVLink:

- GPU to local CPU
- GPU to peer GPU
- GPU to peer CPU

![image-20250508183931464](./hpc-2025-cuda/image-20250508183931464.webp)

These operations can be handled by hardware accelerated memory coherency. Previously, there are separate page table for CPU and GPU but for GPU to access memory in both CPU and GPU, CPU and GPU can use the same page table.

![image-20250508184155087](./hpc-2025-cuda/image-20250508184155087.webp)

### 2025 Blackwell

![image-20250508184455215](./hpc-2025-cuda/image-20250508184455215.webp)

### Compute Capability

The software version to show hardware version features and specifications.

 ## G80 Memory Hierarchy 

### Memory Space

Each thread can

- Read and write per-thread registers.
- Read and write per-thread local memory.
- Read and write pre-block shared memory.
- Read and write pre-grid global memory.
- Read only pre-grid constant memory.
- Read only pre-grid texture memory.

![image-20250508185236920](./hpc-2025-cuda/image-20250508185236920.webp)

Parallel Memory Sharing:

- Local memory is per-thread and mainly for auto variables and register spill.
- Share memory is pre-block which can be used for inter thread communication.
- Global memory is pre-application which can be used for inter grid communication.

### SM Memory Architecture 

![image-20250508185812302](./hpc-2025-cuda/image-20250508185812302.webp)

Threads in a block share data and results in memory and shared memory.

Shared memory is dynamically allocated to blocks which is one of the limiting resources.

### SM Register File

Register File(RF): there are 32KB, or 8192 entries,  register for each SM in G80 GPU.

The tex pipeline and local/store pipeline can read and write register file.

Registers are dynamically partitioned across all blocks assigned to the SM. Once assigned to a block the register is **not** accessible by threads in other blocks and each thread in the same block only access registers assigned to itself.

For a matrix multiplication example:

- If one thread uses 10 registers and one block has 16x16 threads, each SM can contains three thread blocks as one thread blocks need 16 * 16 * 10 =2,560 registers and 3 * 2560 < 8192.
- But if each thread need 11 registers, one SM can only contains two blocks once as 8192 < 2816 * 3.

More on dynamic partitioning: dynamic partitioning gives more flexibility to compilers and programmers.

1. A smaller number of threads that require many registers each.
2. A large number of threads that require few registers each.

So there is a tradeoff between instruction level parallelism and thread level parallelism.

### Parallel Memory Architecture 

In a parallel machine, many threads access memory. So memory is divided into banks to achieve high bandwidth.

Each bank can service one address per cycle. If multiple simultaneous accesses to a bank result in a bank conflict.

Shared memory bank conflicts:

- The fast cases:
  - All threads of a half-warp access different banks, there's no back conflict.
  - All threads of a half-warp access the identical address ,there is no bank conflict (by broadcasting).
- The slow cases:
  - Multiple threads in the same half-warp access the same bank

## Memory in Later Generations

### Fermi Architecture

**Unified Addressing Model** allows local, shared and global memory access using the same address space.

![image-20250508193756274](./hpc-2025-cuda/image-20250508193756274.webp)

**Configurable Caches** allows programmers to configure the size if L1 cache and the shared memory.

The L1 cache works as a counterpart to shared memory:

- Shared memory improves memory access for algorithms with well defined memory access.
- L1 cache improves memory access for irregular algorithms where data addresses are not known before hand.

### Pascal Architecture

**High Bandwidth Memory**: a technology which enables multiple layers of DRAM components to be integrated vertically on the package along with the GPU.

![image-20250508194350572](./hpc-2025-cuda/image-20250508194350572.webp)

**Unified Memory** provides a single and unified virtual address space for accessing all CPU and GPU memory in the system.

And the CUDA system software doesn't need to synchronize all managed memory allocations to the GPU before each kernel launch. This is enabled by **memory page faulting**.

## Advanced GPU Features

### GigaThread

Enable concurrent kernel execution:

![image-20250508195840957](./hpc-2025-cuda/image-20250508195840957.webp)

And provides dual **Streaming Data Transfer** engines to enable streaming data  transfer, a.k.a direct memory access.

![image-20250508195938546](./hpc-2025-cuda/image-20250508195938546.webp)

### GPUDirect

![image-20250508200041910](./hpc-2025-cuda/image-20250508200041910.webp)

### GPU Boost

GPU Boost works through real time hardware monitoring as opposed to application based profiles. It attempts to find what is the appropriate GPU frequency and voltage for a given moment in time.

### SMX Architectural Details

Each unit contains four warp schedulers.

Scheduling functions:

- Register scoreboard for long latency operations.
- Inter-warp scheduling decisions.
- Thread block level scheduling.

### Improving Programmability

![image-20250515183524043](./hpc-2025-cuda/image-20250515183524043.webp)

**Dynamic Parallelism**: The ability to launch new grids from the GPU.

And then introduce data-dependent parallelism and dynamic work generation and even batched and nested parallelism.

The cpu controlled work batching:

- CPU program limited by single point of control.
- Can run at most 10s of threads.
- CPU is fully consumed with controlling launches.

![](./hpc-2025-cuda/image-20250515184225475.webp)

Batching via dynamic parallelism:

- Move top-level loops to GPUs.
- Run thousands of independent tasks.
- Release CPU for other work.

![image-20250515184621914](./hpc-2025-cuda/image-20250515184621914.webp)

### Grid Management Unit

![image-20250515184714663](./hpc-2025-cuda/image-20250515184714663.webp)

Fermi Concurrency:

- Up to 16 grids can run at once.
- But CUDA streams multiplex into a single queue.
- Overlap only at stream edge.

Kepler Improved Concurrency:

- Up to 32 grids can run at once.
- One work queue per stream.
- Concurrency at full-stream level.
- No inter-stream dependencies.

It is called as **Hyper-Q**.

Without Hyper-Q:

![image-20250515185019590](./hpc-2025-cuda/image-20250515185019590.webp)

With Hyper-Q:

![image-20250515185034758](./hpc-2025-cuda/image-20250515185034758.webp)

In pascal, **asynchronous concurrent computing** is introduced.

![image-20250515185801775](./hpc-2025-cuda/image-20250515185801775.webp)

### NVLink: High-Speed Node Network

![image-20250515185212184](./hpc-2025-cuda/image-20250515185212184.webp)

> The *consumer* prefix means the product is designed for gamers.
>
> The *big* prefix means the product is designed for HPC.

### Preemption

Pascal can actually preempt at the lowest level, the instruction level.

![image-20250515190244112](./hpc-2025-cuda/image-20250515190244112.webp)

### Tensor Core

Operates on a 4x4 matrix and performs: D = A x B + C.

![image-20250515190507199](./hpc-2025-cuda/image-20250515190507199.webp)

### GPU Multi-Process Scheduling

- Timeslice scheduling: single process throughput optimization.
- Multi process service: multi-process throughput optimization.

How about multi-process time slicing:

![image-20250515190703918](./hpc-2025-cuda/image-20250515190703918.webp)

Volta introduces the multi-process services:

![image-20250515191142384](./hpc-2025-cuda/image-20250515191142384.webp)

 
