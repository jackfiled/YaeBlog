---
title: 优雅地使用学校VPN
date: 2022-04-14 11:51:07
tags:
  - 技术笔记
---

# 优雅的使用学校VPN

<!--more-->

## 太长不看版

### 背景介绍

我所在的学校使用的是Global Protect提供的入校VPN，但是Global Protect的VPN客户端会劫持掉电脑上所有的网络连接，显然这并不是我们想要的，频繁开关VPN客户端的方法又有点愚蠢，在折腾了一会儿之后我发现了一条不错的思路。在docker的容器中采用openconnect连接学校VPN，再在容器中搭建一个socks5服务器，在本地使用Chrome浏览器使用这个socks5服务器就可以直接访问学校VPN了。而且，在docker Hub上已经有一个刚好符合我们需求的镜像——[openconnect-proxy](https://hub.docker.com/r/wazum/openconnect-proxy)。

### 实际操作

在本地或者服务器上拉取这个镜像，~~安装docker的方式在本文中暂且不表~~，采用docker-compose的方式部署你的容器。新建一个文件夹，在里面放置`docker-compose.yml`和`.env`文件，前者是docker的配置文件，后者是在docker中使用openconnect的配置文件。

docker-compose.yml的内容如下

```yaml
version: '3'
services:
  vpn:
    container_name: openconnect_vpn
    image: wazum/openconnect-proxy:latest
    privileged: true
    env_file:
      - .env
    ports:
      - 8888:8888
      - 8889:8889
    cap_add:
      - NET_ADMIN
```

.env文件的内容如下

```
OPENCONNECT_URL=//你的服务器地址
OPENCONNECT_USER=//你的账号
OPENCONNECT_PASSWORD=//你的密码
OPENCONNECT_OPTIONS=--protocol=//你的服务器采用的协议，gp代表Global Protect
```

然后运行镜像就完事了，如果在服务器上运行记得在安全组开启相应的端口

## 详细的~~折腾~~探索过程

### 找到合适的VPN客户端

为了避免学校提供的VPN客户端会劫持所有的网络流量这个问题，首先得找到一个可以替代掉这个客户端的连接程序。

很快我就找到了也支持Global Protect协议的开源客户端，[OpenConnect](https://www.infradead.org/openconnect/)。但是在简单的查阅文档之后，发现这个客户端也是会代理全局的网络流量。这条道路失败。

### 尝试扩展这个开源的客户端

既然这个连接客户端式开源的，那便有着无限的可能。而且不可能只有我一个人遇到了这个问题，一定有伟大的前人解决了这个问题。在各种博客文章中反复穿梭之后，我发现了一个`OpenConnect`的扩展插件，[ocproxy](https://github.com/cernekee/ocproxy)，这个扩展可以建立一个Socks代理服务器，使OpenConnect只能处理发送给这个代理服务器网络流量。

大胜利~

### 开始愉快的使用

啪的一声，很快啊，这个服务器就在服务器上搭建起来了，就可以使用Chrome浏览器的Switch-Omega之类的插件使用这台服务器了。

还没有高兴到五分钟，在使用中我就发现这个服务貌似有点性能比较垃圾啊啊啊啊啊。

打开学校的通知门户，几乎就没有图片可以正常的打开，打开一些学校里的服务网站，稍微大一点的js文件直接无法加载。在反反复复的研究ocproxy那少得可怜的文档之后，我直接放弃了这个办法，这个玩意儿可以用，但不是完全可以用。

大失败再次降临力！

### 上邪法——docker

> 这已经不是一般的问题，必须出重拳。

既然这些客户端会代理电脑上所有的网络流量，那我就找一台计算机专门让它代理！

但显然这里的计算机不是物理意义上的计算机，而是一台轻量化的应用运行平台，我在这里选择了docker。

在[docker-hub](https://hub.docker.com/)上逛了逛，果然早有人意识到这个问题，在上面已经有了一个准备好的dockers镜像，[openconnect-proxy](https://hub.docker.com/r/wazum/openconnect-proxy)。

很快的pull下镜像，一波建立容器运行，一个新的代理服务器又建立了起来。

大胜利~

### 后记

这个服务器在访问我们的教务系统时还是出问题了。。。。

