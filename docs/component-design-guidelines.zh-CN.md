# AeterniUI 组件开发设计规范

文档版本：`10.5.0`

状态：第一版草案

适用范围：`src/AeterniUI` 中的所有 Blazor 组件，以及 `src/AeterniUI.Sample` 中的组件示例

## 1. 目标

AeterniUI 同时服务于 Blazor Server、Blazor WebAssembly 和 Tauri + Blazor WebAssembly。组件必须尽量保持以下特征：

- 在不同宿主中拥有一致的 API、语义和视觉结果。
- 默认不依赖 JavaScript，只有浏览器能力无法由 CSS 或 Blazor 完成时才使用 JS isolation。
- 只依赖语义化 Token，不在组件中重新定义颜色体系。
- 参数、状态、事件、DOM 属性和销毁行为具有统一约定。
- 组件可以被单独使用，也可以被组合到更复杂的组件中。
- 视觉风格由组件库维护，业务项目只通过公开参数和扩展类名进行定制。

本规范优先解决一致性和可维护性，不追求一次性覆盖所有组件类型。

### 1.1 快速规则

新增或修改组件时至少确认：

1. 组件继承 `AeterniComponent`。
2. 根元素使用 `BuildAttributes()`，class 在 `BuildClass()` 中构建。
3. 组件样式放在组件目录的 `.razor.css`，不把组件选择器写入全局 Token 文件。
4. 颜色、间距、圆角和动效使用现有 `--aeterni-*` Token。
5. JS 只用于必要的浏览器行为，并确保监听器、Observer 和定时器可释放。
6. 保留正确的 HTML、ARIA 和键盘语义。
7. 公共能力同步 `docs/current-features.zh-CN.md`，任务状态同步 `docs/component-roadmap.zh-CN.md`。
8. 完成后运行 `dotnet build aeterni_ui.slnx`。

## 2. 当前架构约定

### 2.1 组件继承关系

所有组件直接或间接继承 `AeterniComponent`。当前阶段不为“元素能力”和“JS 能力”分别创建公共基类，也不要求组件实现 `Ixxx` 接口。

```text
ComponentBase
    |
AeterniComponent
    |
具体组件，例如 ThemeProvider、ThemeSwitch、Button
```

`AeterniComponent` 已经提供以下基础能力：

- 由基类生成的 `InstanceId`。
- 默认的 `ElementId`，由 `Id` 参数覆盖。
- `Class`、`Style` 和 `AdditionalAttributes`。
- `Visible`、`Disabled` 和 `ElementReference` 基础能力。
- `ClassBuilder`、`StyleBuilder`。
- 可选的 JS module 扫描、加载、初始化和销毁生命周期。
- `ElementChanged` 回调。

只有当某种行为被至少两个组件重复使用，并且无法通过组合解决时，才考虑增加新的抽象。新增抽象必须先说明解决的重复问题，不能仅为了“层次看起来完整”而创建基类。

### 2.2 目录结构

组件使用独立目录，组件的 Razor、代码后置和 JS module 放在一起：

```text
Components/
  Button/
    Button.razor
    Button.razor.cs
    Button.razor.js              # 只有需要 JS 时添加
  Theme/
    ThemeProvider.razor
    ThemeProvider.razor.cs
    ThemeProvider.razor.js
```

公共基础类型放在 `Components` 根目录或已有的明确目录中：

```text
Components/AeterniComponent.cs
Components/ClassBuilder.cs
Components/StyleBuilder.cs
Enums/
Services/Impl/
```

服务实现继续放在 `Services/Impl`。没有必要为每个组件创建服务接口。

### 2.3 文件职责

- `.razor`：DOM 结构、语义属性、事件绑定和渲染分支。
- `.razor.cs`：参数、状态、事件处理、class/style 构建和生命周期。
- `.razor.css`：仅在确实需要 CSS isolation 时使用。
- `wwwroot/css/aeterni_ui.css`：当前组件库统一加载的 Token、主题变量和必要的主题别名；不得在此文件新增组件样式。
- `.razor.js`：仅保存该组件需要的浏览器行为，不保存业务状态。

组件公共样式使用各组件目录下的 `.razor.css` 做 CSS isolation；新增组件时应将组件选择器放在自己的隔离样式文件中。`wwwroot/css/aeterni_ui.css` 只维护共享 Token、主题变量和必要的主题别名，不能作为组件样式的集中入口。

## 3. 组件公共 API

### 3.1 参数命名

参数使用 PascalCase，名称优先使用领域中已经稳定的术语：

| 用途 | 统一名称 |
| --- | --- |
| 自定义 DOM id | `Id` |
| 自定义 class | `Class` |
| 自定义 inline style | `Style` |
| 透传 DOM 属性 | `AdditionalAttributes`，由基类捕获 |
| 是否禁用 | `Disabled` |
| 是否渲染为隐藏状态 | `Visible` |
| 根元素引用 | `Element` |
| 根元素引用变化通知 | `ElementChanged` |
| 子内容 | `ChildContent` |
| 尺寸 | `Size` |
| 视觉变体 | `Variant` |
| 颜色语义 | `Color` |
| 加载状态 | `Loading` |
| 是否占满父容器宽度 | `FullWidth` |
| 选中状态 | `Selected` |

不使用含义重复的参数，例如 `IsDisabled`、`Enable`、`CustomClassName`。布尔参数统一使用正向语义。

### 3.2 参数顺序

`.razor.cs` 中按照以下顺序排列参数：

1. 继承的公共参数不重复声明。
2. 组件必需参数。
3. 组件状态参数，例如 `Disabled`、`Loading`、`Selected`。
4. 外观参数，例如 `Variant`、`Color`、`Size`。
5. 内容参数，例如 `ChildContent`、`StartIcon`、`EndIcon`。
6. 事件回调，例如 `OnClick`、`ValueChanged`、`SelectedChanged`。
7. 组件专用的可访问性参数。

参数必须有合理默认值。默认值应使组件在最简单的使用方式下可用：

```razor
<Button>Save</Button>
```

### 3.3 枚举参数

对于有限且稳定的选项使用枚举，不使用字符串常量：

```csharp
public enum ButtonVariant
{
    Default,
    Outline,
    Ghost,
    Text,
    Link
}
```

枚举成员使用 PascalCase。枚举属于公开 API，命名应该使用视觉和交互领域通用的词，不绑定某个具体页面。

不要为只有两个选项的状态创建枚举，优先使用 `bool`。例如使用 `Disabled`，不要创建 `DisabledState`。

例外：表达方向或形态而不是“开/关状态”的二值参数可以使用枚举（例如 `Orientation.Horizontal/Vertical`、
`TextareaResize.None/Vertical`），因为它们描述的是取值域而不是布尔状态。

