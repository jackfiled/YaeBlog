---
title: 在浏览器中打开VSCode
date: 2021-10-30T17:16:13.0000000
tags:
- 技术笔记
---


众所周知，VSCode作为大微软家开发的开源编辑器，一经发布便受到了两广大程序员群体的欢迎。如果我们深入的了解一下VSCode，就会知道VSCode是基于Electron框架构建的Web应用程序，而Electron框架是基于Web技术来开发桌面应用程序，即VSCode只要稍加改造，就可以流畅的在浏览器中运行。那么我们如何才能在浏览器打开一个VSCode呢？   
最简单的方法是在浏览器中输入[这个网址](https://vscode.dev)，就可以在浏览器中打开一个VSCode Online，这个版本的VSCode可以支持打开本地的文件，并且进行编辑。不过这个编辑器并不支持大部分的插件，而且并不支持程序的编译与运行，并不是一个可以开箱即用的编辑器。那么还有什么办法可以让我们拥有一个联网即可得的个人定制化开发环境呢？   
<!--more-->

## 采用code-server搭建自己的VSCode Online服务器端
code-server是一家第三方公司开发的VSCode Online的服务器端，在你自己的服务器上安装这个软件之后，你就可以通过你自己指定的网址访问位于自己服务器上的VSCode了。   
>要是你没有服务器？ 可以去各大云服务器提供商那里购买一台云服务器。

我这里采用自己在阿里云上购买的轻量应用服务器上部署自己的VSCode服务器，这台轻量应用服务器有着2核2G，60GESSD的配置，对于一个VSCode来说是绰绰有余，而且这个套餐提供了最高5M的带宽，较宽的带宽使自己网页中打开VSCode的速度大大提高，极大的提升了使用体验。大家自己的选择自己用来配置VSCode服务器的时候也要记得选择带宽较大的套餐，否则在网页中使用时极慢的加载速度会让你在浏览器中编写代码的体验水深火热。    
再购买好自己的服务器，并确保自己能够通过SSH连接到服务器上进行配置之后，我们便可以正式开始在服务器上配置code-server了。   
首先在自己的服务器上下载coed-server
```
wget https://github.com/cdr/code-server/releases/download/v3.12.0/code-server-3.12.0-linux-amd64.tar.gz
```
我在下载的时候，code-server的最新版是3.12.0，在之后下载的朋友可以在[这里](https://github.com/cdr/code-server/releases)找到最新版code-server的下载安装连接，注意在下载时选择正确的版本。   
在下载完成之后，利用
```
sudo tar -zxvf [你的下载下来的code-server文件名] -C /opt/
```
将code-server解压到/opt目录下，再使用
```
sudo ln -s /opt/[code-server文件名]/bin/code-server /usr/local/bin/code-server
```
为code-server创建一个软链接，这样就可以直接输入
```
code-server
```
来启动code-server服务了。     

安装好code-server之后，首先输入
```
code-server
```
运行code-server一次，这一次运行会让code-server在现在登录的用户目录~/.config/code-server下生成一个名叫config.yaml的配置文件，这样我们后续就可以直接编辑这个配置文件来控制code-server启动的相关参数。    
我们先终止code-server的运行，执行以下命令来编辑code-server的配置文件
```
vim ~/.config/code-server/config.yaml
```
>如果你不喜欢用vim编辑器的话，可以自己采用nano编辑器或者其他的喜欢的编辑器
在默认情况下，这个文件应该是这个样子
```
bind-addr: 127.0.0.1:8080
auth: password
password: [code-server自动生成的密码]
cert: false
```
在第一行，127.0.0.1代表这是本机的IP，如果要在公网上访问的话，需要将这里的IP改为0.0.0.0，后面的端口在默认的情况下是8080，你可以改成自己喜欢的端口号，在第二行的password表示采用密码进行身份验证，我们需要在第三行设置自己熟悉的密码，以方便自己的访问，<del>当然，你把默认生成的密码背下来应该也是可以的</del>    
在进行了这些更改之后，我们再次输入code-server重启服务，如果一次顺利，我们可以看见以下的启动信息
![启动信息](./vscode-in-browser/1.webp)
我们可以打开浏览器，在地址栏中输入你的服务器公网IP加上你自己设置的端口号，就可以打开自己的VSCode Online界面了。    
![主界面](./vscode-in-browser/2.webp)
输入自己的设置密码，就可以开始把浏览器中的VSCode当作自己本地计算机上的VSCode使用了，不过其中的文件是位于自己的服务器上的。
>如果你和我一样使用的阿里云的服务器，可能还需要到服务器的管理界面设置安全组放行相应的端口，具体参考[这篇文章](https://help.aliyun.com/document_detail/59086.html?spm=5176.10173289.help.dexternal.4ff02e77892BZP)

## 保持code-server在服务器中的运行
配置完code-server之后，我们便可以退出SSH登录，愉快的直接利用Online界面来编写代码了。   
>由于VSCode自带有终端界面，我们甚至连SSH登录都不需要了
但是我们会很快意识到，如果我们退出SSH会话，那么code-server服务也会自动的退出，因为我们如果关闭了SSH， 那么依附其运行的进程也会自动的关闭。我们这里就需要用到一个名叫tmux的软件了。    
tmux是一个终端复用器，可以让我们在一个终端中，在打开另一个终端，并在其中运行自己想要运行的程序。   
>以下仅是对于tmux的简答介绍，具体可参看[这篇教程](https://www.ruanyifeng.com/blog/2019/10/tmux.html)

我们首先安装tmux
```
sudo apt update
sudo apt install tmux
```
>如果你并不和我一样使用Ubuntu，请使用你自己常用的包管理命令安装此软件

我们先输入
```
tmux new -s code-server
```
创建一个新的会话，可以将“code-server”任何你喜欢的名字，再在这个会话中运行code-server,按下Ctrl+B后再按下D来脱离这个会话，这样，我们就可以放心的退出SSH会话了。   
以下说说几个tmux常用的命令：
```
tmux ls
```
列出已创建的会话   
```
tmux attach -t <session-name>
```
再次连接到自己创建的会话   
```
tmux kill-session -t <session-name>
```
删除某个会话

