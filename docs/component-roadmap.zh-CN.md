# AeterniUI 组件路线图

文档版本：`10.15.0`

状态：v0.1～v0.5 与第六轮质量收口已交付；v0.6 Toolbar、ToggleGroup、SplitButton、Breadcrumb 已交付（4/5），Stepper 未开始；v0.7 规划中。

## 10.15.0 Breadcrumb 功能发布

- [x] 新增 `Breadcrumb` / `BreadcrumbItem`，交付原生链接、当前页 ARIA 语义、禁用/隐藏过滤和可替换分隔符。
- [x] 新增 `/components/breadcrumb` 可操作示例、组件渲染契约和导航模块索引；不内置路由、自动折叠或异步导航。
- [x] 版本属性统一为 10.15.0；同步当前功能、设计规范、项目索引、README 发布 tag 示例，并通过构建、文档、CSS 检查。

本文档记录当前阶段的组件任务、实现边界和验收规则，并随组件交付同步更新状态。

## 宿主范围收敛（未发布）

- [x] 组件库仅面向 Blazor Server 与 Blazor WebAssembly；删除桌面示例工程、窗口背景与启动恢复脚本及专用样式。
- [x] 主题模块仅使用浏览器 API，移除原生窗口主题同步及专用 JS 互操作参数；保留系统主题监听、明暗/品牌切换与本地持久化。
- [x] 更新包描述与标签、示例介绍、中文文档及开发门禁；保留浏览器交互示例和 GitHub Pages 静态发布流程。
- [x] 验证：解决方案构建 0 警告、0 错误；主题 JS 语法、30 项现有 Node 回归与文档一致性检查通过；已删除宿主的源码、配置和文档引用无残留。
- 本批收敛宿主支持范围，C# 组件参数与事件不变，包版本暂保持当前未发布批次。

## 10.14.3 全组件质量修复

- [x] P1：统一 `Visible=false` 隐藏契约；修复 ComboBox/Rating 键盘默认滚动与焦点；修复 Drawer/Popover `CloseOnEscape=false`；补齐 ComboBox、Radio/RadioGroup 的 FormField 状态继承。
- [x] P2：允许 ComboBox 展示 EmptyContent；修复 DateCalendar 在 `DateOnly.MinValue/MaxValue` 边界溢出；Tooltip 每次参数更新重新绑定；Tabs 动态注销与隐藏过滤；Accordion 禁用项不再保持展开布局；修正 Popup 示例参数。
- [x] P3：Drawer/Popover 增加退出过渡并等待实际 CSS 动画，适配 reduced-motion、快速重开和释放；不改变 Dialog/Notice。TimePicker 映射按 Min/Max 限定候选范围，并按实例复用无自定义委托、参数未变的映射。
- 构建与静态门禁：Debug / Release 解决方案构建均 0 警告/0 错误，15 组组件契约、30 项 Node 回归、组件/共享浮层 JS 语法、57 个 CSS 文件注释、99 项品牌对比度与文档一致性检查通过。
- 验证：15 组组件契约覆盖字段状态、隐藏/动态标签、日期极值、受控关闭与陈旧退场完成、时间映射复用和完整枚举等价性；Node 行为回归已接入 CI。独立无头 Edge 的 27 项重点交互（含窄屏深色模态关闭策略/快速重开）及 41 页 × 1440px 浅色/390px 深色共 82 个基础场景通过，未发现页面异常或横向溢出。浏览器验证退场资源释放与 reduced-motion；真实读屏和触控设备验收仍不在本次自动化覆盖范围。
- 发布范围包含下述 10.14.2 List 修复批次；统一使用 `v10.14.3` 触发现有 NuGet Trusted Publishing 工作流，发布核心包与 Font Awesome 图标包，不单独发布 10.14.2，也不另建 GitHub Release。

## 10.14.2 List 键盘修复（合入 10.14.3）

- [x] AllowClear 仅允许主动激活清空；重复 Home/End、单项循环导航不再清空选择。
- [x] 新增单一 List JS module，按当前可见 DOM 顺序导航，覆盖 keyed 重排；隐藏或禁用项不参与选择，隐藏活动项时清理引用。补齐 List/ListItem 的 hidden 隔离样式。
- [x] 只拦截容器自身已处理的导航/选择键，阻止默认页面滚动；保留 Tab、组合快捷键、输入法和后代控件行为，串行处理快速按键并释放监听。
- [x] 补充 C# 渲染契约、Node 键盘回归及可操作的重排/隐藏示例；其他组件仅进行审核，不在本批修复。
- 验证：Debug 解决方案构建 0 警告/0 错误、11 组组件契约、2 项 List Node 回归、JS 语法、57 个 CSS 文件注释及文档门禁通过；独立无头 Edge 验证重复 Home 不清空、Space 不滚页、keyed 重排后 Home 按画面次序选择，以及隐藏活动项后清理活动引用并跳过导航。本批次合入 10.14.3 统一发布。

## 10.14.1 List 选中样式修复

- [x] 普通行与 CardMode 共用选中、悬停与活动项样式；Card 内层透明并继承文字色，移除卡片模式外围留白及独有活动项描边，选中底色覆盖整个卡片且禁用文字保持一致。
- [x] 示例页增加保留选择的 CardMode 切换，覆盖声明式单选/多选与 Items 数据模式；不更改选择和键盘业务逻辑。
- 验证：Debug / Release 解决方案构建均为 0 警告/0 错误、11 组渲染契约、57 个 CSS 文件注释检查与文档一致性检查通过；两个 10.14.1 NuGet 包完成本地打包验证。浏览器视觉复核尚未执行。
- 使用 `v10.14.1` 触发现有 NuGet Trusted Publishing 工作流，发布核心包与 Font Awesome 图标包；不另建 GitHub Release。

## 10.14.0 List 泛型功能发布

- [x] List<TItem> / ListItem<TItem>、Items 自动推断、ItemTemplate、CardMode/CardTemplate；Card 外壳统一渲染，无模板只回退文本。
- [x] 声明式显式 TItem，保留 object 选择 API；修正集合名称冲突、动态注册 Value、禁用继承、活动项和移除数据的选择清理。
- [x] 交互字符串/模型/卡片/动态示例与泛型迁移说明；`Directory.Build.props` 四个版本属性统一为 10.14.0（程序集/文件版本为 10.14.0.0），同步中文文档、索引和 README，保留 10.13.0 历史发布记录。
- [x] 发布前验证：Debug / Release solution build 均为 0 警告、0 错误；11 组契约覆盖模板、Card 回退、声明式、键盘、动态数据和禁用；21 项 Node 回归、11 个组件 JS 语法、57 个 CSS 注释检查、99 项品牌对比度、343 个图标一致性、Token-only CSS 与文档门禁通过；两个 10.14.0 NuGet 包完成本地打包验证。
- 使用 `v10.14.0` 触发现有 NuGet Trusted Publishing 工作流，发布核心包与 Font Awesome 图标包；不另建 GitHub Release。
- 尚无浏览器视觉/辅助技术实机验收；本次发布不扩大上述验收边界。

## 10.13.0 功能发布

- [x] 将已实现的 ToggleGroup / ToggleGroupItem 与 SplitButton 纳入本次功能发布；v0.6 当时累计完成 3/5，Breadcrumb 在后续 10.15.0 批次交付。
- [x] 纳入字段尺寸一致性收口、ToggleGroup 连体分段外观、SplitButton 紧凑默认外观、Toolbar 间距与 MenuButton 专属菜单留白优化，以及菜单 ID 关联和非模态关闭焦点回归修复。
- [x] `Directory.Build.props` 四个版本属性统一为 10.13.0（程序集/文件版本为 10.13.0.0），同步功能清单、设计规范、路线图、审阅待办、项目索引与 README 发布 tag 示例；使用 `v10.13.0` 触发现有 NuGet Trusted Publishing 工作流，发布核心包与 Font Awesome 图标包。
- [x] 发布前验证：Debug / Release solution build 均为 0 警告、0 错误；10 组组件契约、21 项 Node 回归、11 个组件 JS 语法、57 个 CSS 注释检查、99 项品牌对比度、343 个图标一致性、Token-only CSS 与文档门禁通过；两个 10.13.0 NuGet 包均完成本地打包验证。
- 本次只整理已实现能力并更新发布元数据，不改动 List 功能或扩大组件实现边界；已有人工浏览器验收限制仍保留。

## 10.12.2 修复与优化

- [x] TimePicker / DateTimePicker 共享滚筒文字层：连续透视、缩放与渐淡，真实行几何不变；首次直接定位，后续目标以现有动效 Token 平滑居中。
- [x] 停稳吸附完成才提交草稿，输入中断、快速重选、同值回声、外部 Value、联动、reduced-motion 与释放回归；保留紧凑密度与键盘模型。
- [x] 示例新增外部具体时分秒/延迟设置；版本统一为 10.12.2。node:test 与组件契约门禁覆盖，真实浏览器视觉和触控惯性验收仍待人工复核。

