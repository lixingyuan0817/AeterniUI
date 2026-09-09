# AeterniUI 当前已完成功能

文档版本：`10.1.0`

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
- 支持系统主题跟随。
- 主题切换由 `ThemeService` 管理。
- `ThemeProvider` 为自闭合组件，不需要包裹 Layout 内容。
- `ThemeProvider` 负责注入主题 JS、监听系统主题变化、同步页面主题和 Tauri titlebar 主题。
- 页面主题通过 `<html data-theme>` 输出；Tauri 示例宿主监听该属性并调用 `apply_window_backdrop`，让原生窗口背景模糊/色调跟随 System、Light、Dark 三种模式。
- `ThemeSwitch` 提供 System、Light、Dark 分段切换，并能在刷新后正确反映当前模式。
- 主题切换包含过渡动画，并适配 reduced-motion 场景。

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
- 禁用状态统一消费 `--aeterni-state-*` Token，保持所有颜色和变体的前景、背景可读；`Outline` 和 `Ghost` 仅保留各自的边框结构，`Link` 禁用时不会显示下划线。
- 加载状态包含 spinner、`aria-busy` 和禁用交互。
- Link 变体悬浮时只显示下划线，不显示背景色。

## 5. ButtonGroup

### 支持能力

`ButtonGroup` 当前支持：

- `ChildContent`、`Orientation`（`Horizontal` / `Vertical`）、`Connected`、`FullWidth` 和 `AriaLabel`。
- 横向和纵向排列，以及连体按钮和独立间距两种模式。
- 通过级联上下文向子 Button 传递禁用状态。
- 统一处理按钮之间的边框和分隔关系。

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

## 7. Icon 和 Font Awesome

### 支持能力

- 提供 `Icon` 组件。
- `Definition`（必填 `IconDefinition`）、`Size`、`Color`、`AriaLabel` 和 `Title`。
- 图标尺寸和颜色使用组件库 Token。
- 提供独立的 `AeterniUI.Icons.FontAwesome` 项目。
- 当前已封装常用的新增、方向、确认、关闭、展开收起、主题、设置、搜索、首页、菜单和加载图标。
- 图标库通过 RenderFragment 与 Button、Dialog、Toast 等组件组合使用。

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
- 悬浮与聚焦均使用主题色（`--aeterni-brand-500`）边框，无外圈辉光/阴影；无效状态在悬浮、聚焦下始终保留危险色边框。

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

## 9. Textarea

### 支持能力

`Textarea` 提供多行原生文本输入，复用 `Input` 的绑定、校验和表单级联约定。

- `Value`、`ValueChanged`、`ValueExpression`，兼容 Blazor 标准 `@bind-Value`。
- `Placeholder`、`Name`、`AutoComplete`、`Rows`、`Resize`、`MinLength` 和 `MaxLength`。
- `Disabled`、`ReadOnly`、`Required`、`Invalid` 和 `FullWidth`。
- `OnInput`、`OnChange`、`OnFocus`、`OnBlur` 和 `OnKeyDown` 事件回调。
- `AriaLabel` 和 `AriaDescribedBy` 无障碍属性。

### 行为与无障碍

使用原生 `<textarea>` 语义；通过 `ValueExpression` 接入 EditContext 校验，并同步 `aria-invalid`、`aria-required`、`aria-readonly`、`aria-disabled` 和 `aria-describedby`。`Resize` 支持 `None` 和 `Vertical`，默认允许垂直调整大小。

### 实现边界

当前不提供富文本编辑、自动高度和异步校验。

## 10. FormField

### 支持能力

`FormField` 提供表单控件的统一标签、描述和错误信息布局，公开 `Label`、`Description`、`Error`、`ChildContent`、`Required`、`Invalid` 和 `For` 参数，并继承基类的 `Disabled`。`Label` 提供独立的原生 `<label>` 组件，支持 `For` 参数和 `ChildContent`。

### 行为与无障碍

显式 `Error` 优先于 `EditContext` 验证消息，并通过稳定 ID 建立 label、描述文本、错误文本与控件之间的关联。FormField 会通过级联上下文向内部表单控件传递输入 ID、`aria-describedby`、禁用、必填和验证状态。

### 实现边界

FormField 负责布局和语义关联，不替代内部控件的值绑定或输入行为。

## 11. Checkbox

### 支持能力

`Checkbox` 提供原生复选框语义和稳定的布尔值绑定，使用 `<label>` 包裹原生 `<input type="checkbox">`，可被辅助技术直接识别。

- `Value`、`ValueChanged`、`ValueExpression`，兼容 Blazor 标准 `@bind-Value`。
- `Indeterminate` 仅作为显示状态；用户交互后组件落定到明确的 `true`/`false` 值，不保留不确定状态。
- `ChildContent` 作为标签文本，点击整行即可切换。
- `Name`、`AriaLabel`、`AriaDescribedBy`。
- `Size`：`Small`、`Default`、`Medium`、`Large`，默认/中等 20px、Small 16px、Large 26px 的紧凑盒体，标签与复选框间距为 4px。
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

