# AeterniUI 组件审阅待办

文档版本：`10.7.0`

文档状态：第一轮 29 项已在 v10.2.0 修复；第二轮 21 项（REV-30 ~ REV-50）、第三轮 8 项（REV-51 ~ REV-58）、第四轮 4 项（REV-59 ~ REV-62）已在 v10.4.0 修复 / 收尾；REV-13 与 REV-56 在 v10.4.0 收尾；第五轮 1 项（REV-63）在 v10.5.0 修复。当前无未完成条目。

本文档记录四轮全量审阅发现的问题：第一轮覆盖 token 层、22 个组件的 `.razor.css`、`.razor` 标记与关键 `.razor.cs`/`.razor.js`；第二轮（v10.4）针对「品牌色/语意色在全组件的落地」与「组件结构稳定性」重做审核；第三轮（v10.4）回应「中性容器表面又带紫色」的报告并做样式体系一致性扫描；第四轮（v10.4）回应「界面看起来不干净」，重做文字色阶（Apple label 模型）与分隔线、清理浑浊的 chip 混色。问题按优先级排列，供后续分批修复和验收追踪。

## 文档边界

- 本文档只记录**问题、证据、修复方向和验收条件**，不记录已实现能力。
- 修复涉及公共 API 或组件行为变化时，必须同步 [`current-features.zh-CN.md`](current-features.zh-CN.md)。
- 修复涉及未完成能力时，状态只在 [`component-roadmap.zh-CN.md`](component-roadmap.zh-CN.md) 记录；未补齐前不得在功能文档或索引中写成已实现。
- 修复涉及项目结构、入口或构建脚本时，同步 [`project-index.zh-CN.md`](project-index.zh-CN.md)。
- 组件设计契约以 [`component-design-guidelines.zh-CN.md`](component-design-guidelines.zh-CN.md) 为准；本清单不新增契约。

## 审阅方法与可信度

- 两轮审阅均为**静态审阅**：未运行应用、未做截图比对。视觉结论来自 CSS 规则推导和 token 数值计算。
- 对比度数值由 token 十六进制值按 WCAG 2.1 相对亮度公式计算（含 `color-mix` 的 sRGB 通道混合与半透明叠加），修复前需实测确认。
- 第二轮的**结构结论**额外用 `HtmlRenderer` 与自定义 `Renderer` 渲染真实组件做了验证：确认静态 `Card`/非选择态 `List` 的渲染树中不存在 `onclick`/`onkeydown` 帧、`NoticeCard`/`ListItem` 能接受消费者 `Class`/`Style`/`Visible`、`Progress` 不再输出 inline `width`、ARIA 状态属性输出为字符串而不是最小化布尔属性。
- "证据位置"给的是文件加选择器/成员名，而不是行号，便于在后续重构后继续定位。

## 修复摘要（v10.2.0）

| 编号 | 修复位置 | 关键改动 |
| --- | --- | --- |
| REV-01 | `Switch.razor.css`、Token 层 | 新增 `--aeterni-state-background-thumb`（浅色取 surface、深色取 `gray-0`），滑块不再复用反色文字 Token |
| REV-02 | `Button.razor.css` | 新增 `--aeterni-button-loading-surface/-veil/-foreground`：所有变体统一为“自身表面半透明层 + `blur(2px)`”，遮罩向外扩展一个边框宽度以盖住控件边框，Button 因此不再裁剪子元素 |
| REV-03 | `DialogProvider.razor` | 每个 `ToastPosition` 只渲染一个位置容器，Alert 在前、Toast 在后纵向堆叠 |
| REV-04 | Token 层、`Button.razor.css` | 实心语意控件改用 `--aeterni-color-on-semantic`（浅色近黑墨、深色反色墨）；v10.3 色彩体系优化后取消「hover/active 切换字色」的做法，改为三态同一字色（品牌向下取档、语意向白提亮），因此删除了 `--aeterni-color-on-semantic-strong` |
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
| REV-26 | `current-features` §25 | 明确 Toast 上限为 `MaxToastCount`（默认 5）、超出时移除最早一条、Alert 由调用方控制生命周期；容器超出视口时可滚动 |
| REV-27 | `Rating.razor.css` | 自定义 `Icon` 与内置星形统一使用 `--aeterni-icon-size-lg`，删除 glyph 覆盖 |
| REV-28 | `DialogProvider.razor.js` | 锁定滚动时按滚动条宽度补偿 `body` 右内边距，关闭时恢复 |
| REV-29 | `Button.razor.css`、`ButtonGroup.razor.css` | 文字/链接禁用态改用更轻的半透明表面（与注释一致）；分隔线取自按钮自身前景色，实心变体全部可辨 |

### 后续回归中修正的实现问题

第一轮修复后复测发现三个实现缺陷，均已修正（详见上表 REV-02 / REV-10 / REV-19）：

- `ListItem.razor` 在标记上下文使用了 `/* */`，Razor 会把它当作文本输出，导致 List 预览出现可见注释文字；已改为 `@* *@`。
- `Radio` 的根类原本通过 `@attributes` 落在原生输入上，而显式 `class` 在源码中位于 `@attributes` 之后，会覆盖它，因此 `aeterni-radio` 及其状态类从未出现在 DOM 中；改为由根 `<label>` 承载组件类与输入属性分离后，尺寸 Token 与状态样式才真正生效（同一问题也影响 `RadioGroup` 的 `Orientation` 类和 `Tooltip` 的消费者 `Class` 参数，一并修正）。

约定：`@attributes="BuildAttributes()"` 所在元素不要再写显式 `class`（除非该属性集合不含 class），需要条件类名时把它们放进 `BuildClass()`。

### 未按“补齐实现”处理的条目

- **REV-13**：第一轮选择“暂不补齐 + 三处描述一致”，v10.4 已按另一个分支补齐（共享浮层能力抽取 + Popover 完整版）。
- **REV-23**：审阅允许“随包提供字体”或“文档说明为可选依赖”，本次选择后者，因此字体栈仍保留 Inter / JetBrains Mono 名称。

## 修复摘要（v10.4.0）

第二轮审阅主题是「品牌色/语意色在全组件的落地」与「组件结构稳定」。色彩部分先把两套主题下每条前景/背景组合的对比度算出来，再按「填充档不当 ink 用」的规则改组件；结构部分把所有绕过基类契约、重复标记、双套映射和无效 ARIA 值统一收敛。

