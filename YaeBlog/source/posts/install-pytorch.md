---
title: 安装pytorch，来有深度的学习
date: 2021-11-13 13:21:53
toc: true
tags: 
        - 技术笔记 
typora-root-url: 安装pytorch，来有深度的学习
---

鄙人在下不才我精通深度学习框架的安装和卸载。

<!--more-->

## pytorch简介

现在我们正处在人工智能的风口上，虽然话说得好“在风口上，一头猪都能起飞”，但是我们显然没法像猪一样飞起来，但是跑两步，至少装一个可以运行人工智能的环境是可以的，就算没法吃猪肉，也得看看猪跑。  

`pytorch`就是一个开源的`python`机器学习框架，主要由`facebook`的人工智能团队开发。这个框架采用类似于`numpy`的张量计算，可以使用GPU进行加速，有着自动微分系统的深度学习模块。

## 安装pytorch

### conda的简介

我们首先要明确的是，`pytorch`是一个开源的python深度学习库，在安装pytorch之前我们得先安装python解释器。可能有人会说，安装python解释器难道不是小学二年级的内容吗，无非是下载python，添加到环境变量，再命令行输入python验证安装是否完成三部曲。这难道是很困难的事情吗？    

可如果我们的在~~生活中~~学习中需要用到多个不同的python环境怎么办？安装多个python解释器之后如何在不同的环境中切换？而且可能在不同的版本下所用的库的版本还不尽相同，这时候我们就需要一个便捷的”环境管理器“来管理你电脑上的python环境了，这个管理器便是`conda`。再安装了`conda`之后，我们就可以在电脑里创建一个个”独立“的python环境，每个环境可以有不同的解释器，不同的库。可以说是十分方便了。   

> 比如在复现其他人的研究成果的时候，他所用的`pytorch`版本和你的不一样，甚至你们的`python`解释器的版本都不一定完全相同。

> 至少一行命令就可以安装python，这已经很香了
> 而且`conda`在安装需要的库时，会自动的分析依赖，可以在一定程度上解决环境总是崩溃的问题。

### 安装conda

在安装`conda`的时候，我们可以选择安装`anaconda`这个Python发行版，这是一个致力于简化在科学计算中包管理和部署的发行版。

但是这个的问题在于他过于的全面，而我们只是想要安装自己需要的包，因此我选择`miniconda`这个简化的`conda`安装方式，这个安装包中就只有`conda`和`python`两个程序。同时可以利用`conda`和`pip`这两个包管理器安装`anaconda`和`pypi`这两个包仓库中的所有包。

