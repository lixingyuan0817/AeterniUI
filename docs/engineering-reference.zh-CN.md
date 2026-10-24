# AeterniUI 工程设计参考

文档状态：工程原理、特殊配方、实现范式与验证案例参考；版本以 `Directory.Build.props` 为准

适用范围：`src/AeterniUI` 中的所有 Blazor 组件，以及 `src/AeterniUI.Sample` 中的组件示例。

具有约束力的项目索引与开发规范统一维护在根目录 [`AGENTS.md`](../AGENTS.md)。本文档解释规则背后的设计依据、特殊组件机制和可复用实现范式；与 `AGENTS.md` 冲突时以 `AGENTS.md`、实际代码和 CI 为准。

## 1. 目标

AeterniUI 同时服务于 Blazor Server 和 Blazor WebAssembly。组件必须尽量保持以下特征：

- 在不同宿主中拥有一致的 API、语义和视觉结果。
- 默认不依赖 JavaScript，只有浏览器能力无法由 CSS 或 Blazor 完成时才使用 JS isolation。
- 只依赖语义化 Token，不在组件中重新定义颜色体系。
- 参数、状态、事件、DOM 属性和销毁行为具有统一约定。
- 组件可以被单独使用，也可以被组合到更复杂的组件中。
- 视觉风格由组件库维护，业务项目只通过公开参数和扩展类名进行定制。

本参考优先解释一致性与可维护性要求背后的工程依据，不追求一次性覆盖所有组件类型。

### 1.1 快速复核

新增或修改组件时的必读路由、执行顺序与按改动类型的门禁清单见 [`AGENTS.md`](../AGENTS.md) §4.1、§4.7、§4.8。本文只解释这些规则背后的依据、反例与测量方法。

## 2. 当前架构约定

### 2.1 组件继承关系

所有组件直接或间接继承 `AeterniComponent`；不为“元素能力”和“JS 能力”分别创建公共基类，也不要求实现 `Ixxx` 接口。基类提供实例 ID、`Class`/`Style`/`AdditionalAttributes`、`Visible`/`Disabled`/`ElementReference`、`ClassBuilder`/`StyleBuilder`、可选的 JS module 生命周期与 `ElementChanged`（现行参数清单见 [`delivered-features.zh-CN.md`](delivered-features.zh-CN.md) §2）。

只有某种行为被至少两个组件重复使用、且无法通过组合解决时才增加抽象；新增抽象必须先说明它消除的重复，不能只为“层次看起来完整”而创建基类（失败先例：`Stack`、`Flex` 曾按“布局组件应该有”的直觉列入计划，最终从未实现，布局由 `Surface`、`Card` 与宿主样式承担）。

### 2.2 目录与文件职责

一组件一目录（Razor、代码后置、隔离样式与可选 JS 同目录），公共基础类型放在明确目录；现行目录与聚合例外见 [`AGENTS.md`](../AGENTS.md) §3.3。为什么组件样式不能写进 Token 文件：CSS isolation 的作用域属性只加在组件自己渲染的元素上，混入全局文件会让同一组件出现两套作用域与两套优先级（详见 §5.6）。

## 3. 组件公共 API

### 3.1 参数命名

命名保留词与总规则见 [`AGENTS.md`](../AGENTS.md) §4.3。判断依据只有两条：不用含义重复的参数（`IsDisabled`、`Enable`、`CustomClassName`），布尔参数统一正向语义；公共组件名不加厂牌前缀，组件以领域名命名，避免同一个组件在宿主里出现两个名字。

### 3.2 参数顺序

顺序规则见 [`AGENTS.md`](../AGENTS.md) §4.3。它解决的是阅读顺序问题：先把状态、外观、内容、事件依次排开，读者不必在参数面里来回找。默认值必须让最简单用法可运行：

```razor
<Button>Save</Button>
```

### 3.3 枚举参数

有限且稳定的选项用枚举而不是字符串常量；枚举属于公开 API，成员用 PascalCase 并取视觉/交互领域通用词，不绑定具体页面。不要为只有“开/关”两个状态的参数创建枚举（`Disabled` 而不是 `DisabledState`）；表达方向或形态的二值参数可以用枚举，因为它的取值域不是布尔（`Orientation.Horizontal/Vertical`、`TextareaResize.None/Vertical`）。

### 3.4 事件回调

事件使用 `EventCallback` / `EventCallback<T>`，处理方法返回 `Task`，禁止 `async void`；值组件遵循 Blazor 标准绑定三件套 `Value` / `ValueChanged` / `ValueExpression`（规则见 [`AGENTS.md`](../AGENTS.md) §4.3）。组件内部触发回调前先更新自身状态，除非该事件明确要求由消费者决定状态——否则受控与非受控两条路径会看到不同的中间状态。

### 3.5 子内容与图标作用域

可组合组件用 `RenderFragment`；不要同时提供多个表达相同内容的参数（`Tooltip` 的 `ChildContent` 是触发元素、`Text` 是提示内容，属于分工而非重复）。

组件内部字形使用核心 `AeterniIcons`（供应商无关的 16 × 16 几何），业务图标通过独立适配包提供，核心库不依赖具体图标供应商。

写样式时最容易踩的作用域陷阱：CSS isolation 的作用域属性只加在组件自己文件里渲染的元素上，子组件根元素拿不到父组件作用域，因此 `.aeterni-parent__part { }` 无法命中 `<Icon Class="aeterni-parent__part" />` 渲染出的 `<svg>`。可行做法是：用包装元素承载 class（尺寸与颜色靠继承）、改用 `::deep`，或通过 `Icon` 读取的局部变量 `--aeterni-icon-render-size`（默认回退 `1em`，在祖先元素上设置即可传递尺寸，它不是全局 Token）在祖先元素上传递。

## 4. 基类使用依据

### 4.1 根元素渲染

根属性一律走 `BuildAttributes()`；`@attributes` 之后不要再写显式 `class`，显式属性会覆盖属性集合里的值，条件类名应放进 `BuildClass()`。基类的当前约定是：先复制 `AdditionalAttributes`，再统一写入 `id`、`class`、`style`；`Visible == false` 时写 `hidden` 并追加内联 `display: none`——这一条是为了避免组件的 flex/grid 布局覆盖原生隐藏规则，恢复可见时移除该内联声明；`SupportsDisabled` 被重写后才输出 `disabled` 与 `aria-disabled`。组件不得自行实现第二套根属性合并逻辑，否则两套顺序会互相覆盖且问题只在特定参数组合下出现。

