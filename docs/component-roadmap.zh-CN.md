# AeterniUI 组件路线图

文档版本：`10.7.0`

状态：v0.1、v0.2 与 v0.3 已完成（第三阶段 7/7：Divider、Empty、Spinner、Skeleton、Badge、Segmented、Drawer）；组件审阅待办四轮（29 + 21 + 8 + 4 项）已全部修复，见 `component-review-todo.zh-CN.md`

本文档记录当前阶段的组件任务、实现边界和验收规则，并随组件交付同步更新状态。

## 文档边界

- `docs/current-features.zh-CN.md` 是当前已实现公共 API 和行为的唯一功能事实源；组件参数和行为以源码为准。
- 本文档中的 v0.1 组件小节只保留目标、验收重点、任务状态和实现差异，不重复当前 API。
- 当前 API 与行为以 `current-features.zh-CN.md` 和源码为准；历史计划只有在解释实现差异时才保留。
- v0.2 及后续章节是 TODO/规划，不得在功能文档或项目索引中写成已实现能力。

## v0.1 交付总览

- [x] 记录组件路线图和验收规则
- [x] 1. `FormField` + `Label`
- [x] 2. `Checkbox`
- [x] 3. `Switch`
- [x] 4. `Tag`
- [x] 5. `List` + `ListItem`
- [x] 6. `Rating`
- [x] 7. `ComboBox`

完成某一项后，只勾选对应条目，并在本文档的任务记录中填写构建结果；不要提前勾选依赖它的任务。

## 实施顺序和依赖

```text
Input
  |
  +--> FormField + Label
          |
          +--> Checkbox
          +--> Switch
          +--> Tag
          +--> List + ListItem
                  |
                  +--> Rating
                  +--> ComboBox
```

- `Input` 已完成，是后续表单控件的低层单行输入。
- `FormField` 负责标签、描述和错误信息的布局与语义关联；它不替代 `Input`，也不复制输入值逻辑。
- `ComboBox` 放在最后，复用 `Input` 的输入语义和 `List` 的选项/键盘行为。
- 每个任务完成后都要先运行 `dotnet build aeterni_ui.slnx`，构建通过后才进入下一项。
- 每个公共 API 任务都要同步更新 `docs/current-features.zh-CN.md` 和 `src/AeterniUI.Sample` 示例。

## 已完成任务记录

以下任务的当前 API 和行为只维护在 [`current-features.zh-CN.md`](current-features.zh-CN.md)；本节只保留目标、验收重点和实现差异。

### 1. FormField + Label

状态：已完成

目标：统一标签、描述、错误信息和控件插槽。

验收重点：标签关联、验证消息优先级、窄屏布局、禁用/无效语义，以及与 `Input` 的组合。

当前实现：以 `current-features.zh-CN.md` 和源码为准。

### 2. Checkbox

状态：已完成

目标：提供原生复选框语义和稳定的布尔值绑定。

验收重点：原生 checkbox 语义、Indeterminate 显示、鼠标和键盘操作、禁用状态、EditContext 校验。

实现差异：`Indeterminate` 仅作为显示状态；当前不提供独立的三态值模型。

### 3. Switch

状态：已完成

目标：提供即时设置使用的二态开关，不复用 Checkbox 的视觉实现。

验收重点：原生 checkbox 基础、`role="switch"`、`aria-checked`、禁用行为、reduced-motion 和窄屏稳定性。

### 4. Tag

状态：已完成

目标：提供分类、状态和筛选条件使用的紧凑标签。

验收重点：尺寸和变体、语意色、图标布局、可关闭行为、关闭按钮无障碍名称和 reduced-motion。

实现决策：关闭动作继续复用 Button 图标能力；独立图标操作使用新增的 `IconButton`，菜单触发操作使用 `MenuButton`。

### 5. List + ListItem

状态：已完成

目标：提供静态列表及单选/多选能力，不承担虚拟化和远程数据加载。

验收重点：listbox 语义、鼠标与键盘选择、禁用项、单选/多选绑定、长文本布局。

实现差异：当前选项使用原生 button；容器通过 `aria-activedescendant` 统一键盘导航，不额外实现拖拽、虚拟化、分组或异步数据源。

### 6. Rating

状态：已完成

目标：提供整数评分输入和只读展示。

验收重点：范围校验、radiogroup/radio 语义、方向键、Home/End、只读和清除行为。

实现差异：当前只支持整数评分，不实现半星。

### 7. ComboBox

状态：已完成

目标：提供下拉选择、键盘导航和选项选择。

验收重点：combobox 语义、打开/关闭行为、方向键、Enter、Escape、外部点击和滚动关闭。

