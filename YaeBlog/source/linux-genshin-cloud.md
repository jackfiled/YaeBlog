---
title: 解决云原神无法在Linux中浏览器下运行的问题
tags:
  - 生活小妙招
  - 原神
date: 2023-10-09 23:56:34
---


# 解决云原神无法在Linux中浏览器下运行的问题

本文为转载`bilibili`用户[@SocialismTeen](https://space.bilibili.com/33027704)在他的[专栏](https://www.bilibili.com/read/cv26576757)中给出的解决办法。

<!--more-->

## 问题

在`Linux`平台上使用`Chromium`系列内核的浏览器打开[云原神](https://ys.mihoyo.com/cloud/#/)会发生鼠标无法控制视角的问题。

## 解决

根据上面提到那位同志的研究，该问题是由于云原神在获得鼠标移动时使用的`API`: `Pointer Lock API`。在**其他**平台上该`API`支持名为`unadjustedMovement`的参数以关闭鼠标加速获得更好的体验，但是在`Linux`平台上并不支持该参数，因此程序无法正确获得到鼠标指针的位置。

该同志给出的解决办法为使用钩子函数消除调用该`API`时的参数，使用的代码如下：

```javascript
const origin = HTMLElement.prototype.requestPointerLock
HTMLElement.prototype.requestPointerLock = function () {
  return origin.call(this)
} 
```

为了获得良好的游戏体验，可以使用[油猴插件](https://www.tampermonkey.net/)在进入网页时自动运行上述脚本：

```javascript
// ==UserScript==
// @name         Genshin Cloud
// @namespace    http://tampermonkey.net/
// @version      0.1
// @description  fix a Genshin Impact cloud game bug
// @match        https://ys.mihoyo.com/cloud/*
// @grant        none
// ==/UserScript==

(function () {
  'use strict';

  const origin = HTMLElement.prototype.requestPointerLock
  HTMLElement.prototype.requestPointerLock = function () {
    return origin.call(this)
  }
})();
```



