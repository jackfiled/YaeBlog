---
title: High Performance Computing 25 SP Heterogeneous Computing
date: 2025-05-10T00:36:20.5391570+08:00
tags:
- 高性能计算
- 学习资料
---


Heterogeneous Computing is on the way!

<!--more-->

## GPU Computing Ecosystem

CUDA: NVIDIA's Architecture for GPU computing.

![image-20250417195644624](./hpc-2025-heterogeneous-system/image-20250417195644624.webp)

## Internal Buses

**HyperTransport**:

Primarily a low latency direct chip to chip interconnect, supports mapping to board to board interconnect such as PCIe.

**PCI Expression**

Switched and point-to-point connection.

**NVLink**

![image-20250417200241703](./hpc-2025-heterogeneous-system/image-20250417200241703.webp)

**OpenCAPI**

Heterogeneous computing was in the professional world mostly limited to HPC, in the consumer world is a "nice to have".

But OpenCAPI is absorbed by CXL.

## CPU-GPU Arrangement

![image-20250424184701573](./hpc-2025-heterogeneous-system/image-20250424184701573.webp)

#### First Stage: Intel Northbrige

![image-20250424185022360](./hpc-2025-heterogeneous-system/image-20250424185022360.webp)

### Second Stage: Symmetric Multiprocessors:

![image-20250424185048036](./hpc-2025-heterogeneous-system/image-20250424185048036.webp)

### Third Stage: Nonuniform Memory Access

And the memory controller is integrated directly in the CPU.

![image-20250424185152081](./hpc-2025-heterogeneous-system/image-20250424185152081.webp)

So in such context, the multiple CPUs is called NUMA:

![image-20250424185219673](./hpc-2025-heterogeneous-system/image-20250424185219673.webp)

And so there can be multi GPUs:

![image-20250424185322963](./hpc-2025-heterogeneous-system/image-20250424185322963.webp)

### Fourth Stage: Integrated PCIe in  CPU

![image-20250424185354247](./hpc-2025-heterogeneous-system/image-20250424185354247.webp)

And there is such team *integrated CPU*, which integrated a GPU into the CPU chipset.

![image-20250424185449577](./hpc-2025-heterogeneous-system/image-20250424185449577.webp)

And the integrated GPU can work with discrete GPUs:

![image-20250424185541483](./hpc-2025-heterogeneous-system/image-20250424185541483.webp)

### Final Stage: Multi GPU Board

![image-20250424190159059](./hpc-2025-heterogeneous-system/image-20250424190159059.webp)