实现差异：当前是纯下拉选择，不支持自由输入筛选、`ReadOnly`、`Clearable`、远程搜索、虚拟化或多选。详细 API 见 `current-features.zh-CN.md`。

## 每项任务的固定交付物

1. 组件目录中的 `.razor`、`.razor.cs`、`.razor.css`，仅在确有浏览器行为时添加 `.razor.js`。
2. 必要的公开枚举或模型，参数按组件规范排列并校验枚举/数值边界。
3. 示例页面中的可操作示例，覆盖默认、禁用、聚焦、无效和关键交互状态。
4. `docs/current-features.zh-CN.md` 的已完成功能记录。
5. 静态检查后运行 `dotnet build aeterni_ui.slnx`；构建失败时先修复，再更新 TODO 状态。

## 任务记录

| 任务 | 状态 | 构建 | 备注 |
| --- | --- | --- | --- |
| 路线图文档 | 已完成 | 未涉及 | 仅记录计划，未修改组件代码 |
| FormField + Label | 已完成 | 通过 | 已随 `dotnet build aeterni_ui.slnx` 验证 |
| Checkbox | 已完成 | 通过 | 原生 `<input type="checkbox">` + 最小 JS 处理 indeterminate；已在示例与文档同步 |
| Switch | 已完成 | 通过 | 原生 checkbox + `role="switch"`，无 JS；示例与文档已同步 |
| Tag | 已完成 | 通过 | 三种变体 + 语意色 + 尺寸 + 可关闭（复用 Button 图标能力） |
| List + ListItem | 已完成 | 通过 | `listbox` 容器、原生 button 选项与 `aria-selected`、单选/多选、方向键与空格回车选择；示例与文档已同步 |
| Rating | 已完成 | 通过 | 整数评分、radiogroup 语义、方向键与 Home/End、AllowClear/ReadOnly |
| ComboBox | 已完成 | 通过 | 交付为纯下拉选择（点击弹出、无输入搜索，方向键/Enter/Esc/外部点击/滚动关闭）；原计划中的输入筛选不适用 |

## 第二阶段（v0.2）计划

状态：规划中。功能新增将递增次版本号（例如 `10.0.0` → `10.1.0`）；问题修复递增补丁版本号（例如 `10.0.0` → `10.0.1`）；破坏性公共 API 变更递增主版本号（例如 `10.0.0` → `11.0.0`）。v0.1 已覆盖表单、选择与反馈基础组件，v0.2 补齐高频基础控件与浮层能力。

### TODO 总览

- [x] 8. `Textarea`
- [x] 9. `Radio` + `RadioGroup`
- [x] 10. `Progress`（Linear）
- [x] 11. `Tooltip`（四方位 + 翻转/贴边）
- [x] 12. `PopupHost` + `Popover`（定位、翻转/贴边、遮罩、焦点陷阱、外部点击）
- [x] 13. `Tabs`（原条目中的 `Drawer`、`Empty`、`Divider` 已移入第三阶段）

### 依赖顺序

```text
Input/Input 样式族
  +--> Textarea
  +--> Radio + RadioGroup      （radiogroup 语义，类似 List 的键盘约定）
  |      |
  |      +--> Progress          （纯展示，可被 Loading/上传复用）
  |
  +--> PopupHost               （全局挂载层 + 锚定/翻转/点击外部/滚动关闭的通用能力）
          |
          +--> Tooltip
          +--> Popover
          +--> ComboBox 弹层迁移（替换内置 fixed 定位）
  |
  +--> Tabs                    （roving tabindex + 焦点控制，复用 List/Menu 的键盘约定）
```

### 8. Textarea

状态：已完成

目标：提供复用 `Input` 语义和校验约定的多行文本控件。

依赖：`Input`、`FormField`。

验收重点：Value 绑定、Rows/Resize、只读/禁用/无效状态、窄屏换行、`aria-invalid` 和 EditContext 校验。

暂不包含：富文本编辑、自动高度和异步校验。

交付：`Textarea` 已加入组件库、示例画廊和当前功能文档。

### 9. Radio + RadioGroup

状态：已完成

目标：提供单选项及其分组语义。

依赖：`FormField`、现有 List 的键盘交互约定。

验收重点：原生 radio 语义、`radiogroup`、方向键导航、Tab 单点进出组、禁用状态和表单校验。

暂不包含：远程选项、虚拟化和多选行为。

交付：已加入组件库、示例画廊和当前功能文档。

### 10. Progress

状态：已完成

目标：提供线性进度展示，并复用统一语意色和尺寸 Token。

依赖：现有 Token 和 Button Loading 的视觉约定。

验收重点：Value/Max、Indeterminate、progressbar ARIA、动画和 reduced-motion。

