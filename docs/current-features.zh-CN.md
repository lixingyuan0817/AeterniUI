# AeterniUI 当前已完成功能

文档版本：`10.17.0`

文档状态：当前实现清单；本文档是当前已实现公共 API 和行为的唯一事实源。

本文档用于记录当前组件库已经落地的能力，作为示例项目、后续组件开发和 API 设计的基线。未列出的功能不应被视为已经稳定提供。

## 10.16.0 Stepper 功能发布

新增 `Stepper` / `StepperItem` 线性流程指示组件；支持受控当前步骤、宿主显式完成/禁用状态、横纵向布局、位置 ARIA 语义和键盘可激活步骤。示例与渲染契约已纳入本版本。

## 10.15.0 Breadcrumb 功能发布

新增 `Breadcrumb` / `BreadcrumbItem` 层级导航组件；支持原生链接、最后可见项当前页语义、禁用/隐藏过滤、可替换分隔符和组件级 ARIA 名称。示例与渲染契约已纳入本版本。

## 10.14.3 全组件质量修复（已纳入本版本基线）

完成 P1–P3 质量批次：基类 `Visible=false` 通过内联隐藏样式确保不被组件布局规则覆盖；ComboBox、Rating 的键盘导航阻止默认页面滚动并同步焦点；Drawer/Popover 尊重 `CloseOnEscape` 并提供退出过渡；ComboBox、Radio/RadioGroup 继承 FormField 的 Disabled/Required/Invalid 状态；DateCalendar 保护日期最小/最大边界；Tooltip 在参数变化后重新绑定；Tabs、Accordion 修正隐藏/禁用状态；TimePicker 在有限时间范围内生成候选映射，避免无谓遍历全天。

## 10.14.2 List 键盘修复（未发布）

修复 AllowClear 下导航误清空、声明式 keyed 重排后导航次序错误、隐藏项仍参与导航以及方向键/空格触发页面滚动的问题。选择状态仍由 C# 管理，新增 List 主 JS module 读取可见选项的 DOM 顺序并拦截已处理按键；其余组件仅审核，不包含修复。

## 10.14.1 发布范围

统一 List 普通行与 CardMode 的选中背景、活动项反馈和禁用文字色，示例页支持保留选择切换模式对比；选择 API 与键盘逻辑不变。核心包与 Font Awesome 图标包统一版本为 10.14.1，使用 `v10.14.1` 触发现有 NuGet 发布工作流。

## 10.14.0 发布范围

本次发布将 List 改为 `List<TItem>`，新增 Items、ItemTemplate 及 CardMode / CardTemplate；声明式使用需显式指定 TItem，迁移说明见 §16。核心包与 Font Awesome 图标包统一版本为 10.14.0，使用 `v10.14.0` 触发现有 NuGet Trusted Publishing 工作流；完整发布记录见 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md)。

## 10.13.0 发布范围

本次发布包含已交付的 ToggleGroup、SplitButton，以及日期/时间字段与 Segmented 的尺寸 Token 一致性收口；同时纳入 Toolbar 间距、MenuButton 专属菜单留白、菜单 ID 关联与非模态关闭焦点回归优化。公共 API、受控绑定和无障碍边界以各组件章节为准；List 功能不变。完整发布记录见 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md)。

## 文档口径

组件章节统一按以下顺序描述：支持能力、公共参数/事件、行为与无障碍、实现边界、基础用法（仅在有代表性时提供）。未适用的小节不强行添加。

- 本文档以当前源码中的公开组件、`[Parameter]`、`EventCallback`、公开服务和配置类型为准；实现细节不等同于公共 API。
- 所有组件都继承 `AeterniComponent`。`Id`、`Disabled`、`Visible`、`Class`、`Style`、`Element`、`AdditionalAttributes` 和 `ElementChanged` 是基类公共参数；下文只在某个组件实际处理该参数时重复说明。
- 基类参数始终可传入，但 `Disabled` 只有实现了相应语义的组件才会渲染禁用状态，不能据此推断任意容器都支持原生禁用。
- `ListItem.Selected`、通知卡片等内部状态不是公开参数；规划中的能力只记录在 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md)，不会写入已完成功能。
- 组合组件的公开边界：`RadioGroup`、`ThemeSwitch` 可独立使用；`ListItem` 只能作为 `List` 子项，`Tab` 只能作为 `Tabs` 子项；`PopupHost` 与 `Popover` 用于自定义浮层组合，通常由 `MenuButton`、`ComboBox`、`Tooltip` 等宿主间接使用；`DialogProvider` 与 `ThemeProvider` 分别只应在应用根部注册一次。`DateCalendar` 是 `DatePicker` / `DateRangePicker` 的内部渲染部件，虽保留 Razor 类型以支持内部组合，但已标记为非稳定公共 API，不支持直接使用。

### 默认温和紧凑尺寸（10.12.1）

- 全库共享控件高度 Small / Default / Large 为 28 / 36 / 44px（原 32 / 40 / 48px）；对应内联 padding 为 8 / 12 / 16px（原 12 / 16 / 20px）。Button、Input、ComboBox、日期时间字段及 IconButton 由现有高度别名派生，不新增 Density 参数。
- 保留原始 spacing scale、正文大小/行高、颜色、圆角与焦点环。现有 padding-lg / xl / 2xl 别名改停靠 spacing-3 / 5 / 6（12 / 20 / 24px）；Surface/Card 的 None / Small / Medium / Large / ExtraLarge 留白为 0 / 8 / 12 / 20 / 24px。
- 紧凑导航行 Token 从 36 收至 32px，Menu、Tabs、时间滚轮共用；List 保留内容自适应行高。弹层、Drawer、Dialog、通知同步收紧留白，日期单元格仍至少 28px。
- 可点击图标与选择控件热区至少 24px；Small Segmented 以 1px 内边距保留 24px 选项高度，Rating Small 不随字形缩至 20px。多行列表和表面内容继续使用自适应高度；不通过缩小正文换取密度。
- 时间滚轮的窗口、占位与行高同源，JS 使用真实 offsetHeight / offsetTop 和 ResizeObserver 测量，不假设旧行高；RTL、主题/品牌与 reduced-motion 原有行为不变。

## 1. 宿主支持

- 支持 Blazor Server 使用组件库。
- 支持 Blazor WebAssembly 使用组件库。
- 示例项目 `AeterniUI.Sample` 为 Blazor WebAssembly 项目。
- `scripts/sample-publish.sh` 将示例发布到仓库根目录 `dist/`，供静态部署及 GitHub Pages 使用。
- 示例项目包含文档首页（路由 `/`，品牌介绍与基础使用代码窗口）和组件页（路由 `/components`，左侧分类导航加真实组件交互画廊，展示按钮、分组、表面、输入、选择、通知与主题切换）。固定头部与「首页 / 组件」菜单由 `MainLayout` 承载。
- 组件 API 不依赖具体宿主，浏览器能力通过 JS isolation 提供。

## 2. 设计基础

- 使用统一的 `AeterniComponent` 作为组件基类。
- 基类提供自动生成的实例 ID 和可覆盖的 `Id`。
- 支持 `Class`、`Style`、`AdditionalAttributes`、`Visible`、`Disabled`、`Element`（`ElementReference`）和 `ElementChanged`。`Visible=false` 输出 hidden 及内联 `display: none`，优先于组件布局样式；恢复 Visible 后移除库追加的隐藏样式。
- 提供 `ClassBuilder` 和 `StyleBuilder`，组件可以在代码后置中组合 class 和 style。
- 支持 `ElementChanged` 回调。
- 支持组件级 `.razor.css` CSS isolation。
- JS 分两层：组件模块是组件目录下的 `*.razor.js`（由 `[JsModule]` 声明），共享的浏览器能力放在 `wwwroot/js/aeterni_floating.js`（浮层定位/翻转、焦点陷阱、滚动锁），组件模块通过相对路径导入它。
- JS module 可选，不添加 JS 文件的组件不会执行 JS 初始化。
- 一个组件最多维护一个主 JS module，JS module 支持初始化、调用和释放生命周期。
- 组件库使用统一的语义化 Token，组件内部不重新建立独立颜色体系。

## 3. Token 和主题

- 提供品牌色、语意色、背景色、文字色、边框色、阴影、圆角、间距、字号和动效 Token。
- 提供 Light 和 Dark 两套主题 Token。
- 品牌色与 `Info`、`Success`、`Warning`、`Danger`、`Neutral` 语意色均提供 `default`、`hover`、`active`、`disabled`、`soft` 状态别名；组件无需直接选择色阶。
- 提供统一控件状态 Token：`--aeterni-state-background-*`、`--aeterni-state-color-*` 和 `--aeterni-state-border-*`。其中 `selected` 是淡染的选中底面，`checked` 是实心勾选块（取品牌填充档，其上的墨取 `--aeterni-text-inverse`），并配套 `--aeterni-state-background-checked-hover` / `-active` 两档：已勾选的控件在指针下整块沿品牌色阶换档，而不是被淡染洗浅。
- 提供常用状态别名：`pressed`（等同 `active`）、`invalid`、`readonly`、`placeholder`、`muted` 和 `inverse`，用于表单反馈、只读内容和反色内容的一致表达。
- 提供通用控件组合别名：`--aeterni-control-background-*`、`--aeterni-control-border-*`、`--aeterni-control-foreground-*` 和 `--aeterni-control-focus-ring`，方便组件直接组合控件状态。
- 色板按 OKLCH 重建：每个色族一条明度阶梯、固定色相（仅浅档做少量 Abney 补偿漂移）、chroma 在中档收敛成峰值，因此不再出现「阶梯忽大忽小」「同一色族色相漂移 8°」这类问题。
- 品牌色是紫罗兰色系（浅色主题 `--aeterni-brand-500` = `#795AD9`，深色主题取 `--aeterni-brand-400` = `#9985ED`），峰值 chroma 从 0.237 降到 0.186、色相漂移从 7.8° 收到 1.7°，去掉了原来的荧光感。
- 品牌色阶是一个独立色相层，与明暗层正交：默认紫罗兰色板声明在 `:root` 上，显式 `[data-aeterni-brand="purple"]` 与之逐字节等价；宿主可在库样式表之后声明自己的 `[data-aeterni-brand="…"]` 块重写 `--aeterni-brand-50..900`，色阶一变，品牌填充、强调文字、链接、焦点环以及悬浮/按下/选中状态同时换色，语义别名与组件都不需要改动。该属性必须与 `data-theme` 同元素（`<html>`），因为语义别名在声明元素上解析色阶。
- 除默认紫罗兰外，色相层已交付第二套绿色板 `[data-aeterni-brand="green"]`：色阶同样按 OKLCH 从 50 到 900 完整重建，500 档锚定外部参考绿 `#1F883D`（L 0.552、C 0.145）——比紫罗兰的 L 0.565 低，因为绿色提高 chroma 会抬高明度、同一明度下承白字只有 4.3:1。该锚点承白字 4.52:1，仍满足 4.5:1 正文要求，但余量只有 1.004×，是品牌色组里最紧的一条门禁；承浅色主题正文与链接的 600/700 档因此压得更低（白字 6.17:1 / 8.09:1）。两套板在浅色、深色与系统深色下都通过全部对比度门禁，语义停靠档（浅色 500/600/700、深色 400/300/200）自动跟随切换。
- 第三套是焦橙 `[data-aeterni-brand="orange"]`，配方与前两套逐档一致（同一组相对 ΔL、同一 chroma 剖面、同一浅端色相漂移），500 档锚定 `#C2410C`（L 0.553、C 0.174、色相 38.4°）。锚点必须是焦橙而不是鲜橙：`#E8590C` / `#EA580C` 这类亮橙承白字只有 3.58:1 / 3.56:1，低于 4.5:1 正文要求，要做成实心填充就只能改成 `warning` 那种近黑前景，等于让第三套板脱离「白字填充」这条家族约定。焦橙承白字 5.18:1（余量 1.151×），三套板的同一个停靠档因此可以直接互换。代价是与语意 `danger` 红的距离只有 OKLab ΔE 12.1——橙红在近明度下本来就是邻居，这是全库最紧的一对品牌／语意间距，但在正常色觉下仍清晰可分；`warning` 相距 22.4，冷色语意全部超过 34，压力只来自暖端。
- 品牌色层可由 `ThemeService` 在运行时切换：`Brand` 的类型是 `ThemeBrand`（`Purple` / `Green` / `Orange`），`SetBrand` 校验枚举并落为 `<html>` 上的 `data-aeterni-brand`，`SetPurple()` / `SetGreen()` / `SetOrange()` 是对应的便捷方法。品牌与明暗是两个正交维度，切换品牌不会改动 `Mode` 或 `CurrentTheme`，因此品牌变化走独立的 `BrandChanged` 事件，不会重复解析系统偏好。首次访问的默认品牌由 `AeterniUIOptions.DefaultBrand` 决定（默认 `Purple`）。
- 语意色沿用 Apple 系统色：浅色 500 档为 `#34C759` / `#FF9500` / `#FF3B30` / `#007AFF` / `#8E8E93`，深色默认值为 `#30D158` / `#FF9F0A` / `#FF453A` / `#0A84FF` / `#8E8E93`。色阶以 500 档为锚点按 OKLCH 重建：浅端统一到 L 0.976、深端统一到 L 0.30，中间的 chroma 凹形收敛，因此保住了 Apple 的观感，同时消除了原有的 `Info` 400→500 明度断层（0.111）与色相漂移（254°→265°）。
- 中性色采用「无色容器 + 单墨色文字」的 Apple label 模型：
  - **容器表面一律无色**（浅色 `#F7F7F7 / #F0F0F0 / #E8E8E8`，R = G = B；深色统一 +3 冷偏移 `#101013 / #17171A / #202023 / #1A1A1D`）。容器带 +2 以上的彩偏移在大面积上会被读成品牌色底，磨砂层的 `saturate()` 还会放大它。
  - **文字色阶是一个墨色加不透明度**，而不是一组手调 hex：正文 `rgba(0,0,0,.78)`（11.7:1）、次要 `.62`（6.2:1）、占位 `.56`（4.9:1）、图标级 `.52`（4.3:1，**仅图标与装饰**）、禁用 `.36`（2.5:1，WCAG 豁免）；深色对应白墨 `.86 / .56 / .48 / .40 / .28`。透明的优点是文字会随所在表面（着色 chip、hover 底、毛玻璃）自动调和，而且二级与三级不会“既不同色又只差一点”。
  - `--aeterni-text-muted` 指向 `secondary`（弱化文字的可用级别）；`--aeterni-text-tertiary` 从“第三种文字”改为“图标/装饰级别”，原先用它做正文的 Menu 分组标题、Menu 描述、ThemeSwitch 未选中项已改用 `secondary`。