### 4.2 单根组件与复合组件的禁用语义

`Disabled` 取决于根节点是否就是实际交互元素：单根交互组件（`Button`、`Input`）重写 `SupportsDisabled`，由基类向根元素输出 `disabled` 与 `aria-disabled`；复合组件（`ThemeSwitch`、`ComboBox`）根节点是容器，不应伪造原生禁用能力，而应在内部真正的交互元素上应用 `disabled`，根容器按语义输出 `aria-disabled`。复合组件内部不要再对每个子控件调用 `BuildAttributes()`。原因：容器与内部控件重复表达 Disabled 会让读屏听到两次禁用，也会把 `disabled` 加到不支持它的 HTML 元素上。

### 4.3 class 与 style 构建

class 从 `base.BuildClass()` 起链式构建，命名规则见 [`AGENTS.md`](../AGENTS.md) §4.2；不要用 `List<string>` + `string.Join` 手工拼接，也不要让同一个类名有多套生成路径。`BuildStyle()` 只承载确实属于组件 API 的动态 inline style（动态数值、用户传入尺寸、浏览器运行时值），不用它复制整套主题样式——inline style 会压过宿主与主题表，且无法被 `prefers-*` 媒体查询覆盖。

### 4.4 Id 和 ElementReference

`InstanceId` 是组件实例内部稳定标识（JS module 与内部关联使用）；`ElementId` 是渲染到 DOM 的 id，没有 `Id` 时由基类生成；组件不得自行创建另一套 Guid id。JS 需要关联实例时传 `InstanceId`，不要从 DOM 文本推断状态；`@ref` 通过 `RootElement` 或组件专用的 `ElementReference` 管理，有可见根元素却不绑定 `@ref` 会让 `Element` / `ElementChanged` 永远不回调。

## 5. Token 和样式依据

### 5.1 Token 是唯一视觉来源

组件样式只消费 `--aeterni-*` Token（规则见 [`AGENTS.md`](../AGENTS.md) §4.4）。允许字面量的场景只有：不表达主题含义的尺寸（`1px` 边框）、纯布局微调、图标内部固定几何、经设计确认且尚无 Token 的阴影/透明度。判断标准是“这个值换主题时该不该变”：该变就必须是 Token；重复出现三次以上的例外应升级成新 Token，而不是在多个组件里复制。

### 5.2 命名空间

公开 Token 一律 `--aeterni-` 前缀，组件专用 Token 在名称中包含组件名；不得创建无前缀公共 Token，也不要覆盖 Bootstrap、浏览器或宿主的通用变量——一旦覆盖，宿主无法判断是库泄漏还是自己的样式。

### 5.3 颜色角色

背景 Token 分两类，不能混用：页面与容器背景用 `--aeterni-bg-*`、`--aeterni-bg-elevated`、`--aeterni-surface-soft` 等中性背景（不使用品牌色，避免 Card/Surface/页头/侧边栏形成大面积彩色底）；交互状态用 `--aeterni-state-*` 系列（选中、悬停、按下、焦点反馈），不作为页面或容器的默认背景。

`--aeterni-state-background-checked` 是实心填充而不是淡染，取值就是品牌填充档，因此其上的墨用 `--aeterni-text-inverse`。已勾选的控件在指针下必须整块换档（`-checked-hover` / `-active` 沿同一色阶再走一档，边框与填充一起换）；用半透明 `--aeterni-state-background-hover` 洗浅会让 inverse 墨落在浅底上，勾线读不出来。`Radio` 的圆环与圆点、`Switch` 的轨道与边框必须一起换档，否则同一控件会同时出现两种品牌色。

两个高频失败案例：

- 校验失败的控件在悬停/按下时必须保留危险色：`invalid` 的守卫要排除指针反馈（`.is-invalid` 前缀），否则 `--aeterni-state-border-hover` 覆写 `--aeterni-state-border-invalid`，指针一划过错误提示就消失。
- 悬浮规则必须排除已选中态（`:not(.is-selected)`／`:not(:checked)`／`:not(.is-current)`）；`hover` 与选中态特异性相同时胜负由声明顺序决定，最容易静默失守。

**表面归属**：写样式前先确认组件是否该自带表面，两类结论相反。

| 分类 | 组件 | 规则 |
| --- | --- | --- |
| 自带表面 | `Surface`、`Card`、`List`、`Input`/`Textarea`/`ComboBox` 的控件盒、`Checkbox`/`Radio`/`Switch` 的指示块、`Popover`/`Tooltip`/`Dialog`/Alert/Toast 的浮层、`Tag`/`Button` 的实心与柔和变体 | 自身负责 `--aeterni-bg-*` 或语义填充色，可以单独使用；外围不得再套一层同色底 |
| 内容控件（不自带表面） | `Menu`、`Tabs`、`Rating`、`FormField`、`Label`、`Icon`、`Progress`、`ButtonGroup`、`Button` 的透明变体 | 只画内容与交互状态；底由宿主容器提供，不得自行加 `--aeterni-bg-*` |

判断依据是“这个组件能不能单独构成一块界面”：导航列表、标签页、评分天生嵌在容器里使用，给它们加底会在卡片/侧栏里形成“白底套白底 + 双层边框”；`List` 是一次性交付“选项容器”的可选中列表，所以自带表面。示例页必须展示真实用法——把内容控件放进宿主容器里演示。

