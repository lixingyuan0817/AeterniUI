# AeterniUI 项目索引

文档版本：`0.1.0`

文档状态：项目结构、入口和开发命令索引

本文档只负责项目结构、入口、命令和模块导航，不重复组件 API、设计契约或任务验收内容。

- 组件参数、行为和已完成能力：阅读 [`current-features.zh-CN.md`](current-features.zh-CN.md)。
- 组件设计约束：阅读 [`component-design-guidelines.zh-CN.md`](component-design-guidelines.zh-CN.md)。
- 计划、依赖和验收状态：阅读 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md)。
- Agent 协作和提交门禁：阅读仓库根目录 [`AGENTS.md`](../AGENTS.md)。

## 1. 技术栈

| 层次 | 技术和职责 |
| --- | --- |
| 组件库 | C#、.NET 10、Razor Class Library、Blazor Components |
| 组件项目 | `Microsoft.NET.Sdk.Razor`，目标框架 `net10.0` |
| 示例宿主 | Blazor WebAssembly，目标框架 `net10.0` |
| 桌面宿主 | Tauri 2、Rust 2021、原生 WebView |
| 样式 | CSS Isolation（`.razor.css`）+ AeterniUI 语义 Token |
| 浏览器行为 | 原生 ES Module（组件旁的 `.razor.js`），由 `JsModuleManager` 管理 |
| 图标 | 核心 `Icon` + 独立 `AeterniUI.Icons.FontAwesome` 适配项目 |
| .NET 依赖管理 | NuGet `PackageReference` |
| Rust 依赖管理 | Cargo `Cargo.toml` + 提交的 `Cargo.lock` |
| 前端包管理 | 不使用 npm、pnpm、yarn 或前端 bundler；Node 只用于 JS 语法检查 |

组件库版本由根目录 `Directory.Build.props` 中的 .NET `Version`、`AssemblyVersion`、`FileVersion` 和 `InformationalVersion` 统一管理；当前版本为 `0.1.0`。核心 .NET 包版本目前为 Blazor/ASP.NET Core `10.0.8`；Tauri Rust 依赖版本见 [`src-tauri/Cargo.toml`](../src-tauri/Cargo.toml)。

## 2. 解决方案和项目

解决方案文件为 [`aeterni_ui.slnx`](../aeterni_ui.slnx)，包含三个 .NET 项目：

| 项目 | 类型 | 作用 | 依赖关系 |
| --- | --- | --- | --- |
| `src/AeterniUI` | Razor Class Library | 核心组件、服务、主题、Token 和 JS module | 引用 Blazor Web 包 |
| `src/AeterniUI.Icons.FontAwesome` | .NET Class Library | 提供 Font Awesome `IconDefinition` | 引用 `AeterniUI` |
| `src/AeterniUI.Sample` | Blazor WebAssembly | 可交互组件画廊和宿主示例 | 引用核心库和 Font Awesome 项目 |

Tauri 项目不属于 `.slnx`，位于仓库根目录 [`src-tauri`](../src-tauri)，负责将已发布的 WASM 静态站点加载到桌面窗口。

## 3. 目录结构

```text
AeterniUI/
├── aeterni_ui.slnx                  # .NET 解决方案
├── AGENTS.md                         # Agent 协作和提交规则
├── README.md                         # 项目概览和启动说明
├── docs/                             # 设计、路线图、功能和本索引
├── scripts/
│   └── sample-publish.sh             # 发布 WASM 示例到 dist/
├── src/
│   ├── AeterniUI/                    # 核心组件库
│   │   ├── Attributes/               # JsModule 等特性
│   │   ├── Components/               # Razor、代码后置、隔离样式和组件 JS
│   │   ├── Enums/                    # Button、Color、Size、Theme 等公开枚举
│   │   ├── Icons/                    # IconDefinition 等核心图标类型
│   │   ├── Models/Dialog/             # Dialog、Alert、Toast 配置和结果
│   │   ├── Modules/                  # JS module 元数据和依赖信息
│   │   ├── Services/                 # 服务接口、选项和实现
│   │   └── wwwroot/css/              # 统一 Token 和主题变量
│   ├── AeterniUI.Icons.FontAwesome/  # Font Awesome 图标适配
│   └── AeterniUI.Sample/             # Blazor WASM 示例
│       ├── Layout/                   # MainLayout 和全局 Provider
│       ├── Pages/                    # Home、NotFound 和页面样式
│       └── wwwroot/                  # index.html、示例宿主 JS/CSS 和静态资源
├── src-tauri/                        # Rust/Tauri 桌面宿主
│   ├── capabilities/                 # Tauri 权限声明
│   ├── icons/                        # 桌面应用图标
│   ├── src/                          # main.rs、lib.rs
│   ├── Cargo.toml / Cargo.lock
│   └── tauri.conf.json
└── .github/workflows/
    ├── build.yml                     # CI 构建、JS 和 Token 检查
    └── pages.yml                     # 示例项目 GitHub Pages 发布
```

