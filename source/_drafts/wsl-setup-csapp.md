---
title: 利用WSL设置CSAPP实验环境
tags:
  - 技术笔记
  - Linux
---

# 利用WSL设置CSAPP实验环境

### 背景

`CSAPP`这本书为自学的学生们提供了不少的`LAB`供大家在联系中提高，但是这些`LAB`的编写普遍需要一个`Linux`的实验环境，但是目前大多数人手中的环境都是`Windows`平台，没有办法原生的运行这些`LAB`。在以前的实践中，这个问题往往是通过安装虚拟机来解决的，但是现在我们有了更好的解决方案——`Windows Subsystem for Linux`，简称`WSL`。

### WSL简介

> WSL的[官方文档](https://docs.microsoft.com/zh-cn/windows/wsl)，大部分的信息都可以在官方文档中找到

WSL，官方中文翻译：适用于Linux的Windows子系统，“可以让开发人员直接在Windows上按原样运行GNU/Linux环境，且不会产生传统虚拟机或者双启动设置开销”，简而言之，这就是Windows为广大开发人员提供的一项轻量化的Linux虚拟环境。

### WSL安装

> 需要系统版本在Windows 10 2004以上

按下`Ctrl+S`组合键打开Windows搜索界面，搜索`Powershell`，使用管理员权限运行这个应用程序，在打开的界面中输入

```powershell
wsl --install
```

回车，这条命令将自动启动运行`WSL`并安装一个名叫`Ubuntu`的Linux发行版。

首次打开WSL时，Ubuntu系统会要求你创建一个账号和密码，正确的设置就可以了。

> 在终端中输入密码时，命令行中不会有任何显示
>
> 这个用户将成为此Linux系统的管理员
>
> 现在开始就得习惯命令行操作了

