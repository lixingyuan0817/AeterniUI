# AeterniUI 当前已完成功能

文档版本：`10.2.0`

文档状态：当前实现清单；本文档是当前已实现公共 API 和行为的唯一事实源。

本文档用于记录当前组件库已经落地的能力，作为示例项目、后续组件开发和 API 设计的基线。未列出的功能不应被视为已经稳定提供。

## 文档口径

组件章节统一按以下顺序描述：支持能力、公共参数/事件、行为与无障碍、实现边界、基础用法（仅在有代表性时提供）。未适用的小节不强行添加。

- 本文档以当前源码中的公开组件、`[Parameter]`、`EventCallback`、公开服务和配置类型为准；实现细节不等同于公共 API。
- 所有组件都继承 `AeterniComponent`。`Id`、`Disabled`、`Visible`、`Class`、`Style`、`Element`、`AdditionalAttributes` 和 `ElementChanged` 是基类公共参数；下文只在某个组件实际处理该参数时重复说明。
- 基类参数始终可传入，但 `Disabled` 只有实现了相应语义的组件才会渲染禁用状态，不能据此推断任意容器都支持原生禁用。
- `ListItem.Selected`、通知卡片等内部状态不是公开参数；规划中的能力只记录在 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md)，不会写入已完成功能。

## 1. 宿主支持

- 支持 Blazor Server 使用组件库。
- 支持 Blazor WebAssembly 使用组件库。
- 示例项目 `AeterniUI.Sample` 为 Blazor WebAssembly 项目。
- 已配置 Tauri + Blazor WebAssembly 开发模式。
- Tauri 开发模式直接加载发布到仓库根目录 `dist/` 的静态站点（`scripts/sample-publish.sh` 负责发布），不再是 `dotnet watch` 热重载；使用透明窗口、原生窗口阴影和 macOS Vibrancy 配置。
- 在 Tauri 宿主中，原生窗口背景（macOS vibrancy / Windows acrylic-blur）会随页面明暗主题切换色调；该能力由示例宿主层（`index.html` + `js/host-backdrop.js` + Tauri 命令 `apply_window_backdrop`）实现，组件库保持宿主无关。
- 示例项目包含文档首页（路由 `/`，品牌介绍与基础使用代码窗口）和组件页（路由 `/components`，左侧分类导航加真实组件交互画廊，展示按钮、分组、表面、输入、选择、通知与主题切换）。固定头部与「首页 / 组件」菜单由 `MainLayout` 承载。
- 组件 API 不依赖具体宿主，浏览器能力通过 JS isolation 提供。

## 2. 设计基础

- 使用统一的 `AeterniComponent` 作为组件基类。
- 基类提供自动生成的实例 ID 和可覆盖的 `Id`。
- 支持 `Class`、`Style`、`AdditionalAttributes`、`Visible`、`Disabled`、`Element`（`ElementReference`）和 `ElementChanged`。
- 提供 `ClassBuilder` 和 `StyleBuilder`，组件可以在代码后置中组合 class 和 style。
- 支持 `ElementChanged` 回调。
- 支持组件级 `.razor.css` CSS isolation。
- JS module 可选，不添加 JS 文件的组件不会执行 JS 初始化。
- 一个组件最多维护一个主 JS module，JS module 支持初始化、调用和释放生命周期。
- 组件库使用统一的语义化 Token，组件内部不重新建立独立颜色体系。

## 3. Token 和主题

- 提供品牌色、语意色、背景色、文字色、边框色、阴影、圆角、间距、字号和动效 Token。
- 提供 Light 和 Dark 两套主题 Token。
- 品牌色与 `Info`、`Success`、`Warning`、`Danger`、`Neutral` 语意色均提供 `default`、`hover`、`active`、`disabled`、`soft` 状态别名；组件无需直接选择色阶。
- 提供统一控件状态 Token：`--aeterni-state-background-*`、`--aeterni-state-color-*` 和 `--aeterni-state-border-*`，其中 `selected` 与 `checked` 使用同一套表面状态。
- 提供常用状态别名：`pressed`（等同 `active`）、`invalid`、`readonly`、`placeholder`、`muted` 和 `inverse`，用于表单反馈、只读内容和反色内容的一致表达。
- 提供通用控件组合别名：`--aeterni-control-background-*`、`--aeterni-control-border-*`、`--aeterni-control-foreground-*` 和 `--aeterni-control-focus-ring`，方便组件直接组合控件状态。
- 原始色阶（例如 `--aeterni-brand-500`、`--aeterni-info-600`）继续保留，用于自定义主题或特殊视觉需求。
- 提供浮层与紧凑表面度量 Token：`--aeterni-overlay-*`（对话框、下拉列表和浮层的宽高）、`--aeterni-row-height-compact`、`--aeterni-control-size-*`、`--aeterni-badge-size-md` 和 `--aeterni-width-control-md`。
- 提供可发现滚动条 Token：`--aeterni-scrollbar-size`、`--aeterni-scrollbar-thumb`、`--aeterni-scrollbar-track`；长选项列表和长通知堆栈使用细滚动条而不是隐藏滚动条。
- 提供控件圆角阶梯 `--aeterni-radius-control-sm/md/lg` 与 `--aeterni-radius-button*`、`--aeterni-radius-input*`、`--aeterni-radius-surface`；同一尺寸档位的 Button、Input 和 Textarea 圆角一致。
- 实心语意表面使用专用前景 Token：`--aeterni-color-on-semantic` 及其 `-strong` 档，用于 success/warning/danger/info 实心控件的正文，保证浅色与深色主题下对比度 ≥ 4.5:1。
- 开关类控件的指示块使用 `--aeterni-state-background-thumb`，在浅色与深色轨道上都保持可辨识。
- 焦点环使用 `--aeterni-focus-color`，无效控件使用 `--aeterni-focus-color-invalid`；`Input` 与 `Textarea` 也消费这两组 Token。
- 字体栈中的 Inter 与 JetBrains Mono 是可选的宿主依赖，库不随包提供 webfont；宿主未提供时回落到系统 UI 字体（等宽回落 Consolas / Courier New）。
- 无显式主题时，`@media (prefers-color-scheme: dark)` 提供系统偏好兜底，避免深色偏好用户在脚本执行前看到浅色闪烁；一旦存在显式 `data-theme` / `data-aeterni-mode`，该兜底不再生效。
- 支持系统主题跟随。
- 主题切换由 `ThemeService` 管理。
- `ThemeProvider` 为自闭合组件，不需要包裹 Layout 内容。
- `ThemeProvider` 负责注入主题 JS、监听系统主题变化、同步页面主题和 Tauri titlebar 主题。
- `ThemeProvider` 不渲染 DOM：基类的 `Id`、`Class`、`Style` 和 `Visible` 对它无效，主题只写到 `<html>` 上。
- 推荐在样式表之前放置预渲染主题脚本（读取 `aeterni.theme.mode` 与 `prefers-color-scheme`，写入 `data-theme`），否则深色偏好用户会在 Blazor 启动前看到浅色；示例 `wwwroot/index.html` 已包含该脚本，可直接复制。
- 页面主题通过 `<html data-theme>` 输出；Tauri 示例宿主监听该属性并调用 `apply_window_backdrop`，让原生窗口背景模糊/色调跟随 System、Light、Dark 三种模式。
- `ThemeSwitch` 提供 System、Light、Dark 分段切换，并能在刷新后正确反映当前模式。
- 主题切换包含过渡动画，并适配 reduced-motion 场景。