两条显式例外：控件静止描边（`--aeterni-border*` / `--aeterni-control-border`）是低强度 hairline，不参与非文本 3:1 契约，控件识别由标签、占位符、悬浮边框与焦点环共同承担；装饰色 `--aeterni-decor-*` 不以语义角色出现、不参与对比度契约，只服务 `FlashCard` 的光学饰面，与品牌故意无关（光栅是被表面分解的白光，不是品牌色）。

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
- **每个色相层都要按本节配方完整重建**：新增品牌色相时不能只替换 500 档，也不能照抄另一色相的档位——各档明度与 `-text` 停靠档必须按该色相自身重挑，但浅端仍统一到 L 0.976、深端统一到 L 0.30，chroma 仍在中档收敛。
- **chroma 剖面必须用绝对值，不能套用「占本色相色域宽度的比例」**：sRGB 色域宽度随色相与明度变化，绿色在 L 0.85 附近最宽（maxC ≈ 0.27），紫色恰好相反——L 0.57 附近 ≈ 0.27，到 L 0.85 只剩 ≈ 0.08。按相对比例迁移会把绿色的峰值挤到浅档并产出荧光绿（`#AEFDAB`）。正确做法是把源色相的**绝对 chroma 剖面**归一化到 500 档峰值后套用，紫罗兰即为 `50:0.081, 100:0.183, 200:0.355, 300:0.565, 400:0.807, 500:1.0, 600:0.941, 700:0.812, 800:0.656, 900:0.522`。
- **500 档明度按该色相的对比度行为定，不能只按「看起来像不像」定**：chroma 对白字对比度的作用方向会随色相反转——紫罗兰提高 chroma 提升白字对比度（4.65 → 5.13），绿色提高 chroma 反而降低（5.46 → 5.18）。因此绿色填充档必须比紫罗兰更低：紫罗兰 500 档 L 0.565，绿色锚在 L 0.552（外部参考绿 `#1F883D`）时承白字 4.52:1，刚好过 4.5:1 线。**余量低于 1.1× 的锚点必须显式记账**：绿色 500 当前的 1.004× 就是全库最紧的一条对比度门禁，任何进一步提亮、提高 chroma 或改动语义停靠档都会击穿它；换锚点或新增色相层时把门禁集合重跑一遍，并把新的最紧余量写进注释与对应变更记录。锚点必须**枚举扫描**确定，不能二分——对比度门禁集合对 500 档明度非单调。语意族同样适用这条记账规则：当前最紧的是 `info` 实心承 `--aeterni-color-on-semantic` 的 **4.59:1（margin 1.02）**，改动 info 500 档或 on-semantic 前先复算这一组。
- **上述门禁由 `node scripts/check-contrast.mjs` 实测执行，不是人工校对**：脚本解析 Token 文件、重放「品牌 × 浅色／深色／系统深色」的级联、跟随 `var()` 链解析出实际颜色，再对实心填充承字、品牌文字、链接、图标与焦点环逐组测量（文字 4.5:1，非文字 3:1）；控件静止描边是显式例外，见上一节的边界说明。色相层增删或色阶改动后必须跑它；余量低于 1.1× 的色相层若没在自己的注释块里写明该对比度，脚本会直接失败。因为色相层是「一处改动、全站换色」的间接层，这条自动拦截是唯一能在构建期发现承字对比度被改坏的手段。
- **同一个组件内的多种 accent 角色必须分开命名**：通知卡片同时需要填充（卡片底色、徽标底色、进度环）和 ink（徽标图标、头部图标），因此拆为 `--aeterni-dialog-accent` 与 `--aeterni-dialog-accent-ink`；用一个变量兼两个角色，就会把 ink 拖到填充档的对比度。
- **容器背景只能是黑、白、灰或毛玻璃**：`--aeterni-bg-*`、`--aeterni-bg-surface`、`--aeterni-bg-elevated` 和 `--aeterni-surface-soft` 的 R、G、B 必须相等（深色主题统一允许 ≤ 3 的冷偏移）。容器带上 +2 以上的蓝/紫偏移时，在侧边栏、卡片、磨砂层这种大面积上会被读成「品牌色底」，而且 `backdrop-filter: saturate()` 会把偏移放大。彩色只允许出现在品牌/语意色元素、交互状态和通知卡片这类「内容表面」上。壁纸、背景内容层不在此列——它们是被模糊的对象，不是容器背景。
- **玻璃配方只有一套，声明在 Token 层**：所有磨砂表面都消费 `--aeterni-bg-glass` 与 `--aeterni-blur-glass`（遮罩用 `--aeterni-blur-scrim`），不要在自己组件里重写填充档位或模糊半径——宿主需要一个整体旋钮，而不是每个组件各自为政的配方。填充是独立档位（浅 68% / 深 72%），不是「抬高一层」的别名：抬高只要求落在页面上，磨砂要按最坏背景（纯黑／纯白）保证墨可读，把两者塞进一个值就是填充曾经偏厚、糊不进背景的原因。这两支墨（`--aeterni-text-on-glass-secondary` 93%、`--aeterni-text-on-glass-tertiary` 67%，都按主体墨的比例声明一次）是给磨砂态专用的：**每一个**磨砂内容表面（`Card` / `Surface` 的 `Glass` 变体、非模态 `Drawer` 面板、`Popover` 表面、`Tooltip` 内容、Alert/Toast 卡片）都要把这四个角色名（`--aeterni-text-secondary`、`--aeterni-text-muted`、`--aeterni-state-color-readonly` 接 `--aeterni-text-on-glass-secondary`，`--aeterni-text-tertiary` 接 `--aeterni-text-on-glass-tertiary`）一起接管过去，因为别名在主题块里就已经完成 `var()` 替换，只改基名会留下用 `--aeterni-text-muted` 写的说明文字仍在标准墨上。新增磨砂表面时按同样四条照抄，`Card.razor.css` 是这一段的注释范本（名单曾含 `--aeterni-state-color-muted`：它全库零读取，已随 REV-122 从各玻璃块删除，令牌层声明保留但不再纳入接管）。不透明表面（例如 `is-no-blur` 的通知卡片、模态 Dialog 面板、模态 Drawer 面板）与遮罩不接管——遮罩不承字，改成玻璃墨只会把压暗加深。模态面板落在遮罩上，玻璃填充会采样被压暗的页面：浅色主题下面板呈中灰，而面板内的白色字段比面板更亮，层级反转。因此模态面板使用不透明 `--aeterni-bg-surface`，深度感由遮罩与 `--aeterni-shadow-modal` 承担；非模态 Drawer 仍在页面之上，保留磨砂。模糊只能采样元素**背后**的内容，所以玻璃面板与它的背景内容必须是兄弟节点：把背景放进玻璃元素内部，只会被填充盖住、永远不会被模糊。玻璃表面在 `prefers-reduced-transparency: reduce` 下退到不透明（填充 `--aeterni-bg-solid`、模糊 `none`、两支玻璃专用墨退回标准墨），不能只摘 `backdrop-filter`。玻璃只保证中性墨：品牌墨（`--aeterni-text-link`、`--aeterni-color-brand-text`）的亮度是照着页面定的，透出来的背景一深／一浅就会掉到 4.5:1 以下（浅色最坏 2.98:1、深色 2.27:1），所以玻璃面板要么用在页面级内容之上，要么改用实心变体。通知卡片的徽标是玻璃内唯一承载语意墨的表面：它用 `color-mix(in srgb, var(--aeterni-dialog-accent) 16%, var(--aeterni-bg-solid))` 保持不透明底色，因此不在上述中性墨保证范围内；不要把它改回 `transparent`，否则最坏背景（玻璃采样纯黑/纯白）上语义 ink 会跌到 2.3–2.8:1。
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
- 交互态（hover / active / selected）一律使用 `--aeterni-state-*` 角色；容器背景只取上面的无彩色角色，不得为交互态另立一套带品牌色的容器背景别名。
- **实心填充的字色由填充明度决定**：深档填充（品牌）配 `--aeterni-text-inverse`，亮档填充（success/warning/danger/info）配 `--aeterni-color-on-semantic`。同一控件在 base/hover/active 三个状态必须保持同一字色，否则很容易掉到 4.5:1 以下；品牌填充向下取档，语意填充向白提亮 12%。
- **浅底深字**：带色 chip（Tag）必须用「浅色调底 + 深色文字」，文字由强调色与正文字色按约 1:1 混合得到；只用两成墨色会让 chip 文字掉到 3:1 以下。

