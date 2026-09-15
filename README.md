# AeterniUI

AeterniUI 是一个 Blazor 组件库，面向 Blazor Server、Blazor WebAssembly
以及 Tauri + Blazor WebAssembly 应用，提供一致的用户界面。

## 环境要求

- .NET SDK 10.0 或兼容的 .NET 10 SDK
- Rust 工具链
- Tauri CLI 2.x
- Tauri 支持的平台 WebView

项目使用 .NET 10，并面向 `net10.0`。在新计算机上请重新还原 NuGet
和 Cargo 依赖，不要复制旧计算机生成的构建输出。

## 项目组成

- `src/AeterniUI`：核心组件库，包含组件使用的供应商无关内置
  `AeterniIcons` 图标集。
- `src/AeterniUI.Icons.FontAwesome`：可选的 Font Awesome Free 7.3.1
  图标定义，由 `scripts/generate-fontawesome-icons.mjs` 生成。
- `src/AeterniUI.Sample`：Blazor WebAssembly 示例项目。
- `src-tauri`：Tauri 桌面宿主。
- `docs/project-index.zh-CN.md`：项目结构、入口、启动命令和模块索引。
- `docs/component-design-guidelines.zh-CN.md`：组件设计约束。
- `docs/current-features.zh-CN.md`：当前已实现的功能清单。

## 启动 Tauri 示例

```bash
cd src-tauri
cargo tauri dev
```

该命令通过配置的 `beforeDevCommand` 将 Blazor 示例发布到仓库根目录的
`dist/`，然后由 Tauri 直接加载 `dist/index.html`。项目不使用
`dotnet watch` 或独立开发服务器。修改界面后，重新运行
`cargo tauri dev` 或执行 `scripts/sample-publish.sh` 刷新页面。

仓库根目录的 `dist/` 由脚本生成并已被 Git 忽略，请勿直接编辑或提交。
如果启动时报错，请确认 `dist/index.html` 已生成。`bin`、`obj` 和
`target` 目录不应在计算机之间复制。

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

推送与 `Directory.Build.props` 版本一致的 tag 后，GitHub Actions 会自动
发布 NuGet 包。请先在 NuGet.org 为本仓库配置 Trusted Publishing，然后
修改 `Directory.Build.props` 中的版本并推送对应 tag：

```bash
git tag v10.7.1
git push origin v10.7.1
```

发布流程会同时发布：

- `AeterniUI`
- `AeterniUI.Icons.FontAwesome`

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

## 文档

| 主题 | 文档 |
| --- | --- |
| 项目规则 | [`AGENTS.md`](AGENTS.md) |
| 组件设计契约 | [`docs/component-design-guidelines.zh-CN.md`](docs/component-design-guidelines.zh-CN.md) |
| 当前已实现功能 | [`docs/current-features.zh-CN.md`](docs/current-features.zh-CN.md) |
| 路线图和验收标准 | [`docs/component-roadmap.zh-CN.md`](docs/component-roadmap.zh-CN.md) |
| 项目结构和命令 | [`docs/project-index.zh-CN.md`](docs/project-index.zh-CN.md) |
| Agent 开发流程 | [`docs/agent-development.zh-CN.md`](docs/agent-development.zh-CN.md) |

## 迁移到其他计算机

1. 复制或克隆源代码，排除生成输出和 IDE 元数据。
2. 安装所需的 .NET SDK、Rust 工具链和 Tauri CLI。
3. 执行 `dotnet restore aeterni_ui.slnx`。
4. 执行 `cargo fetch --manifest-path src-tauri/Cargo.toml`。
5. 从 `src-tauri` 目录运行 `cargo tauri dev`。
6. 在请求其他代码 Agent 修改项目之前，先阅读 `AGENTS.md`。

计算机相关的代理设置应放在本机 Shell 或 Agent 配置中，不要提交到仓库。
配置 HTTPS 代理时，请使用正确命名的 `HTTPS_PROXY` 环境变量。
