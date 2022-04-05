---
title: 优雅地使用学校VPN
tags:
---

# 优雅的使用学校VPN

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

