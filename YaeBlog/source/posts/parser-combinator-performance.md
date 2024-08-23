---
title: 解析器组合子和LR(1)分析方法的性能比较
tags:
  - 编译原理
  - 技术笔记
date: 2024-08-19 14:31:00
---

在使用解析器组合子编写编译器前端时，其与LR分析方法之间的性能差距是开发人员关心的重点问题之一。

<!--more-->

## 背景

解析器组合子是一种使用函数式编程思想编写编译器前端中的词法分析器和语法分析器的编程范式。这种方式通过利用函数式编程语言将词法和语法直接嵌入在解析器代码中，大大提高了编写分析器的效率，降低了编写的难度。

但是这种以函数的嵌套和递归为核心的编程范式却带来了分析器运行效率下降的问题，在运行效率和编写效率之前的取舍成为了编译器研究人员十分关心的话题。因此，为了实际对比两种编写方式的运行效率，并为解析器组合子库的后续优化提升指明方向，本文中通过使用两种方式编写针对Pascal-S语言的解析器，设计了多组Benchmark对比两个解析器的运行效率。

### Pascal-S语言

本次测试使用的语言为Pascal-S语言，这是Pascal语言的一个简化版本，语言的具体语法如下所示。

```haskell
ProgramStart -> ProgramStruct
ProgramStruct -> ProgramHead ; ProgramBody .
ProgramHead -> program id (IdList) | program id
ProgramBody -> ConstDeclarations
               VarDeclarations
               SubprogramDeclarations
               CompoundStatement
IdList -> , id IdList | : Type
ConstDeclarations -> $\epsilon$ | const ConstDeclaration ;
ConstDeclaration -> id = ConstValue | ConstDeclaration ; id = ConstValue
ConstValue -> +num | -num | num | 'letter' | true | false
VarDeclarations ->  | var VarDeclaration ;
VarDeclaration -> id IdList | VarDeclaration ; id IdList
Type -> BasicType | array [ Period ] of BasicType
BasicType -> integer | real | boolean | char
Period -> digits .. digits | Period , digits .. digits
SubprogramDeclarations -> $\epsilon$ | SubprogramDeclarations Subprogram ;
Subprogram -> SubprogramHead ; SubprogramBody
SubprogramHead -> procedure id FormalParameter 
                | function id FormalParameter : BasicType
FormalParameter -> $\epsilon$ | () | ( ParameterList )
ParameterList -> Parameter | ParameterList ; Parameter
Parameter -> VarParameter | ValueParameter
VarParameter -> var ValueParameter
ValueParameter -> id IdList
SubprogramBody -> ConstDeclarations
                  VarDeclarations
                  CompoundStatement
CompoundStatement -> begin StatementList end
StatementList -> Statement | StatementList ; Statement
Statement -> $\epsilon$
             | Variable assignOp Expression
             | ProcedureCall
             | CompoundStatement
             | if Expression then Statement ElsePart
             | for id assignOp Expression to Expression do Statement
             | while Expression do Statement
Variable -> id IdVarPart
IdVarPart -> $\epsilon$ | [ ExpressionList ]
ProcedureCall -> id | id () | id ( ExpressionList )
ElsePart -> $\epsilon$ | else Statement
ExpressionList -> Expression | ExpressionList , Expression
Expression -> SimpleExpression | SimpleExpression RelationOperator SimpleExpression
SimpleExpression -> Term | SimpleExpression AddOperator Term
Term -> Factor | Term MultiplyOperator Factor
Factor -> num 
          | true
          | false
          | Variable
          | ( Expression )
          | id () 
          | id ( ExpressionList )
          | not Factor
          | - Factor
          | + Factor
AddOperator -> + | - | or
MultiplyOperator -> * | / | div | mod | and
RelationOperator -> = | <> | < | <= | > | >=
```

### 分析器的实现

本次测试中涉及到两个分析器的实现。两个分析器都是使用C#语言编写，运行在.NET 8.0.7平台上，使用X64 RyuJIT AVX2配置进行运行。

第一个分析器称为`Canon`。词法分析器使用朴素的自动机实现，没有使用任何的高端技术。将所有的词法规则写在一个巨大的自动机中，代码长度600行，是一个在可读性上堪称地狱，但是在效率上做到极致的方式。语法分析器虽然使用LR(1)分析方式，但是没有使用任何成熟的语法分析工具，而是自行实现的LR(1)语法分析器，在构建LR(1)分析表之后将分析表生成为C#代码编译到最终的程序集中，以求获得和传统语法分析器工具近似的运行效率。本分析器具体的实现可以在[jackfiled/Canon](https://git.rrricardo.top/post-guard/Canon)获得。

