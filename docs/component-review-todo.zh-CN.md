# AeterniUI 组件审阅待办

文档版本：`10.2.0`

文档状态：29 项已全部在 v10.2.0 修复；每一项保留问题描述与验收条件，并在条目内记录修复方案。

本文档记录一次全量组件审阅（token 层、22 个组件的 `.razor.css`、`.razor` 标记与关键 `.razor.cs`/`.razor.js`）发现的问题，按优先级排列，供后续分批修复和验收追踪。

## 文档边界

- 本文档只记录**问题、证据、修复方向和验收条件**，不记录已实现能力。
- 修复涉及公共 API 或组件行为变化时，必须同步 [`current-features.zh-CN.md`](current-features.zh-CN.md)。
- 修复涉及未完成能力时，状态只在 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md) 记录；未补齐前不得在功能文档或索引中写成已实现。
- 修复涉及项目结构、入口或构建脚本时，同步 [`project-index.zh-CN.md`](project-index.zh-CN.md)。
- 组件设计契约以 [`component-design-guidelines.zh-CN.md`](component-design-guidelines.zh-CN.md) 为准；本清单不新增契约。

## 审阅方法与可信度

- 本次为**静态审阅**：未运行应用、未做截图比对。视觉结论来自 CSS 规则推导和 token 数值计算。
- 对比度数值（REV-04）由 token 十六进制值按 WCAG 2.1 相对亮度公式计算，修复前需实测确认。
- "证据位置"给的是文件加选择器/成员名，而不是行号，便于在后续重构后继续定位。

## 修复摘要（v10.2.0）

| 编号 | 修复位置 | 关键改动 |
| --- | --- | --- |
| REV-01 | `Switch.razor.css`、Token 层 | 新增 `--aeterni-state-background-thumb`（浅色取 surface、深色取 `gray-0`），滑块不再复用反色文字 Token |
| REV-02 | `Button.razor.css` | 新增 `--aeterni-button-loading-surface/-veil/-foreground`：所有变体统一为“自身表面半透明层 + `blur(2px)`”，遮罩向外扩展一个边框宽度以盖住控件边框，Button 因此不再裁剪子元素 |
| REV-03 | `DialogProvider.razor` | 每个 `ToastPosition` 只渲染一个位置容器，Alert 在前、Toast 在后纵向堆叠 |
| REV-04 | Token 层、`Button.razor.css` | 新增 `--aeterni-color-on-semantic(-strong)`；success/warning/danger/info 实心前景改用该 Token，base 与 hover/active 分别在两种主题下 ≥ 4.5:1 |
| REV-05 | `Input.razor.css`、`Textarea.razor.css` | `:focus-visible` 补回 `--aeterni-focus-width` 焦点环；无效态使用 `--aeterni-focus-color-invalid` 且优先保留危险色边框 |
| REV-06 | `FormFieldContext`、`FormField`、`ComboBox`、`Rating`、`RadioGroup`、`Radio` | 上下文增加 `LabelId`；容器型控件采用输入 ID 并用 `aria-labelledby`/`aria-describedby` 关联标签、描述与错误 |
| REV-07 | `ListItem.razor(.cs)`、`List.razor.cs`、`ListItem.razor.css` | 选项改为非可聚焦 `role="option"`；容器独占焦点，方向键不再调用 `FocusAsync`，新增 `is-active` 视觉提示 |
| REV-08 | `Rating.razor(.cs)` | `aria-checked` 仅当前值为 true；实现 roving tabindex（一次 Tab 进出）；只读/禁用时把当前值并入可访问名称（复用原本的死代码 `ValueLabel`） |
| REV-09 | `ComboBox.razor(.cs/.css)` | 选项改为非聚焦 `role="option"` div，移除选项 `:focus-visible`，保留 `aria-activedescendant` 模型 |
| REV-10 | `Radio.razor(.cs/.css)`、`RadioGroup.razor.cs` | 补齐 hover/invalid/disabled+checked；盒体改用 `--aeterni-control-size-*`、1px 边框、内圆点；新增 `Size` 档位（分组级联可覆盖）。根元素改为 `<label>` 承载组件类（与 Checkbox、Switch 一致），否则类名会被 `@attributes` 之后的显式 `class` 覆盖，尺寸与状态类失效 |
| REV-11 | `Tooltip.razor(.cs/.css/.js)` | 新增 JS module：把提示节点 id 写入首个可聚焦元素的 `aria-describedby`；方位翻转 + 交叉轴偏移 |
| REV-12 | `Tag.razor.css`、Token 层 | 新增 `--aeterni-touch-target-min`（24px），透明伪元素扩大热区，Tag 高度不变 |
| REV-13 | `PopupHost.razor.css`、`Popover.razor(.css/.cs)` | 移除 `display: contents` 死 CSS，`PopupHost` 成为真实定位容器；`is-modal` 获得真实层级样式；文档与示例明确基础版边界（见下） |
| REV-14 | Token 层 + 11 个组件样式表 | 新增浮层/紧凑表面度量 Token；把裸像素收敛到 Token；动画位移、无障碍裁剪与排版微调改为带注释的说明 |
| REV-15 | `ComponentClass.cs` + 各组件 | 默认档位不再输出修饰类；删除 `is-dismissible`/`is-required`/`is-accordion`/`is-multi`、`--default` 死规则、`NoticeCard` 死分隔符标记与 `Card` 重复选择器 |
| REV-16 | Token 层、`Button`、`Input`、`Surface` | 新增 `--aeterni-radius-control-*` 阶梯；Button 与 Input 同档圆角一致；`--aeterni-radius-surface` 与 `--aeterni-radius-card` 的关系写入注释与规范 |
| REV-17 | `ComponentClass.cs` + 各组件 `.razor.cs` | 尺寸/颜色映射集中到 `ComponentClass.ForSize/ForColor/For`；`Size.Medium` 与 `Size.Default` 视为同一档；ThemeSwitch 默认值改为 `Size.Default`；`Severity→Color` 只保留一处 |
| REV-18 | `AeterniUITextOptions` + 6 个组件 | 新增可覆写文案表（默认英文），`AddAeterniUI` 注册并校验空值回退；组件参数优先于文案表 |
| REV-19 | `Menu/Radio/Tooltip/Progress` 等 | CSS 全部改为多行格式；52 处内联 `[Parameter]` 改为独立行声明；`ListItem.razor` 的注释改为 Razor 注释（`@* *@`），避免标记上下文的 `/* */` 被渲染成可见文本 |
| REV-20 | `Card.razor.css`、`Surface.razor.css` | 逐条核对两套修饰规则指向相同 Token（脚本比对全部 SAME），默认差异写入注释与规范 |
| REV-21 | `DialogProvider.razor.css`、`ComboBox.razor.css`、Token 层 | 滚动条改为细样式（`--aeterni-scrollbar-*`），通知区域子元素恢复 `pointer-events` 以支持滚轮 |
| REV-22 | Token 层、示例 `index.html`、`ThemeProvider.razor.cs` | 新增 `prefers-color-scheme: dark` 兜底（显式主题属性时失效）；示例预渲染脚本补 `localStorage` 恢复；文档说明 ThemeProvider 不渲染 DOM |
| REV-23 | Token 层注释 + `current-features` | 字体栈中 Inter / JetBrains Mono 标注为可选宿主依赖，库不随包提供 webfont |
| REV-24 | `TooltipPlacement` 枚举、`Tooltip.razor(.cs/.css/.js)` | 支持 Top/Bottom/Start/End，与 REV-11 共用同一套翻转/偏移实现 |
| REV-25 | `RadioGroup.razor(.cs/.css)` | 新增 `Orientation`，垂直单列布局并同步 `aria-orientation`，fieldset 输出 `role="radiogroup"` |
| REV-26 | `current-features` §24 | 明确 Toast 上限为 `MaxToastCount`（默认 5）、超出时移除最早一条、Alert 由调用方控制生命周期；容器超出视口时可滚动 |
| REV-27 | `Rating.razor.css` | 自定义 `Icon` 与内置星形统一使用 `--aeterni-icon-size-lg`，删除 glyph 覆盖 |
| REV-28 | `DialogProvider.razor.js` | 锁定滚动时按滚动条宽度补偿 `body` 右内边距，关闭时恢复 |
| REV-29 | `Button.razor.css`、`ButtonGroup.razor.css` | 文字/链接禁用态改用更轻的半透明表面（与注释一致）；分隔线取自按钮自身前景色，实心变体全部可辨 |

