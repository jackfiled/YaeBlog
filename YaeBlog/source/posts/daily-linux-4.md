---
title: 日用Linux挑战 第4篇 新的开始
tags:
  - Linux
  - 杂谈
date: 2024/03/09 14:00:00
---

小步快跑，面向未来。

<!--more-->

在上一次更新“日用Linux挑战”之后的六个月时间中，我又重新在我的两台电脑上安装了`ArchLinux`。在这一轮新的系统安装中引入了不少令人激动的新技术。

## BTRFS

在所有的新技术中最激动人心便是新的文件系统`Btrfs`，作为“面向Linux的现代写时复制文件系统”，`Btrfs`致力于在实现高级特性的同时保证对错误的宽容和使用与维护的便利性。

相较于古老但是稳定的`Ext4`文件系统，`Btrfs`对我来说最大的好处便是可以零成本的创建快照，便于在出现错误的时候及时回滚或者直接重装系统。因此，为了方便快照的生成和回滚，我在安装系统时使用**扁平化**的子分区划分方法：即尽力避免出现嵌套的子分区，所有需要快照的分区都处在`/`目录之下：

![Screenshot_20240309_115143](daily-linux-4/Screenshot_20240309_115143.png)

- `@`为根分区，挂载在`/`目录之下，打开写时复制；
- `@home`为家目录分区，挂载在`/home`目录之下，打开写时复制；
- `@swap`为交换分区;
- `@var`挂载在`/var`目录之下，鉴于这下面的文件大多数为生成的数据文件，关闭写时复制；
- `@snapshots`是存储快照的子分区，挂载在`/.snapshots`目录之下，打开写时复制。

在一般情况下上每天对`@home`分区进行快照，存放在`/.snapshots`目录下，每周将当天备份的数据备份到移动硬盘中。

### 使用BTRFS的一些小提示

由于`docker`使用的默认文件系统是`overlay2`文件系统，也是一个支持写时复制的文件系统，而在写时复制文件系统`BTRFS`上再叠一层写时复制文件系统显然不好，而且`docker`官方也提供了对于`btrfs`文件系统的[支持](https://docs.docker.com/storage/storagedriver/btrfs-driver/)，因此需要修改`docker`使用的文件系统，按照官方文档操作即可。

对于一些常常修改的文件夹例如`.cache`，可以关闭写时复制。

在通过`SSH`将数据备份到远程服务器时，可以使用`zstd`压缩之后再发送，可以大幅减少需要传送的数据量：

```shell
sudo btrfs send /.snapshots/home@20240225 | zstd | ssh root@remote "zstd -d | btrfs receive /path-to-backup"
```

## Wayland

算起来，我已经和`Wayland`显示协议相爱相杀了整整一年了，从`KDE plasma X`到`Hyprland`，再尝试小众的`labwc`，最后回到了`KDE plasma X`。而在2024年2月29日`KDE plasma`释出6.0版本，将`Wayland`作为默认的显示协议，我也在第一时间更新了版本并使用`wayland`显示协议。现在，我可以比较确定的说，`Wayland`目前已经达到可用的水平了，而且我还是使用`RTX 3060`显卡。

![image-20240309130329784](daily-linux-4/image-20240309130329784.png)

不过相较于`AMDGPU`可以开箱即用，使用`NVIDIA`启动需要配置如下的模块参数：

```
options nvidia_drm modeset=1 fbdev=1
```

同时`NVIDIA`驱动的版本在`550`以上。如果`NVIDIA`驱动的版本`< 550`，那么就需要在内核参数中配置`nvidia_drm.modeset=1`，因为在之前的`NVIDIA`版本驱动中对于`simpledrm`的支持还不完善，需要通过内核参数禁用，~~而且这是一个`ArchLinux`提供的hack~~。

对于输入法，在使用`fcitx5`输入法并通过`Virutal Keyboard`启动输入法之后，在几乎所有的`Wayland`应用程序中都能够正常的唤起输入法，包括各种基于`chromium`的浏览器和`Electron`应用，不过需要在启动应用时传递如下的参数：

```
--enable-features=UseOzonePlatform 
--ozone-platform=wayland 
--enable-wayland-ime
```

不过`XWayland`应用程序在使用`NVIDIA`驱动时会存在一个神奇的**同步失败**问题，表现为在`xwayland`中部分控件闪烁，交替显示更新前和更新后的帧，而且这个问题几乎不能被截屏抓到，具体可以见`freedesktop`上的这个[issue](https://gitlab.freedesktop.org/xorg/xserver/-/issues/1317)。虽然这个议题下面有着很长的讨论，还是建议大家完整的看一遍，里面甚至还有：

![image-20240309131750535](daily-linux-4/image-20240309131750535.png)

省流：这个议题讨论了在`xserver`中提供显式同步的协议原语，方便图形驱动程序知道什么时候渲染的帧发生了变化。因此这并不是一个`NVIDIA`驱动程序的问题，而是需要将`Linux`显示协议栈从隐式同步迁移到显式同步。但是相关的工作还在开发过程中，因此解决方法有两个：

- 避免使用`xwayland`应用；
- 自行编译该合并请求[!967](https://gitlab.freedesktop.org/xorg/xserver/-/merge_requests/967)，或者使用这个[xorg-xwayland-explicit-sync-git](https://aur.archlinux.org/packages/xorg-xwayland-explicit-sync-git)。

## 安装双系统获得的新知识

在这轮安装过程中，为了保证在极端情况下的可用性，我都是选择了双系统`Windows`和`ArchLinux`进行安装。但是这也带来了一个问题：`Windows`安装的过程中创建的`EFI`分区只有100M的空间，而现在`Linux`内核的大小一般是`14M`左右，而`initramfs`的大小来到了`24M`上下，再加上一个更大的备用`initramfs`，~~装不下，怎么想也装不下~~，分分钟撑爆`/boot`分区。

于是，我就在`Arch Wiki`上学到一条新知识：

![image-20240309134847166](daily-linux-4/image-20240309134847166.png)

原来`efi`分区其实只用放`grub`，，，

![img](daily-linux-4/cfd17cff0701a8e8c69fecf247f17fc1-1709963611271-2.jpg)

