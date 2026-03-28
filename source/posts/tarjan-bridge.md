---
title: Tarjan算法与实现
date: 2026-03-28T21:53:45.1681856+08:00
updateTime: 2026-03-28T21:53:45.1733146+08:00
tags:
- 技术笔记
- 算法
---


Tarjan算法是一类用于无向图中割边和割点的算法。

<!--more-->

## Tarjan算法

Tarjan算法是图论中非常常用的一种算法，基于深度优先搜索（DFS），基础版本的Tarjan算法用于求解无向图中的割点和桥。基于此可以求解图论中的一系列问题，例如无向图的双连通分量、有向图的强连通分量等问题。

Tarjan算法由计算机科学家Robert Tarjan在1972年于论文*Depth-First Search And Linear Graph Algorithms*中提出。Robert Tarjan是一位著名的计算机科学家，解决了图论中的一系列重大问题，同时也是斐波那契堆（Fibonacci Heap）和伸展树（Splay Tree）的开发者之一。他于1986年获得了图灵奖，目前仍在普林斯顿大学担任教职。

## 无向图的割点与桥

如果一个图中所有的边都是无向边，则称之为无向图。

### 割点

如果从无向图中删除节点x和所有与节点x关联的边之后，图将会被分成两个或者两个以上不相连的子图，那么节点x就是这个图的割点。下图中标注为红色的点就是该图的割点。

![image-20260328213522542](./tarjan-bridge/image-20260328213522542.webp)

### 桥

如果从图中删除边e之后，图将分裂为两个不相连的子图，那么就称e是图的桥，或者割边。

![image-20260328213554309](./tarjan-bridge/image-20260328213554309.webp)

图中被标注为红色的边就是该图的桥。

## 求解图中的割点

Tarjan算法中为了求解桥和割点，首先定义了如下几个概念。

### 时间戳

时间戳用来标记图中每个节点在进行深度优先搜索的过程中被访问的时间顺序，这个概念起始也就是在遍历的时候给每个节点编号。

这个编号用`search_number[x]`来表示，其中的x是节点。

### 搜索树

在图中，如果从一个节点x出发进行深度优先的搜索，在搜索的过程中每个节点只能访问一次，所有被访问的节点可以构成一棵树，这棵树就被称为无向连通图的搜索树。

### 追溯值

追溯值的定义和计算是Tarjan算法的核心。

追溯值被定义为，从当前节点x作为搜索树的根节点出现，能够访问到的所有节点中，时间戳的最小值，被记为`low[x]`。

定义中主要的限定条件是“能够访问到的所有节点”，主要考虑的是如下两种访问方式：

- 这个节点在以x为根的搜索树上
- 通过一条不属于搜索树的边，可以到达搜索树的节点。

例如上图的例子中，考虑直接从节点1出发开始深度优先的遍历，此时使用的遍历顺序是节点1、节点2、节点3、节点4、节点5。

![image-20260328213641303](./tarjan-bridge/image-20260328213641303.webp)

当遍历到节点5时，考虑以节点5为根的搜索树（可以认为此时的搜索树中只有节点5一个节点），可以发现有两条不属于搜索树的边(2, 5)和(1, 5)，使得节点1和节点2成为了上述“可以访问到的节点”，因此将节点5的追溯值更新为1。

![image-20260328213702466](./tarjan-bridge/image-20260328213702466.webp)

此时，算法按照深度优先搜索的顺序开始回溯，在回溯的过程中逐步更新当前节点的追溯值，此时就是按照上面“可以访问的所有节点”中的搜索树情形工作了。例如当回溯到节点3时，可以认为存在以节点3为根节点的搜索树{3, 4, 5}，其中追溯值的最小值为1, 将节点3的追溯值更新为1。

![image-20260328213720719](./tarjan-bridge/image-20260328213720719.webp)

### 桥的判定法则

在无向图中，对于一条边`e = (u ,v)`，如果满足`search_number[u] < low[v]`，那么该边就是图中的一个桥。

这个条件所蕴含的意思是，节点u被访问的时间，要小于（优先于）以下所有这些节点被访问的时间：

- 以节点v为根节点的搜索树中的所有节点
- 通过一条非搜索树上的边，能否到达搜索树的所有节点。

