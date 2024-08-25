---
title: LLVM入门笔记
date: 2024-08-25T17:19:45.6572088+08:00
tags:
- 编译原理
- LLVM
- 技术笔记
---

为什么说LLVM是神？

<!--more-->

LLVM是一系列模块化的编译器和工具链技术。虽然名字中有着VM两个字母，但是项目实际上和传统意义上的虚拟机已经没有太多的关系了，LLVM仅作为一个项目代号存在。

LLVM最开始伊利诺伊大学的一个研究项目，最初的目标是为任意语言的静态编译和动态编译提供一种现代的、基于静态单赋值的编译策略。从那之后，LLVM逐渐发展为了一系列相互关联项目的集合，主要的LLVM子项目有：

- LLVM核心项目库。这个库围绕一系列**良好定义**的中间表示形式（LLVM IR）构建，提供了一个独立于源代码和目标机器的优化器和一个支持许多CPU架构的代码生成器。
- Clang。一个LLVM原生实现的C/C++/Objective-C编译器，致力于提供极快的编译速度，已读的错误和警告信息和一个用于开发源代码级别工具的开发平台。Clang静态分析工具和Clang Tidy代码格式化工具都是使用Clang为基础开发工具的优秀样例。
- LLDB。基于LLVM和Clang的调试器。
- `libc++`和`libc++ ABI`，C++标准库的实现。
- `compiler-rt`没看懂，感觉像是为了调试设计的运行时库。
- MLIR。一个为了构建可扩展、可复用编译基础设施的新颖尝试。需要指出的是MLIR是Multi Level Intermediate Representation的缩写，和机器学习似乎没有太大的关系。
- `OpenMP`。提供了并行计算框架OpenMP的一个Clang实现。
- `polly`。提供了自动并行化和自动向量化的缓存本地化优化器。
- `libclc`。OpenCL异构计算框架的标准库实现。
- `klee`。可以遍历程序中的所有动态路径以发现问题和验证程序功能的符号虚拟机（Symbolic Virtual Machine）。
- `LLD`。一个新的链接器
- `BOLT`。一个在链接之后工作的优化器。通过采样分析器得到的数据优化二进制程序的布局来提升运行效率的工具。

因此，在学习LLVM的过程中我们将重点关注LLVM的核心库，主要学习同LLVM IR交互和编写优化的方式。

## LLVM提供的工具使用

在ArchLinux上安装`llvm`和`clang`两个包就可以使用大多数LLVM提供的工具了。在这段中使用一段简单的C语言代码为例子演示各种工具链的使用。

```c
#include <stdio.h>

int main()
{
  printf("Hello, LLVM.\n");
  return 0;
}

```

将上述文件保存为C语言源文件`hello.c`。首先，使用`clang`将源代码编译为可执行文件：

```shell
clang hello.c -o hello
```

> 在默认情况下的`clang`行为和`gcc`编译器的行为表现一致，使用`-S`和`-c`参数能生成原生的汇编代码文件和可重定位文件。

编译完成之后可以正常执行：

![image-20240819213039409](./llvm-naive-0/image-20240819213039409.png)

然后尝试将这个C语言文件编译为LLVM的字节码形式：

```shell
clang -O3 -emit-llvm hello.c -c -o hello.bc
```

当使用`-emit-llvm`参数之后，使用`-S`选项将产生LLVM中间代码的文本形式`.ll`，使用`-c`选项将产生LLVM中间代码的字节码形式。上面这行命令就是编译为了LLVM的字节码形式。

得到字节码形式之后，可以使用JIT编译器直接执行：

```shell
lli hello.bc
```

![image-20240819213624927](./llvm-naive-0/image-20240819213624927.png)

可以使用反编译器将字节码转换为人类可读的文本形式：

```shell
llvm-dis < hello.bc > hello.ll
```

得到的文件内容为：

```ir
; ModuleID = '<stdin>'
source_filename = "hello.c"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-i128:128-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-linux-gnu"

@str = private unnamed_addr constant [13 x i8] c"Hello, LLVM.\00", align 1

; Function Attrs: nofree nounwind sspstrong uwtable
define dso_local noundef i32 @main() local_unnamed_addr #0 {
  %1 = tail call i32 @puts(ptr nonnull dereferenceable(1) @str)
  ret i32 0
}

; Function Attrs: nofree nounwind
declare noundef i32 @puts(ptr nocapture noundef readonly) local_unnamed_addr #1

attributes #0 = { nofree nounwind sspstrong uwtable "min-legal-vector-width"="0" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="x86-64" "target-features"="+cmov,+cx8,+fxsr,+mmx,+sse,+sse2,+x87" "tune-cpu"="generic" }
attributes #1 = { nofree nounwind }

!llvm.module.flags = !{!0, !1, !2, !3}
!llvm.ident = !{!4}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 8, !"PIC Level", i32 2}
!2 = !{i32 7, !"PIE Level", i32 2}
!3 = !{i32 7, !"uwtable", i32 2}
!4 = !{!"clang version 18.1.8"}
```

也可以编译器将字节码转换为汇编代码：

```shell
llc hello.bc -o hello.s
```

