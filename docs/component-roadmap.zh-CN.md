# AeterniUI 组件路线图

版本基线：`0.1`

状态：实施中

本文档记录当前阶段的组件任务、实现边界和验收规则，并随组件交付同步更新状态。

## TODO 总览

- [x] 记录组件路线图和验收规则
- [x] 1. `FormField` + `Label`
- [ ] 2. `Checkbox`
- [ ] 3. `Switch`
- [ ] 4. `Tag`
- [ ] 5. `List` + `ListItem`
- [ ] 6. `Rating`
- [ ] 7. `ComboBox`

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
| FormField + Label | 已完成 | 未验证 | 当前环境的 NuGet fallback package folder 配置阻塞构建 |
| Checkbox | 未开始 | - | 依赖 FormField |
| Switch | 未开始 | - | 依赖 FormField |
| Tag | 未开始 | - | 可在 FormField 后独立实现 |
| List + ListItem | 未开始 | - | ComboBox 的选项基础 |
| Rating | 未开始 | - | 依赖基础交互和无障碍约定 |
| ComboBox | 未开始 | - | 依赖 Input、List + ListItem |
