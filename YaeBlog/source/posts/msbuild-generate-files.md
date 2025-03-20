---
title: 如何在MSBuild中复制生成的文件到发布目录中
date: 2025-03-20T22:33:21.6955385+08:00
tags:
- 技术笔记
- dotnet
---

 如何使用`MSBuild`将构建过程中生成文件复制到生成目录中？

<!--more-->

### 遇到的问题

最近在尝试在`blazor`项目中使用`tailwindcss`作为`css`工具类的提供工具，而不是使用老旧的`bootstrap`框架，不过使用`tailwindcss`需要在项目构建时使用`tailwindcss`工具扫描文件中使用到的`css`属性并生成最终的`css`文件，这就带来了在构建时运行`tailwindcss`生成并复制到输出目录的需求。

由于我是使用`pnpm`作为前端管理工具，我在项目的`csproj`文件中添加了下面的`Target`来生成文件：

```xml
<Target Name="EnsurePnpmInstalled" BeforeTargets="BeforeBuild">
    <Message Importance="low" Text="Ensure pnpm is installed..."/>
    <Exec Command="pnpm --version" ContinueOnError="true">
      <Output TaskParameter="ExitCode" PropertyName="ErrorCode"/>
    </Exec>

    <Error Condition="$(ErrorCode) != 0" Text="Pnpm is not installed which is required for build."/>

    <Message Importance="normal" Text="Installing pakages using pnpm..."/>
    <Exec Command="pnpm install"/>
  </Target>

  <Target Name="TailwindGenerate" AfterTargets="EnsurePnpmInstalled" BeforeTargets="BeforeBuild">
    <Message Importance="normal" Text="Generate css files using tailwind..."/>
     <Exec Command="pnpm tailwindcss -i wwwroot/tailwind.css -o wwwroot/tailwind.g.css"/>
  </Target>
```

这套生成逻辑在本地工作良好，但是却在CI上运行时出现了问题：`CI`上打包的`Docker`镜像中没有`tailwind.g.css`文件，导致最终部署的站点丢失了所有的格式。

### 产生问题的原因

经过反复实验，我发现只有在构建之前`wwwroot`目录中已经存在`tailwind.g.css`文件的情况下，`MSBuild`才会将生成的文件复制到最终的输出目录中。但是在`CI`环境下，因为使用`.gitignore`没有将`*.g.css`文件添加到代码管理，因此`CI`运行构建之前没有该文件，因此构建的结果中也没有该文件。

仔细研究`MSBuild`的文档和网络上的[分享](https://gist.github.com/BenVillalobos/c671baa1e32127f4ab582a5abd66b005)，我意识到这是由于`MSBuild`的构建流程导致的，MSBuild`的构建流程分成两个大的阶段：

- 评估阶段（Evaluation Phase）

  在这个阶段，`MSBuild`将会运行读取所有的配置文件，创建需要的属性，展开所有的`glob`，建立好整个构建流程。

- 执行阶段（Execution Phase）

  在这个阶段，`MSBuild`将按照上一阶段执行的属性执行实际的构建指令。

这两个阶段的划分就导致在生成阶段才生成的文件不会被包含在复制文件的指令中，因此他们不会被拷贝到最终的输出目录中。

>这和`cmake`的构建过程很像，首先调用`cmake`生成一些构建指令，在调用实际的构建指令构建二进制文件。

因此这类问题的推荐解决办法是手动将这些文件添加到构建流程中，即在`BeforeBuild`目标调用之前使用`Content`和`None`等项。

### 解决问题

总结上述的解决问题方法，我在上面的构建流程中添加了如下的`None`项：

```xml
<Target Name="EnsurePnpmInstalled" BeforeTargets="BeforeBuild">
    <Message Importance="low" Text="Ensure pnpm is installed..."/>
    <Exec Command="pnpm --version" ContinueOnError="true">
      <Output TaskParameter="ExitCode" PropertyName="ErrorCode"/>
    </Exec>

    <Error Condition="$(ErrorCode) != 0" Text="Pnpm is not installed which is required for build."/>

    <Message Importance="normal" Text="Installing pakages using pnpm..."/>
    <Exec Command="pnpm install"/>
  </Target>

  <Target Name="TailwindGenerate" AfterTargets="EnsurePnpmInstalled" BeforeTargets="BeforeBuild">
    <Message Importance="normal" Text="Generate css files using tailwind..."/>
    <Exec Command="pnpm tailwindcss -i wwwroot/tailwind.css -o wwwroot/tailwind.g.css"/>

    <!-- Make sure generated file will be copied to output directory-->
    <ItemGroup>
      <Content Include="wwwroot/tailwind.g.css" Visible="false" CopyToOutputDirectory="PreserveNewest"/>
    </ItemGroup>
  </Target>
```

在运行构建之后，在最终的`publish`文件夹的`wwroot`文件夹中就可以找到`tailwind.g.css`文件。

不过我还想进行一点优化，`MSBuild`文档中建议将自动生成的文件放在`IntermediateOutputPath`，也就是`obj`文件加中，因此这里尝试将`tailwind.g.css`文件生成到`IntermediateOuputPath`中，优化之后的`Target`项长这个样子：

```xml
  <Target Name="EnsurePnpmInstalled" BeforeTargets="BeforeBuild">
    <Message Importance="low" Text="Ensure pnpm is installed..."/>
    <Exec Command="pnpm --version" ContinueOnError="true">
      <Output TaskParameter="ExitCode" PropertyName="ErrorCode"/>
    </Exec>

    <Error Condition="$(ErrorCode) != 0" Text="Pnpm is not installed which is required for build."/>

    <Message Importance="normal" Text="Installing pakages using pnpm..."/>
    <Exec Command="pnpm install"/>
  </Target>

  <Target Name="TailwindGenerate" AfterTargets="EnsurePnpmInstalled" BeforeTargets="BeforeBuild">
    <Message Importance="normal" Text="Generate css files using tailwind..."/>
    <Exec Command="pnpm tailwindcss -i wwwroot/tailwind.css -o $(IntermediateOutputPath)tailwind.g.css"/>

    <!-- Make sure generated file will be copied to output directory-->
    <ItemGroup>
      <Content Include="$(IntermediateOutputPath)tailwind.g.css" Visible="false" TargetPath="wwwroot/tailwind.g.css"/>
    </ItemGroup>
  </Target>
```

经过测试，这套生成逻辑在`blazor`类库环境下也可以正常运行，类库的文件会被正确地生成到`wwwroot/_content/<ProjectName>/`文件夹下面。