### 后续回归中修正的实现问题

第一轮修复后复测发现三个实现缺陷，均已修正（详见上表 REV-02 / REV-10 / REV-19）：

- `ListItem.razor` 在标记上下文使用了 `/* */`，Razor 会把它当作文本输出，导致 List 预览出现可见注释文字；已改为 `@* *@`。
- `Radio` 的根类原本通过 `@attributes` 落在原生输入上，而显式 `class` 在源码中位于 `@attributes` 之后，会覆盖它，因此 `aeterni-radio` 及其状态类从未出现在 DOM 中；改为由根 `<label>` 承载组件类与输入属性分离后，尺寸 Token 与状态样式才真正生效（同一问题也影响 `RadioGroup` 的 `Orientation` 类和 `Tooltip` 的消费者 `Class` 参数，一并修正）。

约定：`@attributes="BuildAttributes()"` 所在元素不要再写显式 `class`（除非该属性集合不含 class），需要条件类名时把它们放进 `BuildClass()`。

### 未按“补齐实现”处理的条目

- **REV-13**：审阅给出的两个选项中选择“暂不补齐 + 三处描述一致”。锚点定位、翻转、遮罩、焦点陷阱与点击外部关闭需要先抽取共享浮层能力（第三阶段前置能力，见 roadmap），否则会在 `Dialog`、`Popup`、`Drawer` 各维护一份实现。本次已删除死 CSS、让 `PopupHost` 成为可用的定位容器、把 `Modal` 的实际效果（仅 `role` 与层级）写入 `current-features` 与示例，并保持 roadmap 状态为“基础版”。
- **REV-23**：审阅允许“随包提供字体”或“文档说明为可选依赖”，本次选择后者，因此字体栈仍保留 Inter / JetBrains Mono 名称。

## 优先级定义

