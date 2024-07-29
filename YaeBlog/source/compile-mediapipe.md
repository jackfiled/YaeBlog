---
title: 编译MediaPipe框架
tags:
  - C/C++
  - 动作捕捉
date: 2022-11-11 22:20:25
---

编译MediaPipe框架。
<!--more-->

最近开始研究自己的大创项目，一个关于动作捕捉的小玩意儿，第一步就是~~抄袭开源代码~~借鉴他人优秀成果。在众多的项目中，我看上了这个Google开源的优秀框架，先把这个项目在本地上跑起来再说。这篇文章就记录了我编译这个框架的过程。

> 在我写完这篇文章之后，我就从`WSL`润到了`Arch Linux`，然而我还是有编译`MediaPipe`的需求，所以这篇文章就增加了`Arch Linux`下编译`MediaPipe`的过程。

首先是在`Arch Linux`下编译的过程。

## 编译环境概述

使用`Arch Linux`，需要注意的是`Arch Linux`采用滚动更新，目前的安装方法可能在不久的将来就不适用了。

## 编译

### 环境准备

首先安装`bazelisk`，由于我安装了`pnpm`，直接使用`pnpm install -g @bazel/bazelisk`安装。

然后使用`pacman`安装：

```shell
sudo pacman -S opencv ffmpeg jdk-openjdk git
```

还有编译的过程中可能会涉及到`python`中的`numpy`软件包，我由于已经安装了`conda`来管理`python`环境，于是就采用`conda install numpy`在`base`环境中安装。

克隆`MediaPipe`仓库：

```shell
git clone https://github.com/google/mediapipe.git
```

由于`pacman`仓库中安装的`opencv`版本是最新的`opencv4`，我们需要修改`MediaPipe`中的配置文件来适配，修改`third_party/opencv_linux.BUILD`：

```python
# Description:
#   OpenCV libraries for video/image processing on Linux

licenses(["notice"])  # BSD license

exports_files(["LICENSE"])

# The following build rule assumes that OpenCV is installed by
# 'apt-get install libopencv-core-dev libopencv-highgui-dev \'
# '                libopencv-calib3d-dev libopencv-features2d-dev \'
# '                libopencv-imgproc-dev libopencv-video-dev'
# on Debian Buster/Ubuntu 18.04.
# If you install OpenCV separately, please modify the build rule accordingly.
cc_library(
    name = "opencv",
    hdrs = glob([
        # For OpenCV 4.x
        #"include/aarch64-linux-gnu/opencv4/opencv2/cvconfig.h",
        #"include/arm-linux-gnueabihf/opencv4/opencv2/cvconfig.h",
        #"include/x86_64-linux-gnu/opencv4/opencv2/cvconfig.h",
        # 将下面这行取消注释
        "include/opencv4/opencv2/**/*.h*",
    ]),
    includes = [
        # For OpenCV 4.x
        #"include/aarch64-linux-gnu/opencv4/",
        #"include/arm-linux-gnueabihf/opencv4/",
        #"include/x86_64-linux-gnu/opencv4/",
        # 将下面这行取消注释
        "include/opencv4/",
    ],
    linkopts = [
        "-l:libopencv_core.so",
        "-l:libopencv_calib3d.so",
        "-l:libopencv_features2d.so",
        "-l:libopencv_highgui.so",
        "-l:libopencv_imgcodecs.so",
        "-l:libopencv_imgproc.so",
        "-l:libopencv_video.so",
        "-l:libopencv_videoio.so",
    ],
    visibility = ["//visibility:public"],
)
```

### 编译

跑一下实例中的`Hello, World`。

首先设置一个环境变量：

```shell
$ export GLOG_logtostderr=1
```

然后一把梭哈：

```shell
bazel run --define MEDIAPIPE_DISABLE_GPU=1 \
    mediapipe/examples/desktop/hello_world:hello_world
```

在第一次编译的时候会下载大量的依赖文件，如果遇到网络错误可以多试几次，~~我试了三次才完成~~。

```shell
Starting local Bazel server and connecting to it...
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'com_google_absl' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'com_google_benchmark' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'flatbuffers' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'pybind11_bazel' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'com_googlesource_code_re2' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'com_google_protobuf' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'com_google_googletest' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'com_github_gflags_gflags' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'zlib' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'rules_python' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'build_bazel_rules_apple' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'build_bazel_rules_swift' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'build_bazel_apple_support' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'xctestrunner' because it already exists.
DEBUG: /home/ricardo/.cache/bazel/_bazel_ricardo/9d4c3ea39592a9aa5075148d2a9caf3e/external/org_tensorflow/third_party/repo.bzl:132:14: 
Warning: skipping import of repository 'pybind11' because it already exists.
WARNING: /home/ricardo/Documents/code/cpp/mediapipe/mediapipe/framework/tool/BUILD:184:24: in cc_library rule //mediapipe/framework/tool:field_data_cc_proto: target '//mediapipe/framework/tool:field_data_cc_proto' depends on deprecated target '@com_google_protobuf//:cc_wkt_protos': Only for backward compatibility. Do not use.
WARNING: /home/ricardo/Documents/code/cpp/mediapipe/mediapipe/framework/BUILD:54:24: in cc_library rule //mediapipe/framework:calculator_cc_proto: target '//mediapipe/framework:calculator_cc_proto' depends on deprecated target '@com_google_protobuf//:cc_wkt_protos': Only for backward compatibility. Do not use.
INFO: Analyzed target //mediapipe/examples/desktop/hello_world:hello_world (84 packages loaded, 1747 targets configured).
INFO: Found 1 target...
Target //mediapipe/examples/desktop/hello_world:hello_world up-to-date:
  bazel-bin/mediapipe/examples/desktop/hello_world/hello_world
INFO: Elapsed time: 3.962s, Critical Path: 0.31s
INFO: 1 process: 1 internal.
INFO: Build completed successfully, 1 total action
INFO: Build completed successfully, 1 total action
I20230115 20:26:29.880736 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.880805 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.880817 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.880888 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.880957 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.881028 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.881096 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.881157 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.881198 47247 hello_world.cc:57] Hello World!
I20230115 20:26:29.881268 47247 hello_world.cc:57] Hello World!
```