| 编号 | 修复位置 | 关键改动 |
| --- | --- | --- |
| REV-30 | `Icon.razor.css`、`Icon.razor.cs` | 六个命名色改用「强调文字形态」`--aeterni-color-*-text`（浅色 4.6~6.7:1，原来 success/warning 只有 2.2:1）；`Color.Default` 继续用 `currentColor`；删除第二套颜色 switch，收敛到 `ComponentClass.ForColor` |
| REV-31 | `NoticeCard.razor.css`、`DialogProvider.razor.css` | 拆出填充与文字两个 accent 角色：`--aeterni-dialog-accent`（卡片底色/徽标底色/进度环）与 `--aeterni-dialog-accent-ink`（徽标图标、弹窗头部图标，1.9~3.2:1 → 3.7~5.6:1） |
| REV-32 | `FormField.razor.css` | 必填星号与错误文字改用 `--aeterni-color-danger-text`（3.55:1 → 4.57:1），并把三条单行规则恢复为多行格式 |
| REV-33 | Token 层、`Menu.razor.css` | 新增说明并把 `--aeterni-state-color-selected/-checked` 指向 `--aeterni-color-brand-text`（选中项文字 4.03~4.37:1 → 5.0~7.4:1）；选中项图标同步用文字形态 |
| REV-34 | Token 层 | 浅色 `--aeterni-text-tertiary` 由 `#6E6E79` 改为 `#64646E`，在中性面上达到 4.7:1（原 4.08:1） |
| REV-35 | `Progress.razor(.cs/.css)` | 进度条填充改走强调文字形态，轨道用同色相 12% 染色：填充/轨道 1.78~3.98:1 → 3.2~4.6:1（浅色）、5.5~7.5:1（深色），且轨道与页面 1.23:1 → 1.46:1 |
| REV-36 | `Rating.razor.css` | 填充星改用 `--aeterni-color-warning-text`（2.20:1 → 6.40:1），空星改用 `--aeterni-text-tertiary`（2.44:1 → 5.85:1） |
| REV-37 | `Button.razor.css` | `--neutral` 的 hover/pressed 改为沿「向墨色收敛」方向取色（86%/76%），浅色主题不再出现 hover 变浅的反相反馈 |
| REV-38 | `ButtonGroup.razor.css` | 透明变体的连接分隔线改用按钮自身强调色的 45% 染色，不再用中性 `--aeterni-border-strong`，避免描边组合内出现异色分隔线 |
| REV-39 | `DialogProvider.razor.css`、`NoticeCard.razor.css` | `Severity.Default → Color.Primary` 会发出 `--primary` 修饰类却没有规则；把 `--primary` 并入基础规则选择器组（沿用 Button 先例），消除死类 |
| REV-40 | `NoticeCard.razor(.cs)` | 从 `BuildCardClass()` 改为 `BuildClass()`/`BuildAttributes()` 覆写：`Id`/`Class`/`Style`/`Visible`/`AdditionalAttributes` 重新生效，`Severity → 颜色` 收敛到 `ComponentClass.ToColor()` |
| REV-41 | `ListItem.razor(.cs)` | 两套重复标记合并为单一根元素；class 改用 `ClassBuilder`，根属性改走 `BuildAttributes()`，消费者 `Class`/`Style`/`Visible` 生效，选项 id 仍由 `aria-activedescendant` 契约优先 |
| REV-42 | `Card.razor(.cs)`、`List.razor(.cs)`、`ListItem.razor.cs` | 非交互卡片 / 非选择态列表不再绑定 DOM 事件：改用默认 `EventCallback<T>` 条件绑定（渲染树中无 `onclick`/`onkeydown` 帧，已用自定义 `Renderer` 验证） |
| REV-43 | `Progress.razor(.cs/.css)` | 去掉 inline `width` 与两处 `!important`：宽度改走 `--aeterni-progress-value` 自定义属性，不确定态用特异性覆盖；补 `@ref="RootElement"` |
| REV-44 | `Tag.razor.cs` | `Color.Default` 时不再输出空修饰类 `aeterni-tag--`，改用 `ComponentClass.ForColor` |
| REV-45 | `Radio.razor.cs`、`Icon.razor.cs` | 删除组件内的第二套 `SizeClass`/`ColorClass` 映射，统一走 `ComponentClass.ForSize`/`ForColor` |
| REV-46 | `NoticeCard.razor.css`、`DialogProvider.razor.css`、`Tag.razor.css`、`ComboBox.razor.css` | 图标尺寸统一由 `--aeterni-icon-render-size` 传递，删除 `font-size` 间接生效与 `::deep svg { width/height }` 覆盖；`14px` 裸像素改为 `--aeterni-icon-size-sm` |
| REV-47 | `NoticeCard.razor(.cs)`、`Button.razor.css`、Token 层 | 删除死类与重复：`is-default`、无消费者的 `data-aeterni-notice-card`、恒等于 `foreground` 的 `--aeterni-button-foreground-strong`、四份重复的「System-preference fallback」注释、四个语义变体上重复的同段注释 |
| REV-48 | `ThemeSwitch.razor(.cs)`、6 个组件 | 三个选项改为单一模板循环（`aria-pressed` 与选中类不再三处重复）；`Label`/`FormField`/`Progress`/`Popover`/`PopupHost`/`ThemeSwitch` 补 `@ref="RootElement"`，`Element`/`ElementChanged` 契约恢复生效 |
| REV-49 | `Rating.razor`、`ComboBox.razor`、`Switch.razor`、`ThemeSwitch.razor`、`ListItem.razor.cs` | ARIA 状态属性改为字符串：布尔值会渲染成最小化属性（`aria-selected` 且无值），读屏会当成无效值；现已输出 `"true"`/`"false"` |
| REV-50 | `DialogProvider.razor.css`、示例页 | 抖动动画的硬编码 `320ms` 改用 `--aeterni-duration-slow`，通知位移量补注释；示例新增「色彩与语义」页（`/components/colors`）用于两种主题下验收 |

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

## 第二轮总览（v10.4.0）

所有条目均已修复并通过 `dotnet build aeterni_ui.slnx`、`bash scripts/check-docs.sh` 与渲染验证；对比度数值由 Token 值计算。

