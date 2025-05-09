---
title: High Performance Computing 25 SP Programming SMP Platform
date: 2025-05-10T00:17:26.5784020+08:00
tags:
- 高性能计算
- 学习资料
---



Sharing address space brings simplification.

<!--more-->

### Shared Address Space Programming Paradigm

Vary on mechanism for data sharing, concurrency models and support for synchronization.

- Process based model
- Lightweight processes and threads.

### Thread

A thread is a single stream of control in the flow of a program.

**Logical memory model of a thread**:

All memory is globally accessible to every thread and threads are invoked as function calls.

![image-20250327200344104](./hpc-2025-program-smp-platform/image-20250327200344104.webp)

Benefits of threads:

- Take less time to create a new thread than a new process.
- Less time to terminate a thread than a process.

The taxonomy of thread:

- User-Level Thread(ULT): The kernel is not aware of the existence of threads. All thread management is done by the application using a thread library. Thread switching does not require kernel mode privileges and scheduling is application specific.
- Kernel-Level Thread(KLT): All thread management is done by kernel. No thread library but an API to the kernel thread facility. Switching between threads requires the kernel and schedule on a thread basis.
- Combined ULT and KLT approaches. Combines the best of both approaches.

![image-20250403183104279](./hpc-2025-program-smp-platform/image-20250403183104279.webp)

## PThread Programming

Potential problems with threads:

- Conflicting access to shared memory.
- Race condition occur.
- Starvation
- Priority inversion
- Deadlock

### Mutual Exclusion

Mutex locks: implementing critical sections and atomic operations.

Two states: locked and unlocked. At any point of time, only one thread can lock a mutex lock.

### Producer-Consumer Work Queues

The producer creates tasks and inserts them into a work-queue.

The consumer threads pick up tasks from the task queue and execute them.

Locks represent serialization points since critical sections must be executed by threads one after the other.

**Important**: Minimize the size of critical sections.

### Condition Variables for Synchronization

The `pthread_mutex_trylock` alleviates the idling time but introduce the overhead of polling for availability of locks.

An interrupt driven mechanism as opposed to a polled mechanism as the availability is signaled.

A **condition variable**: a data object used for synchronizing threads. Block itself until specified data reaches a predefined state. 

When a thread performs a condition wait, it's not runnable as not use any CPU cycles but a mutex lock consumes CPU cycles as it polls for the locks.

**Common Errors**: One cannot assume any order of execution, must be explicitly established by mutex, condition variables and joins.

## MPI Programming

Low cost message passing architecture.

![image-20250403191254323](./hpc-2025-program-smp-platform/image-20250403191254323.webp)

Mapping of MPI Processes:

MPI views the processes as a one-dimensional topology. But in parallel programs, processes are arranged in higher-dimensional topologies. So it is required to map each MPI process to a process in the higher dimensional topology.

Non-blocking Send and Receive:

`MPI_ISend` and `MPI_Irecv` functions allocate a request object and return a pointer to it.

## OpenMP

A standard for directive based parallel programming.

Thread based parallelism and explicit parallelism.

Use fork-join model:

![image-20250403195750934](./hpc-2025-program-smp-platform/image-20250403195750934.webp)