## 字段尺寸一致性收口（纳入 10.13.0）

- [x] DatePicker / DateRangePicker / TimePicker / DateTimePicker：三档横向 padding 统一为 `control-padding-x-sm/md/lg`（8/12/16px），字号对齐 Input / ComboBox（12/14/16px），保留 normal 行高、宽度 / FullWidth 和弹层布局；渲染契约检查覆盖三档触发器与样式 Token。
- [x] Segmented：仅将选项等值的 spacing-2/3/4 替换为 control-padding-x-sm/md/lg，不改轨道与滑块几何。
- Pagination 不作未经确认的设计变更。

## 10.12.1 修复与优化

- 已实施全库温和紧凑化：共享控件 28/36/44px、内联 padding 8/12/16px、紧凑行 32px，Surface/Card 与弹层留白同步收紧；正文、颜色、spacing 原始阶梯与焦点环不变，交互目标至少 24px。规则见设计规范与当前功能清单。
- 保留 10.12.0 Toolbar 与交互工作区；v0.6 仍只完成 Toolbar，其余四组件未实施。
- 验证：solution build 0 警告/0 错误，7 组渲染契约、10 项 Node 回归（含 20/28/32/36/44px 时间轮测量）、55 个 CSS 注释检查、99 项对比度与文档门禁通过。当前工作区无可用浏览器自动化依赖；已安装的 Playwright CLI 也未找到可运行的 Playwright 项目，未完成浏览器视觉验收。

## 文档边界

- `docs/current-features.zh-CN.md` 是当前已实现公共 API 和行为的唯一功能事实源；组件参数和行为以源码为准。
- 本文档中的 v0.1 组件小节只保留目标、验收重点、任务状态和实现差异，不重复当前 API。
- 当前 API 与行为以 `current-features.zh-CN.md` 和源码为准；历史计划只有在解释实现差异时才保留。
- 尚未完成的阶段是 TODO/规划，不得在功能文档或项目索引中写成已实现能力。

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
| ComboBox | 已完成 | 通过 | 交付为纯下拉选择（点击弹出、无输入搜索，方向键/Enter/Esc/外部点击/滚动关闭）；弹层已迁移到共享 PopupHost + Popover，原计划中的输入筛选不适用 |

## 第二阶段（v0.2）计划

状态：已完成。版本首位与目标 .NET 主版本对齐（当前固定为 `10`）；功能新增递增第二位（例如 `10.0.0` → `10.1.0`），问题修复与优化递增第三位（例如 `10.0.0` → `10.0.1`）。v0.1 已覆盖表单、选择与反馈基础组件，v0.2 补齐了高频基础控件与浮层能力。

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
          +--> ComboBox 弹层迁移（已替换内置 fixed 定位）
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

暂不包含：多浮层堆叠、嵌套模态和脱离宿主布局上下文的视口级 fixed 定位。

基础验收重点：多实例挂载、Open/hidden 状态、Popover 的模态/非模态语义。

后续影响：Tooltip、MenuButton、DatePicker 和 ComboBox 统一复用共享浮层能力；ComboBox 额外启用页面滚动关闭。

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
| v0.2 计划 | 已完成 | 通过 | 高频基础控件与浮层能力已交付 |
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
| Size 档位补齐（Switch / ComboBox / Rating） | 已完成 | 通过 | 三个控件补 `Size`（`Small`/`Default`/`Large`）：ComboBox 触发器与选项共用一套档位 Token（Small 28px，与 `Input Size="Small"` 等高）；Switch 轨道由旋钮尺寸推导（跑道 = 旋钮×2 + 内边距×2 + 边框×2，滑块行程恰好一个旋钮宽，默认档与旧值一致，Small 34×20 / Large 46×26）；Rating Small 16px 星形、热区至少 24px、Large 24px + 12px |
| 示例页拆分（v10.5） | 已完成 | 通过 | 把原来的单页 + `switch` 拆成「一个组件一个页面」：`Pages/Components/` 下 25 个页面各自带 `@page`/`@layout`/`PageTitle` 与自己的 `@code` 状态，路由 `/components/{id}` 可直接分享；新增 `Layout/ComponentsLayout.razor`（侧栏 + 路由高亮，内嵌 MainLayout）与 `Components/Showcase/ShowcasePageBase.cs`（仅保留跨页共享的注入服务、交互提示与主题订阅）；作用域 CSS 无法跨页，展示样式迁到示例全局表 `wwwroot/css/showcase.css`；安装页改为垂直单列步骤（每步独占整行代码块）；规范 §10 与项目索引同步为「一个组件一个页面」 |
| 表面归属定案（v10.5） | 已完成 | 通过 | 把「组件是否自带表面」写成规范 §5.3 两行表格（自带表面 vs 内容控件，判断依据是“能不能单独构成一块界面”）并进提交清单；`current-features` 的 Menu/Tabs 实现边界各补一条；示例预览把 Menu/Tabs 放进 `Surface` 宿主容器（原来它们在网格画布上裸奔，看起来像漏了背景）。结论是 Menu/Tabs 按内容控件保持透明，`List` 保持自带表面 |
| 共享浮层能力（v10.4） | 已完成 | 通过 | 新增 `wwwroot/js/aeterni_floating.js`：视口贴合计算（`fitsSide`/`oppositeSide`/`clampCenteredShift`/`clampAlignedShift`）、`createFocusTrap`、引用计数的 `lockScroll`；Tooltip 改为复用，DialogProvider 改用共享滚动锁与可聚焦元素列表（Tab/Escape 仍由对话框堆栈自己持有，因为只有最顶层应响应） |
| 多品牌色相层与切换 | 已完成 | 通过 | ① 交互态着色不再写字面量紫色，改由 `color-mix()` 从色阶推导；② 品牌色阶从裸 `:root` 拆成独立可覆盖的色相层（`:root, [data-aeterni-brand="purple"]`）；③ 新增第二套板 `[data-aeterni-brand="green"]`（500 档 `#2B7B33`）。两套板在浅色／深色／系统深色下逐条通过对比度门禁（绿色最紧 margin 1.151，紫罗兰 1.091，均达标；绿色锚点随后在「绿色板提亮重锚」一行重定），512 个 Token × 4 场景中紫罗兰与改造前**数值等价**（最大通道误差 0.0001，为 float32 量化），绿色场景 **0 处残留紫色**且语义停靠档自动跟随（浅色 500/600/700、深色 400/300/200）。配方侧新增两条硬规则进规范 §5.4：chroma 剖面必须用绝对值（相对色域比例跨色相不可迁移）、500 档明度按该色相的对比度行为枚举扫描定档。④ 切换链路打通：新建 `ThemeBrand` 枚举（不复用 Button 的 `Color` 语意色枚举，避免与语意色取值空间混淆）；`ThemeService` 增加 `Brand` 与 `SetBrand()`（`Enum.IsDefined` 校验，非法值抛 `ArgumentOutOfRangeException`，同值不触发）以及 `SetPurple()`／`SetGreen()`，并新增独立的 `BrandChanged` 事件——品牌与明暗正交，复用 `ThemeChanged` 会连带重新解析系统偏好；`AeterniUIOptions.DefaultBrand` 决定首访默认品牌并在 `AddAeterniUI` 里做枚举校验；`ThemeProvider` 首渲染恢复已存品牌、事件里应用并持久化到新键 `aeterni.theme.brand`，JS 侧新增 `applyBrand()`／`getStoredBrand()`／`persistBrand()` 并把 `applyTheme` 与 `applyBrand` 的过渡逻辑抽成共享的 `beginTransition`／`endTransition`；示例宿主 `index.html` 预渲染脚本同步读品牌键并写 `data-aeterni-brand`，示例 `/components/theme` 增加品牌切换控件。浏览器端到端 22 项断言全部通过：冷启动默认紫罗兰、经真实控件切到绿色后属性落在 `<html>`、语义别名整体换色（含深色停靠档）、写入 `localStorage`、**刷新首帧即为绿色**（属性变更轨迹 `[null,"green"]`，全程未出现 `purple`）、明暗切换不干扰品牌、非法存储值回落默认且色彩仍可解析、清空存储回到默认、切换复用 `aeterni-theme-transitioning` 过渡并在 620ms 后清理。⑤ 示例宿主 4 处直接消费原始色阶（`.preview-icon-wrap`、`.component-preview__eyebrow`、`.component-preview__api-row code`、`.doc-intro__eyebrow`）改为语义别名 `--aeterni-color-brand-text`，原先写死 `--aeterni-brand-500/600` 既不会跟随品牌也不会跟随明暗。⑥ 新增 `ThemeBrandSwitch` 组件（与 `ThemeSwitch` 同构的 `Segmented` 专用用法，订阅 `BrandChanged` 同步选中态）与对应文案 `ThemeBrandSwitchLabel`／`ThemeBrandPurpleLabel`／`ThemeBrandGreenLabel`，并放进示例宿主顶栏与明暗开关并列，任意页面都能直接看到整站换色。**待做**：品牌色块预览型选项（会要求每个色相层再声明一份「别人的颜色」Token，等于把第二套色阶写进 CSS，暂不做） |
| 品牌色板对比度门禁脚本化 | 已完成 | 通过 | 新增 `scripts/check-contrast.mjs`，把此前「靠人工校对」的对比度门禁变成构建期拦截（此前该项挂在多品牌行的待做里）。脚本不读注释而是重放真实级联：自带极简 CSS 扫描器切出规则（含 `@media (prefers-color-scheme: dark)` 的系统深色块，其选择器是 `:root:not([data-theme])…`，需要单独识别才能覆盖），按「品牌 × 浅色／深色／系统深色」三种 `<html>` 属性状态合并同特异性声明（文件顺序即优先级），再跟随 `var()` 链解析出实际颜色（`#hex`／`rgb()`／`rgba()`，遇无法解析的值或环状引用直接报错而不是静默跳过），半透明前景先与背景合成再测——因此 78% 墨色这类 Token 也按用户真实看到的像素计算。门禁集合 11 组／状态：实心填充承字（default／hover／active）、品牌文字（surface／tertiary／page）、链接（surface／page）按 4.5:1，边框／图标与焦点环按 3:1，当前 **2 色相层 × 3 状态 × 11 组 = 66 项全部通过**。另加两条结构性检查：色相层必须声明完整且仅为 50~900 十档；**余量低于 1.1× 的色相层必须在自己上方的注释块里写出该对比度**（`4.91:1` 这样的定值字符串），把规范 §5.4 新加的「显式记账」规则变成可执行约束——否则改色阶的人可以悄悄把预算花光。为此紫罗兰注释补记 4.91:1（margin 1.091，原本未记录）。已做反向验证：把绿色 500 提到 `#35A052`（3.33:1）与 `#228A40`（4.40:1）时脚本以 2 条错误失败，只提到 `#1C863B`（4.64:1，过线但花掉余量）时仅记账规则失败，把色相层改名成 `teal` 时无需改脚本即自动纳入检查。已接入 `.github/workflows/build.yml`，并同步 AGENTS.md、agent-development、project-index 与规范 §5.4 |
| 绿色板提亮重锚 | 已完成 | 通过 | 绿色色阶整体重锚到外部参考绿 `#1F883D`：色相 145° → 148.2°，500 档 L 0.517 → 0.552、C 0.132 → 0.145，整条阶梯按同一 ΔL 节奏平移 +0.035，chroma 仍按绝对值剖面（50:0.081 … 900:0.522）缩放，浅端升到 L 0.957、深端升到 L 0.301，比旧板更贴近规范里「浅端 ~0.976 / 深端 0.30」的统一端点。观感上浅色主题的填充、hover、链接、选中态都明显更亮更饱和，深色主题 300/400 停靠档亮度同步上抬。**代价记账**：500 承白字 5.27:1 → 4.52:1，余量 1.004×，成为全库最紧的一条对比度门禁——仍满足 4.5:1，但任何进一步提亮或提高 chroma 都会击穿它；若要恢复余量，把 500 退回 L 0.545（`#1C863B`，4.65:1）或 L 0.540（`#19843A`，4.75:1）即可，其余档位随 ΔL 平移。**验证**：重锚后按「品牌（紫／绿）× 浅色／深色」重跑 8 条对比度门禁共 32 项全部通过，深色侧最紧为绿色 `text-link` 承深色页面 5.95:1（余量 1.323，比旧板更宽松——深色停靠档整体上抬反而放宽了明度约束），浅色侧最紧仍是绿色 500 承白字 4.52:1；浏览器端到端 11 项断言按新色值更新后全通过（浅色 `default`/`hover` = `rgb(31, 136, 61)` / `rgb(5, 113, 44)`，深色 `text` = `rgb(144, 195, 151)`），并逐像素核验渲染结果与 Token 一致（示例页 4 处品牌实心填充采样主色 `#1F883D` 占 81.7%）。规范 §5.4 新增「余量低于 1.1× 的锚点必须显式记账」一条。 |

