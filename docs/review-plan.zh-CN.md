# AeterniUI 审阅计划

文档版本：`10.30.1`

文档状态：当前活动审阅计划；第十轮剩余 REV-158～REV-163 共 6 条，全部待修复（批次 23～26 的 REV-133～REV-157 共 25 条已修复并归档到发布历史）。已完成轮次归档在 [`release-history.zh-CN.md`](release-history.zh-CN.md)。

阅读顺序：先读「约定」四节（文档边界 / 优先级定义 / 状态图例 / 审阅方法与可信度），再读第十轮问题与建议的修复批次。完成项从本文档移除，并归档到 [`release-history.zh-CN.md`](release-history.zh-CN.md)。

## 约定

### 文档边界

- 本文档只记录**问题、证据、修复方向和验收条件**，不记录已实现能力。
- 修复涉及公共 API 或组件行为变化时，必须同步 [`delivered-features.zh-CN.md`](delivered-features.zh-CN.md)。
- 修复涉及未完成能力时，状态只在 [`component-plan.zh-CN.md`](component-plan.zh-CN.md) 记录；未补齐前不得在交付功能或项目索引中写成已实现。
- 修复涉及项目结构、入口或构建脚本时，同步根目录 [`AGENTS.md`](../AGENTS.md) 的项目索引。
- 开发规范以根目录 [`AGENTS.md`](../AGENTS.md) 为准；深层设计依据见 [`engineering-reference.zh-CN.md`](engineering-reference.zh-CN.md)，本清单不新增契约。
- 本文档中的证据必须足够精确：优先给出“文件 + 组件/成员名 + 现象”，避免只写“看起来不对”或“某处样式问题”而没有可追踪位置。

### 优先级定义

| 优先级 | 含义 | 处理时机 |
| --- | --- | --- |
| P0 | 用户可直接观察到的视觉错误，或对比度不达标 | 立即，单批修复 |
| P1 | 一致性缺口和可访问性缺陷，影响正确使用 | P0 之后 |
| P2 | Token 纪律、代码卫生、可维护性 | 与相关组件改动合并 |
| P3 | 体验增强，可延后 | 有余量时 |

### 状态图例

- `[ ]` 未开始
- `[~]` 进行中
- `[x]` 已完成并验收

### 审阅方法与可信度

- 当前轮次以源码、编译后的隔离 CSS、渲染契约、浏览器检查与全库检索取证；视觉结论修复时仍须通过对应的浏览器验收。
- 对比度数值由 token 十六进制值按 WCAG 2.1 相对亮度公式计算（含 `color-mix` 的 sRGB 通道混合与半透明叠加），修复前需实测确认。
- "证据位置"给的是文件加选择器/成员名，而不是行号，便于在后续重构后继续定位。

## 第十轮：全库组件审核（剩余 REV-158～REV-163，共 6 条；P0 无，全部待修复）

范围：全库 53 个组件目录 / 65 个组件文件、49 个 `delivered-features` 章节、示例页、契约与浏览器测试。审阅维度为文档-实现对齐、视觉、功能、设计一致性，另做组件与文档颗粒度的横切比对（参数覆盖率、章节结构、默认值一致性与文案表漏网）。

方法：按组件族通读源码并逐条取证，关键结论经二次核实——源码行、**编译后的作用域 CSS**（`dist` 产物）、几何/混色推导、全库消费者 grep。前九轮修复做了回归抽查（REV-98/101/103/104/106/107/108/116/117/121/122/123/126/127/130/131 未见回归）。限制：视觉类结论（混合模式可见性、RTL 几何、hover 反馈）已按推导确认，修复时仍需补浏览器验收；不含真实触屏与读屏验收。

本轮的突出模式：**多处文档漂移是「实现改了、文档没跟」**（REV-38/71/115/121/122/123 等修复批次的文档侧未同步），此类项中 REV-141/142 已在批次 24 收口，剩余集中在 REV-144/157，按批次 26 的对账方式处理；另新增 `scripts/check-doc-drift.mjs` 门禁用于拦截同类复发。

（P1 已清空：REV-133～REV-143 分别在批次 23、批次 24 修复；P2 的 REV-144～REV-157 随批次 25、批次 26 收口。逐项证据见发布历史中的对应批次记录。）

### P2：一致性、Token 纪律与文档同步（待修复）

| 编号 / 优先级 | 问题与证据 | 修复方向与验收 |
| --- | --- | --- |

### P3：体验增强、卫生与示例（待修复；分组条目内含多项发现）