- 实心语意表面的前景 `--aeterni-color-on-semantic`（浅色 `#141414`、深色取反色墨）改为无色：原先的蓝黑 primitive `#0B0F19` 给绿/黄按钮上的文字带了蓝调。
- 新增 `--aeterni-separator`（不透明，浅色 `#C6C6C6` / 深色 `#3A3A3A`）：卡片头/底与对话框头/底的分界线用它，不再用 7% alpha 的 `--aeterni-border-subtle`（1.17:1，看起来像污渍）。
- 每个色族额外提供「强调文字/描边形态」的 `--aeterni-color-{brand,success,warning,danger,info,neutral}-text`：浅色主题取深档、深色主题取亮档，这解决了「亮色填充档当文字用只有 2.2:1」的老问题（描边/文字语义色按钮现在 5.0~9.5:1）。
- 形态选择写成硬规则：填充档只用于实心表面（实心按钮、开关轨道、选中指示、通知卡片的底色/徽标底/进度环），凡是 ink（文字、图标）或需要与浅色轨道/页面区分的实心图形（进度条填充、评分星形）均取文字形态。
- `--aeterni-state-color-selected` 与 `--aeterni-state-color-checked` 是前景别名，已指向 `--aeterni-color-brand-text`：选中项文字画在自己的选中底上，用填充档只有 4.0~4.4:1。
- 浅色 `--aeterni-text-tertiary` 是图标/装饰级别（`rgba(0,0,0,.52)`，4.27:1）：该级别既要看清字形（≥ 3:1）又不与正文争抢，ComboBox 箭头、关闭字形、Rating 空星都用它。
- 实心控件的字色按填充明度分两层：品牌填充走深档配 `--aeterni-text-inverse`，success/warning/danger/info 填充走亮档配 `--aeterni-color-on-semantic`；两类填充的 hover 都保持同一字色（品牌向下取档、语意向白提亮 12%），因此不再需要 `-strong` 这种按状态切换字色的 Token。`Button` 的 `--neutral` 填充则沿「向墨色收敛」方向取 hover/pressed（86% / 76%），使浅色与深色的反馈方向一致。
- `Icon` 的六个命名色（`Primary`/`Neutral`/`Success`/`Warning`/`Danger`/`Info`）消费 `--aeterni-color-*-text`；`Color.Default` 仍为 `currentColor`。Alert/Toast 卡片同时使用两个 accent 角色：`--aeterni-dialog-accent`（卡片底色、徽标底色、进度环）与 `--aeterni-dialog-accent-ink`（徽标图标、弹窗头部图标）。
- `FormField` 的必填星号与错误文案、`Menu` 选中项文字同样使用强调文字形态。
- `Tag` 的标签文字取词族的「强调文字形态」（`--aeterni-tag-ink`）再向正文墨靠 15%，而不是把亮色填充档与近黑对半混：后者会把绿/黄族混成橄榄色、褐色，看起来脏；当前浅色 4.95~7.15:1、深色 7.35~8.78:1，且色相保持饱和。
- 原始色阶（例如 `--aeterni-brand-500`、`--aeterni-info-600`）继续保留，用于自定义主题或特殊视觉需求。
- 提供浮层与紧凑表面度量 Token：`--aeterni-overlay-*`（对话框、下拉列表和浮层的宽高）、`--aeterni-row-height-compact`、`--aeterni-control-size-*`、`--aeterni-badge-size-md` 和 `--aeterni-width-control-md`。
- 提供玻璃表面的角色别名（填充是独立档位，模糊跟随既有刻度）：`--aeterni-bg-glass`（浅色 68% / 深色 72%，不再跟随 `--aeterni-bg-elevated`）、`--aeterni-blur-glass`（跟随 `--aeterni-blur-md`）和 `--aeterni-blur-scrim`（跟随 `--aeterni-blur-sm`）。库内十处磨砂消费者（`Card` / `Surface` 的 `Glass` 变体、Dialog 面板与遮罩、Drawer 面板与遮罩、Popover 表面与遮罩、Tooltip、Alert/Toast 卡片）统一引用这三个别名，宿主覆盖一处就能整体调整全库玻璃配方，不必逐个组件改。填充停在 68% / 72% 是实测下限，不是偏好值：这两支墨——`--aeterni-text-on-glass-secondary`（`color-mix(in srgb, var(--aeterni-text) 93%, transparent)`）与 `--aeterni-text-on-glass-tertiary`（同样写法的 67%）——都按「最坏背景」（纯黑／纯白）定档，再淡一档就会吃掉这两支墨的 1.1× 余量——它们是全库最紧的一对，浅色由三级墨绑定（1.15×）、深色由次要墨绑定（1.14×）。两支墨都只声明一次、按主体墨的比例给出，所以浅深两主题余量相同、也不会各自漂移，代码里把角色名接过去就行。全部磨砂内容表面（`Card` / `Surface` 的 `Glass` 变体、Dialog 面板、Drawer 面板、Popover 表面、Tooltip 内容、Alert/Toast 卡片）都用它们接管 `--aeterni-text-secondary`、`--aeterni-text-muted`、`--aeterni-state-color-readonly`、`--aeterni-state-color-muted` 与 `--aeterni-text-tertiary`，补掉标准墨在玻璃填充上的缺口（深色最坏背景：标准次要墨 3.41:1、标准三级墨 2.51:1 → 接管后 5.11:1 / 3.51:1）；不透明表面（含 `is-no-blur` 的通知卡片）与三类遮罩仍用标准墨，遮罩本来就不承字。
- 玻璃表面在 `prefers-reduced-transparency: reduce` 与「不支持 `backdrop-filter`」两种情况下退到不透明：填充改 `--aeterni-bg-solid`、模糊归 `none`。只摘模糊会留下半透明色斑——既没有模糊解释它，也没有不透明底承字，两种读法都不如实心表面。同一处把两支玻璃专用墨退回标准墨：它们的用途就是扛住透出来的背景，填充已经是实心表面之后就没有背景要扛了。遮罩只摘模糊、保留压暗，因此它仍然把背后的页面压下去。
- 提供可发现滚动条 Token：`--aeterni-scrollbar-size`、`--aeterni-scrollbar-thumb`、`--aeterni-scrollbar-track`；长选项列表和长通知堆栈使用细滚动条而不是隐藏滚动条。
- 提供控件圆角阶梯 `--aeterni-radius-control-sm/md/lg` 与 `--aeterni-radius-button*`、`--aeterni-radius-input*`、`--aeterni-radius-surface`；同一尺寸档位的 Button、Input 和 Textarea 圆角一致。
- 提供 `--aeterni-transition-control`（背景色 + 边框色 + 文字色 + 阴影）：Input、Textarea 与 ComboBox 触发器共用同一条过渡声明，不再各自重复三个复合变量。
- 实心语意表面使用专用前景 Token `--aeterni-color-on-semantic`（浅色主题为近黑墨色、深色主题为反色墨色），用于 success/warning/danger/info 实心控件的正文，保证两种主题下对比度 ≥ 4.5:1。
- 开关类控件的指示块使用 `--aeterni-state-background-thumb`，在浅色与深色轨道上都保持可辨识。
- 焦点环使用 `--aeterni-focus-color`，无效控件使用 `--aeterni-focus-color-invalid`；`Input` 与 `Textarea` 也消费这两组 Token。
- 字体栈中的 Inter 与 JetBrains Mono 是可选的宿主依赖，库不随包提供 webfont；宿主未提供时回落到系统 UI 字体（等宽回落 Consolas / Courier New）。
- 无显式主题时，`@media (prefers-color-scheme: dark)` 提供系统偏好兜底，避免深色偏好用户在脚本执行前看到浅色闪烁；一旦存在显式 `data-theme` / `data-aeterni-mode`，该兜底不再生效。
- 支持系统主题跟随。
- 主题切换由 `ThemeService` 管理。
- `ThemeProvider` 为自闭合组件，不需要包裹 Layout 内容。
- `ThemeProvider` 负责注入主题 JS、监听系统主题变化、同步页面主题。
- `ThemeProvider` 不渲染 DOM：基类的 `Id`、`Class`、`Style` 和 `Visible` 对它无效，主题只写到 `<html>` 上。
- 推荐在样式表之前放置预渲染主题脚本（读取 `aeterni.theme.mode`、`aeterni.theme.brand` 与 `prefers-color-scheme`，写入 `data-theme` 与 `data-aeterni-brand`），否则深色偏好用户或非默认品牌用户会在 Blazor 启动前看到一帧浅色或紫罗兰；示例 `wwwroot/index.html` 已包含该脚本，可直接复制。该脚本的品牌回退字面量必须与 `AeterniUIOptions.DefaultBrand` 保持一致，脚本读不到 .NET 选项，不一致就表现为一帧闪烁。
- 首屏加载器与预渲染脚本受同一条约束：这个时刻还没有组件树，组件库的隔离样式（含 `Spinner` 的 `@keyframes`）也尚未生效，所以加载器只能是宿主自己声明的静态标记，不能是组件实例。示例 `wwwroot/index.html` + `css/app.css` 的 `.sample-boot` 即按此实现：光环复刻 `Spinner` 的规格（走 `--aeterni-spinner-size` 扩展点、3px 描边、右侧缺口、`--aeterni-duration-slower × 1.4` 线性周期、`opacity .92`），颜色取 `--aeterni-color-brand-text`，文案读运行时的 `--blazor-load-percentage-text` 并在无值时回落；因此两种主题自动换档、`prefers-reduced-motion` 下自动停转。它是刻意的视觉复刻而非复用，改一侧规格必须同步另一侧。
- 页面主题通过 `<html data-theme>` 输出；示例保留浏览器预渲染主题脚本与明暗、品牌色切换。
- `ThemeSwitch` 提供 System、Light、Dark 分段切换，并能在刷新后正确反映当前模式。
- `ThemeBrandSwitch` 提供 Purple、Green、Orange 品牌分段切换，与 `ThemeSwitch` 是同一套结构：它是 `Segmented` 的专用用法，订阅 `BrandChanged` 在外部改动时同步选中态，尺寸档位与标签（`ThemeBrandSwitchLabel` / `ThemeBrandPurpleLabel` / `ThemeBrandGreenLabel` / `ThemeBrandOrangeLabel`）都取既有约定。
- 品牌色层通过 `<html data-aeterni-brand>` 输出，与明暗维度同一元素；切换品牌会复用主题切换的过渡动画（`aeterni-theme-transitioning`）。
- 主题模式与品牌色层分别在 `localStorage` 的 `aeterni.theme.mode` 和 `aeterni.theme.brand` 下持久化，两个键互相独立：清除其中一个不会重置另一个。
- 主题切换包含过渡动画，并适配 reduced-motion 场景。