| 焦橙第三套品牌色相层 | 已完成 | 通过 | 在紫色与绿色之后新增 `[data-aeterni-brand="orange"]`，配方与前两套逐档一致（同一组相对 ΔL、同一 chroma 剖面、同一浅端色相漂移），500 档锚定焦橙 `#C2410C`（L 0.5534、C 0.1739、色相 38.40°）。**为什么不是鲜橙**：`#E8590C` / `#EA580C` 这类亮橙承白字只有 3.58:1 / 3.56:1，低于 4.5:1，要做实心填充就得改成 `warning` 那种近黑前景，第三套板会脱离「白字填充」这条家族约定；焦橙承白字 5.18:1（余量 1.151×），三套板同一停靠档可直接互换。**代价记账**：与语意 `danger` 红的 OKLab ΔE 只有 12.1，是全库最紧的一对品牌／语意间距（`warning` 22.4，`success` 34.8，`info` 37.3）；橙红在近明度下本就是邻居，正常色觉下仍清晰可分，但后续任何把 500 档往暖端推的改动都会继续压缩这段距离。**接线**：`ThemeBrand` 新增 `Orange`、`ThemeService` 新增 `SetOrange()`、`ThemeProvider` 的存储恢复 switch 新增 `"orange"`、文案表新增 `ThemeBrandOrangeLabel`、`ThemeBrandSwitch` 的 `Brands` 数组由两项扩到三项（指示块行程由选项数量推导，组件无需其他改动）、示例 `/components/theme` 的 `BrandText` 同步。JS 与预渲染脚本无需改动：`applyBrand()` 原样透传属性值，未识别值匹配不到任何色相层而回落 `:root` 的紫罗兰。**验证**：`scripts/check-contrast.mjs` 自动发现新色相层，从「2 色相层 × 3 状态 × 11 组」扩到 **3 色相层 × 3 状态 × 11 组 = 99 项全部通过**，最紧一条仍是绿色 500 承白字 4.52:1（margin 1.004）；`dotnet build aeterni_ui.slnx` 0 警告 0 错误。 |

