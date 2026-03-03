---
title: ASP.NET Core中的静态Web资产
date: 2026-01-04T16:36:36.5629759+08:00
tags:
- 技术笔记
- dotnet
- ASP.NET Core
---


Web服务器应该如何扫描与提供静态Web文件，尤其是在考虑到缓存、压缩的情况下，还需要正确的处理开发环境和部署环境之间的差异？让我们来看看ASP.NET Core是如何处理这个问题的。以及如何将通过其他工具（例如`pnpm`）生成的前端资产文件集成到ASP.NET Core中。

<!--more-->

### 引言——Blazor开发中的静态Web资源

Blazor是ASP.NET Core中~~新推出的~~Web应用程序开发框架，通过一系列精巧的设计实现了使用HTML和C#编写运行在浏览器中的应用程序，避免了使用丑陋的JavaScript。但是现代的前端开发生态几乎都建立在JavaScript之上，尤其是考虑在JavaScript在很长的一段时间都是浏览器唯一支持的脚本语言，在Blazor项目开发的过程中必然会遇到一些只能编写JavaScript才能解决的问题。同时，一系列的现代前端工具，例如[tailwindcss](https://tailwindcss.com/)，提供了更加优秀的前端开发体验，但是这些都基于NodeJS和NPM等前端工具。以上的前端生态引入了一个问题，如何在MSBuild驱动的Blazor应用构建流程中自然地运行前端工具链和ASP.NET Core支持的服务器部署生成的静态资源？

Blazor目前提供了一个入口简洁但是功能丰富的静态Web资源提供功能。在使用默认应用目录的情况下，项目将会提供一个`wwwroot`文件夹，这个文件夹中的内容将可以从`/`直接寻址。为了提升前端静态文件的使用体验，该文件夹下的文件将会经过一个复杂的管道：

- 在构建扫描到这些资产文件之后，MSBuild将会给静态文件加上内容指纹，以防止重复使用旧文件。资源还会被压缩，以减少资产交付的时间。
- 在运行时，所发现的资产文件将会作为终结点公开，并添加上合适的缓存头和内容类型头。在设置`ETtag`，`Last-Modified`和`Content-Type`头之后，浏览器将可以合理的缓存这些静态文件直到应用更新。

该静态文件功能还需要适应应用程序的部署状态：

- 在开发时，或者说运行`dotnet run`和`dotnet build`时，该功能需要将对应的静态文件终结点URL映射到磁盘上存储的实际静态文件上，就像它们实际上就在`wwwroot`文件夹中一样。考虑到实际上开发过程中会用到Blazor内部的资产文件`blazor.web.js`，引用项目中的资产文件等等，这实际上一个相当复杂的检测-映射流程。
- 在发布时，或者说运行`dotnet publish`时，该功能需要收集所有需要的静态文件并复制到最终发布文件夹的`wwwroot`文件夹之下。

### Microsoft.AspNetCore.ClientAssets

在默认的应用模板下，如果需要使用其他的现代前端工具生成静态资产文件，最简单的方法就是手动或者编写MSBuild目标（Target）生成资产文件并放在`wwwroot`文件夹中。但是这个方法存在着如下几个问题：

- 开发者需要编写 MSBuild 目标（targets）来调用他们的工具。
- 开发者通常没有在构建过程的恰当时机执行其自定义目标。
- 开发者将工具生成的输出文件放入应用的 wwwroot 文件夹中，这会导致这些文件在下一次构建时被误认为是输入文件。
- 开发者没有为这些工具正确声明输入和输出依赖，导致即使输出文件已是最新，工具仍会重复运行，从而增加构建耗时。