| 优先级 | 含义 | 处理时机 |
| --- | --- | --- |
| P0 | 用户可直接观察到的视觉错误，或对比度不达标 | 立即，单批修复 |
| P1 | 一致性缺口和可访问性缺陷，影响正确使用 | P0 之后 |
| P2 | Token 纪律、代码卫生、可维护性 | 与相关组件改动合并 |
| P3 | 体验增强，可延后 | 有余量时 |

## 状态图例

- `[ ]` 未开始
- `[~]` 进行中
- `[x]` 已完成并验收

## 总览

所有条目均已修复并通过 `dotnet build aeterni_ui.slnx`、`bash scripts/check-docs.sh` 与 `node --check`（涉及 `.razor.js` 时）门禁。

| 编号 | 优先级 | 组件/范围 | 一句话问题 |
| --- | --- | --- | --- |
| REV-01 | P0 | Switch | 深色主题关闭态滑块与轨道同亮度，几乎不可见 |
| REV-02 | P0 | Button | Loading 遮罩让 outline/ghost/text/link 变成实心色块 |
| REV-03 | P0 | DialogProvider | Alert 与 Toast 在同一位置几何重合 |
| REV-04 | P0 | Button | 浅色主题实心 success/danger/info 白字对比度不足 |
| REV-05 | P1 | Input / Textarea | 聚焦无 focus ring，与全库其他控件不一致 |
| REV-06 | P1 | FormField | 对 ComboBox/Radio/Rating 的标签关联失效 |
| REV-07 | P1 | List / ListItem | 子项缺 `role="option"`，listbox 语义不成立 |
| REV-08 | P1 | Rating | radiogroup 语义错误，且 5 颗星全部在 Tab 序列 |
| REV-09 | P1 | ComboBox | 选项用 `<button role="option">`，破坏 listbox 焦点模型 |
| REV-10 | P1 | Radio / RadioGroup | 缺 hover/invalid/disabled+checked，视觉重量与 Checkbox 不一致 |
| REV-11 | P1 | Tooltip | 无 `aria-describedby`，仅 top 定位且无边界避让 |
| REV-12 | P1 | Tag | 关闭按钮 16×16，低于 24×24 最小目标 |
| REV-13 | P1 | Popup | 无定位、`is-modal` 无任何样式、无遮罩与焦点管理 |
| REV-14 | P2 | 全局 | 硬编码尺寸与字号绕过 token 体系 |
| REV-15 | P2 | 全局 | 死类、死 CSS、死标记与重复规则 |
| REV-16 | P2 | Button / Input / Card / Surface | 圆角体系不自洽 |
| REV-17 | P2 | Enums | `Color`/`Severity` 并存，`Size.Default`/`Medium` 语义重复 |
| REV-18 | P2 | 全局 | 库内硬编码中英文文案，无本地化入口 |
| REV-19 | P2 | 全局 | 代码格式双轨（单行压缩 CSS、参数声明两种风格） |
| REV-20 | P2 | Surface / Card | 两套近乎相同的样式，默认值相反 |
| REV-21 | P2 | DialogProvider / ComboBox | 滚动可用性：隐藏滚动条 + `pointer-events` 冲突 |
| REV-22 | P2 | Theme | 主题闪烁（FOUC），ThemeProvider 不渲染 DOM |
| REV-23 | P2 | Token | 字体栈声明 Inter/JetBrains Mono 但未随包提供 |
| REV-24 | P3 | Tooltip | 仅支持 top 定位 |
| REV-25 | P3 | RadioGroup | 缺 `Orientation` 与垂直布局 |
| REV-26 | P3 | DialogProvider | 通知堆栈无数量上限与间距策略 |
| REV-27 | P3 | Rating | 自定义图标与默认星形尺寸不同 |
| REV-28 | P3 | DialogProvider | 打开时未补偿滚动条宽度 |
| REV-29 | P3 | Button / ButtonGroup | 禁用态变量重复、注释与实现不符 |

---

## P0：影响可见效果，优先修复

### REV-01 Switch 深色主题关闭态滑块不可见

- [x] 已修复

- **证据**：`src/AeterniUI/Components/Switch/Switch.razor.css` 中 `.aeterni-switch__knob { background: var(--aeterni-text-inverse); }`；关闭态轨道为 `var(--aeterni-bg-tertiary)`。
- **现象**：浅色主题 `--aeterni-text-inverse` = `#FFFFFF`，滑块为白色，正常；深色主题 `--aeterni-text-inverse` = `#1B1D1F`，轨道 `--aeterni-bg-tertiary` = `#202326`，两者亮度几乎相同，滑块连同深色 `shadow-sm` 一起消失。开启态不受影响（轨道变为亮色 `brand-400`）。
- **根因**：用"反色文字"token 承担"控件本体色"，语义错配。
- **修复方向**：引入 `--aeterni-switch-knob`（或复用 `--aeterni-bg-surface`）语义 token，在 light/dark 两个主题块中分别定义；深色主题需要为滑块提供与轨道明确的亮度差，必要时补一层描边。
- **验收**：浅色与深色主题下，关闭态滑块与轨道均有可辨识边界；开启态对比度不下降。

### REV-02 Button 加载遮罩把非实心变体变成实心色块

- [x] 已修复

