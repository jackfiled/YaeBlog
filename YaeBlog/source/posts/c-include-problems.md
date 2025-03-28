---
title: C项目中有关头文件的一些问题
date: 2022-05-08T11:35:19.0000000
tags:
- 技术笔记
- C/C++
---


最近在完成一门`C`语言课程的大作业，课设老师要求我们将程序分模块的开发。在编写项目头文件的时候，遇到了一些令本菜鸡大开眼界的问题。

<!--more-->

## 头文件相互包含问题

### 问题

我项目的结构大致如图所示：

![](1.webp)

在`include`的头文件目录下有两个头文件，`rail.h`和`bus.h`，这两个头文件分别定义了两个结构体`rail_node_t`和`bus_t`。

但在这两个结构题的定义中，我互相使用了指向对方结构体的指针。

```C
/*rail.h的内容*/
#include "bus.h"

struct {
    ...
    
    bus_t *bus;
    ...
} rail_node;

typedef struct rail_node rail_node_t;
```

```C
/*bus.h的内容*/
#include "rail.h"

struct {
    ...
    
    rail_node_t* rail_node_pos;
	
	...
} bus;

typedef struct bus bus_t;
```

于是在编译的时候，编译器就会报`rail_node_t`和`bus_t`这两个结构体未定义的错误。

### 解决

这个问题解决起来也非常的容易，只要修改其中一个结构体的定义就可以了。在以后的设计中注意不要出现这类相互包含的结构体。

> 而且一般这种时候虽然IDE的静态检查不会报错，但是自动补全却会失效。所以当你发现你的IDE出现一些奇怪行为的时候，就要格外小心了。

## 自己定义的头文件和内部头文件命名冲突的问题

### 问题

在项目中我引入了谷歌的单元测试框架[GTest](https://github.com/google/googletest)。但是在编译测试程序的时候遇到了一些困难。

项目的`test`文件夹下是单元测试文件夹，但是在编译的时候会报错

![](2.webp)

大意就是在一个google test内部的头文件中有几个函数找不到定义，这个函数都位于`io.h`这个头文件中。

在一开始我以为是平台的兼容性问题，但是在我电脑的其他项目中引用这个库都没有问题。在一开始我以为是google test作为一个为`C++`设计的单元测试库在我的`C`项目中出现了不兼容的情况，于是我在设计一个假的单元测试

```C++
#include "gtest/gtest.h"
#include "gmock/gmock.h"

using ::testing::Return;
using ::testing::AtLeast;
using ::testing::Exactly;

using namespace testing;

TEST(test, test)
{
    EXPECT_EQ(1, 1);
}
```

不测试任何我编写的库，而只是验证单元测试框架是否能正确运行。但是这个单元测试仍然无法通过编译，报错和之前的一样。

于是我便打开了编译中出错的`gtest-port.h`文件，发现在预处理的过程中头文件的替换出现了问题：在我自己的头文件中也有一个名叫`io.h`的头文件，负责项目中的输入输出，而在预处理的过程中预处理器用这个头文件代替了标准库中的`io.h`,但在我自己的头文件中自然没有测试库需要的函数了。

> 在找bug的过程中还有比较玄学的事情，如果我把我库里的头文件一个个的添加，就可以编译成功，但是如果在第一次编译成功之后再次`cmake ..`，重新生成编译文件，再编译就失败了。这个玄学现象让我迷惑了很久。

### 解决

重新命名模块即可。在后续的模块设计中注意命名。

> 这里还有一个小插曲，不要轻易相信IDE提供的重构功能。在这里`CLion`这个IDE就在我重命名头文件的时候把`gtest-port.h`中对`io.h`的引用也重构了。真是智能！~~此处应有流汗黄豆~~

## 后记

只有在实际的开发中才能学到这些教训啊！
