# AeterniUI Agent 指南

## 1. 前言

AeterniUI 是面向 Blazor Server 与 Blazor WebAssembly 的 .NET 10 组件库。本文件是 Agent 执行任务的首要入口，定义任务分类、定位与阅读顺序、工作边界和开发约束；项目索引与组件现状、后续计划、审阅任务及历史记录按需维护在本文档和 `docs/` 中。

发生冲突时按以下顺序判断：

1. 实际代码、自动化测试与 CI 检查。
2. 本文件中的工作边界和开发规范。
3. `docs/` 中对应领域的当前文档。
4. 历史发布和已完成批次记录。

同一事实只允许有一个维护位置：公共 API 与行为属于交付功能，未完成能力属于组件计划，已发现缺陷属于审阅计划，已经结束的版本和批次属于发布历史。

### 工作边界

- 用户可能正在运行示例应用；未经明确批准，不得终止其进程或执行破坏性清理。
- 保留工作区中与当前任务无关的用户修改，不顺手重构、重排或回退无关文件。
- 不提交 `bin/`、`obj/`、`dist/`、`.sample-publish/`、IDE 或 OS 生成文件。
- 不使用破坏性 Git 命令清理工作区；删除或覆盖前必须确认目标和任务范围。
- 功能未完成时，不得在交付功能或项目索引中表述为已实现。

## 2. 开工必读

本文件是“代理运行手册”，优先指导任务如何执行，而不是项目资料库索引。所有任务都应先读本文件，再按任务范围读取最小必要的文档，不为了“更全面”而一次性吞下全部历史资料。

代理执行顺序：
1. 先判断任务类型：修复、扩展、重构、文档/版本、审阅。
2. 先用搜索/符号定位准确文件和目标 API，不在未定位前做大范围读文件。
3. 读取目标组件及其共享能力的最小必要章节，而不是整篇文档全读。
4. 先实现与验证，再更新示例/文档，避免“修代码后才补说明”的脱节。
5. 汇报时给出证据：改动文件、验证命令和结果，而不是只说“已修复”。

| 任务 | 最小读取规则 |
| --- | --- |
| 修改现有组件、服务或公共 API | 先读本文件，再读 [`docs/delivered-features.zh-CN.md`](docs/delivered-features.zh-CN.md) 中对应章节；必要时再补充共享能力相关说明 |
| 新增组件或扩大公共能力 | 先读本文件；再读交付功能对应章节 + [`docs/component-plan.zh-CN.md`](docs/component-plan.zh-CN.md) 中相关计划 |
| 修复质量或一致性问题 | 先读本文件；再读 [`docs/review-plan.zh-CN.md`](docs/review-plan.zh-CN.md) 的相关批次与受影响组件章节 |
| 修改色板、玻璃配方、复杂焦点模型或特殊组件机制 | 先读本文件；再读 [`docs/engineering-reference.zh-CN.md`](docs/engineering-reference.zh-CN.md) 的对应章节 |
| 修改版本、整理发布或追溯历史决定 | 先读本文件；再读 [`docs/release-history.zh-CN.md`](docs/release-history.zh-CN.md) 对应版本记录 |

代理阅读原则：
- 先搜后看，先定位后读；不要在没有目标文件名时翻目录。
- 优先读目标文件和受影响测试，必要时再读共享基类/服务。
- 不读“所有文档”作为启动动作；不在本地多轮重复搜索。
- 对同一事实只保留一个维护位置：交付功能、计划、缺陷和历史记录不能互相冲突。

文档职责：

| 文档 | 唯一职责 |
| --- | --- |
| `AGENTS.md` | 前言、必读路由、项目索引和开发规范 |
| `docs/delivered-features.zh-CN.md` | 当前已经实现的公共 API、行为和实现边界 |
| `docs/component-plan.zh-CN.md` | 未完成组件、依赖、优先级、决策门和验收条件 |
| `docs/review-plan.zh-CN.md` | 当前审阅问题、证据、修复批次和验收条件 |
| `docs/engineering-reference.zh-CN.md` | 设计原理、色彩/玻璃配方、特殊机制和验证案例 |
| `docs/release-history.zh-CN.md` | 历史版本、已完成阶段、修复批次和发布验证 |

## 3. 项目索引

### 3.1 技术栈和版本

| 层次 | 技术和职责 |
| --- | --- |
| 组件库 | C#、.NET 10、Razor Class Library、Blazor Components |
| 示例宿主 | Blazor WebAssembly，目标框架 `net10.0` |
| 样式 | CSS isolation + `--aeterni-*` 语义 Token |
| 浏览器行为 | 原生 ES Module，由 `JsModuleManager` 管理 |
| 图标 | 核心 `Icon` / `AeterniIcons` + Font Awesome 适配包 |
| 前端工具 | 不使用 bundler；Node 只运行静态检查、行为回归和生成脚本 |