- **证据**：`src/AeterniUI/Components/Button/Button.razor.css` 中 `.aeterni-button__loading { background: var(--aeterni-button-background); }`（`inset: 0`，不透明）；`.aeterni-button--outline/--ghost/--text/--link { background: transparent; }` 只改了元素背景，未重定义 `--aeterni-button-background`。
- **现象**：outline/ghost/text/link 按钮进入 `Loading` 时被不透明品牌色覆盖，视觉上从文字按钮突变为实心主按钮；`.is-loading` 又被排除在 disabled 样式之外，因此禁用态同样出现该色块。
- **修复方向**：在各变体中同步重定义 `--aeterni-button-background`（或新增 `--aeterni-button-loading-veil`）；文字/链接类变体建议改用 `color-mix` 半透明 + 轻微模糊，保留"仍在原位"的视觉线索。
- **验收**：6 种变体（default/outline/ghost/text/link + 各语义色）在 Loading 时仍能辨认原变体；spinner 与遮罩对比度达标。

### REV-03 Alert 与 Toast 在同一位置几何重合

- [x] 已修复

- **证据**：`src/AeterniUI/Components/Dialog/DialogProvider.razor` 连续两轮遍历 `NoticePositions`，alerts 与 toasts 各自渲染一个 `.aeterni-dialog-provider__notice-region--{position}`；`DialogProvider.razor.css` 中该区域为 `position: fixed` 且各位置偏移量相同，`z-index` 同为 `var(--aeterni-z-toast)`。
- **现象**：同一 `ToastPosition`（如 `bottom-center`）同时存在 Alert 和 Toast 时，两个固定层坐标完全重合，内容互相覆盖（后渲染者压在上面）。
- **修复方向**：合并为单一位置容器（同一容器内先 alerts 后 toasts 纵向堆叠），或为两类通知定义明确的次级偏移/独立层级。
- **验收**：8 个 `ToastPosition` 下，同时触发 Alert 与 Toast 均不重叠、不遮断；退出动效方向仍正确。

### REV-04 浅色主题实心 success/danger/info 按钮前景对比度不足

- [x] 已修复

- **证据**：`src/AeterniUI/Components/Button/Button.razor.css` 基础规则设置 `--aeterni-button-foreground: var(--aeterni-text-inverse)`；`.aeterni-button--success/--danger/--info` 未重定义该变量，只有 `.aeterni-button--warning` 改为 `var(--aeterni-text-primary)`。
- **现象**（浅色主题白字，按 token 值计算）：

  | 变体 | 背景 | 前景 | 对比度 |
  | --- | --- | --- | --- |
  | success | `#34C759` | `#FFFFFF` | ≈2.2:1 |
  | danger | `#FF3B30` | `#FFFFFF` | ≈3.5:1 |
  | info | `#007AFF` | `#FFFFFF` | ≈4.0:1 |
  | warning | `#FF9500` | `#201B29` | ≈7.9:1（已修） |

- **修复方向**：与 warning 保持一致，改用深色前景；或把这些语义色背景替换为 600/700 档（`--aeterni-success-600` 等）。两种主题都要复核，并确认不影响 Alert/Toast 与 Dialog 的语义色映射。
- **验收**：实心语义按钮在浅色和深色主题下正文对比度 ≥ 4.5:1；`Progress`、`Tag`、`Icon` 的同类语义色用法一并复核。

---

## P1：一致性与可访问性

### REV-05 Input / Textarea 缺少焦点环

- [x] 已修复

- **证据**：`Input.razor.css`、`Textarea.razor.css` 的 `:focus, :focus-visible` 规则为 `border-color: ...; box-shadow: none; outline: none;`。
- **现象**：焦点反馈只剩 1px 边框变色；而 Button/Checkbox/Switch/Radio/ComboBox/List/Menu/Rating/ThemeSwitch 都使用 `var(--aeterni-focus-width)` + `var(--aeterni-focus-color)` 的 outline。同一表单内 ComboBox 有焦点环、Input 没有。
- **修复方向**：补回 outline 焦点环，并保证 `.is-invalid` / `[aria-invalid="true"]` 状态仍优先显示危险色边框（不要让环覆盖错误提示）。
- **验收**：键盘 Tab 到 Input/Textarea 时有明显焦点环；invalid 状态下错误提示仍可辨识。

### REV-06 FormField 标签关联对 ComboBox/Radio/Rating 失效

- [x] 已修复

- **证据**：`src/AeterniUI/Components/FormField/FormFieldContext.cs` 只暴露 `InputId`（`{ElementId}-input`）与 `DescribedBy`；`Input`/`Textarea`/`Checkbox`/`Switch` 消费 `InputId`，`ComboBox` 只读取 `FormField?.Invalid`，`Radio`/`Rating` 完全不消费该上下文。
- **现象**：`<FormField Label="X"><ComboBox /></FormField>` 渲染出的 `<label for="…-input">` 指向不存在的 id；ComboBox/Radio/Rating 无可访问名称，点击标签也不聚焦控件。
- **修复方向**：`FormFieldContext` 增加标签 id，三个组件补 `aria-labelledby`（Radio/Rating 还需把描述与错误 id 接到组容器上）；若决定不支持，必须在组件契约中写明。
- **验收**：`Label` + 描述 + 错误三件套在 5 个表单控件上都能被屏幕阅读器正确朗读（读屏或无障碍树检查）。