- `ThemeService.Mode` 的类型是 `ThemeMode`（`System` / `Light` / `Dark`）；`CurrentTheme` 的类型是 `ThemeKind`（`Light` / `Dark`），表示实际生效的主题。
## 4. Button

### 支持能力

`Button` 当前支持：

- `Variant`：`Default`、`Outline`、`Ghost`、`Text`、`Link`。
- `Color`：`Default`、`Primary`、`Neutral`、`Success`、`Warning`、`Danger`、`Info`。
- `Size`：`Small`、`Default`、`Medium`、`Large`；`Default` 和 `Medium` 使用同一默认中等尺寸。
- `Type`：`Button`、`Submit`、`Reset`。
- `StartIcon`、`EndIcon`，以及兼容旧 API 的 `Icon`。
- `ChildContent`、`Loading`、`Disabled`、`FullWidth`。
- `AriaLabel`、`OnClick`。
- 默认、悬浮、按下、聚焦、禁用和加载状态。
- 实心语义色（`Success`、`Warning`、`Danger`、`Info`）使用 `--aeterni-color-on-semantic` 前景；`Primary` 与 `Default` 共用基础品牌规则（`--primary` 与基础选择器同组）。
- 默认档位不输出修饰类：`Size.Default` / `Size.Medium` 与 `Color.Default` 不再生成 `aeterni-button--md` / `aeterni-button--primary` 之外的空类名。
- 禁用状态统一消费 `--aeterni-state-*` Token，保持所有颜色和变体的前景、背景可读；`Outline` 和 `Ghost` 仅保留各自的边框结构，`Link` 禁用时不会显示下划线。
- 加载状态包含 spinner、`aria-busy` 和禁用交互。遮罩对所有变体使用同一套处理：自身表面的半透明层 + `blur(2px)`，因此文字会被模糊但仍然可见，实心变体保持自身语意底色、透明变体保持自身语意色描边。遮罩向外扩展一个边框宽度（`inset: calc(var(--aeterni-border-width) * -1)`），把控件自身的边框也盖在模糊层之下；Button 因此不再对子元素做 `overflow` 裁剪。
- Link 变体悬浮时只显示下划线，不显示背景色。

### 行为与无障碍

使用原生 `<button>` 语义，`Type`（`ButtonType`：`Button` / `Submit` / `Reset`）控制原生类型；`Disabled` 输出原生 `disabled` 与 `aria-disabled`，`Loading` 期间阻止重复提交并保持尺寸稳定；键盘使用原生 Enter/Space 激活，`:focus-visible` 提供清晰焦点环。

### 实现边界

不提供独立 `IconButton`；图标通过 `Icon`（兼容简写）、`StartIcon` 和 `EndIcon` 传入。
## 5. ButtonGroup

### 支持能力

`ButtonGroup` 当前支持：

- `ChildContent`、`Orientation`（`Horizontal` / `Vertical`）、`Connected`、`FullWidth` 和 `AriaLabel`。
- 横向和纵向排列，以及连体按钮和独立间距两种模式。
- 通过级联上下文向子 Button 传递禁用状态。
- 统一处理按钮之间的边框和分隔关系。

### 行为与无障碍

`Disabled` 通过级联上下文传递给内部 Button，而不是只在容器上做视觉置灰；保留原生 Tab 顺序，不实现方向键导航；连接模式下相邻按钮之间保留 1px 分隔线，分隔线取相邻按钮自身前景色的 32% 混合（透明变体改用 `--aeterni-border-strong`），因此在品牌色、中性色和语意色实心按钮上都清晰可辨。

### 实现边界

不提供 Toolbar / ToggleGroup 语义（两者均未实现）。
## 6. Surface 和 Card

### 支持能力

`Surface` 当前支持：

- `Variant`（`Default` / `Subtle` / `Elevated` / `Glass`）。
- `Elevation`（`None` / `Small` / `Medium` / `Large`）。
- `Padding`（`None` / `Small` / `Medium` / `Large` / `ExtraLarge`）。
- `Radius`（`Default` / `None` / `Small` / `Medium` / `Large` / `ExtraLarge` / `Round`）。
- `Bordered`。
- `FullWidth`。
- 任意 `ChildContent`。

