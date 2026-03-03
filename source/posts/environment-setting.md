---
title: 环境配置备忘录
date: 2022-01-15T20:19:39.0000000
tags:
- 技术笔记
---


电脑上的环境三天两头出问题，写下一个备忘录记录一下电脑上环境的配置过程。

<!--more-->

> Update1: 2022年9月4日
>
> 重新配置了一遍电脑中的环境，删除了许多不切实际的地方。

## 操作系统
版本：Windows10专业版

版本号：21H2  

操作系统内部版本：19044.1949

体验：Windows Feature Experience Pack 120.2212.4180.0 

> 虽然不知道上面的有什么用，但是还是写一下，没准什么时候就有用了。  

## Windows Subsystem for Linux(WSL)
安装WSL的官方文档[链接](https://docs.microsoft.com/zh-cn/windows/wsl/install) 。
输入`wsl -l -v`输出

```
  NAME      STATE           VERSION
* Ubuntu    Stopped         2
```
即：使用的Linux分发版是Ubuntu，使用的WSL版本是WSL-2。

## 语言

### Python

前往下载安装Python。

使用下列命令将pip下载换源为TUNA源：

```bash
pip config set global.index-url https://pypi.tuna.tsinghua.edu.cn/simple
```

### Java

在[这里](https://www.oracle.com/java/technologies/downloads/)下载JDK ，然后配置相关的环境变量 ，首先设置JAVA_HOME，指向安装JDK的根路径。
然后是CALSSPATH，内容是

```
.;%JAVA_HOME%\lib;%JAVA_HOME%\lib\tools.jar;
```
再设置PATH相关变量

```
%JAVA_HOME%\bin;
%JAVA_HOME%\jre\bin
```

在PowerShell中输入

```
java --version
```
返回
```
java 17.0.4.1 2022-08-18 LTS
Java(TM) SE Runtime Environment (build 17.0.4.1+1-LTS-2)
Java HotSpot(TM) 64-Bit Server VM (build 17.0.4.1+1-LTS-2, mixed mode, sharing)
```
输入
```
javac --version
```
返回
```
javac 17.0.4.1
```
确认相关的设置完成 。

#### JAVA_TOOL_OPTIONS

设置一个环境变量`JAVA_TOOL_OPTIONS`为

```
-Dfile.encoding=UTF8 -Duser.language=en
```

从而规避中文乱码的问题。

> 其他一些官方文档较为全面，安装不需要复杂配置的编程语言就不再赘述了

## IDE

### VSCode

在[这里](https://code.visualstudio.com/)下载。

#### C/C++时的配置文件

```json
// launch.json
{
    "version": "0.2.0",
    "configurations": [
        {//这个大括号里是我们的‘调试(Debug)’配置
            "name": "Debug",//配置名称
            "type": "cppdbg",//配置类型，cppdbg对应cpptools提供的调试功能；可以认为此处只能是cppdbg
            "request": "launch",// 请求配置类型，可以为launch（启动）或attach（附加）
            "program": "${workspaceFolder}/bin/${fileBasenameNoExtension}.out",// 将要进行调试的程序的路径
            "args": [],// 程序调试时传递给程序的命令行参数，这里设为空即可
            "stopAtEntry": false,// 设为true时程序将暂停在程序入口处，相当于在main上打断点
            "cwd": "${workspaceFolder}/bin/",//程序的工作目录
            "environment": [],//环境变量，设置为空
            "externalConsole": false,// 为true时使用单独的cmd窗口，跳出小黑框；设为false则是用vscode的内置终端，建议用内置终端
            "internalConsoleOptions": "neverOpen",// 如果不设为neverOpen，调试时会跳到“调试控制台”选项卡，新手调试用不到
            "MIMode": "gdb",//指定特定的调试器
            "miDebuggerPath": "/usr/bin/gdb",//指定的调试器所在路径
            "preLaunchTask": "build"// 调试开始前执行的任务，我们在调试前要编译构建。与tasks.json的label相对应，名字要一样
        }
    ]
}
```

```json
// setting.json
{
    "files.associations": {
        "stdio.h": "c",
        "xutility": "c",
        "stdlib.h": "c",
        "math.h": "c",
        "cmath": "c"
    },
    "C_Cpp.errorSquiggles": "EnabledIfIncludesResolve"
}
```

```json
// tasks.json
{
    "version": "2.0.0",
    "tasks": [
        {//这个大括号里是构建的配置文件
            "label": "build",//任务的名称
            "type" : "shell",//任务类型，process是vsc把预定义变量和转义解析后直接全部传给command；shell相当于先打开shell再输入命令，所以args还会经过shell再解析一遍
            "command": "gcc",//在shell中执行的命令，若编译C++改为g++
            "args": [//一些传递给命令的参数
                "${file}",
                "-o",
               "${workspaceFolder}/bin/${fileBasenameNoExtension}.out",//这里是生成exe程序的位置，因为我自己设置了bin文件夹的位置，因此我直接使用绝对路径
                "-g",//生成和调试有关的信息
                "-Wall",//开启额外警告
                "-static-libgcc",//静态链接libgcc
                "-lm",//链接一个库文件
                "-std=c11", // 语言标准，可根据自己的需要进行修改，写c++要换成c++的语言标准，比如c++11
            ],
            "group": {
                "kind": "build",//表示这一组任务类型是构建
                "isDefault": true//表示这个任务是当前这组任务中的默认任务
            },
            "presentation": {
                "echo": true,//表示在执行任务时在终端要有输出
                "reveal": "always",//执行任务时是否跳转到终端面板，可以为always，silent，never
                "focus": false,//设为true后可以使执行task时焦点聚集在终端，但对编译来说，设为true没有意义，因为运行的时候才涉及到输入
                "panel": "new",//每次执行这个task时都新建一个终端面板，也可以设置为shared，共用一个面板，不过那样会出现‘任务将被终端重用’的提示，比较烦人
                "showReuseMessage": true,
                "clear": false
            },
            "problemMatcher":"$gcc",////捕捉编译时编译器在终端里显示的报错信息，将其显示在vscode的‘问题’面板里
        },
        {
            "label": "run",
            "type": "shell",
            "dependsOn":"build",
            "command":"${workspaceFolder}/bin/${fileBasenameNoExtension}.out",//这里是运行生成的程序的命令，同样使用绝对路径
            "group": {
                "kind": "test",
                "isDefault": true,
            },
            "presentation": {
                "echo": true,
                "reveal": "always",
                "focus": true,////这个就设置为true了，运行任务后将焦点聚集到终端，方便进行输入
                "panel": "new",
                "showReuseMessage": true,
                "clear": false
            }
        }
    ]
}

```

### Jetbrians

使用`Jetbrains Toolbox`来管理电脑上的所有IDE。

## 终端美化

首先在微软应用商店下载安装`Windows Terminal`。

### PowerShell美化

前往[oh-my-posh](https://github.com/jandedobbeleer/oh-my-posh)下载这个Powershell的美化工具，将安装的位置放进`PATH`环境变量中，在终端中输入

```powershell
oh-my-posh --version
```

确认是否安装成功。

同时将下载下来的主题文件放在一个特定的地方（我这里是`C:\Users\ricardo\Programs\oh-my-posh\themes`），在PowerShell的启动配置文件`$PROFILE`中加入下面几句

```powershell
Set-Item env:POSH_THEMES_PATH "C:\Users\ricardo\Programs\oh-my-posh\themes"
oh-my-posh init pwsh --config "$env:POSH_THEMES_PATH/paradox.omp.json" | Invoke-Expression
```

下载[posh-git](https://github.com/dahlbyk/posh-git)，这是一个给PowerShell提供Git相关辅助的模块，下载完成之后，将下列命令添加到`$PROFILE`：

```powershell
Import-Module ~\Programs\posh-git\src\posh-git.psd1
```

如果在修改了配置文件之后启动提示运行脚本没有经过签名，可以采用下面这条命令来修改运行脚本的权限：

```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser
```

再重新运行配置文件就没有问题了。

![终端预览](6.webp)

### PowerShell配置文件

```powershell
# oh-my-posh setup
oh-my-posh init pwsh --config "C:\Users\ricardo\Programs\oh-my-posh\themes\paradox.omp.json" | Invoke-Expression

Import-Module ~\Programs\posh-git\src\posh-git.psd1

# proxy functions
function Set-Proxy {
    Set-Item Env:http_proxy "http://127.0.0.1:7890"
    Set-Item Env:https_proxy "http://127.0.0.1:7890"
}

function Remove-Proxy {
    Remove-Item Env:http_proxy
    Remove-Item Env:https_proxy
}
```

### WSL美化

目前我还在使用`bash`，不过使用`oh-my-posh`进行了一定的美化。

同Windows下一致进行`oh-my-posh`的下载和安装，在`~/.bashrc`中添加这样一句配置文件

```bash
eval "$(oh-my-posh init bash --config /home/ricardo/.poshthemes/paradox.omp.json)"
```

### BASH设置

在`~/.bashrc`文件的末尾新增

```bash
# oh-my-posh setup
eval "$(oh-my-posh init bash --config /home/ricardo/.poshthemes/paradox.omp.json)"

# proxy function
export hostip=$(cat /etc/resolv.conf | grep "nameserver" | cut -f 2 -d " ")

proxy()
{
    export http_proxy = "http://${hostip}:7890"
    export https_proxy = "http://${hostip}:7890"
}

unproxy()
{
    export http_proxy=""
    export https_proxy=""
}

# alias settings
alias python=python3
```

## 其他的小工具

字体：

- [Fira Code](https://github.com/tonsky/FiraCode)

命令行工具

- [dust](https://github.com/bootandy/dust)
- [scc](https://github.com/boyter/scc)
- [tcping](https://github.com/cloverstd/tcping)








