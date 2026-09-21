# AeterniUI 项目索引

文档版本：`10.14.3`

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
| 样式 | CSS Isolation（`.razor.css`）+ AeterniUI 语义 Token |
| 浏览器行为 | 原生 ES Module（组件旁的 `.razor.js`），由 `JsModuleManager` 管理 |
| 图标 | 核心 `Icon` 与内置 `AeterniIcons`；`AeterniUI.Icons.FontAwesome` 适配包提供 343 个精选图标 |
| .NET 依赖管理 | NuGet `PackageReference` |
| 前端包管理 | 不使用 npm、pnpm、yarn 或前端 bundler；Node 只用于 `.razor.js` 语法检查和图标生成脚本 |

组件库版本由根目录 `Directory.Build.props` 中的 .NET `Version`、`AssemblyVersion`、`FileVersion` 和 `InformationalVersion` 统一管理；当前版本为 `10.14.3`。首位固定与 .NET 主版本对齐，第二位记录功能更新，第三位记录修复与优化。核心 .NET 包版本目前为 Blazor/ASP.NET Core `10.0.8`。

## 2. 解决方案和项目

解决方案文件为 [`aeterni_ui.slnx`](../aeterni_ui.slnx)，包含四个 .NET 项目：

| 项目 | 类型 | 作用 | 依赖关系 |
| --- | --- | --- | --- |
| `src/AeterniUI` | Razor Class Library | 核心组件、服务、主题、Token 和 JS module | 引用 Blazor Web 包 |
| `src/AeterniUI.Icons.FontAwesome` | .NET Class Library | 提供 Font Awesome `IconDefinition` | 引用 `AeterniUI` |
| `src/AeterniUI.Sample` | Blazor WebAssembly | 可交互组件画廊和宿主示例 | 引用核心库和 Font Awesome 项目 |
| `tests/AeterniUI.ContractChecks` | .NET Console | 使用 `HtmlRenderer` 执行关键组件渲染契约回归检查 | 引用 `AeterniUI` 与 ASP.NET Core 共享框架，不引入外部测试包 |

仓库提供 Blazor WebAssembly 示例宿主。

## 3. 目录结构