版本由根目录 `Directory.Build.props` 单一维护，当前为 `10.30.1`。第一位固定与目标 .NET 主版本一致；新增功能递增第二位，修复与优化递增第三位。

### 3.2 解决方案项目

解决方案入口为 `aeterni_ui.slnx`，包含四个项目：

| 项目 | 作用 |
| --- | --- |
| `src/AeterniUI` | 核心组件、服务、主题、Token 和组件 JS |
| `src/AeterniUI.Icons.FontAwesome` | Font Awesome `IconDefinition` 适配包 |
| `src/AeterniUI.Sample` | 可交互 Blazor WebAssembly 示例 |
| `tests/AeterniUI.ContractChecks` | 使用 `HtmlRenderer` 的最小组件渲染契约门禁 |

### 3.3 目录结构

```text
AeterniUI/
├── AGENTS.md
├── README.md
├── Directory.Build.props
├── aeterni_ui.slnx
├── docs/
│   ├── delivered-features.zh-CN.md
│   ├── component-plan.zh-CN.md
│   ├── review-plan.zh-CN.md
│   ├── engineering-reference.zh-CN.md
│   └── release-history.zh-CN.md
├── scripts/                         # 文档、CSS、对比度、图标和发布脚本
├── src/
│   ├── AeterniUI/
│   │   ├── Attributes/
│   │   ├── Components/
│   │   ├── Enums/
│   │   ├── Icons/
│   │   ├── Models/
│   │   ├── Modules/
│   │   ├── Services/
│   │   └── wwwroot/                 # Token CSS 与共享浏览器能力
│   ├── AeterniUI.Icons.FontAwesome/
│   └── AeterniUI.Sample/
│       ├── Components/Showcase/
│       ├── Layout/
│       ├── Pages/Components/        # 每个组件一个示例页
│       └── wwwroot/
├── tests/                            # Node 行为回归、契约检查和浏览器检查
└── .github/workflows/
```

组件采用一组件一目录：

```text
Components/<Component>/
├── <Component>.razor       # DOM、语义属性、事件绑定和渲染分支
├── <Component>.razor.cs    # 参数、状态、生命周期和 class/style 构建
├── <Component>.razor.css   # 组件隔离样式
└── <Component>.razor.js    # 必要的浏览器行为
```

### 3.4 核心入口

- `src/AeterniUI/Components/AeterniComponent.cs`：所有组件的公共基类。
- `src/AeterniUI/Components/ComponentClass.cs`：尺寸、颜色、Severity 与通用修饰类的唯一映射。
- `src/AeterniUI/Services/AeterniServiceCollectionExtensions.cs`：`AddAeterniUI()` 注册入口。
- `src/AeterniUI/Services/AeterniUIOptions.cs`：全局配置、默认品牌与文案表入口。
- `src/AeterniUI/Services/Impl/ThemeService.cs`：主题模式、实际主题与品牌状态。
- `src/AeterniUI/Services/Impl/DialogService.cs`：Dialog、Confirm、Alert 和 Toast 状态。
- `src/AeterniUI/Services/Impl/JsModuleManager.cs`：组件 JS module 的加载与释放。
- `src/AeterniUI/wwwroot/css/aeterni_ui.css`：Token、主题变量和必要主题别名；禁止组件选择器。
- `src/AeterniUI/wwwroot/js/aeterni_floating.js`：浮层定位、焦点陷阱、滚动锁和退出通知。
- `src/AeterniUI.Sample/Program.cs`、`App.razor`：示例宿主与路由入口。
- `src/AeterniUI.Sample/Layout/MainLayout.razor`：全局 `ThemeProvider` / `DialogProvider`。
- `src/AeterniUI.Sample/Components/Showcase/ShowcaseCatalog.cs`：示例组件目录的事实源。
- `tests/AeterniUI.ContractChecks/Program.cs`：根属性、ARIA、焦点和公开变体契约检查。
- `tests/browser/run.sh`：真实无头 Chrome 行为与布局检查。

### 3.5 常用命令

```bash
dotnet restore aeterni_ui.slnx
dotnet build aeterni_ui.slnx
dotnet run --project src/AeterniUI.Sample/AeterniUI.Sample.csproj --launch-profile http
dotnet run --project tests/AeterniUI.ContractChecks/AeterniUI.ContractChecks.csproj --no-build
bash scripts/check-docs.sh
node scripts/check-css-comments.mjs
node scripts/check-contrast.mjs
node scripts/check-doc-drift.mjs
node --check <改动的>.razor.js
node --test tests/*.test.mjs
tests/browser/run.sh
node scripts/generate-fontawesome-icons.mjs --check
./scripts/sample-publish.sh Release
```