### 3.4 事件回调

组件事件使用 `EventCallback` 或 `EventCallback<T>`，不要暴露 `Action`、`Action<T>` 或组件内部事件对象：

```csharp
[Parameter]
public EventCallback<ThemeMode> ModeChanged { get; set; }
```

事件处理方法返回 `Task`，不要使用 `async void`。组件内部触发回调前，应先更新自身状态，除非该事件明确要求由消费者决定状态。

值组件遵循 Blazor 标准绑定命名：

```text
Value
ValueChanged
ValueExpression
```

### 3.5 子内容

可组合组件优先使用 `RenderFragment`：

```csharp
[Parameter]
public RenderFragment? ChildContent { get; set; }
```

不要同时提供多个表达相同内容的参数，例如 `Text` 和 `ChildContent`（`Tooltip` 的
`ChildContent` 是触发元素、`Text` 是提示内容，属于分工而非重复）。如果组件需要支持图标，图标可以使用明确命名的 `StartIcon` 和 `EndIcon`，但必须保持文本和图标的语义顺序。

图标本身使用核心 `Icon` 组件渲染供应商无关的 SVG 定义：

```razor
<Icon Definition="@FontAwesomeIcons.Solid.Plus" />
```

组件内部的字形（勾选、箭头、星形、严重度提示和关闭按钮）使用核心库自带的
`AeterniIcons`，它是不依赖任何图标供应商的 16 × 16 几何定义，因此更换适配包
不会改变组件本身的观感。业务图标则通过独立适配包提供，例如
`AeterniUI.Icons.FontAwesome`；核心组件库不直接依赖具体图标供应商。

给子组件（尤其是 `Icon`）根元素挂自定义 class 时注意：CSS 隔离的作用域属性只加在
组件自己文件里渲染的元素上，子组件的根元素拿不到父组件作用域，因此
`.aeterni-parent__part { }` 无法命中 `<Icon Class="aeterni-parent__part" />` 渲染出的
`<svg>`。可行做法是：用包装元素承载 class（尺寸、颜色通过继承生效），或改用
`::deep`，或通过 `Icon` 组件读取的局部变量 `--aeterni-icon-render-size`（默认回退 `1em`，
在祖先元素上设置即可传递尺寸，它不是全局 Token）在祖先元素上传递。

## 4. 基类使用规范

### 4.1 根元素渲染

组件必须有明确的根元素，并将基类生成的属性传递给根元素：

```razor
<button @attributes="BuildAttributes()">
    @ChildContent
</button>
```

不要在组件中再次手动渲染 `id`、`class`、`style`，否则会产生重复属性或覆盖顺序不一致。

`BuildAttributes()` 的当前约定是：

- 先复制 `AdditionalAttributes`。
- 基类统一写入 `id`、`class`、`style`。
- `Visible == false` 时写入 `hidden`。
- 组件通过重写 `SupportsDisabled` 后，才由基类写入 `disabled` 和 `aria-disabled`。

因此，组件不要自行生成第二套根元素属性合并逻辑。

### 4.2 单根组件与复合组件

`Disabled` 的处理取决于组件根节点是否就是实际交互元素：

- 单根交互组件，例如 `Button`、`Input`，应重写 `SupportsDisabled`，由基类向根元素输出 `disabled` 和 `aria-disabled`。
- 复合组件，例如 `ThemeSwitch`、`ComboBox`，根节点通常是 `div` 或其他容器，不应重写 `SupportsDisabled` 来伪造原生禁用能力。
- 复合组件应在内部真正的交互元素上应用 `disabled`，并在根容器上根据需要输出 `aria-disabled` 或其他复合组件语义。
- 复合组件内部不要再次调用 `BuildAttributes()` 处理每个子控件；子控件的属性由组件自身按语义生成。

这样可以避免容器和内部控件重复表达 Disabled，也不会向不支持该属性的 HTML 元素添加错误的原生语义。

### 4.3 class 构建

组件通过重写 `BuildClass()` 添加自身的基础类、尺寸类和状态类：

```csharp
protected override ClassBuilder BuildClass()
{
    return base.BuildClass()
        .Add("aeterni-button")
        .Add(SizeClass)
        .Add($"aeterni-button--{Variant.ToString().ToLowerInvariant()}")
        .Add("is-disabled", Disabled)
        .Add("is-loading", Loading);
}
```

约束：

- 第一项调用 `base.BuildClass()`。
- 组件基础类使用 `aeterni-{component}`。
- 变体使用 `aeterni-{component}--{variant}`。
- 状态使用 `is-{state}`。
- 不在 Razor 标记中拼接多套 class 逻辑。
- 不将业务名称写入组件内部 class。

### 4.4 style 构建

组件通过重写 `BuildStyle()` 添加确实属于组件 API 的 inline style：

```csharp
protected override StyleBuilder BuildStyle()
{
    return base.BuildStyle()
        .Add("--aeterni-button-width", Width, Width is not null);
}
```

优先使用 CSS class 和 Token。只有动态数值、用户明确传入的尺寸或浏览器运行时值才使用 `StyleBuilder`。不要用 inline style 复制整套主题样式。

### 4.5 Id 和 ElementReference

- `InstanceId` 是组件实例内部稳定标识，主要用于 JS module 和内部关联。
- `ElementId` 是渲染到 DOM 的 id。没有传入 `Id` 时使用基类生成的 id。
- 组件不得自行创建另一套 Guid id。
- JS 需要关联组件实例时传递 `InstanceId`，不要从 DOM 文本中推断组件状态。
- 使用 `@ref` 时通过 `RootElement` 或组件专用的 `ElementReference` 管理。

## 5. Token 和样式规范

### 5.1 Token 是唯一视觉来源

组件样式必须优先使用当前 Token：

```css
.aeterni-button {
    background: var(--aeterni-color-brand-default);
    color: var(--aeterni-text-inverse);
    border-radius: var(--aeterni-radius-button);
    transition: var(--aeterni-transition-button);
}
```

组件中禁止直接写主题相关的颜色值，例如：

```css
/* 不允许 */
color: #ffffff;
background: #8b4df6;
```

允许使用固定值的场景：

- 不表达主题含义的尺寸，例如 `1px` 边框。
- 纯布局细节，在现有 spacing Token 无法表达时。
- 图标内部必须固定的几何尺寸。
- 阴影或透明度经过设计确认且尚未有对应 Token 时。

这种例外应尽量转化为新的 Token，而不是在多个组件中复制。

### 5.2 命名空间

公开 Token 必须使用 `--aeterni-` 前缀。组件专用 Token 继续使用该前缀，并包含组件名：

```css
--aeterni-button-height          /* 以下三行只是命名示例，不表示这些 Token 已存在 */
--aeterni-button-padding-inline
--aeterni-dialog-width
```

