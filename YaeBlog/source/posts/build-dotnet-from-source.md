---
title: 交叉编译.NET到RISC-V平台
date: 2024-08-25T15:41:05.9519941+08:00
tags:
- dotnet
- 技术笔记
---



我们编译是这样的，在本平台上编译只要敲三条命令就好了，而交叉编译要考虑的就很多了。

<!--more-->

这次我们打算在`x86_64`平台上交叉编译`.NET`到`riscv64`平台上。

首先从相关的[进度跟踪页面](https://github.com/dotnet/runtime/issues/84834)显示，.NET移植到RISC-V的进度还远远没有完成，但是在整个SDK中除了AOT编译器的部分都可以在RISC-V平台上编译了。

## 环境准备

我们构建的环境是Arch Linux，因此依赖包的安装使用`pacman`进行。综合[.NET官方文档](https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/linux-requirements.md)给出的信息和Arch Linux官方打包的脚本，所需要安装的软件包如下：

| 包名          | 备注                                                         |
| ------------- | ------------------------------------------------------------ |
| bash          |                                                              |
| clang         |                                                              |
| lld           |                                                              |
| cmake         |                                                              |
| git           |                                                              |
| icu           | 第一次看见这个名词就想吐槽，谁TM想得到重症监护室会是一个全球化支持库，， |
| inetutils     | 常见的网络工具库，官方文档没有但是构建脚本有                 |
| krb5          | 一个网络通信认证库？不懂                                     |
| libgit2       |                                                              |
| libunwind     | 解析程序运行堆栈的魔法工具                                   |
| libxml2       |                                                              |
| lldb          |                                                              |
| llvm          |                                                              |
| lttng-ust2.12 | 又是一个跟踪运行的魔法工具                                   |
| openssl       |                                                              |
| systemd       |                                                              |
| zlib          |                                                              |

### 交叉编译工具链

在正式开始编译.NET之前，先学习如何搭建一套C/C++的交叉编译工具链。

通常一份GNU工具链只能针对一个平台进行编译，但是LLVM工具链是一套先天的交叉编译工具链，例如对于`llc`工具，使用`llc --version`命令可以看见该编译器可以生成多种目标平台上的汇编代码：

![image-20240824120646587](./build-dotnet-from-source/image-20240824120646587.webp)

在使用`clang++`时加上`--target=<triple>`指定目标三元组就可以进行交叉编译。

但是直接使用`clang++ --target=riscv64-linux-gnu hello.cpp -o hello`时会爆出一个奇怪的找不到头文件错误：

```cpp
// File: hello.cpp
#include <iostream>

int main()
{
    std::cout << "Hello, world!" << std::endl;
    return 0;
}
```

![image-20240824121425007](./build-dotnet-from-source/image-20240824121425007.webp)

看样子交叉编译也不是开箱即用的。最开始我们猜想系统提供的LLVM工具链没有被配置为交叉编译，因此尝试在本地自行编译一套LLVM工具链。

首先从[Github Release](https://github.com/llvm/llvm-project/releases)上下载最新的`llvm-project`源代码并解压到本地文件夹中。这里126M的压缩文件可以解压出一个1.8G大小的源代码文件夹。创建一个`build`文件夹，在该文件夹使用如下的配置进行编译，在配置中使用`LLVM_TARGETS_TO_BUILD`选择启用`X86`和`RISCV`的支持。

```bash
cmake ../llvm-project.src/llvm \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_C_COMPILER=clang \
        -DCMAKE_CXX_COMPILER=clang++ \
        -DLLVM_TARGETS_TO_BUILD="X86;RISCV" \
        -DLLVM_ENABLE_PROJECTS="clang;lld;clang-tools-extra"
 make
 sudo make install
```

编译之后的成果会安装到`/usr/local/`目录下，而在`$PATH`环境变量中`/usr/local`位置将在`/usr`目录之前，因此调用时将会优先调用我们自行编译的LLVM工具链，而不是系统中安装的LLVM工具链。

![image-20240824134158262](./build-dotnet-from-source/image-20240824134158262.webp)

但是使用这套编译工具链仍然会爆出和之前一样的问题。说明这并不是系统安装LLVM工具链的问题。仔细一想也确实，这里提示找不到对应的头文件应该是找不到RISC-V架构之下的头文件——这里的也是交叉编译的主要问题所在：虽然LLVM工具链宣称自己是原生支持交叉编译的，但是没人宣称说标准库和头文件是原生的。这里我们就需要一个根文件系统来提供这些头文件和各种库文件。

### 生成根文件系统

在.NET的构建文档中提供了一个自动生成头文件的脚本，但是这个脚本似乎强依赖某个U开头的发行版，身为Arch神教信徒的我似乎没有办法使用。直接使用预构建好的镜像又屏蔽了太多的技术细节，感觉也不太好。因此打算尝试使用[arch-riscv](https://mirror.iscas.ac.cn/archriscv/)提供的移植Arch Linux系统作为根文件系统。

首先使用移植之后的根文件系统构建一个`archriscv`镜像：

```Dockerfile
FROM archriscv AS bootstrap

COPY etc /rootfs
COPY bootstrap/pacstrap-docker /usr/local/bin/
RUN pacstrap-docker /rootfs base
RUN rm /rootfs/var/lib/pacman/sync/*

FROM scratch AS root

COPY --from=bootstrap /rootfs /
COPY etc /etc

LABEL org.opencontainers.image.title="Arch Linux RISC-V"
LABEL org.opencontainers.image.description="This is an Arch Linux port to the RISC-V architecture."

ENV LANG=en_US.UTF-8
RUN ldconfig && locale-gen
RUN pacman-key --init && \
    pacman-key --populate && \
    bash -c "rm -rf etc/pacman.d/gnupg/{openpgp-revocs.d/,private-keys-v1.d/,pubring.gpg~,gnupg.S.}*"

CMD ["/usr/bin/bash"]
```

虽然这个镜像是一个自举的镜像，给出这个构建文件似乎没有什么用处（笑）。再在这个镜像的基础上新建一层镜像安装各种.NET的依赖项。

```dockerfile
FROM archriscv

RUN pacman -Syyu --noconfirm bash clang cmake git icu inetutils \
    krb5 libgit2 libunwind libxml2 lldb llvm lttng-ust2.12 \
    openssl systemd zlib
```

构建这个镜像，再将这个镜像根目录下的所有文件拷贝出来。

```bash
docker build . --platform linux/riscv64 -t archriscv:base-devel
mkdir rootfs
cid=$(docker run -d --platform linux/riscv64 archriscv:base-devel)
sudo docker cp 	$cid:/ rootfs
sudo chown $USER:$USER -R rootfs
```

新建一个`runtime-build`文件夹，使用下面的指令在`rootfs`文件系统中构建`libcxx`和`compiler-rt`。

> `libcxx`和`compiler-rt`不是常规交叉编译需要的，而是编译.NET所需要的。

```bash
export TARGET_TRIPLE="riscv64-linux-gnu"
export CLANG_MAJOR_VERSION=18
export ROOTFS_DIR=<ROOTFS>
cmake -S ../llvm-project.src/runtimes \
        -DCMAKE_BUILD_TYPE=Release \
        -DCMAKE_ASM_COMPILER=clang \
        -DCMAKE_C_COMPILER=clang \
        -DCMAKE_CXX_COMPILER=clang++ \
        -DCMAKE_ASM_COMPILER_TARGET="$TARGET_TRIPLE" \
        -DCMAKE_C_COMPILER_TARGET="$TARGET_TRIPLE" \
        -DCMAKE_CXX_COMPILER_TARGET="$TARGET_TRIPLE" \
        -DCMAKE_POSITION_INDEPENDENT_CODE=ON \
        -DCMAKE_SYSROOT="$ROOTFS_DIR" \
        -DCMAKE_EXE_LINKER_FLAGS="-fuse-ld=lld" \
        -DCMAKE_FIND_ROOT_PATH_MODE_PROGRAM="NEVER" \
        -DLLVM_USE_LINKER=lld \
        -DLLVM_ENABLE_RUNTIMES="libcxx;compiler-rt" \
        -DLIBCXX_ENABLE_SHARED=OFF \
        -DLIBCXX_CXX_ABI=libstdc++ \
        -DLIBCXX_CXX_ABI_INCLUDE_PATHS="$ROOTFS_DIR/usr/include/c++/14.2.1/;$ROOTFS_DIR/usr/include/c++/14.2.1/riscv64-unknown-linux-gnu/" \
        -DCOMPILER_RT_CXX_LIBRARY="libcxx" \
        -DCOMPILER_RT_STATIC_CXX_LIBRARY=ON \
        -DCOMPILER_RT_BUILD_SANITIZERS=OFF \
        -DCOMPILER_RT_BUILD_MEMPROF=OFF \
        -DCOMPILER_RT_BUILD_LIBFUZZER=OFF \
        -DCOMPILER_RT_DEFAULT_TARGET_ONLY=ON \
        -DCOMPILER_RT_INSTALL_PATH="/usr/local/lib/clang/$CLANG_MAJOR_VERSION"
make -j20
sudo cmake --install . --prefix "$ROOTFS_DIR/usr"
```

在构建指令中需要根据安装的`gcc`版本调整`_DLIBCXX_CXX_ABI_INCLUDE_PATHS`的路径。

完成所有上述的工作之后，回到我们最开始的你好世界样例，使用下面这行神秘的代码进行编译：

```bash
clang++ --target=riscv64-linux-gnu --sysroot=$ROOTFS_DIR -fuse-ld=lld hello.cpp -o hello
```

这次编译不会出现问题，上面指定的三个参数依次为指定目标三元组、指定根文件系统的位置和指定使用`lld`作为链接器。使用Docker镜像进行测试确认编译之后的二进制文件可以正常运行。

### 复盘

在正式开始下一步之前，我们先复盘一下在搭建交叉编译环境时我们都做了什么：

- 使用`LLVM_TARGETS_TO_BUILD`编译了一套新的LLVM，
  - 将安装了基础依赖包的`archriscv`导出作为根文件系统，
- 使用该根文件系统在该根文件系统中编译了`libcxx`和`compiler-rt`两个库。

这三步也带来了三个问题：

1. Arch Linux自带的LLVM工具链难道不能交叉编译吗？
2. Arch Linux 官方提供的`riscv64-linux-gnu-gcc`包能够作为根文件系统吗？
3. 能够在上述的根文件系统中安装我们需要的`libcxx`和`compiler-rt`两个库吗？

第一个问题的回答是Arch Linux安装的LLVM工具是可以交叉编译的。虽然在Arch Linux官方构建LLVM工具链的[构建脚本](https://gitlab.archlinux.org/archlinux/packaging/packages/clang/-/blob/main/PKGBUILD?ref_type=heads)中没有使用`LLVM_TARGETS_TO_BUILD`参数，但是这个参数的默认值是`all`。这一点我们也可以通过实验来验证。

![image-20240824153514149](./build-dotnet-from-source/image-20240824153514149.webp)于是回到编译`llvm`的目录下执行`cat install_manifest.txt | sudo xargs rm`。

第二个问题的回答可以使用实验来验证，首先安装`riscv64-linux-gnu-gcc`，然后将根文件系统的位置设置为`/usr/riscv64-linux-gnu`，重新编译上面的你好世界样例。编译之后可以正常执行。

第三个问题的回答是还是新建一个根文件系统罢，随便往系统目录里面写东西感觉是一个不太好的习惯。

## 正式编译

首先进入克隆代码的目录，运行初始化脚本。

```bash
cd dotnet
./prep-source-build.sh
```

设置根文件系统的目录，这里仍然使用从安装了`base-devel`的Docker容器中导出并自行编译了`compiler-rt`和`libcxx`的根文件系统。

```bash
export ROOTFS_DIR=<rootfs>
```

然后使用下面这条神秘的命令开始交叉编译：

```bash
./build.sh -sb --clean-while-building /p:TargetOS=linux /p:TargetArchitecture=riscv64 /p:Crossbuild=true /p:BuildArgs="/p:BundleNativeAotCompiler=false"
```

上面的第一个参数是指定了`source-build`选项，第二个参数指定了在编译的过程中清理不需要的文件以节省硬盘空间，后面的几个MSBUILD参数则是指定为RISC-V架构上的Linux系统构建，并且不构建AOT编译器。

但是现在的.NET在RISC-V平台上还是废物一个，甚至连`dotnet new`都跑不过，下一步看看能不能运行一下运行时的测试集看看。

![image-20240824214145759](./build-dotnet-from-source/image-20240824214145759.webp)