`Card` 基于 Surface 风格提供：

- Header、Body 和 Footer 三个内容区域。
- `Header`、`ChildContent`、`Footer`，以及 `Variant`、`Elevation`、`Padding`、`Bordered` 和 `FullWidth`。
- `Interactive` 和 `OnClick`。
- `AriaLabel`。
- 可点击状态的悬浮和按下反馈。
- 三个内容区域使用统一的内边距和边界关系。

参数类型分别为 `SurfaceVariant`、`SurfaceElevation`、`SurfacePadding` 和 `SurfaceRadius`；`Card` 复用前三个，没有独立的 `Radius` 参数。

两者共享同一套“选项 → 语意 Token”映射（变体、高度和内边距的每条规则指向相同 Token，评审脚本可逐条比对），默认值则有意不同：`Card` 是结构化容器（圆角 `--aeterni-radius-card`，默认带边框），`Surface` 是通用包装（圆角 `--aeterni-radius-surface`，默认无边框且支持 `Radius`）。默认档位不输出修饰类（`Variant=Default`、`Elevation=None`、`Radius=Default` 均不生成空类名）。

### 行为与无障碍

`Surface` 与 `Card` 默认不是交互控件，不输出按钮或链接语义；只有 `Card.Interactive = true` 时才输出按钮语义、Tab 焦点和 Enter/Space 触发，此时内部不要再嵌套可聚焦控件。

### 实现边界

两者都不做定位、滚动或动画；内容区域由业务内容自行组织。
## 7. Icon、内置图标集和 Font Awesome

### Icon 组件

- 提供 `Icon` 组件。
- `Definition`（必填 `IconDefinition`）、`Size`、`Color`、`AriaLabel` 和 `Title`。
- 图标尺寸和颜色使用组件库 Token。
- 未传 `AriaLabel` 时输出 `aria-hidden="true"`；传入后输出 `role="img"` 和可访问名称。
- 图标库通过 RenderFragment 与 Button、Dialog、Toast 等组件组合使用。

### 核心内置图标集 `AeterniIcons`

- 位于 `AeterniUI/Icons/AeterniIcons.cs`，是组件库自己的几何定义（16 × 16 网格），不依赖任何图标供应商。
- 提供 `Check`、`ChevronDown`、`ChevronRight`、`Xmark`、`Star`、`Info` 和 `Exclamation`。
- 组件内部统一通过它渲染字形，替换了此前散落在组件中的内联 SVG 和文本字符：
  - `Checkbox` 勾选标记；`ComboBox` 下拉箭头；`Rating` 星形。
  - `Menu` 分组折叠指示使用 `ChevronRight`，展开时旋转 90° 变为向下，避免 180° 翻转在中间帧退化成横线。
  - `Dialog` 关闭按钮、`Tag` 关闭按钮统一使用 `Xmark`。
  - Alert / Toast 未传 `Icon` 时按 `Severity` 使用 `Check`、`Exclamation`、`Xmark` 和 `Info`。
- 业务代码可以直接复用这些定义，也可以传入自己的 `IconDefinition`。

### `AeterniUI.Icons.FontAwesome` 适配包

- 提供 Font Awesome Free 7.3.1 Classic Solid 精选集，当前 343 个图标，分为基础操作、导航与方向、表单与数据、状态与反馈、媒体与自然、代码与开发、业务与场景七组。
- 图标以 `FontAwesomeIcons.Solid.<Name>` 类型化属性暴露，另提供 `FontAwesomeIcons.Categories`（分组，供选择器和文档使用）和 `FontAwesomeIcons.TryGet(name, out definition)`（按名称解析）。
- `FontAwesomeIcons.cs` 由 `scripts/generate-fontawesome-icons.mjs` 生成，清单中的每个名称都会对照官方 npm 包校验，重命名或缺失会直接报错。
- 适配包不引入 Font Awesome 的 CSS、字体或 JavaScript 运行时；核心库不直接依赖具体图标供应商。

## 8. Input

### 支持能力

`Input` 当前支持单行文本输入：

- `Value`、`ValueChanged`、`ValueExpression`，兼容 Blazor 标准绑定方式。
- `Type`：`Text`、`Password`、`Email`、`Search`、`Tel`、`Url`。
- `Size`：`Small`、`Default/Medium`、`Large`。
- `Placeholder`、`Name`、`AutoComplete`、`InputMode`、`Pattern`、`MinLength` 和 `MaxLength`。
- `Disabled`、`ReadOnly`、`Required`、`Invalid` 和 `FullWidth`。
- `OnInput`、`OnChange`、`OnFocus`、`OnBlur` 和 `OnKeyDown` 事件回调。
- `AriaLabel` 和 `AriaDescribedBy` 无障碍属性。
- 在 `EditForm` 中通过 `ValueExpression` 读取 `EditContext` 的验证消息，并同步 `aria-invalid`。
- 默认、悬浮、聚焦、禁用、只读和无效状态，尺寸变化不会改变输入的基本语义。
- 悬浮与聚焦均使用主题色边框；`:focus-visible` 额外绘制全库统一的焦点环（`--aeterni-focus-width` + `--aeterni-focus-color`，文本输入在鼠标聚焦时同样匹配 `:focus-visible`）。
- 无效状态在悬浮、聚焦下始终保留危险色边框，焦点环改用 `--aeterni-focus-color-invalid`，错误提示不会被焦点环覆盖。
- `Small` / `Large` 档位同步切换圆角阶梯，保持与同档 Button 一致。

### 行为与无障碍

`Input` 是单行原生 `<input>` 封装；标签、帮助文本、错误文本布局由独立的 `FormField` 组合，不属于 `Input` 自身的渲染职责。

### 基础用法

```razor
<Input @bind-Value="UserName"
       Type="InputType.Email"
       Required
       Placeholder="name@example.com"
       AriaLabel="Email address" />
```