### REV-07 ListItem 缺少 `role="option"`

- [x] 已修复

- **证据**：`List/ListItem.razor` 的交互分支渲染 `<button ... tabindex="-1" aria-selected="…">`，没有 `role`；`List.razor.cs` 的 `BuildAttributes` 已在根节点设置 `role="listbox"`、`aria-multiselectable`、`aria-activedescendant`、`tabindex=0`。
- **现象**：`aria-selected` 在 button 角色上无效，且 listbox 的直接子元素必须是 `role="option"`，当前结构使整个 listbox 语义不成立。另：点击子项会让浏览器把焦点移到该 button，与 `aria-activedescendant` 模型冲突。
- **修复方向**：子项改为 `role="option"`（可保留 button 以便复用，但需确认角色覆盖后的行为），或改为非交互元素 + 由容器统一处理激活，并同步选中态的表达方式。
- **验收**：无障碍树中 listbox → option 层级正确，多选状态的 `aria-selected` 被正确朗读。

### REV-08 Rating 单选语义与 Tab 序列

- [x] 已修复

- **证据**：`Rating.razor` 容器 `role="radiogroup"`，每颗星 `role="radio"` 且 `aria-checked="@(rating <= Value)"`；每颗星都是 `<button>`，没有 roving tabindex。
- **现象**：一个 radiogroup 内出现多个 `aria-checked="true"`（ARIA 违规）；Tab 需要按 5 次才能离开组件。另外 `Rating.razor.cs` 里的 `ValueLabel` 未被任何标记使用（死代码），只读态没有向 AT 说明当前值。
- **修复方向**：仅 `rating == Value` 时 `aria-checked="true"`（"已填充"视觉与"已选中"语义分离）；实现 roving tabindex（只有当前值可 Tab，其余 `tabindex="-1"`，配合方向键）；只读态暴露当前值。
- **验收**：radiogroup 内恒有且仅有一个 checked；Tab 一次进出组件；只读值可被朗读。

### REV-09 ComboBox 选项角色与焦点模型

- [x] 已修复

- **证据**：`ComboBox.razor` 的触发器是 `<button role="combobox">`，选项是 `<button role="option">`；`ComboBox.razor.css` 还为选项定义了 `:focus-visible` 样式。
- **现象**：选项作为 button 仍在 Tab 序列内，焦点可以离开触发器进入弹层，破坏 `aria-activedescendant` 的 select-only combobox 模型；选项被覆盖为 option 角色后，按钮的隐式语义与键盘行为也不再匹配。
- **修复方向**：按 ARIA APG 的 select-only combobox 模式，选项改为非聚焦元素（由触发器保持焦点并通过 `aria-activedescendant` 指示当前项）；触发器保留 `aria-expanded`/`aria-controls`/`aria-haspopup="listbox"`。
- **验收**：打开弹层后 Tab 不进入选项列表；方向键移动时读屏正确播报当前项；Esc/Enter 行为符合 APG。

### REV-10 Radio / RadioGroup 状态缺口与视觉重量

- [x] 已修复

- **证据**：`Radio/Radio.razor.css` 只有 `:checked`、`:focus-visible`、`:disabled` 三组状态，没有 `:hover`，没有 `.is-invalid` 规则，`Radio.razor.cs` 也未把 invalid 变成类名；`:disabled:checked` 没有降级处理（Checkbox 与 Switch 都有）。尺寸为 18px + 2px 边框，Checkbox 为 20px + 1px 边框；Radio 无 sm/lg 档位；CSS 压缩为单行，与全库风格不一致。
- **现象**：同一表单行内 Radio 比 Checkbox 视觉重量更轻且无 hover 反馈；错误状态无任何视觉提示；禁用 + 已选中时仍显示高亮品牌色。
- **修复方向**：补齐 hover、invalid、disabled+checked 三种状态，对齐 Checkbox 的尺寸档位与边框重量，并把 CSS 恢复为多行格式。
- **验收**：Radio 在 hover / invalid / disabled / disabled+checked 下的表现与 Checkbox 对齐；一行内 Checkbox 与 Radio 视觉重量一致。

### REV-11 Tooltip 语义与定位

- [x] 已修复

- **证据**：`Tooltip.razor` 输出 `role="tooltip"` 的内容节点，但触发器没有 `aria-describedby`；`Tooltip.razor.css` 只定义 `bottom: calc(100% + ...)` 一种位置，无翻转或边界避让。
- **现象**：AT 不会朗读提示内容；触发器靠近视口边缘时提示被裁切。
- **修复方向**：为内容生成 id 并在触发器上设置 `aria-describedby`（无文本时应完全移除节点，避免悬空引用）；定位增加翻转/位移避让，可参考 `ComboBox.razor.js` 的 flip + clamp 实现。
- **验收**：读屏可朗读提示文本；视口四边触发时提示完整可见。

### REV-12 Tag 关闭按钮热区过小

- [x] 已修复