```assembly
	.text
	.file	"hello.c"
	.globl	main                            # -- Begin function main
	.p2align	4, 0x90
	.type	main,@function
main:                                   # @main
	.cfi_startproc
# %bb.0:
	pushq	%rax
	.cfi_def_cfa_offset 16
	movl	$.Lstr, %edi
	callq	puts@PLT
	xorl	%eax, %eax
	popq	%rcx
	.cfi_def_cfa_offset 8
	retq
.Lfunc_end0:
	.size	main, .Lfunc_end0-main
	.cfi_endproc
                                        # -- End function
	.type	.Lstr,@object                   # @str
	.section	.rodata.str1.1,"aMS",@progbits,1
.Lstr:
	.asciz	"Hello, LLVM."
	.size	.Lstr, 13

	.ident	"clang version 18.1.8"
	.section	".note.GNU-stack","",@progbits
```

可以使用`gcc`编译器将这段汇编代码转换为可执行文件。

下面这张图展示了LLVM中各种文件的转换关系和使用的对应工具。

![image-20240820221413791](./llvm-naive-0/image-20240820221413791.png)

## LLVM IR

LLVM中间语言是一个基于静态单赋值的，类型安全的低级别中间表示形式。中间语言一般情况下有三种表示形式：内存中的数据结构、便于JIT编译器解析执行的字节码形式和人类可读的文本形式。

> **良好定义（Well formed）** 的中间语言：中间语言可以是在语义上没有问题的，但是并不是良好定义的。例如：
>
> ```
> %x = add i32 1, %x
> ```
>
> 这段IR在语法上没有任何问题，但是变量`%x`的定义并不在所有的使用之前。
>
> LLVM提供了一个Pass在运行所有的优化之前验证输入的IR是否是良好定义的。

### 语法

#### 标识符

LLVM的标识符有两种基本的类型，全局符号和本地符号。全局符号以`@`开头，本地符号以`%`开头。另外标识符的命名也有着三种不同的规则：

- 命名变量：有字符开头的字符串作为名称
- 未命名变量：由无符号整数作为名称
- 常量：在常量章节介绍

LLVM中的关键词同其他语言中的关键词也非常类似，例如对于不同操作的关键词`add`、`bitcast`和`ret`等，对于各种基元类型的`void`、`i32`等。

下面是一段将命名变量`%X`乘以8的代码：

```
%result = mul i32 %X, 8
```

可以对这种代码优化为如下的代码：

```
%0 = add i32 %X, %X           ; yields i32:%0
%1 = add i32 %0, %0           ; yields i32:%1
%result = add i32 %1, %1
```

从这段优化之后的代码中我们可以发现：

- 使用`;`字符开始一个单行的注释。
- 未命名变量（临时变量）是在在计算中结果还没有传递给一个命名变量时存储中间结果使用的。
- 默认情况下未命名变量的名称就是一个从0开始的自增变量。

#### 常量字符串

使用双引号定义一个常量字符串。使用`\`定义字符串中的转义字符：

- `\\`表示一个实际的`\`字符。
- `\`后面接两个十六进制的整数表示对应的字符，例如`\00`表示一个空字符。

### 高级别表示

这里使用LLVM在Rust中的高级别封装[inkwell](https://github.com/TheDan64/inkwell)示范如何使用LLVM IR编写一个简单的程序。在这个程序中涉及到LLVM几个重要的基础概念。

- `context`，LLVM中的上下文。这个对象中保存了LLVM IR中的一些重要全局状态，借助这个变量我们可以方便的将LLVM并行运行起来。
- `module`，LLVM的模块，一个编译的单元，可以包含各种函数和全局变量。
- `type`，LLVM中对于数据类型的抽象，通过基础类型的各种组合可以构建出更复杂的类型，例如函数和结构体。
- `function`：LLVM中的函数，函数需要通过函数类型和名称来定义，函数类型需要通过输入参数类型和返回类型来定义。函数中可以通过附加上基本块来定义函数的实现。
- `basic_block`：LLVM中的基本块，组成控制流的基本单元，中间包含从上到下依次执行的一系列指令序列。

```rust			
	fn main() -> Result<(), Box<dyn Error>> {
    let context = Context::create();
    let module = context.create_module("main");
    
    let puts_function_type = context.i32_type().fn_type(
        &[context.ptr_type(AddressSpace::default()).into()], false
    );
    let puts_function = module.add_function("puts", puts_function_type, None);
    
    let main_function_type = context.i32_type().fn_type(&[], false);
    let main_function = module.add_function("main", main_function_type, None);
    let entry_basic_block = context.append_basic_block(main_function, "entry");

    let builder = context.create_builder();
    builder.position_at_end(entry_basic_block);

    let hello_string = builder.build_global_string_ptr("Hello, LLVM!\n", "str")?;

    builder.build_call(puts_function, &[hello_string.as_pointer_value().into()], "")?;
    builder.build_return(Some(&context.i32_type().const_int(0, false)))?;

    module.print_to_file("hello.ll")?;
    println!("{}", module.print_to_string());
    Ok(())
}
```

执行上面的Rust代码，可以得到一段生成的LLVM IR代码：

```llvm	
; ModuleID = 'main'
source_filename = "main"

@str = private unnamed_addr constant [13 x i8] c"Hello, LLVM!\00", align 1
@format = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1

declare i32 @puts(ptr)

declare i32 @printf(ptr, i32, ...)

define i32 @main() {
entry:
  %0 = call i32 @puts(ptr @str)
  %1 = call i32 (ptr, i32, ...) @printf(ptr @format, i32 3)
  ret i32 0
}
```

使用`lli`解释器可以直接运行这段代码：

![image-20240825171858276](./llvm-naive-0/image-20240825171858276.png)