面对这些问题，M$提供了一个Alpha状态的库`Microsoft.AspNetCore.ClientAssets`来解决这个问题。不幸的是，这个库已经因为年久失修（上一次[更新](https://github.com/aspnet/AspLabs/pull/572)是在3年前，引入对于.NET 7的支持），在.NET 9引入新的静态资产部署管线之后，使用会直接报错了。

^^ 相关的Issues链接：[#38445](https://github.com/dotnet/aspnetcore/issues/38445)，[#62925](https://github.com/dotnet/aspnetcore/issues/62925)

为了良好地解决如上的问题，我们需要首先了解一下ASP.NET Core中静态资产文件的构建和部署过程。

### StaticWebAssetsSdk

在.NET中，构建静态资产文件的相关代码在[dotnet/sdk](https://github.com/dotnet/sdk)仓库中，称作`StaticWebAssetsSdk`。

静态 Web 资源会接管应用程序 wwwroot 文件夹中的内容项，并全面管理这些内容。在开发过程中，系统会生成一个 JSON 清单（manifest），其中包含以下信息：

- 版本号（version number）：标识清单格式的版本。

- 清单内容的哈希值（hash）：用于判断清单内容是否发生变化。

- 库的包 ID（library package id）：用于区分当前项目与其他项目所提供的资源。

- 库的资源基础路径（asset base path）：在将其他库的路径应用“发现模式”时，用于确定要添加的基础路径。

- 清单模式（manifest mode）：定义来自特定项目的资源在构建和发布时应如何处理。

- 相关项目清单及其哈希值的列表：用于判断自清单生成以来，项目引用是否发生变化，或是否有清单被更新。

- “发现模式”（discovery patterns）列表：用于在清单构建完成后，有选择性地在运行时提供某些资源。例如，可以使用如下模式：

  ```json
  { "Path": "<Project>/Pages", "BasePath": "_content/Something", "Pattern": "**/*.js" }
  ```

  表示仅提供该目录下扩展名为`js`的文件。如果有人添加了图片或其他文件，它们将不会被提供。（这一点很重要，因为这些文件并不符合任何资源规则，也不会包含在发布输出目录中。）

- 构建/发布过程中生成的静态 Web 资源列表。

系统会生成两套清单：**构建清单（build manifest）** 和 **发布清单（publish manifest）**：

- **构建清单**在构建过程中生成，用于开发阶段，使资源表现得如同它们属于应用程序本身。
- **发布清单**在发布过程中生成，记录了发布阶段对资源执行的所有转换操作。

资源可以在构建阶段或发布阶段定义，并可在任意阶段被标记为“仅构建”或“仅发布”。例如，你可以有两个文件：一个用于开发，一个用于发布，但它们都需要通过相同的 URL 路径提供服务。`service-worker.js` 就是一个典型例子。

**构建时清单**由项目中发现的资源以及来自被引用项目和包的资源共同组成。
**发布清单**则以构建清单为基础，过滤掉仅用于构建的文件，并包含在发布过程中对这些文件执行的所有转换（如链接、打包、压缩等）。

这种机制使得在发布阶段可以执行如链接（Linking）、打包（Bundling）、压缩（Compression）等优化操作。被引用的项目也会生成自己的发布清单，其内容会在发布过程中与当前项目的清单合并。同时，在发布过程中，我们仍会保留被引用项目的原始构建清单，以便应用程序可以选择忽略被引用项目的发布资源，并对整个依赖传递闭包中的资源执行全局优化。例如，一个类库在发布时可能生成一个压缩后的 JS 包，而主应用可以选择不使用多个独立的包，而是收集所有原始构建阶段的资源，生成一个统一的包。通常情况下，构建清单和发布清单内容相同，除非存在仅在发布阶段才应用的转换。

每份清单中会列出在构建/发布过程中生成或计算出的所有资源及其属性。这些属性包括：

- **Identity**：资源的唯一标识（文件的完整路径）。
- **SourceType**：资源类型（'Discovered', 'Computed', 'Project', 'Package'）。
- **ContentRoot**：开发阶段资源暴露的原始路径。
- **BasePath**：资源暴露的基础路径。
- **RelativePath**：资源的相对路径。
- **AssetKind**：资源用途（'Build', 'Publish', 'All'），由 `CopyToOutputDirectory` / `CopyToPublishDirectory` 推断得出。
- **AssetMode**：资源作用范围（'CurrentProject', 'Reference', 'All'）。
- **AssetRole**：资源角色（'Primary', 'Related', 'Alternative'）。
- **AssetMergeSource**：当资源被嵌入到其他 TFM（目标框架）时的来源。
- **AssetMergeBehavior**：当同一 TFM 中出现资源冲突时的合并行为。
- **RelatedAsset**：当前资源所依赖的主资源的 Identity。
- **AssetTraitName**：区分相关或替代资源与主资源的特征名称（如语言、编码格式等）。
- **AssetTraitValue**：该特征的具体值。
- **CopyToBuildDirectory**：与 Content 项一致（如 PreserveNewest、Always）。
- **CopyToPublishDirectory**：与 Content 项一致。
- **OriginalItemSpec**：定义该资源的原始项规范。

关于资源在不同场景下的使用（作为主项目的一部分，或作为被引用项目的一部分），有三种可能的选项：

- **All**：资源在所有情况下都应被使用。
- **Root**：资源仅在当前项目作为主项目构建时使用。
- **Reference**：资源仅在当前项目被其他项目引用时使用。

例如，CSS 隔离（CSS isolation）生成的两个包：

- `<<Project>>.styles.css` 是 **Root** 资源，仅在作为主项目时使用。
- `<<Project>>.lib.bundle.css` 是 **Reference** 资源，仅在被其他项目引用时使用。

除了上述三种使用模式，项目还需定义其在构建和发布过程中如何处理清单中的文件。对此有三种模式：

- **Default**：项目在发布时将所有内容复制到发布输出目录，但当被其他项目引用时不做任何操作，而是期望引用方负责处理静态 Web 资源的发布。
  → 通常用于类库（class libraries）。
- **Root**：项目被视为静态 Web 资源的“根”，即使被引用，其资源也应像主项目一样被处理（例如，不复制传递依赖资源，而只复制 Root 资源）。
  → 用于如 Blazor WebAssembly 托管项目（被 ASP.NET Core 主机项目引用，但资源应视为根项目）。
- **Isolated**：与 Root 类似，但引用项目完全不知道静态 Web 资源的存在；当前项目会自行在发布时设置处理程序，将资源复制到正确位置。
  → 用于如 Blazor 桌面应用，将静态 Web 资源自动纳入 `GetCopyToPublishDirectoryItems`，使引用方无需了解静态 Web 资源机制。

关于资源类型，静态 Web 资源可分为四类：

- **Discovered assets**：从项目中已有项（如 Content、None 等）中发现的资源。
- **Computed assets**：在构建过程中生成、需要在构建时复制到最终位置的资源。
- **Project**：来自被引用项目的资源。当合并被引用项目的清单时，其 Discovered 和 Computed 资源会转换为 Project 类型。
- **Package**：来自被引用 NuGet 包的资源。

关于资源角色（Asset Role），有三种：

- **Primary（主资源）**：表示可通过其相对路径直接访问的资源。大多数资源属于此类。
- **Related（相关资源）**：表示与另一个资源相关，但两者都可通过各自的相对路径独立访问。
- **Alternative（替代资源）**：表示是另一个资源的替代形式，例如预压缩版本或不同格式版本。通常应通过与主资源相同的相对路径提供（具体实现由运行时决定）。静态 Web 资源层仅记录这种关系。

对于 Related 和 Alternative 资源，其 `RelatedAsset` 属性指向其所依赖的主资源。这种依赖链可多层嵌套，以表示一个资源的多种表示形式。静态 Web 资源仅记录这些信息，具体如何使用由 MSBuild 目标决定。

`AssetTraitName` 和 `AssetTraitValue` 用于区分相关/替代资源与其主资源。例如：

- 对于全球化程序集，可记录程序集的文化（culture）；
- 对于压缩资源，可记录编码方式（如 gzip、brotli）。

下图展示了在构建过程中被调用的MSBuild Target：

![image-20251231225433184](./aspnetcore-swa/image-20251231225433184.webp)

Sdk提供了一些重要的MSBuild Task供程序员调用：

- `DefineStaticWebAssets`：该Task扫描提供了一系列候选的资产文件并构建一个*标准化的*静态资产对象；
- `DefineStaticWebAssetEndpoints`：该Task以上一个任务输出的静态资产对象为输入，输出每个静态资产文件的Web终结点；

在构建中过程中`GenerateStaticWebAssetsManifest`和`GenerateStaticWebAssetsDevelopmentManifest`、`GenerateStaticWebAssetEndpointsManifest`等几个任务会产生一个重要的清单文件，这些文件通常存放在*obj*文件夹中，名称为`staticwebassets.*.json`。其中一个较为重要的清单文件是`staticwebassets.development.json`，其存储了所有的静态资产文件和对应的存储目录。这个文件在构建的过程中会被复制到输出目录`bin`中，名称为`$(PackageId).staticwebassets.runtime.json`。这个文件将会在生产模式下被静态资产中间件读取，作为建立静态文件终结点到实际物理文件的索引。这个文件也为需要调试`StaticWebAssetsSdk`的程序员提供了重要的调试信息，是解决ASP.NET Core中静态资产问题的不二法门。

### 解决方案

现在已经充分了解了`StaticWebAssetsSdk`，可以来设计在MSBuild中集成前端工具并生成最终静态资产文件的管线了。

首先来研究过程的步骤，`npm`或者其类似物也使用类似于MSBuild的先还原再构建两步，首先需要安装程序中使用到的包，然后在运行构建指令构建对应的静态文件，构建完成之后还需要将构建产物交给MSBuild中的静态资产处理管线进行进一步的处理。因此设计如下的三个步骤：

1. `RestoreClientAssets`，这个Target需要运行`npm install`或者类似的指令安装依赖包；
2. `BuildClientAssets`，这个Target运行`npm run build`或者类似的指令构建项目；
3. `DefineClientAssets`，这个Target调用`DefineStaticWebAssets`等Task声明静态资产文件。

确定好生成步骤之后，声明一下会在生成过程中会用到的，可以提供给用户自定义的属性。安装和构建的相应软件包肯定是需要提供给用户自定义的。在一般情况下，前端工具链把将静态文件生成到`dist`文件夹中。为了符合MSBuild的惯例，这里将中间静态文件生成到*obj*文件夹下的`ClientAssets`文件夹中。为了实现这一点，构建过程中的指令就需要支持一个指定生成目录的参数，这个参数也作为一个属性暴露给用户可以自定义。这里就形成了下面三个提供给用户自定义的参数。

```xml
<PropertyGroup>
    <ClientAssetsRestoreCommand Condition="'$(ClientAssesRestoreCommand)' == ''">pnpm install</ClientAssetsRestoreCommand>
    <ClientAssetsBuildCommand Condition="'$(ClientAssetsBuildCommand)' == ''">pnpm run build</ClientAssetsBuildCommand>
    <ClientAssetsBuildOutputParameter Condition="'$(ClientAssetsBuildOutputParameter)' == ''">--output</ClientAssetsBuildOutputParameter>
</PropertyGroup>
```

最终就是完成的构建原始代码了。第一个运行的构建目标`RestoreClientAssets`将会在`DispatchToInnerBuilds`任务运行之后运行，这个目标是MSBuild构建管线中一个不论是针对单架构生成还是多架构生成都只会运行一次的目标，这样在项目需要同时编译到.NET 8和.NET 10的情况下，仍然只会运行前端的安装命令一次。`BuildClientAssets`目标紧接着`RestoreClientAssets`目标的运行而运行，并将所有生成的前端文件添加到`_ClientAssetsBuildOutput`项中。最终的`DefineClientAssets`目标在负责解析项目中的所有静态文件的目标`ResolveWebAssetsConfiguration`运行之前运行，调用`DefineStaticWebAssets`和`DefineStaticWebAssetEndpoints`将前面生成的所有前端静态文件添加到MSBuild的静态文件处理管线中。

```xml
  <PropertyGroup>
    <_RestoreClientAssetsBeforeTargets Condition="'$(TargetFramework)' == ''">DispatchToInnerBuilds</_RestoreClientAssetsBeforeTargets>
  </PropertyGroup>

  <Target Name="RestoreClientAssets" BeforeTargets="$(_RestoreClientAssetsBeforeTargets)">
    <Message Importance="high" Text="Running $(ClientAssetsRestoreCommand)"/>
    <Exec Command="$(ClientAssetsRestoreCommand)"/>
  </Target>

  <Target Name="BuildClientAssets" DependsOnTargets="RestoreClientAssets" BeforeTargets="AssignTargetPaths">
    <PropertyGroup>
      <_ClientAssetsOutputFullPath>$([System.IO.Path]::GetFullPath('$(IntermediateOutputPath)ClientAssets'))</_ClientAssetsOutputFullPath>
    </PropertyGroup>

    <MakeDir Directories="$(_ClientAssetsOutputFullPath"/>
    <Exec Command="$(ClientAssetsBuildCommand) -- $(ClientAssetsBuildOutputParameter) $(_ClientAssetsOutputFullPath)"/>

    <ItemGroup>
      <_ClientAssetsBuildOutput Include="$(IntermediateOutputPath)ClientAssets\**"/>
    </ItemGroup>
  </Target>

  <Target Name="DefineClientAssets" AfterTargets="BuildClientAssets" DependsOnTargets="ResolveWebAssetsConfiguration">
    <ItemGroup>
      <FileWrites Include="@(_ClientAssetsBuildOutput)"/>
    </ItemGroup>

    <DefineStaticWebAssets
      CandidateAssets="@(_ClientAssetsBuildOutput)"
      SourceId="$(PackageId)"
      SourceType="Computed"
      ContentRoot="$(_ClientAssetsOutputFullPath)"
      BasePath="$(StaticWebAssetBasePath)"
    >
      <Output TaskParameter="Assets" ItemName="StaticWebAsset"/>
      <Output TaskParameter="Assets" ItemName="_ClientAssetsStaticWebAsset"/>
    </DefineStaticWebAssets>

    <DefineStaticWebAssetEndpoints
      CandidateAssets="@(_ClientAssetsStaticWebAsset)"
      ContentTypeMappings="@(StaticWebAssetContentTypeMapping)"
    >
      <Output TaskParameter="Endpoints" ItemName="StaticWebAssetEndpoint" />
    </DefineStaticWebAssetEndpoints>
  </Target>
```

为了测试如下的代码，可以在项目中新建一个`Directory.Build.targets`文件，将上述的内容复制进去进行测试，当然别忘了用`<Project>`标签包裹这一切。
