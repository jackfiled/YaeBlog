---
title: 计算机系统结构——流水线复习
tags:
  - 计算机系统结构
  - 笔记
date: 2024-06-12 20:27:25
---

让指令的各个执行阶段依次进行运行是一个简单而自然的想法，但是这种方式执行速度慢、运行效率低。因此一个很自然的想法就是将指令重叠起来运行，让执行功能部件被充分的利用起来，这就是**流水线**。

流水线的表示方法有两种。

![image-20240612184855300](computer-architecture-pipeline/image-20240612184855300.png)

第一种被称作**连接图**，清晰的表达出了流水线内部的逻辑关系。

![image-20240612184949777](computer-architecture-pipeline/image-20240612184949777.png)

> 上图中给出了两个流水线中的概念：通过时间和排空时间。其中通过时间又被称作装入时间，是指第一个任务进入流水线到完成的事件；排空时间则相反，是最后一个任务通过流水线的时间。

第二种被称作**时空图**，通过在图中画出一个指令序列通过流水线的过程表现出流水线的时间关系。

在流水线中，每个功能部件之后需要存在一个寄存器，这个寄存器被称为流水寄存器，其作用为在流水线相邻的两端之间传递数据，并且把各段的处理工作相互隔离。

流水线有着多种分类。

按照等级分：

- 部件级，将处理机中的部件分段相互连接，也称作运算操作流水线。
- 处理机级，将指令的执行过程分解为若干个子过程，就是指令流水线。
- 系统级，把多个处理机串行连接，对同一数据流进行处理，称作宏流水线。

按照完成功能的多倍性：

- 单功能，流水线各段之间的连接固定，只能完成一种功能。
- 多功能，段之间的连接可以变化，不同的连接方式可以完成不同的功能。

其中多功能还能进一步分为：

- 静态流水线，同一时间内，多功能流水线的各段只能按照同一种功能的方式连接。
- 动态流水线，同一时间内，多功能流水线的各种可以按照不同的方式连接，执行不同的功能。

![image-20240612190426368](computer-architecture-pipeline/image-20240612190426368.png)

按照流水线中是否存在反馈回路分类：

- 线性，串行连接，没有反馈回路，每个段只能流过一次。
- 非线性，存在反馈回路。

根据任务流入和流出的顺序是否相同：

- 顺序流水线
- 乱序流水线

流水线的性能指标一般由三个指标衡量：

- 吞吐率，单位时间流水线完成任务的数量，或者输出结果的数量。
- 加速比，同一任务，不使用流水线所使用时间与使用流水线所用时间比。
- 效率，流水线设备的利用率。

![image-20240612192700169](computer-architecture-pipeline/image-20240612192700169.png)

在设计流水线的过程中存在若干问题。

1. 瓶颈问题。当流水线各段不均匀时，机器的时钟周期取决于瓶颈段的延迟时间，因此设计流水线时，应当使各段的时间相等。
2. 流水线的额外开销。由于流水寄存器的延迟和时钟偏移开销，流水线往往会增加单条指令的执行时间，当时钟周期过小时，流水已经没有意义。
3. 冲突问题。数据冲突、结构冲突和控制冲突。

一个典型的五段流水线MIPS流水线：

![image-20240612193301372](computer-architecture-pipeline/image-20240612193301372.png)