| 交互态品牌色阶复原与全组件状态色审计 | 已完成 | 通过 | ① **复原**：`--aeterni-state-background-hover/-active/-selected`、`--aeterni-state-border-hover/-active`、`--aeterni-state-color-*` 与 `--aeterni-button-intent-default-soft` 全部回到从 `--aeterni-brand-*` 推导（`color-mix()` 取档），撤销上一版「选择态中性化」——中性墨让表单控件与选中态读起来发死，品牌按钮的 soft 变体还会退成灰色；容器与页面背景仍严格保持无彩色。② **新增两档**：`--aeterni-state-background-checked-hover` / `-active`（浅色 500→600→700、深色 400→300→200），填满的勾选块在指针下整块沿色阶换档，墨仍用 `--aeterni-text-inverse`。③ **审计并修复 8 类**：Checkbox/Radio/Switch 的 checked hover 曾出现「边框换档、填充不动」与「填充换档、圆点/圆环不动」两种劈裂；三者的无效态悬浮会被 `--aeterni-state-border-hover` 覆写而丢掉危险色，Switch 的无效 + 选中还被拆成危险边框裹品牌填充；Pagination/Tabs/Menu/ListItem/ComboBox/DateCalendar 的 `:hover`（0,2,0）与 `.is-selected`／`.is-current`（同为 0,2,0）特异性相同，写在后面就把选中提示整块压掉；`IconButton` 生成的是 `aeterni-button--*` 而不是 CSS 里定义的 `aeterni-icon-button--*`，四种非 default intent 的形状与状态样式从未命中，且缺 `--aeterni-button-color-active`；`DatePicker.razor.css` 复制了一整份日历规则（101 → 42 行，删掉的 61 行与 `DateCalendar.razor.css` 逐条重复且已是后者子集）。④ **验证**：`dotnet build aeterni_ui.slnx` 0 警告 0 错误、`check-contrast.mjs` 99 项通过（最紧仍是绿色 500 承白字 4.52:1）、`check-css-comments.mjs` 48 文件通过、`check-docs.sh` 通过；用 headless Chrome（CDP `forcePseudoState` + 真鼠标 `mouseMoved`，读 computed style）在「紫/绿/橙 × 浅色/深色」下逐控件复验修复前后的每一档色值；48 张截图逐像素分色相统计显示每个品牌的功能页**跨品牌彩色像素 0.00%**（唯一例外是展示全部语义色的 `/components/colors`），彩色像素占比 0.8~2.2%，即三套色相层完全隔离且状态反馈仍在。**记账（未修）**：四个 `PopupHost` 消费者在自己 `.razor.css` 里写的 `__host` 规则（DatePicker 的 `inline-block`/`min-width`、DateRangePicker、MenuButton）因隔离属性不落在子组件根元素上而从不生效，实测 host 恒为 `display:block`；这些 host 都是 flex/grid 项会被 blockify，无视觉差异，本次只记录不动手，规则已写入 AGENTS.md |
| v10.8.0 发布（多品牌色相层） | 已完成 | 通过 | 版本号在 `Directory.Build.props` 单一来源递增 `10.7.1` → `10.8.0`（`Version`／`AssemblyVersion`／`FileVersion`／`InformationalVersion` 四处齐动），四份门禁文档的「文档版本」与 `project-index` 的「当前版本」同步，`component-review-todo` 一并跟进，README 的发布示例改为 `v10.8.0`；`bash scripts/check-docs.sh`（版本格式 `^10\.[0-9]+\.[0-9]+ + 四文档一致性）与 `dotnet build aeterni_ui.slnx` 通过；在 main 上打注解 tag `v10.8.0` 触发 `Publish NuGet packages`，工作流先由「Validate release tag」比对 tag 与 props 版本，再用 NuGet Trusted Publishing（OIDC）发布 `AeterniUI` 与 `AeterniUI.Icons.FontAwesome`。按仓库约定「功能新增递增次版本号」，本批（多品牌色相层与全组件状态色审计）为 minor
| 示例页与首屏加载器重做 | 已完成 | 通过 | ① 「色彩与语义」页（`/components/colors`）从「摆组件」改为不引用任何组件的 Token 展台：色族色卡（每族三档色阶）、文字墨阶、字体与字号、中性容器表面、圆角阶梯、阴影层级、间距节奏与对比度验收组合；颜色全部消费 `--aeterni-*`，样式里零 hex/rgb 字面量，因此换主题与换品牌色相层都不需要改这一页。② 顺带修掉一个静态缺陷：`.component-preview__eyebrow` 的规则写在 `ComponentPreview.razor.css` 的作用域里（选择器带 `b-*` 属性），其它页面引用该类名永远不会命中，改用 `showcase.css` 的全局 `.showcase-page-eyebrow`，`Install` 页同步修正。③ 首屏加载器删掉 MVC 样板留下的灰色/蓝色 SVG 进度环与 `100%` 文案（`stroke:#e0e0e0` / `#1b6ec2` 是示例静态表里最后一处硬编码颜色），改为复刻 `Spinner` 规格的 `.sample-boot` 光环：走组件公开的 `--aeterni-spinner-size` 扩展点、3px 描边、右侧缺口、`--aeterni-duration-slower × 1.4` 线性周期、`opacity .92`，颜色取 `--aeterni-color-brand-text`，文案继续读运行时的 `--blazor-load-percentage-text`；`prefers-reduced-motion` 下由 Token 层清零时长并显式停转。④ 验证：`dotnet build aeterni_ui.slnx` 0 警告 0 错误；`check-docs.sh`、`check-css-comments.mjs`（48 文件）、`check-contrast.mjs`（99 项，最紧仍为绿色 500 承白字 4.52:1）全部通过；headless Chrome 在限速首屏下读到光环 48px / 3px 描边 / 右侧缺口透明 / 动画运行，浅色 `rgb(102,70,187)` 与深色 `rgb(183,173,247)` 自动换档，`prefers-reduced-motion: reduce` 下 `animation: none`，挂载后加载器标记消失，684px 窄屏无横向溢出。**取舍**：加载器不可能真的实例化 `Spinner`（首屏没有组件树，组件的隔离样式也尚未加载），是刻意的视觉复刻，`app.css` 的注释写明了双向同步要求；色族按「每族一张卡、卡内三档」而不是参考图的「每档一张卡」排布 |
| 玻璃配方落地（三轴解耦） | 已完成 | 通过 | 起因：iOS 27 桌面的背景模糊观感评估（只要模糊，不要液态玻璃的折射／高光边框）。分析结论是模糊半径本身够用（`--aeterni-blur-lg` 的 24px），短板在「填充 82% 把糊过的背景又盖回去」与「缺少可采样的背景内容」。落地方案拆成三根互不耦合的轴：**①填充**——Token 层新增三个玻璃角色别名 `--aeterni-bg-glass`（= `--aeterni-bg-elevated`，浅深同为 `.82`）、`--aeterni-blur-glass`（= `blur-md` 16px/135%）、`--aeterni-blur-scrim`（= `blur-sm` 8px/120%），填充刻意停在 `.82` 而不是提案里的 `.60`：更淡会把标准次要墨拽到 4.5:1 以下，等于逼全库说明文字换墨。**随后补上决策 A**：`--aeterni-text-on-glass-secondary`（`color-mix(in srgb, var(--aeterni-text) 93%, transparent)`，只声明一次所以浅深两主题同余量）接在 `Card`／`Surface` 的磨砂态上，补掉深色「次要 · 标准墨」在 `.82` 上只有 4.35:1 的缺口，实测升到 7.52:1（浅）/ 7.01:1（深）；接管的是次要墨的四个角色名（`--aeterni-text-secondary`／`--aeterni-text-muted`／`--aeterni-state-color-readonly`／`--aeterni-state-color-muted`），因为别名在主题块里就完成了 `var()` 替换，只改基名会留下用 `--aeterni-text-muted` 写的说明文字仍在标准墨上；`--blur-none` 与不透明表面不接管，其余六个磨砂消费者（Dialog／Drawer／Popover／Tooltip／NoticeCard）当时暂未接管，已在「浮层接管玻璃墨」一行补齐。**②半径**——新增公共枚举 `SurfaceBlur`（`Default`／`None`／`Small`／`Medium`／`Large`），`Card` 与 `Surface` 各加 `Blur` 参数；`Default` 沿用既有约定不输出修饰类（宿主可用全局 CSS 调默认档），`None` 在 Glass 变体上同时退回 `--aeterni-bg-solid`——只摘掉 `backdrop-filter` 会留一块 82% 的半透明色斑，比不透明更糟。**③边框**——`.aeterni-card--glass`／`.aeterni-surface--glass` 里硬编码的 `border-color` 删除，玻璃边框改由既有的 `is-bordered` + `--aeterni-border-default` 决定，`Bordered="false"` 从此生效（此前追不上）。九处磨砂消费者（Dialog 面板与遮罩、Drawer、Popover 含遮罩、Tooltip、NoticeCard、Card／Surface 的 Glass 变体）改为引用新别名；`NoticeCard` 经 `--aeterni-notice-fill` 间接层换基（背景是 `color-mix(accent 9%, …)`，够不着 `background` 关键字），`is-no-blur` 重设该别名切实心。降级集中一处：文件尾两块 `@media (prefers-reduced-transparency: reduce)` 与 `@supports not (…)` 把 `--aeterni-bg-glass` 退 `--aeterni-bg-solid`、两支模糊退 `none`（遮罩保留压暗），因此 `Popover`／`Tooltip` 原有的组件级降级块可删——这是本轮唯一的删除。示例项目：`Surface`／`Card` 演示页加 `Blur` 控制项与 `.preview-glass-scene` 玻璃场景（背景层是与面板同级的兄弟节点，`aria-hidden`；`Surface` 的 `_surfaceBordered` 由 `true` 改 `false` 以对齐文档记载的默认值），新增只读对比页 `/components/glass`（`GlassLab` + `GlassContrast` + `GlassPalette`）。验证：`dotnet build aeterni_ui.slnx` 0 警告 0 错误；`check-docs.sh`、`check-css-comments.mjs`（49 文件）、`check-contrast.mjs`（99 项，最紧仍为绿色 500 承白字 4.52:1）、token-only grep（无 `.aeterni-*` 选择器）全部通过；headless Chrome 实测：`Blur=Large` 落到 `blur(24px) saturate(1.5)`、`Blur=None` 在 Glass 上退 `rgb(255,255,255)`、`Bordered=false` 后边框 `rgba(0,0,0,0)`、`--aeterni-bg-glass` 浅色 `rgba(255,255,255,.82)` / 深色 `rgba(26,26,29,.82)`、`prefers-reduced-transparency: reduce` 下 token 退 `#FFFFFF` / `none`（Dialog 遮罩保留 42% 压暗）。**遗留**：`check-contrast.mjs` 的 `contrast()` 忽略背景 alpha，半透明 token 当背景会假通过，未修。版本号未递增，留给发布批次统一走 minor。**后续**：②半径这条轴已被「模糊轴撤销」一行删除，理由是该轴与①填充争夺同一个 `background` |
| 模糊轴撤销（回归 Glass 变体） | 已完成 | 通过 | 起因：`Blur` 与 `Variant` 冲突——`Card.razor.css`／`Surface.razor.css` 里的 `.aeterni-*-blur-small/medium/large` 各自重写 `background`，而这三条规则声明在 `--subtle`／`--elevated` 之后，所以任何非 `Default` 档位都静默压过 `Variant`：`Variant` 参数看上去失效，两个参数争夺同一个属性，且失败是无声的（`Elevated + Blur=Large` 得到的是玻璃填充）。删除 `Enums/SurfaceBlur.cs`、`Card`／`Surface` 的 `Blur` 参数与 `BlurClass` 映射、四组 `--blur-*` 修饰类（含 `--glass.--blur-none` 的实心回退），模糊回归 `Glass` 变体本身（`--aeterni-bg-glass` + `--aeterni-blur-glass`）；ink 规则从 `.aeterni-card--glass:not(.aeterni-card--blur-none), .aeterni-card--blur-small, …` 收成 `.aeterni-card--glass`，`--blur-none` 不再存在所以排除项一并去掉。**为什么不保留轴**：实心状态已由 `Default`／`Subtle`／`Elevated` 表达，「玻璃填充 + 不模糊」等于 `Elevated`，该轴没有带来变体表达不了的能力；而 `Blur=None` 与 `Variant=Default` 是同一个结果的两条路径，保留只会让参数面继续撒谎。示例页 `Card`／`Surface` 的 `Blur` 控制项与参数表行同步删除，`GlassLab` 页四处「模糊档位交给 `SurfaceBlur`」改为「模糊固定由 Glass 变体承担」；`docs/current-features` 的「模糊与实心」小节重写并记下为什么不要再引入正交模糊轴，`component-design-guidelines` 的同项改成硬规则。验证：`dotnet build aeterni_ui.slnx` 0 警告 0 错误；四门禁通过；headless Chrome 实测 `Card` 与 `Surface` 页面四种 `Variant` 各自呈现自身填充（此前四种变体中心像素相同、只有 `Glass` 会显示，根因是示例页预览框的层叠，已在另一行修掉）。版本号未递增 |
| 浮层接管玻璃墨 | 已完成 | 通过 | 起因：磨砂配方统一之后，五类浮层（Dialog 面板、Drawer 面板、Popover 表面、Tooltip 内容、Alert／Toast 卡片）仍在用标准次要墨 `--aeterni-text-secondary`（0.56 倍），深色主题在 82% 填充上只有约 4.35:1，低于 4.5:1 文字底线——只有 `Card`／`Surface` 的 `Glass` 变体切到了 `--aeterni-text-on-glass-secondary`，于是同一块玻璃上出现两种墨。给这五处各加一段与 `Card.razor.css` 同构的四角色重设（`--aeterni-text-secondary`／`--aeterni-text-muted`／`--aeterni-state-color-readonly`／`--aeterni-state-color-muted`），四个必须一起改：别名在主题块里就完成了 `var()` 替换，只改基名会留下用 `--aeterni-text-muted` 写的说明文字仍在旧墨上。两类刻意排除：Dialog／Drawer／Popover 的**遮罩**不承字，换成玻璃墨只会把压暗加深；通知卡片的 `is-no-blur` 是实心回退，保持标准墨。填充与模糊未动，所以这是一次纯墨替换，没有新增 token、没有新档位。验证：`dotnet build aeterni_ui.slnx` 0 警告 0 错误；四门禁通过；headless Chrome 实测浅／深两主题下 Dialog 面板、Drawer 面板、Popover 表面、Tooltip 内容、Alert／Toast 卡片与 `Card`／`Surface` 的 `Glass` 变体解析出同一支 `--aeterni-text-secondary`（浅 `color(srgb 0 0 0 / 0.725765)`、深 `color(srgb 1 1 1 / 0.798706)`），`--aeterni-text-muted`／`--aeterni-state-color-readonly`／`--aeterni-state-color-muted` 三者同值；`is-no-blur` 与 Dialog／Drawer／Popover 三类遮罩仍为 `rgba(0, 0, 0, .62)`／`rgba(255, 255, 255, .56)`。版本号未递增 |
| 玻璃三轴对照矩阵（模糊／饱和度／遮罩） | 已完成 | 通过 | 起因：上一轮只读审计里，「模糊半径没有梯度」「`saturate` 让玻璃带色偏」「遮罩带 8px 模糊」三条都只是静态判断，没有实测依据。做法：把 `/components/glass` 从六节扩到八节——04「模糊半径」由空壳变成真布景（8／16／24／32px 四档），新增 05「饱和度」（100／120／135／150%）与 06「遮罩」（none／8／16px）两行，遮罩那一行用「整块视口 + 浮在中间的面板」的装置。三行一律只改一根轴：04 的填充停在库现值 `.82`、`saturate` 钉 `135%`；05 的填充同上、模糊钉 16px；06 的三格压暗值（`--aeterni-overlay`）完全相同。结论（1440×1000、DPR 2 的无头 Chromium 逐像素实测）：**①模糊半径在 `.82` 的填充上几乎不可测**——壁纸／细网格／12px 正文上从 8px 直接跳到 32px 只有约 4% 的像素变化（噪声底约 2%，全部落在文字抗锯齿），只有「App 图标墙」分得出，面板内部梯度 0.249／0.064／0.061／0.057，即 8→16 一次降掉四分之三、16 以上只剩 5% 与 7%，所以上轮审计「给大面板补 24px」降级为可拍板的取舍而非缺口，`--aeterni-blur-lg` 继续 0 处使用；**②`saturate` 是可测的真轴**——壁纸上面板区域彩度均值 5.46／6.50／7.26／8.06，135% 比 100% 把透出的彩度抬高 33%、150% 抬高 48%，而在无彩色背景（细网格／12px 正文）上是逐像素相同的空操作，这就是「让玻璃带色偏」的量化依据；**③遮罩 8px 已经够**——面板外那片区域的梯度 1.29 → 0.19 → 0.18，16px 只再降 3%，观感上没有收益，收益只在每帧一次的更大核卷积。本行只改示例页与文档，库侧零改动；读数方法（亮度权重、2/255 计数阈值、噪声底）写在 08 节与 `GlassLab.razor.css` 的注释里。**本条读数随后被下一行「磨砂填充降档与玻璃墨阶梯」推翻重测**——旧读数全部基于 `.82` 的填充，填充降到 `.68`／`.72` 之后口径与数字都变了：①**模糊半径由死转活**，面板内残余细节（亮度标准差）`0.0508／0.0323／0.0184／0.0139`，8→16 降 37%、16→24 降 43%、24→32 降 24%，相邻档位的逐像素差异 92%／85%／39%，所以 24px 不再是缺口，而是四步里收益最大的一档；原「其余三档背景分不出四格」的定性结论不变（壁纸档 8→32 只降 25%、单步变化像素 < 1%，细网格档 8px 起残余已在 0.001 量级，12px 正文档过了 16px 就不再变）；②**饱和度读数改写**为彩度均值 `13.14／15.73／17.73／19.69`（仍严格正比于 100／120／135／150%，即 135% 抬高 35%、150% 抬高 50%），无彩色背景上是逐像素相同空操作的结论不变；③**遮罩梯度改写**为 `0.0026／0.00033／0.00026`（8px 一次抹掉 87% 的高频，16px 只再多抹 21%，绝对值只剩 0.00007），结论「8px 已经够、再往上只买到更大的卷积核」不变；④04／05 的读数改为取「截图时把文字遮住」之后的面板区域统计（残余细节用亮度标准差、彩度用 max − min(R,G,B)），因为旧口径的噪声底就落在文字抗锯齿上 |
| 磨砂填充降档与玻璃墨阶梯（on-glass tertiary） | 已完成 | 通过 | 起因：上一轮把「填充不能低于 82%」归因于三级墨的图形门槛，但那条约束只是因为玻璃表面仍在继承标准墨阶梯——该改的是墨，不是填充。做法：①把 `--aeterni-bg-glass` 从 `--aeterni-bg-elevated` 解耦成独立 token，三个主题块各自声明（浅 `.68`／深 `.72`／系统深 `.72`），因为「升起的表面」与「磨砂表面」答的不是同一道题：前者只要求落在页面上，后者要按最坏背景（纯黑／纯白）保证墨可读，共用一个值正是填充偏厚、糊不进背景的原因；②新增 `--aeterni-text-on-glass-tertiary`（`color-mix(in srgb, var(--aeterni-text) 67%, transparent)`），把三级墨也纳入 on-glass 阶梯——三级墨在玻璃上只承图标与装饰（Dialog／Drawer／NoticeCard 的关闭按钮、日期日历的 `.is-outside`），所以按 3:1 图形门槛定档；③七处磨砂内容表面（`Card`／`Surface` 的 `Glass` 变体、Dialog 面板、Drawer 面板、Popover 表面、Tooltip 内容、NoticeCard）各补一条 `--aeterni-text-tertiary`，并同步注释里「深色 4.35:1」这类旧读数；④删除三个编码被否决方向（液态玻璃高光边）的孤儿 token：`--aeterni-backdrop-saturate`、`--aeterni-glass-border-alpha`、`--aeterni-glass-highlight`，全库 0 引用；⑤两个降级块（`prefers-reduced-transparency: reduce` 与 `@supports not (backdrop-filter)`）把两支玻璃专用墨退回标准墨——墨的存在理由是顶住透出来的背景，填充一旦是实心表面就没有背景要顶；⑥修 `Bordered="false"` 的玻璃卡 hover 浮出边框：hover 规则 4 个类压过 `.is-bordered` 的 2 个类，把改边框那一条拆到 `.is-interactive.is-bordered` 上，位移与阴影仍对所有变体生效。实测口径（填充合成后的底色，最坏背景，1.1× 会计余量）：深色 `.82` → 标准次要墨 4.35:1 FAIL、标准三级墨 3.02:1 thin，`.72` → 3.41 FAIL／2.51 FAIL，接管 on-glass 后 5.11／3.51；浅色 `.82` → 5.33／3.86，`.68` → 4.51／3.43，接管后 5.94／3.46。下限由最紧的那支墨决定，两个主题不同：浅色是三级墨（1.15×）、深色是次要墨（1.14×），所以 `.68` 与 `.72` 分别是两个主题的边界值，再淡一档就掉出余量。**本条推翻的旧结论**：上一轮「填充已被三级墨钉死在 82% 以上」不成立——那是标准墨的约束。**未覆盖面（如实记录）**：品牌链接墨不在 on-glass 覆盖内，也不在任何门禁里，它本来就撑不住磨砂——深色最坏背景 `.82` 已是 3.25:1 FAIL、`.72` 掉到 2.27:1，浅色 `.82` 4.36:1 FAIL、`.68` 2.98:1；降填充把「链接可读的背景窗口」进一步收窄（浅色填充要求底色亮于 18/255 → 103/255，深色要求暗于 134/255 → 95/255），这是既有缺口而非本轮引入，已写进组件设计指南的硬规则。验证：`dotnet build aeterni_ui.slnx`、`check-docs.sh`、`check-css-comments.mjs`、`check-contrast.mjs`（99 项）、token-only grep 全过；headless Chrome 逐像素核对七处磨砂表面的墨与两个降级块。示例项目：`GlassLab` 对照页同步新填充（`GlassPalette` 的 `.68`／`.72`）、新增第五支读数「三级 · on-glass」、04／05／06 三节按新填充重测并把基于 `.82` 的旧数字全部替换（口径也改为「截图时把文字遮住」之后的面板区域统计），其中模糊半径轴由死转活、24px 从缺口变成收益最大的一档（更正见上一行）。文档同步三处：`current-features` 的玻璃角色别名条目（含十处消费者与填充下限的归因）与「模糊与实心」小节、`component-design-guidelines` 的玻璃硬规则（含新增的品牌墨警示：品牌墨浅色最坏 2.98:1／深色 2.27:1，不在门禁覆盖内）与提交检查清单新增一条「新增磨砂表面须一次接管五个角色名」、`component-roadmap` 本行。`project-index` 无需改动：本轮没有新增或移动项目、目录、入口文件、宿主、构建脚本或开发命令。版本号未递增 |
| v10.9.0 发布（磨砂配方解耦与填充降档） | 已完成 | 通过 | 版本号在 `Directory.Build.props` 单一来源递增 `10.8.0` → `10.9.0` （`Version`／`AssemblyVersion`／`FileVersion`／`InformationalVersion` 四处齐动）， 四份门禁文档的「文档版本」与 `project-index` 的「当前版本」同步， `component-review-todo` 一并跟进，README 的发布示例改为 `v10.9.0`； `bash scripts/check-docs.sh`（版本格式 `^10\.[0-9]+\.[0-9]+$` 与四文档一致性）与 `dotnet build aeterni_ui.slnx` 通过； 在 main 上打注解 tag `v10.9.0` 触发 `Publish NuGet packages`，工作流先由「Validate release tag」比对 tag 与 props 版本， 再用 NuGet Trusted Publishing（OIDC）发布 `AeterniUI` 与 `AeterniUI.Icons.FontAwesome`。 按仓库约定「功能新增递增次版本号」，本批（玻璃配方三轴解耦、on-glass 墨阶梯与磨砂填充降档）为 minor： 相对已发布的 `10.8.0` 无破坏性公共 API 变更——被撤销的 `Blur` 轴是同一批内引入、从未出现在任何已发布版本里，故不递增主版本号 |

