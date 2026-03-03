---
title: 从零开始的Linux发行版生活
date: 2025-05-27T14:22:45.9208348+08:00
tags:
- Linux
- 技术笔记
---


 总有些时候我们需要自己组装Linux操作系统，比如交叉编译、嵌入式开发和可信执行环境开发等等场景。本文便介绍如何使用Arch Linux作为基础在`riscv`架构上组装操作系统并使用QEMU运行。

<!--more-->

## 初始化根文件系统

`rootfs`是Linux系统中除了内核之外的其他文件的总和，例如`/usr`和`/etc`中重要的系统文件均属于`rootfs`的范围。在进行Linux系统的开发时，同一架构的`rootfs`之间基本上可以互换，例如可以把Arch Linux的`rootfs`替换到`ubuntu`系统中，而内核由于硬件的敏感性，通常需要使用特定厂商提供的内核（在更改合入upstream之前）。

> 实际上，除了各个发行版对于内核的修改，各个发行版之间主要的不同就是rootfs的不同。

首先创建一个`rootfs`文件夹并修改权限为`root`。

```bash
mkdir rootfs
sudo chown root:root ./rootfs
```

然后使用`pacstrap`这个`pacman`的初始化工具在`rootfs`安装`base`软件包，最好也顺便装一个`vim`。

```bash
sudo pacstrap \
	-C /usr/share/devtools/pacman.conf.d/extra-riscv64.conf
	-M ./rootfs \
	base vim
```

`extra-riscv64.conf`是在`archlinuxcn/devtools-riscv64`软件包中提供的便利工具，其中包括了`archriscv`该移植的`pacman.conf`文件，当然一般推荐修改一下该文件的镜像站点，以提高安装的速度。

然后清理一下`pacman`的缓存文件，缩小`rootfs`的大小，尤其是考虑到后面因为各种操作失误可能会反复解压`rootfs`文件。

```bash
sudo pacman  \
	--sysroot ./rootfs \
	--sync --clean --clean
```

然后设置一下该`rootfs`的`root`账号密码：

```bash
sudo usermod --root $(realpath ./rootfs) --password $(openssl passwd -6 "$password") root
```

就可以将`rootfs`打包为压缩包文件备用了。

```bash
sudo bsdtar --create \
    --auto-compress --options "compression-level=9"\
    --xattrs --acls\
    -f archriscv-rootfs.tar.zst -C rootfs/ .
```

## 初始化虚拟机镜像

首先，创建一个`qcow2`格式的QEMU虚拟机磁盘镜像：

```bash
qemu-img create -f qcow2 archriscv.img 10G
```

其中磁盘的大小可以自行定义。

为了能够像正常的磁盘一样进行读写，需要将该文件映射到一个块设备，而这通过`qemu-nbd`程序实现。首先需要加载该程序需要使用的内核驱动程序：

```bash
sudo modprobe nbd max_part=8
```

命令中的`max_part`指定了最多能够挂载的块设备（文件）个数。然后将该文件虚拟化为一个块设备：

```bash
sudo qemu-nbd -c /dev/nbd0 archriscv.img
```

挂载完毕之后就可以进行初始化虚拟机磁盘镜像的工作了。初始化虚拟机镜像主要涉及到如下几步：

- 格式化磁盘
- 安装内核
- 设置引导程序

其中格式化磁盘和后续需要使用的启动引导方式有关系，当使用U-boot这一常用的嵌入式引导系统进行引导时，只需要将磁盘格式化为单个分区即可，只需要在该分区中设置`extlinux/extlinux.conf`文件，至于磁盘的分区表格式是`GPT`还是`MBR`无关紧要。而如果是使用UEFI引导，则需要使用`GPT`分区表，并创建一个ESP（EFI System Partition）分区。这里就以使用UEFI引导的格式化磁盘作为示例，硬盘分区如下表所示：

| 分区        | 格式  | 挂载点 | 大小       |
| ----------- | ----- | ------ | ---------- |
| /dev/nbd0p1 | FAT32 | /boot  | 512M       |
| /dev/nbd0p2 | EXT4  | /      | 余下的空间 |

在使用`fdisk`完成磁盘的分区之后，进行格式化并挂载到当前的`mnt`目录中：

```bash
sudo mkfs.fat -F 32 /dev/nbd0p1
sudo mkfs.ext4 /dev/nbd0p2
sudo mkdir mnt
sudo mount /dev/nbd0p2 mnt
sudo mkdir mnt/boot
sudo mount /dev/nbd0p1 mnt/boot
```

挂载完成之后解压上一步中备好的`rootfs`：

```bash
cd mnt
sudo bsdtar -kpxf ../archriscv.tar.zst
```

