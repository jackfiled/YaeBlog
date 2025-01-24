---
title: Rust中将子特征的特征对象转换为父特征的特征对象
date: 2024-12-15T15:49:33.5102602+08:00
tags:
- Rust
- 技术笔记
---

这辈子就是被Rust编译器害了.jpg

<!--more-->

## 背景

还是在开发同[上一篇](https://rrricardo.top/blog/essays/rust-drop-stack-overflow)相同的项目——一个编译器。在编写语法分析构建抽象语法树的过程中设计了这样一种抽象语法树的数据结构：每一个抽象语法树节点都实现了一个基础的语法树节点特征`SyntaxNode`，同时每个可以参加运算、有返回类型的语法树节点都需要实现`ExpressionSyntaxNode`特征，该特征是`SyntaxNode`特征的子特征。因此，从特征对象`Rc<dyn ExpressionSyntaxNode>`到特征对象`Rc<dyn SyntaxNode>`的转换就成为在语法树构建过程中必然会遇到的问题。

这种数据结构的设计就是一个非常具有面向对象特色的设计思路，但是我们伟大的Rust（目前）却不支持这种特征对象的转换。这种转换在Rust语言内部称作`trait-upcasting`，已经在[RFC3324](https://github.com/rust-lang/rfcs/blob/master/text/3324-dyn-upcasting.md)中完成了定义，但其的实现从2021年开始一直到现在都处于`unstable`的状态，需要在`nightly`版本的编译器中开启`#！[feature(trait_upcasting)]`。具体来说，这个特点允许当特征`Bar` 是另一个特征`Foo`的子特征`Bar : Foo`时是一个特征对象`dyn Bar`被转换为特征对象`dyn Foo`。

## 当前条件下的实现方法

虽然我们可以在使用`nightly`编译器的条件下使用`feature`开关抢先实用这个功能，但是应该没有人会在生产环境下使用`nightly`编译器罢。

所以我们需要一个在当前环境下可以使用的解决方法。解决的思路是设计一个类型转换的辅助特征`CastHelper`，这个特征就提供了需要的转换方法：

```rust
trait CastHelper {
    fn cast(&self) -> &dyn Foo;
}
```

然后在定义`Bar`特征时，让`CastHelper`也成为`Bar`特征的超特征。

```rust
trait Bar : Foo + CastHelper {
    // Other method.
}
```

接下来使用泛型的方式为所有实现了`Bar` 的结构体实现`CastHelper`：

```rust
impl<T> CastHelper for T
where
	T : Bar + 'static
{
    fn cast(&self) -> &dyn Foo {
        self as _
    }
}
```

在`CastHelper`中也可以定义到`Rc<dyn Foo>`和`Box<dyn Foo>`等特征对象的转换。

所有的实现代码如下：

```rust
trait Foo {}
trait Bar: Foo + CastHelper {}

trait CastHelper {
    fn cast(&self) -> &dyn Foo;
}

impl<T> CastHelper for T
where
	T : Bar + 'static
{
    fn cast(&self) -> &dyn Foo {
        self as _
    }
}
```