### 5.5 尺寸和圆角

尺寸与圆角规则见 [`AGENTS.md`](../AGENTS.md) §4.2/§4.4。两条测量结论决定了当前档位：

- 「一个值喂多处几何」的控件，内边距是一次真实上限计算，不是随手取的值。以 `Segmented` 为例：轨道内边距同时喂轨道内边距、滑块内缩与滑块行程，三档共用同一值。选项的可见高度可以低于 `--aeterni-touch-target-min`，但垂直热区不能：Small 档把选项压到 20px（2×描边 + 2×3px 内缩 + 20 = 28px，回到 `--aeterni-control-height-sm`），再由选项自己的透明伪元素把热区补到 24px；这与直接给选项 `min-height: 24px` 的旧写法结果差 4px，后者会把整条轨道顶到 32px。改这类几何前先用真实渲染量出档位高度与热区，并区分「可见盒」与「命中盒」。
- 滑块圆角有两条互斥路线，`Segmented` 选「同形」：同心路线（滑块半径 = 轨道半径 − 描边 − 内缩）在默认档算成 `6 - 1 - 5 = 0`，圆角轨道里嵌方角滑块；同形路线（滑块半径 = 轨道半径）与同档 `Input` 同源，代价是角上留白比四边宽 1.66px（三档同值）。内缩不能由圆角反推：曾把内缩写成「轨道圆角 − 描边 − 滑块半径」，结果随档位漂移，且被 `Pill` 档重指成 `9999px` 时算出 `-9994px`，`padding` 落到 0、滑块 `inset` 被撑到两万像素，整条轨道塌掉。留白偏差必须沿角平分线单独量，并直接钉住滑块半径。

浮层半径按层级递增，不是随手取值：Tooltip 4px < Popover/下拉 8px < Dialog 16px；通知卡片沿 iOS 通知中心观感用 16px 档但属独立一层。圆角与尺寸档位的具体 Token 以 [`AGENTS.md`](../AGENTS.md) §4.4 为准。

组件样式表不允许未注释的裸像素尺寸：能映射 Token 的直接引用；缺档位先在 Token 层补语义 Token 再引用；结构性机制（无障碍裁剪的 `1px` + `50%`）、动画位移与排版微调可以保留字面量，但必须注释说明它是机制/微调而不是尺寸档位。选项到类名的映射集中在 `Components/ComponentClass.cs`（`ForSize`/`ForColor`/`For`），组件不要各自维护 switch。

### 5.6 样式覆盖边界

规则见 [`AGENTS.md`](../AGENTS.md) §4.4。两个失败模式值得记住：跨组件外观归渲染该元素的组件所有——把同一份日历规则复制到 `DatePicker.razor.css` 会在两者之间产生漂移（曾出现 101 行复制、其中 61 行是 `DateCalendar.razor.css` 的子集）；用户传入的 `Class` 只能作为扩展入口，不能改变基类属性合并规则。

### 5.7 结构稳定性

规则见 [`AGENTS.md`](../AGENTS.md) §4.2。以下四种失败都来自「两套真相」：

- 用 `@if` 给两种模式各写一份相同子树，后续只改一份就会分叉；差异应放进 `BuildClass()`/`BuildAttributes()`。
- 用 `class="@SomeBuilder()"` 手写根属性，会让 `Id`、`Class`、`Style`、`Visible`、`AdditionalAttributes` 静默失效。
- 非交互组件注册 DOM 监听器：处理属性可以返回 `default` 的 `EventCallback<T>`（渲染树不产生处理帧），不要把判断全丢进处理器内部。
- 图标尺寸出现第二个入口（`font-size`、`::deep svg { width/height }`）：统一走祖先元素的 `--aeterni-icon-render-size`，否则 `Icon` 的 `1em` 回落会与宿主设置打架。

禁用态用状态 Token 而不是整块 `opacity`；`opacity` 只用于组件局部的光学微调（加载中的标签、弱化字形）并在注释里说明。同一角色的魔数只写一次：已有 Token（如 `--aeterni-opacity-muted`、`--aeterni-radius-badge`）不要在旁边再写一份字面量。

### 5.8 滚动条

滚动条在视觉上“只有一个”，实现上却分散在十几份隔离样式里，因此它同时是一条 Token 约定和一条门禁约定。