| v10.10.0 / v0.5 日期与时间输入 | 已完成 | 通过 | 新增 `TimePicker`、`DateTimePicker`、`TimeFormat` 与 `DateRangePreset`；`DateRangePicker` 补充宿主定义快捷范围和 1～3 个月视图；示例、文案、功能事实源和项目索引同步。版本单一来源由 `10.9.0` 递增为 `10.10.0`。验证：`dotnet build aeterni_ui.slnx --no-restore` 0 警告/0 错误，`check-docs.sh`、`check-css-comments.mjs`（52 个 CSS 文件）、`check-contrast.mjs`（99 项）与 Font Awesome 生成一致性检查全部通过 |
| v10.10.1 / v0.5.1 质量收口 | 已完成 | 通过 | 完成 REV-64～REV-73、REV-75～REV-76：恢复 DatePicker/MenuButton 根契约，补齐 Accordion、DateCalendar、TimeOptionList 焦点与 ARIA，统一日期字段状态、Segmented 选中态、文案与尺寸 Token，扩充示例，并新增 `AeterniUI.ContractChecks` CI 门禁。按项目版本约定，本批属于现有能力修复与优化，版本单一来源由 `10.10.0` 递增为 `10.10.1`；首位继续与 .NET 10 对齐。验证：`dotnet build aeterni_ui.slnx` 0 警告/0 错误，6 组渲染契约检查、全部 `.razor.js` 语法、文档、52 个 CSS 文件注释、99 项品牌对比度、Font Awesome 343 图标一致性和 Token-only 检查全部通过；无头 Chrome 复核 1440px 浅色/深色与 390px 窄屏布局。 |
| DateTimePicker 组合弹层布局修复 | 已完成 | 静态与契约门禁验证，视觉待复核 | 弹层按双栏内容定宽并受视口约束，时间列最小宽度与实际滚轮一致；操作行使用自然高度，空状态居中且保留滚轮占位，避免按钮拉高及溢出。按用户确认保留现有提交行为，不给 DatePicker / DateRangePicker 新增确认模式。 |
| 单值选择器宽度统一与示例合成优化 | 已完成 | 契约检查覆盖，模糊效果待宿主实测 | DatePicker、TimePicker、DateTimePicker 统一 160px 最小宽度与 FullWidth 行为，DateTimePicker 示例新增交互开关；示例容器入场动画不再保留 forwards 状态，避免 opacity 动画残留合成层干扰后代毛玻璃采样，不修改模糊强度与透明度。 |
| 日期与时间选择器布局调整 | 已完成 | 契约检查覆盖 | DateRangePicker 弹层按内容定宽并限制在视口内，修正多月内容溢出导致的左右留白不一致；TimePicker 默认最小宽度使用 160px Token；DatePicker 新增 FullWidth，示例提供交互开关，根布局类纳入契约检查。 |
| TimePicker 滚动流畅性修复 | 已完成 | 自动化回归覆盖 | 时/分/秒滚轮改为停稳后回调草稿，取消逐项强制停止；相同选中值的重渲染不再重新居中，联动列继续同步。新增 `tests/time-wheel.test.mjs` 覆盖连续滚动、回调去重、重渲染、联动及释放；继续修复反向输入与旧吸附动画争抢滚动位置：移除 CSS 强制吸附，输入时取消旧 JS 动画，并通过 ResizeObserver 在隐藏弹层展开时重新居中；回归覆盖隐藏展开与反向中断。实际设备惯性手感仍需人工验证。 |
| v10.11.0 / TimePicker 时分秒滚轮 | 已完成 | 通过 | `TimePicker` / `DateTimePicker` 默认步长由 30 分钟调整为 1 秒，默认显示策略改为 24 小时制和 `HH:mm:ss`；保留 `Step` / `TimeStep` 的 `TimeSpan` API，并支持 1 秒到小于一天的整秒步长。内部选择面从全天扁平列表改为类似系统计时器的时/分/秒三列滚轮，具有五行视窗、顶部中文单位、无圆角且无左右边框的固定中心选择带、平滑吸附、上下渐隐和近大远小反馈，并提供取消/确定按钮与不污染绑定值的草稿状态；1 秒步长最多渲染 24 + 60 + 60 个当前列选项而不是 86,400 个时间点。触发器默认按时间文本与图标紧凑显示，新增 `FullWidth` 作为显式铺满入口；DateRangePicker 示例移除“最近/未来 7 天、30 天”业务预设，仅保留直接范围选择，通用 `Presets` API 仍由宿主按业务需要提供。左右键切列，上下键/Home/End 改值，Enter/Space 确认。按版本规范属于同一批功能扩展，版本保持 `10.11.0`。验证：解决方案构建 0 警告/0 错误，6 组组件契约、全部 JS/CSS/文档/对比度/图标门禁通过；无头 Chrome 实测三列中心与选择带中心完全重合，`00:00:00`、`23:59:59` 均可滚动选中，取消不回写、确定后关闭并更新触发器。 |