### 实现边界

不提供日期时间选择、掩码输入和异步校验；多行输入使用 `Textarea`。
## 9. Textarea

### 支持能力

`Textarea` 提供多行原生文本输入，复用 `Input` 的绑定、校验和表单级联约定。

- `Value`、`ValueChanged`、`ValueExpression`，兼容 Blazor 标准 `@bind-Value`。
- `Placeholder`、`Name`、`AutoComplete`、`Rows`、`Resize`（`TextareaResize`）、`MinLength` 和 `MaxLength`。
- `Disabled`、`ReadOnly`、`Required`、`Invalid` 和 `FullWidth`。
- `OnInput`、`OnChange`、`OnFocus`、`OnBlur` 和 `OnKeyDown` 事件回调。
- `AriaLabel` 和 `AriaDescribedBy` 无障碍属性。

### 行为与无障碍

使用原生 `<textarea>` 语义；通过 `ValueExpression` 接入 EditContext 校验，并同步 `aria-invalid`、`aria-required`、`aria-readonly`、`aria-disabled` 和 `aria-describedby`。`Resize` 支持 `None` 和 `Vertical`，默认允许垂直调整大小。聚焦时绘制与 `Input` 相同的焦点环，无效状态下焦点环使用危险色。

### 实现边界

当前不提供富文本编辑、自动高度和异步校验。

## 10. FormField

### 支持能力

`FormField` 提供表单控件的统一标签、描述和错误信息布局，公开 `Label`、`Description`、`Error`、`ChildContent`、`Required`、`Invalid` 和 `For` 参数，并继承基类的 `Disabled`。`Label` 提供独立的原生 `<label>` 组件，支持 `For` 参数和 `ChildContent`。

### 行为与无障碍

显式 `Error` 优先于 `EditContext` 验证消息，并通过稳定 ID 建立 label、描述文本、错误文本与控件之间的关联。FormField 会通过级联上下文向内部表单控件传递输入 ID、label ID、`aria-describedby`、禁用、必填和验证状态。

`Input`、`Textarea`、`Checkbox`、`Switch` 与独立的 `Radio` 会直接采用该输入 ID，`ComboBox`、`Rating` 和 `RadioGroup` 则是容器型控件：它们采用输入 ID 让 `label for` 仍然可解析，并通过 `aria-labelledby` 关联标签、通过 `aria-describedby` 关联描述与错误文本。

### 实现边界

FormField 负责布局和语义关联，不替代内部控件的值绑定或输入行为。

## 11. Label

### 支持能力

`Label` 渲染原生 `<label>`，用于把可见文字与表单控件关联起来。

- `ChildContent`：标签文字或内容。
- `For`：目标控件的 DOM id，输出到 `for` 属性。
- 继承 `AeterniComponent` 的 `Id`、`Class`、`Style`、`Visible` 等公共参数。

### 行为与无障碍

`For` 与控件的 `Id` 一致时，点击标签即可聚焦或切换对应控件（原生行为，无需 JS）。
`FormField` 已内置标签渲染，只有在需要独立标签或自定义排布时才单独使用 `Label`。

### 实现边界

不提供必填标记、帮助文本或错误文案，这些属于 `FormField`。
## 12. Checkbox

### 支持能力

`Checkbox` 提供原生复选框语义和稳定的布尔值绑定，使用 `<label>` 包裹原生 `<input type="checkbox">`，可被辅助技术直接识别。

- `Value`、`ValueChanged`、`ValueExpression`，兼容 Blazor 标准 `@bind-Value`。
- `Indeterminate` 仅作为显示状态；用户交互后组件落定到明确的 `true`/`false` 值，不保留不确定状态。
- `ChildContent` 作为标签文本，点击整行即可切换。
- `Name`、`AriaLabel`、`AriaDescribedBy`。
- `Size`：`Small`、`Default`、`Medium`、`Large`；尺寸盒体消费 `--aeterni-control-size-*`（Small 16px、默认/Medium 20px、Large 26px），与 `Radio` 使用同一套档位。
- `Required`、`Invalid`、`Disabled`。
- `OnChange` 回调，并在 `EditForm`/`EditContext` 中通过 `ValueExpression` 校验，同步 `aria-invalid`。
- 支持默认、悬浮、聚焦、禁用和无效状态；三态显示通过原生 `:indeterminate` 呈现。
- `indeterminate` 是 DOM 属性而非 HTML 属性，组件使用一个最小 JS module 将其设置到输入框；不设置 `Indeterminate` 时不产生任何浏览器副作用。

### 行为与无障碍

组件通过 `FormField` 级联上下文同步稳定的输入 ID、`aria-describedby` 和验证状态；禁用时不触发值变更。

### 实现边界

`Indeterminate` 只表示显示状态，用户交互后会落定为明确的 `true` 或 `false`，不提供独立的三态值模型。

### 基础用法

```razor
<Checkbox @bind-Value="Subscribe">Subscribe to updates</Checkbox>
<Checkbox Indeterminate>Select all</Checkbox>
```

## 13. Radio / RadioGroup

### 支持能力

- `RadioGroup<TValue>` 提供 `Value`、`ValueChanged`、`ChildContent`、`Name`、`Size`、`Orientation`（`Horizontal` / `Vertical`）、`Disabled`、`Required`、`Invalid` 和 `AriaLabel`。
- `Radio<TValue>` 提供 `Value`、`ValueChanged`、`ValueExpression`、`ChildContent`、`Name`、`Size`、`Required`、`Invalid`、`AriaLabel` 和 `AriaDescribedBy`；`Size` 为空时继承分组值。
- 使用原生 `<input type="radio">` 与 `fieldset` 分组语义，分组输出 `role="radiogroup"` 和与布局一致的 `aria-orientation`。
- 视觉与 `Checkbox` 对齐：同一 `--aeterni-control-size-*` 档位、1px 边框、选中态用内圆点而不是加粗边框，并补齐悬浮、无效、禁用与“禁用 + 选中”状态。
- 根元素是 `<label>`（与 Checkbox、Switch 一致）：组件类与状态类（`aeterni-radio`、`--sm/--lg`、`is-checked`、`is-disabled`、`is-invalid`）落在根标签上，原生输入通过独立的输入属性集合渲染，因此尺寸与状态样式始终作用于可视圆圈。