- **证据**：`Tag.razor.css` 中 `.aeterni-tag__dismiss { width: 16px; min-height: 16px; height: 16px; }`。
- **现象**：16×16 低于 WCAG 2.5.8 的 24×24 最小目标（NoticeCard 的关闭按钮为 24×24，刚好达标）。
- **修复方向**：通过透明 padding 或伪元素扩大热区到 ≥24×24，同时不撑高 chip 高度（保持现有 `margin` 负值技巧）。
- **验收**：热区 ≥24×24，Tag 高度不变。

### REV-13 Popup 模块定位、遮罩与关闭行为缺失

- [x] 已修复

- **证据**：
  - `Popup/Popover.razor.css` 中 `.aeterni-popover` 没有任何 `position` 规则，弹层不可锚定；
  - 同文件 `.aeterni-popup-host { position: relative; display: contents; }` —— `display: contents` 不生成盒子，`position` 失效（死 CSS）；
  - `Popover.razor.cs` 在 `Modal=true` 时只添加 `is-modal` 类，而 CSS 中不存在该规则；
  - 目录内没有 `.razor.js`。
- **现象**：Popover 没有定位、没有遮罩、没有 `aria-modal`、没有焦点管理、没有 Escape 与外部点击关闭，`Modal` 参数实际无效。
- **修复方向**：二选一，且必须与 roadmap 状态一致——
  1. 补齐定位（含 flip）、遮罩、焦点陷阱、Escape/外部点击关闭，并配套 `.razor.js`；
  2. 暂不补齐，则在 roadmap 标记为未完成，并避免在功能文档/示例页把它写成可用能力。
- **验收**：选定方案后，功能文档、roadmap、示例页三处描述一致；若补齐，需通过 Escape、外部点击、焦点回归三项检查。

---

## P2：Token 纪律与代码卫生

### REV-14 硬编码尺寸与字号收敛到 token

- [x] 已修复

- **证据（非 token 尺寸清单）**：
  - `Menu/Menu.razor.css`：`--aeterni-menu-row-height: 36px`、`padding: … 10px`、`border: 1px solid`、`width: 18px`、`--aeterni-icon-render-size: 14px`;
  - `Tag/Tag.razor.css`：`--sm` 字号 `10px`（低于 token 最小档 12px）、`min-height: 24px`、`padding: 0 10px`、图标 `12px`；
  - `Dialog/Notice/NoticeCard.razor.css`：字号 `13px` ×2、badge `30px`、`gap: 2px`、`stroke-width: 2`；
  - `Theme/ThemeSwitch.razor.css`：`48px/40px/56px`、`8px/38px`、`min-height: 38px/30px/46px`；
  - `Rating/Rating.razor.css`：项 `28px`、字形 `20px`；
  - `Dialog/DialogProvider.razor.css`：`width: min(100%, 520px)`、`max-height: min(720px, …)`；
  - `ComboBox.razor.css`：`max-height: min(320px, 45vh)`；`Popup/Popover.razor.css`：`12rem/32rem`；`Tooltip.razor.css`：`20rem/80vw`。
- **修复方向**：能映射到既有 token 的直接替换（如 `24px` → `var(--aeterni-height-xs)`、`12px` → `var(--aeterni-icon-size-xs)`）；确实缺少档位的（Menu 行高、通知字号、Popover 宽度）先在 token 层补语义 token，再引用，禁止在组件内新建第二套尺寸。
- **验收**：组件 CSS 中不再出现未注释的裸像素尺寸；`wwwroot/css/aeterni_ui.css` 仍只包含 Token 选择器（CI 门禁）。

### REV-15 死类、死 CSS、死标记与重复规则

- [x] 已修复

- **命名的类在 CSS 中不存在**：`aeterni-button--md`（`Size.Default`）、`aeterni-button--primary`（`Color.Default`）、`aeterni-tag--md`、`aeterni-progress--md`、`aeterni-theme-switch--md`、`aeterni-icon--default`、`is-dismissible`。
- **CSS 规则永不被匹配**：`.aeterni-button--default:disabled`、`.aeterni-button--default.is-disabled`（代码发出的是 `--primary`；通用 `.aeterni-button:disabled` 已覆盖，故无视觉后果，但注释与实现已脱节）。
- **死标记**：`NoticeCard.razor` 渲染 `<span class="…__separator" aria-hidden="true">/</span>`，而 CSS 设置 `display: none`。
- **重复规则**：`Card.razor.css` 中 `.aeterni-card.is-interactive.is-disabled` 出现两次。
- **修复方向**：确定"默认档位是否显式发出修饰类"的统一约定；删除死规则与死标记，或为其补上真实样式。
- **验收**：组件发出的每个类都能在某个样式表中找到规则（或明确属于约定允许的空类）；无 `display:none` 的死标记。

### REV-16 圆角体系不自洽

- [x] 已修复

- **证据**：`Button.razor.css` 基础规则用 `--aeterni-radius-button-lg`(8px)，`--aeterni-radius-button`(6px) 从未被引用；sm=4px / md=8px / lg=8px 不成比例；`Input` 用 `--aeterni-radius-input`(6px)，因此并排的 Button(8px) 与 Input(6px) 圆角不同；`Card` 用 12px，`Surface` 用 8px。
- **修复方向**：统一按尺寸档位映射（sm/md/lg 各取一档），让 Button 与 Input 在同一行内圆角一致，并明确 Card 与 Surface 的默认圆角关系。
- **验收**：默认档位下 Button 与 Input 圆角相同；同屏 Card 与 Surface 圆角差异有明确设计理由。