暂不包含：环形渲染、上传任务管理、远程数据源和业务状态管理。

交付：已加入组件库和当前功能文档。

### 11. Tooltip

状态：已完成（四方位 + 翻转/贴边）

目标：提供不阻塞页面的 hover/focus-visible 辅助说明。

依赖：`PopupHost`、共享浮层模块。

验收重点：hover/focus-visible 提示、`TooltipPlacement` 四方位、空间不足时翻转与交叉轴贴边、tooltip 语义、`aria-describedby` 关联、reduced-motion。

暂不包含：Escape、点击外部关闭、复杂交互内容和模态行为。

交付：组件库、当前功能文档、示例页均已同步；翻转/贴边改为复用 `wwwroot/js/aeterni_floating.js`。

### 12. PopupHost + Popover

状态：已完成（定位、翻转/贴边、遮罩、焦点陷阱、外部点击、背景滚动锁全部落地）

目标：提供浮层挂载容器、锚定浮层与完整的模态/非模态行为。

依赖：共享浮层模块 `wwwroot/js/aeterni_floating.js`（定位/翻转、焦点陷阱、滚动锁）。

验收重点：`PopupPlacement` 四个方位；空间不足时翻转并贴回视口；Escape / 外部指针 / 遮罩通过 `OpenChanged` 请求关闭（`Open` 由使用方持有）；`:focus-visible` 焦点环不被裁切；`Modal` 提供遮罩、`aria-modal`、Tab 循环、关闭后焦点回归与背景滚动锁；`prefers-reduced-motion` 下关闭入场动画。

暂不包含：多浮层堆叠、嵌套模态、相对视口的 fixed 定位（仍由 `ComboBox` 自己的弹层实现承载）。

基础验收重点：多实例挂载、Open/hidden 状态、Popover 的模态/非模态语义。

后续影响：后续增强定位能力后再迁移 ComboBox，并供 Tooltip 使用。

交付：已加入组件库和当前功能文档。

### 13. Tabs

状态：已完成

目标：补充标签页导航基础组件。

交付：`Tabs` + `Tab`，与 `List`/`RadioGroup` 同为可组合组件：`Tab` 注册自己并渲染自己的面板，`Tabs` 负责渲染标签栏与整套 ARIA 关联。选择由 `Value`/`ValueChanged` 控制（支持 `@bind-Value`），`AriaLabel` 缺省取 `AeterniUITextOptions.TabsLabel`。

验收重点（均已通过渲染验证）：

- `tablist`/`tab`/`tabpanel` 三件套；按钮 id 与面板 id 由注册顺序派生（`{ElementId}-tab-{i}` / `-panel-{i}`），`aria-controls` 与 `aria-labelledby` 成对且不依赖使用方提供 id。
- roving tabindex：整条标签栏只有一个 Tab 停留点；选中项被禁用时停留点回退到首个可用项。
- 自动激活：`ArrowLeft`/`ArrowRight`（RTL 下自动互换）与 `Home`/`End` 在可用标签间循环并同时改变选中项，焦点随后移回标签栏（由 `Tabs.razor.js` 完成唯一的浏览器行为）。
- 禁用标签不可点、不可选中、不进入方向键循环。
- 窄屏横向滚动：标签栏不换行、不压缩，改用细滚动条横向滚动，焦点环不会被裁切。
- `Value` 未匹配任何标签（包括初始 `null`）时默认把首个可用标签当作选中项渲染，但不会自改 `Value`。

暂不包含：垂直标签、复杂导航路由、数据加载、懒加载面板内容与页面业务状态。

说明：本条目原包含的 `Drawer`、`Empty`、`Divider` 已移入第三阶段（v0.3）计划。

说明：本条目原包含的 `Drawer`、`Empty`、`Divider` 已移入第三阶段（v0.3）计划，见下文第 15、16、21 项。

### 14. Menu

状态：已完成

目标：提供可分组折叠的菜单基础组件，支持手风琴模式、选中项、图标、描述和禁用状态。

交付：已加入组件库，并用于示例组件页面的侧边栏导航。

### 第二阶段固定交付物与验收

沿用第一阶段固定交付物；每个公共 API 任务完成时同步 `docs/current-features.zh-CN.md`、示例页与本文档 TODO/任务记录，并在 `dotnet build aeterni_ui.slnx` 通过后勾选。

### 第二阶段任务记录