示例默认地址在当前启动配置中通常为 `http://localhost:5178`，但若 `launch-profile`、端口或宿主配置变更，应以实际配置为准；不要把固定端口当作未变更事实。静态站点由 `sample-publish.sh` 生成到 `dist/`，不得手工修改。

## 4. 开发规范

### 4.1 开发流程

1. 先从交付功能确认目标能力是否已经存在，避免重复实现；如果已存在，先确认属于修正缺陷、扩展能力还是磨平文档漂移，再决定是否继续。
2. 先判断任务类别：现有行为修复、计划能力、审阅缺陷、文档/版本更新；随后只读取对应最小章节，而不是按顺序整篇翻阅。
3. 修改前确认根元素、公共 API、状态、Token、ARIA/键盘和 JS 边界；优先定位真正受影响的文件和测试，而不是从项目入口一路读到尾。
4. 只修改任务所需文件，公共行为变化同步示例和对应事实文档；不顺手重构、重排或回退无关文件。
5. 若改动涉及项目入口、宿主、目录、脚本、命令或示例页面，必须同步更新本文件的项目索引与常用命令，避免文档与代码脱节。
6. 在动手前先准备验证方式：最小命令、适配测试、真实渲染或契约检查；不要“写完再谈验证”。
7. 根据改动类型执行门禁；全部通过后才进入提交或发布。
8. 结束时给出证据：改动文件、验证命令、结果摘要，并说明是否还有未决项。

### 4.2 组件结构和基类

- 所有组件直接或间接继承 `AeterniComponent`。
- 有可见根元素的组件必须在根元素使用 `BuildAttributes()` 并绑定 `@ref="RootElement"`。
- class 从 `base.BuildClass()` 构建；动态 inline style 使用 `BuildStyle()`。
- 不重复生成 `id`、`class`、`style`、Guid 或第二套根属性合并逻辑。
- 一个根元素、一份内容标记；模式差异放在 class、属性或小范围渲染分支中。
- 根 class 使用 `aeterni-{component}`，变体使用 `aeterni-{component}--{variant}`，状态使用 `is-{state}`。
- 默认档位不输出无意义修饰类；组件发出的每个内部类名必须存在消费者。
- 非交互分支不得注册空事件处理器；禁用态使用状态 Token，不整块降低透明度。

### 4.3 公共 API

- 参数使用 PascalCase 和稳定领域术语：`Disabled`、`Visible`、`Size`、`Variant`、`Color`、`ChildContent`、`Value` / `ValueChanged` / `ValueExpression`。
- 有限稳定选项使用枚举；所有公开枚举在参数设置阶段校验非法值。
- 事件使用 `EventCallback` / `EventCallback<T>`，处理方法返回 `Task`，禁止 `async void`。
- 默认值必须让最简单用法可运行；不提供两个表达相同含义的参数。
- 公共 API 或行为变化必须同步 `delivered-features`、可交互示例和相关计划状态。

### 4.4 样式、Token 和主题

- `src/AeterniUI/wwwroot/css/aeterni_ui.css` 只允许 Token 选择器；组件样式必须放在自己的 `.razor.css`。
- 示例页不得用 `::deep` 或全局 `.aeterni-*` 规则覆盖组件原生样式。
- 只消费现有 `--aeterni-*` 语义 Token，不建立第二套颜色、尺寸、间距、圆角或动效体系。
- 组件内部间距使用 `--aeterni-spacing-*`；`padding/gap/margin` 别名留给宿主。
- 容器与页面背景只能是无彩色（R = G = B，深色允许不超过 +3 冷偏移）或统一玻璃配方；彩色只用于品牌/语意元素、交互状态和通知内容表面。
- 主题只能由 `ThemeProvider` / `ThemeService` 驱动；组件不得修改 `data-theme` / `data-aeterni-brand` 或自行读取系统主题。
- 跨组件外观归渲染该 DOM 元素的组件所有；父组件隔离样式不会自动作用于子组件根元素。

交互状态必须遵守：

1. 已勾选/已选中的实心控件在 hover/active 时使用 `--aeterni-state-background-checked-hover` / `-active` 整块换档，不用淡染洗浅。
2. hover/active 规则必须排除 invalid，不能覆盖危险色。
3. 普通 hover 规则必须排除 selected、checked、current 等已选中态。
4. 状态优先级默认是 `disabled > loading > invalid/selected > focus-visible > hover > base`。
5. `focus-visible` 必须有清晰焦点环；不得只移除 outline。