第二个分析器称为`CanonSharp`，词法分析器和语法分析都是使用自行实现的解析器组合子实现，解析器组合子的实现可以参考我的[上一篇文章](https://rrricardo.top/blog/essays/parser-combinator)。词法分析器的解析器以字符作为输入，解析之后输出词法令牌。语法分析器的解析器以词法令牌作为输入，解析之后输出抽象语法树。这个分析器的代码可以在[jackfiled/CanonSharp](https://git.rrricardo.top/jackfiled/CanonSharp)获得。需要说明的是，两个分析器最终的输出有一定的不同，`Canon`分析器的输出是完整的语法树，在语法定义中的每一个非终结节点都在语法树上存在，`CanonSharp`分析器的输出是抽象语法树，语法定义中的一些冗余的节点在抽象语法树中均不存在。这种实现上的差异可能会导致两个分析器在占用内存上存在一定的差异，但是并不会在运行效率上造成明显的影响。

## 基准测试程序的编写

测试程序使用[BenchmarkDotnet](https://github.com/dotnet/BenchmarkDotNet)作为驱动程序。这是一个.NET平台上简单易用的基准测试框架，可以按照编写单元测试的方式编写基准测试程序。本文中使用的测试代码如下所示：

```csharp
public class GrammarParserBenchmark
{
    private readonly List<string> _inputFiles = [];

    // CanonSharp
    private readonly LexicalScanner _scanner = new();
    private readonly GrammarParser _parser = new();

    // Canon
    private readonly IGrammarParser _grammarParser = GeneratedGrammarParser.Instance;

    public GrammarParserBenchmark()
    {
        // 读取文件系统中的程序文件
        _inputFiles.AddRange(ReadOpenSet());
    }

    [Benchmark]
    [ArgumentsSource(nameof(InputFiles))]
    public Pascal.SyntaxTree.Program CanonSharpParse(int index)
    {
        IEnumerable<LexicalToken> tokens = _scanner.Tokenize(new StringReadState(_inputFiles[index]));
        return _parser.Parse(tokens);
    }

    [Benchmark]
    [ArgumentsSource(nameof(InputFiles))]
    public ProgramStruct CanonParse(int index)
    {
        Lexer lexer = new();
        IEnumerable<SemanticToken> tokens = lexer.Tokenize(new StringSourceReader(_inputFiles[index]));

        return _grammarParser.Analyse(tokens);
    }

    public IEnumerable<object> InputFiles()
    {
        for (int i = 0; i < _inputFiles.Count; i++)
        {
            yield return i;
        }
    }
}

```

在编写测试程序中需要说明的是：在创建类的过程中，`CanonSharp`解析器的创建并没有计算在测试程序的运行时间中，但是`Canon`解析器的创建被计算在了测试程序的运行时间中。这是因为`Canon`解析器的词法分析器并不是一个无状态的解析器，不能重复的使用，只能在每次使用之前重新创建。

## 测试结果

在对原始数据进行数据处理之后我们绘制了如下的图。图中的横轴是输入测试文件的编号，图中的纵轴是`CanonSharp`解析器和`Canon`解析器运行时间的比值。从图中可以看出`CanonSharp`解析器的运行时间大约是`Canon`解析器运行时间的65到75倍，在某些极端的情况下可能会达到90倍。

![image-20240819140523087](./parser-combinator-performance/image-20240819140523087.png)

## 结论

从相对的效率上说，使用解析器组合子编写的分析器在运行效率上大约要比使用表驱动的分析器慢上两个数量级；从绝对的运行时间上看，解析器组合子在面向一般长度的输入代码时运行的时间大约在毫秒量级。因此从Amdahl定律的角度和实际用户体验的角度出发，使用解析器组合子编写分析器不会导致最终的编译器在运行效率上的实际下降。同时，使用编译器组合子可以在保证代码高度可读性的条件下敏捷开发针对不同语言的编译器，在编写以教学和实验为目的的编译器中有着非常巨大的应用空间。

虽然在运行效率上，使用解析器组合子不会导致过多的担忧，但是从解析器组合子库的设计者角度出发，如何尽可能的提高解析器组合子库运行的效率并避免在运行时发生栈溢出错误都是重要的研究课题。

## 原始数据

| Method              | index  |             Mean |          Error |         StdDev |
| ------------------- | ------ | ---------------: | -------------: | -------------: |
| **CanonSharpParse** | **0**  |   **353.627 μs** |  **1.8258 μs** |  **1.7078 μs** |
| CanonParse          | 0      |         4.917 μs |      0.0130 μs |      0.0115 μs |
| **CanonSharpParse** | **1**  |   **653.161 μs** |  **1.7194 μs** |  **1.6084 μs** |
| CanonParse          | 1      |         8.798 μs |      0.0280 μs |      0.0234 μs |
| **CanonSharpParse** | **2**  |   **630.299 μs** |  **1.4253 μs** |  **1.1902 μs** |
| CanonParse          | 2      |         8.724 μs |      0.0179 μs |      0.0149 μs |
| **CanonSharpParse** | **3**  |   **387.579 μs** |  **1.5613 μs** |  **1.3038 μs** |
| CanonParse          | 3      |         5.649 μs |      0.0098 μs |      0.0082 μs |
| **CanonSharpParse** | **4**  |   **356.240 μs** |  **2.3247 μs** |  **2.1745 μs** |
| CanonParse          | 4      |         4.716 μs |      0.0062 μs |      0.0055 μs |
| CanonParse          | 5      |         4.980 μs |      0.0154 μs |      0.0136 μs |
| **CanonSharpParse** | **6**  |   **979.392 μs** |  **4.0016 μs** |  **3.5473 μs** |
| CanonParse          | 6      |        13.479 μs |      0.0428 μs |      0.0379 μs |
| **CanonSharpParse** | **7**  |   **600.507 μs** |  **2.1920 μs** |  **1.9431 μs** |
| CanonParse          | 7      |         8.072 μs |      0.0134 μs |      0.0125 μs |
| **CanonSharpParse** | **8**  |   **524.578 μs** |  **1.6822 μs** |  **1.4047 μs** |
| CanonParse          | 8      |         7.695 μs |      0.0254 μs |      0.0225 μs |
| **CanonSharpParse** | **9**  |   **315.395 μs** |  **0.2694 μs** |  **0.2250 μs** |
| CanonParse          | 9      |         4.780 μs |      0.0156 μs |      0.0138 μs |
| **CanonSharpParse** | **10** |   **510.408 μs** |  **1.3935 μs** |  **1.2353 μs** |
| CanonParse          | 10     |         6.968 μs |      0.0203 μs |      0.0190 μs |
| **CanonSharpParse** | **11** |   **444.388 μs** |  **1.0900 μs** |  **0.9663 μs** |
| CanonParse          | 11     |         5.952 μs |      0.0116 μs |      0.0091 μs |
| **CanonSharpParse** | **12** |   **523.964 μs** |  **4.2651 μs** |  **3.9896 μs** |
| CanonParse          | 12     |         7.391 μs |      0.0106 μs |      0.0100 μs |
| **CanonSharpParse** | **13** |   **290.775 μs** |  **0.9223 μs** |  **0.8176 μs** |
| CanonParse          | 13     |         4.373 μs |      0.0096 μs |      0.0090 μs |
| **CanonSharpParse** | **14** |   **669.914 μs** |  **5.7419 μs** |  **5.3710 μs** |
| CanonParse          | 14     |         9.321 μs |      0.0195 μs |      0.0173 μs |
| **CanonSharpParse** | **15** |   **329.593 μs** |  **0.9726 μs** |  **0.9098 μs** |
| CanonParse          | 15     |         4.759 μs |      0.0124 μs |      0.0103 μs |
| **CanonSharpParse** | **16** |   **389.419 μs** |  **1.2592 μs** |  **1.1778 μs** |
| CanonParse          | 16     |         5.889 μs |      0.0188 μs |      0.0167 μs |
| **CanonSharpParse** | **17** |   **390.669 μs** |  **0.8737 μs** |  **0.7295 μs** |
| CanonParse          | 17     |         5.677 μs |      0.0114 μs |      0.0106 μs |
| **CanonSharpParse** | **18** | **1,783.017 μs** | **10.1009 μs** |  **9.4484 μs** |
| CanonParse          | 18     |        18.990 μs |      0.0787 μs |      0.0736 μs |
| **CanonSharpParse** | **19** | **1,832.231 μs** |  **6.9808 μs** |  **6.5299 μs** |
| CanonParse          | 19     |        19.032 μs |      0.0968 μs |      0.0906 μs |
| **CanonSharpParse** | **20** | **1,397.956 μs** |  **7.1686 μs** |  **6.7055 μs** |
| CanonParse          | 20     |        19.161 μs |      0.0301 μs |      0.0282 μs |
| **CanonSharpParse** | **21** | **1,920.760 μs** |  **5.6297 μs** |  **5.2661 μs** |
| CanonParse          | 21     |        24.183 μs |      0.0531 μs |      0.0497 μs |
| **CanonSharpParse** | **22** | **1,937.031 μs** |  **7.3762 μs** |  **6.5388 μs** |
| CanonParse          | 22     |        24.551 μs |      0.0411 μs |      0.0385 μs |
| **CanonSharpParse** | **23** |   **827.700 μs** |  **4.8717 μs** |  **4.5570 μs** |
| CanonParse          | 23     |        12.082 μs |      0.0233 μs |      0.0194 μs |
| **CanonSharpParse** | **24** |   **901.852 μs** |  **4.4028 μs** |  **3.6765 μs** |
| CanonParse          | 24     |        12.923 μs |      0.0282 μs |      0.0250 μs |
| **CanonSharpParse** | **25** |   **664.637 μs** |  **1.8827 μs** |  **1.4699 μs** |
| CanonParse          | 25     |        10.172 μs |      0.0288 μs |      0.0240 μs |
| CanonParse          | 26     |        30.176 μs |      0.0517 μs |      0.0484 μs |
| **CanonSharpParse** | **27** | **2,655.310 μs** | **11.8520 μs** | **11.0864 μs** |
| CanonParse          | 27     |        33.174 μs |      0.1600 μs |      0.1497 μs |
| **CanonSharpParse** | **28** |   **355.309 μs** |  **2.3985 μs** |  **2.2436 μs** |
| CanonParse          | 28     |         5.229 μs |      0.0153 μs |      0.0143 μs |
| CanonParse          | 29     |        19.519 μs |      0.0676 μs |      0.0599 μs |
| CanonParse          | 30     |        15.602 μs |      0.0260 μs |      0.0217 μs |
| **CanonSharpParse** | **31** |   **419.458 μs** |  **1.0384 μs** |  **0.9714 μs** |
| CanonParse          | 31     |         5.272 μs |      0.0073 μs |      0.0061 μs |
| **CanonSharpParse** | **32** | **2,148.625 μs** | **10.1403 μs** |  **9.4852 μs** |
| CanonParse          | 32     |        30.954 μs |      0.0335 μs |      0.0280 μs |
| CanonParse          | 33     |        38.046 μs |      0.0833 μs |      0.0738 μs |
| CanonParse          | 34     |        58.688 μs |      0.2072 μs |      0.1938 μs |
| CanonParse          | 35     |       132.349 μs |      0.3033 μs |      0.2689 μs |
| **CanonSharpParse** | **36** | **2,251.311 μs** | **11.0098 μs** |  **9.7599 μs** |
| CanonParse          | 36     |        29.074 μs |      0.1068 μs |      0.0999 μs |
| CanonParse          | 37     |        59.152 μs |      0.1773 μs |      0.1659 μs |
| CanonParse          | 38     |        58.291 μs |      0.1733 μs |      0.1621 μs |
| CanonParse          | 39     |        78.906 μs |      0.2064 μs |      0.1830 μs |
| CanonParse          | 40     |       141.906 μs |      0.4749 μs |      0.4210 μs |
| CanonParse          | 41     |       142.309 μs |      0.4521 μs |      0.4229 μs |
| CanonParse          | 42     |       185.218 μs |      0.6247 μs |      0.5538 μs |
| CanonParse          | 43     |        50.808 μs |      0.2504 μs |      0.2342 μs |
| **CanonSharpParse** | **44** | **1,461.460 μs** |  **4.7521 μs** |  **4.2126 μs** |
| CanonParse          | 44     |        19.431 μs |      0.0391 μs |      0.0347 μs |
| CanonParse          | 45     |       103.431 μs |      0.4603 μs |      0.4306 μs |
| CanonParse          | 46     |     1,031.768 μs |     20.4417 μs |     29.3168 μs |
| CanonParse          | 47     |        38.072 μs |      0.1364 μs |      0.1276 μs |
| CanonParse          | 48     |        80.956 μs |      0.1958 μs |      0.1736 μs |
| CanonParse          | 49     |       152.900 μs |      0.8712 μs |      0.7723 μs |
| CanonParse          | 50     |        49.622 μs |      0.1501 μs |      0.1404 μs |
| **CanonSharpParse** | **51** | **1,150.554 μs** |  **4.0367 μs** |  **3.5784 μs** |
| CanonParse          | 51     |        16.331 μs |      0.0426 μs |      0.0378 μs |
| CanonParse          | 52     |        87.271 μs |      0.1576 μs |      0.1475 μs |
| CanonParse          | 53     |        25.740 μs |      0.0657 μs |      0.0583 μs |
| CanonParse          | 54     |        94.270 μs |      0.1679 μs |      0.1571 μs |
| CanonParse          | 55     |        54.342 μs |      0.2107 μs |      0.1971 μs |
| CanonParse          | 56     |    53,845.477 μs |  1,047.6308 μs |  1,661.6535 μs |
| CanonParse          | 57     |       453.672 μs |      1.2739 μs |      1.1293 μs |
| CanonParse          | 58     |        83.867 μs |      0.2897 μs |      0.2709 μs |
| CanonParse          | 59     |       190.913 μs |      0.6662 μs |      0.5906 μs |
| CanonParse          | 60     |       155.175 μs |      0.3931 μs |      0.3677 μs |
| CanonParse          | 61     |       113.926 μs |      0.8090 μs |      0.7567 μs |
| CanonParse          | 62     |       368.975 μs |      0.9096 μs |      0.8064 μs |
| CanonParse          | 63     |       142.392 μs |      0.3123 μs |      0.2922 μs |
| CanonParse          | 64     |       168.598 μs |      0.3496 μs |      0.3099 μs |
| CanonParse          | 65     |       102.763 μs |      0.3745 μs |      0.3503 μs |
| CanonParse          | 66     |        73.413 μs |      0.4360 μs |      0.3865 μs |
| CanonParse          | 67     |        77.734 μs |      0.2080 μs |      0.1945 μs |
| CanonParse          | 68     |        91.893 μs |      0.3471 μs |      0.3077 μs |
| CanonParse          | 69     |        76.277 μs |      0.1656 μs |      0.1468 μs |
| CanonParse          | 70     |         2.991 μs |      0.0050 μs |      0.0044 μs |
| **CanonSharpParse** | **71** | **1,225.389 μs** |  **8.1085 μs** |  **7.1880 μs** |
| CanonParse          | 71     |        14.829 μs |      0.0372 μs |      0.0330 μs |
| CanonParse          | 72     |        76.865 μs |      0.1630 μs |      0.1445 μs |
| **CanonSharpParse** | **73** |   **838.646 μs** |  **5.7775 μs** |  **5.4043 μs** |
| CanonParse          | 73     |        11.358 μs |      0.0200 μs |      0.0187 μs |
| **CanonSharpParse** | **74** |   **850.399 μs** |  **4.4444 μs** |  **4.1573 μs** |
| CanonParse          | 74     |        10.957 μs |      0.0126 μs |      0.0118 μs |
| CanonParse          | 75     |       369.698 μs |      0.5347 μs |      0.4740 μs |
| CanonParse          | 76     |       168.135 μs |      0.3753 μs |      0.3134 μs |
| CanonParse          | 78     |       105.286 μs |      0.4302 μs |      0.3813 μs |
| CanonParse          | 80     |        96.353 μs |      0.3182 μs |      0.2977 μs |
| CanonParse          | 81     |        89.891 μs |      0.2942 μs |      0.2608 μs |
| CanonParse          | 82     |       110.896 μs |      0.6884 μs |      0.6440 μs |
| CanonParse          | 83     |       135.258 μs |      0.1460 μs |      0.1366 μs |
| CanonParse          | 84     |        66.339 μs |      0.1386 μs |      0.1229 μs |
| CanonParse          | 86     |       300.468 μs |      0.3804 μs |      0.3177 μs |
| CanonParse          | 87     |       299.640 μs |      0.6466 μs |      0.6048 μs |
| CanonParse          | 88     |        80.081 μs |      0.1701 μs |      0.1508 μs |
| CanonParse          | 89     |     2,762.159 μs |     33.4829 μs |     31.3199 μs |
| CanonParse          | 90     |       751.693 μs |      3.4209 μs |      3.1999 μs |
| CanonParse          | 91     |        56.290 μs |      0.3105 μs |      0.2752 μs |
| CanonParse          | 93     |       258.514 μs |      0.6600 μs |      0.5851 μs |

> 原始数据中缺失的数据为对应测试运行失败。