| 任务 | 状态 | 构建 | 备注 |
| --- | --- | --- | --- |
| v0.2 计划 | 规划中 | 未涉及 | 仅记录计划，未修改组件代码 |
| Textarea | 已完成 | 通过 | 原生 `<textarea>`、标准绑定、Rows/Resize、表单校验和无障碍状态；示例已同步 |
| Radio / RadioGroup | 已完成 | 通过 | 原生 radio/fieldset 语义、泛型绑定、禁用/必填/无效状态；示例已同步 |
| Progress | 已完成 | 通过 | 线性 progressbar、确定/不确定状态、语意色、尺寸和 reduced-motion |
| PopupHost / Popover | 已完成 | 通过 | 定位（`PopupPlacement` 四方位）、视口翻转与交叉轴贴边、Escape/外部指针/遮罩关闭（通过 `OpenChanged`）、模态遮罩与 `aria-modal`、Tab 焦点陷阱、背景滚动锁与焦点回归 |
| Tooltip | 已完成 | 通过 | 四方位 hover/focus-visible 提示、`aria-describedby` 关联、翻转与贴边改为复用共享浮层模块 |
| Tabs | 已完成 | 通过 | `tablist`/`tab`/`tabpanel` 三件套与按注册顺序派生的成对 id、roving tabindex（含选中项被禁用时回退到首个可用项）、自动激活模型 + 方向键/Home/End（RTL 自动互换）、禁用标签、窄屏横向滚动与细滚动条、`Value` 未匹配任何标签时默认显示首个可用项 |
| Menu | 已完成 | 通过 | 分组折叠、Accordion、选中项、图标/描述/禁用项；已接入组件示例侧边栏并新增示例专区；键盘模型、`aria-expanded`/`aria-controls`、折叠可见性、`Href` 链接项、受控 `OpenKeys`、分组级禁用/可见性、`ul`/`li` 语义、RTL 方向键与方向键滚动抑制已补齐 |
| 文档与代码一致性审计 | 已完成 | 通过 | 修正 current-features 标题编号（§21 重复）并补 Label、DialogProvider 章节；补齐 List/Menu/Rating 参数与枚举类型名；示例页补 Progress、Tooltip、Popover 参数表与 Radio 无障碍参数；清理规范中的陈旧组件名（Select→ComboBox、Stack/Flex、Toolbar/ToggleGroup）；把 check-docs.sh 写入 AGENTS.md 与 agent-development |
| 组件审阅修复批次 1（P0） | 已完成 | 通过 | Switch 关闭态滑块改用 `--aeterni-state-background-thumb`；Button 加载遮罩按变体取值、实心语意色改用 `--aeterni-color-on-semantic`；Alert 与 Toast 合并到同一位置容器 |
| 组件审阅修复批次 2（P1） | 已完成 | 通过 | Input/Textarea 焦点环；FormField 标签关联（ComboBox/Rating/RadioGroup 补 `aria-labelledby`）；List 选项改 `role="option"` 并移除选项焦点；Rating roving tabindex 与唯一 checked；ComboBox 选项非聚焦化；Radio 状态补齐与 Size 档位；Tooltip `aria-describedby` 与四方位翻转偏移；Tag 关闭热区 24×24；Popup 死 CSS 清理并在文档中明确基础版边界 |
| 组件审阅修复批次 3（P2） | 已完成 | 通过 | 补齐浮层/紧凑表面度量 Token 并把组件裸像素收敛到 Token；删除死类、死 CSS 与死标记；统一圆角阶梯与类名映射（`ComponentClass`）；文案集中到 `AeterniUIOptions.Text`；CSS 与参数声明格式统一；滚动条改为细样式；主题系统偏好兜底与预渲染脚本；字体栈标注为可选依赖 |
| 组件审阅修复批次 4（P3） | 已完成 | 通过 | Tooltip 支持四个方位；RadioGroup 增加 `Orientation` 与垂直布局；通知数量上限写入文档；Rating 自定义图标与内置星形同尺寸；对话框打开时补偿滚动条宽度；Button 禁用态变量与 ButtonGroup 分隔线对齐注释 |
| 色彩体系优化（v10.3） | 已完成 | 通过 | 品牌色按 OKLCH 重建（紫罗兰 `#795AD9`／深色 `#9985ED`）：峰值 chroma 0.237→0.186、色相漂移 7.8°→1.7°、明度阶梯拉直；语意色沿用 Apple 系统色，色阶以 500 档为锚点重建（浅端统一 L 0.976、深端 L 0.30），消除 `Info` 的 400→500 断层与色相漂移；中性文字从紫灰改为冷中性并换掉紫调阴影墨色；新增 `-text` 强调文字形态 Token（修掉亮色档当文字只有 2.2:1 的老问题）；实心控件字色改为「按填充明度分两层、三态不换字」，删掉 `--aeterni-color-on-semantic-strong`；Tag 改浅底深字（2.5~3.5:1 → 5.0~6.9:1）；全系统 44 项对比度校验通过 |
| 图标库完善 | 已完成 | 通过 | 新增核心 `AeterniIcons`（组件内字形全部改为图标渲染）；Font Awesome 适配包升级到 7.3.1 精选 343 个图标并提供 `Categories`/`TryGet`；新增生成脚本和 `/icons` 图标浏览页 |
| 全组件色彩与结构复核（v10.4） | 已完成 | 通过 | 把「填充档不当 ink 用」的规则落实到全部组件：Icon 命名色、Alert/Toast 徽标与弹窗头部图标、FormField 反馈文字、Menu 选中项、Progress 填充/轨道、Rating 星形共六处换上文字形态或按色相染色（最低组合从 1.78:1 提到 3.15:1，ink 类从 1.91:1 提到 3.65:1）；浅色三级文字锚到最暗中性面；中性填充按钮 hover 方向改为两个主题一致；拆出通知卡片的填充/ink 双 accent 角色，消除 `Severity.Default` 死类。结构侧：`NoticeCard`/`ListItem` 重新接入基类属性契约并合并重复标记，静态 `Card`/展示型 `List` 不再注册 DOM 事件，`Progress` 去掉 inline `width` 与 `!important`，`Tag`/`Icon`/`Radio` 的第二套映射收敛到 `ComponentClass`，图标尺寸统一走 `--aeterni-icon-render-size`，六个组件补 `@ref`，`ThemeSwitch` 选项改单一模板，五处布尔型 ARIA 状态改为字符串输出；示例新增 `/components/colors` 验收页 |
| 样式体系一致性（v10.4） | 已完成 | 通过 | 中性容器表面去除彩偏移：浅色四个容器背景改为完全无色（v10.3 的 +2~+4 蓝紫偏移在大面积上会被读成品牌色底，且磨砂层的 `saturate()` 会放大它），深色统一 +3，规则写入 AGENTS.md、规范与提交清单（容器背景只能是黑/白/灰/毛玻璃）；Menu 禁用态由整行 `opacity` 改为 `--aeterni-state-color-disabled`；组件内间距收敛到 `--aeterni-spacing-*`（gap/padding/margin 降为宿主别名）；新增 `--aeterni-transition-control` 消除字段控件三处重复的过渡声明；`--aeterni-opacity-muted` 与 `--aeterni-radius-badge` 获得真实消费者，交互态别名 `bg-hover/-active` 补语义注释；浮层半径层级与 `surface-soft`/`bg-secondary` 分工写入规范 |
| 文字色阶与观感清理（v10.4） | 已完成 | 通过 | 文字改为「单一墨色 + 不透明度阶梯」（Apple label 模型）：浅色 `rgba(0,0,0,.78/.62/.56/.52/.36)`、深色 `rgba(255,255,255,.86/.56/.48/.40/.28)`，主色从 17:1 降到 11.7:1（不再发硬），二级/三级不再只差 1.16:1，且文字会随所在表面自动调和；`tertiary` 降为图标/装饰级，原先用它做正文的 Menu 分组标题、Menu 描述、ThemeSwitch 未选中项与示例页元信息改用 `secondary`/`muted`；Tag 标签文字改用 accent 的 text 形态（85% + 15% 正文墨）而不是「亮填充档 52% + 近黑 48%」，绿/黄 chip 不再浑浊（4.95~7.15:1）；新增不透明的 `--aeterni-separator` 作为卡片/对话框头尾分界（1.71:1 / 1.53:1，原 7% alpha 像污渍）；`--aeterni-color-on-semantic` 从蓝黑 `#0B0F19` 改为无色 `#141414` |
| Size 档位补齐（Switch / ComboBox / Rating） | 已完成 | 通过 | 三个控件补 `Size`（`Small`/`Default`/`Large`）：ComboBox 触发器与选项共用一套档位 Token（Small 32px，与 `Input Size="Small"` 等高）；Switch 轨道由旋钮尺寸推导（跑道 = 旋钮×2 + 内边距×2 + 边框×2，滑块行程恰好一个旋钮宽，默认档与旧值一致，Small 34×20 / Large 46×26）；Rating Small 16px 星形 + 4px 余量、Large 24px + 12px |
| 示例页拆分（v10.5） | 已完成 | 通过 | 把原来的单页 + `switch` 拆成「一个组件一个页面」：`Pages/Components/` 下 25 个页面各自带 `@page`/`@layout`/`PageTitle` 与自己的 `@code` 状态，路由 `/components/{id}` 可直接分享；新增 `Layout/ComponentsLayout.razor`（侧栏 + 路由高亮，内嵌 MainLayout）与 `Components/Showcase/ShowcasePageBase.cs`（仅保留跨页共享的注入服务、交互提示与主题订阅）；作用域 CSS 无法跨页，展示样式迁到示例全局表 `wwwroot/css/showcase.css`；安装页改为垂直单列步骤（每步独占整行代码块）；规范 §10 与项目索引同步为「一个组件一个页面」 |
| 表面归属定案（v10.5） | 已完成 | 通过 | 把「组件是否自带表面」写成规范 §5.3 两行表格（自带表面 vs 内容控件，判断依据是“能不能单独构成一块界面”）并进提交清单；`current-features` 的 Menu/Tabs 实现边界各补一条；示例预览把 Menu/Tabs 放进 `Surface` 宿主容器（原来它们在网格画布上裸奔，看起来像漏了背景）。结论是 Menu/Tabs 按内容控件保持透明，`List` 保持自带表面 |
| 共享浮层能力（v10.4） | 已完成 | 通过 | 新增 `wwwroot/js/aeterni_floating.js`：视口贴合计算（`fitsSide`/`oppositeSide`/`clampCenteredShift`/`clampAlignedShift`）、`createFocusTrap`、引用计数的 `lockScroll`；Tooltip 改为复用，DialogProvider 改用共享滚动锁与可聚焦元素列表（Tab/Escape 仍由对话框堆栈自己持有，因为只有最顶层应响应） |