- **三个 Token 对应一个角色。** `--aeterni-scrollbar-size`（6px）、`--aeterni-scrollbar-thumb`（`--aeterni-border-strong`）、`--aeterni-scrollbar-track`（**transparent**）。滑轨必须透明：容器一旦用 `scrollbar-gutter: stable` 预留槽位，实色滑轨就成了一条常驻的灰／白色竖条，与它所在的玻璃、卡片或弹层格格不入——这正是“滚动条太显眼”这类报障的来源。
- **两类声明必须成对。** 标准属性（`scrollbar-color` / `scrollbar-width`）被 Firefox 与 Chromium 121+ 读取；`::-webkit-scrollbar`（含 `-thumb` / `-track`，按滚动主轴只写 `width` 或 `height`）被 Safari 与更早的 Chromium 读取。只写一边，另一半浏览器就回落到默认外观。
- **`scrollbar-color` 继承，`scrollbar-width` 不继承。** 这条差异决定宿主与组件的分工：宿主把颜色写在根元素上一次，页内所有容器都跟随；宽度必须对滚动元素本身声明（`*` 选择器或逐容器），只在 `:root` 上写宽度只会改到根滚动条。组件侧不能假设宿主做了什么，所以每个可滚动容器都自己把两类声明写全。
- **隐藏滚动条也走同一套约定。** 确实需要隐藏的容器（`TimeOptionList` 的时／分／秒列）写 `scrollbar-width: none` + `::-webkit-scrollbar { display: none }`，两处成对——只写后者会让 Firefox 上仍画出滚动条。
- **门禁**：`node scripts/check-css.mjs`（`--only=scrollbars` 可单独运行）扫描库内所有 `.razor.css`，逐个检查带 `overflow: auto|scroll` 的规则块是否满足上述约定，并断言 Token 文件的滑轨仍透明、示例站仍保留宿主级声明。漏一处从此不再只是“某个组件看起来有点怪”；同一次遍历还检查注释提前闭合（`--only=comments`）。

## 6. 状态和交互依据

### 6.1 标准状态

状态清单与优先级规则见 [`AGENTS.md`](../AGENTS.md) §4.4；不适用的状态不必强行实现（静态展示组件不需要 `active`）。优先级之所以把 `disabled` 放在最前，是因为禁用态一旦被悬浮/选中覆盖，用户会得到「可按但按了无效」的错误反馈。

### 6.2 状态优先级

`disabled > loading > invalid / selected > focus-visible > hover > base`（规范见 [`AGENTS.md`](../AGENTS.md) §4.4）。具体组件可以调整优先级，但必须保证禁用状态不会被悬浮或选中覆盖。

### 6.3 Hover、Active 和 Focus

指针反馈可以改背景、边框、颜色和阴影；`active` 表达按下；`focus-visible` 必须有清晰焦点环且不能只靠颜色变化，也不能用 `outline: none` 删除焦点而不给替代方案。位移/缩放/阴影变化不得造成布局尺寸变化；`ThemeSwitch` 这类滑块按下时保持尺寸和位置稳定，只在选项切换时移动。

日期网格的月份箭头与日格是 roving tabindex 的真实焦点目标，`DateCalendar` 同样绘制 `--aeterni-focus-width` / `--aeterni-focus-color` 的品牌焦点环；选中日已铺品牌填充，环改用 `--aeterni-text-inverse`，与 `Segmented` 选中 pill 的处理一致。

### 6.4 动效依据

动效复用 duration/easing Token（规则见 [`AGENTS.md`](../AGENTS.md) §4.4），以下数值是实测与踩坑结论：

- 折叠面板用 `grid-template-rows: 0fr → 1fr` 是已登记例外：不引入 JS 测量高度时它是不改 DOM 结构的唯一方案，只能用在自有折叠容器上并兼容 `prefers-reduced-motion: reduce`。
- 状态切换用短档：`Segmented` 选中滑块 200ms（normal 档），不用 `slower + fast` 的 600ms 长轨。
- 主题与品牌切换只过渡颜色承载属性（`color`/`background-color`/`border-color`/`outline-color`/`box-shadow`/`fill`/`stroke`），300ms；把 `transform`/`opacity` 纳入切换会把整页卷进合成层，读起来像「页面在动」。
- 通知的自动关闭计时与环形进度必须同源暂停：指针悬停或焦点进入卡片时暂停（进度环 `animation-play-state: paused`），离开后从剩余时间继续；只停 CSS 动画而计时器仍走，会让卡片在读者眼前消失，只停计时器而动画继续则进度与关闭时刻对不上。
- 浮层退场约为进场的 60–75% 并用加速曲线：Popover 80/100ms、Dialog 与 Notice 150/200ms、Drawer 210/300ms。退场等待只认实际运行的动画，并按实测 `delay + duration` 兜底：短动画可能在 Blazor interop 抵达前结束，`Animation.finished` 在部分引擎上不再结算，没有兜底会把浮层永久留在 `is-closing`（10.42.0 修正共享 `waitForExit`）。
- 进场按体量分三级：小浮层 100ms `ease-out`；通知 200ms `ease-emphasized`；Dialog 200ms `ease-out`、Drawer 300ms `ease-emphasized`。Dialog 面板必须与遮罩同帧进入；Drawer 关闭过程中页面始终处于暗场。遮罩拒绝关闭时的抖动每次点击重新触发，抖完保持 `is-settled`（`animation: none`），不得落回进场关键帧从 `opacity: 0` 重播，否则会被读成页面刷新；reduced-motion 下省略抖动。
- 滑块与进度条的数值长度只用 `transform: scaleX()/scaleY()`，不用 `width`/`inline-size` 过渡；RTL 下填充起点随方向镜像。`ease-emphasized` 下可感知行程约为时长的 2/3（Segmented 200ms 实测约 130ms），调参以感知时长而非名义时长为锚点。

## 7. HTML 语义和可访问性依据

### 7.1 原生元素优先

规则见 [`AGENTS.md`](../AGENTS.md) §4.5。为什么不用 `<div @onclick>` 模拟按钮：读屏与键盘用户拿不到角色、Tab 焦点与 Enter/Space 语义，必须补齐 `role="button"`、Tab 焦点、键盘与禁用语义；`Card` 的 `Interactive` 是明确的少数例外。