| 编号 | 优先级 | 组件/范围 | 一句话问题 |
| --- | --- | --- | --- |
| REV-30 | P0 | Icon | 六个命名色用填充档做前景，浅色下 success/warning 只有 2.2:1 |
| REV-31 | P0 | NoticeCard / DialogProvider | Alert/Toast 徽标与弹窗头部图标用填充档做 ink，最低 1.9:1 |
| REV-32 | P0 | FormField | 必填星号与错误文字用 `danger` 填充档，只有 3.55:1 |
| REV-33 | P0 | Token 层 / Menu | 选中项文字取填充档，在自己的选中底上只有 4.0~4.4:1 |
| REV-34 | P1 | Token 层 | 浅色三级文字在中性面上只有 4.08~4.47:1 |
| REV-35 | P1 | Progress | 语意进度条填充与轨道只有 1.78~2.88:1，且轨道与页面几乎同亮度 |
| REV-36 | P1 | Rating | 填充星 2.20:1、空星 2.44:1，均低于图形 3:1 |
| REV-37 | P2 | Button | `--neutral` 的 hover 在浅色主题下反向变浅 |
| REV-38 | P2 | ButtonGroup | 透明变体的连接分隔线用中性色，与两侧强调色边框色相不一致 |
| REV-39 | P2 | DialogProvider / NoticeCard | `Severity.Default` 发出的 `--primary` 类没有规则（死类） |
| REV-40 | P1 | NoticeCard | 绕过 `BuildAttributes()`，`Id`/`Class`/`Style`/`Visible` 全部失效 |
| REV-41 | P1 | ListItem | 两套重复标记 + 手写 class 拼接，消费者属性失效 |
| REV-42 | P1 | Card / List / ListItem | 静态卡片与非选择态列表仍绑定 DOM 事件监听器 |
| REV-43 | P2 | Progress | inline `width` + 两处 `!important` 覆盖，且没有 `@ref` |
| REV-44 | P2 | Tag | `Color.Default` 输出空修饰类 `aeterni-tag--` |
| REV-45 | P2 | Radio / Icon | 组件内维护第二套尺寸/颜色映射 |
| REV-46 | P2 | 全局 | 图标尺寸存在两套机制（`font-size` 间接生效与 `::deep svg` 覆盖） |
| REV-47 | P2 | 全局 | 死类、无消费者的 data 属性、恒等 Token 与重复注释 |
| REV-48 | P2 | ThemeSwitch + 6 个组件 | 选项标记三处重复；六个组件缺 `@ref`，`Element`/`ElementChanged` 失效 |
| REV-49 | P1 | Rating / ComboBox / Switch / ThemeSwitch / ListItem | 布尔型 ARIA 状态渲染成最小化属性，读屏读到无效值 |
| REV-50 | P3 | DialogProvider / 示例页 | 硬编码动画时长与未注释的位移量；缺一个可用于验收色彩改动的示例页 |

---

## 第三轮总览（v10.4.0）

| 编号 | 优先级 | 范围 | 一句话问题 |
| --- | --- | --- | --- |
| REV-51 | P0 | Token 层 | 中性容器表面重新带上蓝紫偏移（+2~+6），大面积看起来又变成紫色 |
| REV-52 | P1 | Menu | 禁用态用整行 `opacity`，与全库禁用 Token 的表达不一致 |
| REV-53 | P2 | 全局 | `spacing` / `gap` / `padding` / `margin` 四套别名并存且组件用法不一致 |
| REV-54 | P2 | Input / Textarea / ComboBox | 同一条四属性过渡声明在三处重复 |
| REV-55 | P2 | Menu / Tag / NoticeCard / Token 层 | 已有角色 Token 无消费者，旁边却写着字面量；`bg-hover/-active` 语义误导 |
| REV-56 | P2 | Switch / ComboBox / Rating | 没有 `Size` 档位，无法与同排 Input 对齐（记入 roadmap，未实现） |
| REV-57 | P2 | Token 层 / 文档 | 浮层圆角四档并存但无成文依据 |
| REV-58 | P3 | Token 层 / 文档 | `--aeterni-surface-soft` 与 `--aeterni-bg-secondary` 同为「次级底」但取值机制不同，需要写明是有意区别 |

---

## 第四轮总览（v10.4.0）

主题：界面「不干净」——文字色阶、分隔线与浑浊的彩色 chip。

| 编号 | 优先级 | 范围 | 一句话问题 |
| --- | --- | --- | --- |
| REV-59 | P0 | Token 层 + Menu / ThemeSwitch / 示例页 | 文字用四个手调 hex：主色过黑（17:1 发硬）、二级与三级几乎同色（6.8 vs 5.9 看起来脏）、且固定 hex 在着色表面上偏色 |
| REV-60 | P1 | Tag | 标签文字是「亮色填充档 52% + 近黑 48%」的混色，绿/黄族混出橄榄、褐色 |
| REV-61 | P2 | Card / DialogProvider / Token 层 | 结构分隔线用 7% alpha（1.17:1），卡片头/底分界看起来像污渍 |
| REV-62 | P2 | Token 层 | `--aeterni-color-on-semantic` 用蓝黑 primitive `#0B0F19`（B-R=+14），给实心绿/黄按钮上的文字带蓝调 |

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

- [x] 已修复（v10.4 补齐）

- **证据**：
  - `Popup/Popover.razor.css` 中 `.aeterni-popover` 没有任何 `position` 规则，弹层不可锚定；
  - 同文件 `.aeterni-popup-host { position: relative; display: contents; }` —— `display: contents` 不生成盒子，`position` 失效（死 CSS）；
  - `Popover.razor.cs` 在 `Modal=true` 时只添加 `is-modal` 类，而 CSS 中不存在该规则；
  - 目录内没有 `.razor.js`。
- **现象**：Popover 没有定位、没有遮罩、没有 `aria-modal`、没有焦点管理、没有 Escape 与外部点击关闭，`Modal` 参数实际无效。
- **修复方向**：二选一，且必须与 roadmap 状态一致——
  1. 补齐定位（含 flip）、遮罩、焦点陷阱、Escape/外部点击关闭，并配套 `.razor.js`；
  2. 暂不补齐，则在 roadmap 标记为未完成，并避免在功能文档/示例页把它写成可用能力。
- **当时的处理**：先取方案 2（删死 CSS、让 `PopupHost` 成为真实定位容器、文档与示例保持基础版描述），并把补齐列入第三阶段前置能力。
- **v10.4 收尾**：先抽出共享浮层模块 `wwwroot/js/aeterni_floating.js`（`fitsSide`/`oppositeSide`/`clampCenteredShift`/`clampAlignedShift`、`createFocusTrap`、引用计数的 `lockScroll`），再按方案 1 补齐：`PopupPlacement` 四方位 + 视口翻转与交叉轴贴边、Escape、外部指针、模态遮罩与 `aria-modal`、Tab 焦点陷阱（关闭后焦点回归）、背景滚动锁（带滚动条宽度补偿）；Tooltip 与 DialogProvider 同步改用共享实现。
- **验收**：Escape、外部点击、焦点回归三项均通过渲染与代码审查；功能文档、roadmap、示例页三处描述一致（不再写“基础版”）。

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

---

## 第二轮 P0：色彩可读性

### REV-30 Icon 命名色把填充档当 ink 用

- [x] 已修复

- **证据**：`Icon.razor.css` 的 `.aeterni-icon--primary/success/warning/danger/info` 全部取 `--aeterni-color-*-default`。
- **现象**：图标是 ink，不是实心表面。按 Token 值计算，success/warning 在浅色页面只有 **2.20/2.22:1**，danger 3.55:1、info 4.02:1、primary 4.91:1，均低于图形 3:1 与正文 4.5:1。
- **修复**：六个命名色改用强调文字形态 `--aeterni-color-*-text`（4.6~6.7:1）；`Color.Default` 继续留给 `currentColor`。
- **验收**：六个命名色在两种主题下均 ≥ 4.5:1；`/icons` 与 `/components` 中的示例图标肉眼可辨。

### REV-31 Alert/Toast 徽标与弹窗头部图标

- [x] 已修复