### 编译Pose解决方案的Android Archieve

我的需求有一个是在安卓手机上使用`MediaPipe`的`Pose`解决方案，需要编译出一个`AAR`安卓打包文件。

首先安装需要的依赖，`Android SDK`和`Android NDK`。这里使用`Android Studio`安装。

然后创建一个`bazel`编译的目标文件：在目录`mediapipe/examples/android/src/java/com/google/mediapipe/apps/`下创建一个文件夹`pose_tracking_aar`，在其中创建`BUILD`文件，写入：

```python
load("//mediapipe/java/com/google/mediapipe:mediapipe_aar.bzl", "mediapipe_aar")

mediapipe_aar(
    name = "mediapipe_pose_tracking",
    calculators = ["//mediapipe/graphs/pose_tracking:pose_tracking_gpu_deps"],
)
```

使用命令编译：

```shell
bazel build -c opt --strip=ALWAYS \
    --host_crosstool_top=@bazel_tools//tools/cpp:toolchain \
    --fat_apk_cpu=arm64-v8a,armeabi-v7a \
    --legacy_whole_archive=0 \
    --features=-legacy_whole_archive \
    --copt=-fvisibility=hidden \
    --copt=-ffunction-sections \
    --copt=-fdata-sections \
    --copt=-fstack-protector \
    --copt=-Oz \
    --copt=-fomit-frame-pointer \
    --copt=-DABSL_MIN_LOG_LEVEL=2 \
    --linkopt=-Wl,--gc-sections,--strip-all \
    //mediapipe/examples/android/src/java/com/google/mediapipe/apps/pose_tracking_aar:mediapipe_pose_tracking.aar
```

如果在编译的过程中提示缺失`dx.jar`这个文件而且你用的SDK版本还是高于31的，那可能是SDK中缺失了这个文件，可以将SDk降级到30就含有这个文件了。我使用的解决办法比较离奇，我是将30版本的SDK文件中的这个文件软链接过来，解决了这个问题。

![](compile-mediapipe/2023-01-15-22-05-41-Screenshot_20230115_220521.png)

编译消耗的时间可能比较的长，耐心等待即可。

为了在手机上使用，我们还需要编译出`binarypb`文件，从Google的服务器上下载`tflite`文件。

编译`binarypb`的过程比较的简单，编译目标在`mediapipe/graphs/pose_tracking`中，名称是`pose_tracking_gpu_binary_graph`，使用下列指令编译：

```shell
bazel build -c opt //mediapipe/graphs/pose_tracking:pose_tracking_gpu_binary_graph
```

> 在这里，Google默认添加了一个`input side packet`打开人体遮罩，如果不需要这个效果，需要删除`mediapipe/graphs/pose_tracking/pose_tracking_gpu.pbtxt`文件中的以下内容：
> 
> ```
> # Generates side packet to enable segmentation.
> node {
>   calculator: "ConstantSidePacketCalculator"
>   output_side_packet: "PACKET:enable_segmentation"
>   node_options: {
>     [type.googleapis.com/mediapipe.ConstantSidePacketCalculatorOptions]: {
>       packet { bool_value: true }
>     }
>   }
> }
> ```

然后还需要从服务器上下载`tflite`文件，`Pose Tracking`这个解决方案需要两个`tflite`文件，第一个是[pose_detection.tflite](https://storage.googleapis.com/mediapipe-assets/pose_detection.tflite)，第二个文件则有三个不同的选择，分别对于解决方案中提供的三个质量版本：

![](compile-mediapipe/2023-01-19-20-20-40-Screenshot_20230119_202008.png)

下载地址是[pose_landmark_full.tflite](https://storage.googleapis.com/mediapipe-assets/pose_landmark_full.tflite)，[pose_landmark_heavy.tflite](https://storage.googleapis.com/mediapipe-assets/pose_landmark_heavy.tflite)和[pose_landmark_lite.tflite](https://storage.googleapis.com/mediapipe-assets/pose_landmark_lite.tflite)。

> 下面是原来使用`WSL`编译的过程。

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
