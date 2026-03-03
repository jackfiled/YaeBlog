---
title: High Performance Computing 25 SP Potpourri
date: 2025-08-31T13:51:29.8809980+08:00
tags:
- 高性能计算
- 学习资料
---


Potpourri has a good taste.

<!--more-->

## Heterogeneous System Architecture

![image-20250612185019968](./hpc-2025-potpourri/image-20250612185019968.webp)

The goals of the HSA:

- Enable power efficient performance.
- Improve programmability of heterogeneous processors.
- Increase the portability of code across processors and platforms.
- Increase the pervasiveness of heterogeneous solutions.

### The Runtime Stack

![image-20250612185221643](./hpc-2025-potpourri/image-20250612185221643.webp)

## Accelerated Processing Unit

A processor that combines the CPU and the GPU elements into a single architecture.

![image-20250612185743675](./hpc-2025-potpourri/image-20250612185743675.webp)

## Intel Xeon Phi

The goal:

- Leverage X86 architecture and existing X86 programming models.
- Dedicate much of the silicon to floating point ops.
- Cache coherent.
- Increase floating-point throughput.
- Strip expensive features.

The reality:

- 10s of x86-based cores.
- Very high-bandwidth local GDDR5 memory.
- The card runs a modified embedded Linux.

## Deep Learning: Deep Neural Networks

The network can used as a computer.

## Tensor Processing Unit

A custom ASIC for the phase of Neural Networks (AI accelerator).

### TPUv1 Architecture

![image-20250612191035632](./hpc-2025-potpourri/image-20250612191035632.webp)

### TPUv2 Architecture

![image-20250612191118473](./hpc-2025-potpourri/image-20250612191118473.webp)

Advantages of TPU:

- Allows to make predications very quickly and respond within fraction of a second.
- Accelerate performance of linear computation, key of machine learning applications.
- Minimize the time to accuracy when you train large and complex network models.

Disadvantages of TPU:

- Linear algebra that requires heavy branching or are not computed on the basis of element wise algebra.
- Non-dominated matrix multiplication is not likely to perform well on TPUs.
- Workloads that access memory using sparse technique.
- Workloads that use highly precise arithmetic operations.