## 第三阶段（v0.3）计划

状态：已完成（7/7）。第三阶段在 v0.2 的基础上补齐纯展示类基础组件、通用分段控件和侧边面板，并为浮层类组件沉淀共享的定位与焦点能力。

进度：15 `Divider`、16 `Empty`、17 `Spinner`、18 `Skeleton`、19 `Badge` 已在 v10.6 交付；20 `Segmented`（含 `ThemeSwitch` 重构）与 21 `Drawer` 在 v10.7 交付。

版本规则沿用第二阶段：功能新增递增次版本号；问题修复递增补丁版本号；破坏性公共 API 变更递增主版本号。

### TODO 总览

- [x] 15. `Divider`
- [x] 16. `Empty`
- [x] 17. `Spinner`
- [x] 18. `Skeleton`
- [x] 19. `Badge`
- [x] 20. `Segmented`
- [x] 21. `Drawer`

### 依赖顺序

```text
Token 和现有基础组件
  +--> Divider / Skeleton / Badge     （纯展示，无额外依赖）
  +--> Spinner                        （沿用 Button Loading 的视觉约定）
  +--> Empty                          （Icon + Button + Surface）
  +--> Segmented                      （Radio 的单选语义；落地后反向重构 ThemeSwitch）
  +--> FocusTrap + 背景滚动锁抽取       （与 v0.2 第 12 项的 Popup 增强共用）
          |
          +--> Drawer
```