不得创建无前缀的公共 Token，也不要覆盖 Bootstrap、浏览器或宿主项目的通用变量。

### 5.3 颜色角色

组件依赖语义角色，不依赖色阶：

背景 Token 分为两类，不能混用：

- 页面和容器背景使用 `--aeterni-bg-*`、`--aeterni-bg-elevated` 和 `--aeterni-surface-soft` 等中性背景 Token；它们不使用品牌紫色，避免 Card、Surface、Header 或侧边栏形成大面积彩色底。
- 组件交互状态可以使用 `--aeterni-surface-hover`、`--aeterni-surface-active`、`--aeterni-surface-selected` 和 `--aeterni-focus-color` 等品牌状态 Token；它们用于选中、悬停、按下和焦点反馈，不作为页面或容器的默认背景。

组件新增背景时，先判断它是容器背景还是交互状态背景，再选择对应 Token。

**表面归属**：写样式前先确认这个组件**是否该自带表面**，两类组件的结论相反：

| 分类 | 组件 | 规则 |
| --- | --- | --- |
| 自带表面 | `Surface`、`Card`、`List`、`Input`/`Textarea`/`ComboBox` 的控件盒、`Checkbox`/`Radio`/`Switch` 的指示块、`Popover`/`Tooltip`/`Dialog`/Alert/Toast 的浮层、`Tag`/`Button` 的实心与柔和变体 | 自身负责 `--aeterni-bg-*` 或语义填充色，可以单独使用；外围不得再套一层同色底 |
| 内容控件（不自带表面） | `Menu`、`Tabs`、`Rating`、`FormField`、`Label`、`Icon`、`Progress`、`ButtonGroup`、`Button` 的透明变体 | 只画内容与交互状态；底由宿主容器（`Card`/`Surface`/侧边栏/代码块）提供，**不得**自行加 `--aeterni-bg-*` |

判断依据是"这个组件能不能单独构成一块界面"：导航列表、标签页、评分这类控件天生是嵌在容器里使用的，给它们加底会在卡片/侧栏里形成"白底套白底 + 双层边框"；反过来，`List` 是一次性交付"选项容器"的可选中列表，所以自带表面。示例页与组件文档必须展示真实用法——把内容控件放进宿主容器里演示，而不是让它们裸在页面底上。

| 用途 | 优先 Token |
| --- | --- |
| 页面或容器背景 | `--aeterni-bg-primary`、`--aeterni-bg-surface` |
| 次级背景 | `--aeterni-bg-secondary`、`--aeterni-bg-tertiary` |
| 主要文本 | `--aeterni-text-primary` |
| 次要文本 | `--aeterni-text-secondary`、`--aeterni-text-tertiary` |
| 边框 | `--aeterni-border`、`--aeterni-border-strong` |
| 悬浮表面 | `--aeterni-state-background-hover` |
| 按下表面 | `--aeterni-state-background-active` |
| 选中/勾选表面 | `--aeterni-state-background-selected`、`--aeterni-state-background-checked` |
| 禁用控件 | `--aeterni-state-color-disabled`、`--aeterni-state-background-disabled`、`--aeterni-state-border-disabled` |
| 校验失败 | `--aeterni-state-color-invalid`、`--aeterni-state-background-invalid`、`--aeterni-state-border-invalid` |
| 只读控件 | `--aeterni-state-color-readonly`、`--aeterni-state-background-readonly` |
| 占位/弱化文本 | `--aeterni-state-color-placeholder`、`--aeterni-state-color-muted` |
| 反色内容 | `--aeterni-state-color-inverse`、`--aeterni-state-background-inverse` |
| 品牌动作 | `--aeterni-color-brand-default`、`--aeterni-color-brand-hover`、`--aeterni-color-brand-active`、`--aeterni-color-brand-disabled`、`--aeterni-color-brand-soft` |
| 状态反馈 | `--aeterni-color-success-*`、`--aeterni-color-warning-*`、`--aeterni-color-danger-*`、`--aeterni-color-info-*`、`--aeterni-color-neutral-*` |

每个颜色族都提供 `default`、`hover`、`active`、`disabled` 状态前景/边框别名，并提供 `soft` 柔和背景别名，例如 `--aeterni-color-info-default`、`--aeterni-color-info-hover` 和 `--aeterni-color-info-soft`。组件应优先消费这些别名；`--aeterni-brand-500` 等色阶只用于自定义主题或确实需要精确色阶的场景。通用控件的 `selected` 与 `checked` 状态使用同一套 `--aeterni-state-background-*` Token，禁用状态使用同一套 `--aeterni-state-*-disabled` Token。

常见交互状态还提供 `--aeterni-state-*-pressed`（等同 `active`），以及 `--aeterni-state-border-focus`、`--aeterni-state-*-invalid` 等别名。需要同时组合背景、边框和前景时，优先使用 `--aeterni-control-background-*`、`--aeterni-control-border-*`、`--aeterni-control-foreground-*`；焦点环使用 `--aeterni-control-focus-ring` 或现有的 focus 尺寸 Token。`focus` 不强制改变背景，避免键盘焦点和悬浮状态互相覆盖。

组件不得通过自己增加 `.dark`、`.light` 或媒体查询来实现主题切换。主题由 `ThemeProvider` 设置，组件只消费语义 Token。

### 5.4 色板构建

色阶按 OKLCH 构建，不用手调 hex：

- **每族一条明度阶梯**，锚在「该色相看起来最自然」的中档明度上——黄色与绿色天生比蓝色亮，所以不强行让所有族的 500 档明度相同；族内相邻档的 ΔL 必须单调且不要出现 2 倍以上的跳变。
- **色相固定**，只允许浅档做 ≤6° 的 Abney 补偿漂移（浅色看起来偏粉/偏黄是视觉规律）。同一色族内出现 8° 以上漂移要重做。
- **chroma 凹形收敛**：中档最高、两端下降；品牌色的峰值建议 ≤ 0.20（0.24 以上会发荧光）。
- **中档锚点可以来自外部参考色**（例如沿用 Apple 系统色的语意色）：此时以参考色的 OKLCH 为 500 档锚点，浅端插值到统一的 L 0.976、深端插值到统一的 L 0.30，再按固定比例缩放 chroma。这样既保留参考色的观感，又不会出现「参考色直接塞进另一套阶梯」造成的明度断层与色相漂移。
- 每族必须提供三种形态，缺一就会出现对比度问题：
  | 形态 | Token | 用途 |
  | --- | --- | --- |
  | 填充 | `--aeterni-color-{role}-default`（500 档） | 实心按钮、开关轨道、选中指示、通知卡片底色/徽标底/进度环 |
  | 文字/描边 | `--aeterni-color-{role}-text` | 描边/文字变体的文字与边框、图标 |
  | 柔和底 | `--aeterni-color-{role}-soft` | 选中态、Tag/Alert 底色 |
  直接拿填充档当文字用是常见错误：亮色填充档（绿、黄）在浅底上只有 2.2:1。需要与浅色轨道/页面拉开明度的**实心图形**（进度条填充、评分星形）同样取文字形态：它们虽然“实心”，但对比对象不是自己的墨色而是浅色表面。