- `ThemeService.Mode` 的类型是 `ThemeMode`（`System` / `Light` / `Dark`）；`CurrentTheme` 的类型是 `ThemeKind`（`Light` / `Dark`），表示实际生效的主题。`ThemeService.Brand` 的类型是 `ThemeBrand`（`Purple` / `Green` / `Orange`），不属于 `ThemeMode` 的取值空间。
## 4. Button

### 支持能力

`Button` 当前支持：

- `Intent`：`Default`（主题色主操作）、`Neutral`、`Warning`、`Danger`。
- `Variant`：`Solid`、`Outline`、`Soft`、`Ghost`、`Link`；默认是 `Solid`。
- `Size`：`Small`、`Default`、`Large`。
- `Type`（原生 HTML button 类型）：`Button`、`Submit`、`Reset`。
- `StartIcon`、`EndIcon`，以及兼容旧 API 的 `Icon`。
- `ChildContent`、`Loading`、`Disabled`、`FullWidth`。
- `AriaLabel`、`OnClick`。
- `Intent` 表达操作语义，`Variant` 表达视觉形式；二者可组合，例如 `Danger + Outline` 表达低强调危险操作。
- `IconButton` 提供独立的方形图标操作，默认使用 `Ghost + Neutral`，要求 `AriaLabel`，支持 `Icon`、`Size`、`Loading`、`Disabled` 和 `OnClick`。
- `MenuButton` 组合 `Button`、`PopupHost`、`Popover` 与 `Menu`，支持 `Items`、`Open`/`OpenChanged`、`Placement`、`Intent`、`Variant`、`Size`、`OnItemSelected`、`Loading` 和 `FullWidth`；触发器输出 `aria-haspopup`、`aria-expanded` 和 `aria-controls`，并与菜单根节点建立稳定 id 关联。
- MenuButton 的弹出内容使用 Menu，不使用 List；PopupHost 仅定位、无 padding。仅其 Popover 外围 padding 从 12px 收至 spacing-1（4px），Menu 行内横向 padding 从 12px 收至 control-padding-x-sm（8px），保留 Menu 的 spacing-2（8px）子项缩进表达折叠层级；参照现有 List 的 4px 外围与 8px 行内留白，Menu 根不重复加外围 padding，List 本身不变。折叠或无可见子项的分组不保留标题后的 gap，避免末组产生额外底部留白；Popover 上下各 4px。菜单行高仍为 32px，普通内容 Popover 和独立 Menu 保持原留白。SplitButton 复用此紧凑菜单。
- `MenuButton` 拥有自己的真实根元素，`Id`、`Class`、`Style`、`Visible`、`AdditionalAttributes`、`Element` 与 `ElementChanged` 均遵循基类契约；打开、禁用和 `FullWidth` 状态同时落在根容器与真实触发按钮的正确层级。
- 默认、悬浮、按下、聚焦、禁用和加载状态。
- `Warning` 与 `Danger` 实心按钮使用 `--aeterni-color-on-semantic` 前景；透明与 `Soft` 变体使用对应语意色的文字/柔和 Token。
- 按钮圆角使用 `--aeterni-radius-button` 系列 Token，默认值为 `0.625rem`，Small/Large 与默认保持同一圆角视觉。
- 禁用状态统一消费 `--aeterni-state-*` Token，保持所有变体的前景、背景和边框可读；`Link` 禁用时不会显示下划线。
- 加载状态包含 Spinner、`aria-busy` 和禁用交互。遮罩对所有变体使用同一套处理：自身表面的半透明层 + `blur(2px)`，因此文字会被模糊但仍然可见。
- `Link` 变体悬浮时只显示下划线，不显示背景色。

### 行为与无障碍

使用原生 `<button>` 语义，`Type`（`ButtonType`：`Button` / `Submit` / `Reset`）控制原生类型；`Disabled` 输出原生 `disabled` 与 `aria-disabled`，`Loading` 期间阻止重复提交并保持尺寸稳定；键盘使用原生 Enter/Space 激活，`:focus-visible` 提供清晰焦点环。按钮语义通过 `Intent` 表达，不与原生 `ButtonType` 混淆。

### 实现边界

`Button` 仍支持通过 `Icon`（兼容简写）、`StartIcon` 和 `EndIcon` 传入图标；独立图标操作使用 `IconButton`，菜单触发操作使用 `MenuButton`。
### SplitButton

- `SplitButton` 组合 `Button` 与 `MenuButton`，后者继续拥有 `PopupHost` / `Popover` / `Menu`，不新增 JS 或菜单状态模型。示例：`/components/split-button`。
- `Label` 为必填主动作文本，`AriaLabel` 可单独覆盖主动作可访问名称；箭头菜单按钮的 `MenuAriaLabel` 必填且不能为空，两侧名称独立。`Items` 使用现有 `IReadOnlyList<MenuGroup>`；`OnClick` 与 `OnItemSelected` 分别通知主动作和菜单动作。
- 两侧共享 `Variant`（默认 Ghost）、`Intent`（默认 Neutral）、`Size`（默认 Small），默认即紧凑、弱边界外观，不需要额外变体；显式设置仍可使用全部 Button 尺寸、变体与语意色。`FullWidth` 只让主动作填满剩余宽度。箭头段使用现有 spacing-1 横向内边距，弱分隔线与连接圆角由 Button 自身隔离样式处理；两侧 hover 原位高亮、不上浮，使用逻辑方向支持 RTL，并保留独立 focus-visible。菜单复用 Menu 原有紧凑行高；普通 Button / MenuButton 默认值不变。
- `Disabled`（含上层 ButtonGroup 禁用）优先禁用整体；`PrimaryDisabled` 和 `MenuDisabled` 独立禁用。`Loading` 仅使主动作忙碌且不可点击，不阻止菜单；需锁定两侧时使用 `Disabled`。
- `Open` / `OpenChanged` 采用 MenuButton 的可选受控模式（绑定回调才由外部控制）；未绑定时内部维护。`MenuDisabled` / `Disabled` 强制隐藏菜单并清除内部打开状态；受控值仍归宿主所有，重新启用时按宿主值显示。`Placement` 默认 BottomEnd，相对箭头按钮定位。
- 根容器透传 `Id`、`Class`、`Style`、`Visible`、`AdditionalAttributes`、`Element` / `ElementChanged`；属性不误投到两个按钮。Tab 保留两个原生动作，Enter / Space 激活；菜单内键盘、Escape、外部点击和视口翻转沿用现有实现。MenuButton 启用 Popover 现有 `RestoreFocusOnClose`：菜单选择/Escape 关闭后回归打开时的元素；非模态关闭时若外部点击、Tab 或业务回调已将焦点移到其他控件，不抢回焦点，已禁用/隐藏的触发器也不恢复。Tab 本身不自动关闭菜单，保留现有 Popover 行为。
- 边界：主动作只提供普通按钮，不支持提交/重置、链接、任意子内容、自动将菜单选择替换为主动作、异步编排或 Toolbar roving-focus 集成；加载与业务状态由宿主管理。

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

ButtonGroup 不提供 Toolbar / ToggleGroup 语义；Toolbar 现为独立组件。
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

参数类型分别为 `SurfaceVariant`、`SurfaceElevation`、`SurfacePadding` 和 `SurfaceRadius`；`Card` 复用除 `Radius` 外的全部类型，没有独立的 `Radius` 参数。

### 模糊与实心

模糊是 `Glass` 变体自己的配方，不是独立参数。`Glass` 取 `--aeterni-bg-glass` 的 `.68`／`.72` 填充加 `--aeterni-blur-glass` 的 `backdrop-filter`；其余变体就是各个实心填充（`Default` / `Subtle` / `Elevated`），没有第三种状态需要表达。

这里曾有一条与 `Variant` 正交的 `Blur` 轴（`SurfaceBlur`），已删除：它的每个档位都在重写 `background`，而这些规则声明在 `--subtle` / `--elevated` 之后，所以任何非 `Default` 档位都会静默压过 `Variant`——`Variant` 参数看上去失效，两个参数争夺同一个属性。实心状态已经由其它变体表达，这条轴并没有带来变体表达不了的能力。

- `Glass` 变体不再自己写边框，边框完全跟随 `Bordered`，因为「不要边框的模糊」正是玻璃配方的默认形态；`Card` 默认 `Bordered=true`，所以玻璃卡片要无边框需要显式 `Bordered="false"`。
- 宿主覆盖 `--aeterni-blur-glass` 就能一次性调整库里所有玻璃表面的模糊，组件上没有会把它钉住的修饰类。
- 容器与页面背景仍必须是无彩色（R=G=B），模糊只处理透明度与采样，不引入色相。

模糊必须有东西可采样才看得出来：宿主需要在玻璃表面背后放一层背景内容，平坦背景上的模糊在视觉上与不模糊几乎一致。

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
- `Color` 的六个命名色取 `--aeterni-color-*-text`（浅色 4.6~6.7:1、深色 7.5~10.5:1）；`Color.Default` 不附加修饰类，继续继承 `currentColor`。
- 未传 `AriaLabel` 时输出 `aria-hidden="true"`；传入后输出 `role="img"` 和可访问名称。
- 图标库通过 RenderFragment 与 Button、Dialog、Toast 等组件组合使用。

### 核心内置图标集 `AeterniIcons`

- 位于 `AeterniUI/Icons/AeterniIcons.cs`，是组件库自己的几何定义（16 × 16 网格），不依赖任何图标供应商。
- 提供 `Calendar`、`Clock`、`Check`、`ChevronDown`、`ChevronRight`、`ChevronLeft`、`Xmark`、`Star`、`EmptyBox`、`Info` 和 `Exclamation`。
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
- 选中/不确定填充取品牌填充档（`--aeterni-state-background-checked`），边框与填充同档，勾线墨固定 `--aeterni-text-inverse`；悬浮/按下时整块换档（`--aeterni-state-background-checked-hover` / `-active`），未选中时只提升一档边框，焦点环保持品牌色。无效控件的悬浮/按下保留危险色，指针划过不会把错误提示盖掉。
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
- 视觉与 `Checkbox` 对齐：同一 `--aeterni-control-size-*` 档位、1px 边框、选中态用内圆点而不是加粗边框，并补齐悬浮、无效、禁用与“禁用 + 选中”状态。选中圆环与圆点同取品牌填充档、在悬浮/按下时一起换档；未选中控件的悬浮只提升一档边框；无效控件的悬浮保留危险色。
- 根元素是 `<label>`（与 Checkbox、Switch 一致）：组件类与状态类（`aeterni-radio`、`--sm/--lg`、`is-checked`、`is-disabled`、`is-invalid`）落在根标签上，原生输入通过独立的输入属性集合渲染，因此尺寸与状态样式始终作用于可视圆圈。

### 行为与无障碍

单选值通过 RadioGroup 级联上下文统一绑定；禁用、必填、无效和尺寸档位会传递到选项，并输出相应的原生/ARIA 语义。RadioGroup 与独立 Radio 均合并 FormField 的 Disabled / Required / Invalid，动态更新可恢复正常状态。垂直布局时容器切换为单列，并由 `aria-orientation="vertical"` 同步表达。

位于 `FormField` 中的独立 `Radio` 会采用字段的输入 ID（点击标签可直接聚焦），`RadioGroup` 则让 fieldset 采用该 ID 并用 `aria-labelledby` 关联字段标签。

### 实现边界

当前不提供远程选项、虚拟化和多选行为；方向键导航沿用原生 radio 行为，选中变化通过原生 `change` 事件上报（示例中的方向键选择会同步回业务状态）。

## 14. Switch

### 支持能力

`Switch` 提供适合即时开关设置的二态控件：