```text
AeterniUI/
├── aeterni_ui.slnx                  # .NET 解决方案
├── AGENTS.md                         # Agent 协作和提交规则
├── README.md                         # 项目概览和启动说明
├── docs/                             # 设计、路线图、功能和本索引
├── scripts/
│   ├── generate-fontawesome-icons.mjs # 从官方 npm 包生成 Font Awesome 定义
│   ├── check-docs.sh                 # 文档一致性检查
│   ├── check-css-comments.mjs        # CSS 注释提前闭合检查
│   ├── check-contrast.mjs            # 品牌色板承字/图形对比度门禁
│   └── sample-publish.sh             # 发布 WASM 示例到 dist/
├── src/
│   ├── AeterniUI/                    # 核心组件库
│   │   ├── Attributes/               # JsModule 等特性
│   │   ├── Components/               # Razor、代码后置、隔离样式和组件 JS
│   │   ├── Enums/                    # Button、Color、Size、Theme、TimeFormat 等公开枚举
│   │   ├── Icons/                    # IconDefinition、内置 AeterniIcons 图标集
│   │   ├── Models/                   # DateRangePreset 与 Dialog/Alert/Toast 模型
│   │   ├── Modules/                  # JS module 元数据和依赖信息
│   │   ├── Services/                 # 服务接口、选项和实现
│   │   └── wwwroot/                  # 统一 Token、主题变量与共享浏览器能力（css/、js/）
│   ├── AeterniUI.Icons.FontAwesome/  # Font Awesome 图标适配
│   └── AeterniUI.Sample/             # Blazor WASM 示例
│       ├── Components/Showcase/      # 示例页共享件：ComponentPreview、组件目录、页面基类
│       ├── Layout/                   # MainLayout（固定头部 + 全局 Provider）与组件页侧栏布局
│       ├── Pages/                    # 首页、图标浏览、NotFound 和页面样式
│       │   └── Components/           # 每个组件一个独立页面（路由 /components/{id}）
│       └── wwwroot/                  # index.html、示例 CSS、共享展示样式和静态资源
├── tests/
│   └── AeterniUI.ContractChecks/      # 根属性、ARIA、焦点停留点和公开样式契约检查
└── .github/workflows/
    ├── build.yml                     # CI 构建、JS 和 Token 检查
    ├── nuget-release.yml             # 版本 tag 触发 NuGet 包发布
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
- [`src/AeterniUI.Sample/Layout/MainLayout.razor`](../src/AeterniUI.Sample/Layout/MainLayout.razor)：注册 `ThemeProvider` 和 `DialogProvider`，并承载示例项目固定头部（品牌、首页/组件导航、主题切换）。
- [`src/AeterniUI.Sample/Pages/Home.razor`](../src/AeterniUI.Sample/Pages/Home.razor)：首页文档页（路由 `/`），品牌介绍与右侧基础使用代码窗口；背景是铺满整个视口（含页头与外壳留白）的淡彩光晕与细网格动画。
- [`src/AeterniUI.Sample/Layout/ComponentsLayout.razor`](../src/AeterniUI.Sample/Layout/ComponentsLayout.razor)：组件页的侧栏布局（`@layout MainLayout` 之上再嵌一层），负责分类导航、路由高亮和预览容器；页面本体是 `@Body`。
- [`src/AeterniUI.Sample/Pages/Components/`](../src/AeterniUI.Sample/Pages/Components)：**每个组件一个独立页面**，路由形如 `/components/button`、`/components/tabs`，安装页是 `/components`，色彩页是 `/components/colors`；组件页只包含该组件的演示标记与自己的 `@code` 状态，色彩页则不引用任何组件，只用 `showcase-*` 标记陈列语义 Token（色族、文字墨阶、字体、中性表面、圆角、阴影、间距与对比度基线）。
- [`src/AeterniUI.Sample/Components/Showcase/ShowcasePageBase.cs`](../src/AeterniUI.Sample/Components/Showcase/ShowcasePageBase.cs)：跨页共享的少量状态（注入的服务、Button 演示的交互提示、主题订阅），其余状态都留在各自页面里。
- [`src/AeterniUI.Sample/Components/Showcase/ShowcaseCatalog.cs`](../src/AeterniUI.Sample/Components/Showcase/ShowcaseCatalog.cs)：侧栏分类与页面清单的单一事实源，导航 Href 由条目 id 生成。
- [`src/AeterniUI.Sample/wwwroot/css/showcase.css`](../src/AeterniUI.Sample/wwwroot/css/showcase.css)：所有组件页共享的展示样式（`showcase-*` / `preview-*` / `control-*`）；组件页拆成独立文件后，作用域 CSS 无法跨页生效，所以这里是全局表。
- [`src/AeterniUI.Sample/Pages/Icons.razor`](../src/AeterniUI.Sample/Pages/Icons.razor)：图标浏览页（路由 `/icons`），展示内置 `AeterniIcons` 与 Font Awesome 精选集，支持按名称搜索和 Size/Color 预览。
- [`src/AeterniUI.Sample/wwwroot/index.html`](../src/AeterniUI.Sample/wwwroot/index.html)：静态 HTML、CSS、Blazor runtime 和预渲染主题脚本入口；`<head>` 的预渲染主题脚本与 `#app` 内的首屏加载器（`.sample-boot`，样式在 `wwwroot/css/app.css`）都在这里。

### 组件库基础入口

- [`src/AeterniUI/Components/AeterniComponent.cs`](../src/AeterniUI/Components/AeterniComponent.cs)：所有组件的基类和公共生命周期。
- [`src/AeterniUI/Services/AeterniServiceCollectionExtensions.cs`](../src/AeterniUI/Services/AeterniServiceCollectionExtensions.cs)：服务注册入口 `AddAeterniUI()`，同时校验并注册 `AeterniUIOptions`。
- [`src/AeterniUI/Services/AeterniUIOptions.cs`](../src/AeterniUI/Services/AeterniUIOptions.cs)：弹层默认值、默认品牌色层与文案表入口。
- [`src/AeterniUI/Services/AeterniUITextOptions.cs`](../src/AeterniUI/Services/AeterniUITextOptions.cs)：库内用户可见文案的集中定义与覆写入口。
- [`src/AeterniUI/Components/ComponentClass.cs`](../src/AeterniUI/Components/ComponentClass.cs)：尺寸、语意色与 Severity 到 CSS 修饰类的唯一映射。
- [`src/AeterniUI/Services/Impl/ThemeService.cs`](../src/AeterniUI/Services/Impl/ThemeService.cs)：主题模式、实际主题与品牌色层状态。
- [`src/AeterniUI/Services/Impl/DialogService.cs`](../src/AeterniUI/Services/Impl/DialogService.cs)：Dialog、Confirm、Alert 和 Toast 状态管理。
- [`src/AeterniUI/Services/Impl/JsModuleManager.cs`](../src/AeterniUI/Services/Impl/JsModuleManager.cs)：组件 JS module 的扫描、加载、调用和释放。
- [`src/AeterniUI/wwwroot/js/aeterni_floating.js`](../src/AeterniUI/wwwroot/js/aeterni_floating.js)：共享浮层能力（视口贴合与翻转、`createFocusTrap`、引用计数的 `lockScroll`），由 Tooltip、Popover、Drawer 和 DialogProvider 导入。

