# AeterniUI 组件路线图

文档版本：`10.2.0`

状态：v0.1 已完成，v0.2 进行中（仅剩 `Tabs`），v0.3 规划中；组件审阅待办（29 项）已全部修复，见 `component-review-todo.zh-CN.md`

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

实现决策：关闭动作复用 Button 图标能力，不创建独立 `IconButton`。

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
- [x] 11. `Tooltip`（基础版）
- [x] 12. `PopupHost` + `Popover`（基础版）
- [ ] 13. `Tabs`（原条目中的 `Drawer`、`Empty`、`Divider` 已移入第三阶段）

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

状态：已完成（基础版）

目标：提供不阻塞页面的 hover/focus-visible 辅助说明。

依赖：`PopupHost`。

基础验收重点：hover/focus-visible 提示、固定位置、tooltip 语义和 reduced-motion。

暂不包含：Placement、Escape、点击外部关闭、复杂交互内容和模态行为。

交付：已加入组件库和当前功能文档。

### 12. PopupHost + Popover

状态：已完成（基础版；定位、翻转、遮罩、焦点陷阱与点击外部关闭仍未实现，见 `component-review-todo.zh-CN.md` REV-13 的结论）

目标：提供基础浮层挂载和 Popover 模态/非模态语义；锚定、翻转、贴边、点击外部和滚动关闭列为后续增强。

依赖：现有 ComboBox 弹层定位逻辑。

基础验收重点：多实例挂载、Open/hidden 状态、Popover 的模态/非模态语义。

后续影响：后续增强定位能力后再迁移 ComboBox，并供 Tooltip 使用。

交付：已加入组件库和当前功能文档。

### 13. Tabs

状态：未开始

目标：补充标签页导航基础组件。

验收重点：`tablist`/`tab`/`tabpanel` 语义、roving tabindex、方向键与 Home/End、禁用标签、`aria-controls` 与面板 id 对应、窄屏横向滚动。

暂不包含：复杂导航路由、数据加载和页面业务状态。

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
| PopupHost / Popover | 已完成（基础版） | 通过 | 基础挂载容器、Popover region/dialog 语义和 hidden 状态 |
| Tooltip | 已完成（基础版） | 通过 | hover/focus-visible 提示、tooltip 语义和 reduced-motion |
| Tabs | 未开始 | - | 原条目中的 Drawer、Empty、Divider 已移入第三阶段（v0.3）计划 |
| Menu | 已完成 | 通过 | 分组折叠、Accordion、选中项、图标/描述/禁用项；已接入组件示例侧边栏并新增示例专区；键盘模型、`aria-expanded`/`aria-controls`、折叠可见性、`Href` 链接项、受控 `OpenKeys`、分组级禁用/可见性、`ul`/`li` 语义、RTL 方向键与方向键滚动抑制已补齐 |
| 文档与代码一致性审计 | 已完成 | 通过 | 修正 current-features 标题编号（§21 重复）并补 Label、DialogProvider 章节；补齐 List/Menu/Rating 参数与枚举类型名；示例页补 Progress、Tooltip、Popover 参数表与 Radio 无障碍参数；清理规范中的陈旧组件名（Select→ComboBox、Stack/Flex、Toolbar/ToggleGroup）；把 check-docs.sh 写入 AGENTS.md 与 agent-development |
| 组件审阅修复批次 1（P0） | 已完成 | 通过 | Switch 关闭态滑块改用 `--aeterni-state-background-thumb`；Button 加载遮罩按变体取值、实心语意色改用 `--aeterni-color-on-semantic`；Alert 与 Toast 合并到同一位置容器 |
| 组件审阅修复批次 2（P1） | 已完成 | 通过 | Input/Textarea 焦点环；FormField 标签关联（ComboBox/Rating/RadioGroup 补 `aria-labelledby`）；List 选项改 `role="option"` 并移除选项焦点；Rating roving tabindex 与唯一 checked；ComboBox 选项非聚焦化；Radio 状态补齐与 Size 档位；Tooltip `aria-describedby` 与四方位翻转偏移；Tag 关闭热区 24×24；Popup 死 CSS 清理并在文档中明确基础版边界 |
| 组件审阅修复批次 3（P2） | 已完成 | 通过 | 补齐浮层/紧凑表面度量 Token 并把组件裸像素收敛到 Token；删除死类、死 CSS 与死标记；统一圆角阶梯与类名映射（`ComponentClass`）；文案集中到 `AeterniUIOptions.Text`；CSS 与参数声明格式统一；滚动条改为细样式；主题系统偏好兜底与预渲染脚本；字体栈标注为可选依赖 |
| 组件审阅修复批次 4（P3） | 已完成 | 通过 | Tooltip 支持四个方位；RadioGroup 增加 `Orientation` 与垂直布局；通知数量上限写入文档；Rating 自定义图标与内置星形同尺寸；对话框打开时补偿滚动条宽度；Button 禁用态变量与 ButtonGroup 分隔线对齐注释 |
| 图标库完善 | 已完成 | 通过 | 新增核心 `AeterniIcons`（组件内字形全部改为图标渲染）；Font Awesome 适配包升级到 7.3.1 精选 343 个图标并提供 `Categories`/`TryGet`；新增生成脚本和 `/icons` 图标浏览页 |