- `Value` / `ValueChanged` / `ValueExpression`，支持 `@bind-Value`，内部使用可聚焦的原生 checkbox，并输出 `role="switch"` 与 `aria-checked`。
- 支持 `ChildContent`、`Name`、`Required`、`Invalid`、`Disabled`、`Size`、`AriaLabel`、`AriaDescribedBy` 与 `OnChange`；不提供独立的 `Label` 参数。
- `Size` 提供 `Small` / `Default` / `Large` 三档：轨道尺寸由档位推导（跑道宽 = 旋钮 × 2 + 内边距 × 2 + 边框 × 2，滑块行程恰好等于一个旋钮宽），默认档与之前的固定 38×22 完全一致；`Small` 为 34×20（旋钮 14，与同排 16px Checkbox 成比例）、`Large` 为 46×26。
- 可从 `FormField` 级联获取标签关联、禁用与校验状态。
- 选中轨道取品牌填充档（`--aeterni-state-background-checked`），旋钮保持 `--aeterni-state-background-thumb`（浅色与深色轨道上都可辨识）；悬浮/按下时轨道与边框一起换档，无效控件保留危险色，无效 + 选中时轨道整块取危险色而不是危险边框裹品牌填充。
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

- `List<TItem>` 的 `Items` 为 `IEnumerable<TItem>?`，可从字符串/模型集合自动推断类型，每次参数更新枚举为快照；非 null（包括空集合）优先于 `ChildContent`，null 使用声明式内容。
- `ItemTemplate` 为 `RenderFragment<TItem>?`；默认 context 是当前 item，支持 Context 别名。无模板显示 `item?.ToString()`，null 为空，文本正常 HTML 编码。
- `CardMode` 为 bool，默认 false（没有 Mode 参数）。true 时每项由库统一渲染现有 Card 外壳；`CardTemplate` 为 `RenderFragment<TItem>?`，只负责内容，无模板显示文本，**不回退 ItemTemplate**。普通模式忽略 CardTemplate。声明式 CardMode 包裹 ListItem.ChildContent，Leading/TrailingContent 只用于普通行。
- 普通行与 CardMode 共用选中底色、字重、悬停及活动项反馈；内层 Card 通过现有 Style 参数采用透明背景和继承文字色，保留卡片边框与内容留白，选中底色覆盖整张卡片。CardMode 不再添加额外的活动项描边；禁用选中项保留选择底色并使用禁用文字色。
- `List<TItem>` 提供 `ChildContent`、`SelectionMode`（无选择 / 单选 / 多选）、`SelectedValue`/`SelectedValues` 与 `SelectedValueChanged`/`SelectedValuesChanged`、`AllowClear`、`AriaLabel` 与 `OnItemSelected`。
- 选择模式下容器输出 `role="listbox"` 与 `aria-multiselectable`；交互式 `ListItem` 渲染非可聚焦的 `role="option"` 元素并同步 `aria-selected`、`aria-disabled`。
- `ListItem` 支持 `Value`、`ChildContent`、继承的 `Disabled`、`LeadingContent` / `TrailingContent`；`Selected` 是由 `List` 计算的内部状态，不是可设置参数。
- `AllowClear` 只影响单选模式；多选模式通过再次选择已选项移除该项。
- 支持鼠标、方向键、Home/End 与空格/回车选择；禁用和隐藏项不可选择。导航不会因 AllowClear 清空已选项，点击或空格/回车仍可主动清空。交互式浏览器按当前 DOM 顺序导航，支持声明式 keyed 重排，并阻止已处理按键的默认页面滚动；不拦截 Tab、组合快捷键或后代控件事件。
- 无障碍：容器保持 `role="listbox"` 单点 Tab 聚焦，并用 `aria-activedescendant` 指向当前高亮选项；选项本身带稳定 id、不进 Tab 序列也不接收 DOM 焦点，由容器键盘统一驱动，当前项用 `is-active` 样式提示。
- 无选择模式渲染为普通列表结构，不输出按钮语义。

### 行为与无障碍

选择模式下容器负责单点 Tab 聚焦和键盘导航，禁用项不可选择；无选择模式不伪造按钮或 listbox 交互语义。

### 实现边界

Items 改变清除失效活动项；缺失单选/多选值清理后通过 Changed 回传，不触发 OnItemSelected。相等性沿用 object 默认比较，重复值共享选择状态；null 与单选“未选择”沿用相同表示。动态复用行更新注册 Value/Disabled，释放时注销并清理活动引用。禁用 List 继承到子项、移出 Tab 序列并忽略点击/键盘。选择列表模板不要嵌套按钮/链接等可聚焦控件。

### 10.14.0 迁移与用法

经授权直接迁移为泛型，不保留非泛型 List/ListItem。无 Items 的 `<List>` 改为 `<List TItem="string">`（混合值可选 object）；后代 ListItem 级联推断 TItem，保留 object 单/多选 API。独立包装组件需转发同名 TItem 或显式指定子项 TItem。C# 引用改为 `List<string>` / `ListItem<string>`；集合名称冲突时写为 `System.Collections.Generic.List<T>`。

```razor
<List Items="@(new[] { "设计", "开发" })" />
<List Items="@models">
    <ItemTemplate><strong>@context.Name</strong></ItemTemplate>
</List>
<List Items="@models" CardMode="true">
    <CardTemplate Context="item"><h3>@item.Name</h3></CardTemplate>
</List>
<List TItem="string" SelectionMode="SelectionMode.Single">
    <ListItem Value="@("design")">设计</ListItem>
</List>
```

当前不提供拖拽、虚拟化、分组和异步数据源。

## 17. Rating

### 支持能力

- 整数评分：`Value` / `ValueChanged` / `ValueExpression`、`OnChange`，`Max`（默认 5）与越界钳制。
- 支持 `ReadOnly`、`Disabled`、`AllowClear`（再次点击当前值清零）与 `Icon` 自定义（缺省使用内置 `AeterniIcons.Star`）。Disabled 合并 FormField 状态；只读/禁用星级不进入 Tab 序列。方向键/Home/End 同步真实焦点与值、不清零、不滚动页面，空格继续激活当前聚焦星级；主 JS module 仅处理默认键盘行为。
- 按 `radiogroup` / `radio` 语义输出，支持方向键与 Home/End；`AriaLabel` 缺省取 `AeterniUITextOptions.RatingLabel`。
- 每颗星只有当前值 `aria-checked="true"`（“已填充”视觉与“已选中”语义分离），并使用 roving tabindex：只有当前值（未选中时为第一颗）在 Tab 序列内，一次 Tab 即可进出。
- `Size` 提供三档：`Small` 为 16px 星形，命中区域下限 24px（与 Checkbox Small 同档）、`Default` 为 20px + 8px（与之前一致）、`Large` 为 24px + 12px。
- `aria-checked` 输出显式字符串 `"true"`/`"false"`（布尔值会被渲染成最小化属性，读屏会当成无效值）。
- 填充星取 `--aeterni-color-warning-text`、空星取 `--aeterni-text-tertiary`：星形是实心图形，填充档在浅色下只有 2.2:1。
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
- `Placeholder`、`Required`、`Invalid`、继承的 `Disabled`、`Size`、`AriaLabel` 和 `OnChange`；未提供时 `Placeholder` 取 `AeterniUITextOptions.ComboBoxPlaceholder`、选项列表名取 `AeterniUITextOptions.ComboBoxListLabel`；接入 `EditContext` 校验（`ValueExpression`）并在 `FormField` 中级联。
- `Size` 提供三档并与 `Input` 对齐：`Small` 触发器高度为 `--aeterni-control-height-sm`（28px，与 `Input Size="Small"` 等高）、`Default` 36px、`Large` 44px；触发器与选项列表共用同一套档位 Token（高、水平内边距、字号、圆角、箭头尺寸），选项行高由触发器高度推导。
- 点击触发按钮弹出选项；打开后支持上下方向键、Home/End、Enter 确认；点击外部、Escape 或页面滚动（弹层内部滚动除外）都会关闭，行为接近原生 select。
- `role="combobox"`、`aria-expanded`、`aria-controls` 与选项同步；打开时触发器通过 `aria-activedescendant` 指向高亮项，无效时输出 `aria-invalid`。
- 选项渲染为非可聚焦的 `role="option"` 元素：打开后焦点始终留在触发器上，Tab 不会进入选项列表，键盘提示由 `is-active` 与 `aria-activedescendant` 表达。
- 触发器采用 `FormField` 的输入 ID，并用 `aria-labelledby` / `aria-describedby` 关联字段标签、描述与错误文本。
- 长选项列表使用细滚动条（`--aeterni-scrollbar-*`）而不是隐藏滚动条，滚动提示可见且仍可用鼠标滚轮滚动。
- 弹层通过共享的 `PopupHost` + `Popover` 承载，由共享浮层模块负责锚定、翻转、贴边和外部点击关闭；`CloseOnScroll` 保留页面滚动即关闭的原生 select 行为，选项列表自身滚动不关闭。

### 行为与无障碍

ComboBox 的触发器保持 combobox 语义；方向键可打开并导航，打开后支持 Home/End、Enter/空格确认和 Escape。已处理按键不滚动页面，Enter/空格确认不会再次产生原生点击而重新打开；Tab 和组合快捷键保留原生行为。键盘活动项只滚入选项视窗，不滚动页面。FormField 的 Disabled/Required/Invalid 合并到触发器，禁用或隐藏时关闭选项并拒绝残留点击。空 Items 仍允许打开以显示 EmptyContent。外部点击或页面滚动会关闭选项。

### 实现边界

当前不支持自由输入筛选、远程搜索、虚拟化、无限滚动和多选。

## 19. Progress

### 支持能力

`Progress` 提供线性进度展示，支持 `Value`、`Max`、`Indeterminate`、`Color`、`Size`、`ShowValue` 和 `AriaLabel`。

- 填充取色族的强调文字形态、轨道取同色相 12% 染色（填充/轨道浅色 3.2~4.6:1、深色 5.7~7.5:1；轨道与页面 1.4:1）；填充档直接做进度条时，浅色 success/warning 只有 1.8:1。
- 宽度通过 `--aeterni-progress-value` 自定义属性传入，`indeterminate` 与 reduced-motion 用特异性覆盖，不使用 `!important`。

### 行为与无障碍

输出 `role="progressbar"`、`aria-valuemin`、`aria-valuemax`，确定进度时输出 `aria-valuenow`；不确定状态使用 CSS 动画，并在 reduced-motion 下停止动画。
### 实现边界

当前提供线性进度，不包含环形渲染、上传任务管理和远程数据源。

## 20. PopupHost / Popover

10.14.3：模态/非模态均遵守动态 CloseOnEscape；受控关闭只提议 OpenChanged，父级拒绝时继续显示。退出过渡等待实际 CSS 动画结束才隐藏、恢复焦点及解锁滚动；重开/销毁取消旧完成，reduced-motion 立即完成，Visible=false 不保留退出层。

### 支持能力

`PopupHost` 提供浮层内容挂载容器（`position: relative` 的真实盒子）；`Popover` 支持 `Open` / `OpenChanged`、`Placement`、`Modal`、`CloseOnEscape`、`CloseOnOutsideClick`、`CloseOnScroll`、`RestoreFocusOnClose`、`Header`、`ChildContent` 和 `AriaLabel`。

- `Placement`（`PopupPlacement`：`BottomStart` / `BottomEnd` / `TopStart` / `TopEnd`，默认 `BottomStart`）指定相对 `PopupHost` 的起始方位。
- **定位与翻转**：浮层在 `PopupHost` 内绝对定位，由 `Popover.razor.js` 在打开、`resize` 与页面滚动时重算：优先使用指定方位，空间不足时翻到对侧（每次重算都从首选方位开始，所以视口变宽后能翻回），再沿交叉轴贴回视口边缘（偏移写入 `--aeterni-popover-shift-x`）。
- **关闭**：Escape、非模态下的外部指针、模态下的遮罩点击都会通过 `OpenChanged` 请求关闭（`Open` 始终由使用方持有，与 `@bind-Open` 配套）；`CloseOnEscape`、`CloseOnOutsideClick` 与 `CloseOnScroll` 可分别关掉，滚动关闭会忽略浮层内部滚动。
- **模态**：`Modal` 除切换到 `dialog` 语义外，还渲染遮罩、输出 `aria-modal`、把 Tab 困在层内（`tabindex="-1"` 作为无交互内容时的回退焦点）、锁定背景滚动（带滚动条宽度补偿）并在关闭后把焦点还给打开前的元素。

### 行为与无障碍

Popover 根据 `Modal` 输出 `dialog` 或 `region` 语义，关闭时通过 `hidden` 移除可见内容；模态浮层额外输出 `aria-modal="true"` 并提升到 `--aeterni-z-modal`。

### 实现边界