然后使用`systemd-nspawn`工具进入`rootfs`中调用`pacman`安装内核：

```bash
sudo systemd-nspawn -D mnt pacman \ 
	--nonconfirm --needed \
	-Syu linux linux-firmware
```

接下来分别介绍使用U-boot启动和使用UEFI启动的操作方法。

### 使用U-boot启动

为了使用U-boot启动，需要手动编译U-boot并打包到OpenSBI中作为QEMU启动的固件。

首先编译U-boot:

```bash
git clone --filter=blob:none -b v2025.04 https://github.com/u-boot/u-boot.git
cd u-boot
make \
	CROSS_COMPILE=riscv64-linux-gnu- \
	qemu-riscv64_smode_defconfig
./scripts/config
make \
	CROSS_COMPILE=riscv64-linux-gnu- \
	olddefconfig
make CROSS_COMPILE=riscv64-linux-gnu- -j18
```

编译好之后检查当前目录下是否存在`u-boot.bin`的固件。

然后去编译OpenSBI并将`u-boot.bin`打包进来：

```bash
git clone --filter=blob:none -b v1.6 https://github.com/riscv-software-src/opensbi.git
cd opensbi
make \
	CROSS_COMPILE=riscv64-linux-gnu- \
	PLATFORM=generic \
	FW_PAYLOAD_PATH=../u-boot/u-boot.bin -j18
```

编译好的三个启动固件应当在`./build/platform/generic/firmware`目录中：

- `fw_dynamic.bin`使用启动程序设置的地址进行跳转。
- `fw_jump.bin`跳转到一个固定的地址执行。
- `fw_payload.bin`执行编译打包的`u-boot`文件，这也是U-boot启动所需要的。

编译完成之后，在`mnt`文件中创建`/boot/extlinux/extlinux.conf`文件以告知U-boot启动Linux内核的参数：

```
menu title Arch RISC-V Boot Menu
timeout 100
default linux-fallback

label linux
    menu label Linux linux
    kernel /vmlinuz-linux
    initrd /initramfs-linux.img
    append earlyprintk rw root=UUID=903944ec-a4d3-4820-ac89-c0eac37721f9 rootwait console=ttyS0,115200 

label linux-fallback
    menu label Linux linux (fallback initramfs)
    kernel /vmlinuz-linux
    initrd /initramfs-linux-fallback.img
    append earlyprintk rw root=UUID=903944ec-a4d3-4820-ac89-c0eac37721f9 rootwait console=ttyS0,115200
```

文件中的UUID可以使用如下的指令获得：

```bash
findmnt mnt -o UUID -n
```

其中需要说明的是，文件中指定kernel和intird的时候使用的是`/`而不是`/boot`，这是因为虽然现在把该分区挂载到了`/boot`目录下，但是在U-boot进行启动时会将该分区挂载在`/`目录下，因此需要使用`/`。也是因为同样的原因，当只格式化为一个分区并只使用U-boot进行引导启动时，则需要将目录改为`/boot`。

此时即可取消挂载镜像了：

```bash
sudo umount mnt/boot
sudo umount mnt
sudo qemu-nbd -d /dev/nbd0
```

使用如下的指令即可启动虚拟机：

```bash
#!/bin/bash

qemu-system-riscv64 \
    -nographic \
    -machine virt \
    -smp 8 \
    -m 4G \
    -bios opensbi/build/platform/generic/firmware/fw_payload.bin \
    -device virtio-blk-device,drive=hd0 \
    -drive file=archriscv-1.img,format=qcow2,id=hd0,if=none \
    -object rng-random,filename=/dev/urandom,id=rng0 \
    -device virtio-rng-device,rng=rng0 \
    -monitor unix:/tmp/qemu-monitor,server,nowait
```

### 使用UEFI启动

使用UEFI启动，就需要编译对应的UEFI固件，即开源固件EDK2。

```bash
git clone -b edk2-stable202505 --recursive-submodule https://github.com/tianocore/edk2.git
export WORKSPACE=`pwd`
export GCC5_RISCV64_PREFIX=riscv64-linux-gnu-
export PACKAGES_PATH=$WORKSPACE/edk2
export EDK_TOOLS_PATH=$WORKSPACE/edk2/BaseTools
source edk2/edksetup.sh --reconfig
make -C edk2/BaseTools -j18
source edk2/edksetup.sh BaseTools
build -a RISCV64 --buildtarget RELEASE -p OvmfPkg/RiscVVirt/RiscVVirtQemu.dsc -t GCC5
```

编译之后得到的两份固件应该在`Build/RiscVVirtQemu/RELEASE_GCC5/FV`目录下：

- `RISCV_VIRT_CODE.fd`固件的代码部分。
- `RISCV_VIRT_VARS.fd`固件的数据部分，可以被UEFI工具修改。