| 编号 / 优先级 | 问题与证据 | 修复方向与验收 |
| --- | --- | --- |
| REV-158 / P3 | **章节结构与参数颗粒度。** §41 Toolbar、§42 ToggleGroup 是仅有的两个无「支持能力/行为与无障碍/实现边界」小节的组件章；§46 FlashCardGroup 缺实现边界；Accordion 章无参数清单（`Multiple`/`AriaLabel` 未点名，`AccordionItem` 字段未枚举）；Drawer 缺 `CloseLabel`/`AriaLabel`；Segmented 缺 `AriaLabel`；DatePicker 用「对应回调和字段表达式参数」统称代替点名；InputNumber 章漏写 `Min`/`Max` 的有限数校验；多处默认值未记录（MenuButton 四个、Button `Intent`/`Size`、Progress `Color=Primary`、Descriptions `Columns=3`、VirtualList `Height`/`LoadMoreThreshold`） | 逐章补齐（锁名使用既有小节标题）；参数逐一点名；默认值补记——与 REV-159 之外的文档侧统一收口 |
| REV-159 / P3 | **示例缺口包。** Progress 页最薄（29 行、无交互控件、无 Color/Size）；Rating 示例 Events 表列出已删除的 `OnChange`；Radio 参数表把 `AriaDescribedBy`（仅 Radio 有）挂在 RadioGroup 名下；Tag `StartIcon/EndIcon`、Avatar 图片路径（Src/Alt/装饰分支）、FormField/InputNumber 的 EditContext 链路、DatePicker `Presets`、Segmented Required/Invalid、MenuButton `Open`/Disabled/Loading、IconButton Size/Disabled/Type 均无演示 | 逐页补可交互演示与状态对照；示例参数表与组件真实参数对齐 |
| REV-160 / P3 | **死代码与卫生包。** VirtualList 恒等死分支（`_activeIndex < 0 && _rows.Count > 0` 置 -1）；VirtualList/ListItem/Tabs 等仍用裸 `_ = InvokeAsync(StateHasChanged)`（第九轮新增的 `RequestStateHasChanged()` 只有 Slider 采用）；Pagination reduced-motion 块重复基础声明；Radio 禁用态两条规则两个同值别名；Theme 示例 `_feedbackNote` 是死字段；Pagination Notes 提到不存在的「缩放过渡」；ComboBox `_visibleItems` 纯拷贝（REV-124 删了 MultiSelect 同构未同步）；TimeOptionList 传给 JS 的 `ActiveOptionId`/`shouldAlign` 未被使用 | 逐项清理或改走统一入口；随相关组件改动合并 |
| REV-161 / P3 | **性能。** DateTimePicker 每次 `OnParametersSet` 清空日期禁用缓存，1 秒步长下最坏单次 86,400 次迭代 × 42 格；`DisabledDateTime` 非空时 `CreateMap` 的实例复用永不命中；DateRangePicker `ValidatePresets` 在 `DisabledDate` 非空时按天枚举预设区间（全范围最大约 365 万次委托调用，发生在 `OnParametersSet`） | DateTimePicker 在无禁用委托时跳过缓存；预设校验设区间宽度上限或惰性化 |
| REV-162 / P3 | **小功能与待复核项。** Tooltip `AriaLabel` 落在无 role 的 `<span>` 上（通用角色不参与可访问名称计算，参数实际无效）；Alert 的 `CloseOnEscape` 依赖根元素 keydown、非模态时不抢焦点故基本不可达；RTL 下日历/时间滚轮键盘方向不翻转；`.aeterni-dark` 类主题入口 token 层有 3 处声明与示例依赖、但全库无代码应用且文档未登记；§7/§19 对比度数字在 REV-129/131 改档后未复算 | 逐项决策：修复、登记为宿主契约或删除；对比度数字以脚本重跑结果为准更新 |
| REV-163 / P3 | **JS 模块单测覆盖。** Checkbox、DialogProvider、Menu、ThemeProvider 四个 `.razor.js` 无直接模块测试（其余 16 个有；DialogProvider/ThemeProvider 有浏览器级覆盖） | 补模块测试或成文说明由浏览器检查覆盖 |

### 建议的修复批次（第十轮）

| 批次 | 范围 | 说明 |
| --- | --- | --- |
| 批次 27 ⏳（待开始） | REV-158 ~ REV-163 | P3 收尾：文档颗粒度、示例补齐、死代码、性能、小功能与测试覆盖 |

### 执行计划评审（第十轮）

#### 前置决策（D1～D3 已全部定调，无阻塞项）

| 编号 | 决策 | 选项与建议 | 影响面 |
| --- | --- | --- | --- |
| D1 ✅ | 独立 `Radio` 是否支持值绑定 | **已定调（支持）**：显示改由 `Value` 驱动，组模式不变；已在批次 23 落地并归档 | REV-133、§13、Radio 示例页、契约断言 |
| D2 ✅ | `FlashCard` Holo 混合模式 | **已定调（回 `color` + 白光另起 `screen` 层）**：彩虹层 `color`、白光层 `screen`，四处文档与示例注释同向；已在批次 24 落地并归档 | REV-140、`FlashCard.razor.css`、`engineering-reference` §13、`delivered-features` §45、示例注释 |
| D3 ✅ | `AlertOptions.CloseText` 去留 | **已定调（接线到通知关闭按钮）**：`CloseText` 留空回退 `AeterniUITextOptions.AlertCloseLabel`；因其不再是退役成员，`check-doc-drift` 的 CloseText 条目已删除（该门禁现无待修复项） | REV-142、`DialogProvider`、`NoticeCard`、§26、Feedback 示例、`check-doc-drift` |

D1～D3 均已落定并随批次 23/24 归档：三项都同步了交付功能、示例与契约检查，`check-doc-drift` 随之不再有待修复项。

#### 批次 27（REV-158 ~ REV-163，P3）

文档颗粒度与示例补齐、死代码卫生、性能（缓存与预设校验上限）、小功能决策（REV-162 需逐项判定修复/登记为宿主契约/删除）、JS 模块测试补齐（REV-163）。REV-162 涉及 §7/§19 的对比度数字，必须以 `node scripts/check-contrast.mjs` 重跑结果为准更新。

#### 每批必过门禁

`dotnet build aeterni_ui.slnx`、`AeterniUI.ContractChecks`、`bash scripts/check-docs.sh`、token 文件组件选择器检查、`check-css-comments.mjs`、`check-contrast.mjs`、`check-doc-drift.mjs`、`node --test tests/*.test.mjs`；涉及浏览器行为时加 `tests/browser/run.sh`，涉及图标时加 `generate-fontawesome-icons.mjs --check`（CI 已接入）。

版本定档：纯行为与文档修复递增第三位；若批次引入新的公共成员（例如 REV-137 与 REV-156 的文案表键），按 `AGENTS.md` §4.9 递增第二位。