- **证据**：`NoticeCard.razor.css` 的 `--aeterni-dialog-accent` 同时用于卡片底色、徽标底色、进度环和徽标图标颜色（`__notice-icon`/`__notice-glyph`）；`DialogProvider.razor.css` 的 `__icon` 同样取它。
- **现象**：accent 是填充档。徽标图标画在「accent 16% 透明」的淡色徽标上，浅色主题下 success 1.93:1、warning 1.91:1，几乎不可见；弹窗头部图标同理。
- **修复**：拆成两个角色——`--aeterni-dialog-accent`（填充：卡片底色、徽标底色、进度环）与 `--aeterni-dialog-accent-ink`（文字：徽标图标、头部图标）。
- **验收**：四种语意在浅色/深色下徽标图标对比度 ≥ 3:1（实测 3.65~7.97:1）；卡片底色与进度环仍取填充档。

### REV-32 FormField 反馈文字

- [x] 已修复

- **证据**：`FormField.razor.css` 的 `__required` 与 `__error` 取 `--aeterni-color-danger-default`。
- **现象**：`danger-500` 在浅色页面上只有 **3.55:1**，12px 正文需要 4.5:1。
- **修复**：改用 `--aeterni-color-danger-text`（4.57:1）。
- **验收**：必填星号与错误文案在两种主题下 ≥ 4.5:1；`Invalid` 状态演示仍可辨。

### REV-33 选中态文字取填充档

- [x] 已修复

- **证据**：`Menu.razor.css` 的 `.aeterni-menu__item.is-selected { color: var(--aeterni-state-color-selected); }`；Token 层把该别名指向 `--aeterni-color-brand-default`。
- **现象**：`selected`/`checked` 是**前景**别名，却指向填充档。选中项文字画在自己的选中底上（浅色 `rgba(121,90,217,.13)`），实测只有 **4.03~4.37:1**；hover 叠加后更低。
- **修复**：别名改指 `--aeterni-color-brand-text`（5.02~5.93:1），深色主题 7.13~7.44:1；组件内不再各自取档。
- **验收**：Menu 选中项、`--aeterni-control-foreground-selected` 的所有消费者在两种主题下 ≥ 4.5:1。

---

## 第二轮 P1：一致性与可访问性

### REV-34 浅色三级文字在中性面上不足

- [x] 已修复

- **证据**：浅色 `--aeterni-text-tertiary: #6E6E79`。
- **现象**：该 Token 被 ComboBox 箭头、Menu 分组标题、ThemeSwitch 未选中项、弹窗关闭字形使用。在 `--aeterni-bg-secondary` 上只有 **4.47:1**，在 `--aeterni-bg-tertiary` 上只有 **4.08:1**。
- **修复**：改为 `#64646E`（页面 5.85:1、secondary 5.19:1、tertiary 4.74:1）。
- **验收**：该 Token 在三个中性面（surface / secondary / tertiary）上均 ≥ 4.5:1；次级文字的层级差仍然可辨。

### REV-35 Progress 填充与轨道对比不足

- [x] 已修复

- **证据**：`Progress.razor.css` 中填充取 `--aeterni-color-*-default`，轨道取 `--aeterni-bg-tertiary`。
- **现象**：填充与轨道实测 success **1.80:1**、warning 1.78:1、neutral 2.64:1、danger 2.88:1（浅色），低于图形 3:1；同时轨道与页面只有 1.23:1，低进度时看不出进度条有多长。
- **修复**：填充改走强调文字形态，轨道用同色相 12% 染色。
- **验收**：六族的填充/轨道对比在浅色 3.15~4.70:1、深色 5.67~7.90:1；轨道与页面 ≥ 1.4:1；`indeterminate` 与 `prefers-reduced-motion` 行为不变。

### REV-36 Rating 星形

- [x] 已修复

- **证据**：`Rating.razor.css` 中 `.is-filled` 取 `--aeterni-color-warning-default`，空星取 `--aeterni-state-color-disabled`。
- **现象**：填充星是实心图形，浅色下只有 **2.20:1**；空星 2.44:1，两个状态都看不出形状。
- **修复**：填充星改用 `--aeterni-color-warning-text`（6.40/10.51:1），空星改用 `--aeterni-text-tertiary`（5.85/6.38:1）。
- **验收**：两种状态在两种主题下的表面比度均 ≥ 3:1；填充/空星仍能一眼区分。

### REV-40 NoticeCard 绕过基类属性契约

- [x] 已修复

- **证据**：`NoticeCard.razor` 的根元素写 `class="@BuildCardClass()"` 并手写 `role`/`aria-live`/`data-aeterni-notice-card`，没有 `@attributes="BuildAttributes()"`。
- **现象**：基类提供的 `Id`/`Class`/`Style`/`Visible`/`AdditionalAttributes` 全部静默失效；`Class` 连 `ClassBuilder(Class)` 都没传入；另外 `SeverityClass` 是第二份 Severity→class 映射，`is-default` 与 `data-aeterni-notice-card` 没有任何消费者。
- **修复**：改为 `BuildClass()` + `BuildAttributes()` 覆写，颜色映射收敛到 `ComponentClass.ToColor()`，删除死类与无消费者的 data 属性。
- **验收**：渲染验证确认根元素拿到消费者 `Class`/`Style`，`Visible=false` 输出 `hidden`，`role`/`aria-live` 仍在（`assertive`/`polite` 保持原规则）。

### REV-41 ListItem 两套重复标记

- [x] 已修复

- **证据**：`ListItem.razor` 用 `@if (Interactive)` 写了两份完全相同的行内容，两份都直接输出 `class="@BuildItemClass()"`；`BuildItemClass()` 用 `List<string>` + `string.Join` 手工拼 class，不经 `ClassBuilder`。
- **现象**：同一行内容有两份，后续只改其中一份就会分叉（第一轮的 `ListItem` 注释事故就是同一类问题）；消费者 `Class`/`Style`/`Visible` 同样失效。
- **修复**：单一根元素（带 `@ref`）承载两份语义，`role`/`aria-selected`/`aria-disabled`/选项 id 只在可选态输出，class 改由 `BuildClass()` 构建。
- **验收**：渲染验证确认非选择态没有 `role`、选择态有 `role="option"` 且 id 仍为 `{listId}-option-…`（`aria-activedescendant` 契约不变）。

### REV-42 静态容器仍绑定事件

- [x] 已修复

- **证据**：`Card.razor` 无条件写 `@onclick`/`@onkeydown`；`List.razor` 无条件写 `@onkeydown`；`ListItem.razor` 同样无条件写 `@onclick`。
- **现象**：静态卡片与展示型列表也会为每个实例注册 DOM 监听器（长列表中纯属开销），且事件处理器内部再写一次状态判断。
- **修复**：处理器改为返回 `EventCallback<T>` 的属性，非交互时返回 `default`（渲染树中不产生处理帧，且不需要可空委托）。`List` 的处理前置判断改由绑定条件承担，`ListItem` 保留点击处理器内部的数据校验。
- **验收**：自定义 `Renderer` 检查渲染帧：静态 `Card` 无 `onclick`/`onkeydown`，交互 `Card` 两个都在；展示型 `List` 无 `onkeydown`，选择态 `List` 有。