### REV-17 枚举重复与语义重叠

- [x] 已修复

- **证据**：`Enums/Color.cs`（含 Primary/Neutral）与 `Enums/Severity.cs`（缺 Primary/Neutral）并存，`DialogProvider.razor.cs` 里用 `ActionColor(Severity)` 手工映射；`Enums/Size.cs` 同时有 `Default` 与 `Medium`，`ThemeSwitch` 默认 `Size.Medium` 而其他组件默认 `Size.Default`。
- **修复方向**：合并语义枚举或明确各自边界并集中映射；去掉 `Size.Default`/`Size.Medium` 的重复语义，统一各组件默认值。
- **验收**：不存在两个含义相近的公共语义枚举，或映射只存在一处；所有组件的 `Size` 默认值一致。

### REV-18 库内硬编码文案与本地化入口

- [x] 已修复

- **证据**：`ComboBox.razor` 的 `"请选择"`（中文）与 `"Options"`；`Rating.razor.cs` 的 `AriaLabel = "Rating"`；`ThemeSwitch.razor.cs` 的 `"Theme mode"` 与标记中的 `System`/`Light`/`Dark`；`DialogProvider.razor` 的 `"Close alert"`/`"Close notification"`/`"Close dialog"` 与 `aria-label="Dialog"`；`Tag.razor.cs` 的 `DismissLabel = "Remove tag"`。
- **现象**：同一个库里同时出现中英文默认值，除少数 `AriaLabel` 参数外没有本地化入口。
- **修复方向**：统一文案来源（可选方案：`AeterniUIOptions` 增加可覆写文案表，或把默认值改为必填参数/可空并在文档说明），并统一默认语言。
- **验收**：库内不再出现未集中管理的用户可见文案；文档说明文案覆写方式。

### REV-19 代码格式双轨

- [x] 已修复

- **证据**：`Menu.razor.css`、`Radio.razor.css`、`Tooltip.razor.css` 为单行压缩风格，其余 `.razor.css` 为多行；`[Parameter]` 声明有"独立行 + XML 注释"（Button/Input/Checkbox/Switch/Rating/List/Tag）与"内联"（FormField/Progress/Radio/RadioGroup/Popup/Menu/Label）两种写法。
- **修复方向**：选定一种 CSS 书写格式与参数声明格式，统一全库；优先保持现状占比更高的多行风格。
- **验收**：新增代码与既有风格一致；无单行压缩样式文件。

### REV-20 Surface 与 Card 样式重复

- [x] 已修复

- **证据**：`Surface.razor.css` 与 `Card.razor.css` 各自实现 `--subtle/--elevated/--glass`、`is-bordered`、`--elevation-*`、`--padding-*`、`is-full-width`；Card 额外提供 `header/body/footer`。默认值相反：`Surface.Bordered = false`，`Card.Bordered = true`。
- **修复方向**：抽出共享的表面修饰规则（例如让 Card 复用同一套变量映射），并明确两者默认值的差异是否有意为之。
- **验收**：相同参数组合下 Surface 与 Card 的视觉结果一致（除 Card 的区块结构外）；默认值差异在文档中有说明。

### REV-21 滚动可用性与滚动条可发现性

- [x] 已修复

- **证据**：`DialogProvider.razor.css` 的 `.aeterni-dialog-provider__notice-region` 同时使用 `pointer-events: none`、`overflow-y: auto`、`scrollbar-width: none`；`ComboBox.razor.css` 的 `.aeterni-combobox__popover` 与 `::-webkit-scrollbar` 同样完全隐藏滚动条。
- **现象**：通知区域因 `pointer-events: none` 无法用滚轮滚动，长堆栈只能靠键盘；隐藏滚动条让长选项列表缺少可滚动提示。
- **修复方向**：让卡片自身保留 `pointer-events: auto` 的同时允许区域接收滚轮（例如只在卡片上禁用指针、区域本身可滚动）；滚动条改为细样式而非完全隐藏。
- **验收**：鼠标滚轮可滚动长通知堆栈与长下拉列表；仍能看出内容可滚动。

### REV-22 主题闪烁与 ThemeProvider DOM 契约

- [x] 已修复

- **证据**：`src/AeterniUI.Sample/wwwroot/index.html` 的早期主题脚本只在检测到 `__TAURI__` 时执行；`wwwroot/css/aeterni_ui.css` 中 `:root` 默认浅色，没有 `prefers-color-scheme` 兜底；`ThemeProvider.razor` 无任何标记，基类提供的 `Id/Class/Style` 参数被静默忽略。
- **现象**：普通浏览器下深色偏好用户会先看到浅色再切深色；`localStorage` 中记录的显式模式不参与早期应用；`ThemeProvider` 的参数契约与"继承 `AeterniComponent` 即拥有 DOM 参数"的约定不符。
- **修复方向**：把早期脚本移到库自身（CSS `prefers-color-scheme` 兜底 + 可选的内联脚本钩子）；并在文档中说明 `ThemeProvider` 不渲染 DOM、DOM 参数无效。
- **验收**：深色偏好下刷新无可见闪烁；`ThemeProvider` 的参数行为有文档说明。