- **同一个组件内的多种 accent 角色必须分开命名**：通知卡片同时需要填充（卡片底色、徽标底色、进度环）和 ink（徽标图标、头部图标），因此拆为 `--aeterni-dialog-accent` 与 `--aeterni-dialog-accent-ink`；用一个变量兼两个角色，就会把 ink 拖到填充档的对比度。
- **容器背景只能是黑、白、灰或毛玻璃**：`--aeterni-bg-*`、`--aeterni-bg-surface`、`--aeterni-bg-elevated` 和 `--aeterni-surface-soft` 的 R、G、B 必须相等（深色主题统一允许 ≤ 3 的冷偏移）。容器带上 +2 以上的蓝/紫偏移时，在侧边栏、卡片、磨砂层这种大面积上会被读成「品牌色底」，而且 `backdrop-filter: saturate()` 会把偏移放大。彩色只允许出现在品牌/语意色元素、交互状态和通知卡片这类「内容表面」上。
- **文字色阶 = 单一墨色 + 不透明度阶梯**（Apple 的 label 模型）。不要给每一级另调一个 hex：那样两级之间既不同色又只差一点，看起来像脏。当前阶梯与允许的用法：

  | Token | 浅色 | 深色 | 允许的用法（浅色主题在页面上的对比度） |
  | --- | --- | --- | --- |
  | `--aeterni-text-primary` | `rgba(0,0,0,.78)` | `rgba(255,255,255,.86)` | 正文、标题、控件值（11.7:1） |
  | `--aeterni-text-secondary` | `rgba(0,0,0,.62)` | `rgba(255,255,255,.56)` | 标签、导航、说明、分组标题、未选中项（6.2:1） |
  | `--aeterni-text-placeholder` | `rgba(0,0,0,.56)` | `rgba(255,255,255,.48)` | 输入占位符（4.9:1） |
  | `--aeterni-text-tertiary` | `rgba(0,0,0,.52)` | `rgba(255,255,255,.40)` | **仅图标与装饰**（4.3:1），不得用于正文或标签 |
  | `--aeterni-text-disabled` | `rgba(0,0,0,.36)` | `rgba(255,255,255,.28)` | 禁用态（2.5:1，WCAG 对禁用态豁免） |

  用不透明度的另一个好处是文字会随所在表面自动调和（着色 chip、hover 底、毛玻璃层），不需要为每个表面另写一个 hex。
- **结构分隔线用 `--aeterni-separator`**（不透明，浅色 1.7:1 / 深色 1.5:1），不用 7% alpha 的 `--aeterni-border-subtle`：后者在卡片头/底分界上看起来像一片污渍。
- `--aeterni-bg-hover` 与 `--aeterni-bg-active` 是**交互态别名**（有意带品牌色），不属于上面的「中性容器背景」；组件一律使用 `--aeterni-state-background-*`。
- **实心填充的字色由填充明度决定**：深档填充（品牌）配 `--aeterni-text-inverse`，亮档填充（success/warning/danger/info）配 `--aeterni-color-on-semantic`。同一控件在 base/hover/active 三个状态必须保持同一字色，否则很容易掉到 4.5:1 以下；品牌填充向下取档，语意填充向白提亮 12%。
- **浅底深字**：带色 chip（Tag）必须用「浅色调底 + 深色文字」，文字由强调色与正文字色按约 1:1 混合得到；只用两成墨色会让 chip 文字掉到 3:1 以下。

### 5.5 尺寸和圆角

组件尺寸应映射到统一尺度：

```text
Small  -> 紧凑控件
Medium -> 默认控件
Large  -> 强调控件
```

尺寸变化必须保持稳定的布局尺寸，不能让文字、图标或状态切换导致组件跳动。圆角优先使用已有 `--aeterni-radius-*` Token。胶囊控件使用 `--aeterni-radius-full`，不要在组件中重新定义 `9999px`。

控件型组件的圆角走同一档阶梯：`Small -> --aeterni-radius-control-sm`、`Medium/Default -> --aeterni-radius-control-md`、`Large -> --aeterni-radius-control-lg`，并通过 `--aeterni-radius-button*` / `--aeterni-radius-input*` 引用，保证同一表单行内 Button、Input、Textarea 圆角一致。容器型组件使用 `--aeterni-radius-surface`（通用包装）或 `--aeterni-radius-card`（带分区的结构化容器），两者的默认差异必须在组件文档中写明理由。

尺寸档位遵循“默认档位不输出修饰类”的约定：`Size.Default` 与 `Size.Medium` 表示同一中间档，`Variant=Default`、`Elevation=None`、`Radius=Default` 等同样不生成类名。组件不得让 `Default` 与 `Medium` 指向不同档位（`Icon` 的中档有独立规则，是文档化的唯一例外）。组件发出的每个类名都必须在某个样式表中有匹配规则；不写空的占位类，也不写永不匹配的死规则。

尺寸阶梯只使用 `--aeterni-spacing-*`：`--aeterni-gap-*`、`--aeterni-padding-*` 和 `--aeterni-margin-*` 是留给宿主的同名别名，组件内部不得使用，否则同一个 4px 在库里会有三种写法。

浮层半径按层级递增，不是随手取值：Tooltip `--aeterni-radius-tooltip`（4px）< Popover / 下拉列表 `--aeterni-radius-dropdown`（8px）< Dialog `--aeterni-radius-modal`（16px）；通知卡片沿 iOS 通知中心的观感使用 `--aeterni-radius-2xl`（16px），与 Dialog 同档但卡片是独立一层。同一层级新组件应沿用对应 Token。

过渡声明优先复用复合 Token（`--aeterni-transition-button`、`--aeterni-transition-control`、`--aeterni-transition-color` 等）；不要在多个组件里重复写同一串属性 + 时长 + 缓动的组合。确实属于该组件特有的属性（`width`、`grid-template-rows`、`translate`）才就地声明。

选项到类名的映射集中在 `Components/ComponentClass.cs`：`ForSize`、`ForColor`、`For` 负责尺寸、语意色与通用修饰类的生成，组件不要各自维护一套 switch。`Severity` 到 `Color` 的映射也只保留这一处。