### 行为与无障碍

单选值通过 RadioGroup 级联上下文统一绑定；禁用、必填、无效和尺寸档位会传递到选项，并输出相应的原生/ARIA 语义。垂直布局时容器切换为单列，并由 `aria-orientation="vertical"` 同步表达。

位于 `FormField` 中的独立 `Radio` 会采用字段的输入 ID（点击标签可直接聚焦），`RadioGroup` 则让 fieldset 采用该 ID 并用 `aria-labelledby` 关联字段标签。

### 实现边界

当前不提供远程选项、虚拟化和多选行为；方向键导航沿用原生 radio 行为。

## 14. Switch

### 支持能力

`Switch` 提供适合即时开关设置的二态控件：

- `Value` / `ValueChanged` / `ValueExpression`，支持 `@bind-Value`，内部使用可聚焦的原生 checkbox，并输出 `role="switch"` 与 `aria-checked`。
- 支持 `ChildContent`、`Name`、`Required`、`Invalid`、`Disabled`、`AriaLabel`、`AriaDescribedBy` 与 `OnChange`；不提供独立的 `Label` 参数。
- 可从 `FormField` 级联获取标签关联、禁用与校验状态。
- 空格/原生 checkbox 行为可切换；reduced-motion 下关闭滑块过渡动画。

### 行为与无障碍

内部使用可聚焦的原生 checkbox，并输出 `role="switch"` 与 `aria-checked`；禁用时不可切换，也不会触发值变更。

### 实现边界

Switch 不提供独立的 `Label` 参数；标签内容使用 `ChildContent`，复杂标签关联由 `FormField` 负责。

## 15. Tag

### 支持能力

`Tag` 提供分类、状态与筛选的紧凑标签：

- `ChildContent`、`StartIcon`、`EndIcon`、`Color`（沿用 Button 语意色）、`Size`、`Variant`（`TagVariant`：`Default` / `Soft` / `Outline`）。
- 可选 `StartIcon` / `EndIcon` 与 `Dismissible`、`OnDismiss`、`DismissLabel`；关闭按钮复用 `Button` 的图标能力。
- 可关闭 Tag 的关闭按钮带有可访问名称、只触发一次事件，热区通过透明伪元素扩展到 `--aeterni-touch-target-min`（24×24，满足 WCAG 2.5.8）且不改变 Tag 高度。
- `Size` 档位基于 `--aeterni-height-xs` 上下各取一档（`--sm` 20px、默认 24px、`--lg` 28px），不再使用 10px 字号等低于 Token 下限的裸值。

### 行为与无障碍

非可关闭 Tag 保持展示语义；可关闭 Tag 使用带可访问名称的 Button 关闭动作，且关闭事件只触发一次。

### 实现边界

不提供分组、折叠或拖拽；`Dismissible` 只触发 `OnDismiss`，是否从集合中移除由业务决定。
## 16. List + ListItem

### 支持能力

- `List` 提供 `ChildContent`、`SelectionMode`（无选择 / 单选 / 多选）、`SelectedValue`/`SelectedValues` 与 `SelectedValueChanged`/`SelectedValuesChanged`、`AllowClear`、`AriaLabel` 与 `OnItemSelected`。
- 选择模式下容器输出 `role="listbox"` 与 `aria-multiselectable`；交互式 `ListItem` 渲染非可聚焦的 `role="option"` 元素并同步 `aria-selected`、`aria-disabled`。
- `ListItem` 支持 `Value`、`ChildContent`、继承的 `Disabled`、`LeadingContent` / `TrailingContent`；`Selected` 是由 `List` 计算的内部状态，不是可设置参数。
- `AllowClear` 只影响单选模式；多选模式通过再次选择已选项移除该项。
- 支持鼠标、方向键、Home/End 与空格/回车选择；禁用项不可选择。
- 无障碍：容器保持 `role="listbox"` 单点 Tab 聚焦，并用 `aria-activedescendant` 指向当前高亮选项；选项本身带稳定 id、不进 Tab 序列也不接收 DOM 焦点，由容器键盘统一驱动，当前项用 `is-active` 样式提示。
- 无选择模式渲染为普通列表结构，不输出按钮语义。

### 行为与无障碍

选择模式下容器负责单点 Tab 聚焦和键盘导航，禁用项不可选择；无选择模式不伪造按钮或 listbox 交互语义。

### 实现边界

当前不提供拖拽、虚拟化、分组和异步数据源。

## 17. Rating

### 支持能力

- 整数评分：`Value` / `ValueChanged` / `ValueExpression`、`OnChange`，`Max`（默认 5）与越界钳制。
- 支持 `ReadOnly`、`Disabled`、`AllowClear`（再次点击当前值清零）与 `Icon` 自定义（缺省使用内置 `AeterniIcons.Star`）。
- 按 `radiogroup` / `radio` 语义输出，支持方向键与 Home/End；`AriaLabel` 缺省取 `AeterniUITextOptions.RatingLabel`。
- 每颗星只有当前值 `aria-checked="true"`（“已填充”视觉与“已选中”语义分离），并使用 roving tabindex：只有当前值（未选中时为第一颗）在 Tab 序列内，一次 Tab 即可进出。
- 只读或禁用时把当前值并入分组可访问名称（`aria-label`），自定义 `Icon` 与内置星形使用同一尺寸（`--aeterni-icon-size-lg`）。
- 接入 `EditContext` 校验（`ValueExpression`），无效状态输出 `aria-invalid` 并可在 `FormField` 中级联。

