---
title: 构建运行基于MLIR的独立项目
date: 2025-03-19T20:57:31.1928528+08:00
tags:
- 技术笔记
- LLVM
---

MLIR是多层次中间表示形式（Multi-Level Intermediate Representation），是LLVM项目中提供的一项新编译器开发基础设施，使得编译器开发者能够在源代码和可执行代码之间定义多层IR来保留程序信息指导编译优化。本博客指导如何创建一个独立（out-of-tree）的MLIR项目。

<!--more-->

## 编译LLVM和MLIR

考虑到大多数的Linux发行版在打包LLVM时不会编译MLIR，因此自行编译安装包括MLIR项目的LLVM就成为开发独立MLIR项目的前置条件。

首先在GitHub上下载LLVM的源代码包，我这里选择最新的稳定版本`20.1.0`。

```shell
wget https://github.com/llvm/llvm-project/releases/download/llvmorg-20.1.0/llvm-project-20.1.0.src.tar.xz
```

下载之后解压进入，准备进行构建。

```shell
tar xvf llvm-project-20.1.0.src.tar.xz
cd llvm-project-20.1.0.src
```

创建`build`文件夹，使用下面的命令进行生成构建文件。在这里选择使用`Release`构建类型，安装的位置是`~/.local/share/llvm`文件夹，构建的项目包括`llvm`、`clang`和`mlir`三个项目，并指定使用系统上的`clang`和`clang++`编译器作为编译过程中使用的编译器。

```shell
mkdir build
cd build
cmake -G Ninja -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX="/home/ricardo/.local/share/llvm-20.1.0" -DLLVM_ENABLE_PROJECTS="llvm;clang;mlir" -DCMAKE_C_COMPILER=clang -DCMAKE_CXX_COMPILER=clang++ -DLLVM_INSTALL_UTILS=true ../llvm
```

![image-20250319192618697](./mlir-standalone/image-20250319192618697.png)

生成构建文件之后使用`ninja`进行构建。

```shell
ninja
```

![image-20250319194742171](./mlir-standalone/image-20250319194742171.png)

构建在我的i5-13600K上大约需要20分钟。

构建完成之后进行安装。

```shell
ninja install
```

## 编译MLIR官方的独立项目

MLIR的官方提供了一个独立项目，项目文件夹在`mlir/examples/standalone`中，将这个文件夹中的内容复制到我们需要的地方，尝试使用上面构建的`mlir`进行构建。

```shell
cp -r ~/Downloads/llvm-project-20.1.0.src/mlir/examples/standalone mlir-standalone 
cd mlir-standalone
```

### 不启用测试

编译过程中可能遇到的最大问题是`llvm-lit`，这个使用`python`编写的LLVM集成测试工具，在`standalone`的`README.md`中要求编译过程中使用`LLVM_EXTERNAL_LIT`变量指定到LLVM编译过程中生成的`llvm-lit`可执行文件。

> 也许就是因为`llvm-lit`是用Python撰写的，所以`llvm-lit`不会安装到`PREFIX`指定的位置。

不过我们可以禁用测试（笑）。在`CMakeLists.txt`文件中注释对于测试文件夹的添加：

```cmake
add_subdirectory(include)
add_subdirectory(lib)
if(MLIR_ENABLE_BINDINGS_PYTHON)
  message(STATUS "Enabling Python API")
  add_subdirectory(python)
endif()
#add_subdirectory(test)
add_subdirectory(standalone-opt)
add_subdirectory(standalone-plugin)
add_subdirectory(standalone-translate)

```

回到构建文件夹，使用如下的`cmake`指令生成构建文件。

```shell
export LLVM_DIR=/home/ricardo/.local/share/llvm-20.1.0
cmake -G Ninja -DMLIR_DIR=$LLVM_DIR/lib/cmake/mlir  ..
```

可以顺利通过编译。

![image-20250319202218503](./mlir-standalone/image-20250319202218503.png)

### 启用测试

但是测试还是非常重要的。我们尝试启动测试看看，取消对于测试文件夹的注释：

```shell
rm -rf build && mkdir build && cd build
cmake -G Ninja -DMLIR_DIR=$LLVM_DIR/lib/cmake/mlir  ..
```

很好顺利报错，报错的提示是缺失`FileCheck`、`count`和`not`。

![image-20250319202553644](./mlir-standalone/image-20250319202553644.png)

那么按照`README.md`中的提示添加上来自构建目录的`llvm-lit`会怎么样呢？

```shell
rm -rf build && mkdir build && cd build
export LLVM_BUILD_DIR=/home/ricardo/Downloads/llvm-project-20.1.0.src/build
cmake -G Ninja -DMLIR_DIR=$LLVM_DIR/lib/cmake/mlir -DLLVM_EXTERNAL_LIT=$LLVM_BUILD_DIR/bin/llvm-lit ..
```

同样的报错，看来问题不是出在这里。

经过对于LLVM文档的仔细研究，发现原来是没有启动这个变量：

![image-20250319204057832](./mlir-standalone/image-20250319204057832.png)

遂修改最初的LLVM编译指令。

重新运行来自`README.md`的构建文件生成指令之后，测试也完美运行通过：

```shell
ninja test-standalone
```

![image-20250319204522857](./mlir-standalone/image-20250319204522857.png)

不过这个还是有一点令我不是特别满意，这依赖了一个来自于构建目录的工具`llvm-lit`，如果我编译安装的时候眼疾手快的删除了编译目录不就完蛋了。而且我都**standalone**了还依赖似乎有点说不过去？

于是发现了一篇从`pip`上下载使用`llvm-lit`的[博客](https://medium.com/@mshockwave/using-llvm-lit-out-of-tree-5cddada85a78)和一个LLVM Discourse上面的[帖子](https://discourse.llvm.org/t/running-llvm-lit-on-external-project-test-file-derived-from-standalone-fails/67787)，遂进行尝试。

首先在当前目录下创建一个虚拟环境，并下载安装`llvm-lit`。

```shell
python -m venv .llvm-lit
source .llvm-lit/bin/activate
pip install lit
```

不过这个库似乎没有提供运行入口点，需要我们手动创建一个可执行的`python`文件：

```python
#!/usr/bin/env python
from lit.main import main
if __name__ == '__main__':
    main()
```

然后尝试在`cmake`指令中修改为`lit`为这个可执行文件：

```shell
cmake -G Ninja -DMLIR_DIR=$LLVM_DIR/lib/cmake/mlir -DLLVM_EXTERNAL_LIT=$(pwd)/../llvm-lit ..
ninja test-standalone
```

![image-20250319205520649](./mlir-standalone/image-20250319205520649.png)