## 12. Radio / RadioGroup

### 支持能力

- `RadioGroup<TValue>` 提供 `Value`、`ValueChanged`、`ChildContent`、`Name`、`Disabled`、`Required`、`Invalid` 和 `AriaLabel`。
- `Radio<TValue>` 提供 `Value`、`ValueChanged`、`ValueExpression`、`ChildContent`、`Name`、`Required`、`Invalid`、`AriaLabel` 和 `AriaDescribedBy`。
- 使用原生 `<input type="radio">` 与 `fieldset` 分组语义。

### 行为与无障碍

单选值通过 RadioGroup 级联上下文统一绑定；禁用、必填和无效状态会传递到选项，并输出相应的原生/ARIA 语义。

### 实现边界

当前不提供远程选项、虚拟化和多选行为；方向键导航沿用原生 radio 行为。

## 13. Switch

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

## 14. Tag

### 支持能力

`Tag` 提供分类、状态与筛选的紧凑标签：

- `ChildContent`、`StartIcon`、`EndIcon`、`Color`（沿用 Button 语意色）、`Size`、`Variant`（`Default` / `Soft` / `Outline`）。
- 可选 `StartIcon` / `EndIcon` 与 `Dismissible`、`OnDismiss`、`DismissLabel`；关闭按钮复用 `Button` 的图标能力。
- 可关闭 Tag 的关闭按钮带有可访问名称且只触发一次事件。

### 行为与无障碍

非可关闭 Tag 保持展示语义；可关闭 Tag 使用带可访问名称的 Button 关闭动作，且关闭事件只触发一次。

## 15. List + ListItem

### 支持能力

- `List` 提供 `ChildContent`、`SelectionMode`（无选择 / 单选 / 多选）、`SelectedValue`/`SelectedValues`、对应变更事件、`AllowClear`、`AriaLabel` 与 `OnItemSelected`。
- 选择模式下容器输出 `role="listbox"` 与 `aria-multiselectable`；交互式 `ListItem` 使用原生 `<button>` 并同步 `aria-selected`，不额外设置 `role="option"`。
- `ListItem` 支持 `Value`、`ChildContent`、继承的 `Disabled`、`LeadingContent` / `TrailingContent`；`Selected` 是由 `List` 计算的内部状态，不是可设置参数。
- `AllowClear` 只影响单选模式；多选模式通过再次选择已选项移除该项。
- 支持鼠标、方向键、Home/End 与空格/回车选择；禁用项不可选择。
- 无障碍：容器保持 `role="listbox"` 单点 Tab 聚焦，并用 `aria-activedescendant` 指向当前高亮选项；选项本身带稳定 id 且移出 Tab 序列，由容器键盘统一驱动。
- 无选择模式渲染为普通列表结构，不输出按钮语义。

### 行为与无障碍

选择模式下容器负责单点 Tab 聚焦和键盘导航，禁用项不可选择；无选择模式不伪造按钮或 listbox 交互语义。

### 实现边界

当前不提供拖拽、虚拟化、分组和异步数据源。

## 16. Rating

### 支持能力

- 整数评分：`Value` / `ValueChanged` / `ValueExpression`、`OnChange`，`Max`（默认 5）与越界钳制。
- 支持 `ReadOnly`、`Disabled`、`AllowClear`（再次点击当前值清零）与 `Icon` 自定义（缺省使用星号字形）。
- 按 `radiogroup` / `radio` 语义输出，支持方向键与 Home/End；`aria-label` 可自定义。
- 接入 `EditContext` 校验（`ValueExpression`），无效状态输出 `aria-invalid` 并可在 `FormField` 中级联。

### 行为与无障碍

Rating 使用 radiogroup/radio 语义，支持方向键、Home/End 和当前值播报；只读或禁用时不改变值。

### 实现边界

当前只支持整数评分，不实现半星。

## 17. ComboBox

### 支持能力

- 泛型 `ComboBox<TItem>`，默认下拉选择控件（不含自由输入搜索）。
- `Items`、`Value`/`ValueChanged`、`TextSelector`、`ItemTemplate`、`EmptyContent`。
- `Placeholder`、`Required`、`Invalid`、继承的 `Disabled`、`AriaLabel` 和 `OnChange`；接入 `EditContext` 校验（`ValueExpression`）并在 `FormField` 中级联。
- 点击触发按钮弹出选项；打开后支持上下方向键、Home/End、Enter 确认；点击外部、Escape 或页面滚动（弹层内部滚动除外）都会关闭，行为接近原生 select。
- `role="combobox"`、`aria-expanded`、`aria-controls` 与选项同步；打开时触发器通过 `aria-activedescendant` 指向高亮项，无效时输出 `aria-invalid`。
- 弹层由最小 JS module（`ComboBox.razor.js`）按触发按钮锚定为 fixed 定位并自动上下翻转/贴边，避免被卡片/容器裁剪遮挡。