组件样式表中不允许出现未注释的裸像素尺寸。能映射到 Token 的直接引用 Token；确实缺少档位的先在 Token 层补语义 Token（例如浮层宽度、紧凑行高、通知字号）再引用；结构性机制（无障碍裁剪模式的 `1px` + `50%`）、动画位移与排版微调可以保留字面量，但必须用注释说明它是机制或微调而不是尺寸档位。

### 5.6 样式覆盖边界

新增组件时：

- 不修改已有 Token 的含义。
- 不修改其他组件的基础样式来适配新组件。
- 不把组件专用规则放进 Token 区域。
- 不使用全局 `button`、`input` 等选择器覆盖宿主应用。
- 组件样式必须以 `.aeterni-{component}` 为根选择器。
- 用户传入的 `Class` 只能作为扩展入口，不能改变基类属性合并规则。

### 5.7 结构稳定性

组件结构必须只有一个真相源：

- **一个根元素、一份内容标记。** 不要用 `@if` 给同一个组件的两种模式各写一份相同的子树（把差异放进 `BuildClass()` / `BuildAttributes()`），否则后续只改一份就会分叉。
- **根属性一律走 `BuildAttributes()`。** 除文档化的例外（`ThemeProvider` 不渲染 DOM）外，不要用 `class="@SomeBuilder()"` 手写根元素属性：那会让 `Id`、`Class`、`Style`、`Visible`、`AdditionalAttributes` 静默失效。
- **子元素 class 也用 `ClassBuilder`。** 不要用 `List<string>` + `string.Join` 手工拼接。
- **事件只在必要时绑定。** 非交互组件不应注册 DOM 监听器：处理属性可以返回 `default` 的 `EventCallback<T>`（渲染树不产生处理帧），不要把判断全部丢进处理器内部。
- **图标尺寸只有一个入口。** 通过祖先元素的 `--aeterni-icon-render-size` 传递；不要用 `font-size` 依赖 Icon 的 `1em` 回落，也不要用 `::deep svg { width/height }` 覆盖。
- **有可见根元素的组件必须绑 `@ref="RootElement"`**，否则 `Element` / `ElementChanged` 永远不会回调。
- **禁用态用 Token，不要整块降透明度。** 使用 `--aeterni-state-color-disabled` / `--aeterni-state-background-disabled` / `--aeterni-state-border-disabled`；`opacity` 只用于组件局部的光学微调（加载中的标签、弱化的字形）并在注释里说明。
- **同一角色的魔数只写一次。** 已经存在的 Token（如 `--aeterni-opacity-muted`、`--aeterni-radius-badge`）不要在旁边再写一份字面量；先看 Token 层是否已有对应角色。

## 6. 状态和交互规范

### 6.1 标准状态

交互组件应根据组件语义实现适用状态：

```text
base
hover
focus-visible
active
disabled
loading
selected / checked
invalid
readonly
```

不适用的状态不需要强行实现。例如静态展示组件不需要 `active`。

### 6.2 状态优先级

状态样式应遵循以下优先级：

```text
disabled > loading > invalid / selected > focus-visible > hover > base
```

具体组件可以调整优先级，但必须确保禁用状态不会被悬浮或选中状态覆盖。

### 6.3 Hover、Active 和 Focus

- `hover` 用于反馈可交互性，可以改变背景、边框、颜色和阴影。
- `active` 用于表达按下反馈，可以改变背景或阴影。
- `focus-visible` 必须有清晰的焦点轮廓，不能只依赖颜色变化。
- 不能通过 `outline: none` 删除焦点而不提供替代方案。
- 位移、缩放或阴影变化不能造成布局尺寸变化。
- 对于 ThemeSwitch 这类滑块组件，按下时保持尺寸和位置稳定，滑块只在选项切换时移动。

### 6.4 动画

动画只使用已有 duration 和 easing Token：

```css
transition: var(--aeterni-transition-button);
```

通用要求：

- 背景、颜色、边框、阴影和透明度可以过渡。
- 变换动画应使用 `transform`，不要改变布局属性。已登记的例外：折叠面板的自动高度展开/收起使用 `grid-template-rows: 0fr → 1fr`（`Menu` 的 `aeterni-menu__collapse`），因为在不引入 JS 测量高度的情况下它是不改变 DOM 结构的唯一方案；该属性只能用在自有折叠容器上，且必须同时兼容 `prefers-reduced-motion: reduce`。
- 不为点击添加夸张的缩放或跳动效果。
- 不使用无限循环动画表达普通交互状态。
- 必须兼容 `prefers-reduced-motion: reduce`。

## 7. HTML 语义和可访问性

### 7.1 原生元素优先

优先使用正确的原生元素：

- 动作使用 `<button>`。
- 导航使用 `<a>`；组件在提供导航项时，应像 `Menu` 一样允许消费者传入 `Href`（`Target="_blank"` 时自动补 `rel="noopener noreferrer"`），不要只能用 `@onclick` 模拟跳转。
- 输入使用 `<input>`、`<textarea>`、`<select>`。
- 分组使用 `<fieldset>` 和 `<legend>`。
- 弹窗使用合适的 dialog 语义。

不要用 `<div @onclick="...">` 模拟按钮，除非组件本身是允许完整内容结构的交互容器。此时必须补齐 `role="button"`、Tab 焦点、Enter/Space 键盘行为和禁用语义；Card 的 `Interactive` 模式属于这一明确例外。

### 7.2 Disabled 和 Loading

原生支持 `disabled` 的元素应使用原生属性。不能禁用原生元素的组件，应至少提供：

```html
aria-disabled="true"
```

加载状态应：

- 阻止重复提交。
- 保留组件的稳定尺寸。
- 为屏幕阅读器提供可理解的状态。
- 不仅依赖旋转图形表达加载中。

### 7.3 ARIA

ARIA 用来补充语义，不用来替代正确的 HTML 元素。每个 ARIA 属性都必须对应一个真实的交互状态。组件的 `AriaLabel` 等参数只在文本内容不能提供可访问名称时使用。

带值的 ARIA 状态必须输出字符串：Blazor 把 `bool` 属性值渲染成最小化属性（`aria-selected` 而不是 `aria-selected="true"`，为 `false` 时直接省略），两者对读屏都是无效值。统一写成 `? "true" : "false"`；原生布尔属性（`disabled`、`hidden`、`checked`）不受影响。

复合控件的角色层次必须成立：`listbox` 的直接子元素是 `role="option"`，`radiogroup` 内恒有至多一个 `aria-checked="true"`，非可聚焦的选项元素不得进入 Tab 序列，也不得抢走容器的 DOM 焦点。单选组、评分等复合控件的键盘模型使用 roving tabindex：组件整体只有一次 Tab 停留点。

