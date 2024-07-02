---
title: 初学Qt的一点小笔记
tags:
  - 技术笔记
  - C/C++
typora-root-url: qt-learning
date: 2022-07-01 14:32:39
---

# 初学Qt的一点小笔记

最近的大作业需要用 `C/C++`的技术栈实现一个图形化界面，`Qt`作为C++图形化框架久负盛名，正好借着这个写大作业的机会学习一下这个应用广泛的框架。
<!--more-->

## Qt的安装

现在比较新版本的Qt貌似都不支持下载离线安装包了，受制于网络环境，在线安装的体验并不是很好。为了比较顺利的完成安装，使用老套路——国内镜像。清华大学就提供了Qt在线安装的镜像，具体的使用说明参照镜像的这篇[帮助](https://mirrors.tuna.tsinghua.edu.cn/help/qt/)，在下载之前记得注册一个Qt的账号。

### 版本的选择

现在`Qt`最新的版本已经来到了`Qt6`，虽然现在`Qt5`仍然十分的流行，网上大部分的资料与教程都是采用`Qt5`写成的，我最后还是选选择了`Qt6`，理由主要有以下三点：

- 工程并不是很复杂，而且开发的周期很长，即使在中间遇到了一些新版本的玄学问题，我也有比较充足的时间来解决或者避开问题
- `Qt6`开始完全转向`cmake`管理项目，而我的这个大作业“恰好”就是采用`cmake`管理的
- `Qt6`是新版本，以后开发的时间还很长，稳定的开发环境还不是我现在需要的，学习的人还是应该积极的跟踪新技术（当然只是我自己的想法）

### 编译器的选择

我在`Windows`平台上进行开发，虽然在之前学习过程中`MinGW64 GCC`是我更常用的编译器，但是他毕竟是一个移植的编译器，而不是“官方”的编译器，我选择了`MSVC`作为这次项目的编译器。

>这里还有一个理由是我正在大力推进`WSL`在我日常开发中的使用，我正在把我所有的`C/C++`项目都迁移到`WSL`中进行开发，在`Windows`中我已经不保留`MinGW64`编译器了。

### 预处理的使用

在这次`Qt`开发中我用到了两个预处理器——`moc`和`uic`，前者服务于`Qt`的元对象系统(Meta Object System)，后者负责与将`Qt Designer`生成了`*.ui`文件转换为编译器可以编译的`ui_*.h`文件。这里重点说说前者，`Qt`的元对象系统是`Qt`信号和槽机制的基础，所有需要用到信号槽的类都需要继承`QObject`这个基类，同时在类里申明`Q_OBJECT`这个宏，而且，这个头文件还得被`moc`预处理器预处理为`moc_*.cpp`之后才能被编译器所编译，否则就会报`LNK2001`连接错误，提示有三个函数无法找到定义，

```
@errorMsg.obj : error LNK2001: unresolved external symbol "public: virtual struct QMetaObject const * __thiscall errorMsg::metaObject(void)const " (?metaObject@errorMsg@@UBEPBUQMetaObject@@XZ)

errorMsg.obj : error LNK2001: unresolved external symbol "public: virtual void * __thiscall errorMsg::qt_metacast(char const *)" (?qt_metacast@errorMsg@@UAEPAXPBD@Z)

errorMsg.obj : error LNK2001: unresolved external symbol "public: virtual int __thiscall errorMsg::qt_metacall(enum QMetaObject::Call,int,void * *)" (?qt_metacall@errorMsg@@UAEHW4Call@QMetaObject@@HPAPAX@Z)@
```

在我这次的工程中，需要以下设置才能让`moc`预处理器正确工作：

- 在`CMakeLists.txt`文件中需要定义`set(CMAKE_AUTOMOC ON)`
- 在包含这个头文件的地方需要将头文件的名称改为预处理之后的名称`moc_*.cpp`，如下图所示

![](1.png)

## Qt Designer

在一开始我对于这个软件的使用是比较迷茫的，没有搞明白哪些内容是在`Designer`中完成的，哪些内容是在代码中完成的，在代码中是如何访问控件的。

> 这里需要说明的是，我是用`CLion`作为IDE开发`C/C++`项目，在这里`Qt Designer`是作为外部工具存在的。

```C++
MainWindow::MainWindow(QWidget *parent) : QMainWindow(parent)
{
    ui = new Ui::MainWindow;
    ui->setupUi(this);
}
```

那个名叫`Ui`的命名空间让我迷茫了一阵子，在那个空间中也有一个和当前创建的UI类名字相同的类，我以为这是单例模式之类的高级设计模式，在研读了几篇博客，查看了`UIC`处理器生成的头文件之后，我才意识到这是两个处在不同命名空间但名字相同的类，在`Ui`命名空间中的那个类就是`*.ui`文件中定义了那个界面，可以通过这个指针来访问我们在`Qt Designer`中定义的那些控件。

## Qt Property Animation

`QPropertyAnimation`是`Qt`自己实现的一个简单实用的动画框架，在这次开发中，我使用这个框架实现了对`QGraphicsItem`这个对象的动画。这里主要的问题是，`QGraphicsItem`这个类并没有继承`QObject`，然而`QPropertyAnimation`这个动画框架所作用的对象必须是一个继承自`QObject`的对象，而且需要实现动画的属性必须注册`Q_PROPERTY`，根据文档的说明，我定义了这样的一个类：

```C++
class BusItem: public QObject, public QGraphicsPixmapItem
{
    Q_OBJECT
    Q_PROPERTY(QPointF pos READ pos WRITE setPos)
public:
    explicit BusItem(const QPixmap& pixmap);
};
```

这个类多重继承了`QObject`和`QGraphicsPixmapItem`，这样既可以被`QPropertyAnimation`所作用，也可以像正常的`QGraphicsItem`一样被添加进`QGrphicsScene`并设置各种属性。

而`Q_PROPERTY`这个宏给`Qt`的类型系统注册了一个类型为`QPointF`名叫`pos`的变量，这个变量的读通过调用`pos()`函数来实现，这个变量的写通过调用`setPos()`函数来实现，这个变量也就是我需要设计动画的变量。

> 这句代码给我的感觉像是在C#中的`set`和`get`两个访问器。