### 行为与无障碍

ComboBox 的触发器保持 combobox 语义；打开后支持方向键、Home/End、Enter 和 Escape，外部点击或页面滚动会关闭选项。

### 实现边界

当前不支持自由输入筛选、远程搜索、虚拟化、无限滚动和多选。

## 18. Progress

### 支持能力

`Progress` 提供线性进度展示，支持 `Value`、`Max`、`Indeterminate`、`Color`、`Size`、`ShowValue` 和 `AriaLabel`。

### 行为与无障碍

输出 `role="progressbar"`、`aria-valuemin`、`aria-valuemax`，确定进度时输出 `aria-valuenow`；不确定状态使用 CSS 动画，并在 reduced-motion 下停止动画。

### 实现边界

当前提供线性进度，不包含环形渲染、上传任务管理和远程数据源。

## 19. PopupHost / Popover

### 支持能力

`PopupHost` 提供浮层内容挂载容器；`Popover` 支持 `Open`、`Header`、`ChildContent`、`Modal` 和 `AriaLabel`。

### 行为与无障碍

Popover 根据 `Modal` 输出 `dialog` 或 `region` 语义，关闭时通过 `hidden` 移除可见内容。

### 实现边界

当前提供基础容器与语义表面，不包含锚点定位、翻转、点击外部关闭和滚动关闭；这些能力留待后续 PopupHost 增强。

## 20. Tooltip

### 支持能力

`Tooltip` 支持 `Text`、`ChildContent`、`Disabled` 和 `AriaLabel`，通过 hover/focus-visible 展示说明。

### 行为与无障碍

提示内容使用 `role="tooltip"`，不阻塞触发元素，也不影响页面布局。

### 实现边界

当前提供上方固定位置，不包含 Placement、Escape、点击外部关闭和复杂交互内容。

## 21. ThemeProvider 和 ThemeSwitch 使用方式

### 基础用法

```razor
<ThemeProvider />
<ThemeSwitch />
```

`ThemeProvider` 应放置在 Layout 或应用根组件中，但不包裹页面内容。业务代码通过注入 `ThemeService` 或使用 `ThemeSwitch` 修改主题模式。

`ThemeProvider` 没有组件参数；`ThemeSwitch` 提供 `AriaLabel`、`Size`、`ModeChanged`，并继承 `Disabled`。`Size` 使用公共 `Size` 枚举，`System`、`Light`、`Dark` 三个按钮分别切换 `ThemeService.Mode`。

主题模式（System / Light / Dark）会在每次切换时通过 `localStorage`（键 `aeterni.theme.mode`）持久化，下次启动（浏览器或 Tauri webview 均支持）自动恢复；存储不可用或值非法时回退到默认的 System 模式。

## 22. Dialog、Confirm、Alert 和 Toast

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
- 采用左侧图标、中间内容、右侧关闭按钮的三段式布局；未传 `Icon` 时按 Severity 自动生成默认图标，关闭按钮只占自身内容宽度并上下居中。
- 默认自动关闭，四边环绕进度边框显示剩余时间。
- 支持 `Blur = false` 关闭自身背景模糊。
- 支持关闭后的 `OnClosedAsync` 回调。
- 支持 Alert 滑出动画。

### Toast

- 非阻塞通知。
- 支持 TopStart、TopCenter、TopEnd、CenterStart、CenterEnd、BottomStart、BottomCenter、BottomEnd 八个位置，默认右上角（TopEnd）。
- 支持全局默认位置、单条通知位置和最大显示数量。
- 支持图标、标题、手动关闭和复杂 RenderFragment 内容；未传 `Icon` 时按 Severity 自动生成默认图标。
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

## 23. 服务注册

使用以下扩展完成基础服务注册：

```csharp
builder.Services.AddAeterniUI();
```

注册内容包括：

- `AeterniUIOptions`。
- `JsModuleManager`。
- `ThemeService`。
- `DialogService`。
- `IDialogService`。

## 24. 当前边界

- 当前项目暂不包含自动化测试，这是当前开发阶段的明确决策。
- 组件库目前优先完善基础组件和基础服务，复杂表单、数据展示和导航组件尚未纳入已完成清单。
- `IconButton` 暂不纳入当前阶段；Button 已支持 `Icon`、`StartIcon` 和 `EndIcon`。
- `Stack` 和 `Flex` 尚未实现。
- Tauri 开发模式依赖本机 Rust、Tauri CLI 和 .NET SDK 环境。