### 行为与无障碍

Rating 使用 radiogroup/radio 语义，支持方向键、Home/End 和当前值播报；只读或禁用时不改变值。

### 实现边界

当前只支持整数评分，不实现半星。

## 18. ComboBox

### 支持能力

- 泛型 `ComboBox<TItem>`，默认下拉选择控件（不含自由输入搜索）。
- `Items`、`Value`/`ValueChanged`、`TextSelector`、`ItemTemplate`、`EmptyContent`。
- `Placeholder`、`Required`、`Invalid`、继承的 `Disabled`、`AriaLabel` 和 `OnChange`；未提供时 `Placeholder` 取 `AeterniUITextOptions.ComboBoxPlaceholder`、选项列表名取 `AeterniUITextOptions.ComboBoxListLabel`；接入 `EditContext` 校验（`ValueExpression`）并在 `FormField` 中级联。
- 点击触发按钮弹出选项；打开后支持上下方向键、Home/End、Enter 确认；点击外部、Escape 或页面滚动（弹层内部滚动除外）都会关闭，行为接近原生 select。
- `role="combobox"`、`aria-expanded`、`aria-controls` 与选项同步；打开时触发器通过 `aria-activedescendant` 指向高亮项，无效时输出 `aria-invalid`。
- 选项渲染为非可聚焦的 `role="option"` 元素：打开后焦点始终留在触发器上，Tab 不会进入选项列表，键盘提示由 `is-active` 与 `aria-activedescendant` 表达。
- 触发器采用 `FormField` 的输入 ID，并用 `aria-labelledby` / `aria-describedby` 关联字段标签、描述与错误文本。
- 长选项列表使用细滚动条（`--aeterni-scrollbar-*`）而不是隐藏滚动条，滚动提示可见且仍可用鼠标滚轮滚动。
- 弹层由最小 JS module（`ComboBox.razor.js`）按触发按钮锚定为 fixed 定位并自动上下翻转/贴边，避免被卡片/容器裁剪遮挡。

### 行为与无障碍

ComboBox 的触发器保持 combobox 语义；打开后支持方向键、Home/End、Enter 和 Escape，外部点击或页面滚动会关闭选项。

### 实现边界

当前不支持自由输入筛选、远程搜索、虚拟化、无限滚动和多选。

## 19. Progress

### 支持能力

`Progress` 提供线性进度展示，支持 `Value`、`Max`、`Indeterminate`、`Color`、`Size`、`ShowValue` 和 `AriaLabel`。

### 行为与无障碍

输出 `role="progressbar"`、`aria-valuemin`、`aria-valuemax`，确定进度时输出 `aria-valuenow`；不确定状态使用 CSS 动画，并在 reduced-motion 下停止动画。

### 实现边界

当前提供线性进度，不包含环形渲染、上传任务管理和远程数据源。

## 20. PopupHost / Popover

### 支持能力

`PopupHost` 提供浮层内容挂载容器；`Popover` 支持 `Open`、`Header`、`ChildContent`、`Modal` 和 `AriaLabel`。

### 行为与无障碍

Popover 根据 `Modal` 输出 `dialog` 或 `region` 语义，关闭时通过 `hidden` 移除可见内容；`Modal` 同时把浮层提升到模态层级（`--aeterni-z-modal` 与模态阴影）。

### 实现边界

仍是**基础版**：`PopupHost` 现在是真实的定位容器（`position: relative` 的正常盒子），但库不提供锚点定位、翻转/贴边、遮罩、焦点陷阱、Escape 与点击外部关闭。需要这些能力时由使用方在 `PopupHost` 内自行定位，或等待 v0.3 的共享浮层能力（见 `component-roadmap.zh-CN.md` 第三阶段前置能力）。`Modal` 目前只切换 `role` 与层级，不提供模态交互行为。

## 21. Menu

`Menu` 提供可复用的分组导航菜单，支持 `Items`、`SelectedId`、`Accordion`、`OpenKeys` 和 `AriaLabel`。

### 数据模型

- `MenuGroup`：`Key`、`Label`、`Items`、`Icon`、`InitiallyOpen`、`Disabled`、`Visible`。
- `MenuItem`：`Id`、`Label`、`Description`、`Icon`、`Disabled`、`Href`、`Target`、`Visible`。
- 标签与 `Description` 在同一行显示，空间不足时按需省略（`…`），因此分组开关与菜单项保持同高（`36px`）；菜单本身不会因父容器更高而拉伸行高。
- 设置 `Href` 的菜单项渲染为真实 `<a>`（保留中键新标签页、浏览器历史与链接语义），`Target="_blank"` 会自动带上 `rel="noopener noreferrer"`；未设置 `Href` 或项被禁用时渲染为原生 `<button>`。
- `Href` + `Target` 由规范 §7.1“导航使用 `<a>`”约束；链接项仍会触发 `OnItemSelected`，只是导航交给浏览器。

### 展开状态

- 默认非受控：组件自行维护展开分组，初始渲染最多展开一个 `InitiallyOpen` 分组（`Accordion`），选中项所在分组优先打开；`Items` 被替换时清理已不存在的分组。
- 传入 `OpenKeys` 后变为受控：所有变更通过 `OpenKeysChanged` 回传（支持 `@bind-OpenKeys`），组件不再自行修改状态。
- 分组折叠/展开后触发 `OnGroupToggled`；菜单项激活后触发 `OnItemSelected`（旧名 `ItemSelected` 已改名为 `OnItemSelected`，与 `List.OnItemSelected` 保持一致）；选中项变化通过 `SelectedIdChanged` 回传。

### 禁用与可见性

