---
title: Podman容器中特定域名解析失败问题排查
date: 2025-10-22T17:42:07.7738936+08:00
tags:
- 技术笔记
- Bug Collections
---

最难以置信的一集。

<!--more-->

## 问题现象

在折腾我的Gitea  CI/CD管线的时候我，我尝试进行一项重要的变更：在运行CI任务时，任务中的容器支持从Docker out of Docker（DoD）迁移到Podman inside Podman（PiP）。但是在执行该项迁移的过程中，我遇到了如下一个非常神秘的Bug。

在CI任务运行的过程中，我发现一些**特定的**、**稳定的**域名无法在任务容器中正常解析并请求。目前注意到的域名有如下两个：

- dot.net
- rcj.bupt-hpc.cn

在容器内使用`curl http://dot.net -vvvvi`时，日志中重复出现如下内容。

```
10:35:43.261007 [0-x] == Info: [MULTI] [INIT] added to multi, mid=1, running=1, total=2
10:35:43.261252 [0-x] == Info: [MULTI] [INIT] multi_wait(fds=1, timeout=0) tinternal=0
10:35:43.261533 [0-x] == Info: [MULTI] [INIT] -> [SETUP]
10:35:43.261690 [0-x] == Info: [MULTI] [SETUP] -> [CONNECT]
10:35:43.261838 [0-x] == Info: [READ] client_reset, clear readers
10:35:43.262042 [0-0] == Info: [MULTI] [CONNECT] [CPOOL] added connection 0. The cache now contains 1 members
10:35:43.262315 [0-0] == Info: [DNS] init threaded resolve of rcj.bupt-hpc.cn:9090
10:35:43.262597 [0-0] == Info: [DNS] resolve thread started for of rcj.bupt-hpc.cn:9090
10:35:43.262854 [0-0] == Info: [MULTI] [CONNECT] [TIMEOUT] set ASYNC_NAME to expire in 1000ns
10:35:43.263081 [0-0] == Info: [MULTI] [CONNECT] -> [RESOLVING]
10:35:43.263254 [0-0] == Info: [MULTI] [RESOLVING] multi_wait pollset[fd=4 IN], timeouts=1
10:35:43.263500 [0-0] == Info: [MULTI] [RESOLVING] [TIMEOUT] ASYNC_NAME expires in 355ns
10:35:43.263743 [0-0] == Info: [MULTI] [RESOLVING] multi_wait(fds=2, timeout=1) tinternal=1
10:35:43.265041 [0-0] == Info: [DNS] Curl_resolv_check() -> 0, missing
10:35:43.265218 [0-0] == Info: [MULTI] [RESOLVING] multi_wait pollset[fd=4 IN], timeouts=1
10:35:43.265497 [0-0] == Info: [MULTI] [RESOLVING] [TIMEOUT] ASYNC_NAME expires in 1545ns
10:35:43.265736 [0-0] == Info: [MULTI] [RESOLVING] multi_wait(fds=2, timeout=2) tinternal=2
10:35:43.268052 [0-0] == Info: [DNS] Curl_resolv_check() -> 0, missing
10:35:43.268236 [0-0] == Info: [MULTI] [RESOLVING] multi_wait pollset[fd=4 IN], timeouts=1
10:35:43.268471 [0-0] == Info: [MULTI] [RESOLVING] [TIMEOUT] ASYNC_NAME expires in 3582ns
10:35:43.268688 [0-0] == Info: [MULTI] [RESOLVING] multi_wait(fds=2, timeout=4) tinternal=4
```

## 初步假设与排除过程

这个问题非常奇怪，在问题排查初期，我进行了如下的操作。

1. 首先单独测试容器中的域名解析，使用`dig rcj.bupt-hpc.cn`进行解析测试，解析的结果符合预期，使用的DNS服务器是当前的容器的Podman网关地址10.17.14.1。
2. 独立启动容器，而不是使用CI管线启动的容器，使用`podman run --rm -it --privileged ccr.ccs.tencentyun.com/jackfiled/runner-base:latest bash -l`创建容器，对上述出现问题的两个域名上的服务进行请求，没有问题，排除镜像本身的问题。

此时已经束手无策了，遂尝试AI辅助进行排查。

## AI辅助下的问题排查

### dig工具和libc的解析机制之前存在差异

AI首先提到，上面测试的dig工具和其他应用使用的libc解析机制之间存在差异：

- `dig` 是直接使用 DNS 协议向 DNS 服务器发查询请求（绕过系统的解析器）。
- `curl`（以及大多数应用）依赖系统 C 库（如 glibc）的 `getaddrinfo()` 函数，它会读取 `/etc/nsswitch.conf` 和 `/etc/resolv.conf`，并可能使用 `systemd-resolved`、`nscd` 等服务。

