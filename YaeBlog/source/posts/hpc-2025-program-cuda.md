---
title: High Performance Computing 25 SP Programming CUDA
date: 2025-08-31T13:50:53.6891520+08:00
tags:
- 高性能计算
- 学习资料
---


Compute Unified Device Architecture

<!--more-->

## CUDA

General purpose programming model:

- Use kicks off batches of threads on the GPU.

![image-20250515195739382](./hpc-2025-program-cuda/image-20250515195739382.webp)

The compiling C with CUDA applications:

![image-20250515195907764](./hpc-2025-program-cuda/image-20250515195907764.webp)

### CUDA APIs

Areas:

- Device management
- Context management
- Memory management
- Code module management
- Execution control
- Texture reference management
- Interoperability with OpenGL and Direct3D

Two APIs:

- A low-level API called the CUDA driver API.
- A higher-level API called the C runtime for CUDA that is implemented on top of the CUDA driver API.

