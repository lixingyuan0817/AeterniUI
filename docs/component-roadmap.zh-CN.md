# AeterniUI 组件路线图

版本基线：`0.1`

状态：v0.1 已完成，v0.2 规划中

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

状态：规划中。v0.1 已覆盖表单、选择与反馈基础组件，v0.2 补齐高频基础控件与浮层能力。

### TODO 总览

- [ ] 8. `Textarea`
- [ ] 9. `Radio` + `RadioGroup`
- [ ] 10. `Progress`（Linear / Ring）
- [ ] 11. `Tooltip`
- [ ] 12. `PopupHost` + `Popover`
- [ ] 13.（可延后）`Tabs`、`Drawer`、`Empty`、`Divider`

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

- 多行文本控件，`Value` / `ValueChanged` / `ValueExpression`，`Placeholder`、`Rows`、`Resize`（none/vertical）、`Disabled`、`ReadOnly`、`Required`、`Invalid`、`FullWidth`。
- 复用 Input 的边框/状态视觉与 `EditContext` 校验、`FormField` 级联约定。
- 验收：默认、只读、禁用、无效、尺寸与窄屏换行稳定；键盘与表单校验同步 `aria-invalid`。

### 9. Radio + RadioGroup

- `RadioGroup<TItem>`：`Items` + `Value`/`ValueChanged` + `ItemTemplate`/`Radio` 子项两种用法；`Orientation`（Horizontal/Vertical）、`Name`、`Disabled`、`AriaLabel`。
- 容器输出 `role="radiogroup"`；`Radio` 输出原生 `<input type="radio">` 语义，选中态同步 `aria-checked`。
- 键盘：方向键在同组内移动焦点并选中，Tab 单点进出组（roving focus）。
- 接入 `EditContext` 校验与 `FormField` 级联。

### 10. Progress

- `Value`/`Max`（默认 100 且支持百分比显示）、`Variant`（Linear/Ring）、`Indeterminate`、`Color`（沿用语意色）、`Size`。
- 输出 `role="progressbar"` 与 `aria-valuenow/min/max`；纯 CSS 动画 + reduced-motion 降级；可被 Button Loading 的环形指示复用。

### 11. Tooltip

- 文本/内容触发器 + `Placement`（上下左右 + 起止），hover/focus-visible 触发，Esc/点击外部关闭可选；使用 `PopupHost` 定位。
- 保持键盘可聚焦目标可见焦点；不阻塞页面交互。

### 12. PopupHost + Popover

- `PopupHost`：应用根部挂载一次，提供“相对锚点元素定位、视口贴边/翻转、点击外部与滚动关闭、多实例”的基础能力（把 ComboBox 现役 JS 逻辑提升为通用层）。
- `Popover`：基于 PopupHost 的浮层表面（关闭按钮、标题可选、语义 `role="dialog"` 或非模态）。
- 迁移后 ComboBox/后续 Tooltip 不再依赖组件内 fixed 定位，能放进带 `backdrop-filter`/`overflow` 的容器内使用。

### 13.（可延后）Tabs、Drawer、Empty、Divider

- `Tabs`/`TabList`/`Tab`/`TabPanel`：tablist 语义与方向键切换。
- `Drawer`：左侧/右侧滑入的模态面板（沿用 Dialog 的焦点/滚动锁定约定）。
- `Empty`：空状态占位。`Divider`：分割线（水平/垂直）。

### 第二阶段固定交付物与验收

沿用第一阶段固定交付物；每个公共 API 任务完成时同步 `docs/current-features.zh-CN.md`、示例页与本文档 TODO/任务记录，并在 `dotnet build aeterni_ui.slnx` 通过后勾选。

### 第二阶段任务记录

| 任务 | 状态 | 构建 | 备注 |
| --- | --- | --- | --- |
| v0.2 计划 | 规划中 | 未涉及 | 仅记录计划，未修改组件代码 |
| Textarea | 未开始 | - | - |
| Radio + RadioGroup | 未开始 | - | - |
| Progress | 未开始 | - | - |
| Tooltip | 未开始 | - | 依赖 PopupHost |
| PopupHost + Popover | 未开始 | - | 需先确认挂载层设计 |
| Tabs / Drawer / Empty / Divider | 未开始 | - | 可延后 |
