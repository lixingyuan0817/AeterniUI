# AeterniUI

AeterniUI 是一个 Blazor 组件库，面向 Blazor Server 和 Blazor WebAssembly
应用，提供一致的用户界面。

## 环境要求

- .NET SDK 10.0 或兼容的 .NET 10 SDK

项目使用 .NET 10，并面向 `net10.0`。在新计算机上请重新还原 NuGet
依赖，不要复制旧计算机生成的构建输出。

项目组成、文档职责与开发约束见 [`AGENTS.md`](AGENTS.md)。

## 启动浏览器示例

```bash
dotnet run --project src/AeterniUI.Sample/AeterniUI.Sample.csproj --launch-profile http
```

访问 `http://localhost:5178` 查看 Blazor WebAssembly 交互示例。

静态发布使用 `./scripts/sample-publish.sh Release`，GitHub Pages 工作流也使用
该脚本。仓库根目录的 `dist/` 由脚本生成并已被 Git 忽略，请勿直接编辑或提交。
`bin`、`obj` 目录不应在计算机之间复制。

## 使用组件库

在应用中注册服务：

```csharp
builder.Services.AddAeterniUI();
```

在根布局中放置 Provider，每个应用只需要放置一次：

```razor
<ThemeProvider />
<DialogProvider />
@Body
```

公共 API 和组件规则请参阅 `docs/` 目录中的文档。

## 发布 NuGet 包

先在 NuGet.org 为本仓库配置 Trusted Publishing，再修改 `Directory.Build.props`
中的 `Version` 并推送同名 tag（`<version>` 取自该文件，README 不复制版本字面量）：

```bash
git tag v<version>
git push origin v<version>
```

工作流会校验 tag 与 `Directory.Build.props` 一致，并同时发布 `AeterniUI`
与 `AeterniUI.Icons.FontAwesome`。

## 图标

组件内部使用核心库提供的 `Icon` 组件和内置 `AeterniIcons` 图标集渲染
所有字形，包括勾选、箭头、评分星标、通知语意图标和关闭按钮，因此核心
库本身不依赖任何图标供应商。

消费者可以使用 `AeterniUI.Icons.FontAwesome` 适配包。该包提供精选的
Font Awesome Free 7.3.1 图标，并通过以下 API 暴露：

- `FontAwesomeIcons.Solid.<Name>`：类型化图标属性
- `FontAwesomeIcons.Categories`：按用途分组的图标集合
- `FontAwesomeIcons.TryGet(name, out definition)`：按名称解析图标

示例项目的图标浏览页面位于 `/icons`。

`FontAwesomeIcons.cs` 由官方 npm 包生成。请修改
`scripts/generate-fontawesome-icons.mjs` 中的清单后重新生成，不要手动
编辑生成文件。首次运行脚本需要 Node.js 和网络连接；.NET 构建本身不依赖
Node.js。

## 许可证

AeterniUI 自有源代码采用 [MIT License](LICENSE)。

`AeterniUI.Icons.FontAwesome` 包含来自 Font Awesome Free Classic Solid 7.3.1
的精选 SVG 路径数据。相关图标资源按照 CC BY 4.0 许可使用，并保留
Font Awesome 的版权和许可说明。详情请参阅：

- [`src/AeterniUI.Icons.FontAwesome/THIRD-PARTY-NOTICES.md`](src/AeterniUI.Icons.FontAwesome/THIRD-PARTY-NOTICES.md)
- [Font Awesome Free License](https://fontawesome.com/license/free)

示例站（`src/AeterniUI.Sample`）内置一份本地 vendored 的
[GSAP 3.15.0](https://gsap.com/)（`wwwroot/js/vendor/gsap.min.js`）用于首页动画，
不通过网络加载，断网与内网环境同样可用。GSAP 采用 Standard "no charge" license
（多数用途免费），来源、版本与更新方式见
[`src/AeterniUI.Sample/wwwroot/js/vendor/README.md`](src/AeterniUI.Sample/wwwroot/js/vendor/README.md)。

## 文档

完整文档索引与开发约束见 [`AGENTS.md`](AGENTS.md)；当前交付的功能与公共 API 见 [`docs/delivered-features.zh-CN.md`](docs/delivered-features.zh-CN.md)。

计算机相关的代理设置应放在本机 Shell 或 Agent 配置中，不要提交到仓库；
配置 HTTPS 代理时请使用正确命名的 `HTTPS_PROXY` 环境变量。
