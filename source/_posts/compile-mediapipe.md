---
title: 编译MediaPipe框架
tags:
  - C/C++
  - 动作捕捉
date: 2022-11-11 22:20:25
---


# 编译MediaPipe框架

最近开始研究自己的大创项目，一个关于动作捕捉的小玩意儿，第一步就是~~抄袭开源代码~~借鉴他人优秀成果。在众多的项目中，我看上了这个Google开源的优秀框架，先把这个项目在本地上跑起来再说。这篇文章就记录了我编译这个框架的过程。

<!--more-->

## 编译环境概述

我使用的基于WSL2的Ubuntu 22.04编译。主要是参考官方的[安装文档](https://google.github.io/mediapipe/getting_started/install.html#installing-on-debian-and-ubuntu)。

## 编译

### 环境准备

首先安装两个基础性的包：

```bash
sudo apt install build-essential git
```

然后安装MediaPipe的编译管理工具`bazel`，这里我是通过`npm`安装：

```bash
pnpm add -g @bazel/bazelisk
```

通过运行`bazel version`验证安装成功：

```bash
$ bazel version
Bazelisk version: v1.13.2
WARNING: Invoking Bazel in batch mode since it is not invoked from within a workspace (below a directory having a WORKSPACE file).
Extracting Bazel installation...
Build label: 5.3.2
Build target: bazel-out/k8-opt/bin/src/main/java/com/google/devtools/build/lib/bazel/BazelServer_deploy.jar
Build time: Wed Oct 19 18:22:12 2022 (1666203732)
Build timestamp: 1666203732
Build timestamp as int: 1666203732
```

安装`Miniconda`，再在环境中安装`numpy`。编译中依赖于`Python`和`numpy`，这里网上的资料汗牛充栋，我就不过多赘述。

在准备玩上面的环境之后，我们就可以用`git`将MediaPipe的源代码克隆下来了。

```bash
git clone https://github.com/google/mediapipe.git
```

### 安装Opencv和FFmpeg

我这里选择的是手动编译安装opencv，安装的步骤参考官方的安装脚本，但是脚本中的不少内容已经过时。

首先安装必要的依赖库和编译管理工具：

```bash
sudo apt install cmake ffmpeg libavformat-dev libdc1394-dev libgtk2.0-dev \
                       libjpeg-dev libpng-dev libswscale-dev libtbb2 libtbb-dev \
                       libtiff-dev
```

> 注意：官方脚本中要求安装`libdc1394-22-dev`这个包，但是按照这篇[回答](https://askubuntu.com/questions/1407580/unable-to-locate-package-libdc1394-22-dev)，这个包已经被`libdc1392-dev`取代了。

在临时文件夹中创建一个文件专门用来编译：

```bash
cd /tmp
mkdir opencv
cd opencv/
```

使用`git`下载Opencv的源代码：

```bash
git clone https://github.com/opencv/opencv_contrib.git
git clone https://github.com/opencv/opencv.git
```

在仓库中签出到指定的版本分支：

```bash
cd opencv
git checkout 3.4
cd ../opencv_contrib
git checkout 3.4
```

创建编译文件，使用指定的`cmake`参数生成编译文件：

```bash
cd ../opencv
mkdir release
cd release
cmake .. -DCMAKE_BUILD_TYPE=RELEASE -DCMAKE_INSTALL_PREFIX=/usr/local \
          -DBUILD_TESTS=OFF -DBUILD_PERF_TESTS=OFF -DBUILD_opencv_ts=OFF \
          -DOPENCV_EXTRA_MODULES_PATH=/tmp/opencv/opencv_contrib/modules \
          -DBUILD_opencv_aruco=OFF -DBUILD_opencv_bgsegm=OFF -DBUILD_opencv_bioinspired=OFF \
          -DBUILD_opencv_ccalib=OFF -DBUILD_opencv_datasets=OFF -DBUILD_opencv_dnn=OFF \
          -DBUILD_opencv_dnn_objdetect=OFF -DBUILD_opencv_dpm=OFF -DBUILD_opencv_face=OFF \
          -DBUILD_opencv_fuzzy=OFF -DBUILD_opencv_hfs=OFF -DBUILD_opencv_img_hash=OFF \
          -DBUILD_opencv_js=OFF -DBUILD_opencv_line_descriptor=OFF -DBUILD_opencv_phase_unwrapping=OFF \
          -DBUILD_opencv_plot=OFF -DBUILD_opencv_quality=OFF -DBUILD_opencv_reg=OFF \
          -DBUILD_opencv_rgbd=OFF -DBUILD_opencv_saliency=OFF -DBUILD_opencv_shape=OFF \
          -DBUILD_opencv_structured_light=OFF -DBUILD_opencv_surface_matching=OFF \
          -DBUILD_opencv_world=OFF -DBUILD_opencv_xobjdetect=OFF -DBUILD_opencv_xphoto=OFF \
          -DCV_ENABLE_INTRINSICS=ON -DWITH_EIGEN=ON -DWITH_PTHREADS=ON -DWITH_PTHREADS_PF=ON \
          -DWITH_JPEG=ON -DWITH_PNG=ON -DWITH_TIFF=ON
```

> 注意安装自己下载源代码的地址修改`-DOPENCV_EXTRA_MUDULES_PATH`的值

> 安装过程中还会下载一系列的依赖包，请注意自己的网络环境

使用`make`指令进行编译和安装

```bash
make -j 16
sudo make install
```

编辑链接器的配置：

```bash
sudo touch /etc/ld.so.conf.d/mp_opencv.conf
sudo bash -c  "echo /usr/local/lib >> /etc/ld.so.conf.d/mp_opencv.conf"
sudo ldconfig -v
```

然后进行MediaPipe的目录，用脚本进行配置文件的修改：

```bash
./setup_opencv.sh config_only
```

## 运行首个例子：

```bash
export GLOG_logtostderr=1
bazel run --define MEDIAPIPE_DISABLE_GPU=1 mediapipe/examples/desktop/hello_world:hello_world
```

在等待一段时间的下载依赖和编译之后，我们可以看见：

```bash
I20221110 22:00:50.899885 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.899948 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.899955 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.899960 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.899962 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.899982 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.900000 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.900025 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.900030 14357 hello_world.cc:57] Hello World!
I20221110 22:00:50.900193 14357 hello_world.cc:57] Hello World!
```

如果出现了各种奇怪的报错，那可以执行这条命令重新安装依赖再编译试试：

```bash
bazel clean --expunge
```