## 第三阶段（v0.3）计划

状态：已完成（7/7）。第三阶段在 v0.2 的基础上补齐纯展示类基础组件、通用分段控件和侧边面板，并为浮层类组件沉淀共享的定位与焦点能力。

进度：15 `Divider`、16 `Empty`、17 `Spinner`、18 `Skeleton`、19 `Badge` 已在 v10.6 交付；20 `Segmented`（含 `ThemeSwitch` 重构）与 21 `Drawer` 在 v10.7 交付。

版本规则沿用第二阶段：首位跟随目标 .NET 主版本，功能新增递增第二位，问题修复与优化递增第三位；破坏性公共 API 变更应优先避免或提供兼容迁移。

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

交付：`Segmented<TValue>` 基于原生 radio（`Items` / `TextSelector` / `ItemTemplate` / `DisabledSelector`）；单选语义、`aria-checked`、roving tabindex、方向键与 RTL 方向交给浏览器，因此组件没有键盘 JS；滑块宽度为「一列」、位移为「index 列」，来自组件写入的 `--aeterni-segmented-count` / `--aeterni-segmented-index`，2/3/5 项共用同一份几何；尺寸走控件高度阶梯（28/36/44px）；`ThemeSwitch` 重构为它的专用用法（文案仍取自文案表，不再有硬编码英文）。示例页 `/components/segmented`。

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
| ~~浮层定位能力（锚定/flip/shift/外部点击）~~（v10.4 已完成：`fitsSide` / `oppositeSide` / `clampCenteredShift` / `clampAlignedShift`） | 21 `Drawer` 的遮罩、v0.2 的 Tooltip 复用（已迁移）与 ComboBox 弹层迁移（已完成） |
| ~~语义枚举收敛（`Color`/`Severity`）~~（v10.5 已完成：`ComponentClass.ForColor` / `ToColor` 是唯一映射；v10.6 的 `Spinner` 与 `Badge` 直接复用它） | 19 `Badge`、17 `Spinner` 等新增带色组件（已解决） |
| ~~文案本地化入口~~（v10.6 已完成：`Spinner`/`Badge`/`Empty` 的可访问名称与空状态标题全部走 `AeterniUITextOptions`；v10.7 的 `Segmented` / `Drawer` 也沿用同一入口） | 20 `Segmented`（含 `ThemeSwitch` 重构，已解决） |
| ~~核心图标字形补充~~（v10.6 已完成：新增 `AeterniIcons.EmptyBox`） | 16 `Empty`（已解决）及后续新增组件 |
| ~~示例页拆分~~（v10.5 已完成：一个组件一个页面） | 第三阶段每个组件都需要可交互演示（已具备） |