- `Menu.Disabled`（基类参数）会下传到所有分组开关和菜单项；`MenuGroup.Disabled` 只禁用该分组，`MenuItem.Disabled` 只禁用该项。
- `MenuGroup.Visible` / `MenuItem.Visible` 为 false 时对应的 DOM 节点不渲染，也不参与键盘遍历。

### 语义与无障碍

- 根为 `<nav>`（`AriaLabel` 提供可访问名称），分组与菜单项使用 `ul` / `li`（`role="list"`），折叠区域通过 `visibility` 退出无障碍树和 Tab 顺序。
- 分组开关输出 `aria-expanded="true"` / `"false"` 字符串和 `aria-controls`，折叠区域带对应 `id`；当前项输出 `aria-current="page"`。
- 键盘：Tab 保持原生顺序；`ArrowUp` / `ArrowDown` 在当前可达节点（分组开关 + 已展开分组的非禁用项）间循环，`Home` / `End` 跳到首尾，`ArrowRight` 展开、`ArrowLeft` 折叠（RTL 下由模块读取 `direction` 自动互换），`ArrowLeft` 在展开分组内会把焦点退回分组开关，`Enter` / `Space` 保持原生行为。
- 组件声明一个 JS module（`Components/Menu/Menu.razor.js`），只负责把焦点移到 C# 模型选中的节点、在菜单内部抑制方向键的默认页面滚动，并报告书写方向；折叠动画仍由 CSS 完成。
- 长菜单不强制滚动容器：把 `Menu` 放进带 `max-height` 的滚动容器（示例侧边栏即如此）即可，避免组件自己裁切焦点环。

### 实现边界

不提供多于两层的嵌套、组合式子组件（`ChildContent` + 子组件）、搜索过滤和多选；长菜单不内置滚动容器，把它放进带 `max-height` 的容器即可（示例侧边栏即如此）。
## 22. Tooltip

### 支持能力

`Tooltip` 支持 `Text`、`ChildContent`、`Disabled`、`AriaLabel` 和 `Placement`（`TooltipPlacement`：`Top` / `Bottom` / `Start` / `End`，默认 `Top`）：`ChildContent` 是被描述的触发元素，`Text` 才是提示内容（两者不是重复内容参数）。

### 行为与无障碍

提示内容使用 `role="tooltip"`，不阻塞触发元素，也不影响页面布局。`Tooltip.razor.js` 会把提示节点的 id 写入 `ChildContent` 中第一个可聚焦元素的 `aria-describedby`（没有可聚焦元素时回退到触发包装元素），因此读屏可以直接朗读提示文本；提示节点在 `Text` 为空或禁用时完全不渲染，不会留下悬空引用。

方位由 CSS 决定，JS 只在需要时切换方位类并写入偏移变量：优先使用 `Placement` 指定的一侧，空间不足时翻转到对侧，再沿交叉轴偏移以留在视口内（`resize` 时重算）。`prefers-reduced-motion` 下只保留位移过渡的关闭。

### 实现边界

不提供 Escape、点击外部关闭、富交互内容和模态行为；无 JS 时视觉提示仍可用，只是缺少 `aria-describedby` 关联与翻转/偏移。

## 23. ThemeProvider 和 ThemeSwitch 使用方式

### 基础用法

```razor
<ThemeProvider />
<ThemeSwitch />
```

`ThemeProvider` 应放置在 Layout 或应用根组件中，但不包裹页面内容。业务代码通过注入 `ThemeService` 或使用 `ThemeSwitch` 修改主题模式。

`ThemeProvider` 没有组件参数；`ThemeSwitch` 提供 `AriaLabel`、`Size`、`ModeChanged`，并继承 `Disabled`。`Size` 使用公共 `Size` 枚举，`System`、`Light`、`Dark` 三个按钮分别切换 `ThemeService.Mode`。

主题模式（System / Light / Dark）会在每次切换时通过 `localStorage`（键 `aeterni.theme.mode`）持久化，下次启动（浏览器或 Tauri webview 均支持）自动恢复；存储不可用或值非法时回退到默认的 System 模式。

### 实现边界

系统主题跟随依赖浏览器 `matchMedia`；Tauri 窗口主题由 `src-tauri` 宿主同步，浏览器中没有 Tauri API 时自动降级。
## 24. DialogProvider

### 支持能力

`DialogProvider` 是无可见内容的宿主组件，负责挂载 Dialog、Confirm、Alert 和 Toast：

- 无公开参数（继承基类的 `Id`、`Class`、`Style`、`Visible` 等）。
- 同一个 `ToastPosition` 下 Alert 与 Toast 共用一个位置容器（先 Alert 后 Toast），不会互相覆盖；容器超出视口高度时可用滚轮滚动，并使用细滚动条提示可滚动。
- 在应用 Layout 或根组件中**只放置一次**，业务代码不要直接引用它。

### 行为与无障碍

Provider 统一负责遮罩、焦点管理、背景滚动锁定和弹层层级，并通过 `IDialogService` 对外提供能力，
因此应用组件不访问 Provider 内部状态。消息内容按纯文本安全输出。

锁定背景滚动时会按“视口宽度 − 文档可用宽度”补偿 `body` 的右内边距，打开与关闭对话框都不会让背景横向跳动。

### 实现边界

Provider 自身不提供定位或动画开关；位置、时长与数量等默认值由 `AddAeterniUI` 的
`AeterniUIOptions` 配置，单个弹层可通过对应 options 覆盖。

同时显示的 Toast 数量上限为 `AeterniUIOptions.MaxToastCount`（默认 5），超出后最早的一条会被完成并移除；Alert 由调用方 `await` 控制生命周期，不设数量上限。

## 25. Dialog、Confirm、Alert 和 Toast

应用根部放置一个 Provider：

```razor
<DialogProvider />
```

通过 `IDialogService` 使用弹出能力：

```csharp
await DialogService.AlertAsync("Saved", new AlertOptions
{
    Severity = Severity.Success
});

var confirmed = await DialogService.ConfirmAsync("Continue?");

DialogService.ShowToast("Completed", new ToastOptions
{
    Severity = Severity.Info
});
```

