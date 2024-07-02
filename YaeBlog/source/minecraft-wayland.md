---
title: 让Minecraft运行在Wayland下
tags:
  - Linux
  - 生活小妙招
date: 2024-1-12 20:10:06
---

<!--more-->

让Minecraft游戏使用`Wayland`显示协议。

## Update At 2024-2-24

在两天前，2024年的2月22日，`glfw`释出了一个新版本`3.4`。在新版本中，`glfw`大幅强化了对于`wayland`显示协议的支持，在默认情况下就会直接使用`wayland`显示协议。但是为了能够正常的运行`minecraft`，还需要对源代码进行修改：

```diff
diff --git a/src/wl_window.c b/src/wl_window.c
index 5b491ffb..e96a21e4 100644
--- a/src/wl_window.c
+++ b/src/wl_window.c
@@ -2227,8 +2227,9 @@ void _glfwSetWindowTitleWayland(_GLFWwindow* window, const char* title)
 void _glfwSetWindowIconWayland(_GLFWwindow* window,
                                int count, const GLFWimage* images)
 {
-    _glfwInputError(GLFW_FEATURE_UNAVAILABLE,
-                    "Wayland: The platform does not support setting the window icon");
+    // _glfwInputError(GLFW_FEATURE_UNAVAILABLE,
+    //                 "Wayland: The platform does not support setting the window icon");
+    fprintf(stderr, "!!! Ignoring Error: Wayland: The platform does not support setting the window icon\n");
 }
 
 void _glfwGetWindowPosWayland(_GLFWwindow* window, int* xpos, int* ypos)
```

进行编译的命令也需要修改为：

```shell
cmake -S . -B build -D GLFW_BUILD_WAYLAND=1 -D BUILD_SHARED_LIBS=ON -D GLFW_BUILD_EXAMPLES=no -D GLFW_BUILD_TESTS=no -D GLFW_BUILD_DOCS=no
```

在使用`NVIDIA`显卡仍然需要设置环境变量：

```shell
export __GL_THREADED_OPTIMIZATIONS=0
```

虽然在新的版本中`glfw`对于`wayland`的支持有了长足的进展，但是完全支持还需要一定的时间。

## TL;DR

需要手动编译一个依赖库`glfw`，并在编译之前修改源代码：

```diff
diff --git a/src/platform.c b/src/platform.c
index c5966ae7..3e7442f9 100644
--- a/src/platform.c
+++ b/src/platform.c
@@ -49,12 +49,12 @@ static const struct
 #if defined(_GLFW_COCOA)
     { GLFW_PLATFORM_COCOA, _glfwConnectCocoa },
 #endif
-#if defined(_GLFW_X11)
-    { GLFW_PLATFORM_X11, _glfwConnectX11 },
-#endif
 #if defined(_GLFW_WAYLAND)
     { GLFW_PLATFORM_WAYLAND, _glfwConnectWayland },
 #endif
+#if defined(_GLFW_X11)
+    { GLFW_PLATFORM_X11, _glfwConnectX11 },
+#endif
 };
 
 GLFWbool _glfwSelectPlatform(int desiredID, _GLFWplatform* platform)
diff --git a/src/wl_window.c b/src/wl_window.c
index 7b9e3d0d..dd1c89ed 100644
--- a/src/wl_window.c
+++ b/src/wl_window.c
@@ -2109,8 +2109,7 @@ void _glfwSetWindowTitleWayland(_GLFWwindow* window, const char* title)
 void _glfwSetWindowIconWayland(_GLFWwindow* window,
                                int count, const GLFWimage* images)
 {
-    _glfwInputError(GLFW_FEATURE_UNAVAILABLE,
-                    "Wayland: The platform does not support setting the window icon");
+    fprintf(stderr, "!!! Ignoring Error: Wayland: The platform does not support setting the window icon\n");
 }
 
 void _glfwGetWindowPosWayland(_GLFWwindow* window, int* xpos, int* ypos)
@@ -2353,8 +2352,7 @@ void _glfwRequestWindowAttentionWayland(_GLFWwindow* window)
 
 void _glfwFocusWindowWayland(_GLFWwindow* window)
 {
-    _glfwInputError(GLFW_FEATURE_UNAVAILABLE,
-                    "Wayland: The platform does not support setting the input focus");
+    fprintf(stderr, "!!! Ignoring Error: Wayland: The platform does not support setting the input focus\n");
 }
 
 void _glfwSetWindowMonitorWayland(_GLFWwindow* window,
```

首先克隆[glfw](https://github.com/glfw/glfw)源代码，并将上述代码保存为`glfw.patch`文件。使用下列指令应用变更并编译`glfw`代码：

```shell
git apply glfw.patch
cmake -S . -B build -D GLFW_USE_WAYLAND=1 -D BUILD_SHARED_LIBS=ON -D GLFW_BUILD_EXAMPLES=no -D GLFW_BUILD_TESTS=no -D GLFW_BUILD_DOCS=no
cd build
make
```

使用`make install`将编译好的库文件安装到`/usr/local/lib`中。

在启动游戏时添加`java`启动参数`-Dorg.lwjgl.glfw.libname=/usr/local/lib/libglfw.so`以使用自行编译的`glfw`库。

尝试运行游戏。如果游戏仍然运行失败并报错为`An EGLDisplay argument does not name a valid EGL display connection`，尝试在运行游戏前设置如下的环境变量：

```shell
export __GL_THREADED_OPTIMIZATIONS=0
```

## 细说

最近将自己手上的`Linux`设备逐渐迁移到`Wayland`显示协议，在使用`AMD`显卡的机器上基本没遇到什么严重的显示问题，在`NVIDIA`却处处都是问题，看来老黄确实罪大恶极。本文便是解决Minecraft在`XWayland`上显示时疯狂闪烁的`bug`，通过将Minecraft设置为使用`Wayland`显示协议。

可能有人要问的是，`java`上对于`Wayland`的支持不是还没有正式合并到主线吗，`Minecraft`作为一个使用`java`实现的游戏为什么能够使用`Wayland`显示协议呢？这是因为传统意义上`java`对于`Wayland`的支持是指`java`上的图形库对于`Wayland`的支持还没有就绪，但是Minecraft作为一个游戏并没有使用这些图形库而是直接和底层的渲染API进行交互的，因此可以直接修改底层API让Minecraft使用`Wayland`显示协议。

具体来说，Minecraft使用的是一个称作`glfw`跨平台图形库，因此我们只需要修改该图形库让其使用`Wayland`进行显示就可以了。需要做的便是给源代码打上上面给出的补丁，重新编译安装即可。

然后就是设置Minecraft使用我们自行编译的图形库，需要设置`java`启动参数，我使用的[hmcl](https://github.com/huanghongxun/HMCL)这款启动器，可以在设置里面很方便的设置启动参数：

![image-20240105212744116](./minecraft-wayland/image-20240105212744116.png)

但是，如果你使用的是`nvidia`显卡，这里还会遇到一个问题：

![image-20240105213439528](./minecraft-wayland/image-20240105213439528.png)

这个问题从一些资料显示仍然是老黄整的好活：

![image-20240105213942445](./minecraft-wayland/image-20240105213942445.png)

设置环境变量解决，再次强烈推荐[hmcl](https://github.com/huanghongxun/HMCL)启动器，可以方便的设置环境变量。

```
export __GL_THREADED_OPTIMIZATIONS=0
```

## 参考资料

- https://github.com/Admicos/minecraft-wayland/issues/54
- https://github.com/Admicos/minecraft-wayland/issues/55
- https://www.mcmod.cn/class/2785.html