### REV-49 布尔型 ARIA 状态被渲染成最小化属性

- [x] 已修复

- **证据**：`ListItem.razor.cs`（`aria-selected`）、`Switch.razor`（`aria-checked`）、`ThemeSwitch.razor`（`aria-pressed`）、`Rating.razor`（`aria-checked`）、`ComboBox.razor`（`aria-expanded`、`aria-selected`）直接绑定 `bool`。
- **现象**：Blazor 把 `bool` 属性值渲染为最小化属性，即输出裸的 `aria-selected` / `aria-checked`（无值），为 `false` 时直接省略；两者对读屏都是无效值。库内 `Menu` 已使用 `? "true" : "false"` 的写法，属于同一库内的两种契约。
- **修复**：全部改为显式字符串 `"true"/"false"`。
- **验收**：渲染验证确认输出 `aria-selected="true"`、`aria-checked="false"`、`aria-pressed="true"`、`aria-expanded="false"`；Rating 仍然只有一个 `aria-checked="true"`。

---

## 第二轮 P2：Token 纪律与结构卫生

### REV-37 Button 中性填充 hover 反向

- [x] 已修复

- **证据**：`.aeterni-button--neutral` 的 `--aeterni-button-background: var(--aeterni-bg-tertiary)`、`-hover: var(--aeterni-bg-secondary)`；语义变体则用 `color-mix(填充 88%, gray-0)`。
- **现象**：浅色主题 `bg-secondary` 比 `bg-tertiary` **更浅**，hover 反而变淡（反相反馈）；深色主题方向又相反，两个主题不同步；`-active` 还混入了紫色的 `--aeterni-state-background-active`。
- **修复**：中性色阶统一沿「向墨色收敛」方向移动（86% / 76%），两类填充方向一致且与主题无关。
- **验收**：浅色 hover/pressed 比 rest 更深、深色更亮；正文对比度均 ≥ 8:1。

### REV-38 ButtonGroup 连接分隔线色相

- [x] 已修复

- **证据**：透明变体（outline/ghost/text/link）的连接分隔线取 `--aeterni-border-strong`。
- **现象**：描边组合的两侧边框是强调色，共享边却是中性灰，同一组内出现异色线。
- **修复**：改用按钮自身强调色的 45% 染色。
- **验收**：连接态分隔线在六个色族 × 四种变体下均与两侧边框同色相且更轻。

### REV-39 `--primary` 死类

- [x] 已修复

- **证据**：`Severity.Default → Color.Primary`，`ComponentClass.ForColor` 因此输出 `aeterni-dialog-provider__dialog--primary` / `--alert--primary` / `--toast--primary`，但两个样式表只有 `--info/--success/--warning/--danger` 规则。
- **修复**：把 `--primary` 并入基础规则选择器组（与 `Button.razor.css` 的 `.aeterni-button--primary` 同一先例）。
- **验收**：组件发出的每个类名都能在某个样式表中找到匹配规则（包括 `Severity.Default` 路径）。

### REV-43 Progress 绕开 Token 与样式源

- [x] 已修复

- **证据**：`Progress.razor` 用 `style="width: @(Percentage)%"` 写内联宽度，CSS 再用两处 `!important` 覆盖它（`is-indeterminate` 与 reduced-motion）；根元素没有 `@ref="RootElement"`。
- **修复**：宽度改走 `--aeterni-progress-value`（用 `StyleBuilder` 输出，不确定态不输出），删除两处 `!important`；补 `@ref`。
- **验收**：渲染验证确认输出 `style="--aeterni-progress-value: 62%;"`、不确定态无 inline style；`is-indeterminate` 仍由特异性覆盖宽度。

### REV-44 Tag 空修饰类

- [x] 已修复

- **证据**：`Tag.BuildClass()` 写 `.Add($"aeterni-tag--{ColorClass}")`，而 `ColorClass` 在 `Color.Default` 时为 `null`。
- **现象**：输出名为 `aeterni-tag--` 的空修饰类（匹配不到任何规则）。
- **修复**：直接用 `.Add(ComponentClass.ForColor("aeterni-tag", Color))`。
- **验收**：渲染验证确认默认色只输出 `aeterni-tag aeterni-tag--default`。

### REV-45 组件内的第二套映射

- [x] 已修复

- **证据**：`Radio.razor.cs` 自带 `SizeClass` switch（产出 `aeterni-radio--sm/--lg`）；`Icon.razor.cs` 自带 `ColorClass` switch（并把 `Color.Default` 映射为 `"default"`）。
- **现象**：与 `ComponentClass` 的「默认档不输出修饰类」约定并行，两处逻辑需要同步维护；Icon 的 `--default` 也没有规则。
- **修复**：两处均改用 `ComponentClass.ForSize` / `ForColor`。
- **验收**：`grep SizeClass/ColorClass` 在组件内只剩对 `ComponentClass` 的调用；渲染输出不变。

### REV-46 图标尺寸两套机制

- [x] 已修复

- **证据**：`NoticeCard.razor.css` 用 `::deep svg { width/height: 16px }` 覆盖徽标内图标，同时 `__notice-glyph` 又用 `font-size` 让其 1em 回落生效；`DialogProvider` 与 `Tag` 的关闭字形同样用 `font-size`；`ComboBox` 箭头写死 `14px`。
- **现象**：徽标内自定义图标与内置严重度字形的尺寸来自两条不同规则，`--aeterni-icon-render-size` 声明形同虚设；`font-size` 方案依赖 Icon 的 1em 回落，属于间接生效。
- **修复**：统一在祖先元素上设 `--aeterni-icon-render-size`，删除 `::deep svg` 覆盖与 `font-size` 间接生效；`14px` 改为 `--aeterni-icon-size-sm`。
- **验收**：徽标内的自定义图标与内置字形同尺寸（16px）；Tag/弹窗关闭字形为 14px/16px；组件 CSS 中不再用 `font-size` 控制图标尺寸。

### REV-47 死类、恒等 Token 与重复注释

- [x] 已修复

- **证据**：Token 层有四份完全相同的「System-preference fallback」注释；`Button.razor.css` 在四个语义变体上重复同一段注释，且 `--aeterni-button-foreground-strong` 在基础规则已恒等于 `--aeterni-button-foreground`，四个变体又各自重写一次；`NoticeCard` 的 `is-default` 与 `data-aeterni-notice-card` 无消费者。
- **修复**：注释各保留一份（变体处改为指向 `--success` 的短注释），删除 `--aeterni-button-foreground-strong` 及其全部使用点（hover/active 直接用 `--aeterni-button-foreground`），删除无消费者的类与属性。
- **验收**：`grep foreground-strong` 无结果；每段共享行为只有一处注释。

### REV-48 ThemeSwitch 重复标记与缺失的 `@ref`

- [x] 已修复