### Dialog

- 通过 `IDialogService.Show` / `ShowAsync(RenderFragment, DialogOptions?)` 提供 `RenderFragment` 自定义内容。
- `DialogOptions` 支持 `Title`、`Severity`、`Icon`、`ShowCloseButton`、`CloseOnEscape`、`CloseOnOverlayClick` 和 `Footer`。
- 支持模态遮罩、背景模糊、焦点管理、焦点恢复和背景滚动锁定。
- 关闭时支持淡出和缩小动画。

### Confirm

- 通过 `IDialogService.ConfirmAsync` 提供基于 Dialog 的确认交互。
- `ConfirmOptions` 提供 `ConfirmText`、`CancelText`，并继承 `DialogOptions`。
- 提供取消和确认两个动作，返回 `Task<bool>`。
- 点击不可关闭的外部遮罩时，弹窗会进行轻微抖动反馈。

### Alert

- 非模态、不阻塞页面、不显示模态遮罩的单条提示，默认底部居中；消息内容最多显示两行，超出部分截断。
- 支持 Toast 的八个位置，可通过 `AlertOptions.Position` 或 Service 的位置重载覆盖，也可在全局配置中修改默认位置，便于避开顶部页面元素。
- `AlertOptions` 支持 `Position`、`Duration`、`Blur`、`OnClosedAsync`，并继承 `DialogOptions` 的语意色、图标和关闭设置。
- 使用 Button 相同的 `Info`、`Success`、`Warning`、`Danger` 语意色映射。
- 语意背景使用浅色混合，去除左侧语意边框。
- 采用左侧图标、中间内容、右侧关闭按钮的三段式布局；未传 `Icon` 时按 Severity 使用内置 `AeterniIcons` 严重度图标，关闭按钮只占自身内容宽度并上下居中。
- 默认自动关闭，四边环绕进度边框显示剩余时间。
- 支持 `Blur = false` 关闭自身背景模糊。
- 支持关闭后的 `OnClosedAsync` 回调。
- 支持 Alert 滑出动画。

### Toast

- 非阻塞通知。
- 支持 TopStart、TopCenter、TopEnd、CenterStart、CenterEnd、BottomStart、BottomCenter、BottomEnd 八个位置，默认右上角（TopEnd）。
- 支持全局默认位置、单条通知位置和最大显示数量。
- 支持图标、标题、手动关闭和复杂 RenderFragment 内容；未传 `Icon` 时按 `Severity` 自动使用内置严重度图标。
- `ToastOptions` 支持 `Title`、`Severity`、`Icon`、`Position`、`Duration`、`ShowCloseButton`、`Blur` 和 `OnClosedAsync`。
- 默认自动关闭，四边环绕进度边框显示剩余时间。
- 支持 `Blur = false` 关闭自身背景模糊。
- 支持关闭后的 `OnClosedAsync` 回调。
- 根据通知位置向对应屏幕边缘滑出。

### 全局配置

```csharp
builder.Services.AddAeterniUI(options =>
{
    options.DefaultToastPosition = ToastPosition.TopEnd;
    options.DefaultAlertPosition = ToastPosition.BottomCenter;
    options.MaxToastCount = 5;
    options.DefaultAlertDuration = TimeSpan.FromSeconds(5);
    options.DefaultToastDuration = TimeSpan.FromSeconds(5);
});
```

单条通知的 `Duration` 规则：

- `null`：使用全局默认时长。
- `TimeSpan.Zero`：不自动关闭，只能手动关闭。
- 大于零：按指定时长自动关闭。

### 实现边界

Provider 负责遮罩、焦点与滚动锁定；弹层消息按纯文本安全输出，需要复杂结构时使用 `Show(RenderFragment, DialogOptions)`，并避免在交互式 Dialog 内再嵌套模态入口。
## 26. 服务注册

使用以下扩展完成基础服务注册：

```csharp
builder.Services.AddAeterniUI();
```

注册内容包括：

- `AeterniUIOptions`（含 `Text` 文案表）。
- `JsModuleManager`。
- `ThemeService`。
- `DialogService`。
- `IDialogService`。

### 文案与本地化

库内所有用户可见文案集中在 `AeterniUIOptions.Text`（类型 `AeterniUITextOptions`），默认值为英文；通过 `AddAeterniUI` 覆写即可整体本地化：

```csharp
builder.Services.AddAeterniUI(options =>
{
    options.Text.ComboBoxPlaceholder = "请选择";
    options.Text.ComboBoxListLabel = "选项";
    options.Text.RatingLabel = "评分";
    options.Text.ThemeSwitchLabel = "主题模式";
    options.Text.ThemeSystemLabel = "跟随系统";
    options.Text.ThemeLightLabel = "浅色";
    options.Text.ThemeDarkLabel = "深色";
    options.Text.AlertCloseLabel = "关闭提示";
    options.Text.ToastCloseLabel = "关闭通知";
    options.Text.DialogCloseLabel = "关闭对话框";
    options.Text.DialogLabel = "对话框";
    options.Text.TagDismissLabel = "移除标签";
});
```

组件参数（如 `AriaLabel`、`Placeholder`、`DismissLabel`）的优先级始终高于文案表；文案表为空白的条目会回落到英文默认值。

## 27. 当前边界

- 当前项目暂不包含自动化测试，这是当前开发阶段的明确决策。
- 组件库目前优先完善基础组件和基础服务，复杂表单、数据展示和导航组件尚未纳入已完成清单。
- `IconButton` 暂不纳入当前阶段；Button 已支持 `Icon`、`StartIcon` 和 `EndIcon`。
- `Stack` 和 `Flex` 尚未实现。
- Tauri 开发模式依赖本机 Rust、Tauri CLI 和 .NET SDK 环境。