### 7.2 Disabled 和 Loading

原生支持 `disabled` 的元素用原生属性；不能禁用的元素至少输出 `aria-disabled="true"`。加载态要阻止重复提交、保留稳定尺寸、给读屏可理解的状态，且不能只靠旋转图形表达。

只读（ReadOnly）不是禁用：原生输入用只读属性；`role="combobox"` / `role="textbox"` 的触发器可以用 `aria-readonly`，但 `role="button"` 的弹层触发器（日期/时间族）不支持这个属性，改用 `is-readonly` 状态类 + `aria-disabled="true"` 表达“可聚焦、可复制但不可操作”。不要把 `aria-readonly` 写到 button 角色上——它属于 ARIA 允许属性表之外，读屏不会消费，审计会报 `aria-allowed-attr`。

### 7.3 ARIA 与表单关联

ARIA 只补充语义，不能替代正确元素；每个 ARIA 属性都必须对应真实交互状态。最典型的失败：Blazor 把 `bool` 属性值渲染成最小化属性（`aria-selected` 而不是 `aria-selected="true"`，为 `false` 时直接省略），两者对读屏都是无效值——统一写 `? "true" : "false"`，原生布尔属性不受影响。

复合控件角色层次必须成立：`listbox` 直接子元素是 `role="option"`，`radiogroup` 内至多一个 `aria-checked="true"`，不可聚焦选项不进入 Tab 序列也不抢容器焦点；单选组、评分等使用 roving tabindex，整组只有一次 Tab 停留点。

表单字段关联有三种合法形态：原生可聚焦控件直接采用 `FormField` 的输入 ID 让 `label for` 生效；容器型控件（`ComboBox`、`Rating`、`RadioGroup`、`Slider`）既使用输入 ID 又用 `aria-labelledby` 关联标签、`aria-describedby` 关联描述与错误文本。用户可见默认文案统一来自 `AeterniUIOptions.Text`，组件参数优先于文案表。

### 7.4 键盘行为

至少检查 Tab 进出、Enter/Space 原生预期、Escape 关闭浮层、方向键用于 tabs/列表/分段、焦点在状态变化后保持合理位置（规则见 [`AGENTS.md`](../AGENTS.md) §4.5）。

时间选择器是这套规则的集中案例：时/分/秒三列滚轮不把一天内所有步长展开成扁平 DOM（1 秒步长最多渲染 24+60+60 个列选项，而不是 86,400 个时间点）；单位列名固定顶部，中心选择带固定不随内容滚动，刻度吸附、边缘渐隐、近大远小；打开直接定位当前值，用户操作后的吸附才播放平滑动画；滚轮组只保留一个 Tab 停留点，Left/Right 切列，ArrowUp/Down 与 Home/End 改值，Enter/Space 确认；滚动与点击只更新草稿，取消不得改写绑定值。当前交互细节见 [`delivered-features.zh-CN.md`](delivered-features.zh-CN.md) §40。

## 8. JS isolation 规范

### 8.1 使用条件

只有 DOM 测量/定位、焦点控制、外部点击关闭、滚动锁、Observer 或 CSS/Blazor 无法可靠完成的行为才引入 JS（规则见 [`AGENTS.md`](../AGENTS.md) §4.6）。普通 hover/focus 样式、过渡与 transform 动画、只为切 class 的状态、以及 C# 已能维护的业务状态，都不该用 JS。

### 8.2 声明方式

一个组件最多一个主 `JsModuleAttribute`；`Interactive` 只决定是否执行 `init`，不表示可以声明多个 module。路径相对组件库静态资源根目录，`Name` 稳定且具有组件语义；`Interactive = false` 时 module 可加载但不执行 `init`，组件销毁时仍执行实例级 `dispose`。不要在组件初始化阶段访问浏览器，浏览器调用放在首次渲染之后。

### 8.3 JS module 生命周期

组件 module 必须提供实例级 `dispose`，`Interactive = true` 时还必须提供 `init`，由基类自动调用（规则见 [`AGENTS.md`](../AGENTS.md) §4.6）。三个易漏点：所有监听器/Observer/定时器都要在 `dispose` 中释放；实例 key 用 `InstanceId`；JS 回调失败不能让页面永久崩溃，必须有降级路径。JS 不保存业务状态，状态源仍是 C# 或 DOM 原生状态。

### 8.4 共享浮层能力

锚定浮层（Tooltip、Popover、后续的 Drawer）共用 `wwwroot/js/aeterni_floating.js`，不得各自重写视口贴合、焦点陷阱与滚动锁：