- `Components/List`：泛型数据/声明式列表、普通模板与统一 Card 外壳；ListItem 级联类型及选择注册。迁移说明见功能清单 §16。
- `Components/ComboBox/ComboBox.razor.js`、`Components/Rating/Rating.razor.js`：选择/评分控件的默认按键行为与选项视窗滚动；业务值保留在 C#，各组件仍只有一个主 module。
- `wwwroot/js/aeterni_floating.js` 的 `waitForExit`：Drawer/Popover 共用的可取消退出动画完成通知，复用实际 CSS 动画与 reduced-motion 设置。

### 渲染契约检查

- [`tests/AeterniUI.ContractChecks/Program.cs`](../tests/AeterniUI.ContractChecks/Program.cs)：通过 `HtmlRenderer` 检查 DatePicker/MenuButton 根属性、Accordion ARIA/inert、DateCalendar 网格、TimeOptionList 单一 Tab 停留点和 Avatar 公开变体。同时覆盖 SplitButton 根属性、独立名称/禁用/加载、尺寸变体、受控打开与 MenuButton 菜单 id 关联。同时通过 `ListContractHost.cs` 覆盖 List 模板、Card、声明式注册、动态数据与键盘。该项目是小型稳定门禁，不替代完整交互或端到端测试。
- [`tests/popover-focus.test.mjs`](../tests/popover-focus.test.mjs)：`node --test tests/popover-focus.test.mjs`，用 DOM 替身验证非模态菜单选择/Escape 焦点回归、外部点击/Tab/业务回调不抢焦点和禁用触发器跳过；不是浏览器端到端测试。
- [`tests/quality-interactions.test.mjs`](../tests/quality-interactions.test.mjs)：模态 Esc 策略、可取消退出、reduced-motion、键盘与 Tooltip 动态关联的浏览器行为回归；C# 的 `QualityContractHost.cs` 配合 15 组渲染契约覆盖状态、日期极值与时间映射复用。

### 静态发布

- [`scripts/sample-publish.sh`](../scripts/sample-publish.sh)：执行 `dotnet publish` 并整理仓库根目录 `dist/`；发布后校验 `dist/index.html` 引用的同源本地资源都存在（例如 `css/showcase.css`），缺失时直接失败，避免产出一个「样式表没加载、布局静默崩坏」的站点。

### 图标

- [`src/AeterniUI/Icons/AeterniIcons.cs`](../src/AeterniUI/Icons/AeterniIcons.cs)：核心库自带的供应商无关图标集，组件内部字形均由此渲染。
- [`src/AeterniUI.Icons.FontAwesome/FontAwesomeIcons.cs`](../src/AeterniUI.Icons.FontAwesome/FontAwesomeIcons.cs)：生成的 Font Awesome Free 精选定义，含 `Categories` 分组和 `TryGet` 名称解析。
- [`scripts/generate-fontawesome-icons.mjs`](../scripts/generate-fontawesome-icons.mjs)：从官方 npm 包重新生成上述定义，并校验清单中的图标名称。

## 5. 启动和开发命令

### 浏览器版示例

```bash
dotnet restore aeterni_ui.slnx
dotnet run --project src/AeterniUI.Sample/AeterniUI.Sample.csproj --launch-profile http
```

默认开发地址为 `http://localhost:5178`，端口配置位于 `src/AeterniUI.Sample/Properties/launchSettings.json`。

### 静态站点发布

```bash
./scripts/sample-publish.sh Release
```

脚本将示例发布到仓库根目录 `dist/`，供静态托管及 GitHub Pages 工作流使用。

### 构建和检查

