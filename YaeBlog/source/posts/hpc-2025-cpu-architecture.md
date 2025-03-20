---
title: High Performance Computing 25 SP CPU Architecture
date: 2025-03-13T23:59:08.8167680+08:00
tags:
- 学习资料
- 高性能计算
---

How to use the newly available transistors?

<!--more-->

Parallelsim:

Instruction Level Parallelism(ILP):

- **Implicit/transparent** to users/programmers.
- Instruction pipelining.
- Superscalar execution.
- Out of order execution.
- Register renaming.
- Speculative execution.
- Branch prediction.

Task Level Parallelism(TLP):

- **Explicit** to users/programmers.
- Multiple threads or processes executed simultaneously.
- Multi-core processors.

Data Parallelism:

- Vector processors and SIMD.

Von Neumann Architecture: the **stored-program** concept. Three components: processor, memory and data path.

Bandwidth: the gravity of modern computer system.

## Instruction Pipelining

Divide incoming instructions into a series of sequential steps performed by different processor unit to keep every part of the processor busy.

Superscalar execution can execute more than one instruction during a clock cycle.

Order of order execution.

Very long instruction word(VLIW): allows programs to explicitly specify instructions to execute at the same time.

EPIC: Explicit parallel instruction computing.

Move the complexity of instruction scheduling from the CPU hardware to the software compiler:

- Check dependencies between instructions.
- Assign instructions to the functional units.
- Determine when instructions are initiated placed together into a single word.

![image-20250313184421305](./hpc-2025-cpu-architecture/image-20250313184421305.png)

Comparisons between different architecture:

![image-20250313184732892](./hpc-2025-cpu-architecture/image-20250313184732892.png)

## Multi-Core Processor Gala

Symmetric multiprocessing(SMP): a multiprocessor computer hardware and software architecture.

Two or more identical processors are connected to a **single shared main memory** and have full access to all input and output devices.

> Current trend: computer clusters, SMP computers connected with network.

Multithreading: exploiting thread-level parallelism.

Multithreading allows multiple threads to share the functional units of a single processor in an overlapping fashion **duplicating only private state**. A thread switch should be much more efficient than a process switch.

Hardware approaches to multithreading: 

**fine-grained multithreading**:

- Switches between threads on each clock.
- Hide the throughput losses that arise from the both short and long stalls.
- Disadvantages: slow down the execution of an individual thread.

**Coarse-grained multithreading**:

- Switch threads only on costly stalls.
- Limited in its ability to overcome throughput losses

**Simultaneous multithreading(SMT)**:

- A variation on fine-grained multithreading

![image-20250313190913475](./hpc-2025-cpu-architecture/image-20250313190913475.png)

## Data Parallelism: Vector Processors

Provides high-level operations that work on vectors.

Length of the array also varies depending on hardware.

SIMD and its generalization in vector parallelism approach improved efficiency by the same operation be performed on multiple data elements.