在启动之前首先将这两个文件填充到32M的大小以符合QEMU对于`pflash`文件的大小要求：

```bash
truncate -s 32M Build/RiscVVirtQemu/RELEASE_GCC5/FV/RISCV_VIRT_CODE.fd
truncate -s 32M Build/RiscVVirtQemu/RELEASE_GCC5/FV/RISCV_VIRT_VARS.fd
```

然后就可以使用如下的指令启动QEMU虚拟机了，这里复用U-boot中编译的OpenSBI固件，如果没有执行这一步可以选择删除下面指令中的`-bios`选项，使用QEMU自带的OpenSBI实现。

```bash
#!/bin/bash

qemu-system-riscv64  \
    -M virt,pflash0=pflash0,pflash1=pflash1,acpi=off \
    -m 4096 -smp 8  -nographic \
    -bios opensbi/build/platform/generic/firmware/fw_dynamic.bin \
    -blockdev node-name=pflash0,driver=file,read-only=on,filename=Build/RiscVVirtQemu/RELEASE_GCC5/FV/RISCV_VIRT_CODE.fd  \
    -blockdev node-name=pflash1,driver=file,filename=Build/RiscVVirtQemu/RELEASE_GCC5/FV/RISCV_VIRT_VARS.fd \
    -device virtio-blk-device,drive=hd0  \
    -drive file=archriscv-1.img,format=qcow2,id=hd0,if=none \
    -netdev user,id=n0 -device virtio-net,netdev=n0 \
    -monitor unix:/tmp/qemu-monitor,server,nowait
```

但是，这一步启动并不会进入Linux内核，这是因为还没有向UEFI注册需要启动的系统，使得UEFI可以识别到可以执行启动的磁盘。在普通的系统安装上，由于是使用安装镜像直接从UEFI启动的，在`chroot`环境中可以直接使用`grub-install`直接安装，但是在目前的`systemd-nspawn`环境中是缺少`efivarfs`等必要的文件系统的。

因此可以首先尝试在启动之后进入`UEFI Shell`之后，手动设置参数直接启动Linux内核。

![image-20250527134233659](./linux-distribution-from-zero/image-20250527134233659.webp)

进入`UEFI Shell`之后，首先选择文件系统`FS0:`，然后使用如下的指令尝试手动启动Linux内核：

```bash
\vmlinuz-linux initrd=\initramfs-linux.img  earlyprintk rw root=UUID=903944ec-a4d3-4820-ac89-c0eac37721f9 rootwait console=ttyS0,115200
```

但是可能会遇到如下的问题：

![image-20250527134421403](./linux-distribution-from-zero/image-20250527134421403.webp)

这里也尝试了使用`mkinitcpio`生成的Unified Kernel Image，放在`EFI/Linux`文件目录下，同样遇到了如下的问题：

![image-20250527134540583](./linux-distribution-from-zero/image-20250527134540583.webp)

暂时不清楚这是EDK2的问题还是这里操作的问题，至少能确定这里编译内核时是启用了`CONFIG_EFI_STUB`选项的。

因此这里使用`grub`方式尝试绕过这个问题，首先在`systemd-nswpan`环境中使用如下的指令安装`grub`，虽然会因为环境问题报错，但是手动查看可以发现安装脚本已经将`grubriscv64.efi`文件复制到`/boot/EFI/GRUB`目录了。

此时再次进入`UEFI Shell`，手动指定启动`grub`，所幸这次启动成功，此时我们再从`grub shell`中尝试启动Linux，使用的指令如下：

```bash
linux (hd0,gpt1)/vmlinuz-linux earlyprintk rw root=UUID=903944ec-a4d3-4820-ac89-c0eac37721f9 rootwait console=ttyS0,115200
initrd (hd0,gpt1)/initramfs-linux.img
boot
```

![image-20250527135748547](./linux-distribution-from-zero/image-20250527135748547.webp)

此时就可以正常的进入完成完整的安装过程了。

> 首次启动的时候推荐使用`fallback initramfs`，因为在`chroot`环境中生成的驱动可能不全。如果在使用主要的`initramfs`进行启动时遇到了无法挂载真`/`目录而进入`emergency shell`，同时在该Shell中也无法发现虚拟机的磁盘，就极有可能是系统缺少对应的驱动无法挂载。
>
> 例如在`chroot`环境中生成的`initcpio`包含如下的模块：
>
> ![image-20250325160729310](./linux-distribution-from-zero/image-20250325160729310.webp)
>
> 而在进入系统之后，重新运行`mkinitcpio`之后包含的模块如下所示：
>
> ![image-20250325161310820](./linux-distribution-from-zero/image-20250325161310820.webp)