```bash
dotnet build aeterni_ui.slnx -c Debug --nologo
bash scripts/check-docs.sh
dotnet run --project tests/AeterniUI.ContractChecks/AeterniUI.ContractChecks.csproj --no-build
node --test tests/list-keyboard.test.mjs          # Components/List/List.razor.js 的 DOM 次序、按键拦截与释放回归
node --test tests/*.test.mjs                     # CI 同步执行全部浏览器行为单元回归
node --check src/AeterniUI/Components/<Component>/<Component>.razor.js
node scripts/check-css-comments.mjs                    # CSS 注释是否提前闭合
node scripts/check-contrast.mjs                        # 品牌色板对比度是否达标
node scripts/generate-fontawesome-icons.mjs --check   # 图标定义是否与生成脚本清单一致
```

CI 位于 [`.github/workflows/build.yml`](../.github/workflows/build.yml)，执行 .NET 构建、最小组件渲染契约检查、所有 `.razor.js` 的 `node --check`、`scripts/check-css-comments.mjs`、`scripts/check-contrast.mjs`，以及全局 Token CSS 检查。示例项目由 [`.github/workflows/pages.yml`](../.github/workflows/pages.yml) 在 `main` 推送后发布到 GitHub Pages。推送匹配当前版本的 `v*.*.*` tag 时，[`.github/workflows/nuget-release.yml`](../.github/workflows/nuget-release.yml) 会通过 NuGet Trusted Publishing 发布核心包和 Font Awesome 图标包。

GitHub Pages 没有 SPA 重写：发布步骤把 `dist/index.html` 复制为 `dist/404.html`，因此直接访问或刷新深链接（例如 `/components/button`）会拿到 404 状态码的完整应用外壳。应用仍会正常启动并路由到目标页面，但浏览器控制台会记录该 404；页面内部的链接点击走框架拦截，不产生文档请求。

## 6. 核心模块索引

### 组件基础设施

- `AeterniComponent`：统一 ID、属性合并、可见/禁用语义、ElementReference 和生命周期。
- `ClassBuilder` / `StyleBuilder`：统一生成 class 和 inline style。
- `ComponentClass`：把 `Size`、`Color`、`Severity` 与通用修饰类映射为组件根类名，默认档位不输出修饰类。
- `Attributes` / `Modules`：声明并解析组件 JS module 及其依赖。

### 已完成组件

| 能力组 | 组件 |
| --- | --- |
| 动作和布局 | `Button`、`ButtonGroup`、`SplitButton`（`Components/SplitButton`：主动作与菜单动作组合，示例 `/components/split-button`）、`Surface`、`Card`、`Divider`、`Accordion` |
| 表单基础 | `Input`、`FormField`、`Label`、`Textarea`、`Checkbox`、`Switch`、`Radio`、`RadioGroup`、`Segmented`、`DatePicker`、`DateRangePicker`、`TimePicker`、`DateTimePicker` |
| 内容和选择 | `Tag`、`Badge`、`Avatar`、`Empty`、`List<TItem>`、`ListItem<TItem>`、`Rating`、`ComboBox`、`Menu`、`Tabs`、`Tab` |
| 状态反馈 | `Progress`、`Spinner`、`Skeleton` |
| 图标和主题 | `Icon`、`AeterniIcons`、`ThemeProvider`、`ThemeSwitch`、`ThemeBrandSwitch` |
| 浮层和反馈 | `PopupHost`、`Popover`、`Tooltip`、`Drawer`、`Pagination`、`DialogProvider`、Dialog、Confirm、Alert、Toast |

详细参数、ARIA 约定和交互行为以 [`current-features.zh-CN.md`](current-features.zh-CN.md) 为准。

### 日期与时间选择模块

- `Components/DatePicker`：`DatePicker`、`DateRangePicker` 与内部 `DateCalendar`；范围选择支持宿主提供的 `DateRangePreset` 和 1～3 个月视图。
- `Components/TimePicker`：`TimePicker` 与内部 `TimeOptionList`，共用 `TimePickerOptions` 的整秒可用性映射、时/分/秒滚轮、过滤和格式化能力；默认 24 小时制与 `HH:mm:ss`，1 秒步长不会展开全天 DOM 列表。
- `Components/DateTimePicker`：以单一 `DateTime?` 组合日期网格与共享时/分/秒滚轮；`TimeFormat` 定义 12/24 小时策略，组件不执行时区转换。

### 服务模块

