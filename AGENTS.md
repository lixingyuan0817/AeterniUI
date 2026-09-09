# AeterniUI Agent 指南

> 本文档只记录项目硬性约束、禁止事项、验证门禁和 Agent 协作规则。组件设计契约、当前 API、路线图和项目导航分别以对应文档为准。

## 0. 开工前置与提交自检（跨 agent 协作约定）

开始任何任务前，先按 `docs/agent-development.zh-CN.md` 阅读以下文档：

1. `docs/component-design-guidelines.zh-CN.md`（组件契约）
2. `docs/component-roadmap.zh-CN.md`（任务、验收和状态）
3. `docs/current-features.zh-CN.md`（公共表面事实源）

涉及项目结构、入口、宿主、构建命令或模块定位的任务，还必须阅读
`docs/project-index.zh-CN.md`（项目索引）。索引文档只负责导航，不替代组件契约、功能清单或路线图。

禁止事项：不得在 `wwwroot/css/aeterni_ui.css` 写入组件样式（CI 会拦截）；不得用 `::deep` 或全局规则在示例页覆盖组件原生样式；不得新建第二套尺寸、颜色、圆角体系；不得绕过 `ThemeProvider` 自行修改 `data-theme`。

提交前自检：`dotnet build aeterni_ui.slnx` 通过；修改 `.razor.js` 时运行 `node --check`；同步 `current-features` 与 roadmap 状态；示例页有可交互演示；不提交 `bin/`、`obj/`、`target/`、`dist/`、`.sample-publish/` 及 IDE/OS 文件。

## 项目范围

AeterniUI 是一个 Blazor 组件库，支持以下三类宿主：

- Blazor Server 应用。
- Blazor WebAssembly 应用。
- 承载 Blazor WebAssembly 示例项目的 Tauri 桌面应用。

示例项目位于 `src/AeterniUI.Sample`。Tauri 开发宿主位于根目录 `src-tauri`。

## 编辑前

1. 阅读 `docs/component-design-guidelines.zh-CN.md`，了解组件契约。
2. 阅读 `docs/current-features.zh-CN.md`，了解已实现的公共表面。
3. 引入抽象前，检查现有组件和 Token 的实现模式。
4. 保留工作区中与当前任务无关的用户修改。

## 架构规则

组件设计细节以 [`docs/component-design-guidelines.zh-CN.md`](docs/component-design-guidelines.zh-CN.md) 为准；以下只保留项目级硬规则。

- 所有组件都直接或间接继承 `AeterniComponent`。
- 使用基类提供的生成 ID、`ClassBuilder`、`StyleBuilder`、公共属性和可选 JS module 生命周期。
- JS 是可选能力。不包含 `.razor.js` 文件的组件不得要求 JS 初始化。
- 每个组件最多维护一个主 JS module。浏览器行为放在 module 中，业务状态放在 C# 中。
- 服务接口放在 `Services`，实现放在 `Services/Impl`。
- 不要为每个组件都创建一个 `Ixxx` 接口。
- 使用现有语义 Token。组件内部不得创建第二套颜色、间距、圆角或动效体系。
- 标记放在 `.razor`，行为和参数放在 `.razor.cs`，隔离样式放在 `.razor.css`。
- 除非确实需要修改共享 Token 系统，否则不要修改 `src/AeterniUI/wwwroot/css/aeterni_ui.css`。
- 遵循现有公共命名：`Button`、`Surface`、`Card`、`ThemeProvider`、`ThemeSwitch` 和 `DialogProvider` 不使用 `Au` 前缀。

## 组件 API

- 通过类型化参数和 `EventCallback` 暴露公共行为。
- 保留 `aria-label`、`aria-live`、`aria-modal`、`aria-busy` 等无障碍属性及键盘行为。
- 在服务边界校验枚举和数值配置。
- 定时通知明确持久显示时使用 `TimeSpan.Zero`；需要继承全局默认值时使用 `null`。
- 释放事件订阅、定时器、取消令牌和 JS 资源。

