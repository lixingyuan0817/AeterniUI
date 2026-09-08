# AeterniUI 组件路线图

版本基线：`0.1`

状态：实施中

本文档记录当前阶段的组件任务、实现边界和验收规则，并随组件交付同步更新状态。

## TODO 总览

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

## 1. FormField + Label

目标：提供表单字段的统一标签、帮助文本、错误文本和控件插槽。

计划 API：

- 公开组件命名为 `FormField`，不额外创建仅转发的公共 `Field`；`Field` 作为概念名称保留在文档中。
- `Label`、`Description`、`Error`、`ChildContent`、`Required`、`Disabled`、`Invalid`。
- 使用 `For`/`FieldIdentifier` 时自动读取 `EditContext` 验证消息；显式 `Error` 优先级高于验证消息。
- 生成稳定的 label、description、error id，并通过 `for`、`aria-describedby`、`aria-invalid` 与子控件关联。
- 支持自定义 `Class`、`Style` 和 `AdditionalAttributes`，根元素遵循 `AeterniComponent` 属性合并规则。

验收：标签和消息在窄屏不溢出；禁用/无效状态一致；`Input` 可以在 `FormField` 中直接使用；示例包含成功、帮助文本和校验错误状态。

## 2. Checkbox

目标：提供原生复选框语义和稳定的布尔值绑定。

计划 API：

- `Value`、`ValueChanged`、`ValueExpression`，支持 `@bind-Value`。
- `Indeterminate` 仅作为显示状态；点击后进入明确的 `true/false` 值。
- `Label`/`ChildContent`、`Required`、`Invalid`、`Disabled`、`AriaLabel`。
- 支持 `OnChange`、键盘操作和 `EditContext` 验证状态。
- 优先使用原生 `<input type="checkbox">`；只有设置 indeterminate 属性确有必要时才添加最小 JS isolation。

验收：鼠标和键盘均可操作；原生复选框可被辅助技术识别；三态初始显示正确，点击后不保留不确定状态；禁用状态不触发值回调。

## 3. Switch

目标：提供适合即时开关设置的二态控件，不把 Checkbox 的视觉样式直接复制成 Switch。

计划 API：

- `Value`、`ValueChanged`、`ValueExpression`、`Disabled`、`Required`、`Invalid`。
- `Label`/`ChildContent`、`AriaLabel`、`OnChange`。
- 内部使用可聚焦的原生 checkbox，并输出 `role="switch"`、`aria-checked`；不使用容器伪造原生禁用。
- 使用现有尺寸、焦点、语意色和 reduced-motion Token。

验收：Space/Enter 或原生 checkbox 行为可以切换；`aria-checked` 与值同步；禁用时既不可切换也不可触发事件；窄屏布局不跳动。

## 4. Tag

目标：提供用于分类、状态和筛选条件的紧凑标签。

计划 API：

- `ChildContent`、`Color`、`Size`、`Variant`。
- `Dismissible`、`OnDismiss`、`AriaLabel`，可选 `StartIcon`/`EndIcon`。
- 非可关闭 Tag 保持纯展示语义；可关闭 Tag 的关闭按钮使用 `Button` 的图标能力，不创建 `IconButton`。
- 关闭按钮必须有可访问名称，且不会触发 Tag 外层的其他交互。

验收：文本、图标和关闭按钮在所有尺寸下不重叠；关闭事件只触发一次；语意色来自现有 `Color` Token；支持 reduced-motion。

## 5. List + ListItem

目标：提供可组合的选项列表，先覆盖静态列表和单/多选，不承担虚拟化和远程数据加载。

计划 API：

- `List` 提供 `SelectionMode`（无选择、单选、多选）、`SelectedValue`/`SelectedValues`、对应变更事件和 `AriaLabel`。
- `ListItem` 提供 `Value`、`ChildContent`、`Disabled`、`Selected`、`LeadingContent`/`TrailingContent`。
- 通过级联上下文传递列表选择状态，避免每个 `ListItem` 重复实现选择逻辑。
- 使用 `role="listbox"`/`role="option"` 时同步 `aria-selected`；无选择模式使用普通列表语义。
- 第一版不加入拖拽、虚拟化、分组和异步数据源。

验收：鼠标、方向键和 Enter/Space 选择行为一致；禁用项不可选择；单选和多选绑定不会互相覆盖；列表高度和长文本布局稳定。

## 6. Rating

目标：提供紧凑的整数评分输入和只读展示。

计划 API：

- `Value`、`ValueChanged`、`ValueExpression`、`Max`、`ReadOnly`、`Disabled`。
- `AllowClear`、`AriaLabel`、`OnChange`，支持键盘方向键和 Home/End。
- 第一版只支持整数评分，不实现半星；内部按 `radiogroup`/`radio` 语义表达选项。
- 图标通过 `RenderFragment` 或现有 `Icon` 组合，不绑定具体图标供应商。

验收：评分范围和 `Max` 参数校验明确；键盘可逐级调整；只读状态不改变值；辅助技术可读出当前值和最大值；空评分状态可配置清除。

## 7. ComboBox

目标：提供可输入筛选、键盘导航和选项选择的组合框。

计划 API：

- 泛型组件 `ComboBox<TItem>`，提供 `Items`、`Value`、`ValueChanged`、`ValueExpression`。
- 使用 `TextSelector` 将 `TItem` 映射为展示文本；允许自定义 `ItemTemplate` 和空状态内容。
- `Placeholder`、`Disabled`、`ReadOnly`、`Required`、`Invalid`、`Clearable`、`AriaLabel`。
- 支持输入筛选、上下方向键、Enter、Escape、Home/End、点击外部关闭和空状态。
- 使用 `Input` 作为输入语义，使用 `List`/`ListItem` 作为选项语义；只在点击外部或定位确实需要时添加单一 JS module。
- 第一版不包含远程搜索、虚拟化、无限滚动和多选；多选应另立任务。

验收：`role="combobox"`、`aria-expanded`、`aria-controls`、`aria-activedescendant` 正确同步；筛选结果可由键盘访问；选择、清除和失焦提交事件顺序稳定；长列表和窄屏不会溢出。

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
| List + ListItem | 已完成 | 通过 | `listbox`/`option` 语义、单选/多选、方向键与空格回车选择；示例与文档已同步 |
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