于是需要使用`getent`工具测试libc库的DNS解析是否存在问题，测试发现解析结果正常。

### IPv6 优先导致超时或失败

AI还提到，如果系统尝试先用 IPv6 解析（AAAA 记录），但网络不支持 IPv6，可能导致 `curl` 卡住或失败，而 `dig` 默认查 A 记录。

于是在使用`curl`时手动指定使用的网络协议，的确当使用`curl -4 http://dot.net`时可以正常的完成请求。

虽然这一方法可以解决问题，但是其给出的理由似乎无法解释我遇到的问题，问题域名中的rcj.bupt-hpc.cn并没有任何的AAAA解析记录，执行 `getent ahosts rcj.bupt-hpc.cn` 仅返回 IPv4 地址，无 AAAA 记录，说明系统解析器未返回 IPv6 地址，`curl -4` 的成功不能用IPv6解释。

### DNS服务器

在尝试AI给出的几个建议之后，我也开始尝试修改容器内的DNS服务器，进行这个尝试是因为我发现，使用`podman run`启动容器时，容器内部使用的DNS服务器是10.3.9.4这一主机使用的DNS服务器，而在CI管线中启动的容器使用的DNS服务器是172.17.14.1这一容器网关。

于是在CI启动的容器中，我通过直接写入`/etc/resolv.conf`文件的方式修改DNS服务器为10.3.9.4，在修改之后，针对两个域名的网络请求就恢复了正常。

在将上述的新发现同步给AI之后，AI又给出了一些新的思路。

### c-ares 异步 DNS 解析器兼容性问题？ 

- 某些编译版本的 `curl`（尤其是 Alpine、某些容器镜像）会链接 `c-ares` 库，**绕过 glibc 的 `getaddrinfo`**，直接读取 `/etc/resolv.conf` 并发 DNS 请求。
- `c-ares` **不读取 `/etc/nsswitch.conf`**，也不支持 mDNS、systemd-resolved 等机制。
- 如果 `/etc/resolv.conf` 中的 nameserver（如 `172.17.14.1`）对 **某些域名响应异常**（如超时、截断、不响应 UDP 包），`c-ares` 会卡住。

但是按照AI提供的步骤进行排查：

- `curl --version` 输出中**未包含 `c-ares`**；
- curl 使用 glibc 的 `getaddrinfo()` 或 libcurl threaded resolver；
- **结论**：非 c-ares 导致。

### UDP DNS 响应截断（>512 字节）？

- `c-ares` 默认使用 **UDP** 查询 DNS。
- 如果 Podman 的内置 DNS 代理（通常是 `netavark` + `aardvark-dns` 或旧版 `dnsmasq`）在处理 **特定域名** 的 UDP 请求时：
  - 响应过大导致 UDP 截断（TC=1），但 `c-ares` 未自动重试 TCP；
  - 或代理本身 bug 导致丢包；
- 则 `c-ares` 会认为“无响应”，返回“missing”。

而 `dig` 默认使用 UDP，但**当 TC=1时会自动使用TCP重试**，所以成功，而你的域名 `rcj.bupt-hpc.cn` 可能有较多 CNAME 或 TXT 记录，导致 UDP 响应超 512 字节。 

但是我清楚的知道域名 `rcj.bupt-hpc.cn` 仅有一条 A 记录，DNS 响应极小，`dig` 与 `getent` 均能快速返回结果。

## AI Conclusion：Podman 内置 DNS 代理（aardvark-dns）行为异常

Podman 4.5.2 默认使用 **aardvark-dns** 作为容器网络的 DNS 代理，监听于网关地址（如 `172.10.0.1`）。其职责包括：

- 解析容器名称（服务发现）；
- 转发外部域名查询至上层 DNS。

可能的故障点包括：

1. aardvark-dns 对特定域名后缀（如 `.bupt-hpc.cn`）的转发逻辑存在缺陷；
2. 响应格式不符合 glibc 或 libcurl 的预期（如缺少 AA 标志、TTL 异常）；
3. 首次查询存在延迟或丢包，而 libcurl threaded resolver 未充分重试；
4. 与宿主机 `/etc/resolv.conf` 中的 `search` 域交互异常，导致拼接错误查询。

尽管 `dig` 能获取结果，但 `dig` 具有更强的容错性和自动重试机制（如 TC=1 时切 TCP），而 libcurl 的 resolver 更严格。 

> 对于AI的这个结果，说实话我表示怀疑，我感觉还是和IPv6有关系，这样才能同时解释`curl -4`和更换DNS都可以运行的现象。

## 临时缓解

最终，我在CI启动容器的配置选项中添加`--dns 10.3.9.4`，不使用Podman启动的DNS服务器，暂时绕过了这个问题。

不过这个问题并没有从本质上得到解决，甚至都还不知道背后的具体问题是什么，感觉会在后面攻击我，特此记录。