表单字段的关联有三种合法形态：原生可聚焦控件（`Input`、`Textarea`、`Checkbox`、`Switch`、独立 `Radio`）直接采用 `FormField` 的输入 ID 让 `label for` 生效；容器型控件（`ComboBox`、`Rating`、`RadioGroup`）既采用输入 ID 以保持 `for` 可解析，又必须用 `aria-labelledby` 关联标签 ID、用 `aria-describedby` 关联描述与错误文本。

库内所有用户可见文案（无障碍名称、占位符、关闭按钮文案）统一取自 `AeterniUIOptions.Text`，不得在组件里硬编码中英文混排的默认值；组件参数优先于文案表。

### 7.4 键盘行为

交互组件至少检查：

- Tab 是否可以进入和离开。
- Enter 和 Space 是否符合原生预期。
- Escape 是否用于关闭可关闭的浮层。
- 方向键是否用于 tabs、列表选择或分段控件。
- 焦点是否在状态变化后保持合理位置。

## 8. JS isolation 规范

### 8.1 使用条件

只有以下场景才引入 JS：

- DOM 测量和定位。
- 焦点控制或焦点陷阱。
- 点击外部关闭。
- 滚动锁定。
- ResizeObserver、IntersectionObserver 等浏览器观察器。
- Tauri 窗口或原生宿主 API。
- CSS 和 Blazor 无法可靠完成的浏览器行为。

以下场景不要使用 JS：

- 普通 hover、focus、active 样式。
- 普通过渡和 transform 动画。
- 仅为了切换 class 的简单状态。
- 组件 C# 已经可以直接维护的业务状态。

### 8.2 声明方式

一个组件最多声明一个 `JsModuleAttribute`。该 module 是组件唯一的 JS module；`Interactive` 只用于判断是否需要执行 `init`，不表示组件可以声明多个 module。

组件通过 `JsModuleAttribute` 声明 module：

```csharp
[JsModule(
    "Components/Dialog/Dialog.razor.js",
    Name = "dialog",
    Interactive = true)]
public partial class Dialog : AeterniComponent
{
}
```

约定：

- 路径相对于组件库程序集的静态资源根目录。
- `Name` 必须稳定且具有组件语义。
- 需要执行 `init` 或接收 C# 回调时才设置 `Interactive = true`。
- `Interactive = false` 时 module 可以被加载，但不会执行 `init`；组件销毁时仍由基类执行实例级 `dispose`。
- 不在组件初始化阶段访问浏览器；浏览器调用放在首次渲染之后。

### 8.3 JS module 生命周期

组件 module 必须提供实例级 `dispose`。当 `Interactive = true` 时还必须提供 `init`。`AeterniComponent` 基类会自动调用它们：

```javascript
const instances = new Map();

export function init(reference, key) {
    dispose(key);
    instances.set(key, createInstance(reference));
}

export function dispose(key) {
    const instance = instances.get(key);
    instance?.dispose?.();
    instances.delete(key);
}
```

要求：

- 所有监听器、Observer、定时器都必须在 `dispose` 中释放。
- 使用 `InstanceId` 作为实例 key。
- 组件不需要自行调用 `dispose`；基类会在组件销毁时调用 `dispose(InstanceId)`。
- JS 回调失败不能让页面永久崩溃，应根据组件能力提供降级行为。
- JS 不直接保存组件业务状态，状态源仍是 C# 或 DOM 原生状态。
- 不为了绕过静态资源完整性问题而关闭 integrity 校验。

### 8.4 Tauri 兼容

Tauri 能力必须是可选的：

- 浏览器中没有 `window.__TAURI__` 时，组件仍然正常运行。
- Tauri API 调用需要捕获失败并允许 Web 环境继续使用。
- 页面主题和 titlebar 主题必须由同一个 `ThemeService` 状态驱动。
- 不把 Tauri 专用代码写入普通组件的核心渲染逻辑。

### 8.5 共享浮层能力

锚定浮层（Tooltip、Popover、后续的 Drawer）共用 `wwwroot/js/aeterni_floating.js`，不得各自重写视口贴合、焦点陷阱与滚动锁：

- `fitsSide` / `oppositeSide` / `clampCenteredShift` / `clampAlignedShift` —— 翻转与交叉轴贴边的全部数值。居中对齐（Tooltip）与边对齐（Popover）用不同的 clamp，不要拿一个公式套两种几何。
- `createFocusTrap(container, { onEscape })` —— Tab 循环、Escape 上报、释放时把焦点还给打开前的元素；返回幂等的 `release()`。
- `lockScroll()` —— 带滚动条宽度补偿、引用计数的页面滚动锁；返回幂等的释放函数，重叠浮层不会互相解锁。
- 组件模块通过相对路径导入（`../../js/aeterni_floating.js`）。共享文件放 `wwwroot/js/`，因为它不属于任何单个组件；而组件模块必须是组件目录下的 `*.razor.js`（只有这个命名会被 Razor SDK 当作静态资源发布）。
- **浮层不得用 `transform`/`translate` 做偏移**：变换会让浮层成为自身 `position: fixed` 遮罩的包含块。需要像素级偏移时用 `margin`（Popover 用 `margin-left` 承载 `--aeterni-popover-shift-x`）。
- 关闭始终由使用方的 `OpenChanged` 驱动：JS 只负责“请求关闭”，不直接改 DOM 状态，也不假设参数未被绑定。
- 堆栈类浮层（Dialog）可以保留自己的 Tab/Escape 处理，因为它要判断“只有最顶层响应”；这类差异必须在代码注释与当前功能文档里写明。

## 9. 主题规范

### 9.1 状态定义

主题服务中的两个概念必须区分：

- `Mode`：用户选择的来源，`System`、`Light` 或 `Dark`。
- `CurrentTheme`：当前实际生效的主题，类型为 `ThemeKind`（`Light` 或 `Dark`）。

System 模式下，`CurrentTheme` 由系统主题决定；Light 或 Dark 模式下，`CurrentTheme` 由用户选择决定。

### 9.2 组件约束

组件不得：

- 自己读取操作系统主题。
- 自己修改 `data-theme`。
- 自己操作 titlebar 主题。
- 直接调用 `ThemeProvider` 的内部 JS module。

组件只需要消费语义 Token，或者订阅 `ThemeService.ThemeChanged` 来刷新组件自身的展示状态。

### 9.3 ThemeProvider

`ThemeProvider` 是无可见内容的宿主组件，通常放在 Layout 中：

```razor
<ThemeProvider />
@Body
```

它负责：

- 订阅 `ThemeService`。
- 初始化页面主题。
- 监听系统主题变化。
- 应用网页主题和 Tauri 原生主题。
- 释放 JS 监听和对象引用。

它不包裹 Layout，也不承担业务布局职责。