浮层是在 `PopupHost` 内部定位的：**宿主就是锚点盒**，因此触发元素与浮层都应放进 `PopupHost`——这也让“点击外部”把宿主算作层内，触发按钮不会在同一次按压里既关又开。祖先容器若带 `overflow: hidden` 仍可能裁切浮层；需要脱离祖先裁剪时应由宿主提供合适的布局上下文。浮层故意不使用 `transform`/`translate` 做偏移（改用 `margin-left`），因为变换会让浮层成为自身 `position: fixed` 遮罩的包含块。`CloseOnScroll` 可让非模态浮层在页面/祖先滚动时请求关闭，同时忽略浮层内部滚动。多浮层堆叠与嵌套模态留给后续组件。

## 21. Menu

`Menu` 提供可复用的分组导航菜单，支持 `Items`、`SelectedId`、`Accordion`、`OpenKeys` 和 `AriaLabel`。

### 数据模型

- `MenuGroup`：`Key`、`Label`、`Items`、`Icon`、`InitiallyOpen`、`Disabled`、`Visible`。
- `MenuItem`：`Id`、`Label`、`Description`、`Icon`、`Disabled`、`Href`、`Target`、`Visible`。
- 标签与 `Description` 在同一行显示，空间不足时按需省略（`…`），因此分组开关与菜单项保持同高（`32px`）；菜单本身不会因父容器更高而拉伸行高。
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
- 长菜单不强制滚动容器：把 `Menu` 放进带 `max-height` 的滚动容器（示例侧边栏即如此）即可。分组开关与子项的 `focus-visible` 使用内收焦点环，避免折叠区域和宿主滚动容器裁切；选中态边框与侧边标记保持独立。

### 实现边界

- **表面归属**：`Menu` 是导航内容控件，自身不提供背景/边框/圆角，底由宿主容器提供（示例侧边栏与示例预览都用 `Surface`/侧栏容器给底）。这与 `List` 相反——`List` 一次交付“选项容器”，所以自带 `bg-surface` + 边框。分类规则见 `component-design-guidelines.zh-CN.md` §5.3「表面归属」。
- 不提供多于两层的嵌套、组合式子组件（`ChildContent` + 子组件）、搜索过滤和多选；长菜单不内置滚动容器，把它放进带 `max-height` 的容器即可（示例侧边栏即如此）。
## 22. Tabs

10.14.3：Visible=false 的 Tab 不渲染标签按钮，面板保持隐藏，不参与键盘导航；选中项隐藏时显示首个可用面板作为视觉回退，不擅自回写 Value。动态显示/注销刷新标签栏；焦点模块按实际可见按钮次序移动焦点。

### 支持能力

`Tabs` + `Tab` 是可组合的标签页导航（与 `List`/`RadioGroup` 同一形态：子组件注册自己，父组件渲染标签栏）：

- `Tabs`：`Value` / `ValueChanged`（支持 `@bind-Value`）、`AriaLabel`、`ChildContent`。`AriaLabel` 缺省取 `AeterniUITextOptions.TabsLabel`，可通过 `AeterniUIOptions.Text` 覆写。
- `Tab`：`Value`（必填，同一个 `Tabs` 内必须唯一且非空）、`Header`（标签按钮内容）、`ChildContent`（面板内容）、`Disabled`。重复的 `Value` 会在注册时抛出 `InvalidOperationException`，避免出现两个同时选中的标签。
- `Tab` 自己渲染 `tabpanel`，`Tabs` 渲染整条 `tablist`；两者都接入基类属性契约（`Id`/`Class`/`Style`/`Visible`/`AdditionalAttributes`）。

### 行为与无障碍

- `role="tablist"`（带 `aria-orientation="horizontal"`）、每个按钮 `role="tab"` + `aria-selected`（字符串）、每个面板 `role="tabpanel"`。
- 按钮 id 与面板 id 由注册顺序派生（`{ElementId}-tab-{i}` / `{ElementId}-panel-{i}`），`aria-controls` 与 `aria-labelledby` 成对且不依赖使用方提供 id；面板默认 `tabindex="0"`，无焦点内容时也能用键盘到达。
- roving tabindex：整条标签栏只有一个 Tab 停留点；选中项被禁用时，停留点回退到首个可用标签，因此标签栏始终可用键盘进入。
- 自动激活模型：`ArrowLeft`/`ArrowRight`（RTL 下由模块报告书写方向并自动互换）与 `Home`/`End` 在可用标签之间循环，同时改变选中项并把焦点移回标签栏；`Tabs.razor.js` 只做这两件事（移动焦点、报告方向），无 JS 时标签仍可点击与 Tab 进出。
- `Value` 未匹配任何标签（包括初始 `null`）时，组件把首个可用标签当作选中项渲染，但不会自行改写 `Value`；点击或方向键选中后才会通过 `ValueChanged` 上报。
- 窄屏横向滚动：标签栏不换行、不压缩，使用细滚动条（`--aeterni-scrollbar-*`）横向滚动，焦点环使用外扩负偏移避免被滚动容器裁切。
- 键盘焦点环与禁用态沿用全库 Token（`--aeterni-focus-*`、`--aeterni-state-color-disabled`）；选中标签使用品牌色的文字形态与 2px 指示条。

### 实现边界

- **表面归属**：`Tabs` 是内容控件，不自带表面（在卡片里再套一层底会出现“白底套白底 + 双层边框”）；标签栏只自带一条 `--aeterni-separator` 底部分界线，因此建议整体放进 `Card`/`Surface` 等宿主容器，示例预览即如此。
- 当前只提供水平标签栏：不支持垂直标签、懒加载面板内容、关闭按钮和拖拽排序，也不接管路由（导航用例请配合 `Menu` 的 `Href` 或页面级导航）；面板内容在首次渲染时全部构建，未选中项只是通过 `hidden` 移出无障碍树与布局。

## 23. Tooltip

10.14.3：参数重渲染后同步 Placement、Disabled、Text/Id 和触发内容；禁用或移除提示时仅清理组件自己添加的 aria-describedby token，保留宿主描述关联。

### 支持能力

`Tooltip` 支持 `Text`、`ChildContent`、`Disabled`、`AriaLabel` 和 `Placement`（`TooltipPlacement`：`Top` / `Bottom` / `Start` / `End`，默认 `Top`）：`ChildContent` 是被描述的触发元素，`Text` 才是提示内容（两者不是重复内容参数）。

### 行为与无障碍

提示内容使用 `role="tooltip"`，不阻塞触发元素，也不影响页面布局。`Tooltip.razor.js` 会把提示节点的 id 写入 `ChildContent` 中第一个可聚焦元素的 `aria-describedby`（没有可聚焦元素时回退到触发包装元素），因此读屏可以直接朗读提示文本；提示节点在 `Text` 为空或禁用时完全不渲染，不会留下悬空引用。

方位由 CSS 决定，JS 只在需要时切换方位类并写入偏移变量：优先使用 `Placement` 指定的一侧，空间不足时翻转到对侧，再沿交叉轴偏移以留在视口内（`resize` 时重算）。`prefers-reduced-motion` 下只保留位移过渡的关闭。翻转与贴边的数值计算来自共享浮层模块 `wwwroot/js/aeterni_floating.js`（与 `Popover` 同一套实现）。

### 实现边界

不提供 Escape、点击外部关闭、富交互内容和模态行为；无 JS 时视觉提示仍可用，只是缺少 `aria-describedby` 关联与翻转/偏移。

## 24. ThemeProvider、ThemeSwitch 和 ThemeBrandSwitch 使用方式

### 基础用法

```razor
<ThemeProvider />
<ThemeSwitch />
<ThemeBrandSwitch />
```

`ThemeProvider` 应放置在 Layout 或应用根组件中，但不包裹页面内容。业务代码通过注入 `ThemeService`、使用 `ThemeSwitch` / `ThemeBrandSwitch` 或绑定自己的控件修改主题。