色板、玻璃、文字墨阶和特殊组件配方的推导依据见 `engineering-reference`；约束冲突时以本节为准。

### 4.5 HTML、ARIA 和键盘

- 优先使用正确的原生元素：动作使用 `button`，导航使用 `a`，输入使用原生表单元素。
- ARIA 只补充原生语义；带值的 ARIA 状态必须输出 `"true"` / `"false"` 字符串。
- 复合控件必须保证角色层级正确，并明确采用原生 Tab 顺序还是 roving tabindex。
- 单一复合控件通常只保留一个 Tab 停留点；不可聚焦选项不得进入 Tab 序列或抢走容器焦点。
- Tab、Enter、Space、Escape、方向键和 Home/End 只在组件语义要求时接管；处理后的按键必须阻止错误的页面滚动。
- 用户可见默认文案统一来自 `AeterniUIOptions.Text`，实例参数优先于全局文案表。

### 4.6 JavaScript

- 只有 DOM 测量、焦点控制、外部点击、滚动锁、Observer 或 CSS/Blazor 无法可靠完成的行为才使用 JS。
- 一个组件最多声明一个主 `JsModuleAttribute`；业务状态保留在 C#。
- 监听器、Observer、定时器和动画完成回调必须在实例级 `dispose(instanceId)` 中释放。
- 浮层定位、焦点陷阱、滚动锁与退出等待优先复用 `aeterni_floating.js`。
- JS 回调失败必须有降级路径，不得让页面永久失效。

### 4.7 示例和文档

- 示例采用一个组件一个页面，覆盖默认、主要变体、尺寸、禁用、错误、键盘和主题状态。
- 示例必须使用真实公共参数，不得用静态标记伪造尚未实现的能力。
- 现行 API 只写入 `delivered-features`；未完成任务只写入 `component-plan`；缺陷只写入 `review-plan`；结束的记录写入 `release-history`。
- 新增、删除、移动项目、目录、入口、宿主、脚本或命令时，同步本文件的项目索引。
- 设计原因和测量方法可以写入 `engineering-reference`，但不得在那里建立与本文件冲突的第二套规范。
- 对文档的写入必须遵守“单一事实源”规则：同一事实不能同时在交付功能、组件计划、审阅计划和发布历史中重复描述为相互冲突的状态。新增或修订文档时，先确认应归类到哪个事实源，再同步其版本头、状态和链接。
- 文档改动必须同步对应的示例、README 和 `scripts/check-doc-drift.mjs` 相关门禁；若某项能从源码直接推断，则不得仅靠历史摘要叙述承诺。
- 任何“已修复/已发布/已实现”的表述，必须有对应源码、示例或验证结果支撑；没有证据的记录必须保留为“计划”或“待验证”。

### 4.8 验证方法

写断言前必须先核对被测量的内容在真实渲染中确实以该形式出现：确认 Token 有消费者、属性用途正确、伪类或状态实际命中。Node DOM 替身、计算样式、浏览器像素和 C# 渲染契约各自只证明其覆盖范围，不得互相冒充；前端行为验证不能仅依赖替身或静态字符串，而应在真实渲染/浏览器环境中确认。

按改动类型执行门禁：

```bash
dotnet build aeterni_ui.slnx                    # 所有代码改动
dotnet run --project tests/AeterniUI.ContractChecks/AeterniUI.ContractChecks.csproj --no-build
                                                   # 根属性、ARIA、焦点或公开变体
bash scripts/check-docs.sh                     # 文档、结构、入口或命令
node scripts/check-css-comments.mjs            # 任意 CSS
node scripts/check-contrast.mjs                # 色阶、品牌或对比度相关 Token
node scripts/check-doc-drift.mjs               # 交付功能或文案表
node --check <改动的>.razor.js                 # 组件 JS
node --test tests/*.test.mjs                   # 浏览器行为
tests/browser/run.sh                           # 层叠、时序、焦点环、键盘焦点模型
node scripts/generate-fontawesome-icons.mjs --check
                                                   # 图标清单或组件字形
```

### 4.9 版本与提交纪律

- 第一位固定为当前 .NET 主版本 `10`；不得把它当作普通 SemVer 主版本递增。
- 功能新增递增第二位；修复与优化递增第三位；内部重构按是否改变公共行为判断。
- 修改版本时同步 `AGENTS.md` 的当前版本、带文档版本头的当前文档、README tag 示例和 `release-history`，随后运行 `bash scripts/check-docs.sh`。
- 提交前使用 `git diff --name-status` 核对职责文档、示例、测试和生成目录边界。