`ThemeProvider` **不渲染 DOM**：主题通过 JS module 写到 `<html>` 的 `data-theme` / `data-aeterni-mode` 上，因此基类提供的 `Id`、`Class`、`Style`、`Visible` 对它是无效参数，文档中必须这样说明，不能暗示它支持 DOM 参数。

为避免首帧主题闪烁，宿主应在样式表之前放一段预渲染脚本（读取 `aeterni.theme.mode` 与 `prefers-color-scheme` 并写入 `<html data-theme>`）。样式表本身也提供 `prefers-color-scheme: dark` 兜底：只有在没有显式主题属性时才生效。

### 9.4 DialogProvider 和 IDialogService

`DialogProvider` 与 `ThemeProvider` 一样是无业务内容的自闭合宿主组件，通常在 Layout 中注册一次：

```razor
<ThemeProvider />
<DialogProvider />
@Body
```

业务组件只注入 `IDialogService`，不直接操作 Provider：

```csharp
await DialogService.AlertAsync("Saved", new AlertOptions
{
    Severity = Severity.Success,
    Icon = CheckIcon
});

var confirmed = await DialogService.ConfirmAsync("Continue?");
DialogService.ShowToast("Completed");
```

全局 Toast 位置等基础配置在服务注册阶段设置，组件运行过程中也可以通过 `IDialogService` 临时覆盖：

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

指定单条 Toast 或 Alert 的位置可以使用对应 options 的 `Position` 属性，或者直接使用 Service 的位置重载：

```csharp
DialogService.ShowToast("Completed", ToastPosition.BottomCenter);
await DialogService.AlertAsync("Saved", ToastPosition.TopCenter);
```

三种弹出内容的职责必须区分：

- `Show` / `ConfirmAsync` 是模态内容，显示遮罩、锁定背景滚动并管理焦点；默认只有最顶层 Dialog 响应 Escape。
- `AlertAsync` 是非模态、不阻塞页面的语义化提示，默认底部居中，可指定任意 `ToastPosition` 位置以避开页面顶部元素；支持 `Severity`、`Icon` 和手动关闭。消息内容最多显示两行，过长内容截断。
- `AlertAsync` 和 `ShowToast` 默认自动关闭，四边环绕的边框进度条由 CSS 线性动画驱动（不显示倒计时文本，渲染过程不触发中间态重绘，多消息同时显示时也不会抖动）；将对应 options 的 `Duration` 设置为 `TimeSpan.Zero` 可改为仅手动关闭，设置为 `null` 时使用 `AddAeterniUI` 的全局默认值。
- Alert 与 Toast 的卡片采用类似 iOS 通知中心的展示方式：磨砂圆角卡片、左侧图标徽标、标题与两行内内容、右侧顶部的轻量关闭按钮。
- Alert 和 Toast 的语意背景沿用 Button 的 `Info`、`Success`、`Warning`、`Danger` 色值映射；`Blur = false` 可以关闭通知自身的背景模糊。
- Alert 与 Toast 采用左侧图标、中间内容、右侧关闭按钮的三段式布局；未传 `Icon` 时按 `Severity` 使用内置 `AeterniIcons` 严重度图标，关闭按钮只占自身内容宽度并保持上下居中。
- 需要在关闭后执行逻辑时使用 `AlertOptions.OnClosedAsync` 或 `ToastOptions.OnClosedAsync`。
- `ShowToast` 是非阻塞通知，支持 `Info`、`Success`、`Warning`、`Danger`、手动关闭、自动关闭以及八个位置。

Toast 的全局默认位置由 `IDialogService.DefaultToastPosition` 提供（默认 `TopEnd`，右上角），单条 Toast 可以通过 `ToastOptions.Position` 覆盖。Alert 的全局默认位置由 `IDialogService.DefaultAlertPosition` 提供（默认 `BottomCenter`，底部居中），单条 Alert 可以通过 `AlertOptions.Position` 覆盖；两者都可在 `AddAeterniUI` 的 `AeterniUIOptions` 中设置默认值。需要 RTL 兼容时优先使用 `Start` / `End`，不要在业务层重新实现定位 CSS。

Dialog、Alert 和 Toast 的消息内容应保持纯文本安全输出；需要复杂结构时使用 `Show(RenderFragment, DialogOptions)`，并避免在交互式 Dialog 内嵌套另一个模态入口。

## 10. 示例项目规范

示例项目不是营销页面，而是组件库的可运行契约展示。每个组件示例应尽量覆盖：

- 默认状态。
- 所有公开尺寸。
- 主要变体。
- Hover、Focus、Disabled、Loading 等适用状态。
- 键盘行为。
- 主题切换后的结果。
- Web 和 Tauri 环境下的降级行为。

示例页面中的样式可以用于展示，但不能反向成为组件库的实现依赖。示例项目可以使用自己的页面 class，但不得覆盖 `.aeterni-*` 的核心规则来伪造组件效果。

推荐的示例结构：

```text
Sample/
  Pages/
    Home.razor        # 首页文档页（/）
    Home.razor.css
    Components.razor  # 组件文档页（/components）
    Components.razor.css
  Layout/
    MainLayout.razor  # 固定头部与导航 + 全局 Provider
```

组件变更后，示例页面必须同步展示真实参数和真实状态，不能使用静态文本模拟组件已支持的能力。

## 11. 新增组件工作流

新增组件按以下顺序进行：

1. 明确组件解决的问题，以及它与现有组件的边界。
2. 确认是否可以通过现有组件组合完成，避免重复组件。
3. 设计公开参数、枚举和事件回调。
4. 明确根 HTML 元素和可访问性语义。
5. 列出适用状态：base、hover、focus、active、disabled、loading 等。
6. 将视觉决策映射到已有 Token。
7. 创建组件目录和 Razor 文件。
8. 继承 `AeterniComponent`，实现 `BuildClass()`，必要时实现 `BuildStyle()`。
9. 使用 `@attributes="BuildAttributes()"` 渲染根元素。
10. 只有确有必要时添加 JS module，并导出符合约定的 `dispose(instanceId)`。
11. 在示例项目中展示默认、变体、尺寸和状态。
12. 进行人工检查，确认 Web、主题切换和 Tauri 降级行为。

## 12. 组件提交检查清单

### API

- [ ] 组件继承 `AeterniComponent`。
- [ ] 参数名称和顺序符合规范。
- [ ] 没有创建不必要的 `Ixxx` 接口或基类。
- [ ] 事件使用 `EventCallback`，没有 `async void`。
- [ ] 默认参数可以支持最简单的使用方式。

### DOM 和基类

