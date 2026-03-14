---
title: High Performance Computing 25 SP Dichotomy of Parallel Computing Platforms
date: 2025-03-28T01:03:32.2187720+08:00
tags:
- 高性能计算
- 学习资料
---


Designing algorithms is always the hardest.

<!--more-->

Flynn's classical taxonomy:

- SISD
- SIMD
- MISD
- MIMD

Multiple instruction and multiple data is currently the most common type of parallel computer.

> A variant: single program multiple data(SPMD).

## Dichotomy of Parallel Computing Platforms

Based on the logical and physical organization of parallel platforms.

Logical organization (from a programmer's perspective):

- Control structure: ways of expressing parallel tasks.
- Communication model: interactions between tasks.

Hardware organization:

- Architecture
- Interconnection networks.

Control Structure of Parallel Platform: parallel tasks can be specified at various levels of granularity.

Communication Model: **Shared address space platforms**. Support a common data space that is accessible to all processors. Two types of architectures:

- Uniform memory access (UMA)
- Non-uniform memory access(NUMA)

> NUMA and UMA are defined in term of memory access times not the cache access times.

![image-20250313193604905](./hpc-2025-parallel-computing/image-20250313193604905.webp)

NUMA and UMA:

- The distinction between NUMA and UMA platforms is important from the point of view of algorithm design.

- Programming these platforms is easier since reading and writing are implicitly visible to other processors.

- Caches is such machines requires coordinated access to multiple copies.

  > Leads to cache coherence problem.

- A weaker model of these machines provides an address map but not coordinated access.

**Global Memory Space**:

- Easy to program.

- Read-only interactions:

  Invisible to programmers.

  Same as in serial programs.

- Read/write interactions:

  Mutual exclusion for concurrent access such as lock and related mechanisms.

- Programming paradigms: Threads/Directives.

Caches in shared-address-space:

- Address translation mechanism to locate a memory word in the system.
- Well-defined semantics over multiple copies(**cache coherence**).

> Shared-address-space vs shared memory machine:
>
> Shared address space is a programming abstraction.
>
> Shared memory machine is a physical machine attribute.

Distributed Shared Memory(DSM) or Shared Virtual Memory(SVM):

- Page-based access control: leverage the virtual memory support and manage main memory as a fully associative cache on the virtual address space by embedding a coherence protocol in the page fault handler.
- Object based access control: flexible but no false sharing.

## Parallel Algorithm Design

Steps in parallel algorithm design:

- Identifying portions of the work that can be performed concurrently.
- Mapping the concurrent pieces of work onto multiple processors running in parallel.
- Distributing the input, output and intermediate data associated with the program.
- Managing accesses to data shared by multiple processors.
- Synchronizing the processors at various stages of the parallel program execution.

### Decomposition

Dividing a computation into smaller parts some or all of which may be executed in parallel.

Tasks: programmer-defined units with arbitrary size and is indivisible.

Aim: **reducing execution time**

Ideal decomposition:

- All tasks have similar size.
- Tasks are **not** waiting for each other **not** sharing resources.

Dependency graphs:

Task dependency graph: an abstraction to express dependencies among tasks and their relative order of execution.

- Directed acyclic graphs.
- Nodes are tasks.
- Directed edges: dependencies amongst tasks.

> The fewer directed edges, the better as parallelism.

Granularity:

The granularity of the decomposition: the number and size of tasks into which a problem is decomposed.

- Fine-grained: a large number of small tasks.
- Coarse-grained: a small number of large tasks.

Concurrency: 

**maximum degree of concurrency**

**Average degree of concurrency**

The critical path determines the average degree of concurrency.

Critical path is the longest directed path between any pair of start and finish nodes. So a shorter critical path favors a higher degree of concurrency.

**Limited Granularity**:

It may appear that increasing the granularity of decomposition will utilize the resulting  concurrency.

But there is a inherent bound on how fine-grained a decomposition a problem permits.

Speedup:

The ratio of serial to parallel execution time. Restrictions on obtaining unbounded speedup from:

- Limited granularity.
- Degree of concurrency.
- Interaction among tasks running on different physical processors.

Processor:

Computing agent that performs tasks, an abstract entity that uses the code and data of a tasks to produce the output of the task within a finite amount of time.

Mapping: the mechanism by which tasks are assigned to processor for execution. The task dependency and task interaction graphs play an important role.

Decomposition techniques:

Fundamental steps: split the computations to be performed into a set of tasks for concurrent execution.

1. Recursive decomposition.

   A method for inducing concurrency in problems that can be solved using the **divide-and-conquer** strategy.

2. Data decomposition.

   A method for deriving concurrency in algorithms that operate on large data structures.

   The operations performed by these tasks on different data partitions.

   Can be partitioning output data and partitioning input data or even partitioning intermediate data.

3. Exploratory decomposition.

   Decompose problems whose underlying computations correspond to a search of a space for solutions.

   Exploratory decomposition appears similar to data decomposition.

4. Speculative decomposition.

   Used when a program may take one of many possible computationally significant branches depending on the output of preceding computation.

   Similar to evaluating branches in a *switch* statement in `C` as evaluate multiple branches in parallel and correct branch will be used and other branches will be discarded.

   The parallel run time is smaller than the serial run time by the amount of time to evaluate the condition.

### Characteristics of Tasks

**Task generation**:

- Static: all the tasks are known before the algorithm starts executing.
- Dynamic: the actual tasks and the task dependency graph are not explicitly available at priori.
- Either static or dynamic.

**Task Sizes**:

The relative amount of time required t complete the task.

- Uniform
- Non-uniform

The knowledge of task sizes will influence the choice of mapping scheme.

**Inter-Task Interactions**:

- Static versus dynamic.
- Regular versus irregular.
- Read-only versus read-write
- One-way versus two-way.

### Mapping Techniques

Mapping techniques is for loading balancing.

Good mappings:

- Reduce the interaction time.
- Reduce the idle time.

![image-20250320200524155](./hpc-2025-parallel-computing/image-20250320200524155.webp)

There are two mapping methods:

- **Static Mapping**: determined by programming paradigm and the characteristics of tasks and interactions.

  Static mapping is often used in conjunction with *data partitioning* and *task partitioning*.

- **Dynamic Mapping**: distribute the work among processors during the execution. Also referred as dynamic load-balancing.

  The **centralized scheme** as all the executable tasks are maintained in a common central data structure and distributed by a special process or a subset of processes as **master** process.

  Centralized scheme always means easy to implement but with limited scalability.

  The **distributed scheme** as the set of executable tasks are distributed among processes which exchange tasks at run time to balance work.

**Minimize frequency of interactions**: 

There is a relatively high startup cost associated with each interaction on many architectures.

So restructure the algorithm such that shared data are accessed and used in large pieces.

**Minimize contention and hot spots**:

Contention occurs when multiple tasks try to access the same resources concurrently.

And centralized scheme for dynamic mapping are a frequent source of contention so use the distributed mapping schemes.

**Overlapping computations with interactions**:

When waiting for shared data, do some useful computations.

- Initiate an interaction early enough to complete before it needed.
- In dynamic mapping schemes, the process can anticipate that it is going to run out of work and initiate a work which transfers interaction in advance.

Overlapping computations with interaction requires support from the programming paradigm, the operating system and the hardware.

- Disjoint address-space paradigm: non-blocking message passing primitives.
- Share address-space paradigm: prefetching hardware which can anticipate the memory addresses and initiate access in advance of when they are needed.

**Replicating data or computations**:

Multiple processors may require frequent read-only access to shared data structure such as a hash-table.

For different paradigm:

- Share address space use cache.
- Message passing: remote data accesses are more expensive and harder than local accesses.

Data replication increases the memory requirements. In some situation, it may be more cost-effective to compute these intermediate results than to get then from another place.

**Using optimized collective interaction operations**:

Collective operations are like:

- Broadcasting some data to all processes.
- Adding up numbers each belonging to a different process.

### Parallel Algorithm Model

The way of structuring  parallel algorithm by

- Selecting a decomposition
- Selecting a mapping technique.
- Applying the appropriate strategy to minimize interactions.

**Data parallel model**:

The tasks are statically or semi-statically mapped onto processes and each task performs similar operations on different data.

Example: matrix multiplication.

**Task graph model**:

The interrelations among the tasks are utilized to promote locality or to reduce interaction costs.

Example: quick sort, sparse matrix factorization and many other algorithms using divide-and-conquer decomposition.

**Work pool model**:

Characterized by a dynamic mapping of task onto processes for load balancing.

Example: parallelization of loops by chunk scheduling.

**Master-slave model** :

One or more master processes generate work and allocate it to worker processes.

**Pipeline or producer-consumer model**:

A stream of data is passed on through a succession of processes, each of which performs some tasks.

### Analytical Modeling of Parallel Programs

**Performance evaluation**:

Evaluation in terms of execution time.

A parallel system is the combination of an algorithm and the parallel architecture on which it is implemented.

**Sources of overhead in parallel program**:

A typical execution includes:

- Essential computation

  Computation that world be performed by the serial program for solving the same problem instance.

- Interprocess communication

- Idling

- Excess computation

  Computation which not performed by the serial program.

**Performance metrics for parallel system**:

- Execution time
- Overhead function
- Total overhead
- Speedup

> For a given problem, more than one sequential algorithm may be available.

Theoretically speaking, speed up can never exceed the number of PE.

If super linear speedup: the work performed by a serial program is greater than its parallel formulation, maybe hardware features that put the serial implementation at a disadvantage.

**Amdahl's Law**:

![image-20250327194045418](./hpc-2025-parallel-computing/image-20250327194045418.webp)

The overall performance improvement gained by optimizing a single part of a system is limited by the fraction of time that the improved part is actually used.

Efficiency: a measure of the fraction of time for which a PE is usefully employed.

Cost: the product of parallel run time and the number of processing elements used.

![image-20250327194312962](./hpc-2025-parallel-computing/image-20250327194312962.webp)