## 第三阶段（v0.3）计划

状态：规划中。第三阶段在 v0.2 的基础上补齐纯展示类基础组件、通用分段控件和侧边面板，并为浮层类组件沉淀共享的定位与焦点能力。

版本规则沿用第二阶段：功能新增递增次版本号；问题修复递增补丁版本号；破坏性公共 API 变更递增主版本号。

### TODO 总览

- [ ] 15. `Divider`
- [ ] 16. `Empty`
- [ ] 17. `Spinner`
- [ ] 18. `Skeleton`
- [ ] 19. `Badge`
- [ ] 20. `Segmented`
- [ ] 21. `Drawer`

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

状态：未开始

目标：提供水平与垂直分隔线，支持中缝文字或图标。

依赖：Token 层（`--aeterni-border`、间距和字号 Token）。

验收重点：`role="separator"` 与 `aria-orientation`；垂直形态在 flex 容器内自适应高度；中缝内容与线宽对齐；纯 CSS 无 JS。

暂不包含：渐变装饰线、可拖拽的 splitter。

### 16. Empty

状态：未开始

目标：提供空状态占位，支持图标、标题、描述与操作区。

依赖：`Icon`、`Button`、`Surface`；需要新增“空/无数据”核心字形（`AeterniIcons` 目前只有 7 个字形）。

验收重点：默认图标可被 `Icon` 参数或自定义插画替换；`Description` 与操作区插槽；尺寸档位；文本居中与窄屏换行；`ChildContent` 为空时不渲染空壳；无 JS。

暂不包含：数据加载逻辑、插图资源包、动画插画。

### 17. Spinner

状态：未开始

目标：提供独立加载指示器，供内容区、对话框和页面级加载复用。

依赖：现有 Button spinner 的视觉与 keyframes，落地时应先把该视觉抽成共享实现，再让 `Button.Loading` 引用，避免两份动画。

验收重点：尺寸与语义色档位；可访问名称（`role="status"` 或 `aria-label`）；`prefers-reduced-motion` 降级；与 Button Loading 视觉一致。

暂不包含：进度百分比（使用 `Progress`）和遮罩层布局。

### 18. Skeleton

状态：未开始

目标：提供内容加载占位（文本行、矩形、圆形及常见组合）。

依赖：Token 层（表面色、圆角和动效 Token）。

验收重点：形状与尺寸参数；微光动画在 `prefers-reduced-motion` 下退化为静态；占位内容对读屏不可见（`aria-hidden`），容器按需标记 `aria-busy`；无 JS。

暂不包含：与具体组件绑定的骨架模板和延迟加载策略。