- **证据**：`ThemeSwitch.razor` 手写三个选项按钮，标签分散在 `SystemLabel`/`LightLabel`/`DarkLabel` 三个属性；`Label`/`FormField`/`Progress`/`Popover`/`PopupHost`/`ThemeSwitch` 的根元素没有 `@ref="RootElement"`。
- **现象**：三处选项标记需要同步修改（选中类、`aria-pressed`、禁用态、回调参数），是 REV-10 同类分叉风险；缺 `@ref` 的组件即使声明了 `Element` / `ElementChanged` 也不会有任何回调。
- **修复**：选项改为 `foreach` 单一模板 + `ModeLabel(mode)`，模式顺序写成显式数组并注明它与滑块的列偏移契约相关；六个组件补 `@ref`。
- **验收**：渲染验证确认三个选项的类名、`aria-pressed` 与 `data-mode` 输出不变；所有带可见根元素的组件都绑定了根引用。

### REV-50 硬编码动效时长与验收缺失

- [x] 已修复

- **证据**：`DialogProvider.razor.css` 的 `.is-shaking` 动画写 `320ms`；`.aeterni-dialog-provider__notice-region` 的 `12px/14px/28px` 位移量无注释；示例页原本没有任何用于验收色彩体系的区域。
- **修复**：时长改用 `--aeterni-duration-slow`；位移量补「这是运动距离不是尺寸」注释；示例新增 `/components/colors`（侧栏「设计基础」组），展示 6 个色族 × 5 种用法的对照矩阵、四个语意 Alert 的实发通知，以及对比度基线表。
- **验收**：`bash scripts/check-docs.sh` 通过；示例页在 Light 与 Dark 下都能对照同一组组件。

## 第五轮（v10.5.0）

| 编号 | 优先级 | 范围 | 一句话问题 |
| --- | --- | --- | --- |
| REV-63 | P1 | Token 层 / 规范 / 示例 | 「组件是否该自带表面」没有成文规则，Menu 与 Tabs 的预览因此看起来像漏了背景 |

### REV-63 表面归属未成文，内容控件预览缺宿主容器

- [x] 已修复（用户报告：Menu / Tabs 预览背景透明）

- **证据**：`Menu.razor.css` 的 `.aeterni-menu` 从 v0.1 起就没有 `background` 声明（`.aeterni-menu__item`/`__group-toggle` 一直是 `background: transparent`，只有 hover/active/selected 上色）；`Tabs.razor.css` 同样如此。而 `List.razor.css` 的 `.aeterni-list` 自带 `background: var(--aeterni-bg-surface)` + 1px 边框 + 圆角。示例预览画布（`.component-preview__canvas`）的背景是网格 + `--aeterni-bg-primary`，于是 Menu/Tabs 直接裸在页面底上，看起来像漏了背景。
- **现象**：同一族里 `List` 有表面、`Menu`/`Tabs` 没有，但规范 §5.3 只区分「容器背景 vs 交互状态背景」，从未规定「组件是否自带表面」；`current-features` 的 Menu/Tabs 章节也没写这条边界。结果是**规则缺失**，而不是渲染错误——两个组件都是按"内容控件"实现的，且示例侧边栏（真实用法）本来就给 Menu 提供了底色。
- **修复**：把判定写成规范 §5.3「表面归属」的两行表格（自带表面 vs 内容控件，判断依据是"能不能单独构成一块界面"），并进提交清单；`current-features` 的 Menu/Tabs 实现边界各补一条；示例预览改为真实语境——Menu 与 Tabs 都套 `Surface`（`Variant=Elevated` + `Bordered`，与示例侧栏同观感），两处 Notes 说明"组件不自带表面、底由宿主容器提供"。
- **验收**：规范、功能文档、组件文档与示例四处描述一致；Menu/Tabs 的 CSS 仍无 `--aeterni-bg-*` 声明；预览在 Light/Dark 下都能看出宿主容器的边界。

## 第三轮细节（v10.4.0）

### REV-51 中性容器表面重新带上蓝紫偏移

- [x] 已修复（用户报告）

- **证据**：v0.1 容器为红紫调 `--aeterni-bg: #F4F1F8`（B-R=+4、R>G）；v10.2 曾改为完全无色 `#F7F7F7 / #F0F0F0 / #E8E8E8`；v10.3 色彩体系重建后变成 `#F7F7F9 / #F1F1F4 / #E7E7EB`（B-R=+2/+3/+4），深色 `#101013 / #17171B / #202026 / #1A1A1F`（+3/+4/+6/+5），文字 `#5A5A65 / #64646E`（+11/+10）与深色 `#B4B4BE / #9C9CA7`（+10/+11）。
- **现象**：+2 以上的彩偏移在小面积上不可见，但侧栏、卡片、磨砂面板这种大面积会被读成「品牌色底」，而且 `backdrop-filter: var(--aeterni-blur-md)` 里的 `saturate(135%)` 会再放大它。`current-features` 当时已写「不再带紫调」，实现与文档不一致。
- **修复**：把「大面积面」与「小面积文字」拆开约束。浅色容器全部无色（`#F7F7F7 / #F0F0F0 / #E8E8E8`）；深色容器统一 +3（`#101013 / #17171A / #202023 / #1A1A1D`）；文字统一 +4（`#1B1B1F / #5A5A5E / #646468 / #A5A5A9`，深色 `#F3F3F6 / #B4B4B8 / #9C9CA0 / #6E6E72`）。规则写入设计规范 §5.4 与提交检查清单，避免下一次色板重建又引入彩偏移。
- **验收**：浅色四个容器背景的 R = G = B 均为 0 偏移；文字偏移 ≤ 4；全部对比度复测仍达标（三级文字在中性面 4.81~5.89:1，正文 17.2:1）。

### REV-52 Menu 禁用态用整行透明度

- [x] 已修复

- **证据**：`Menu.razor.css` 的 `.aeterni-menu button:disabled { cursor: not-allowed; opacity: var(--aeterni-opacity-disabled); }`。
- **现象**：全库其余组件均用 `--aeterni-state-color-disabled` / `--aeterni-state-background-disabled` / `--aeterni-state-border-disabled` 表达禁用；Menu 用整块透明度，会把文字、图标一起淡化到不可控的程度，也让禁用行的量度与其他控件不一致。
- **修复**：改用 `color: var(--aeterni-state-color-disabled)`（图标继承 `currentColor`，跟随级联）。
- **验收**：禁用菜单项的正文与图标均为 `--aeterni-state-color-disabled`，行高与布局不变，hover 底仍不会出现在禁用行上。

### REV-53 四套间距别名并存

- [x] 已修复

- **证据**：Token 层同时定义 `--aeterni-spacing-*`、`--aeterni-gap-*`、`--aeterni-padding-*`、`--aeterni-margin-*`；组件里 Checkbox/FormField/Tag 用 `--aeterni-gap-xs`、Switch 用 `--aeterni-gap-sm`，而其余组件用 `--aeterni-spacing-*`（两者解析到同一个值）。`--aeterni-padding-*`、`--aeterni-margin-*` 零消费者。
- **现象**：同一个 4px 在库里有三种写法，复现了 REV-19 的「双轨制」问题——只是这次是命名而非格式。
- **修复**：组件内部统一 `--aeterni-spacing-*`（四处 `gap-*` 全部替换）；三个别名家族保留为宿主面向的兼容别名，并在规范 §5.5 写明组件不得使用。
- **验收**：组件与示例样式中 `grep aeterni-gap-/padding-/margin-` 无结果；别名仍可被宿主引用。

