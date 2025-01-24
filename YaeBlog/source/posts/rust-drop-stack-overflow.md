---
title: 内存栈被Rust自动生成的Drop函数塞满了
date: 2024-11-05T20:36:07.3930374+08:00
tags:
- Rust
- 技术笔记
---

这辈子就是被Rust编译器害了.jpg

<!--more-->

最近在用Rust写一个[Sysy](https://gitlab.eduxiji.net/csc1/nscscc/compiler2022/-/blob/master/SysY2022%E8%AF%AD%E8%A8%80%E5%AE%9A%E4%B9%89-V1.pdf)语言的编译器，但是在实现完语法分析之后针对官方提供的测试用例进行测试时遇到的一个抽象的栈溢出报错。

事情是这样的，当我实现完`Sysy`语言的语法分析器并编写了一些白盒测试用例之后，我便打算将官方提供的100个测试用例作为输入运行看看能不能**正常**的解析成抽象语法树（显然不可能手动检查生成的抽象语法树是否正确）。我首先在`main.rs`里面实现了读取所有的`.sy`文件，进行词法分析和语法分析的逻辑，程序在这里这正常的识别了大多数的输入文件，在一些浮点数的输入上还存在一些问题。于是我便打算将这些逻辑重构到一个Rust的集成测试中，方便在CI中使用`cargo test`进行运行测试。但是在重构完成之后使用`cargo test`进行运行时我去遇到了如下的运行时错误。

![image-20241105181144993](./rust-drop-stack-overflow/image-20241105181144993.png)

看到这个报错的第一瞬间，我怀疑是因为`cargo test`和`cargo run`的运行环境不同，导致测试程序读取到了其他其实不是`sysy`程序但是以`.sy`结尾的文件，而恰好这个文件又能被解析，使得解析器组合子工作的过程中调用链太长而导致栈溢出，于是我在`RustRover`中打断点调试运行，却发现程序正确的读取到输入文件。这就奇怪了，我于是让程序继续运行到报错，看看报错时候程序的调用栈是被什么东西填满了，然后发现程序的调用栈长这样：

![image-20241105181612954](./rust-drop-stack-overflow/image-20241105181612954.png)

并不是我程序中代码的调用太深导致的，而是Rust编译器自动生成的`drop`函数导致的。于是尝试看看调用栈的底部，看看是在读取什么输入数据，`drop`什么神仙数据结构的时候发生的。调试器很快告诉我们，`drop`的数据结构是抽象语法树中的二元表达式，而此时的输入代码则如下图所示，而且图中的代码重复了400行。

![image-20241105182036975](./rust-drop-stack-overflow/image-20241105182036975.png)

我已经能想象到那棵高耸如云的抽象语法树了。

虽然找到了问题的根源，但是还有一个问题没有解决：为什么在`main.rs`上运行的时候程序并不会出现问题，但是在`cargo test`上运行时却会遇到栈溢出的问题？

这个问题其实在[Rust语言圣经](https://course.rs/compiler/pitfalls/stack-overflow.html)中就有记载，不过问题的背景略有不同。Rust语言圣经中导致栈溢出的问题是尝试在栈上分配一个4MB的超大数组，但是出现问题的原因是一致的。在`main.rs`中运行程序时，如果不使用多线程，那么程序的所有逻辑将运行在`main`线程上，这个线程在Linux下的栈大小是8MB，而当使用Rust提供的集成测试时，Rust为了实现测试的并行运行，会把所有的测试都运行在新线程上，这就导致在使用`cargo test`时程序会出现问题。

解决这个问题的方案可以是设置环境变量设置创建新线程的栈大小：`RUST_MIN_STACK=8388608 cargo test`，但是这种方法总是不太优雅。合理的解决方案是重写造成问题数据结构的`drop`方法，避免使用编译器自动生成的`drop`方法。这里我提供的抽象语法树`drop`方法如下所示。通过广度优先搜索的方式遍历语法树，手动释放一些可能子节点可能较多的语法树节点（其中释放内存的方式来自于[reddit](https://www.reddit.com/r/rust/comments/x97a4a/stack_overflow_during_drop_of_huge_abstract/)）。

```rust
fn collect_node_rubbishes(
    rubbish: &mut Vec<Rc<RefCell<SyntaxNode>>>,
    node_type: &mut SyntaxNodeType,
) {
    match node_type {
        SyntaxNodeType::BinaryExpression(node) => {
            rubbish.push(std::mem::replace(&mut node.left, SyntaxNode::unit()));
            rubbish.push(std::mem::replace(&mut node.right, SyntaxNode::unit()));
        }
        SyntaxNodeType::Block(nodes) => {
            while let Some(child) = nodes.pop() {
                rubbish.push(child);
            }
        }
        _ => {}
    }
}

impl Drop for SyntaxNode {
    fn drop(&mut self) {
        let mut rubbish = Vec::new();
        collect_node_rubbishes(&mut rubbish, &mut self.node_type);

        while let Some(node) = rubbish.pop() {
            collect_node_rubbishes(&mut rubbish, &mut node.borrow_mut().node_type);
        }
    }
}
```