组件目录遵循“一组件一目录”的约定。典型文件职责如下：

```text
Components/<Component>/
├── <Component>.razor       # DOM 结构、事件和渲染分支
├── <Component>.razor.cs    # 参数、状态、生命周期和 class/style
├── <Component>.razor.css   # 组件隔离样式（需要时）
└── <Component>.razor.js    # 浏览器行为（需要时）
```

## 4. 主要入口

### .NET 和示例应用

- [`src/AeterniUI.Sample/Program.cs`](../src/AeterniUI.Sample/Program.cs)：创建 WASM Host、注册根组件和 `AddAeterniUI()`。
- [`src/AeterniUI.Sample/App.razor`](../src/AeterniUI.Sample/App.razor)：Blazor Router 入口。
- [`src/AeterniUI.Sample/Layout/MainLayout.razor`](../src/AeterniUI.Sample/Layout/MainLayout.razor)：注册 `ThemeProvider` 和 `DialogProvider`。
- [`src/AeterniUI.Sample/Pages/Home.razor`](../src/AeterniUI.Sample/Pages/Home.razor)：组件交互画廊。
- [`src/AeterniUI.Sample/wwwroot/index.html`](../src/AeterniUI.Sample/wwwroot/index.html)：静态 HTML、CSS、Blazor runtime 和宿主脚本入口。

### 组件库基础入口

- [`src/AeterniUI/Components/AeterniComponent.cs`](../src/AeterniUI/Components/AeterniComponent.cs)：所有组件的基类和公共生命周期。
- [`src/AeterniUI/Services/AeterniServiceCollectionExtensions.cs`](../src/AeterniUI/Services/AeterniServiceCollectionExtensions.cs)：服务注册入口 `AddAeterniUI()`。
- [`src/AeterniUI/Services/Impl/ThemeService.cs`](../src/AeterniUI/Services/Impl/ThemeService.cs)：主题模式和实际主题状态。
- [`src/AeterniUI/Services/Impl/DialogService.cs`](../src/AeterniUI/Services/Impl/DialogService.cs)：Dialog、Confirm、Alert 和 Toast 状态管理。
- [`src/AeterniUI/Services/Impl/JsModuleManager.cs`](../src/AeterniUI/Services/Impl/JsModuleManager.cs)：组件 JS module 的扫描、加载、调用和释放。

### Tauri

- [`src-tauri/src/main.rs`](../src-tauri/src/main.rs)：Rust 进程入口，转发到 `app_lib::run()`。
- [`src-tauri/src/lib.rs`](../src-tauri/src/lib.rs)：Tauri Builder、窗口初始化和 `apply_window_backdrop` 命令。
- [`src-tauri/tauri.conf.json`](../src-tauri/tauri.conf.json)：窗口、静态资源和发布钩子配置。
- [`scripts/sample-publish.sh`](../scripts/sample-publish.sh)：执行 `dotnet publish` 并整理仓库根目录 `dist/`。

## 5. 启动和开发命令

### 浏览器版示例

```bash
dotnet restore aeterni_ui.slnx
dotnet run --project src/AeterniUI.Sample/AeterniUI.Sample.csproj --launch-profile http
```

默认开发地址为 `http://localhost:5178`，端口配置位于 `src/AeterniUI.Sample/Properties/launchSettings.json`。

### Tauri 桌面版