## Dialog 和通知规则

- `DialogProvider` 是自闭合组件，应在应用 Layout 或根组件中只放置一次。
- 应用组件使用 `IDialogService`，不得访问 Provider 内部状态。
- `Confirm` 和自定义 `Dialog` 是模态内容，负责遮罩、焦点和背景滚动。
- `Alert` 是非模态语义提示，默认底部居中，支持全部八个 `ToastPosition` 位置和自动关闭；消息最多显示两行。
- `Toast` 是非阻塞通知，支持全部八个 `ToastPosition` 位置。
- Alert 和 Toast 使用 Button 的语义颜色映射、可选模糊、四边进度边框和退出动效。

## Tauri 开发

Tauri 宿主加载示例项目发布后的静态构建，不使用开发服务器：

```bash
cd src-tauri
cargo tauri dev
```

`src-tauri/tauri.conf.json` 中的 `beforeDevCommand` 会运行 `scripts/sample-publish.sh Debug`，将 Blazor 示例发布到仓库根目录的 `dist/`，使 `dist/index.html` 成为 Web 根目录。Tauri CLI 从配置目录的父目录（仓库根目录）运行前置命令；不要给脚本路径添加 `../`。不存在 `dotnet watch` 或独立前端开发服务器。不要手动发布或编辑 `dist/`；它由脚本重新生成并已被 Git 忽略。

刷新 UI 时重新运行 `cargo tauri dev`，或从仓库根目录直接运行：

```bash
./scripts/sample-publish.sh Debug
```

Tauri 配置文件是 `src-tauri/tauri.conf.json`，其中 `frontendDist` 指向 `../dist`。

## 验证

用户可能正在运行 Tauri 热重载。未经明确批准，不要终止其进程或执行破坏性清理。

针对窄范围修改，先执行静态检查。当前项目没有自动化测试。需要构建时使用：

```bash
dotnet build aeterni_ui.slnx
```

仅修改 JS 时使用：

```bash
node --check path/to/module.razor.js
```

如果静态 Web 资源错误指向 `obj\\Debug`，先停止运行中的 Tauri 进程，再清理生成的 `bin`/`obj`，然后通过 `scripts/sample-publish.sh` 或 `cargo tauri dev` 重新构建。

## 修改纪律

- 修改范围应聚焦于用户要求的行为。
- 没有具体需求时，不要用新框架或依赖替换现有项目架构。
- 不要提交生成的 `bin`、`obj`、`target`、IDE 或机器本地文件。
- 公共 API 或组件能力变化时，更新对应的中文文档。

## 项目索引维护

- `docs/project-index.zh-CN.md` 是项目结构、项目入口、启动命令、构建工具和核心模块的导航索引。
- 新增、删除、重命名或移动项目、目录、入口文件、宿主、构建脚本、公开模块或开发命令时，必须在同一变更中同步更新索引。
- 修改 Tauri 静态发布流程、`beforeDevCommand`/`beforeBuildCommand`、解决方案项目列表或 CI 门禁时，必须检查索引中的命令和路径仍然有效。
- 新增公共组件或服务时，在索引中补充模块归属和简短职责；完整参数、行为和示例继续分别维护在 `current-features`、组件文档和示例页中，避免复制整套 API。
- 功能尚未完成时，索引只能标记为“规划中/未完成”或链接到 roadmap，不得把计划能力写成已实现能力。
- 文档中的路径、命令和入口必须以仓库当前文件为准；不要记录本机代理、缓存、生成目录或临时环境状态。
- 提交前如果变更触及上述范围，应检查 `git diff --name-status`，确认索引、`current-features` 和 roadmap 的职责没有遗漏或重复，并确认没有加入 `bin/`、`obj/`、`target/`、`dist/` 或 `.sample-publish/`。
