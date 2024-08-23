---
title: 被Windows更新狂暴鸿儒的Arch Linux
tags:
  - 技术笔记
  - Linux
date: 2023-11-05 14:34:36
---

在Windows更新之后发现Linux无法启动之后面露难色的~~两人~~一人。

<!--more-->

## 故障现象

某日Windows进行了一次更新，本以为无事发生，结果第二天试图启动Linux时发现：

```
Initramfs unpacking failed: invalid magic at start of compressed archive
```

## 修复

首先判断是Windows更新橄榄了Linux启动使用的`initramfs`导致的，关于这个东西的介绍详解[Arch boot process](https://wiki.archlinux.org/title/Arch_boot_process#initramfs)。

于是尝试使用`Arch ISO`启动系统重新生成`initramfs`解决。

在`arch-chroot`之后使用`mkinitramfs -P`试图重新生成`initramfs`，但是在重启之后仍然出现类似的提示。遂求助伟大的搜索引擎。

发现搜索引擎给出了相同的解决方案，，，这时我已经开始慌乱了。再次进入`Arch ISO`打包`home`目录为重装做准备。

有帖子分析说Windows更新可以会把`/boot`的分区格式给橄榄，可以尝试使用`fsck`进行修复。

进入`Arch ISO`，使用

```shell
fsck -r /dev/nvmen0p1
```

对分区进行修复，没想到还扫描出现错误：

```
There are differences between boot sector and its backup.
This is mostly harmless.
```

（具体提示的错误bit我给忘记了）

我选择使用`Copy backup to original`来修复这个错误。还出现了一个

```
two or more files share the same cluseters
```

的错误，冲突的文件是`amd-ucode.img`和`bootmgfw.efi.mui`文件，我也选择了修复。

我又在网上翻到一篇帖子说可以试试`pacman`更新系统和重新安装`linux`包，再想到这两天一直在`Windows`下写文档，确实可以试试，于是就：

```shell
pacman -Syu
pacman -Sy linux
```

回来吧，我的Linux，，，

## 总结分析

因为在第二次进入`Arch ISO`中我同时使用了`fsck`和`pacman`两个工具同时进行修复，因此我无法判断具体是哪个工具修复了这个问题，不过鉴于这次更新也没有安装新的内核版本，而且我之间也已经手动执行过`mkinitramfs -P`。

在加上在互联网上已经有大量关于Windows更新可能会破坏启动分区，尤其是这次的Windows更新还是一个较大的版本更新，因此我认为就是Windows干的好事，微软你不得house，，，

## Update at 2023-11-12

今天早上爬起来一看，乐，Linux又启动失败了。

这波我实在是蚌埠住了，直接把Windows干掉算了，，，，

得到的教训就是如果要装双系统，最好在两块不同的硬盘上安装系统，这样Windows就没有办法对于Linux那边的`boot`分区上下其手了。

