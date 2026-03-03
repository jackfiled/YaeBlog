---
title: High Performance Computing 25 SP OpenCL Programming
date: 2025-08-31T13:51:02.0181970+08:00
tags:
- 高性能计算
- 学习资料
---


Open Computing Language.

<!--more-->

OpenCL is Open Computing Language.

- Open, royalty-free standard C-language extension.
- For parallel programming of heterogeneous systems using GPUs, CPUs , CBE, DSP and other processors including embedded mobile devices.
- Managed by Khronos Group.

![image-20250529185915068](./hpc-2025-opencl/image-20250529185915068.webp)

### Anatomy of OpenCL

- Platform Layer APi
- Runtime Api
- Language Specification

### Compilation Model

OpenCL uses dynamic/runtime compilation model like OpenGL.

1. The code is compiled to an IR.
2. The IR is compiled to a machine code for execution.

And in dynamic compilation, *step 1* is done usually once and the IR is stored. The app loads the IR and performs *step 2* during the app runtime.

### Execution Model

OpenCL program is divided into

- Kernel: basic unit of executable code.
- Host: collection of compute kernels and internal functions.

The host program invokes a kernel over an index space called an **NDRange**.

NDRange is *N-Dimensional Range*, and can be a 1, 2, 3-dimensional space.

A single kernel instance at a point of this index space is called **work item**. Work items are further grouped into **work groups**.

### OpenCL Memory Model

![image-20250529191215424](./hpc-2025-opencl/image-20250529191215424.webp)

Multiple distinct address spaces: Address can be collapsed depending on the device's memory subsystem.

Address space:

- Private: private to a work item.
- Local: local to a work group.
- Global: accessible by all work items in all work groups.
- Constant: read only global memory.

> Comparison with CUDA:
>
> ![image-20250529191414250](./hpc-2025-opencl/image-20250529191414250.webp)

Memory region for host and kernel:

![image-20250529191512490](./hpc-2025-opencl/image-20250529191512490.webp)

### Programming Model

#### Data Parallel Programming Model

1. Define N-Dimensional computation domain
2. Work-items can be grouped together as *work group*.
3. Execute multiple work-groups in parallel.

#### Task Parallel Programming Model

> Data parallel execution model must be implemented by all OpenCL computing devices, but task parallel programming is a choice for vendor.

Some computing devices such as CPUs can also execute task-parallel computing kernels.

- Executes as s single work item.
- A computing kernel written in OpenCL.
- A native function.

### OpenCL Framework 

![image-20250529192022613](./hpc-2025-opencl/image-20250529192022613.webp)

The basic OpenCL program structure:

![image-20250529192056388](./hpc-2025-opencl/image-20250529192056388.webp)

**Contexts** are used to contain the manage the state of the *world*.

**Command-queue** coordinates execution of the kernels.

