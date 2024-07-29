---
title: 利用WSL设置CSAPP实验环境
tags:
  - 技术笔记
  - Linux
typora-root-url: wsl-setup-csapp
date: 2022-09-03 19:02:58
---

`CSAPP`这本书为自学的学生们提供了不少的`LAB`供大家在联系中提高，但是这些`LAB`的编写普遍需要一个`Linux`的实验环境，但是目前大多数人手中的环境都是`Windows`平台，没有办法原生的运行这些`LAB`。在以前的实践中，这个问题往往是通过安装虚拟机来解决的，但是现在我们有了更好的解决方案——`Windows Subsystem for Linux`，简称`WSL`。

<!--more-->

### WSL简介

> WSL的[官方文档](https://docs.microsoft.com/zh-cn/windows/wsl)，大部分的信息都可以在官方文档中找到

WSL，官方中文翻译：适用于Linux的Windows子系统，“可以让开发人员直接在Windows上按原样运行GNU/Linux环境，且不会产生传统虚拟机或者双启动设置开销”，简而言之，这就是Windows为广大开发人员提供的一项轻量化的Linux虚拟环境。

### WSL安装

> 需要系统版本在Windows 10 2004以上

按下`Ctrl+S`组合键打开Windows搜索界面，搜索`Powershell`，使用管理员权限运行这个应用程序，在打开的界面中输入

```powershell
wsl --install
```

回车，等待所有的进度条都走完，重新启动系统。

![设置的画面](1.png)

在重启完成之后，Ubuntu系统会自动启动并完成一系列的配置，在其中需要你为这个系统设置一个用户。输入这个用户的名称和密码即可。

> 在终端中输入密码时，命令行中不会有任何显示
>
> 这个用户将成为此Linux系统的管理员
>
> 现在开始就得习惯命令行操作了

在完成设置之后，我们就可以在任意的命令行界面（比如`cmd`, `powershell`）输入`wsl`来启动这个虚拟的Linux环境了。

### 设置一个漂亮的终端界面

在上文中我们已经完成了WSL的安装和设置，但是作为我们将常常使用的界面，这些个黑框框莫名显得有点过于简陋。我们可以从`Microsoft Store`下载`Windows Terminal`这个应用来优化终端界面的显示，还有一系列的插件和`Shell`可供我们选择，由于这个部分和本文关系不大，这里就不赘述了。

### 更换APT下载源

APT是Ubuntu系统中默认的软件包管理器，是一个很方便的软件下载安装更新卸载神器，不过唯一的问题是下载源在国外，我们需要切换为国内的镜像源。

清华的[TUNA源](https://mirrors.tuna.tsinghua.edu.cn/)是比较推荐的，使用帮助[见此](https://mirrors.tuna.tsinghua.edu.cn/help/ubuntu/)。

使用

```bash
sudo apt update
```

测试换源是否成功。

![证书错误](2.png)

如果在换源的过程中报错`Certificate verification failed`，可以将配置文件中的所有`https`更改为`http`来临时解决。

> 我平时使用[北邮镜像](https://mirrors.bupt.edu.cn/)，貌似没遇到这个问题。不过还是记录一下，~~而且在校内速度拉满~~

### 下载CSAPP的LAB资料并使用编辑

这里介绍两种下载LAB进入`WSL`中的方法。

#### 直接使用命令从网络上下载

打开你想要编写csapp的lab的位置，我这里是`~/Documents/Code/C/csapp`：

```bash
cd ~/Documents/Code/C/csapp
```

使用下列命令从csapp的官方网站上下载lab资料

```bash
$ wget http://csapp.cs.cmu.edu/3e/datalab-handout.tar
--2022-09-03 16:57:26--  http://csapp.cs.cmu.edu/3e/datalab-handout.tar
Resolving csapp.cs.cmu.edu (csapp.cs.cmu.edu)... 128.2.100.230
Connecting to csapp.cs.cmu.edu (csapp.cs.cmu.edu)|128.2.100.230|:80... connected.
HTTP request sent, awaiting response... 200 OK
Length: 1075200 (1.0M) [application/x-tar]
Saving to: ‘datalab-handout.tar’

datalab-handout.tar                   100%[=======================================================================>]   1.03M   103KB/s    in 10s

2022-09-03 16:57:37 (101 KB/s) - ‘datalab-handout.tar’ saved [1075200/1075200]

$ ls
datalab-handout.tar
```

在命令中的网址是在csapp的[官方页面](http://csapp.cs.cmu.edu/3e/labs.html)中复制粘贴下来的

#### 先下载在Windows系统中在复制粘贴进入WSL

> 在WSL中Windows系统的硬盘均会被挂在到`/mnt`文件夹下，比如C盘就会被挂载到`/mnt/c`中
>
> 不过最好不要直接在WSL中使用Windows文件路径下的文件搞开发，这个访问是靠网络实现的，性能很弱。最好复制进入WSL中之后在进行开发操作

先在Windows系统中使用浏览器将所有的lab下载到本地。

在WSL的命令行中使用

```bash
cp /mnt/c/Users/ricardo/Downloads/datalab-handout.tar ~/Documents/Code/C/csapp
```

命令的第一个路径是需要复制的文件在Windows系统中的路径，第二个是在WSL中的目标路径。

再将lab的数据压缩包下载到本地之后，使用以下的命令来解压压缩包

```bash
tar xvf datalab-handout.tar
```

来解压对应的压缩包

### 编译运行第一个实验

> 这里就以一个是作为例子来说明如何运行这些lab，其他的lab请仔细阅读压缩包中的README文件

使用`make`指令编译在实验中需要使用到的二进制文件。首先安装会被使用的软件

```bash
sudo apt install gcc make
```

使用

```bash
$ make -v
GNU Make 4.2.1
Built for x86_64-pc-linux-gnu
Copyright (C) 1988-2016 Free Software Foundation, Inc.
License GPLv3+: GNU GPL version 3 or later <http://gnu.org/licenses/gpl.html>
This is free software: you are free to change and redistribute it.
There is NO WARRANTY, to the extent permitted by law.
$ gcc -v
Using built-in specs.
COLLECT_GCC=gcc
COLLECT_LTO_WRAPPER=/usr/lib/gcc/x86_64-linux-gnu/9/lto-wrapper
OFFLOAD_TARGET_NAMES=nvptx-none:hsa
OFFLOAD_TARGET_DEFAULT=1
Target: x86_64-linux-gnu
Configured with: ../src/configure -v --with-pkgversion='Ubuntu 9.4.0-1ubuntu1~20.04.1' --with-bugurl=file:///usr/share/doc/gcc-9/README.Bugs --enable-languages=c,ada,c++,go,brig,d,fortran,objc,obj-c++,gm2 --prefix=/usr --with-gcc-major-version-only --program-suffix=-9 --program-prefix=x86_64-linux-gnu- --enable-shared --enable-linker-build-id --libexecdir=/usr/lib --without-included-gettext --enable-threads=posix --libdir=/usr/lib --enable-nls --enable-clocale=gnu --enable-libstdcxx-debug --enable-libstdcxx-time=yes --with-default-libstdcxx-abi=new --enable-gnu-unique-object --disable-vtable-verify --enable-plugin --enable-default-pie --with-system-zlib --with-target-system-zlib=auto --enable-objc-gc=auto --enable-multiarch --disable-werror --with-arch-32=i686 --with-abi=m64 --with-multilib-list=m32,m64,mx32 --enable-multilib --with-tune=generic --enable-offload-targets=nvptx-none=/build/gcc-9-Av3uEd/gcc-9-9.4.0/debian/tmp-nvptx/usr,hsa --without-cuda-driver --enable-checking=release --build=x86_64-linux-gnu --host=x86_64-linux-gnu --target=x86_64-linux-gnu
Thread model: posix
gcc version 9.4.0 (Ubuntu 9.4.0-1ubuntu1~20.04.1)
```

确认需要使用的软件安装完成。

进入解压之后的文件夹，使用`make`指令编译需要用到的二进制文件

如果遇到报错，部分头文件未找到：

![Error](3.png)

不要慌张，这是正常的。

使用下列命令来安装缺失的运行时库

```bash
sudo apt install gcc-multilib
```

在库安装完成之后，再使用`make`编译就没有问题了

```bash
$ make
gcc -O -Wall -m32 -lm -o btest bits.c btest.c decl.c tests.c
btest.c: In function ‘test_function’:
btest.c:332:23: warning: ‘arg_test_range[1]’ may be used uninitialized in this function [-Wmaybe-uninitialized]
  332 |     if (arg_test_range[1] < 1)
      |         ~~~~~~~~~~~~~~^~~
gcc -O -Wall -m32 -o fshow fshow.c
gcc -O -Wall -m32 -o ishow ishow.c
```

按照README文件中的说明，使用`btest`程序测试

```bash
$ ./btest
Score   Rating  Errors  Function
ERROR: Test bitXor(-2147483648[0x80000000],-2147483648[0x80000000]) failed...
...Gives 2[0x2]. Should be 0[0x0]
ERROR: Test tmin() failed...
...Gives 2[0x2]. Should be -2147483648[0x80000000]
ERROR: Test isTmax(-2147483648[0x80000000]) failed...
...Gives 2[0x2]. Should be 0[0x0]
ERROR: Test allOddBits(-2147483648[0x80000000]) failed...
...Gives 2[0x2]. Should be 0[0x0]
ERROR: Test negate(-2147483648[0x80000000]) failed...
...Gives 2[0x2]. Should be -2147483648[0x80000000]
ERROR: Test isAsciiDigit(-2147483648[0x80000000]) failed...
...Gives 2[0x2]. Should be 0[0x0]
ERROR: Test conditional(-2147483648[0x80000000],-2147483648[0x80000000],-2147483648[0x80000000]) failed...
...Gives 2[0x2]. Should be -2147483648[0x80000000]
ERROR: Test isLessOrEqual(-2147483648[0x80000000],-2147483648[0x80000000]) failed...
...Gives 2[0x2]. Should be 1[0x1]
ERROR: Test logicalNeg(-2147483648[0x80000000]) failed...
...Gives 2[0x2]. Should be 0[0x0]
ERROR: Test howManyBits(-2147483648[0x80000000]) failed...
...Gives 0[0x0]. Should be 32[0x20]
ERROR: Test floatScale2(0[0x0]) failed...
...Gives 2[0x2]. Should be 0[0x0]
ERROR: Test floatFloat2Int(0[0x0]) failed...
...Gives 2[0x2]. Should be 0[0x0]
ERROR: Test floatPower2(0[0x0]) failed...
...Gives 2[0x2]. Should be 1065353216[0x3f800000]
Total points: 0/36
```

测试运行完成，开始~~痛苦的~~快乐的学习吧！

