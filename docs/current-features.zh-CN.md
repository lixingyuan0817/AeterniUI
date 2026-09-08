# AeterniUI 当前已完成功能

版本基线：`0.1`

文档状态：当前实现清单

本文档用于记录当前组件库已经落地的能力，作为示例项目、后续组件开发和 API 设计的基线。未列出的功能不应被视为已经稳定提供。

## 1. 宿主支持

- 支持 Blazor Server 使用组件库。
- 支持 Blazor WebAssembly 使用组件库。
- 示例项目 `AeterniUI.Sample` 为 Blazor WebAssembly 项目。
- 已配置 Tauri + Blazor WebAssembly 开发模式。
- Tauri 开发模式通过 `dotnet watch` 提供热重载，并使用透明窗口、原生窗口阴影和 macOS Vibrancy 配置。
- 组件 API 不依赖具体宿主，浏览器能力通过 JS isolation 提供。

## 2. 设计基础

- 使用统一的 `AeterniComponent` 作为组件基类。
- 基类提供自动生成的实例 ID 和可覆盖的 `Id`。
- 支持 `Class`、`Style`、`AdditionalAttributes`、`Visible`、`Disabled` 和 `ElementReference`。
- 提供 `ClassBuilder` 和 `StyleBuilder`，组件可以在代码后置中组合 class 和 style。
- 支持 `ElementChanged` 回调。
- 支持组件级 `.razor.css` CSS isolation。
- JS module 可选，不添加 JS 文件的组件不会执行 JS 初始化。
- 一个组件最多维护一个主 JS module，JS module 支持初始化、调用和释放生命周期。
- 组件库使用统一的语义化 Token，组件内部不重新建立独立颜色体系。

## 3. Token 和主题

- 提供品牌色、语意色、背景色、文字色、边框色、阴影、圆角、间距、字号和动效 Token。
- 提供 Light 和 Dark 两套主题 Token。
- 支持系统主题跟随。
- 主题切换由 `ThemeService` 管理。
- `ThemeProvider` 为自闭合组件，不需要包裹 Layout 内容。
- `ThemeProvider` 负责注入主题 JS、监听系统主题变化、同步页面主题和 Tauri titlebar 主题。
- `ThemeSwitch` 提供 System、Light、Dark 分段切换，并能在刷新后正确反映当前模式。
- 主题切换包含过渡动画，并适配 reduced-motion 场景。

## 4. Button

`Button` 当前支持：

- `Variant`：`Default`、`Outline`、`Ghost`、`Text`、`Link`。
- `Color`：`Default`、`Primary`、`Neutral`、`Success`、`Warning`、`Danger`、`Info`。
- `Size`：`Small`、`Default`、`Large`。
- `StartIcon`、`EndIcon`，以及兼容旧 API 的 `Icon`。
- `Loading`、`Disabled`、`FullWidth`。
- `ButtonType`、`AriaLabel`、`OnClick`。
- 默认、悬浮、按下、聚焦、禁用和加载状态。
- 加载状态包含 spinner、`aria-busy` 和禁用交互。
- Link 变体悬浮时只显示下划线，不显示背景色。

## 5. ButtonGroup

`ButtonGroup` 当前支持：

- 横向和纵向排列。
- 连体按钮和独立间距两种模式。
- `FullWidth`。
- 通过级联上下文向子 Button 传递禁用状态。
- 统一处理按钮之间的边框和分隔关系。
- `AriaLabel`。

## 6. Surface 和 Card

`Surface` 当前支持：

- `Variant`。
- `Elevation`。
- `Padding`。
- `Radius`。
- `Bordered`。
- `FullWidth`。
- 任意 `ChildContent`。

`Card` 基于 Surface 风格提供：

- Header、Body 和 Footer 三个内容区域。
- `Variant`、`Elevation`、`Padding`、`Bordered` 和 `FullWidth`。
- `Interactive` 和 `OnClick`。
- `AriaLabel`。
- 可点击状态的悬浮和按下反馈。
- 三个内容区域使用统一的内边距和边界关系。

## 7. Icon 和 Font Awesome

- 提供 `Icon` 组件。
- 支持 `IconDefinition`、`Size`、`Color`、`AriaLabel` 和 `Title`。
- 图标尺寸和颜色使用组件库 Token。
- 提供独立的 `AeterniUI.Icons.FontAwesome` 项目。
- 当前已封装常用的新增、方向、确认、关闭、展开收起、主题、设置、搜索、首页、菜单和加载图标。
- 图标库通过 RenderFragment 与 Button、Dialog、Toast 等组件组合使用。

## 8. ThemeProvider 和 ThemeSwitch 使用方式

```razor
<ThemeProvider />
<ThemeSwitch />
```

`ThemeProvider` 应放置在 Layout 或应用根组件中，但不包裹页面内容。业务代码通过注入 `ThemeService` 或使用 `ThemeSwitch` 修改主题模式。

## 9. Dialog、Confirm、Alert 和 Toast

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

- 支持 RenderFragment 自定义内容。
- 支持标题、图标、语意色、关闭按钮、Escape 关闭和底部 Footer。
- 支持模态遮罩、背景模糊、焦点管理、焦点恢复和背景滚动锁定。
- 关闭时支持淡出和缩小动画。

### Confirm

- 基于 Dialog 的确认交互。
- 提供取消和确认两个动作。
- 返回 `Task<bool>`。
- 点击不可关闭的外部遮罩时，弹窗会进行轻微抖动反馈。

### Alert

- 顶部居中显示，不阻塞页面，不显示模态遮罩。
- 使用 Button 相同的 `Info`、`Success`、`Warning`、`Danger` 语意色映射。
- 语意背景使用浅色混合，去除左侧语意边框。
- 支持图标、标题、手动关闭和单行省略显示。
- 默认自动关闭，四边环绕进度边框显示剩余时间。
- 支持 `Blur = false` 关闭自身背景模糊。
- 支持关闭后的 `OnClosedAsync` 回调。
- 支持 Alert 滑出动画。

### Toast

- 非阻塞通知。
- 支持 TopStart、TopCenter、TopEnd、CenterStart、CenterEnd、BottomStart、BottomCenter、BottomEnd 八个位置。
- 支持全局默认位置、单条通知位置和最大显示数量。
- 支持图标、标题、手动关闭和复杂 RenderFragment 内容。
- 默认自动关闭，四边环绕进度边框显示剩余时间。
- 支持 `Blur = false` 关闭自身背景模糊。
- 支持关闭后的 `OnClosedAsync` 回调。
- 根据通知位置向对应屏幕边缘滑出。

### 全局配置

```csharp
builder.Services.AddAeterniUI(options =>
{
    options.DefaultToastPosition = ToastPosition.BottomEnd;
    options.MaxToastCount = 5;
    options.DefaultAlertDuration = TimeSpan.FromSeconds(5);
    options.DefaultToastDuration = TimeSpan.FromSeconds(5);
});
```

单条通知的 `Duration` 规则：

- `null`：使用全局默认时长。
- `TimeSpan.Zero`：不自动关闭，只能手动关闭。
- 大于零：按指定时长自动关闭。

## 10. 服务注册

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

## 11. 当前边界

- 当前项目暂不包含自动化测试，这是当前开发阶段的明确决策。
- 组件库目前优先完善基础组件和基础服务，复杂表单、数据展示和导航组件尚未纳入已完成清单。
- Tauri 开发模式依赖本机 Rust、Tauri CLI 和 .NET SDK 环境。
- Tauri 与 Blazor 热重载同时启动时必须避免多个进程占用同一个开发端口。