## 实现

以下以[1192. 查找集群内的关键连接 - 力扣（LeetCode）](https://leetcode.cn/problems/critical-connections-in-a-network/description/)为例，给出Tarjan算法的实现。

```cpp
namespace {
/// Graph structure.
/// Store the graph using linked forwarded stars.
/// The linked forwarded stars store the graph using linked list.
///
/// To accelerate the loading and storing, use array to simulate the linked
/// list.
struct Graph {
  explicit Graph(const size_t nodeCount, const size_t edgeCount) {
    // The edgeID starts from 2, as 0 is used as null value.
    // And to find the reverse edge by i ^ 1, so 0 and 1 are both skipped.
    endNodes = vector<size_t>(edgeCount + 2, 0);
    nextEdges = vector<size_t>(edgeCount + 2, 0);
    headEdges = vector<size_t>(nodeCount, 0);
  }

  void addEdge(const size_t x, const size_t y) {
    endNodes[edgeID] = y;
    nextEdges[edgeID] = headEdges[x];
    headEdges[x] = edgeID;

    edgeID += 1;
  }

  vector<bool> calculateBridges() {
    // Initialize values used by tarjan algorithm.
    const auto nodeCount = headEdges.size();
    bridges = vector(endNodes.size(), false);
    nodeIDs = vector<size_t>(nodeCount, 0);
    lowValues = vector<size_t>(nodeCount, 0);

    number = 1;

    for (auto i = 0; i < nodeCount; i++) {
      if (nodeIDs[i] == 0) {
        tarjan(i, 0);
      }
    }

    return bridges;
  }

private:
  size_t edgeID = 2;

  /// Represent the end node of edge i.
  vector<size_t> endNodes;

  /// Represent the next edge of edge i.
  vector<size_t> nextEdges;

  /// Represent the head edge of node i.
  /// Also, head of simulated linked list.
  vector<size_t> headEdges;

  vector<bool> bridges;

  /// Represent timestamp of node i, 0 is used as unvisited.
  vector<size_t> nodeIDs;

  vector<size_t> lowValues;

  size_t number = 1;

  void tarjan(const size_t node, const size_t inEdge) {
    nodeIDs[node] = lowValues[node] = number;
    number += 1;

    for (auto i = headEdges[node]; i != 0; i = nextEdges[i]) {

      // If the next node is not visited.
      if (const auto end = endNodes[i]; nodeIDs[end] == 0) {
        tarjan(end, i);

        lowValues[node] = min(lowValues[node], lowValues[end]);
        if (lowValues[end] > nodeIDs[node]) {
          // Subtract 2 as the edge ID starts from 2.
          bridges[i - 2] = true;
          bridges[(i ^ 1) - 2] = true;
        }
      } else {
        // If edge i is visited and edge i is not the coming edge.
        if (i != (inEdge ^ 1)) {
          lowValues[node] = min(lowValues[node], nodeIDs[end]);
        }
      }
    }
  }
};
} // namespace

class Solution {
public:
  vector<vector<int>> criticalConnections(int n,
                                          vector<vector<int>> &connections) {
    // To store the undirected graph, double the edge count.
    auto g = Graph{static_cast<size_t>(n), connections.size() * 2};

    for (const auto &edge : connections) {
      g.addEdge(edge[0], edge[1]);
      g.addEdge(edge[1], edge[0]);
    }

    auto bridges = g.calculateBridges();
    vector<vector<int>> result;
    for (auto i = 0; i < bridges.size(); i = i + 2) {
      if (bridges[i]) {
        const auto &edge = connections[i / 2];
        result.push_back(edge);
      }
    }

    return result;
  }
};


```

### 链式前向星

在上面的实现中使用了一种较为高效的图存储方法-链式前向星（Linked Forward Star）。

链式前向星是一种类似于邻接表的图存储方法，提供了较为高效的边遍历方法。这种方法的本质上是按节点聚合的边链表，不过在上面的实现中使用了数组来存储链表的头结点和每个节点的下一个节点指针。

同时这种存储方法还提供了一种非常方便的反向边查找方法：考虑在存储无向图中的边时，将一条边成对的存储在数组中，由此针对任意一条边i，`i ^ 1`就是这条边的反向边。
