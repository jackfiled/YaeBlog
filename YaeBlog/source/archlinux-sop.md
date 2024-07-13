---
title: 日用Linux挑战第五篇 ArchLinux标准安装流程
tags:
  - Linux
  - 技术笔记
---

标准化ArchLinux的安装流程是日用Linux道路上一个重要的里程碑。

<!--more-->

## 概述

本安装流程的目的在于规范化一台设备安装ArchLinux的过程，通过明确各个步骤选择的软件和格式确保不同的设备之间有着良好的互操作性，减少维护各种不同安装配置的ArchLinux示例的心智负担。

具体而言，本标准操作流程主要试图标准化如下几个问题：

- 进行硬盘分区时使用什么格式？分成几个区？
- 使用`pacstrap`安装时，应该安装哪些软件包？
- 在编辑`locale.gen`时，应该选择哪些`locale`？
- 主机名是否应该设计统一的规则进行设置？
- `boot loader`的选择和配置？
- 图形化环境应该如何选择？

同时，本标准操作流程亦是一份ArchLinux系统安装指南，但是流程中将更多的注重于应该做什么，而不是解释为什么要这样做。

## 在安装开始之前

在启动`Live CD`环境之后，首先进行如下的操作。

验证启动模式是否为64位：

```shell
cat /sys/firmware/efi/fw_platform_size
```

连接互联网：

- 如果是有线网应该可以自动进行连接；
- 如果是无线网，使用`iwctl`进行连接。

测试到互联网的连接通畅之后，同步系统时钟：

```shell
timedatectl
```

### 硬盘格式化

首先使用`fdisk`工具对需要安装系统的磁盘进行分区，系统一般情况下使用`UEFI`进行启动，磁盘使用`GPT`分区表。各分区的参数如下表所示。

| 挂载的位置 | 大小           | 分区的类型       | 分区后的设备号（示例） |
| ---------- | -------------- | ---------------- | ---------------------- |
| /boot      | 1G             | EFI System       | /dev/nvme0n1p1         |
| /          | 磁盘余下的大小 | Linux root (x86) | /dev/nvme0n1p2         |

对分区好的磁盘进行格式化。

```shell
mkfs.fat -F 32 /dev/nvme0n1p1
mkfs.btrfs -L ArchLinux /dev/nvme0n1p2
```

将`/dev/nvme0n1p2`挂载到`/mnt`目录中，对`btrfs`文件系统进行顶级`subvolume`的划分，具体划分如下表所示：

| subvolme名称 | 挂载的位置  | 是否打开写时复制 |
| ------------ | ----------- | ---------------- |
| @root        | /           | 是               |
| @home        | /home       | 是               |
| @swap        | /swap       | 是               |
| @var         | /var        | 否               |
| @snapshots   | /.snapshots | 是               |

完成顶级`subvolume`的划分之后，取消`/mnt`的挂载，使用`subvol`选项进行挂载：

```shell
mount --mkdir /dev/nvme0n1p2 /mnt -o subvol=@root
mount --mkdir /dev/nvme0n1p2 /mnt/home -o subvol=@home
mount --mkdir /dev/nvme0n1p2 /mnt/swap -o subvol=@swap
mount --mkdir /dev/nvme0n1p2 /mnt/var -o subvol=@var
mount --mkdir /dev/nvme0n1p2 /mnt/.snapshots -o subvol=@snapshots
```

设置一个和内存大小相同的`swap`文件，下面的指令假设机器的内存大小为16G：

```shell
btrfs filesystem mkswapfile --size 16g --uuid clear /swap/swapfile
swapon /swap/swapfile
```

挂载`EFI`分区：

```shell
mount --mkdir /dev/nvme0n1p1 /mnt/boot
```

## 安装系统

首先是选择合适的镜像源，这里推荐的几个镜像源为：

```
Server = https://mirrors.bupt.edu.cn/archlinux/$repo/os/$arch
Server = https://mirrors.bfsu.edu.cn/archlinux/$repo/os/$arch
Server = https://mirrors.tuna.tsinghua.edu.cn/archlinux/$repo/os/$arch
Server = https://mirrors.cernet.edu.cn/archlinux/$repo/os/$arch
```

同时调整一些`pacman.conf`中的设置，打开输出颜色，将并行下载设置为8。

使用`pacstrap`安装需要的软件包，具体的软件包列表如下：

| 软件包名称     | 用途                |
| -------------- | ------------------- |
| base           | 基础软件包          |
| base-devel     | 基础开发软件包      |
| linux          | 系统内核            |
| linux-firmware | 系统固件            |
| btrfs-progs    | `btrfs`文件系统工具 |
| networkmanager | 网络连接工具        |
| vim            | 文本编辑器          |

安装的指令如下：

```shell
pacstrap -K /mnt base base-devel linux linux-fireware btrfs-progs networmanager vim
```

## 配置系统

首先生成`fstab`：

```shell
genfstab -U /mnt >> /mnt/etc/fstab
```

注意生成之后验证生成文件。

使用`chroot`进入安装的新系统：

```shell
arch-chroot /mnt
```

配置系统时间和硬件时间：

```shell
ln -sf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime
hwclock --systohc
```

配置系统的本地化，编辑`/etc/locale.gen`，取消下面这些区域设置：

| 名称              | 解释           |
| ----------------- | -------------- |
| en_US.UTF-8 UTF-8 | 英语，美国     |
| en_GB.UTF-8 UTF-8 | 英语，大不列颠 |
| zh-CN.UTF-8 UTF-8 | 中文，中国     |

编辑好之后，使用`locale-gen`生成本地化选项。配置系统的默认本地化选项，创建`/etc/locale.conf`：

```shell
LANG=en_GB.UTF-8
```

编辑`/etc/hostname`，在文件中填入系统的主机名，系统中的主机名一般为当前主机的型号。

使用`passwd`设置`root`用户的密码。

安装`grub`。

```shell
pacman -S grub efibootmgr
grub-install --target=x86_64-efi --efi-directory=/boot --bootloader-id=GRUB
grub-mkconfig -o /boot/grub/grub.cfg
```

退出`chroot`的系统，取消挂载硬盘，重启系统退出`Live CD`环境：

```shell
umount -R /mnt
reboot
```

## 安装图形化界面

新安装的系统启动之后，进行普通用户的创建和图形化界面的安装。

首先创建普通用户并添加到`sudo`用户组中。

```shell
useradd -m ricardo
pacman -S sudo
usermod -aG wheel ricardo
```

使用新创建的用户登录系统，在新用户的目录下创建一些不需要进行快照的`subvolume`：

```shell
btrfs subvolume create .cache
btrfs subvolume create .wine
chattr +C .cache
chattr +C .wine
```

安装`plasma`图形化界面。

```shell
sudo pacman -S plasma sddm
sudo systemctl enable sddm.service
```

## 硬件相关联的操作

### CPU

按照CPU的厂商，分别安装`intel-ucode`或者`amd-ucode`两个微码文件。

### GPU

按照GPU的厂商，分别安装对应的驱动程序。

同样的，在使用`NVIDIA`显卡和`Wayland`显示协议，仍然需要配置对应的驱动参数：

```
options nvidia_drm modeset=1 fbdev=1
```