### REV-23 字体栈与随包资源不一致

- [x] 已修复

- **证据**：`wwwroot/css/aeterni_ui.css` 声明 `'Inter'`、`'JetBrains Mono'` 为首选字体，但包内没有任何 webfont 资源（`wwwroot` 下仅有该 CSS）。
- **现象**：实际渲染落到 `Segoe UI` / `PingFang SC` / `-apple-system` / `Consolas`，跨平台视觉不一致，token 名称与实际不符。
- **修复方向**：二选一——随包提供字体或提供可选字体包并文档说明；或把字体栈改为纯系统栈，避免声明不可用字体。
- **验收**：字体栈中出现的字体要么随包提供，要么在文档中说明为可选依赖。

---

## P3：体验增强（可延后）

### REV-24 Tooltip 仅支持 top 定位

- [x] 已处理

- **说明**：需要 bottom/start/end 等方向时再补；可与 REV-11 的避让逻辑一起实现。
- **验收**：支持至少四个方向，且与 REV-11 的边界避让共用同一实现。

### REV-25 RadioGroup 缺 `Orientation` 与垂直布局

- [x] 已处理

- **说明**：`Enums/Orientation.cs` 目前只有 `ButtonGroup` 使用；`RadioGroup.razor.css` 只有 `flex-wrap` 横向流。垂直单选组是常见需求。
- **验收**：`Orientation="Vertical"` 可渲染单列布局，并与 `aria-orientation` 保持一致。

### REV-26 通知堆栈无数量上限与间距策略

- [x] 已处理

- **说明**：`.notice-region` 只有 `gap: var(--aeterni-spacing-3)` 和 `max-height`，没有同时存在卡片数量的约定，堆栈过长时体验下降。
- **验收**：文档明确最大同时显示数量，或实现自动折叠/合并策略。

### REV-27 Rating 自定义图标与默认星形尺寸不同

- [x] 已处理

- **证据**：`.aeterni-rating__item` 设置 `--aeterni-icon-render-size: var(--aeterni-icon-size-md)`（16px），而 `.aeterni-rating__glyph` 覆盖为 `20px`。传入 `Icon` 参数时使用 16px，默认星形是 20px。
- **验收**：自定义图标与默认星形在同一 Rating 内尺寸一致。

### REV-28 Dialog 打开时未补偿滚动条宽度

- [x] 已处理

- **证据**：`DialogProvider.razor.js` 通过 `document.body.style.overflow = 'hidden'` 锁定滚动，未补偿滚动条占位。
- **现象**：在经典滚动条平台（Windows/Linux，含 Windows 下的 Tauri）打开对话框时页面内容会横向位移。
- **验收**：打开/关闭对话框时背景内容不发生横向跳动。

### REV-29 Button 禁用态变量与 ButtonGroup 分隔线

- [x] 已处理

- **证据**：`Button.razor.css` 中 `--aeterni-button-disabled-background-subtle` 与 `--aeterni-button-disabled-background` 取同一值，但注释称 text/link 使用"更轻的表面"；`ButtonGroup.razor.css` 的 `--aeterni-button-group-divider` 用 `color-mix(text-inverse 28%)` 适配实心按钮，outline 变体另有 `border-strong` 覆盖。
- **修复方向**：要么让 text/link 禁用态真的更轻，要么修正注释；确认分隔线在浅色 outline 组合下的表现。
- **验收**：注释与实现一致；各变体组合下连接态分隔线清晰可辨。

---

## 建议的修复批次

| 批次 | 范围 | 说明 |
| --- | --- | --- |
| 批次 1 | REV-01 ~ REV-04 | 纯 CSS/标记修复，不涉及公共 API，可一次性提交并做视觉复核 |
| 批次 2 | REV-05 ~ REV-13 | 含 ARIA 结构变化，需同步 `current-features` 与示例页演示 |
| 批次 3 | REV-14 ~ REV-23 | Token 与卫生类，可与相关组件的后续改动合并，避免大范围无功能改动 |
| 批次 4 | REV-24 ~ REV-29 | 体验增强，按需排期 |

## 每项修复的固定交付物

1. 组件实现（`.razor` / `.razor.cs` / `.razor.css`，必要时 `.razor.js`）。
2. 示例页中可交互的演示或状态对照（AGENTS.md 提交自检要求）。
3. 公共 API 或行为变化时同步 `current-features.zh-CN.md`；状态变化同步 `component-roadmap.zh-CN.md`。
4. 遵守 CSS 门禁：不得在 `wwwroot/css/aeterni_ui.css` 写入组件样式，不得新建第二套尺寸/颜色/圆角体系。

## 验收门禁

```bash
dotnet build aeterni_ui.slnx
bash scripts/check-docs.sh
node --check <改动的>.razor.js
```

- 修改 Token 层时额外确认 `wwwroot/css/aeterni_ui.css` 仍只包含 Token 选择器。
- 修改图标清单或组件内字形时额外执行 `node scripts/generate-fontawesome-icons.mjs --check`。
- 视觉类修复（P0/P1）建议在浅色与深色两种主题下各复核一次，并确认 `prefers-reduced-motion` 下动效降级仍生效。