- `fitsSide` / `oppositeSide` / `clampCenteredShift` / `clampAlignedShift` —— 翻转与交叉轴贴边的全部数值。居中对齐（Tooltip）与边对齐（Popover）用不同的 clamp，不要拿一个公式套两种几何。
- `createFocusTrap(container, { onEscape })` —— Tab 循环、Escape 上报、释放时把焦点还给打开前的元素；返回幂等的 `release()`。
- `lockScroll()` —— 引用计数的页面滚动锁，返回幂等的释放函数，重叠浮层不会互相解锁。页面确实可滚动且浏览器支持时，优先在根元素保留 `scrollbar-gutter: stable`，让视口宽度与 fixed 页头、流内正文的对齐关系在开、关浮层前后保持不变（fixed 元素按含滚动条的视口取百分比，流内元素按扣除滚动条的内容宽度取百分比；只补 `body` 的 `padding-right` 无法同时修好这两条基准）；不支持时退回 `body` 的 `padding-right` 补偿，解锁时把 `overflow`、`padding-right` 与 `scrollbar-gutter` 一并还原，不留下第二套布局基准。 预留槽位会把布局视口缩小一个滚动条宽，而 `position: fixed` 层正是按这个缩小后的视口排的：因此锁定时还会把实测宽度写成根元素上的 `--aeterni-scroll-lock-gutter`，**每个铺满视口的覆盖层都必须用 `inset-inline-end: calc(-1 * var(--aeterni-scroll-lock-gutter, 0px))` 把内联末端伸出去**，否则经典滚动条引擎（Linux / Windows、或关闭覆盖式滚动条的 macOS）上右侧会留一条未压暗的亮条，居中的面板也会偏半个滚动条。 但固定层只负责几何：Chrome 把 `position: fixed` 的绘制裁在布局视口内，外扩出来的那 10px 不会被画到。所以锁定时还会把「遮罩色叠在页面底色上」的合成色写到根元素的 `background-color`（画布铺满整个窗口，这是唯一能画进槽位的一层），解锁时还原宿主原来的行内值；页面底色不透明时才做这一步，透明底会把画布变暗透到内容后面变成二次压暗，此时宁可不染。当前消费者是 `DialogProvider` 的遮罩、`Drawer` 根层、`Popover` 遮罩与示例站的搜索模态；覆盖式滚动条引擎上该变量为 `0px`，声明等于不存在，不需要分支。
- 组件模块通过相对路径导入（`../../js/aeterni_floating.js`）。共享文件放 `wwwroot/js/`，因为它不属于任何单个组件；而组件模块必须是组件目录下的 `*.razor.js`（只有这个命名会被 Razor SDK 当作静态资源发布）。
- **浮层不得用 `transform`/`translate` 做偏移**：变换会让浮层成为自身 `position: fixed` 遮罩的包含块。需要像素级偏移时用 `margin`（Popover 用 `margin-left` 承载 `--aeterni-popover-shift-x`）。
- **宿主不要把 `PopupHost` 关进层叠上下文**：`isolation: isolate`、`filter`、`backdrop-filter`、`transform`、`will-change` 都会建立层叠上下文；浮层的 `--aeterni-z-popover`（1060）只在同一个层叠上下文里与同级元素比较，一旦被关进卡片这类隔离层，就会被后绘制的同级元素（例如 `position: sticky` 的页内目录）压住。需要隔离卡片内部层级时，把浮层的 `PopupHost` 提到页面级，或让目录不再与浮层处在同一个父层叠上下文里。
- **玻璃面不要放在带 `filter` 的祖先里**：`filter` 会建立新的 backdrop root，内部 `backdrop-filter` 只能采样这个 root 里的内容，模糊会整块失效（填充色还在，于是表现为“有底色、没模糊、字直接透出来”）。页头这类玻璃外壳需要投影时用 `box-shadow`，不要用 `filter: drop-shadow()`。
- 关闭始终由使用方的 `OpenChanged` 驱动：JS 只负责“请求关闭”，不直接改 DOM 状态，也不假设参数未被绑定。
- 堆栈类浮层（Dialog）可以保留自己的 Tab/Escape 处理，因为它要判断“只有最顶层响应”；这类差异必须在代码注释与当前功能文档里写明。

## 9. 主题机制依据

### 9.1 状态定义

`Mode`（`System`/`Light`/`Dark`）是用户选择的来源，`CurrentTheme`（`ThemeKind`：`Light`/`Dark`）是实际生效主题，`Brand`（`ThemeBrand`：`Purple`/`Green`/`Orange`）是品牌色层。System 模式下 `CurrentTheme` 由系统决定，Light/Dark 下由用户选择决定。

`Brand` 与明暗正交：明暗决定取色阶的哪几档，品牌决定用哪条色阶。品牌变化不影响 `Mode`/`CurrentTheme`，因此必须走独立的 `BrandChanged` 事件，不能复用 `ThemeChanged`——后者会触发系统偏好重新解析，而品牌切换与系统偏好无关。

### 9.2 组件约束

组件不读取操作系统主题、不修改 `data-theme`/`data-aeterni-brand`、不直接调用 `ThemeProvider` 的内部 JS module；只消费语义 Token，或订阅 `ThemeService.ThemeChanged`/`BrandChanged` 刷新自身展示（规则见 [`AGENTS.md`](../AGENTS.md) §4.4）。

### 9.3 ThemeProvider 与首帧

`ThemeProvider` 是无可见内容的宿主组件，订阅明暗与品牌两个维度、初始化并应用页面属性、监听系统主题变化、释放 JS 监听；它不包裹 Layout，也不承担业务布局。它不渲染 DOM（规则见 [`AGENTS.md`](../AGENTS.md) §4.4）。

首帧闪烁的根因是脚本尚未执行、CSS 已可用：宿主应在样式表之前放预渲染脚本（读 `aeterni.theme.mode`、`aeterni.theme.brand` 与 `prefers-color-scheme`，写 `data-theme` 与 `data-aeterni-brand`）。CSS 侧只有 `prefers-color-scheme: dark` 兜底且仅在无显式属性时生效；品牌没有等价 CSS 兜底（`:root` 默认色板就是回退），脚本的品牌回退字面量必须与 `AeterniUIOptions.DefaultBrand` 一致；存储值非法时属性匹配不到色相层而回落 `:root`，因此非法值不需要在脚本里额外过滤。

### 9.4 DialogProvider 与表单弹出机制

`DialogProvider` 与 `ThemeProvider` 一样是自闭合宿主，业务只注入 `IDialogService`（用法见 [`delivered-features.zh-CN.md`](delivered-features.zh-CN.md) §26–§27）。机制上只有三点需要单独记住：

- 三类弹出内容职责不同：`Show`/`ConfirmAsync` 是模态内容（遮罩、滚动锁、焦点管理，只有最顶层响应 Escape）；`AlertAsync` 是非模态语义提示；`ShowToast` 是非阻塞通知。模态面板使用不透明 `--aeterni-bg-surface`（原因见 §5.3 玻璃段），遮罩仍保留 `--aeterni-overlay` 与 `--aeterni-blur-scrim`。Alert/Toast 的自动关闭进度由 CSS 线性动画驱动，`Duration=Zero` 改为仅手动关闭、`null` 使用全局默认；悬停/焦点暂停由卡片上报服务、计时器与进度环同步暂停（§6.4）。
- 表单弹窗（10.42.0）把「标题 + 表单内容 + 主/次动作」收进 Provider，`FormDialog` 只是声明式宿主；焦点与 Enter 由 Provider 的同一个 JS 模块统一处理：打开时优先聚焦表单体首个字段（找不到才回退首个可聚焦元素），Enter 只在单行字段转成提交（`textarea`、`select`、按钮、链接、`contenteditable`、输入法组合与修饰键组合保持各自语义），判定依据是事件目标而不是按键本身。
- 忙碌状态机归 Provider（`aria-busy`、主按钮 `Loading`、取消/关闭禁用、Escape/遮罩/重复提交忽略），宿主不再维护第二份状态；`OnSubmit` 抛异常时先复位忙碌态再继续抛出。表单 footer 由 Provider 生成，因为忙碌态必须同时到达主、次两个动作。