## 第三阶段固定交付物与验收

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
| Accordion / Pagination / Avatar | 已完成 | 通过 | 折叠面板平滑高度动画、分页省略号与当前页过渡、图片/姓名缩写头像；示例与当前功能文档已同步 |

## 第四阶段（v0.4）计划

状态：基础交付已完成。该阶段已交付基于 `DateOnly` 的单日与日期范围选择，并复用 PopupHost / Popover、FormField 与现有 Token。

### 22. DatePicker / DateRangePicker

状态：已完成

目标：提供单日和日期范围选择控件。

当前已交付：`DateOnly?` 绑定、月份切换、日期边界、禁用日期、Popover 日历弹层、Size、基础 ARIA 语义、键盘导航、范围 hover 预览、FormField / EditContext 校验、必填/无效状态和关闭后焦点回归；日期格式化已支持 `Format` 与当前 Culture。v0.5 在此基础上补充了快捷范围和 1～3 个月视图。

后续边界：虚拟化、自定义日历系统和复杂本地化历法仍不在当前范围。`DateCalendar` 仅作为 DatePicker / DateRangePicker / DateTimePicker 的内部渲染部件，不属于稳定公共 API。

### 第四阶段固定交付物与验收

沿用既有固定交付物；完成后同步 `current-features.zh-CN.md`、示例页、项目索引和本文档任务记录，并运行构建、文档、CSS 与 JavaScript 检查。

## 第五阶段交付与后续规划（v0.5～v0.7）

状态：v0.5 的功能清单与 v0.5.1 第六轮质量收口均已交付；v0.6 已交付 Toolbar、ToggleGroup、SplitButton、Breadcrumb（4/5），Stepper 与 v0.7 仍为规划。本节中只有标记为已完成的能力属于当前公共表面，其余阶段不代表组件已经实现。

### 后续计划与实施优先级

以下优先级表示**实施顺序**，不替代审阅待办中的 P0～P3 严重度。原则是先恢复已发布组件的基础契约和键盘可用性，再扩充新的公共组件面。

| 实施优先级 | 计划 | 当前状态 | 排序依据 |
| --- | --- | --- | --- |
| 优先级 0 | v0.5.1 质量收口（REV-64～REV-73、REV-75～REV-76） | 已完成 | 已恢复根属性、键盘焦点、折叠内容可达性、视觉状态和本地化契约，并加入最小渲染回归门禁 |
| 优先级 1 | v0.6 动作编排：`Toolbar` → `ToggleGroup` → `SplitButton` | 三项均已完成 | ToggleGroup 复用键盘规则；SplitButton 复用 MenuButton 浮层与键盘实现 |
| 优先级 2 | v0.6 导航：`Breadcrumb` → `Stepper` | Breadcrumb 已完成（4/5） | Breadcrumb 先明确链接导航边界；Stepper 后续复用线性状态语义 |
| 优先级 3 | v0.7 搜索与数据选择：`Search` → `Autocomplete` → `MultiSelect` | 未开始 | 依赖稳定的输入、列表、浮层与焦点模型，复杂度和回归面最大 |

文档事实偏差 REV-74 已在本轮规划同步时直接修正。当前未实现计划从优先级 2 的 v0.6 Stepper 开始。

### v0.5.1 质量收口（优先级 0）

目标：在新增组件前关闭第六轮审阅发现的现有组件缺口。详细证据、修复方向和逐项验收见 [`component-review-todo.zh-CN.md`](component-review-todo.zh-CN.md) 的 REV-64～REV-76。

交付顺序：

1. **可见视觉阻断项（P0）**
   - [x] REV-69 `DatePicker` / `DateRangePicker`：对齐字段控件的 hover、focus-visible、disabled、invalid、过渡和 Token。
   - [x] REV-71 `Segmented` / 主题切换：修正内置标签截断与选中态 hover/active 整块换档。
2. **基础契约与可访问性（P1）**
   - [x] REV-64 `DatePicker`：恢复根元素 `BuildAttributes()`、CSS isolation 和 `Element` 契约。
   - [x] REV-65 `MenuButton`：恢复根元素 class/style/visible/disabled/full-width 等基类与组件状态契约。
   - [x] REV-66 `Accordion`：关闭面板退出键盘与辅助技术可达范围，ARIA 布尔值输出字符串。
   - [x] REV-67 `DateCalendar`：方向键后移动真实 DOM 焦点，并修正 grid 行/单元格语义。
   - [x] REV-68 `TimeOptionList`：改为单一 Tab 停留点的 listbox 焦点模型，补方向键/Home/End。
   - [x] REV-70 `Avatar`：收敛尺寸枚举、默认类和颜色变体，修正图片/容器重复命名与自定义内容名称。
3. **一致性、示例与防回归（P1/P2）**
   - [x] REV-72 日期、范围、日历导航和分页文案进入 `AeterniUITextOptions`；分页箭头改用核心图标。
   - [x] REV-73 日期/时间组件裸尺寸收敛到既有 Token，必要的几何例外补注释。
   - [x] REV-75 Accordion、Avatar、Pagination 与日期/时间示例补齐禁用、错误、键盘、窄屏和 reduced-motion 验收状态。
   - [x] REV-76 增加最小化渲染契约回归门禁，覆盖根属性透传、ARIA 字符串、焦点停留点和公开修饰类/样式匹配；不在此阶段扩张为业务测试套件。

依赖顺序：

```text
DatePicker / MenuButton 根契约
  +--> 日期字段视觉状态
  +--> SplitButton 的稳定组合基础
Accordion / DateCalendar / TimeOptionList 焦点语义
  +--> Toolbar / ToggleGroup 的共享键盘模型
本地化 + Token 收敛
  +--> 示例状态矩阵
          +--> 最小化渲染契约门禁
```

完成状态：REV-64～REV-73、REV-75～REV-76 已逐项实现并纳入示例或契约检查；REV-74 文档偏差也已同步关闭。构建、文档、CSS、JS、对比度与图标一致性门禁均作为本批次交付验证，质量收口完成后 Toolbar、ToggleGroup、SplitButton 与 Breadcrumb 已交付，下一实施项为 v0.6 `Stepper`。

### 规划原则