### REV-54 字段控件过渡声明重复

- [x] 已修复

- **证据**：`Input`/`Textarea` 写 `var(--aeterni-transition-background), var(--aeterni-transition-border), var(--aeterni-transition-color), var(--aeterni-transition-shadow)`，`ComboBox` 触发器写前三个。
- **现象**：Button 已有 `--aeterni-transition-button` 这一先例，字段类控件却各自拼装，修改时长或缓动需要改三处。
- **修复**：新增 `--aeterni-transition-control`（与上面四个复合变量完全等价），三处引用它。
- **验收**：渲染后的过渡效果不变；组件中不再出现多变量拼装的 `transition` 声明。

### REV-55 已有角色 Token 无消费者

- [x] 已修复

- **证据**：`--aeterni-opacity-muted`（.68）无消费者，而 Tag 关闭字形写 `.72`、Menu 箭头写 `.7`；`--aeterni-radius-badge`（胶囊/圆形输牌）无消费者，而 NoticeCard 徽标写 `--aeterni-radius-round`；`--aeterni-bg-hover` / `--aeterni-bg-active` 无消费者且指向品牌色，与「`bg-*` 是中性容器背景」的规范描述矛盾。
- **现象**：「同一角色的魔数只写一次」是 REV-14 的目标，但已存在的角色 Token 旁边仍写着字面量；`bg-hover/-active` 则是命名陷阱——未来谁拿它当容器底，就会得到一块紫色底。
- **修复**：Menu 箭头与 Tag 关闭字形改用 `--aeterni-opacity-muted`；NoticeCard 徽标改用 `--aeterni-radius-badge`（30×30 方形上两值渲染相同）；`bg-hover/-active` 补注释说明它们是交互态别名、并要求组件使用 `--aeterni-state-background-*`。
- **验收**：三个 Token 各自至少有一个消费者；组件内只剩 Button 加载态的两处光学透明度（已在注释中说明为微调）。

### REV-56 Switch / ComboBox / Rating 没有 Size 档位

- [x] 已实现（v10.4）

- **证据**：`Input`/`Textarea`/`Button`/`Checkbox`/`Radio`/`ThemeSwitch`/`Tag`/`Progress` 均有 `small`/`large` 档；`Switch`、`ComboBox`、`Rating` 没有 `Size` 参数。
- **现象**：小尺寸表单行里 `Input Size="Small"`（32px）与 `ComboBox`（固定 40px）无法对齐；Switch 与 Rating 同样无法随表单密度缩放。
- **修复**：三个组件补 `Size` 参数（`ComponentClass.ForSize` + `Enum.IsDefined` 校验）。`ComboBox` 把触发器的四项度量提升为根 Token（高/水平内边距/字号/圆角）并加上箭头尺寸，三档只重指这些值，选项行高由触发器高度推导（`calc(高 - spacing-2)`），因此 Small 档触发器 32px 与 `Input Size="Small"` 等高。`Switch` 把轨道从写死的 38×22 改为由旋钮推导（跑道 = 旋钮×2 + 内边距×2 + 边框×2），滑块行程恰好等于一个旋钮宽，默认档与旧值逐像素一致，Small 34×20（旋钮 14）、Large 46×26（旋钮 20）。`Rating` 把星形尺寸与命中余量提到根 Token，Small 16+4、Default 20+8（与之前一致）、Large 24+12。
- **验收**：三个组件的 `Small` 档与同排控件对齐（ComboBox 32px = Input Small；Switch 旋钮与 Checkbox Small 成比例；Rating 星形与 Icon Small 同档）；默认档渲染与改动前一致（渲染验证：`Size.Default` 不输出修饰类）。

### REV-57 浮层圆角缺少成文依据

- [x] 已修复（文档）

- **证据**：Tooltip 4px（`--aeterni-radius-tooltip`）、Popover 与下拉列表 8px（`--aeterni-radius-dropdown`）、Dialog 16px（`--aeterni-radius-modal`）、通知卡 16px（`--aeterni-radius-2xl`）。
- **现象**：四档取值都能用，但规范只写了控件与容器半径，没有说明浮层层级关系；新组件容易随手取一档。
- **修复**：在规范 §5.5 补「浮层半径按层级递增」的说明与对应 Token。
- **验收**：新增浮层组件能按层级找到对应 Token，不再自选数值。

### REV-58 `--aeterni-surface-soft` 与 `--aeterni-bg-secondary` 的职责重叠

- [x] 已修复（文档）

- **证据**：`Surface`/`Card` 的 `--subtle` 变体用 `--aeterni-surface-soft`（`rgba(0, 0, 0, .045)`，随父层叠色），而示例页的次级底与禁用底用 `--aeterni-bg-secondary`（`#F0F0F0`，固定中性面）。
- **现象**：两者在白色卡片上几乎同色（`#F4F4F4` vs `#F0F0F0`），容易被当成重复 Token 而收敛掉，从而丢失「叠在任意父层上都成立」的 alpha 语义。
- **修复**：规范中明确两者分工——需要叠在未知父层上的容器表面用 alpha 的 `surface-soft`，需要与中性面严格对齐的静态底用 `bg-secondary`。
- **验收**：文档能回答「新组件该用哪个」，不再出现两者混用。

## 第四轮细节（v10.4.0）

### REV-59 文字色阶不干净

- [x] 已修复（用户报告）

- **证据**：文字是四个手调 hex：浅色 `#1B1B1F` / `#5A5A5E` / `#646468` / `#A5A5A9`，深色 `#F3F3F6` / `#B4B4B8` / `#9C9CA0` / `#6E6E72`；二级与三级只差 1.16:1（6.83 vs 5.89），主色对页面 17.1:1。
- **现象**：（1）**主色过黑**：17:1 的正文在白色上发硬，与苹果 10:1 左右的 label 观感差距很大；（2）**二级/三级分不出来**：两者色相、色阶都接近，同屏出现时像「同一个灰没对齐」，这是“看起来不干净”的主要来源；（3）**固定 hex 不随表面变**：同一串 hex 落在着色 chip、hover 底、毛玻璃上不会随表面调和，加上本身带冷偏移，很容易读成脏。
- **修复**：改成 **单一墨色 + 不透明度阶梯**（Apple label 模型）。浅色墨 `rgba(0,0,0,α)`：正文 `.78`（11.7:1）、次要 `.62`（6.2:1）、占位 `.56`（4.9:1）、图标级 `.52`（4.3:1）、禁用 `.36`（2.5:1）；深色白墨 `.86 / .56 / .48 / .40 / .28`。同时重新划分职责：`--aeterni-text-tertiary` 降为**图标与装饰级**，原先用它做正文的 Menu 分组标题、Menu 条目描述、ThemeSwitch 未选中项改用 `secondary`；`--aeterni-text-muted` 指向 `secondary`；示例页里的元信息文字也一并从 `tertiary` 改为 `muted`。
- **验收**：每一级只用一个透明度定义；正文 / 次要 / 占位在四个中性面上均 ≥ 4.5:1（正文 10.3~11.7:1、次要 5.8~6.2:1、占位 4.7~4.9:1），图标级 ≥ 3:1（4.1~4.3:1），禁用态 2.5:1（WCAG 豁免）；库内不再有 `tertiary` 作为正文的用法。