### 15. Divider

状态：已完成

目标：提供水平与垂直分隔线，支持中缝文字或图标。

依赖：Token 层（`--aeterni-border`、间距和字号 Token）。

验收重点：`role="separator"` 与 `aria-orientation`；垂直形态在 flex 容器内自适应高度；中缝内容与线宽对齐；纯 CSS 无 JS。

暂不包含：渐变装饰线、可拖拽的 splitter。

交付：`Orientation` + 可选中缝内容；两种形态共用同一份标记（有内容时两段线各保留最小长度），带值 ARIA 输出字符串，无 JS。示例页 `/components/divider`。

### 16. Empty

状态：已完成

目标：提供空状态占位，支持图标、标题、描述与操作区。

依赖：`Icon`、`Button`、`Surface`；需要新增“空/无数据”核心字形（`AeterniIcons` 目前只有 7 个字形）。

验收重点：默认图标可被 `Icon` 参数或自定义插画替换；`Description` 与操作区插槽；尺寸档位；文本居中与窄屏换行；`ChildContent` 为空时不渲染空壳；无 JS。

暂不包含：数据加载逻辑、插图资源包、动画插画。

交付：新增核心字形 `AeterniIcons.EmptyBox`（字形数 7 → 8）；`Size` 三档同时管插画与字号；组件不绘制表面（在 `Card`/`Surface` 宿主里演示）；`Title`/`Description` 空值分别回落到文案表与不渲染；`ChildContent` 为 `null` 时不渲染操作区容器。示例页 `/components/empty`。

### 17. Spinner

状态：已完成

目标：提供独立加载指示器，供内容区、对话框和页面级加载复用。

依赖：现有 Button spinner 的视觉与 keyframes，落地时应先把该视觉抽成共享实现，再让 `Button.Loading` 引用，避免两份动画。

验收重点：尺寸与语义色档位；可访问名称（`role="status"` 或 `aria-label`）；`prefers-reduced-motion` 降级；与 Button Loading 视觉一致。

暂不包含：进度百分比（使用 `Progress`）和遮罩层布局。