## 10. 示例项目依据

示例项目是组件库的可运行契约，不是营销页面（规范见 [`AGENTS.md`](../AGENTS.md) §4.7）。两个结构决定值得保留原因：

- 一个组件一个页面：页面只包含该组件的演示标记与自己的 `@code` 状态，跨页共享的只有少量基础件和展示样式表。用一个大页面加 `switch` 会让每个组件无法单独分享链接，也无法独立演进。
- 示例样式放在示例项目的全局样式表里：作用域 CSS 无法跨页生效；类名仍用示例自己的前缀，不得覆盖 `.aeterni-*` 组件规则，否则示例会反向成为组件实现依赖。

示例必须展示真实参数和真实状态，不能用静态文本模拟尚未实现的能力；主题切换与浏览器能力不可用时的降级行为也要覆盖。

API 表的公共参数不能机械地贴到每一页：`ShowcaseApiReference.Sections` 按组件分组，`scripts/lib/showcase-support.mjs` 从源码结构推导每个基类参数是否真的落到 DOM（是否消费 `BuildAttributes()`、是否转发 `Class`/`Style`/`Element`、是否 `SupportsDisabled`），推导不出的个别例外在 `scripts/api-groups.json` 显式登记。这样 ThemeProvider、FormDialog 这类无 DOM 的组件不会被列出无效参数，多组件合页（Dialog/Confirm/Alert/Toast、DatePicker/DateRangePicker）也不会混成一张表。

## 11. 新增组件工作流

1. 明确组件解决的问题与和现有组件的边界；先确认能否用现有组件组合完成。
2. 设计公开参数、枚举与事件回调；确认根 HTML 元素和可访问性语义。
3. 列出适用状态（base、hover、focus、active、disabled、loading 等），把视觉决策映射到已有 Token。
4. 创建组件目录与 Razor 文件，继承 `AeterniComponent`，实现 `BuildClass()`，必要时 `BuildStyle()`。
5. 只在确有必要时添加 JS module，并导出 `dispose(instanceId)`。
6. 在示例项目中展示默认、变体、尺寸和状态，跑门禁，再同步文档。

完整的执行顺序与门禁见 [`AGENTS.md`](../AGENTS.md) §4.1/§4.8；提交前逐项核对 §12。

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
- [ ] 悬浮规则排除已选中态（`:not(.is-selected)`／`:not(:checked)`／`:not(.is-current)`）。
- [ ] 文字只用 `primary` / `secondary` / `placeholder` / `disabled`；`tertiary` 只用于图标与装饰。
- [ ] 新增磨砂表面时，已按上文「玻璃配方只有一套」把四个角色名（`--aeterni-text-secondary`、`--aeterni-text-muted`、`--aeterni-state-color-readonly`、`--aeterni-text-tertiary`）一并接管到两支 on-glass 墨，而不是只改其中几个。
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
- [ ] 组件没有自己修改主题根节点。
- [ ] 主题切换后组件状态仍然正确。

### 示例

- [ ] 示例使用真实组件，而不是静态仿制样式。
- [ ] 示例覆盖主要参数和适用状态。
- [ ] 示例没有依赖组件内部 class 实现业务逻辑。
- [ ] 示例页面在 Light、Dark、System 三种模式下都可读。

## 13. 特殊组件实现契约（现行）

以下契约是当前实现的一部分，不是计划；现行参数与行为以 [`delivered-features.zh-CN.md`](delivered-features.zh-CN.md) §42、§46、§54 为准，这里只保留决定「为什么这样实现」的部分。

- `Toolbar`：无背景、无边框的动作布局，只接纳 Button、IconButton 与 MenuButton；整条工具栏共享一个 roving tabindex，所属轴方向键循环、Home/End 首尾、横向 RTL 反转，非所属轴与 Enter/Space 留给按钮/菜单。菜单弹层与嵌套工具栏不属于外层候选；Disabled 使用 inert 阻止后代交互；业务状态留在宿主，JS 仅负责焦点与监听器释放。
- `NavMenu`：`Density`/`ChildIndent` 只重设根元素上的组件级 CSS 变量，`Comfortable` 的 38px 行高与 3px 留白来自 `--aeterni-control-height-md`/`--aeterni-spacing-1` 的派生计算，不是新增尺寸 Token；`Default` 档逐项保持原观感。图标与描述尺寸属于节点内容或 `RowTemplate`，不做组件档位，否则同一份内容在自定义模板里还要再对一次参数。行级 hover/选中/按下画在 `.aeterni-nav-menu__row` 上，`Trailing` 与标签共用同一块底与同一支墨；行容器的 `is-selected`/`is-disabled` 是状态事实源，`:has(> .aeterni-nav-menu__button:active)` 只同步按下态。
- `FlashCard`：整个 3D 舞台收在组件内部——根元素只承载透视，`__tilt` 负责指针旋转与悬停缩放，`__inner` 负责翻面，两面绝对定位于同一个 `aspect-ratio` 盒子。指针跟随只写旋转变量，不在 JS 里改 `transform`；透视放在根元素而不是旋转元素上，否则焦点轮廓与透视原点会一起旋转。卡片只在提供 `Back` 时才是控件；未翻出的一面用 `aria-hidden` 与 `inert` 退出无障碍树。`FlipTrigger="Hover"` 是样式表驱动的指针预览，组件不输出控件语义也不隐藏任何一面——浏览器拥有的状态不能变成 ARIA 谎报。容器背景保持无彩色；`Sheen` 是唯一允许的彩色表面，且必须铺满媒体区、只覆盖图片、一个效果一层（彩虹用 `color` 保留明度，白光用 `screen` 提亮），不能退化成「指针下面一块高光」。