- `AeterniUIOptions`：Toast/Alert 默认位置、数量和时长，以及首访默认品牌色层。
- `ThemeService`：`System`、`Light`、`Dark` 模式、当前实际主题，以及 `Purple`／`Green`／`Orange` 品牌色层。
- `JsModuleManager`：按组件实例管理模块加载与释放。
- `DialogService` / `IDialogService`：业务代码使用的弹层和通知 API。

### 宿主边界

- 示例为标准 Blazor WebAssembly 应用，预渲染主题脚本位于 `wwwroot/index.html`。
- 组件库面向 Blazor Server 与 Blazor WebAssembly；`ThemeProvider` 通过浏览器 API 管理页面主题与品牌色层。

组件当前能力：见 [`current-features.zh-CN.md`](current-features.zh-CN.md)。

任务状态和后续规划：见 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md)。

## 7. 当前边界和后续计划

当前路线图已交付 v0.1～v0.5 的功能清单、v0.5.1 第六轮质量收口和 v10.11.0 时间选择增强：`TimePicker` / `DateTimePicker` 已支持高效的时/分/秒滚轮并默认使用 24 小时制 `HH:mm:ss`，`DateRangePicker` 已支持快捷范围与 1～3 个月多月视图，根属性、键盘焦点、视觉状态、本地化和示例覆盖缺口已关闭。`DateCalendar` 与 `TimeOptionList` 是选择器内部渲染部件，不属于稳定公共 API。v0.6 Toolbar、ToggleGroup 与 SplitButton 已交付（3/5），下一项为 `Breadcrumb`，其余两项未开始；时区转换、跨午夜时间范围、虚拟化与复杂本地化日历仍不在当前范围。

时间滚轮的 JS 回归检查位于 `tests/time-wheel.test.mjs`，使用 `node --test tests/time-wheel.test.mjs` 验证 Token 动画完成回调、重复渲染、快速改值、输入中断、不同真实行高、禁用项、边缘居中、reduced-motion 与资源释放；滚筒仅变换文字视觉层。

当前项目提供最小组件渲染契约门禁，但尚未建立完整业务测试或浏览器端到端测试套件。未完成组件和实现边界以路线图、当前功能文档和源码为准，不在项目索引中重复维护。

## 8. 文档职责和事实源

不同文档的职责保持分离：

| 文档 | 事实范围 |
| --- | --- |
| `AGENTS.md` | Agent 协作、架构硬规则、验证和索引维护规范 |
| `docs/project-index.zh-CN.md` | 项目结构、入口、启动命令和模块导航 |
| `docs/current-features.zh-CN.md` | 已实现的公共 API 和行为 |
| `docs/component-roadmap.zh-CN.md` | 阶段任务、依赖、验收状态和构建记录 |
| `docs/component-design-guidelines.zh-CN.md` | 组件 API、样式、可访问性和 JS 设计契约 |
| `docs/component-review-todo.zh-CN.md` | 组件审阅发现的质量问题待办、优先级和验收条件 |

发生冲突时，优先以实际代码、`AGENTS.md` 的硬规则和 CI 检查为准，并在同一变更中修正文档冲突。

### ToggleGroup 模块

- `src/AeterniUI/Components/ToggleGroup/`：单选/多选按钮动作、ToggleGroupItem 记录与单停留点焦点模块；复用现有 SelectionMode / Orientation / Size 枚举。完整受控绑定与语义边界见 current-features 的 ToggleGroup 章节。
- `src/AeterniUI.Sample/Pages/Components/ToggleGroup.razor`：`/components/toggle-group` 交互示例，由 ShowcaseCatalog 注册。
- `tests/toggle-group.test.mjs`：运行 `node --test tests/toggle-group.test.mjs` 验证焦点移动、RTL、动态修复和资源释放；`tests/AeterniUI.ContractChecks/Program.cs` 覆盖渲染、参数校验和受控状态提议。不代替浏览器端到端验收。

### Toolbar 模块

- `src/AeterniUI/Components/Toolbar/`：Toolbar 的布局、单停留点焦点模块与命名 ToolbarGroup，归属基础动作组件。
- `src/AeterniUI.Sample/Pages/Components/Toolbar.razor`：`/components/toolbar` 交互示例，目录由 ShowcaseCatalog 注册。
- `tests/toolbar.test.mjs`：无依赖 node:test DOM 焦点回归，运行 `node --test tests/toolbar.test.mjs`；不代替浏览器端到端验收。