交付：环形定义集中到 `Spinner.razor.css`，删掉 `Button.razor.css` 里的第二份 ring/keyframes；Button 改为渲染 `Spinner`，只通过 `--aeterni-spinner-size` 保留自己 14/16/20px 的档位（加载态尺寸未变）；`role="status"` + 文案表可覆盖的可访问名称；命名色取文字形态，`Default` 继承文字色。示例页 `/components/spinner`。

### 18. Skeleton

状态：已完成

目标：提供内容加载占位（文本行、矩形、圆形及常见组合）。

依赖：Token 层（表面色、圆角和动效 Token）。

验收重点：形状与尺寸参数；微光动画在 `prefers-reduced-motion` 下退化为静态；占位内容对读屏不可见（`aria-hidden`），容器按需标记 `aria-busy`；无 JS。

暂不包含：与具体组件绑定的骨架模板和延迟加载策略。

交付：`SkeletonVariant`（`Text`/`Rectangle`/`Circle`）+ `Lines`/`Width`/`Height`；宽高走 `--aeterni-skeleton-*` 自定义属性；文本块末行按 80% 收尾，圆形宽度跟随显式高度；微光取 `--aeterni-surface-soft` 轨道 + 8% 正文墨高光，reduced-motion 下退化为静态色块。示例页 `/components/skeleton`。

### 19. Badge

状态：已完成

目标：提供数字或圆点角标，附着在图标、头像或按钮上。

依赖：`Tag` 的语义色与 soft/outline 思路。

验收重点：数字上限显示（如 `99+`）、圆点模式、相对父级定位、语义色、最大宽度与溢出处理；数字需要有可访问名称。

暂不包含：独立堆叠布局和消息计数业务逻辑。

交付：`Count`/`Max`/`Dot`/`Color`/`AriaLabel`；逻辑 `inset-*` 定位并按自身尺寸位移对准角点（RTL 自动镜像）；最小 16px 胶囊、最大宽度 40px 并做省略；圆点把可访问名称作为视觉隐藏文本输出（`AeterniUITextOptions.BadgeLabel`）；填充/墨色两角色与参数边界校验（负数、`Max < 1`）。示例页 `/components/badge`。

### 20. Segmented

状态：已完成

目标：提供通用分段控件（互斥选项切换），并把 `ThemeSwitch` 重构为它的专用用法。

依赖：`Radio`/`RadioGroup` 的单选语义与现有 `ThemeSwitch` 的滑块动画。

验收重点：`radiogroup` 语义与 roving tabindex；滑块指示器与选项数量无关（不得像 `ThemeSwitch` 那样硬编码三列和 `translate3d(200%)`）；尺寸档位；单项禁用与整体禁用；方向键；RTL 方向。

暂不包含：多选分段、可编辑标签和路由集成。

说明：公共名为 `Segmented`。落地后 `ThemeSwitch` 应改为基于该组件，并同步解决英文硬编码文案问题（见 [`component-review-todo.zh-CN.md`](component-review-todo.zh-CN.md) 的 REV-18）。

交付：`Segmented<TValue>` 基于原生 radio（`Items` / `TextSelector` / `ItemTemplate` / `DisabledSelector`）；单选语义、`aria-checked`、roving tabindex、方向键与 RTL 方向交给浏览器，因此组件没有键盘 JS；滑块宽度为「一列」、位移为「index 列」，来自组件写入的 `--aeterni-segmented-count` / `--aeterni-segmented-index`，2/3/5 项共用同一份几何；尺寸走控件高度阶梯（32/40/48px）；`ThemeSwitch` 重构为它的专用用法（文案仍取自文案表，不再有硬编码英文）。示例页 `/components/segmented`。

顺带修复：`Radio` 的选项原来只监听 `onclick`，方向键改变原生选中态时不会上报到 C#（受控值会停在旧值）；改为监听原生 `change` 后方向键与鼠标走同一条上报路径。

### 21. Drawer

状态：已完成

目标：提供从视口边缘滑出的面板，支持非模态和模态两种形态。

依赖：必须先抽取 `Dialog` 中已有的焦点陷阱与背景滚动锁能力（见 [`component-review-todo.zh-CN.md`](component-review-todo.zh-CN.md) 的 REV-13、REV-28），避免 `Drawer`、`Popup`、`Dialog` 各维护一份实现。

验收重点：四个方向（start/end/top/bottom）；模态形态使用 `role="dialog"` + `aria-modal` + 焦点陷阱 + 背景滚动锁 + 关闭后焦点回归；非模态形态不抢焦点；Escape 与外部点击（可配置）；`prefers-reduced-motion` 下关闭滑入动效；窄屏宽度自适应。

暂不包含：多抽屉堆叠、可拖拽调整宽度和路由集成。