- [ ] 有明确且正确的根 HTML 元素。
- [ ] 使用 `BuildAttributes()` 传递基类属性。
- [ ] 没有重复生成 `id`、`class` 或 `style`。
- [ ] 没有自行创建 Guid id。
- [ ] `Disabled` 的语义与根元素能力一致。
- [ ] 可见根元素绑定了 `@ref="RootElement"`。
- [ ] 两种渲染模式共用同一份内容标记（模式差异只在 class/属性）。
- [ ] 非交互分支不注册 DOM 事件处理器。
- [ ] 禁用态使用 state-disabled Token，而不是整块 `opacity`。

### 样式

- [ ] 根 class 使用 `aeterni-{component}`。
- [ ] 变体和状态 class 命名统一。
- [ ] 主题颜色使用 `--aeterni-*` Token。
- [ ] 填充档只用于实心表面；ink 与需与浅色轨道/页面区分的实心图形用文字形态。
- [ ] 容器背景是无色的（R = G = B，深色 ≤ +3）。
- [ ] 文字只用 `primary` / `secondary` / `placeholder` / `disabled`；`tertiary` 只用于图标与装饰。
- [ ] 尺寸只用 `--aeterni-spacing-*`，不用 gap/padding/margin 别名。
- [ ] 已经按「表面归属」确认过本组件是否该自带表面，并与规范表格一致。
- [ ] 没有覆盖其他组件或宿主项目的全局元素样式。
- [ ] 尺寸变化不会造成布局跳动。
- [ ] 动画使用统一 duration 和 easing。
- [ ] 支持减少动画偏好。

### 可访问性

- [ ] 使用了正确的原生 HTML 语义。
- [ ] 键盘可以完成核心操作。
- [ ] `focus-visible` 清晰可见。
- [ ] Disabled、Loading、Selected、Invalid 等状态有对应语义。
- [ ] 带值的 ARIA 状态输出字符串而不是 `bool`。
- [ ] 没有用视觉效果替代必要的文本或 ARIA 语义。

### JS 和主题

- [ ] JS 只用于必要的浏览器行为。
- [ ] JS module 的监听器和定时器可以释放。
- [ ] 浏览器没有 Tauri API 时仍能工作。
- [ ] 组件没有自己修改主题根节点。
- [ ] 主题切换后组件状态仍然正确。

### 示例

- [ ] 示例使用真实组件，而不是静态仿制样式。
- [ ] 示例覆盖主要参数和适用状态。
- [ ] 示例没有依赖组件内部 class 实现业务逻辑。
- [ ] 示例页面在 Light、Dark、System 三种模式下都可读。

## 13. 第一阶段落地范围

本规范第一版落地时，优先统一以下组件和能力：

```text
Button
ButtonGroup
Surface
Input
FormField
```

`Stack`、`Flex` 曾列入本阶段范围，但从未实现，当前 roadmap 也没有排期；
布局能力由 `Surface`、`Card` 与业务自身的布局样式承担。规范中不再把它们
写成已落地能力，需要的组件应先进入 roadmap 再实现。

`IconButton` 暂不纳入当前阶段。Button 已通过 `Icon`、`StartIcon` 和 `EndIcon` 支持带图标操作；只有在独立图标操作的 API 需求明确后，再单独设计 IconButton。

其中 `Button` 作为第一份参考实现，重点验证：

- `Variant`、`Color`、`Size` 的 API 设计。
- `ButtonVariant` 提供 `Default`、`Outline`、`Ghost`、`Text`、`Link`。
- `Ghost` 用于保留完整控件区域的低强调操作；`Text` 用于更紧凑的文字操作，不应复用 Ghost 的边框和背景强度。
- `Link` 仅通过颜色和下划线表达悬浮、按下反馈，不显示背景色；它适合导航或文本链接语义。
- `Color.Default` 映射到品牌主色，`Size.Default` 映射到默认中等尺寸。
- `FullWidth` 默认关闭，启用后按钮宽度为父容器的 100%。
- `StartIcon` 和 `EndIcon` 使用 `RenderFragment`，分别表示内容左侧和右侧图标；`Icon` 保留为左侧图标的兼容简写。
- 图标按钮必须通过 `AriaLabel` 提供可访问名称。
- `Type` 支持 `Button`、`Submit` 和 `Reset`，默认值为 `Button`。
- Disabled、Loading、Focus 和键盘行为。
- Token 使用方式。
- `BuildClass()` 和 `BuildAttributes()` 的组合。
- 示例页面如何展示组件状态。

`ButtonGroup` 的第一版约定如下：

- 默认使用水平、连接式布局；设置 `Connected="false"` 时恢复按钮之间的 token 间距并保留 Button 自身的默认圆角。
- 连接式布局在相邻 Button 之间保留 1px token 分隔线，避免相同颜色的实心按钮视觉上合并；分隔线由 ButtonGroup 管理，不修改 Button 核心样式。
- `Orientation="Orientation.Vertical"` 用于垂直组合。
- `FullWidth` 让组占满父容器，并让内部 Button 平均分配可用宽度。
- `Disabled` 通过级联上下文传递给内部 Button，不只在容器上添加视觉状态。
- 普通按钮组保留原生 Tab 顺序，不实现方向键导航；方向键行为留给后续 Toolbar 或 ToggleGroup（两者均未实现，roadmap 中无排期）。

`Surface` 和 `Card` 的职责需要区分：

- `Surface` 是无内容结构的视觉容器，负责背景、边框、模糊、阴影、圆角和内边距。
- `Card` 是有内容结构的容器，可提供 `Header`、主体 `ChildContent` 和 `Footer` 三个区域，默认带边框并使用卡片圆角。
- Card 的 `Padding` 统一作用于 Header、Body 和 Footer 三个区域；设置为 `None` 才表示三个区域都采用无内边距，不能依赖业务 CSS 为各区域重复补间距。
- 两者都使用 `SurfaceVariant`、`SurfaceElevation` 和 token 化的内边距；不要在业务页面重复实现相同的 surface CSS。
- `Card` 默认不是交互控件，不输出按钮或链接语义；需要整卡触发动作时使用 `Interactive="true"` 和 `OnClick`，组件会提供按钮语义、Tab 焦点以及 Enter/Space 键盘触发。
- 交互式 Card 内不要嵌套 Button、Link 或其他可聚焦控件。如果卡片主要用于导航，优先使用页面中的 Link；如果同时存在多个独立动作，应保持 Card 为静态容器并把 Button 放在 Footer。

基础用法：

```razor
<Surface Variant="SurfaceVariant.Glass" Elevation="SurfaceElevation.Medium">
    Content
</Surface>

<Card Variant="SurfaceVariant.Elevated">
    <Header>Title</Header>
    Content
    <Footer>Actions</Footer>
</Card>

<Card Interactive OnClick="OpenDetails">
    Open details
</Card>
```

后续组件如果与本规范冲突，应优先修改规范或明确记录例外，再实现组件。不能在单个组件中悄悄形成新的命名、状态或样式体系。