```bash
dotnet restore aeterni_ui.slnx
cargo fetch --manifest-path src-tauri/Cargo.toml
cd src-tauri
cargo tauri dev
```

Tauri 开发模式加载发布后的静态站点，不启动 `dotnet watch` 或独立前端开发服务器。修改 UI 后通常需要重新运行 `cargo tauri dev`，或者从仓库根目录单独执行：

```bash
./scripts/sample-publish.sh Debug
```

### 构建和检查

```bash
dotnet build aeterni_ui.slnx -c Debug --nologo
cargo check --manifest-path src-tauri/Cargo.toml
node --check src/AeterniUI/Components/<Component>/<Component>.razor.js
```

CI 位于 [`.github/workflows/build.yml`](../.github/workflows/build.yml)，执行 .NET 构建、所有 `.razor.js` 的 `node --check`，以及全局 Token CSS 检查。示例项目由 [`.github/workflows/pages.yml`](../.github/workflows/pages.yml) 在 `main` 推送后发布到 GitHub Pages。

当前配置中，`src-tauri/tauri.conf.json` 的 `frontendDist` 为 `../dist`，它由 Tauri 按配置目录解析；`beforeDevCommand` / `beforeBuildCommand` 为 `scripts/sample-publish.sh ...`，Tauri CLI 从配置目录的父目录（仓库根目录）执行这两个命令。两类配置的相对路径基准不同，不能同时添加 `../`。

## 6. 核心模块索引

### 组件基础设施

- `AeterniComponent`：统一 ID、属性合并、可见/禁用语义、ElementReference 和生命周期。
- `ClassBuilder` / `StyleBuilder`：统一生成 class 和 inline style。
- `Attributes` / `Modules`：声明并解析组件 JS module 及其依赖。

### 已完成组件

| 能力组 | 组件 |
| --- | --- |
| 动作和布局 | `Button`、`ButtonGroup`、`Surface`、`Card` |
| 表单基础 | `Input`、`FormField`、`Label`、`Checkbox`、`Switch` |
| 内容和选择 | `Tag`、`List`、`ListItem`、`Rating`、`ComboBox` |
| 图标和主题 | `Icon`、`ThemeProvider`、`ThemeSwitch` |
| 浮层和反馈 | `DialogProvider`、Dialog、Confirm、Alert、Toast |

详细参数、ARIA 约定和交互行为以 [`current-features.zh-CN.md`](current-features.zh-CN.md) 为准。

### 服务模块

- `AeterniUIOptions`：Toast/Alert 默认位置、数量和时长。
- `ThemeService`：`System`、`Light`、`Dark` 模式和当前实际主题。
- `JsModuleManager`：按组件实例管理模块加载与释放。
- `DialogService` / `IDialogService`：业务代码使用的弹层和通知 API。

### 宿主模块

- 示例宿主的 `host-backdrop.js`：处理 Tauri 原生窗口背景同步。
- Rust `lib.rs`：处理窗口初始化、原生背景和 IPC 命令。
- 组件库不直接依赖 Tauri API，浏览器环境应继续正常运行。

组件当前能力：见 [`current-features.zh-CN.md`](current-features.zh-CN.md)。

任务状态和后续规划：见 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md)。

## 7. 当前边界和后续计划

当前项目没有自动化测试。未完成组件和实现边界以路线图、当前功能文档和源码为准，不在项目索引中重复维护。

## 8. 文档职责和事实源

不同文档的职责保持分离：

| 文档 | 事实范围 |
| --- | --- |
| `AGENTS.md` | Agent 协作、架构硬规则、验证和索引维护规范 |
| `docs/project-index.zh-CN.md` | 项目结构、入口、启动命令和模块导航 |
| `docs/current-features.zh-CN.md` | 已实现的公共 API 和行为 |
| `docs/component-roadmap.zh-CN.md` | 未完成任务、依赖、验收和构建记录 |
| `docs/component-design-guidelines.zh-CN.md` | 组件 API、样式、可访问性和 JS 设计契约 |

发生冲突时，优先以实际代码、`AGENTS.md` 的硬规则和 CI 检查为准，并在同一变更中修正文档冲突。