交付：`DrawerPlacement` 四方位（Start/End 用逻辑内联边，RTL 自动镜像）；模态形态复用共享模块的 `createFocusTrap` / `lockScroll`（遮罩、`aria-modal`、焦点回归、滚动条补偿）；非模态形态不渲染遮罩、不锁滚动也不抢焦点，并在面板外指针与 Escape 时请求关闭；关闭统一经 `OpenChanged`；三段式内容（Title / body / Footer）与内置关闭按钮；`--aeterni-drawer-size` 控制尺寸、窄屏收到 100%；入场动画按方位用 `transform`，reduced-motion 下停止（修正了方位规则因特异性高于媒体查询而未能被覆盖的问题）。示例页 `/components/drawer`。

### 第三阶段前置能力

以下能力不产出新组件，但会阻塞或影响第三阶段质量，需在同批次内完成：

| 能力 | 影响的任务 |
| --- | --- |
| ~~FocusTrap + 背景滚动锁抽取~~（v10.4 已完成：`wwwroot/js/aeterni_floating.js` 提供 `createFocusTrap` / `lockScroll`） | 21 `Drawer`，并回填 v0.2 第 12 项的 Popup 增强（已回填） |
| ~~浮层定位能力（锚定/flip/shift/外部点击）~~（v10.4 已完成：`fitsSide` / `oppositeSide` / `clampCenteredShift` / `clampAlignedShift`） | 21 `Drawer` 的遮罩、v0.2 的 Tooltip 复用（已迁移）与 ComboBox 弹层迁移（待办） |
| ~~语义枚举收敛（`Color`/`Severity`）~~（v10.5 已完成：`ComponentClass.ForColor` / `ToColor` 是唯一映射；v10.6 的 `Spinner` 与 `Badge` 直接复用它） | 19 `Badge`、17 `Spinner` 等新增带色组件（已解决） |
| ~~文案本地化入口~~（v10.6 已完成：`Spinner`/`Badge`/`Empty` 的可访问名称与空状态标题全部走 `AeterniUITextOptions`；v10.7 的 `Segmented` / `Drawer` 也沿用同一入口） | 20 `Segmented`（含 `ThemeSwitch` 重构，已解决） |
| ~~核心图标字形补充~~（v10.6 已完成：新增 `AeterniIcons.EmptyBox`） | 16 `Empty`（已解决）及后续新增组件 |
| ~~示例页拆分~~（v10.5 已完成：一个组件一个页面） | 第三阶段每个组件都需要可交互演示（已具备） |

### 第三阶段固定交付物与验收

沿用第一阶段固定交付物；每个公共 API 任务完成时同步 `docs/current-features.zh-CN.md`、示例页与本文档 TODO/任务记录，并在 `dotnet build aeterni_ui.slnx` 通过后勾选。

### 第三阶段任务记录

| 任务 | 状态 | 构建 | 备注 |
| --- | --- | --- | --- |
| v0.3 计划 | 已完成 | 未涉及 | 仅记录计划，未修改组件代码；7/7 已完成 |
| Divider | 已完成 | 通过 | `role="separator"` + 显式 `aria-orientation`、中缝内容、垂直形态贴合 flex 高度；示例已同步 |
| Empty | 已完成 | 通过 | 新增 `AeterniIcons.EmptyBox`；`Size` 三档、自定义插画、描述与操作区插槽、无表面宿主用法；示例已同步 |
| Spinner | 已完成 | 通过 | 环形视觉抽成共享实现并让 Button Loading 复用（删除第二份 keyframes），尺寸/语义色档位与 `role="status"`；示例已同步 |
| Skeleton | 已完成 | 通过 | Text/Rectangle/Circle + Lines/Width/Height、末行 80%、`aria-hidden`、reduced-motion 静态降级；示例已同步 |
| Badge | 已完成 | 通过 | 数字上限、圆点模式、逻辑定位（RTL 镜像）、最大宽度与溢出处理、可访问名称；示例已同步 |
| Segmented | 已完成 | 通过 | 原生 radio 单选语义（`aria-checked` / roving tabindex / 方向键 / RTL 由浏览器提供）、数量无关的滑块几何、尺寸档位、单项与整体禁用、FormField 接入；`ThemeSwitch` 已重构为它的专用用法；顺带修复 radio 方向键不上报的问题 |
| Drawer | 已完成 | 通过 | 四方位与 RTL 镜像、模态/非模态两态（遮罩、`aria-modal`、焦点陷阱、滚动锁、焦点回归 vs. 不抢焦点/不锁滚动）、`OpenChanged` 受控关闭、三段式内容、窄屏 100% 宽度、reduced-motion 停止动画 |