> 这两者的比较，也可以参看[官方文档](https://docs.conda.io/projects/conda/en/latest/user-guide/install/download.html#anaconda-or-miniconda)

我们访问`miniconda`的[官方网站](https://docs.conda.io/en/latest/miniconda.html) ，下滑找到最新的`miniconda`安装包下载链接（Latest Miniconda Installer Links）。 

![](1.png)

按照自己的电脑系统下载相应的安装包就可以了。也不大，就50M出头的样子。

下载完成之后，我们运行这个安装包，一路下一步就可以了，一般情况下不会有什么要改动的地方。   

安装完成之后，我们就会发现自己的开始菜单里多出来了两个快捷方式,

![](2.png)
在上面的那个快捷方式**Anaconda Prompt**是运行含有`conda`命令的CMD的快捷方式，而下面那个**Anaconda Power Shell Prompt**是运行含有`conda`命令的Power shell界面的快捷方式。因为我个人对于power shell更加的熟悉我们就是用下面的那个快捷方式。   

这个时候肯定会有人好奇，竟然你都用power shell了，我们为啥不直接使用系统里已经有了的power shell，而要启动这里的这个显得有点奇怪的power shell呢？     

如果你好奇，你可以在系统里的power shell里键入`conda`, 会发现系统会告诉你`conda并不是内部化外部应用程序`，马上又有人会说，这个表示`conda`这个程序所在的位置没有被添加进入系统的环境变量里，如果添加了之后就可以在系统的power shell里使用了，而且我在安装`miniconda`的时候，还看见了添加到环境变量的选项，只是默认的情况下没有勾选。确实，在默认的状态下，`conda`并不会被添加在环境变量中，因为在`conda`的所在目录下还有`python`，如果你将`conda`添加进入了环境变量，就有可能改变原来了系统自带的`python`解释器。所以在默认情况下`conda`是不会被添加到环境变量中的。不过如果你的电脑中并没有`python`，或者像我一样直接采用`conda`来管理电脑上的`python`环境，也可以直接将`conda`添加在环境变量中，而且在后面我们使用`VSCode`编写代码的时候，我们只有将`conda`添加进入环境变量，才可以直接运行代码。~~当然，大概率还是有其它的办法，只是我太菜了不知道~~    

好，我们运行**Anaconda Power Shell Prompt**，打开了这个窗口

![](3.png)

这个窗口的出现就标志着我们已经把`conda`正确的安装在我们的电脑之中了。

### conda的使用

我们很容易发现，这个power shell与我们常用的power shell之家最大的不同就是在PS的前面多了一个（base），这个括号就表示我们目前所处的环境是`conda`自动生成的初始环境(base环境)。
在我安装的时候，这个环境的python版本是3.9，一会我们就会在这个环境之中安装我们本文的主角**pytorch**,不过在安装之前，我们先来熟悉一下`conda`，毕竟我们会使用这个包管理器来安装我们需要用到的大部分包。   

#### 使用conda创建与删除环境

> 在没有指明的情况下，请在**Anaconda Powershell Prompt**环境中使用以下的命令
```
conda info -e
```
这个命令会展示目前系统中已经创建的环境，输入后的效果：
```
# conda environments:
#
base                  *  C:\Users\ricardo.DESKTOP-N6OVBK5\Programs\miniconda3
py310                    C:\Users\ricardo.DESKTOP-N6OVBK5\Programs\miniconda3\envs\py310
```
其中会指出环境所在的文件夹，星号标志当前所处的环境。  

> 这里也可以使用`conda env list`

```
conda create --name newev python=3.10
```
使用这个命令来在创建一个新的环境，这个环境的由“--name”之后的内容决定，这个环境的python解释器版本由python=决定，注意这里的环境只用指定到3.x，`conda`会自动寻找这个系列下最新的版本安装。   
在创建了一个环境之后，使用

```
conda activate <envirname>
```
来切换到相应的环境，`envirname`是你自己指定的环境的名称。  

如果像退出这个环境，回到`conda`的默认环境，使用

```
conda deactivate
```
再使用这个命令之后，我们就会回到base环境，我们可以通过看PS前面的名字来确认这一点。   

#### 使用conda管理自己的库

```
conda list
```
这个命令会展示在在当前环境中安装的包。而且注意，虽然我们使用的是`conda`来管理与安装库，但是这并不表示python自带的pip包管理工具就被废弃，而且就算你通过pip安装的python库页会被`conda`上面的命令正确的识别到。
```
conda list -n <envirname>
```
使用这个命令来查看指定的环境中安装的包。    
```
conda search <pkgname>
```
搜索指定的包的信息，比如我输入
```
conda search numpy
```
`conda`会给我返回

```
Loading channels: done
# Name                       Version           Build  Channel
······
numpy                         1.21.2  py37hfca59bb_0  anaconda/pkgs/main
numpy                         1.21.2  py38hfca59bb_0  anaconda/pkgs/main
numpy                         1.21.2  py39hfca59bb_0  anaconda/pkgs/main
```
`conda`就会返回给我们包的版本，相关的python版本，下载的渠道  

```
conda install <pkgname>
```
使用这个命令在当前所处的环境里安装我们所需要的包
```
conda install -n <envname> <pkgname>
```
使用“-n”参数来指定一个环境安装我们需要的包
```
conda update -n <envname> <pkgname>
```
使用这个命令来更新指定的包
```
conda remove -n <envname> <pkgname>
```
使用这个命令删除指定环境中我们所不需要的包
分别使用
```
conda update conda 
conda update python
```
来升级`conda`与`python`
> 注意，假设现在python版本是3.5，则只会升级到3.5系列的最新版

#### 使用conda的国内镜像源

由于部分原因，使用conda的默认镜像源安装我们需要的库可能花费的时间较长，或者提示网络连接失败等等。为了解决这个问题，我们可以使用清华大学提供的镜像源，镜像站提供了相关设置的[帮助文档](https://mirrors.tuna.tsinghua.edu.cn/help/anaconda/)，根据文档的指示一步步设置就可以了。   

### 使用conda安装pytorch
我们访问pytorch的[官方网站](https://pytorch.org/),我们可以轻松在首页就找到这个表格
![](4.png)
先简单介绍一下这个表格的每一行，第一行是选择我们安装`pytorch`的版本，是稳定版，预览版，还是长期支持版。第二行是选择我们索要下载的操作系统，这里就默认大家都是用Windows了，第三行是选择我们安装`pytorch`的方式，可以看见有我们刚学习的`conda`，我们很熟悉的`pip`, 还有两种其他的方式。我们选择`conda`。第三行是我们所使用的语言，我们自然选择`python`。然后是第四行，“Compute Platform”， 如果直译的话是计算平台，就是我们选择用来计算的设备。这里就是又一个新的知识点了。一般来说，我们在使用电脑时，都是使用CPU作为计算的主力，显卡（GPU)一般只是用来输出图像，但在深度学习出现后，人们发现显卡原本专精于图形计算的计算力也很适用于深度学习中人工神经网络的计算。为了能够使用GPU的算力，而不是让它仅仅输出图像，我们就得下载相关的工具，这个工具就是CUDA。当然，这种比较强大的能力并不是只要下载一个CUDA就可以拥有的，~~你还得有钱~~你还得拥有一块英伟达的显卡，如果你没有显卡，那你就只能老老实实的使用自己的CPU进行计算了，在表格的第四行选择CPU。而对于我们~~土豪~~有显卡的同学，我们得先确定确定自己电脑上CUDA的版本，打开我们的老朋友power shell，输入

```
nvidia-smi
```
这是英伟达显卡的一个监控命令，可以看见当前GPU的使用情况，我们从这里来看自己的CUDA版本。

在我的RTX3060笔记本上，输出是这样的

```
Mon Nov 15 10:43:50 2021
+-----------------------------------------------------------------------------+
| NVIDIA-SMI 462.36       Driver Version: 462.36       CUDA Version: 11.2     |
|-------------------------------+----------------------+----------------------+
| GPU  Name            TCC/WDDM | Bus-Id        Disp.A | Volatile Uncorr. ECC |
| Fan  Temp  Perf  Pwr:Usage/Cap|         Memory-Usage | GPU-Util  Compute M. |
|                               |                      |               MIG M. |
|===============================+======================+======================|
|   0  GeForce RTX 306... WDDM  | 00000000:01:00.0 Off |                  N/A |
| N/A   49C    P0    19W /  N/A |    121MiB /  6144MiB |      0%      Default |
|                               |                      |                  N/A |
+-------------------------------+----------------------+----------------------+

+-----------------------------------------------------------------------------+
| Processes:                                                                  |
|  GPU   GI   CI        PID   Type   Process name                  GPU Memory |
|        ID   ID                                                   Usage      |
|=============================================================================|
|  No running processes found                                                 |
+-----------------------------------------------------------------------------+
```
> 注意：以下内容仅供参考，我只能说，我这样是成功了的，不代表所有人都可以成功

在表格的第一行，我们可以看见我电脑上CUDA的版本是11.2，我们回到`pytorch`的官网，发现它的表格中只有cuda10.2与cuda11.3两个版本供我们选择，虽然我的电脑上的`cuda`只是11，2的，但是我选择的仍是cuda11.3版本的，因为3060貌似是不支持cuda10的了。~~玄学开始了~~我们在表格中选择完之后，就会得到一句命令，把这个命令复制到**Anaconda Power shell Prompt**中去运行，在提示确认的地方回车，就会开始下载安装`pytorch`，我们耐心等待一段时间。   
在安装完成之后，我们来到了~~最激动人心的~~验证安装是否成功以及cuda是否能够被正确调用的环节，我们首先输入

```
conda list
```
查看当前的环境中是否已经有`pytorch`这个库，一般来说，这一步不会有大问题。
然后我们输入

```
python
```
打开python的交互式解释器，输入
```
import torch
```
看看能否正确的导入这个库，这个导入的过程可能有点长，不要紧张    

如果这锅过程没有报错的话，就说明导入正常了，我们简单的使用一下这个库来进一步判断，输入

```
x = torch.rand(3,5)
print(x)
```
输出
```
tensor([[0.7280, 0.0764, 0.1278, 0.0408, 0.8655],
        [0.8270, 0.2127, 0.1831, 0.0908, 0.6578],
        [0.8396, 0.4007, 0.2550, 0.8508, 0.4947]])
```
这里是输出的均是随机数，如果数不一样才正常。

我们在验证一下是否可以调用`cuda`, 输入

```
print(torch.cuda.is_available())
```
如果输出True就是皆大欢喜，安装成功，如果输出False，那~~建议放弃治疗~~我也爱莫能助，毕竟我这一路下来虽然在`cuda`的版本上有点不对劲的地方，但他就是给我返回了
```
True
```

## 写在最后

在安装`pytorch`的这一路下来，我在网上查了无数的资料，似乎在安装`pytorch`的一路上就死了不少的人~~大概是因为他们是在Ubuntu上安装的~~，特别是调用`cuda`的那里。不过我按照[官方文档](https://pytorch.org/get-started/locally/)一路下来，没有出什么大的幺蛾子，只能说希望我自己的经历能对大家有所帮助吧。
