---
title: 安装vs2019踩的坑
date: 2021-08-14T10:06:00.0000000
tags:
- 技术笔记
---


在某个月黑风高的夜晚，我在折腾了很久的Python之后，突然感觉自己应该去学学C和C++，于是乎我便打算折腾一下在vscode上写C和C++。在网上一番搜寻之后，我发现了这篇[知乎文章](https://zhuanlan.zhihu.com/p/87864677)和这篇[知乎文章](https://zhuanlan.zhihu.com/p/147366852),然后我就被安装MinGW编译器和配置一大堆的json文件给干碎了。<br>
于是，我决定转向传说中的宇宙第一IDE——Visual Studio。<br>
<!--more-->
上网一查，现在最新的vs版本是2019，于是立刻进入[Visual Studio官网](https://visualstudio.microsoft.com/zh-hans/vs/)点击下载vs2019<del>enterprise</del>Community版。<br>
> 无他，就是太穷了。<br>

在点击下载链接之后，跳转到了感谢下载的界面。<br>
![](./question-in-install-vs-2019/1.webp)
上面清楚的写着，下载将很快开始。
在经过漫长的等待之后，我下载下来了这个东西
>vs_community__355502915.1625217430.exe

于是一波双击，飞快地下载安装选好工作负载，又在一波漫长的下载之后，我兴奋的双击打开了安装好的“vs2019”。

但是！我却惊讶的发现，我安装的是Visual Studio 2017!   
我当时就是一脸懵逼，想了想以为是自己在安装工作负载的时候选择错了vs的版本，于是我又打开了visual studio installer，却惊讶的发现，只能安装vs2017的社区版，专业版和企业版。
>不可能，微软的软件不会出错，一定是我自己选错了。

我立刻卸载了电脑上的vs2017和Visual Studio Installer,再次前往vs的[官网](https://visualstudio.microsoft.com/zh-hans/vs/),这次我前检查后检查，确定自己下载的是Visual Studio 2019的Community版本，在单击了下载链接之后，我又跳转到了感谢下载的界面。   
![](./question-in-install-vs-2019/1.webp)
但是这次，在令人十分无语的等待之后，<strong>我不得不点了下单击此处以重试</strong>。
在一段令人紧张的等待之后，Visual Studio Installer安装好了，我也失望的发现，我安装的仍然是vs2017。
>谢谢，电脑已经砸了。

但是，作为一个程序员，我们不能说不！  
于是，我想到，既然下载开始的如此之慢，要不，<b>挂个梯子</b>试试？  
说做就做，我又卸载了Visual Studio Installer,在打开VPN之后，我又双打开了[Visual Studio官网](https://visualstudio.microsoft.com/zh-hans/vs/), 怀着紧张而激动的心情，点击了vs2019社区版的下载按钮。仍然一如既往的跳转到了感谢下载的界面，不过这次，下载几乎立刻就开始了！
>OHHHHHHHHHHHHHHHHHHH

接着就是双击，下载，安装，我打开Visual Studio Installer，让我安装的果然是vs2019！接着，我泪流满面的选好了工作负载，点击了下载。  

## 现在，我们来复盘一下
我记不清清楚我第一次下载时是否点击了<i>单击此处以重试</i>,但是我的第二次尝试时确定点击了的，在结合开了VPN立刻下载的”解法“，我便将注意力放在了这个按钮上。  
这是点击重试之后的下载地址：
```
https://download.visualstudio.microsoft.com/download/pr/343898a7-7d12-4faa-b5df-958b31e57b3e/0e17eb53023c8a4d07e1dfd201e8a0ebff2c56c74ad594c8f02521fb5b27c7db/vs_Community.exe
```
这是不点击重试的下载地址：
```
https://download.visualstudio.microsoft.com/download/pr/45dfa82b-c1f8-4c27-a5a0-1fa7a864ae21/9dd77a8d1121fd4382494e40840faeba0d7339a594a1603f0573d0013b0f0fa5/vs_Community.exe
```
下面是我挂着梯子下载下来的安装程序的MD5值：
>7382158e92bb9af82a24c5c3eba80c20

下面是不单击重试下载的安装程序的MD5值：
>7382158e92bb9af82a24c5c3eba80c20

下面是单击重试下载的安装程序的MD5值：
>88f28257ae1e6ce4a7ebd5d6f7f94f0f

至此，真相已经摆在了我的面前。

## 总结
在感谢下载的页面，如果你耐心等待，那么在一段时间后下载的就是vs2019，但是如果你是一个急性子，点击了“单击此处以重试”的按钮。那么你下载的就是vs2017的版本。