### REV-60 Tag 标签文字是浑浊混色

- [x] 已修复

- **证据**：`Tag.razor.css` 的 `--aeterni-tag-fg: color-mix(in srgb, var(--aeterni-tag-color) 52%, var(--aeterni-text-primary))`——`--aeterni-tag-color` 是**填充档**（success `#34C759`、warning `#FF9500`）。
- **现象**：把高亮度填充档与近黑对半混，得到的不是“深一档的同色”，而是去饱和后的橄榄色 / 褐色（绿的 52% + 黑 48% 是暗黄绿）；对比度虽然达标（5.1~7.7:1），但色相丢了，chip 看起来脏。
- **修复**：像通知卡片一样拆出两个 accent 角色：`--aeterni-tag-color`（填充：底色、描边）与 `--aeterni-tag-ink`（文字形态），文字 = ink 的 85% + 正文墨 15%。这一档既保住色相又拉到 4.95~7.15:1（浅色）/ 7.35~8.78:1（深色），并让 `--default`/`--soft`/`--outline` 三个变体共用同一条文字规则。
- **验收**：六个色族 × 三个变体在两种主题下 ≥ 4.5:1；danger 族（最紧）浅色 4.95:1；chip 文字肉眼不再偏绿/偏褐。

### REV-61 结构分隔线几乎不可见

- [x] 已修复

- **证据**：`Card.razor.css` 与 `DialogProvider.razor.css` 的头/底分界用 `var(--aeterni-border-subtle)` = `rgba(0,0,0,.07)`（对页面 1.17:1）。
- **现象**：7% alpha 的分界线在卡片上几乎看不到，头/底分界看起来像没对齐的灰痕，而不是一道边。
- **修复**：新增 `--aeterni-separator`（不透明：浅色 `#C6C6C6` = 1.71:1，深色 `#3A3A3A` = 1.53:1，参照 Apple 的 opaqueSeparator），卡片与对话框的头/底分界改用它；`--aeterni-border-subtle` 保留给宿主页面装饰。
- **验收**：卡片头/底分界在两种主题下能辨认且不抢文字；控件描边（Input/Checkbox 等）保持原来的柔和取值不变。

### REV-62 实心语意按钮上的文字带蓝调

- [x] 已修复

- **证据**：浅色 `--aeterni-color-on-semantic: var(--aeterni-gray-950)` = `#0B0F19`（R11 G15 B25，B-R=+14，且 G>R）。
- **现象**：这是 Tailwind 的蓝黑 primitive，用在亮色 success/warning 填充上时，文字是发蓝的近黑，与绿/黄底色不协调。
- **修复**：改为无色 `#141414`（success 8.30:1、warning 8.38:1、danger 5.19:1、info 4.59:1），保持所有实心语意控件 ≥ 4.5:1。
- **验收**：实心语意按钮/通知上的文字在两种主题下均为无彩色墨色，对比度不低于 4.5:1。

## 建议的修复批次

| 批次 | 范围 | 说明 |
| --- | --- | --- |
| 批次 1 | REV-01 ~ REV-04 | 纯 CSS/标记修复，不涉及公共 API，可一次性提交并做视觉复核 |
| 批次 2 | REV-05 ~ REV-13 | 含 ARIA 结构变化，需同步 `current-features` 与示例页演示 |
| 批次 3 | REV-14 ~ REV-23 | Token 与卫生类，可与相关组件的后续改动合并，避免大范围无功能改动 |
| 批次 4 | REV-24 ~ REV-29 | 体验增强，按需排期 |
| 批次 5（v10.4） | REV-30 ~ REV-36 | 色彩可读性：填充档不当 ink 用 + 进度条/评分图形对比度 |
| 批次 6（v10.4） | REV-37 ~ REV-39 | 色彩一致性与死类收敛 |
| 批次 7（v10.4） | REV-40 ~ REV-42、REV-49 | 结构稳定与可访问性：基类属性契约、单一标记、条件事件绑定、ARIA 字符串 |
| 批次 8（v10.4） | REV-43 ~ REV-48、REV-50 | Token 纪律与代码卫生，示例页新增验收区域 |
| 批次 9（v10.4） | REV-51 ~ REV-55、REV-57、REV-58 | 中性面无彩化 + 样式体系一致性（禁用态、间距阶梯、过渡声明、角色 Token 复用） |
| 批次 10（v10.4） | REV-56 | 新增公共 API（3 个 `Size` 档位），走 roadmap 交付，不在样式批次内实现 |
| 批次 11（v10.4） | REV-59 ~ REV-62 | 文字色阶（Apple label 模型）、chip 混色、结构分隔线、实心语意墨色 |

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

## 按钮家族重构基础交互审阅清单

以下清单用于 `Button`、`IconButton`、`MenuButton` 和 `ButtonGroup` 的每次变更验收：

- [ ] `ButtonIntent` 只表达 `Default`、`Neutral`、`Warning`、`Danger`；`ButtonVariant` 只表达 `Solid`、`Outline`、`Soft`、`Ghost`、`Link`。
- [ ] `ButtonType` 继续只表达原生 `button`、`submit`、`reset`，默认输出 `type="button"`。
- [ ] `Loading` 输出 `aria-busy="true"`、禁用交互，并保持高度、圆角和主要布局稳定；Spinner 使用 `aria-hidden="true"`。
- [ ] `Disabled`、`Loading`、`ButtonGroup` 级联禁用的优先级一致，不会被 hover/active 覆盖。
- [ ] `IconButton` 保持正方形尺寸，必须有 `AriaLabel`，Loading 时只显示 Spinner。
- [ ] `MenuButton` 的 `Open` / `OpenChanged` 保持受控状态一致；Escape、外部点击、菜单选择都能正确关闭。
- [ ] `MenuButton` 触发器与菜单共用 `PopupHost`，触发器不会被误判为外部点击；菜单支持键盘导航和视口翻转。
- [ ] Button 家族只消费 `--aeterni-*` Token；Button、IconButton 不复制第二套颜色、尺寸、圆角体系。
- [ ] 浅色/深色主题、`prefers-reduced-motion`、Small/Default/Large 和 FullWidth 均完成示例页验证。
- [ ] 修改后运行 `dotnet build aeterni_ui.slnx`、`bash scripts/check-docs.sh`、`node scripts/check-css-comments.mjs`，并使用 Tauri 的 `scripts/sample-publish.sh Debug` 验证静态发布结果。