`ThemeProvider` 没有组件参数；`ThemeSwitch` 提供 `AriaLabel`、`Size`、`ModeChanged`，并继承 `Disabled`。控件本身是 [`Segmented`](#32-segmented) 的专用用法：三个模式是它的选项，尺寸档位走同一套控件高度（28 / 36 / 44px），标签取文案表的 `ThemeSystemLabel` / `ThemeLightLabel` / `ThemeDarkLabel`。

`ThemeBrandSwitch` 是同一模式在品牌维度上的实例：`AriaLabel`、`Size`、`BrandChanged` 加继承的 `Disabled`，选项固定为 `Purple` / `Green` / `Orange`（顺序属组件契约，指示块行程由选项数量推导），标签取 `ThemeBrandPurpleLabel` / `ThemeBrandGreenLabel` / `ThemeBrandOrangeLabel`。两个控件可以并排放置，例如示例宿主的顶栏就把它们放在同一组动作区里。

组输出 `role="radiogroup"` 与三个 `role="radio"` 选项（`aria-checked`），整组只有一个 Tab 停留点，方向键即可切换模式；滑块位置由选项数量推导，不再写死三列。

主题模式（System / Light / Dark）会在每次切换时通过 `localStorage`（键 `aeterni.theme.mode`）持久化，下次在浏览器中启动时自动恢复；存储不可用或值非法时回退到默认的 System 模式。

### 品牌色层

品牌与明暗是两个正交维度：明暗决定用色阶的哪几档，品牌决定用哪条色阶。切换品牌可以使用 `ThemeBrandSwitch`，也可以直接调用 `ThemeService`：

```razor
<ThemeBrandSwitch Size="Size.Small" />

@* 或者绑定自己的控件 *@
@inject ThemeService ThemeService

<Button OnClick="@ThemeService.SetGreen">绿色</Button>
<Button OnClick="@ThemeService.SetOrange">焦橙</Button>
<Button OnClick="@ThemeService.SetPurple">紫罗兰</Button>
```

`SetBrand(ThemeBrand)` 会校验枚举值，非法值抛 `ArgumentOutOfRangeException`；同值调用不重复触发事件。品牌变化后 `ThemeProvider` 把 `data-aeterni-brand` 写到 `<html>` 并持久化到 `aeterni.theme.brand`，语义别名随之整体换色，组件层无需任何改动。

首次访问的默认品牌来自 `AeterniUIOptions.DefaultBrand`：

```csharp
builder.Services.AddAeterniUI(options => options.DefaultBrand = ThemeBrand.Green);
```

宿主要自定义第三套品牌色，只需在库样式表之后追加自己的色相层，不需要改 C#：

```css
[data-aeterni-brand="teal"] {
    --aeterni-brand-500: #0F766E;
    /* …其余档位… */
}
```

此时 `ThemeService.SetBrand` 不接受未在 `ThemeBrand` 中枚举的值，需要宿主自行调用 JS 写入属性，或提交新的枚举项。

### 实现边界

系统主题跟随依赖浏览器 `matchMedia`；`ThemeProvider` 仅管理页面主题与品牌色层。
## 25. DialogProvider

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

## 26. Dialog、Confirm、Alert 和 Toast

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
## 27. Divider

### 支持能力

- `Orientation`（`Orientation`：`Horizontal` / `Vertical`，默认 `Horizontal`）与可选 `ChildContent` 中缝内容（文字或图标）。
- 两个形态共用同一份标记：没有中缝内容时根元素本身就是线段（零高度的 `border-top`）；有中缝内容时同一元素内渲染「线段 + 内容 + 线段」，两侧线段各保留 `--aeterni-spacing-4` 的最小长度，所以在收缩宽度的 flex 父级里也不会消失。
- 垂直形态 `align-self: stretch`，长度跟随父级 flex 行；脱离 flex 容器时以 `1em` 兜底。
- 线段使用 `--aeterni-separator`（不透明，浅色 1.7:1 / 深色 1.5:1），随主题切换；无 JS。

### 行为与无障碍

根元素输出 `role="separator"` 与显式的 `aria-orientation`（带值 ARIA 一律输出字符串：该角色没有垂直默认值）；装饰用的线段 `aria-hidden`，中缝内容是普通文本内容。

### 实现边界

不提供渐变/图案装饰线、可拖拽 splitter 和中缝对齐位置参数；中缝内容固定居中，长文本由使用方控制换行。

## 28. Empty

### 支持能力

`Empty` 提供空状态占位：

- `Size`（`Small` / `Default` / `Large`）、`Title`、`Description`、`Icon`（自定义图标或插画）与 `ChildContent`（操作区）。
- 默认插画是新增的核心字形 `AeterniIcons.EmptyBox`，尺寸通过祖先元素上的 `--aeterni-icon-render-size` 传递：Small 取 `--aeterni-icon-size-xl`（24px）、Default 取 `--aeterni-icon-size-2xl`（32px）、Large 在 2xl 基础上加 `--aeterni-spacing-2`（40px）；传入 `Icon` 片段则整体替换插画。
- 插画取 `--aeterni-text-tertiary`（图标/装饰级），标题取 `--aeterni-text-primary`，描述取 `--aeterni-text-secondary`，`Size` 同步字号档位。
- `Title` 为空时使用 `AeterniUITextOptions.EmptyTitle`；`Description` 为空时不渲染该段落；`ChildContent` 为 `null` 时不渲染操作区容器。
- 文本居中、长单词与窄屏用 `overflow-wrap: anywhere` 换行，描述限制在 `--aeterni-empty-measure`（28em）的行长内；无 JS。

### 行为与无障碍

插画是装饰内容（`aria-hidden`），标题与描述是普通文本；操作区里的可访问性由使用方传入的 `Button` 等组件负责。

### 实现边界

不包含数据加载逻辑、插图资源包与动画插画。组件**不绘制表面**（内容控件），应放进 `Card`、`Surface` 或表格主体使用；操作区包装元素只看 `ChildContent` 是否为 `null`——若在片段内部用 `@if` 关掉全部内容，包装元素仍会存在，需要完全移除时请在外层判断或传 `null` 片段。

## 29. Spinner

### 支持能力

- `Size` 三档：`Small` 16px（`--aeterni-icon-size-md`）、`Default` 20px（`--aeterni-icon-size-lg`）、`Large` 32px（`--aeterni-icon-size-2xl`，描边同时从 2px 加粗到 3px）。
- `Color` 命名色取色族的**文字形态**（`--aeterni-color-{role}-text`），`Color.Default` 继承当前文字色。
- `AriaLabel` 提供可访问名称，未提供时取 `AeterniUITextOptions.SpinnerLabel`。
- 环形视觉只有一份实现：`Spinner.razor.css` 拥有 currentColor 描边、缺一个象限的缺口和 700ms 线性旋转；`Button.Loading` 改为渲染该组件，只通过 `--aeterni-spinner-size` 按档位（14/16/20px）指定尺寸，两级不再各自维护一份动画。
- `prefers-reduced-motion: reduce` 下停止旋转，环形保持静止但仍作为可见的忙碌指示。

### 行为与无障碍

根元素输出 `role="status"` 与始终存在的可访问名称，因此空白环形也能被读屏播报为加载状态。Button 的加载遮罩本身带 `aria-busy`，它内部的 Spinner 处在 `aria-hidden` 容器中，不会重复播报。

### 实现边界

不显示进度百分比（使用 `Progress`）、不提供遮罩层布局和加载文案；尺寸与颜色之外的视觉由宿主决定。

## 30. Skeleton

### 支持能力

- `Variant`（`SkeletonVariant`：`Text` / `Rectangle` / `Circle`）、`Lines`（至少 1，用于多行文本占位）、`Width` 与 `Height`（CSS 长度）。
- 宽高通过自定义属性 `--aeterni-skeleton-width` / `--aeterni-skeleton-height` 传入；默认尺寸按变体：文本行 `1em`（圆角 `sm`）、矩形 `--aeterni-height-md`（圆角 `md`）、圆形 `--aeterni-icon-size-2xl`（`--aeterni-radius-round`，宽度跟随显式高度保持正圆）。
- 多行文本的最后一行按宽度的 80% 收尾，显式 `Width` 也参与这个比例计算。
- 微光由「`--aeterni-surface-soft` 轨道 + 8% 正文墨高光」组成，两种主题下都有可见的明度差；无 JS。

### 行为与无障碍

占位图形对读屏不可见（根元素 `aria-hidden="true"`）；正在加载的容器由使用方标记 `aria-busy`。`prefers-reduced-motion: reduce` 下停止动画并移除高光渐变，占位退化为静态色块。

### 实现边界

不提供与具体组件绑定的骨架模板、延迟加载策略和虚拟列表占位；`Lines` 只是把同一个形状堆叠多次，头像 + 文本这类组合由使用方用多个 `Skeleton` 拼装。

## 31. Badge

### 支持能力

- `Count`（`int?`）、`Max`（默认 99）、`Dot`、`Color`、`AriaLabel` 与 `ChildContent`（被附着的图标、头像或按钮）。
- 数字上限：`Count > Max` 时显示 `{Max}+`；`Count` 为 `null` 且未开启 `Dot` 时不渲染指示器；显式 0 显示为 0；负数与 `Max < 1` 抛出参数异常。
- 指示器压在锚点右上角（逻辑 `inset-*`，RTL 下自动镜像），用自身尺寸的百分比位移精确对准角点；最小 16px 胶囊、最大宽度 40px，超长数字做省略处理；`Dot` 模式是 12px 圆点。
- 颜色分两个角色：`--aeterni-badge-fill`（指示器底）与 `--aeterni-badge-ink`（文字/圆点）。品牌填充配 `--aeterni-text-inverse`，中性与语意填充（亮档）配 `--aeterni-color-on-semantic`。

### 行为与无障碍

数字模式把数字本身作为文本内容；提供 `AriaLabel` 时用完整名称替换裸数字。圆点模式没有可见文字，因此把可访问名称（`AriaLabel` 缺省取 `AeterniUITextOptions.BadgeLabel`）作为视觉隐藏文本输出，而不是在空盒子上挂 `aria-label`。

### 实现边界

只有数字与圆点两种形态，不支持自定义文本内容、独立堆叠布局和消息计数业务逻辑；指示器不自带描边，与锚点重叠处的分离由使用方决定。

## 32. Segmented

### 支持能力

`Segmented<TValue>` 提供互斥选项切换（分段控件）：

- `Items`（`IReadOnlyList<TValue>`，每项等宽）、`Value` / `ValueChanged`（支持 `@bind-Value`）、`TextSelector`、`ItemTemplate`、`DisabledSelector`。
- `Size` 三档走统一控件高度：`Small` 28px、`Default` 36px、`Large` 44px（`--aeterni-control-height-*`）；`FullWidth` 占满父容器，否则宽度跟随内容。
- 选项是原生 `<input type="radio">`，整组共用一个自动生成的 `name`，因此单选语义、`aria-checked`、roving tabindex（整组一个 Tab 停留点）、方向键与 RTL 箭头方向全部由浏览器提供，组件没有键盘 JS。
- 滑块（选中胶囊）的几何与选项数量无关：宽度是「一列」、位移是「index 列」，两者来自组件写入的 `--aeterni-segmented-count` / `--aeterni-segmented-index`，因此 2 项与 5 项共用同一份样式，不再像旧的 `ThemeSwitch` 那样硬编码三列与 `translate3d(200%)`。
- RTL 下选项从内联末端开始排列，滑块方向随之镜像（`:dir(rtl)`，并对不识别 `:dir()` 的引擎保留 `[dir="rtl"]` 回退）。
- `DisabledSelector` 停用单个选项（原生 `disabled`，跳过方向键循环）；`Disabled` 停用整组并输出 `aria-disabled`。`Value` 不匹配任何项时控件不显示选中态与滑块（`is-empty`）。
- 位于 `FormField` 中时采用字段的输入 ID，并用 `aria-labelledby` / `aria-describedby` 关联标签与描述；`Required` / `Invalid` 输出 `aria-required` / `aria-invalid`。
- `ThemeSwitch` 已重构为它的专用用法（三个主题模式作为选项），两者共用同一份滑块动画与尺寸档位。

### 行为与无障碍

根元素输出 `role="radiogroup"`、`aria-orientation="horizontal"` 与可访问名称；选项是原生 radio，选中变化通过原生 `change` 事件上报（鼠标点击与方向键都走同一条路径）。禁用项不可点、不进方向键循环，禁用且被选中的项仍保留胶囊与反色文字，表示“值已选中但不可更改”。

### 实现边界

暂不包含垂直方向、多选分段、可编辑标签和路由集成；选项等宽，超长标签在选项内截断（`text-overflow: ellipsis`）而不是无限撑开控件。非全宽模式按每项最小内容宽度建列，库内置 Orange / System 等标签在常规桌面与窄屏布局中保持完整；选中胶囊在 hover/active 时使用 checked 状态色阶整块换档，普通 hover 不覆盖选中态。

## 33. Accordion

10.14.3：禁用项即使仍在 OpenKeys 中也渲染为折叠、inert 状态，不保留空白展开区域；不擅自修改宿主 OpenKeys，重新启用可恢复原展开状态。标题与箭头使用禁用文字 Token。

### 支持能力

`Accordion` 提供折叠面板，支持 `AccordionItem`、单开/多开模式、受控 `OpenKeys`、`OpenKeysChanged`、禁用项和唯一项 ID 校验。

### 行为与无障碍

每个面板使用原生按钮，并通过 `aria-expanded` / `aria-controls` 建立按钮与内容关联；展开/折叠通过 CSS grid 行高与透明度过渡实现，`prefers-reduced-motion` 下自动关闭动画。关闭面板显式输出 `aria-hidden="true"` 与 `inert`，内部链接、按钮和表单控件会同时退出 Tab 顺序及辅助技术可达范围。

### 实现边界

组件只负责展开状态，不提供远程数据加载、虚拟化或嵌套面板管理。示例：`/components/accordion`。

## 34. Pagination

### 支持能力

`Pagination` 提供轻量分页导航，支持 `CurrentPage`、`TotalPages`、`CurrentPageChanged`、`SiblingCount`、`AriaLabel`、`PreviousLabel`、`NextLabel` 和 `PageLabelFormat`；页码较多时显示省略号，当前页通过颜色突出，不缩放；按钮使用 Small 控件高度、明确的小号字号与单倍行高，等宽数字居中排列。实例文案优先于 `AeterniUITextOptions` 的全局默认值。

### 行为与无障碍

输出 `aria-current="page"`、上一页/下一页禁用状态和键盘可访问按钮；`prefers-reduced-motion` 下关闭过渡。上一页、页码和下一页的可访问名称来自可覆写文案，方向字形复用核心 `ChevronLeft` / `ChevronRight` 图标。

### 实现边界

分页只负责导航状态，不承担数据加载、分页查询或缓存。示例：`/components/pagination`。

## 35. Avatar

### 支持能力

`Avatar` 支持图片、姓名缩写和自定义内容，提供 `Src`、`Alt`、`Name`、`AriaLabel`、公共 `Size`、`Color` 和 `ChildContent`；缺少图片时从 `Name` 生成最多两个字符的缩写。默认尺寸不输出修饰类，所有公开颜色（含 `Primary` / `Info`）都有对应样式。

### 行为与无障碍

图片路径只由内部 `<img alt>` 命名，外层不重复声明图像角色；姓名缩写和自定义内容路径由根元素的 `role="img"` / `aria-label` 命名。自定义内容必须通过 `AriaLabel`、`Alt` 或 `Name` 提供可访问名称，否则组件会抛出参数异常；显式空 `Alt` 可让纯装饰图片保持静默。

### 实现边界

Avatar 不负责图片加载失败后的远程重试或头像组布局。尺寸统一使用全库公共 `Size` 枚举；原 `AvatarSize.Small/Default/Large` 仅保留为已弃用且同类型的常量别名，常见 Razor 调用可以继续编译，迁移时直接替换为 `Size`。示例：`/components/avatar`。

## 36. Drawer

### 支持能力

`Drawer` 提供从视口边缘滑出的面板：

- `Open` / `OpenChanged`（受控，支持 `@bind-Open`）、`Placement`（`DrawerPlacement`：`Start` / `End` / `Top` / `Bottom`，默认 `End`）、`Modal`（默认 `true`）。
- 模态形态：遮罩、`role="dialog"` + `aria-modal="true"`、Tab 焦点陷阱、背景滚动锁（带滚动条宽度补偿）、关闭后焦点回到打开它的元素。
- 非模态形态：不渲染遮罩、不锁滚动、不抢焦点，输出 `role="region"`；页面保持可交互，只在面板外的指针按下或 Escape 时请求关闭。
- 关闭路径统一由 `OpenChanged` 驱动：`CloseOnEscape`、`CloseOnOutsideClick`、内置关闭按钮（`ShowCloseButton`）都只“请求关闭”，`@bind-Open` 仍是唯一事实源；`Disabled` 会锁住所有关闭路径。
- 内容分三段：`Title`（标题，同时作为面板的无障碍名称）、`ChildContent`（可滚动的 body）与 `Footer`（不滚动的底部操作条）；关闭按钮的无障碍名称缺省取 `AeterniUITextOptions.DrawerCloseLabel`。
- 面板尺寸由 `--aeterni-drawer-size`（默认 `--aeterni-overlay-width-sm`，420px）决定，可用 `Style` 覆盖；`Start` / `End` 用逻辑内联边定位，RTL 下自动镜像；窄屏自动收到视口宽度（100%）。
- 入场动画按方位（内联 / 块方向）使用 `transform` 位移，`prefers-reduced-motion: reduce` 下停止；非模态的遮罩不参与渲染，因此固定定位层不会被自身的 `transform` 影响。

### 行为与无障碍

面板在打开时可用 `Escape` 关闭（模态由焦点陷阱接管，非模态由文档级监听接管），关闭后如果面板是模态且焦点仍在面板内则交给焦点陷阱送回打开它的元素。

### 实现边界

面板是 `position: fixed`，所以应放在 Layout 或页面层级，而不是放在带 `transform` / `filter` / `backdrop-filter` 的容器（例如玻璃卡）里——那些祖先会成为固定定位的包含块。当前不支持多抽屉堆叠、可拖拽调宽与路由集成。Drawer 按方位滑出、Popover 内容淡出上移；退出复用现有动效 Token，等待浏览器实际动画结束后隐藏并释放焦点陷阱与滚动锁。快速重开/释放会取消旧完成回调；reduced-motion 不等待固定毫秒数。Visible=false 立即隐藏并释放，不保留退场。

## 37. 服务注册

使用以下扩展完成基础服务注册：

```csharp
builder.Services.AddAeterniUI();
```

注册内容包括：

- `AeterniUIOptions`（含 `Text` 文案表、`DefaultBrand` 品牌默认值）。
- `JsModuleManager`。
- `ThemeService`。
- `DialogService`。
- `IDialogService`。

### 文案与本地化

库内用户可见文案集中在 `AeterniUIOptions.Text`（类型 `AeterniUITextOptions`），默认值为英文；通过 `AddAeterniUI` 覆写即可整体本地化。日期、范围、日历导航与分页文案也使用同一入口：

```csharp
builder.Services.AddAeterniUI(options =>
{
    options.Text.ComboBoxPlaceholder = "请选择";
    options.Text.ComboBoxListLabel = "选项";
    options.Text.TimePickerPlaceholder = "请选择时间";
    options.Text.TimePickerLabel = "时间选择器";
    options.Text.TimePickerOptionsLabel = "可用时间";
    options.Text.TimePickerEmptyText = "没有可用时间";
    options.Text.TimePickerHourLabel = "时";
    options.Text.TimePickerMinuteLabel = "分";
    options.Text.TimePickerSecondLabel = "秒";
    options.Text.TimePickerCancelText = "取消";
    options.Text.TimePickerConfirmText = "确定";
    options.Text.DateTimePickerPlaceholder = "请选择日期和时间";
    options.Text.DateTimePickerLabel = "日期时间选择器";
    options.Text.DatePickerPlaceholder = "请选择日期";
    options.Text.DatePickerLabel = "日期选择器";
    options.Text.DatePickerCalendarLabel = "日历";
    options.Text.DatePickerPreviousMonthLabel = "上个月";
    options.Text.DatePickerNextMonthLabel = "下个月";
    options.Text.DateRangePickerLabel = "日期范围选择器";
    options.Text.DateRangePickerPresetsLabel = "快捷范围";
    options.Text.PaginationLabel = "分页导航";
    options.Text.PaginationPreviousLabel = "上一页";
    options.Text.PaginationNextLabel = "下一页";
    options.Text.PaginationPageLabelFormat = "第 {0} 页";
    options.Text.RatingLabel = "评分";
    options.Text.ThemeSwitchLabel = "主题模式";
    options.Text.ThemeSystemLabel = "跟随系统";
    options.Text.ThemeLightLabel = "浅色";
    options.Text.ThemeDarkLabel = "深色";
    options.Text.ThemeBrandSwitchLabel = "品牌色";
    options.Text.ThemeBrandPurpleLabel = "紫罗兰";
    options.Text.ThemeBrandGreenLabel = "绿色";
    options.Text.ThemeBrandOrangeLabel = "焦橙";
    options.Text.AlertCloseLabel = "关闭提示";
    options.Text.ToastCloseLabel = "关闭通知";
    options.Text.DialogCloseLabel = "关闭对话框";
    options.Text.DialogLabel = "对话框";
    options.Text.TagDismissLabel = "移除标签";
    options.Text.SpinnerLabel = "加载中";
    options.Text.EmptyTitle = "暂无数据";
    options.Text.BadgeLabel = "有新内容";
    options.Text.DrawerLabel = "面板";
    options.Text.DrawerCloseLabel = "关闭面板";
});
```

组件参数（如 `AriaLabel`、`Placeholder`、`DismissLabel`）的优先级始终高于文案表；文案表为空白的条目会回落到英文默认值。

## 38. DatePicker / DateRangePicker

10.14.3：最小/最大日期的月份导航与键盘运算不会越界；日历维持 6×7 网格，超出 DateOnly 范围的位置为空白不可操作格。最大年份不足指定月份数时只显示存在的月份，范围预设允许结束于 DateOnly.MaxValue。

日期时间字段（`DatePicker`、`DateRangePicker`、`TimePicker`、`DateTimePicker`）的 Small / Default / Large 触发器统一消费 `control-padding-x-sm/md/lg`（8 / 12 / 16px），字号与 Input / ComboBox 对齐为 12 / 14 / 16px，保持 `line-height-normal`、原有最小高度及宽度 / FullWidth 行为；不调整日历或时间面板。Segmented 选项的三档水平留白也改用同组 control Token（数值不变），轨道 padding、滑块几何与字号均不变。

`DatePicker.FullWidth` 默认为 `false`，设为 `true` 时触发器铺满父容器，日历弹层仍按内容定宽。`DateRangePicker` 弹层按预设区与多月日历的内容宽度展开，并受视口宽度限制，保留左右一致的表面内边距；极窄视口下内容可横向滚动。

### 支持能力

`DatePicker` 通过 `Value` / `ValueChanged` 绑定 `DateOnly?`；`DateRangePicker` 通过 `StartDate` / `EndDate` 及对应回调绑定范围。两者支持 `MinDate`、`MaxDate`、`DisabledDate`、`Format`、`Placeholder`、`AriaLabel`、`Placement`、`Size`、`Required`、`Invalid` 和对应的字段表达式参数。

`DateRangePicker` 额外支持：

- `Presets`（`IReadOnlyList<DateRangePreset>`）：宿主提供标签、开始日期和结束日期；组件不内置“最近 7 天”等业务算法。预设标签不得为空、开始不得晚于结束，整个区间必须满足边界与 `DisabledDate` 规则。
- `VisibleMonths`：同时显示 1～3 个月，默认 1；多月模式使用共享导航栏，隐藏相邻网格重复的跨月日期。
- `PresetsAriaLabel`：快捷范围区域的可访问名称。

### 行为与无障碍

日历处理月份切换、方向键/Home/End/PageUp/PageDown、Enter/Space 选择，以及日期范围选择时的悬停预览。日期网格使用 `grid` / `row` / `gridcell` 层级与 roving tabindex；方向键跨月后会在新渲染完成时把真实 DOM 焦点移到活动日期，连续操作时视觉焦点与浏览器焦点保持一致。触发按钮输出展开状态、弹层语义、必填和无效状态；放在 `EditForm` 中时会通过 `EditContext` 通知字段变化并反映验证状态。嵌套 `FormField` 时会继承标签、描述、错误和禁用语义，弹层关闭后可将焦点回到触发按钮。快捷范围按钮使用 `aria-pressed` 表示当前范围。

### 组合关系与实现边界

两个选择器共享 `PopupHost`、`Popover` 和内部 `DateCalendar`；`DateCalendar` 只负责月份网格及键盘导航，标记为非稳定公共 API，不应直接使用。两者都有本组件拥有的真实根元素，基类属性完整落到 DOM；Small/Default/Large、hover、focus-visible、disabled 与 invalid 状态统一消费字段控件 Token，错误态不会被指针反馈覆盖。多月视图限制为 3 个月，不包含虚拟化、自定义日历系统或复杂本地化历法。示例：`/components/date-picker`。

## 39. TimePicker

10.14.3：候选枚举只访问 MinTime/MaxTime 范围内、按午夜对齐 Step 的整秒点；小数秒下界向上取整。TimePicker 与内部滚轮按实例复用无 DisabledTime、且 Step/Min/Max 不变的不可变映射；有委托时每次重新计算，不缓存可变闭包结果。

10.12.2：共享时间轮的文字视觉层采用连续 rotateX、适度缩放与渐淡，中心保持原字号与清晰度；真实行高、选项命中区及 listbox 语义不变，28/36/44 控件密度不变。首次显示直接定位，点击、键盘、外部 Value 与联动列改值沿现有 duration-slow / ease-standard Token 平滑居中；滚动停稳并完成吸附后只回调一次草稿。重复渲染与同值回声不重启动画，新输入取消旧动画，快速改值以最新目标为准。reduced-motion 直接定位并取消透视；隐藏展开与尺寸变化重新测量真实行高。TimePicker 示例支持输入目标时分秒及延迟 2 秒外部设置，便于打开弹层观察；DateTimePicker 共用视觉与动画，并同步打开期间的外部 Value。

可用性、秒步长、Min/Max 和时分秒联动仍由 C# 映射决定，不在 JS 建立业务值副本。滚动吸附继续由单一 JS 动画负责，不叠加 CSS 强制 snap；异步回调返回仅触发渲染，不恢复旧浏览器动画。局部透视半径为实际行高的 8 倍、每行旋转 18°，仅是视觉几何而非新增 Token 体系。自动化覆盖模拟浏览器时序与真实 module，不能代替真实设备惯性、触控和 Apple 风格视觉验收；本轮未进行真实浏览器视觉验收。


滚轮连续滚动期间仅更新浏览器端的远近视觉反馈，停稳后才同步草稿并居中吸附；允许一次跨越多个候选，不再逐项强制停顿。相同选中值的重渲染不会重新定位滚轮，联动改变的其他列仍会同步居中。该行为由 `DateTimePicker` 共用。吸附仅由 JS 停稳逻辑负责，不与 CSS 强制吸附叠加；新的滚轮或触控输入会中断旧吸附动画，支持立即反向。弹层从隐藏变为可见时，按实际列高重新定位到当前选中值。

### 支持能力

- `Value` / `ValueChanged` / `ValueExpression` 绑定 `TimeOnly?`，并接入 `EditContext` 字段通知与验证状态。
- `Step` 定义时间精度，默认 1 秒；支持 1 秒到小于一天的整秒步长。选择面始终显示“时 / 分 / 秒”三列，较大步长只保留实际可用的刻度；`MinTime` / `MaxTime` 定义同一天内的连续边界，`DisabledTime` 由宿主过滤具体候选值。
- `TimeFormat` 默认 `TwentyFourHour`，默认触发器格式为 `HH:mm:ss`；也支持显式选择 `Auto` 或 `TwelveHour`，显式 `Format` 始终优先。12 小时制的小时列把 AM/PM 与小时一起显示，不额外制造一列停留点。
- 支持 `Placeholder`、`AriaLabel`、`OptionsAriaLabel`、`CancelText`、`ConfirmText`、`Placement`、`Size`、`FullWidth`、`Required`、`Invalid` 和继承的 `Disabled`。默认宽度至少使用 `--aeterni-width-input-xs`（160px，受父容器宽度限制），较长文本可继续撑开；`FullWidth=true` 时占满父容器。默认文案来自 `AeterniUITextOptions`。

### 行为与无障碍

触发器使用 `aria-haspopup="dialog"` 与 `aria-expanded`；弹层内的时/分/秒滚轮组只有一个 Tab 停留点，各列使用 `listbox` / `option` 语义，根容器通过 `aria-activedescendant` 指向活动值，取消与确定按钮保持原生 Tab 顺序。列名固定在顶部；五行视窗中的选择带固定在垂直中心且只保留上下分隔线，三个当前值不再各自画框。内容在选择带后滚动并平滑吸附，上下渐隐和近大远小表达滚轮层次；首次打开则直接定位，不播放入场滚动。鼠标滚轮、触控滚动和点击只更新草稿，“确定”才回写并关闭，“取消”丢弃草稿；Left/Right 切换列，ArrowUp/ArrowDown 与 Home/End 在当前列移动，Enter/Space 等同确定。触发器上的 ArrowUp/ArrowDown 直接选择前后可用时间，Escape 关闭；弹层复用 `PopupHost` + `Popover`，关闭后恢复触发器焦点。

### 实现边界

有效时间仍以午夜为锚点按固定步长计算，但内部使用一天 86,400 个整秒槽位的可用性映射并只渲染当前时/分/秒三列：1 秒步长最多输出 144 个 `option`，不会创建 86,400 个 DOM 节点。当前只支持同一天内 `MinTime <= MaxTime` 的连续范围；步长必须是整秒，不支持亚秒精度、跨午夜范围、自由文本编辑、虚拟化或时区换算。示例：`/components/time-picker`。

## 40. DateTimePicker

组合弹层按日期与时间两栏的实际内容定宽，最大宽度受视口限制；窄屏保持上下排列，极窄视口允许内容横向滚动。时间区底部操作行按按钮自然高度布局，不随日历高度拉伸；无可用时间时保留六行滚轮等高占位并居中提示，取消仍可用、确定禁用。提交行为不变：TimePicker / DateTimePicker 使用确认与取消，DatePicker / DateRangePicker 保持原有选择提交方式。

`DatePicker`、`TimePicker`、`DateTimePicker` 均支持 `FullWidth`（默认 `false`），默认最小宽度统一为 `--aeterni-width-input-xs`（160px，受父容器限制），长文本可撑开；设为 `true` 时触发器铺满父容器，不改变弹层内容布局。`DateRangePicker` 不在此统一范围内。

### 支持能力

- `Value` / `ValueChanged` / `ValueExpression` 绑定单一 `DateTime?`，在一个触发器和弹层中组合内部日期网格与时间列表。
- `MinDateTime` / `MaxDateTime` 定义边界，`DisabledDate` 禁用整天，`DisabledDateTime` 禁用具体候选值；没有任何可用时间的日期自动不可选。
- `TimeStep`、`TimeFormat` 控制共享时/分/秒滚轮的精度与显示；`TimeStep` 默认 1 秒、`TimeFormat` 默认 24 小时制，完整触发器默认显示到秒，显式 `Format` 始终优先。
- `Kind` 指定组件新建值的 `DateTimeKind`；支持 `Placeholder`、`AriaLabel`、`CancelText`、`ConfirmText`、`Placement`、`Size`、`Required`、`Invalid`、继承的 `Disabled` 和 `FormField` / `EditContext` 语义。

### 行为与无障碍

选择日期后弹层保持打开；如果原时间在新日期不可用，自动选择当天最接近原时间的可用候选。日期和滚轮修改共同保存在弹层草稿中，确定后一次性提交完整值，取消或外部关闭不会改写原值。日期与时间区域分别使用完整的日历 roving-focus 和单停留点时/分/秒滚轮语义，Popover 负责外部点击、Escape 与焦点回归。

### 实现边界

`Kind` 只通过 `DateTime.SpecifyKind` 标记组件创建的值，不做 UTC、本地时间或任意时区之间的转换；宿主应让 Value、Min/Max 与禁用规则使用一致的时间语义。当前不包含时区数据库、夏令时歧义处理、自由文本解析、亚秒精度或多时区格式化。示例：`/components/date-time-picker`。

## 41. Toolbar / ToolbarGroup

默认布局为紧凑工具栏：组间使用 `spacing-2`、组内使用 `spacing-1`，仍无背景、无边框；不覆盖子按钮显式 `Size` / `Variant`，也不改变普通 Button / MenuButton 的默认外观。示例使用 Small 图标动作展示 IDE 式文件、编辑和更多操作分组。

- `Toolbar`：`ChildContent`、`Orientation`（Horizontal 默认 / Vertical，拒绝无效枚举）、必填非空 `AriaLabel`，以及基类 `Id`、`Class`、`Style`、`Visible`、`Disabled`、`Element` / `ElementChanged`、`AdditionalAttributes`。
- `ToolbarGroup`：必填非空 `AriaLabel`、`ChildContent` 与同一基类公共属性；命名分组不增加 Tab 停留点，布局继承工具栏方向。
- 无背景动作容器，仅接纳 `Button`、`IconButton`、`MenuButton`；整条工具栏一个 Tab 停留点，记住最近可用项；所属轴方向键循环、Home/End 首尾、横向 RTL 反转。非所属轴、Enter/Space、菜单内部键盘不被接管。
- 跳过隐藏、disabled、aria-disabled、加载中或 inert 后代；整条或分组 Disabled 使用 inert 禁止交互。动态卸载、重排后更新候选，必要时恢复焦点；没有可用项时可见且未禁用的根成为停留点。卸载 JS 时恢复原 tabindex。
- 名称由宿主提供，无需新增默认文案。命名不明确时抛出参数异常。
- 不支持输入框混合控件、自动折叠、自动溢出或业务选择状态；宿主显式放置 MenuButton。无 JS 的静态输出保留按钮原生 Tab 顺序，交互模块加载后启用 roving focus。示例：`/components/toolbar`。

## 42. ToggleGroup

默认使用连体分段外观：项间无 gap、横向不自动换行，共享接缝重叠一个 border-width，只有外侧首尾保留圆角；逻辑方向边角兼容 RTL / 纵向，单项保留完整圆角。选中、悬浮与键盘焦点分层，focus-visible 不被邻项遮挡；选中 hover / active 继续整块沿品牌色阶换档。不改 Segmented、普通 Button 或受控选择/键盘模型。

- 命名空间 `AeterniUI.Components.ToggleGroup`：`ToggleGroup` 与不可变记录 `ToggleGroupItem(string Id, string Text, bool Disabled = false)`；Id 按 Ordinal 区分大小写且稳定，Text 是按钮可访问名称，均不可空白，Id 不可重复。
- 参数：必填 `Items`、非空 `AriaLabel`；`SelectionMode` 默认 Single，仅接受现有枚举 Single / Multiple（None 与非法枚举拒绝）；`SelectedValues`（`IReadOnlyList<string>`，默认空）及 `SelectedValuesChanged`；`Orientation`（默认 Horizontal）、`Size`（默认 Default）和全部基类根属性。枚举、null 列表、重复或未知选中 Id、Single 多值输入均抛出参数异常。
- 严格受控：点击或 Enter/Space 只发出新列表提议，父级通过 `@bind-SelectedValues` 或回调回写；不修改参数和宿主列表，不自动选首项。Single 选择另一项会替换；再次激活已选项允许清空。Multiple 独立切换。禁用保留选择但禁止提议；动态删除动作必须同批删除对应选中 Id，切换为 Single 前必须将多值收敛到至多一个。
- 根为具名 `role="group"`，项为原生 `type="button"` 与字符串 `aria-pressed`；两个模式都表达动作开关，不输出 `aria-checked`/radio/表单校验语义。`Segmented` 继续负责表单值单选。方向由 `data-orientation` 提供给焦点模块，不向 group 写入不支持的 `aria-orientation`。
- JS 仅管焦点：单 Tab 停留点，初始首个可用动作；方向键只移焦点不改选择，横向 RTL 反转，纵向上下键，Home/End 首尾并循环；Enter/Space 使用原生激活，Tab 离开。跳过禁用、隐藏、inert 项，记住最后焦点；动态重排/移除/禁用后只修复所属焦点，不抢外部焦点。无可用项时可见且未禁用的根可聚焦；整组禁用移出 Tab 顺序。销毁恢复 tabindex 并释放监听器与 Observer。无 JS 时保留原生按钮 Tab 顺序。
- 无背景容器，选中态与 checked hover/active 均使用现有品牌状态 Token；Small / Default / Large 最小高度继续为 28 / 36 / 44px，水平留白统一消费 `control-padding-x-sm/md/lg`（8 / 12 / 16px），垂直 padding 为 0；内容显式居中，字号、semibold 字重和 `leading-none` 行高与 Button 对齐。横向不自动换行，支持系统 reduced-motion。示例 `/components/toggle-group` 含单选清空、多选、父级拒绝提议、外部重置、动态禁用、重排、RTL/纵向和可见性。
- 第一版不支持 FormField/EditContext、Required/Invalid、图标/内容模板、嵌套 Toolbar、跨组焦点、自动溢出、异步动作编排或快捷键注册。焦点规则与 Toolbar 一致，但模块独立，不提前抽象共享基础设施。

## 43. Breadcrumb

### 支持能力

`Breadcrumb` 使用 `IReadOnlyList<BreadcrumbItem>` 渲染层级导航路径。每项包含 `Label`、可选 `Href`、可选装饰 `Icon`、`Disabled`、`Visible` 和可选 `Target`（取 `_blank` 时自动补 `rel="noopener noreferrer"`，与 `Menu` 一致）；可通过 `AriaLabel` 指定导航区域名称，也可用 `Separator` 替换默认 Chevron 分隔图标。

### 行为与无障碍

- 根元素是带名称的 `<nav>`，内部使用有序列表表达阅读顺序。
- 非当前且有 `Href` 的项渲染为原生链接，保留浏览器的中键、新标签和历史行为；组件不注入路由或导航服务。
- 最后一个可见项自动标记为当前页（`aria-current="page"`）；当前项回退为纯文本。
- 被禁用的层级保留链接语义并标记 `aria-disabled="true"`，移出 Tab 序列，并由样式阻断指针导航；没有 `Href` 的禁用层级只呈现禁用文字，不再输出无对应交互状态的 `aria-disabled`。
- 隐藏项不参与分隔符和当前项判断；图标与分隔符为装饰内容并标记 `aria-hidden`。
- 根属性沿用 `AeterniComponent` 的 `Id`、`Class`、`Style`、`Visible`、`Element`、`ElementChanged` 和 `AdditionalAttributes`。

### 实现边界

第一版不提供路由集成、自动折叠/省略、拖拽排序和异步导航；响应式换行与层级可见性由组件样式和宿主数据控制。示例：`/components/breadcrumb`。

## 44. Stepper

### 支持能力

`Stepper` 使用 `IReadOnlyList<StepperItem>` 按顺序表达线性流程。`StepperItem` 包含稳定 `Id`、`Label`、可选 `Description`、`Completed`、`Disabled` 和 `Visible`；Stepper 通过 `Value` / `ValueChanged` 受控当前步骤，并支持 `Orientation.Horizontal` / `Vertical`、`AriaLabel` 和 `OnStepClick`。

### 行为与无障碍

- 根元素是带名称的 `role="group"`，步骤使用有序列表和原生按钮，保留浏览器 Tab、Enter 和 Space 行为。
- 当前项输出 `aria-current="step"`；每个可见项输出 `aria-posinset` / `aria-setsize`，描述文本通过 `aria-describedby` 关联。
- 完成状态只消费宿主传入的 `Completed`，不根据当前索引或点击行为自动推断；禁用状态由组件根 `Disabled` 或项级 `Disabled` 合并决定。
- 点击可用且非当前步骤时提出 `ValueChanged`，随后触发 `OnStepClick`；组件不擅自修改业务流程状态。
- 当前步骤不可重新激活，因此不提供 hover 换档或 `pointer` 光标，只有可激活的已完成步骤保留指针反馈。
- 隐藏项不参与编号、连接线和位置计算；根属性沿用 `AeterniComponent` 公共契约。

### 实现边界

第一版不提供异步编排、路由集成、自动推进、步骤内容面板、拖拽排序或复杂流程校验；步骤完成与是否可跳转由宿主提供。示例：`/components/stepper`。

## 45. 当前边界

- 当前包含不依赖浏览器服务的最小组件渲染契约检查，覆盖关键根属性、ARIA、Tab 停留点、日历网格和公开样式变体；它不是完整业务或端到端测试套件。
- 组件库目前优先完善基础组件和基础服务，复杂表单、数据展示和导航组件尚未纳入已完成清单。
- `Drawer` 是固定定位面板，不能嵌在带 `transform` / `filter` / `backdrop-filter` 的容器内；多抽屉堆叠与可拖拽调宽不在当前范围。
- `Button`、`IconButton` 和 `MenuButton` 已提供基础动作、图标动作与菜单触发能力；Toolbar、ToggleGroup、SplitButton、Breadcrumb 与 Stepper 已交付。
- `Stack` 和 `Flex` 尚未实现。
- `ComboBox` 的弹层已迁移到共享 `PopupHost` + `Popover`；通过 `CloseOnScroll` 保留“页面滚动即关闭、列表自身滚动不关闭”的原生 select 行为。
- `Dialog` 的 Tab/Escape 处理仍由 `DialogProvider` 自己持有，因为对话框是一个堆栈（只有最顶层响应）：共享模块只提供了滚动锁与可聚焦元素列表。