- **先扩展已有组合，再引入高复杂度数据组件。** 优先复用 `PopupHost`、`Popover`、`FormField`、`Menu`、`Tabs`、`Progress` 和现有日期网格，避免复制定位、焦点和校验逻辑。
- **每个阶段保持可独立交付。** 时间选择、动作编排和数据展示之间不互相阻塞；阶段内按依赖顺序逐项交付。
- **默认不承担业务数据源。** 新组件只负责渲染、交互、绑定和无障碍语义；远程加载、缓存、分页请求和业务状态由宿主应用负责。
- **复杂能力先做最小稳定公共面。** 不在第一版同时承诺虚拟化、拖拽、服务端查询、路由集成或多浮层堆叠。

### v0.5 日期与时间输入

目标：在现有 `DatePicker` / `DateRangePicker` 的基础上补齐常用时间输入，同时保持 `DateOnly` API 不被迫升级。

交付状态：

1. [x] `TimePicker`：`TimeOnly?` 绑定、步长、最小/最大时间、禁用规则、键盘选择、12/24 小时格式、`FormField`/`EditContext` 校验。
2. [x] `DateTimePicker`：使用单一 `DateTime?` 组合日期与时间；明确 Culture、Kind 和格式化规则，不做时区转换。
3. [x] `DateRangePicker` 增强：宿主提供的 `DateRangePreset` 快捷范围，以及 1～3 个月的 `VisibleMonths` 多月视图。

依赖顺序：

```text
TimePicker
  +--> DateTimePicker
DateCalendar 多月/快捷范围能力
  +--> DateRangePicker 增强
```

验收边界：沿用现有 Popover、焦点回归和无效态约定；不包含跨午夜时间范围、时区数据库、时区转换、虚拟化、复杂本地化历法和自定义日历系统。`DateTimePicker.Kind` 只标记新建值，边界比较沿用 `DateTime` 本身的比较规则；快捷范围由宿主提供纯数据，并在应用前校验边界和禁用日期。`DateCalendar` 继续保持内部部件，不直接升级为稳定公共 API。

验证：`dotnet build aeterni_ui.slnx`、`bash scripts/check-docs.sh`、CSS 注释、品牌色对比度和图标生成一致性门禁均通过；示例新增 `/components/time-picker`、`/components/date-time-picker`，日期选择页补充双月与快捷范围演示。

### v0.6 动作编排与导航

目标：覆盖工具栏和复杂动作入口，补齐现有 Button、ButtonGroup、Segmented、MenuButton 之间的组合空白。

状态：Toolbar、ToggleGroup、SplitButton 与 Breadcrumb 已交付（4/5）；Stepper 未开始。

计划顺序：

1. [x] `Toolbar`：无背景动作容器、命名分组、横纵方向、RTL、方向键/Home/End、整条工具栏一个 Tab 停留点、禁用/隐藏/加载跳过；仅接纳按钮与菜单触发器，溢出由宿主显式放置 MenuButton。
2. [x] `ToggleGroup`：单选/多选动作切换，单选再次激活允许清空；严格受控 SelectedValues，合法且唯一 Id 校验。具名 group / 原生按钮 aria-pressed（不使用 aria-checked），与 Segmented 表单值选择明确分离；方向键/Home/End 只移焦点、RTL/纵向、禁用跳过与参数更新焦点修复；交互示例、渲染/状态契约及 JS 回归已覆盖。
3. [x] `SplitButton`：主动作 + 菜单动作，复用 `Button`、`MenuButton`、`PopupHost`；独立名称与禁用、整体禁用优先、仅主动作 Loading、受控菜单、按钮尺寸/变体与 RTL 连接样式、根属性透传已交付。包含 `/components/split-button` 可交互示例及渲染契约回归。主动作不支持提交/链接/自动替换，无异步编排或 Toolbar 焦点集成；关闭焦点回归复用 Popover。
4. [x] `Breadcrumb`：服务于层级导航，不把路由跳转内置到组件库；提供可见/禁用项过滤、当前页 ARIA 语义、原生链接与可替换分隔符。
5. [ ] `Stepper`：服务于线性流程，不把业务流程状态管理内置到组件库。

依赖顺序：

```text
Button / ButtonGroup / MenuButton
  +--> Toolbar
  +--> ToggleGroup
  +--> SplitButton
Tabs / Menu 语义与键盘模型
  +--> Breadcrumb / Stepper
```

验收边界：第一版不包含拖拽排序、响应式自动折叠、快捷键注册中心、路由集成、异步动作编排和跨组焦点堆栈。`Toolbar` 的方向键模型需与未来 `ToggleGroup` 共享规则，但不应把普通 `ButtonGroup` 改造成方向键组件。

### v0.7 搜索与数据选择

目标：在完成时间和动作基础后，补齐从关键词输入到结果选择的搜索组件能力；搜索结果的数据获取、缓存和业务状态仍由宿主应用负责。

状态：未开始（0/3）。

计划顺序：

1. [ ] `Search`：关键词输入、清除、提交、搜索状态和可访问名称；优先复用 `Input`、`Button`、`IconButton` 与现有表单校验语义。
2. [ ] `Autocomplete`：自由输入、建议列表、键盘导航、异步结果由宿主通过参数/回调提供；复用 `Search` 的输入状态与 `PopupHost` + `List` 语义。
3. [ ] `MultiSelect`：多值绑定、已选项展示、移除操作、全选/清空策略；与现有纯下拉 `ComboBox` 保持明确边界。

依赖顺序：

```text
Input + Button + IconButton
  +--> Search
Search + List + PopupHost
  +--> Autocomplete
  +--> MultiSelect
```

验收边界：第一版不包含服务端数据源、查询缓存、搜索历史、筛选器编排、全文检索实现、树形结果、数据表格、虚拟滚动和复杂结果渲染。`Search` 负责输入与提交契约，不内置请求客户端或结果列表；`Autocomplete` 与 `MultiSelect` 的异步数据和业务筛选由宿主提供。

### 后续阶段固定交付物与决策门

每个新组件沿用既有固定交付物：组件源码、公开枚举/模型、可操作示例、`current-features` 更新、路线图任务记录，以及构建、文档、CSS 和 JavaScript 检查。

进入实现前必须完成以下决策：

| 决策门 | 通过标准 |
| --- | --- |
| API 边界 | 已说明与现有组件的组合关系、受控/非受控状态和不支持能力 |
| HTML/ARIA | 已确定根元素、键盘模型、焦点模型和错误/禁用/加载语义 |
| Token/主题 | 只消费现有 Token；没有新增第二套尺寸、颜色、圆角或动效体系 |
| 浮层/JS | 已确认能否复用共享浮层模块；只有必要的浏览器行为才引入 `.razor.js` |
| 示例/验收 | Light、Dark、System、窄屏、键盘和 reduced-motion 均有可操作验收路径 |

| 发布 | 状态 | 验证 | 内容 |
| --- | --- | --- | --- |
| v10.12.0 / v0.6 Toolbar | 已完成（1/5） | 构建、渲染契约、JS 焦点回归、语法、CSS 与文档门禁 | 新增 Toolbar / ToolbarGroup、交互示例；不更改普通 ButtonGroup 语义。此版本交付时其余四项未开始。 |
| v0.6 ToggleGroup（纳入 10.13.0） | 已完成（累计 2/5） | 构建、渲染/状态契约、JS 焦点回归、语法、CSS 与文档门禁 | 新增 ToggleGroup / ToggleGroupItem、严格受控单选/多选、单选再次点击清空、可操作示例与目录注册；默认外观改为连体分段（无 gap、单接缝、首尾圆角、横纵向/RTL 与焦点分层）；三档最小高度保持 28/36/44px，水平留白改用 control-padding-x-sm/md/lg、垂直留白归零、显式居中且字号/字重/行高对齐 Button，补充三档渲染与 CSS 尺寸契约；不改变受控状态和键盘语义，实现阶段未修改其他组件尺寸，版本号由 10.13.0 发布批次统一递增。 |
| v0.6 SplitButton（纳入 10.13.0） | 已完成（累计 3/5） | 构建、渲染契约、Popover 焦点回归、JS 语法、CSS 与文档门禁 | 主动作与菜单动作组合、独立名称和禁用、仅主动作加载、受控菜单、可操作示例；修复 MenuButton 菜单 ID 关联与非模态关闭焦点回归。默认外观收紧为 Small / Ghost / Neutral、窄箭头和原位高亮，保留显式尺寸/变体；Toolbar 同步缩小组间/组内间距，图标主工具栏示例不覆盖组件样式，不增加 Toolbar 与 SplitButton 焦点集成。 同批以现有 List 留白为参考（List 不变），收紧 MenuButton 专属 Popover 外围、Menu 行内；保留 Menu 的 8px 子项层级缩进，修复折叠/空组残留 gap 导致的上下留白不一致；保留行高与通用内容弹层默认留白，补充隔离渲染断言。 |
| v0.6 Breadcrumb | 已完成（累计 4/5） | 构建、渲染契约、CSS 与文档门禁 | 新增 `Breadcrumb` 与 `BreadcrumbItem`，原生链接、最后可见项当前页语义、禁用/隐藏过滤、自定义分隔符与 `/components/breadcrumb` 交互示例；不包含路由集成或自动折叠。 |