### 19. Badge

状态：未开始

目标：提供数字或圆点角标，附着在图标、头像或按钮上。

依赖：`Tag` 的语义色与 soft/outline 思路。

验收重点：数字上限显示（如 `99+`）、圆点模式、相对父级定位、语义色、最大宽度与溢出处理；数字需要有可访问名称。

暂不包含：独立堆叠布局和消息计数业务逻辑。

### 20. Segmented

状态：未开始

目标：提供通用分段控件（互斥选项切换），并把 `ThemeSwitch` 重构为它的专用用法。

依赖：`Radio`/`RadioGroup` 的单选语义与现有 `ThemeSwitch` 的滑块动画。

验收重点：`radiogroup` 语义与 roving tabindex；滑块指示器与选项数量无关（不得像 `ThemeSwitch` 那样硬编码三列和 `translate3d(200%)`）；尺寸档位；单项禁用与整体禁用；方向键；RTL 方向。

暂不包含：多选分段、可编辑标签和路由集成。

说明：公共名为 `Segmented`。落地后 `ThemeSwitch` 应改为基于该组件，并同步解决英文硬编码文案问题（见 [`component-review-todo.zh-CN.md`](component-review-todo.zh-CN.md) 的 REV-18）。

### 21. Drawer

状态：未开始

目标：提供从视口边缘滑出的面板，支持非模态和模态两种形态。

依赖：必须先抽取 `Dialog` 中已有的焦点陷阱与背景滚动锁能力（见 [`component-review-todo.zh-CN.md`](component-review-todo.zh-CN.md) 的 REV-13、REV-28），避免 `Drawer`、`Popup`、`Dialog` 各维护一份实现。

验收重点：四个方向（start/end/top/bottom）；模态形态使用 `role="dialog"` + `aria-modal` + 焦点陷阱 + 背景滚动锁 + 关闭后焦点回归；非模态形态不抢焦点；Escape 与外部点击（可配置）；`prefers-reduced-motion` 下关闭滑入动效；窄屏宽度自适应。

暂不包含：多抽屉堆叠、可拖拽调整宽度和路由集成。

### 第三阶段前置能力

以下能力不产出新组件，但会阻塞或影响第三阶段质量，需在同批次内完成：

| 能力 | 影响的任务 |
| --- | --- |
| FocusTrap + 背景滚动锁抽取 | 21 `Drawer`，并回填 v0.2 第 12 项的 Popup 增强 |
| 浮层定位能力（锚定/flip/shift/外部点击/滚动关闭） | 21 `Drawer` 的遮罩、v0.2 的 ComboBox 弹层迁移与 Tooltip 复用 |
| 语义枚举收敛（`Color`/`Severity`） | 19 `Badge`、17 `Spinner` 等新增带色组件 |
| 文案本地化入口 | 20 `Segmented`（含 `ThemeSwitch` 重构） |
| 核心图标字形补充 | 16 `Empty` 及后续新增组件 |
| 示例页拆分 | 第三阶段每个组件都需要可交互演示 |

### 第三阶段固定交付物与验收

沿用第一阶段固定交付物；每个公共 API 任务完成时同步 `docs/current-features.zh-CN.md`、示例页与本文档 TODO/任务记录，并在 `dotnet build aeterni_ui.slnx` 通过后勾选。

### 第三阶段任务记录

| 任务 | 状态 | 构建 | 备注 |
| --- | --- | --- | --- |
| v0.3 计划 | 规划中 | 未涉及 | 仅记录计划，未修改组件代码 |
| Divider | 未开始 | - | - |
| Empty | 未开始 | - | - |
| Spinner | 未开始 | - | - |
| Skeleton | 未开始 | - | - |
| Badge | 未开始 | - | - |
| Segmented | 未开始 | - | 落地后需重构 ThemeSwitch |
| Drawer | 未开始 | - | 依赖焦点陷阱与滚动锁抽取 |
