# AeterniUI 发布历史

文档状态：历史版本、已完成阶段、修复批次和发布验证归档；版本以 `Directory.Build.props` 为准。当前未完成计划见 [`component-plan.zh-CN.md`](component-plan.zh-CN.md)，活动审阅任务见 [`review-plan.zh-CN.md`](review-plan.zh-CN.md)。本文件内容用于追溯决定，不再作为当前计划或现行 API 的事实源。

验证口径：除条目内单独记录的失败项、环境例外与计数差异外，每批均在发布时通过 [`AGENTS.md`](../AGENTS.md) §4.8 的全部门禁（构建、契约、Node 回归、CSS/对比度/文档、生成物 `--check` 与真实浏览器检查）。本节不逐批复制门禁清单与通过计数。

历史措辞说明：下列早期条目中的「文档版本头」「五份版本头文档」记录的是当时的同步动作；自本轮文档精简起，版本只在 `Directory.Build.props` 维护，文档不再复制版本头，历史记录不改写。

## 10.46.0 第十二轮审阅修复：API 默认值、示例信息架构与交互状态

本批来自一次全库 UI/UX、组件 API、示例站与文档/脚本的独立审核（5 位评审者 + 主审，真实渲染与源码双取证）。修复范围按审核计划的 P0/P1 与高价值 P2 执行，没有删除任何已交付组件；新增 `DateRangePicker.FullWidth`、修正若干公共行为，按 §4.9 第二位递增。

### 可被宿主观察到的修复

- **API 表默认值**：`generate-api-reference.mjs` 此前在属性体 `{}` 内找初始值，自动属性写在 `}` 之后的 `= 99;` 全部丢失；774 行 API 里只有 33 行（全部来自手写编辑文件）带默认值。生成器改为按 C# 结构读取声明后的初始化表达式，重新生成后 775 行、Badge `Max=99`、Skeleton `Lines=1`、VirtualList `Height="320px"` 等恢复；`--check` 之外新增“生成的 API 表保留默认值”与“示例页组件属性名契约”两条测试。
- **模态表面层级**：Dialog、模态 Drawer 与示例站搜索面板改为不透明 `--aeterni-bg-surface`。玻璃填充落在压暗遮罩上会采样被压暗的页面，浅色主题下面板呈中灰、面板内白色字段比面板更亮；遮罩仍保留模糊与压暗，深度由 `--aeterni-shadow-modal` 承担。非模态 Drawer 保留磨砂。
- **通知计时可暂停**：Alert/Toast 的自动关闭计时改为可暂停时钟；指针悬停或键盘焦点进入卡片时计时与进度环同步暂停，离开后从剩余时间继续（WCAG 2.2.1）。
- **焦点与状态**：`Popover.RestoreFocusOnClose` 默认改为 `true`（模态浮层关闭归还焦点，非模态仍有外部点击/Tab 守卫）；`DateCalendar` 的月份箭头与日格补品牌 `:focus-visible`（选中日用反色）；`Tag` 与 `Pagination` 的 `Disabled` 真正生效（根元素 `aria-disabled` + 按钮禁用 + 状态 Token 视觉）；`Splitter` 交互态改引用存在的 `--aeterni-color-brand-default`。
- **表单与尺寸一致性**：独立 `Radio` 接入级联 `EditContext`（`ValueExpression`、`NotifyFieldChanged`、校验消息参与 invalid；`Value`+`ValueChanged` 兼容语义保留）；`DateRangePicker` 补 `FullWidth`；`Segmented` Small 档回到 28px（选项 20px + 伪元素 24px 热区），禁用且选中时与 Checkbox/Radio 一样中性化；`ToggleGroup` 禁用选中中性化，并与 Segmented 统一描边与控件圆角。

### 示例站

- `<html lang="zh-CN">`；移动端搜索按钮补可访问名称；搜索空查询显示引导而不是“没有匹配”。
- 侧栏筛选在筛选态受控展开全部命中分组，消除“看起来零结果”；移动端目录展开时定位当前项、路由变化后收起。
- 新增 `/components/overview` 组件总览，首页“全部 N 个组件页”、页头/页脚“组件”、404 入口均落到总览；首页组件墙内容区整块可点，计数口径改为“组件页”。
- API 表名称列不再逐字符断词、表头吸顶、窄屏保留列标签；正文行长/tertiary 误用、FormField 预览居中、页头阴影 Token、skip link、404 标题与入口、触控热区、`prefers-reduced-motion` 下的平滑滚动一并修正。
- FlashCard 示例移除有版权风险的动漫角色图，换为自绘抽象 SVG；删除 `magician.jpg`。

### 文档与脚本精简

- 版本只保留在 `Directory.Build.props`：删除 6 份文档的版本头与 AGENTS/README 版本字面量，`check-docs.sh` 移除对应校验，13 个维护点收敛为 1。
- `release-history` 删除 v0.1–v0.4 计划期正文（约 506 行），换成 16 行阶段归档表；47 行“验证”流水账（19,948 字符）压到 24 行例外记录（1,929 字符）；>300 字符巨段 117→73 行。
- `component-plan` 删除已交付的 §4（占全文 61%）与陈旧计数；`engineering-reference` 1,043→383 行（规则条文回归 AGENTS，只保留推导/反例/测量）；`delivered-features` Token 章与机制复述压缩；README 只留消费者视角；`review-plan` 压缩并登记本轮未修项。文档合计 4,926→3,741 行（−24%）、581,897→467,252 B（−19.7%）。
- 脚本：`check-css-comments.mjs` 与 `check-scrollbars.mjs` 合并为 `scripts/check-css.mjs`（一次遍历、两条规则独立报告，可 `--only` 单跑），删除重复的 `collectCss` / `SKIP_DIRS`；`check-docs.sh` 114→67 行，删掉版本头、组件目录抽查与三章 grep 等已被构建/CI/生成物覆盖的检查；目录解析收进 `scripts/lib/showcase-catalog.mjs`，三个消费方（两个生成器与示例测试）共用一份正则与 Pascal 推导，改目录格式不再需要同步三处。

### 验证

- `dotnet build aeterni_ui.slnx` 0 错误（仅 NuGet 漏洞数据源的 NU1900 环境警告，无代码警告）；契约检查 47 组通过；`node --test tests/*.test.mjs` 133 项通过；`tests/sample-docs.test.mjs` 10 项通过；`check-contrast` 315 门禁通过；`check-css`（85 文件；16 个滚动容器）、`check-docs.sh`、`check-doc-drift`、`generate-api-reference --check`（61 页/775 行）、`generate-search-index --check`（546 条/69 页）、Font Awesome 343 图标 `--check` 全部通过。
- 真实浏览器检查 194/194 通过：补充“模态面板不透明”“Toast 悬停暂停后继续关闭”两条断言，同步搜索面板由“自身带模糊”改为“不透明表面”，并把 Segmented 高度断言从 32/36/44 改到 28/36/44（Small 用伪元素保住 24px 命中盒）；Cascader 用例对 WASM 事件处理器就绪增加重试，消除既有的首点竞态。

## 10.45.0 `NavMenu` 移除 `IconSize` / `DescriptionSize`，`IconButton` 默认圆形

`NavMenu` 在 10.43.0 增加了图标槽与描述字号的组件档位，但这两项本来就由节点内容决定：图标是 `NavMenuNode.Icon` 的 `RenderFragment`，描述是纯文本，模板行更是完全由宿主绘制。继续把它们做成 `NavMenu` 参数，会让默认行、节点内容与 `RowTemplate` 出现三套尺寸来源，模板里还得再对一次组件的档位；因此本版删除这两个参数及只服务它们的枚举，图标与描述的大小改由内容/模板自己控制。

同一批把 `IconButton` 的默认轮廓改为圆形：图标动作主要出现在标题栏、工具栏和行尾，圆形是这一类的默认 chrome；方角保留为显式选项。

- [x] 删除 `IconSize`（`NavMenuIconSize`）与 `DescriptionSize`（`NavMenuTextSize`）参数、参数校验、根 class 档位与 `aeterni-nav-menu--icon-*` / `--description-*` 规则；`Density`、`ChildIndent` 保持不变。
- [x] 默认行继续使用 16px 图标槽与 11px 描述档（原 `Default` 档观感不变）；图标槽仍通过 `--aeterni-icon-render-size` 把尺寸传给内容里未显式指定尺寸的 `Icon`，需要其它大小时在 `Icon` / `Description` 内容或 `RowTemplate` 内指定。
- [x] `IconButton.Shape` 默认由 `Square` 改为 `Circle`：圆形写进基础规则（`--aeterni-radius-full`），默认不输出修饰类；`Square` 输出 `aeterni-icon-button--square` 并回到 `--aeterni-radius-button`。大小档不再重复声明圆角，避免把默认圆形覆盖回方角。
- [x] `IconButtonMenu.Shape` 默认同步为 `Circle`，与内部触发器一致；示例的固定「行操作」触发器改为 `Square` 继续验证透传，浏览器断言同步改成检查 `--square` 与较小的实测圆角。
- [x] 示例页移除图标尺寸/描述字号控件与对应状态，保留密度与子级缩进控件，并在「说明与边界」写明图标/描述尺寸由内容控制；IconButton 的 chrome 示例改为「默认圆形，Shape=Square 切回方角」。
- [x] 契约检查删除两个档位的默认值/class/非法枚举断言，改为断言 `NavMenu` 不再暴露 `IconSize` / `DescriptionSize` 属性、CSS 不再残留 icon/description 修饰类；IconButton 断言改为「默认无形状修饰类 + 基础规则全圆角 + Square 输出 `--square`」；`api-since` 移除两条记录，`api-descriptions` 与 API 表（61 页 / 776 → 774 行）、搜索索引重新生成。
- [x] 文档：`delivered-features` §54 与 `engineering-reference` 的 NavMenu 契约改为「内容控制图标/描述尺寸」，并注明 10.45.0 的移除；IconButton 与 IconButtonMenu 的 Shape 说明改为圆形默认。
- 按「移除公开 API 与默认行为变更」的破坏性变更口径（§4.9，第二位递增）从 `10.44.0` 升到 `10.45.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。上一批 `IconButton` 悬浮修正随本批一并推送，未单独打 tag。

## 10.44.1 `IconButton` 系列悬浮改为原位高亮

本批与 `10.45.0` 同批推送，未单独打 tag/发布 NuGet 包。

`IconButton` 的悬浮反馈沿用了带文字 `Button` 的 1px 上浮与 `--aeterni-shadow-sm`。图标动作多用在标题栏、工具栏和行尾，抬起会带动整行视觉抖动，也容易被误读成可拖拽；10.43.0 的 chrome 预设只能逐条关闭位移与阴影来绕开它。本版把「原位高亮」提升为整个系列的规则，并让图标墨色步进承担反馈。

- [x] 基础悬浮/按下只换表面，删除 `transform: translateY(-1px)`、`--aeterni-shadow-sm` 与 transform 过渡；禁用态的位移复位、`Tone=Muted` / `Tone=DangerTinted` 的位移与阴影复位一并删除（danger 的 3px 16% 环保留，按下仍会清除）。
- [x] 透明变体（Outline/Soft/Ghost/Link）新增 `--aeterni-icon-button-hover-foreground`：以共享 `--aeterni-button-color-hover` 为起点，向 `--aeterni-text-primary` 按 72/28 混合，浅色更深、深色更亮；中性档本身已是主墨，混合结果不变，反馈由表面档位承担。`Link` 图标没有文字下划线可承载提示，悬浮/按下改用与 Ghost 相同的原位柔和表面。
- [x] 契约检查从「Tone 需自行复位上浮」改为文件级断言：IconButton CSS 不得出现 `transform` 与 `--aeterni-shadow-sm`，基础 hover 不得加阴影，透明变体必须消费派生 hover 墨色；真实浏览器新增一条 hover 断言（指针悬停 Soft + Warning，比较位移、`transform`、阴影、墨色与表面档位），**194/194 PASS**。
- [x] 文档：`delivered-features` 与 `engineering-reference` 写明系列规则、与 `Button` 1px 上浮的分工，以及 IconButton `Link` 的差异。
- 按「修复与优化递增第三位」（§4.9）从 `10.44.0` 升到 `10.44.1`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。本批未单独发版，`10.45.0` 的版本头与 NuGet 包同时包含这两批改动。

## 10.44.0 `IconButtonMenu` 支持 `Shape`

`IconButtonMenu` 内部一直用 `IconButton` 做触发器，但 10.43.0 给 `IconButton` 增加的 `Shape` 没有透传出来，圆形 chrome 动作（例如标题栏/行尾的圆形「更多」）只能退回手写 `IconButton + Popover + Menu`。

- [x] `IconButtonMenu` 新增 `Shape`（`IconButtonShape`：`Square` 默认 / `Circle`），直接传给内部 `IconButton`；非法值在参数设置阶段抛 `ArgumentOutOfRangeException`，`Tone` / `Compact` 等其余触发器参数保持原状。
- [x] 示例页基础用法增加 Shape 控件，并把第二个触发器固定为 `Circle` 展示轮廓；契约检查增加 Shape 默认值、非法枚举与 `aeterni-icon-button--circle` 透传断言；浏览器断言新增「Shape=Circle 落到真实触发器」（**193/193 PASS**）。
- [x] 文档：`delivered-features` §55 补充 Shape 的参数/边界说明；API 表（61 页 / 776 行）与 `api-since` 同步。
- 按「功能新增递增第二位」（§4.9）从 `10.43.1` 升到 `10.44.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例、`scripts/api-since.json`。

## 10.43.1 修复：NavMenu 分组行与首个子项之间没有行距

10.43.0 把 hover/选中画到 `.aeterni-nav-menu__row` 后暴露了一个间距缺口：分组节点的首个子项紧贴分组行（collapse 轨道没有外边距），选中的子项与上方 hover 的分组行会画出两块粘连的背景；这也与 Menu 的「分组标题 → 子项」间距配方不一致。

- [x] `.aeterni-nav-menu__node` 改为 `display: grid; align-content: start`，`.is-open` 时使用 `gap: var(--aeterni-nav-menu-gap)`：展开分组在行与子列表之间保留与同级行一致的行距（Default/Compact 4px、Comfortable 2px），已折叠分组不保留空轨道；子级缩进与引导线不受影响。
- [x] 契约检查新增 `.aeterni-nav-menu__node.is-open` 的 gap 配方断言（防止以后又把间距画回按钮层级）；浏览器断言新增「分组行与首个子项保留行距且等于列表 gap」（**192/192 PASS**）。
- 按「修复与优化递增第三位」（§4.9）从 `10.43.0` 升到 `10.43.1`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.43.0 NavMenu 密度与行级状态、IconButton chrome 预设、Menu/Popover/Dialog 补全

本批包含 10.42.0 的 `IconButtonMenu` / `FormDialog`，并继续给现有组件补上工作台侧栏与窗口 chrome 直接需要的能力，目标是删掉宿主里的 `::deep` 与内联 token 覆盖。参考实现与验收依据是 `/Users/lixingyuan/GithubProjects/AeterniWorkbench` 的 `WorkbenchSidebar`、`TitleBar`、`ProjectActionsMenu`。

- [x] `NavMenu`：新增 `Density`（`Default` / `Compact` / `Comfortable`）、`IconSize`（16/14/20px）、`DescriptionSize`（11/10/12px）与 `ChildIndent`（16·8 / 12·4 / 20·12），全部通过根 class 上的组件级 CSS 变量消费；`Default` 逐项保持原观感，`Comfortable` 精确对齐工作台侧栏（38px 最小行高、3px 纵向留白、8px 圆角、2px 行距、13px 字号、primary 行墨）。
- [x] `NavMenu` 行级状态：hover、选中与按下从行按钮移到 `.aeterni-nav-menu__row`，行容器持有 `is-selected` / `is-disabled`，Trailing 与名称共享同一块背景与同一支墨；按钮透明，只保留原生 Tab/键盘激活与 `focus-visible` 环。选中继续使用 `--aeterni-state-background-selected` / `-color-selected` / `-background-checked`，hover 使用 `--aeterni-state-background-hover`，按下用 `:has(...:active)` 同步到行盒。
- [x] `IconButton`：新增 `Shape`（`Square` / `Circle`，后者使用 `--aeterni-radius-full`）、`Tone`（chrome 预设 `Muted` / `DangerTinted`，覆盖 Variant/Intent 配色并关闭上浮/阴影；danger tint 用现有 danger 色阶现算 12%/20%/26% 底、25%/55% 描边与 3px 16% 环）与 `Compact`（24px 盒 / 14px 字形，优先于 `Size`，只改几何）。`Tone=Default`、`Shape=Square`、`Compact=false` 渲染逐字节保持原状。
- [x] `Button.ContentAlignment`（`Center` 默认 / `Start`）：只重排 `.aeterni-button__content`，`Start` 让图标与文字从左侧起排并填满内容行，用于 `FullWidth` 的「新会话」式动作；按钮宽度/对齐契约不变。
- [x] `Icon.Tone`（`Default` / `Muted` / `Tertiary`）：补中性墨阶，`Color` 继续只表达语义色；两者同时设置时 `Tone` 生效。没有把 `Muted` / `Tertiary` 塞进共享 `Color` 枚举，避免 Badge/Tag/Avatar/Progress/Spinner 五个消费者无声接受未实现的取值。
- [x] `Popover.MinWidth` / `Width`：写入根内联 `--aeterni-popover-min-width` / `--aeterni-popover-width`，surface 消费且不覆盖宿主其他 `Style`；新增同族 token `--aeterni-overlay-min-width-xs: 10rem`，`MenuButton` / `IconButtonMenu` 的菜单型弹层默认切到它。`Menu.IconSize`（`Small` 16 / `Default` 20 / `Large` 24）同时调整分组与条目图标槽，chevron 保持自身 16px 档。
- [x] `DialogOptions.AutofocusFirstField`（默认 false）：任意自定义 Dialog 可打开即聚焦弹窗内首个字段，无字段回退原首个可聚焦元素；`FormDialogOptions` 构造器默认 true，`DialogProvider` 对普通 Dialog 仅在 true 时输出标记，JS 按标记聚焦。`FormDialog` 的忙碌/校验保持 10.42.0 的专属实现，普通 Dialog 不造假参数。
- [x] `List`：把选中态复用配方（`--aeterni-state-background-selected` + `--aeterni-text-primary` + `--aeterni-font-weight-medium`，hover 排除选中/禁用，附属内容用 `currentColor`）写入交付功能，供工作台工具行直接引用而不是仿写。
- [x] 示例与生成物：NavMenu 增加 4 个档位 Controls 与「整行状态覆盖 Trailing」demo；IconButton 增加 chrome 预设 demo；Button/Icon 各增加内容对齐与墨阶 demo；Menu/Popup 在现有 demo 内补 IconSize、MinWidth/Width 控件；Feedback 增加 `AutofocusFirstField` 自定义 Dialog。API 表（61 页 / 775 行）与搜索索引（544 条 / 68 页）重跑，`api-descriptions` / `api-extra-rows` / `api-since` 同步（新成员 10.43.0）。
- [x] 测试：契约检查扩到 **47 组**（新增 `NavMenuEnhancementChecks`、`ChromeControlsChecks`、`MenuDialogChecks`）；`node --test tests/*.test.mjs` **131 项**；`tests/browser/verify.mjs` 新增 7 条真实浏览器断言（NavMenu 密度与行级选中、hover 含 Trailing、IconButton 圆形 danger 色调与 24px 紧凑、Button 内容左对齐、Icon 墨阶、Popover MinWidth/Width 落位、普通 Dialog 首字段聚焦），**191/191 PASS**。
- [x] 测试基建：`tests/browser/serve.mjs` 对每个请求做错误隔离（Node 24 下浏览器中止导航产生的写错误此前会变成未捕获 rejection 并杀掉静态服务器，导致套件在长跑中途挂起）；真实浏览器断言继续用 `Page.bringToFront` 保证被测标签页不是 `hidden`。
- 验证：例外：本机 `NU1900` 为沙箱拒绝写 NuGet 漏洞缓存；本机无外网，`generate-fontawesome-icons.mjs --check` 拉取图标清单超时未完成（本批未改图标清单）。
- 按「功能新增递增第二位」（§4.9）从 `10.42.0` 升到 `10.43.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例、`scripts/api-since.json`。

## 10.42.0 `IconButtonMenu`、`Menu.Flat` 与 `FormDialog` 表单弹窗约定

本批与 10.43.0 同批推送，未单独打 tag/发布 NuGet 包；v10.43.0 的包同时包含本批改动。

本批交付两个 P0 交互补全组件：`IconButtonMenu`（图标按钮 + 触发表层 + `Menu` 一体）与 `FormDialog`（`FormDialogOptions` + `IDialogService.ShowForm/ShowFormAsync` + 声明式宿主），并把状态点 `StatusDot` 只登记为 [`component-plan`](component-plan.zh-CN.md) 的 P2 候选，未实现。两者都复用既有 `PopupHost` / `Popover` / `Menu` / `DialogProvider` 与 `aeterni_floating.js`，没有新增第二套模态或键盘实现。

- [x] `Components/IconButtonMenu/`（`.razor` / `.razor.cs` / `.razor.css`）：`Icon`、`Items`、`AriaLabel` 必填；`Variant`（默认 Ghost）、`Intent`（默认 Neutral）、`Size`（默认 Default）、`Placement`（默认 BottomEnd）、`Flat`、受控/非受控 `Open` / `OpenChanged`、`OnItemSelected`、`Loading`。
  - 触发器输出 `aria-haspopup="menu"`、`aria-expanded` 与指向真实菜单根 id 的 `aria-controls`；AriaLabel 空白、`Items`/`Icon` 为 null、四个枚举非法值在参数设置阶段分别抛 `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException`。
- [x] `Menu.Flat`：扁平模式按分组顺序渲染一份无分组标题的列表，不输出 group toggle、collapse 与 `aria-expanded`；条目保留链接/按钮变体、`aria-current`、图标、说明与禁用语义；方向键/Home/End 只在可见非禁用条目间移动，左右键不再展开折叠。`Flat=false` 的渲染与键盘分支保持不变。
- [x] 表单弹窗：`FormDialogSubmitResult`（`Success` / `Fail`）、`FormDialogOptions : DialogOptions`（`SubmitText`、`CancelText`、`OnSubmit`、`OnCancel`、`AutofocusFirstField`、`SubmitOnEnter`、`ValidationMessage`）、`IDialogService.ShowForm/ShowFormAsync`，以及不渲染 DOM 的声明式 `FormDialog` 宿主（受控 `Open` / `OpenChanged`，处理关闭中重开与 Dispose）。
  - 表单 kind 复用 `DialogProvider`：footer 由 Provider 生成（主按钮 Loading、次按钮与关闭按钮禁用、`aria-busy`），提交失败写入 `role="alert"` 校验区，忙碌期间忽略 Escape/遮罩/重复提交；成功以 `DialogResult.Confirm(Data)` 关闭。
- [x] 焦点与 Enter 约定：打开时聚焦表单首个字段（无字段回退原首个可聚焦元素）；`SubmitOnEnter` 只在单行字段触发，textarea/select/按钮/链接/contenteditable、输入法组合与 Shift/Ctrl/Meta/Alt 组合键不触发。由 `DialogProvider.razor.js` 统一处理，组件与宿主不再各拼一套。
- [x] 文案表新增 `FormDialogSubmitLabel`（Save）、`FormDialogCancelLabel`（Cancel）并纳入归一逻辑；`check-doc-drift` 的 98 个文案键覆盖通过。
- [x] 示例与生成物：新增 `/components/icon-button-menu`（4 段 demo：基础 + Controls、Flat/分组、受控打开、禁用/加载）与 `/components/form-dialog`（声明式校验/忙碌/成功 + 服务 API 两段 demo）；目录条目、API 表（61 页 / 762 行）与搜索索引（540 条 / 68 页）重跑，`api-descriptions` / `api-extra-rows` / `api-since` 同步（新成员标记 10.42.0）。
- [x] 测试：契约检查扩到 **44 组**（新增 `IconButtonMenuChecks`、`FormDialogChecks`：Flat 语义、根属性转发、AriaLabel/枚举/null 校验、表单标记/文案回退/忙碌/校验/成功 Data）；`node --test tests/*.test.mjs` **131 项**；`tests/browser/verify.mjs` 新增 7 条真实浏览器断言（IconButtonMenu 打开关联与选择后关闭回归、Flat 无分组且方向键只走条目、FormDialog 首字段聚焦、Enter 忙碌 + Escape 忽略 + 成功后关闭回归、失败校验），最终 **183/183 PASS**。
- [x] 验证过程中修掉两处会让真实浏览器门禁失真的问题：共享 `waitForExit` 在「动画已在观察前结束」时可能拿到永不结算的 `Animation.finished`，导致浮层永久停在 `is-closing`——改为按实测 `delay + duration` 兜底并在已结束时立即完成（`delivered-features` §26 与 `engineering-reference` §6 同步）；浏览器夹具在每次导航后 `Page.bringToFront`，避免 Chrome 首次运行的 `chrome://settings/help` 页把被测标签页挤成 `hidden` 后焦点/动画断言静默失真；Segmented 轨道高度断言改为亚像素容差（44 实测 43.9999）。
- 验证：例外：本机 `NU1900` 为沙箱拒绝写 NuGet 漏洞缓存；本机无外网，`generate-fontawesome-icons.mjs --check` 拉取图标清单超时未完成（本批未改图标清单）。
- 按「功能新增递增第二位」（§4.9）从 `10.41.1` 升到 `10.42.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例、`scripts/api-since.json`。

## 10.41.1 修复：`NavMenu` 折叠的分组保留子内容高度，分组之间留下大空隙

PR #34（`fix/nav-menu-collapse`）以 squash 合入 main（`7438efd`）。`NavMenu` 的折叠照搬了 `Menu` 的 `grid-template-rows: 0fr` 写法，但漏掉内层列表的 `min-height: 0` + `overflow: hidden`（`Menu` 的 `.aeterni-menu__items` 有这两条）：grid item 的自动最小尺寸按 `min-content` 解析，零高度轨道被内容撑开，于是 `aria-expanded` 已经是 `false`、子内容仍占据 80px，两个分组之间留下大空隙。

- [x] `.aeterni-nav-menu__children` 补 `min-height: 0` + `overflow: hidden`；`.aeterni-nav-menu__collapse` 补 `align-content: start`，与 `Menu` 的折叠配方逐条对齐。
- [x] 回归断言：`tests/browser/verify.mjs` 的 NavMenu 断言新增「折叠后子内容让出高度」——折叠后 collapse 高度为 0、分组高度与菜单高度各至少减少 40px；本机发布站实测折叠前 112 / 264 / 80px → 折叠后 32 / 184 / 0px，分组间距保持 4px 不变。
- 按「修复与优化递增第三位」（§4.9）从 `10.41.0` 升到 `10.41.1`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.41.0 `NavMenu` 侧边导航菜单（替换 `NavTree`，破坏性变更）

PR #33（`feat/nav-menu`）以 squash 合入 main（`710c59a`）。用 `NavMenu` 替换 10.40.0 交付的 `NavTree`：沿用 `Menu` 的展开/选中模型，但分组行没有内置的右侧折叠图标、任意深度的每一行都可以整行模板、展开的分组下方带左侧引导线；`NavTree` 同版本物理删除。

- [x] 新组件 `Components/NavMenu/`（`.razor` / `.razor.cs` / `.razor.css` / `.razor.js`）：`Items`、`SelectedId` + `SelectedIdChanged`、`OnItemSelected`、`OpenKeys` + `OpenKeysChanged`、`OnGroupToggled`、`Accordion`、`AriaLabel`（缺省 `AeterniUITextOptions.NavMenuLabel`）。`NavMenuNode` 支持 `Children`、`Icon`、`Description`、`Trailing`、`RowTemplate`、`Href`/`Target`、`Disabled`、`Visible`、`InitiallyOpen`，Id 必须非空且唯一。
- [x] 分组与选中：带可见子节点的行是导航组，只有 `aria-expanded` / `aria-controls` 与展开/折叠，不输出 `aria-current`、不写 `SelectedId`、不触发选中回调；叶子行才可选中（`aria-current="page"` + 与 `Menu` 同一组 state token 的选中样式）。`SelectedId` 指向分组时被忽略。
- [x] 任意深度与模板：`Children` 递归渲染，每层左侧引导线表达层级；`RowTemplate`（`RenderFragment<NavMenuRowContext>`）接管整行内容，上下文提供 `Node` / `Depth` / `IsExpanded` / `IsSelected` / `IsDisabled`，按钮的可访问名称始终取 `Label`；`Trailing` 在行按钮之外，可放独立动作菜单，不产生嵌套按钮。
- [x] 键盘与语义：根是 `<nav aria-label>`，列表 `role="list"`；沿用 `Menu` 的模型（自然 Tab 顺序 + 方向键在可见行间移动、`→`/`←` 按书写方向展开/折叠并进入/回到父级、`Home`/`End` 首尾、`Enter`/`Space` 原生激活）；方向键只在行按钮上抑制滚动，trailing 动作保留自己的键盘行为；JS 只做焦点、滚动抑制与书写方向。
- [x] 破坏性移除：删除 `Components/NavTree/`（`NavTree`、`NavTreeNode`）、示例页 `/components/nav-tree`、目录条目 `nav-tree`、`tests/nav-tree.test.mjs`、`delivered-features` §54 的 NavTree 章节与 `scripts/api-since.json` 的 `NavTree.*` 记录；文本键 `NavTreeLabel` 更名为 `NavMenuLabel`；示例辅助组件 `NavTreeActionMenu` 更名为 `NavMenuActionMenu`。
  - 迁移口径：`/components/nav-tree` → `/components/nav-menu`，`OnNodeSelected` 拆成 `OnItemSelected`（叶子）与 `OnGroupToggled`（分组），分组行只更新 `OpenKeys`。
- [x] 示例与生成物：`/components/nav-menu` 覆盖默认行与「整行模板 + 右侧操作」两段 demo；API 表（59 页 / 731 行）与搜索索引（524 条 / 66 页）重跑；`scripts/api-since.json` 补 9 个 `NavMenu.*` 成员（`10.41.0`），示例 API 表的「自哪个版本」列显示该版本。
- [x] 回归：契约检查仍为 42 组，NavMenu 覆盖导航地标、展开/控制、叶子 `aria-current`、分组不选中、链接 target guard、`Accordion` 单开、`RowTemplate` 深度、`Trailing` 兄弟位置与 Id 校验；`tests/nav-menu.test.mjs` 守键盘模块的行级滚动抑制、trailing 放行、焦点与 RTL；`tests/browser/verify.mjs` 新增 4 条真实浏览器断言（分组只展开、引导线、叶子选中、模板行与 Trailing）。
- 按「功能新增递增第二位」（§4.9）从 `10.40.2` 升到 `10.41.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例、`scripts/api-since.json`。

## 10.40.2 修复：Splitter 持久化恢复循环、NavTree 选中态与辅助说明

PR #30（`fix/splitter-navtree-polish`）以 squash 合入 main（`f4ec37d`）。该提交只包含组件与文档改动，没有随带的版本号与发布记录；本批按 §4.9 补上版本与归档，代码改动以 `f4ec37d` 为准。

- [x] **Splitter 持久化恢复循环**：带 `PersistKey` 且 localStorage 已有宽度时，`attach` 期间恢复回调会触发 `OnSizeRestoredAsync` → `StateHasChanged`；此前 `_attached` 在 `await` 之后才置真，重渲染判定为"未挂载"又调一次 `attach`、再触发恢复，形成 JS/.NET 微任务无限循环（Workbench 的 Tauri WebView 上 99.7% CPU、界面可渲染但不可点击；无痕环境没有已存宽度，因此不触发）。现在在 await 前先标记 attached，恢复回调触发的重渲染走 `sync`；`attach` 抛错时 catch 还原标记以保留重试。同一源码实测 CPU 从 99.7% 降到约 3%，拖动/折叠/点击恢复。
- [x] **NavTree 选中态与辅助说明**：选中行改用与 `Menu` 相同的 `--aeterni-state-*` token（选中背景、品牌文字、左侧强调线），并补 hover/active 不覆盖选中态的规则；辅助说明字号减小 1px、使用 `--aeterni-text-secondary` 与 `line-height: normal`，标题与说明间距 1px → 2px。
- [x] **文档**：`delivered-features` 的 Splitter 章节补上恢复回调与 attached 标记的因果，NavTree 章节把"使用共享语义 token"收敛到与 `Menu` 同组的具体口径。
- 验证：差异：本发版提交只改版本记录与 `release-history`，门禁复跑通过。
- 按「修复与优化递增第三位」（§4.9）从 `10.40.1` 升到 `10.40.2`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.40.1 修复：锁滚动预留的滚动条槽没被压暗（CI 浏览器作业的两条几何断言）

10.40.0 之后 `build` 工作流的 `browser` 作业仍红：171 条断言中 169 通过，恒失败的是「面板在视口里居中」「遮罩铺满整页视口」。复核确认不是环境项，而是 10.38.4 引入 `scrollbar-gutter: stable` 后留下的系统性缺陷：预留槽让布局视口缩小一个滚动条宽（CI 10px、经典滚动条本机 11px），两端都按布局视口排布：

- `position: fixed; inset: 0` 的覆盖层只排到 1440 减槽宽，右侧留下一条未压暗的亮条（实测 `RGB(247,247,247)` 页面底色对比遮罩 `RGB(142,142,142)`）；
- 居中容器按缩小后的宽度居中，面板中心落在 715 而不是 720。

覆盖式滚动条（macOS 默认）宽度为 0，本地历史上无法复现，10.38.4 因此把它误记成「本机 Edge 环境项」。用 `--disable-features=OverlayScrollbar` 得到经典滚动条后，本机 Chrome 156 完整复现了 CI 的两个数字。

修复要同时解决「几何」与「绘制」两件事，因为 Chrome 把固定定位的绘制裁在布局视口内，**外扩出来的槽位不会被画到**：

- [x] 几何：`lockScroll()` 在预留槽位时把实测滚动条宽度写成根元素上的 `--aeterni-scroll-lock-gutter`（锁前测量、引用计数只在首个锁写入、末个释放恢复；宿主同名值原样还原）；`DialogProvider` 遮罩、`Drawer` 根层、`Popover` 遮罩、示例搜索模态四处用 `inset-inline-end: calc(-1 * var(--aeterni-scroll-lock-gutter, 0px))` 把内联末端伸出去，遮罩矩形与居中容器回到窗口尺寸，面板中心回到 720；RTL 下向左侧扩。覆盖式滚动条引擎上变量为 `0px`，声明等于不存在。
- [x] 绘制：固定层画不进槽位，只有根元素的画布底色能铺到那里。锁定时把「遮罩色叠在页面底色上」的合成色写到根元素行内 `background-color`（读 `--aeterni-overlay` 与页面底色现算，解锁还原宿主原值）；页面底色透明时跳过，避免把画布变暗透到内容后面形成二次压暗。
- [x] 回归断言：`tests/browser/verify.mjs` 新增「锁滚动预留的滚动条槽被覆盖层接住（几何外扩 + 画布着色）」——经典滚动条下要求 `scrollbar-gutter: stable`、变量 > 0、遮罩右边界与 `innerWidth` 差 ≤1、容器 `inset-inline-end` 为负、根元素底色的亮度比页面底色低 20 以上；覆盖式滚动条下变量为空，自然跳过。`tests/scroll-lock.test.mjs` 从 5 项扩到 8 项（画布合成色、透明底不染、宿主行内值还原）。
- [x] 文档：`engineering-reference` 的共享浮层章节写明「覆盖层外扩 + 画布着色」两部分及原因（固定层被裁在布局视口内、透明底不染）；`delivered-features` 的 Dialog/Drawer 行为段补上「槽位被画成与遮罩相同的压暗色」；并更正 10.38.4 条目里把这两条失败归因为本机环境的记录。
- 验证：修复后实测：scrim 1440 / 面板中心 720，槽位与遮罩同为 `RGB(143,143,143)`（本机 Chrome 156 + `--disable-features=OverlayScrollbar`）。
- 按「修复与优化递增第三位」（§4.9）从 `10.40.0` 升到 `10.40.1`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.40.0 `NavTree` 导航树与 trailing 操作区（新组件交付）

PR #29（`feat/nav-tree`）以 squash 合入 main（`6ba6173`）后落地的发布批次。交付层级导航组件 `NavTree` 与节点模型 `NavTreeNode`：节点按钮右侧的 `Trailing` 是按钮的兄弟区域，可承载独立的 `PopupHost` + `Popover` + `Menu` 操作入口，不会再出现 `MenuGroup` 只有内置 chevron、直接嵌套产生 button-in-button 的问题。

合并前按要求只做了合并面核查，未重复审查组件实现：GitHub 判定 `MERGEABLE`（与 10.39.0 的文档改动无冲突），PR diff 中未发现凭据、密钥、token、本机绝对路径、`.env`、外部网络请求或 `eval` / `innerHTML` 一类危险调用（仅文档里的「语义 token」字样命中关键词）。

- [x] 组件 `Components/NavTree/`（`.razor` / `.razor.cs` / `.razor.css` / `.razor.js` / `NavTreeNode.cs`）：`Items`、`SelectedId` + `SelectedIdChanged`、`OnNodeSelected`、`OpenKeys` + `OpenKeysChanged`、`AriaLabel`；节点支持 `Children`、`Icon`、`Description`、`Trailing`、`Disabled`、`Visible`，节点 Id 必须非空且唯一。
- [x] 语义与键盘：`role="tree"` / `treeitem` / `group` + `aria-level`、`aria-selected`、`aria-expanded`；单一 Tab 停留点，`↑`/`↓` 移动、`→` 展开或进入子节点、`←` 折叠或回父节点、`Home`/`End` 首尾，`Enter`/`Space` 保留原生按钮激活；JS 只负责焦点移动与方向键滚动抑制，展开/选中模型在 C#。
- [x] 示例与生成物：`/components/nav-tree`（两级导航 + trailing 动作菜单）并登记进 `ShowcaseCatalog`（导航条目 59）；API 表（59 页 / 729 行）与搜索索引（524 条 / 66 页）生成物重跑；`scripts/api-since.json` 补 7 个 `NavTree.*` 成员（`10.40.0`），示例 API 表的「自哪个版本」列显示该版本。
- [x] 测试：`tests/nav-tree.test.mjs` 覆盖方向键滚动抑制、trailing 不拦截、焦点与释放；契约检查扩到 **42 组**，`node --test tests/*.test.mjs` 共 **123 项**。
- [x] 文档与版本：`delivered-features` §54（NavTree 章节由 PR 提供）、`component-plan` §2 / §3 登记与 §4 计数、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例同步。
- 验证：例外：浏览器作业沿用 10.38.4 起的既有环境项 —— Dialog 居中、遮罩铺满视口两条视口几何断言。
- 按「功能新增递增第二位」（§4.9）从 `10.39.0` 升到 `10.40.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例、`scripts/api-since.json`。

## 10.39.0 `Splitter` 可拖动分栏（新组件交付）

PR #28（`feat/splitter-component`）以 squash 合入 main（`1065b3b`）后落地的发布批次。交付布局组件 `Splitter`，并同批修掉合并前只读审查发现的三个阻塞问题：`Element` / `ElementChanged` 未绑定、合法尺寸 `0` 被当成未初始化哨兵、无消费者的 `is-disabled` 类名；RTL、命中区、禁用态与真实浏览器断言一并补齐。

- [x] 组件 `Components/Splitter/`（`.razor` / `.razor.cs` / `.razor.css` / `.razor.js`）：参数 `Orientation`、`FirstSize`（默认 `280`）、`MinFirstSize`（`200`）、`MaxFirstSize`（`600`）、`FirstSizeChanged`、`PersistKey`、`AriaLabel`、`FirstPane` / `SecondPane`；参数在设置阶段校验（`0 <= MinFirstSize < MaxFirstSize`，`FirstSize` 必须落在区间内），非法枚举与越界尺寸抛 `ArgumentOutOfRangeException`。
- [x] 根元素绑定：`@ref` 经私有 `RenderedRootElement` 回写 `RootElement`（REV-147 写法），`Element` / `ElementChanged` 恢复可用；JS 模块仍只拿组件自己渲染的节点（与 Toolbar、ToggleGroup、Menu、三个 combobox 的归属口径一致）。契约检查新增该守卫，防止再次退回私有字段直绑。
- [x] 尺寸语义：`0` 是合法尺寸（`MinFirstSize` 可为 `0`）。C# 用 `_hasSize` 标志、JS 用 `Number.isFinite` 区分「未给出」与「0」，修复 `MinFirstSize=0` 折叠后宿主重渲染把宽度弹回 `FirstSize` 的问题。
- [x] 语义与键盘：`role="separator"` + 显式 `aria-orientation`（水平分栏 `vertical`、垂直分栏 `horizontal`）+ `aria-valuemin` / `aria-valuemax` / `aria-valuenow` + 可访问名称（缺省回退 `AeterniUITextOptions.SplitterLabel`）。单一 Tab 停留点；`←`/`→` 或 `↑`/`↓` 每次 8px、`Shift` 32px、`Home`/`End` 到端点、双击回最小；只对处理过的按键 `preventDefault`。禁用时 `tabindex="-1"` + `aria-disabled="true"`，指针与键盘都不提交。
- [x] 指针与持久化：Pointer Capture 跟随；拖动期间 JS 直接把宽度写进 `--aeterni-splitter-first-size`（不逐帧回调），松开 / 键盘提交 / 双击 / 恢复持久化宽度时才回调 `FirstSizeChanged`；`PersistKey` 读写 localStorage 并 clamp 到当前区间。拖动中 `dispose` 会移除 window 监听、释放指针捕获并还原 body 光标。
- [x] RTL 与命中区：水平分栏改用逻辑 inset（`inset-inline-start`）并把命中区宽度按半宽内缩，RTL 下第一栏在右、分隔条仍落在接缝上；拖拽增量与 `←`/`→` 按逻辑方向镜像，方向在每次手势与按键时读取，宿主运行时改 `dir` 也生效。分隔条命中区取 `--aeterni-touch-target-min`（24px），视觉仍是 `--aeterni-border-width-thin` 发丝线；hover 组排除 `aria-disabled="true"`，焦点环改用 `--aeterni-focus-offset`。
- [x] 清理：删掉无消费者的 `is-disabled` 与 `aeterni-splitter__pane--first/--second` 类；`OnSizeChangedAsync` / `OnSizeRestoredAsync` 复用同一提交路径；`sync` 在启用态移除 `aria-disabled`（与 markup 一致）；`readStored` 只读一次。
- [x] 示例与生成物：`/components/splitter` 覆盖水平、垂直与禁用三态并登记进 `ShowcaseCatalog`（导航条目 58）；API 表（58 页 / 722 行）与搜索索引（517 条 / 65 页）生成物重跑；`scripts/api-since.json` 补 9 个 `Splitter.*` 成员（`10.39.0`），示例 API 表的「自哪个版本」列显示该版本。
- [x] 文档：`delivered-features` §53 补 RTL、命中区、禁用与实现边界（固定像素轨道在窄容器下的表现、无 JS 时不响应拖动与键盘）；`component-plan` §2 / §3 登记交付并更新 §4 的页面与索引计数；`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例同步。
- [x] 回归：`tests/splitter.test.mjs` 从 4 项扩到 10 项（合法 0 尺寸、按键 `preventDefault` 边界、拖动中 `dispose`、禁用 no-op、存储 clamp、RTL 镜像）；`tests/browser/verify.mjs` 新增 5 条 Splitter 断言（分隔条语义与命中区、键盘步进、真实指针拖动与宿主回显、禁用态、RTL 接缝与方向键）。
- 验证：例外：本机 6 条 `NU1900` 为沙箱拒绝写 NuGet 漏洞缓存；`tests/browser/run.sh` 全量在本机运行到 upload 页后卡住（既有环境项，10.38.2 起记录）；CI 浏览器作业在 PR #28 上的失败项仍是 10.38.4 基线的两条视口几何断言。
- 按「功能新增递增第二位」（§4.9）从 `10.38.4` 升到 `10.39.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例、`scripts/api-since.json`。

## 10.38.4 交互修补：滚动锁视口稳定、Confirm 抖动反馈与 InputNumber 连体步进

针对 10.38.3 批次落地后的本地验收反馈落地的修复批次；不新增公共 API，不改 Token 色板、玻璃配方与组件对外语义。

- [x] **滚动锁视口稳定**：`aeterni_floating.js` 的 `lockScroll` 不再用 `body { padding-right }` 模拟滚动条宽度。页面确实可滚动且浏览器支持时，在根元素保留 `scrollbar-gutter: stable`，开、关浮层前后视口宽度不变，fixed 页头与流内页脚始终对齐；不支持时退回原来的 padding 方案，解锁时 `overflow`、`padding-right`、`scrollbar-gutter` 一并还原。此前 fixed 页头按含滚动条的视口宽度解析、正文按扣除滚动条的宽度解析，Confirm 一开一关页头就会横向跳一下。
- [x] **Confirm 抖动不再像刷新**：每次点击外围都会重新抖一下；C# 端在抖动结束后再摘掉 `is-shaking` 并补 `is-settled`，下一次点击先清 `is-settled` 再挂 `is-shaking`，CSS 把 `.is-settled { animation: none }` 放在 `is-shaking` 之前，面板不会落回 `animation: enter` 从 `opacity: 0` 重播；抖动关键帧改为 0 / 15% / 35% / 55% / 75% 的 `-6px / 5px / -4px / 2px` 衰减，时长仍取 `--aeterni-duration-slow`。
  - JS 的 `waitForShake` 改为在 500ms 内用 `requestAnimationFrame` 轮询到抖动动画再等它结束，并遵守 `prefers-reduced-motion`。
- [x] **InputNumber 连体步进**：外层字段 `overflow: hidden`，方角步进按钮的悬停 / 按下底色被字段圆角裁掉；分隔线改用字段自身描边（`--aeterni-button-group-divider: var(--aeterni-control-border)`），不再因 `IconButton` 缺少 `--aeterni-button-divider-*` 让 `border-inline-start-color` 在计算值阶段失效并回落到 `currentColor` 的深色墨线。`IconButton` 同步补上 45% / 32% 两档分隔线 Token，供其他透明按钮组复用。
- [x] **回归断言**：`tests/browser/verify.mjs` 新增「锁滚动不再让固定页头与正文左锚线错位」「锁滚动用 scrollbar-gutter 而不是 body padding」「点击 Confirm 外围每次都只抖一下、不重播进场」「InputNumber 分隔线复用字段描边」「方角步进底色被圆角裁切」五项。
- 验证：例外：本机 Edge 不支持 CDP `setDeviceMetricsOverride`，视口退化为 360×489，4 条几何断言失真；其中 Dialog 居中、遮罩铺满视口两条后由 10.40.1 复核为 `scrollbar-gutter` 系统性缺陷。
- 按「修复与优化递增第三位」将版本从 `10.38.2` 直接升到 `10.38.4`（`10.38.3` 批次没有单独提交与发布，两批改动同批提交）：`Directory.Build.props`、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.38.3 设计审核修复：浮层动效、状态墨阶与表单测量（未单独发布，随 10.38.4 一并发布）

对 10.38.1 之后 UI / 色彩 / 动效三层审核与组件板审核落地的修复批次；不新增公共 API，不改 Token 色板、玻璃配方与组件对外语义。

- [x] **浮层动效**：Dialog 面板补 200ms 进场（此前只有遮罩淡入、面板瞬时出现），退场保持 150ms；遮罩补同一 150ms 淡出，关闭时等待面板实测退场动画结束后再移除节点（遮罩同帧同长淡出），关闭过程页面始终处于暗场。Drawer 遮罩补 300ms 进场 / 210ms 退场，与面板同帧开始、同帧结束（此前遮罩瞬时出现、消失）。
- [x] **状态墨阶**：Upload 文件行的元信息（文件大小等）改用 `--aeterni-text-secondary`：它用于判断超限原因，按正文可读信息处理；此前用 `--aeterni-text-tertiary`，在 `--aeterni-bg-tertiary` 行底上浅色仅 4.08:1、深色 3.75:1。状态文字继续取语意 accent 档，靠状态色条与文案区分，不再依赖弱化墨色；FormField 的说明与错误文案改用 `--aeterni-line-height-normal`，12px 字号下从 1.2 提到 1.5。
- [x] **表单测量**：Slider 的填充、滑块槽与竖向轨道改用 `transform: scaleX()/translateX()/scaleY()`，轨道保持满宽，RTL 镜像与禁用态不变；Progress 的确定态改为一层 `transform: scaleX(Value/Max)` 的满宽填充，不确定态沿用独立扫动关键帧。
- [x] **提示卡**：NoticeCard 徽标从不透明底改为 `color-mix(accent 16%, --aeterni-bg-solid)`，语义图标对比度从 2.26–2.79:1 提升到 4.81–7.83:1；Alert 倒计时条不再给 `--aeterni-dialog-duration` 写 `5s` 字面量兜底，时长由 `NoticeCard` 以内联自定义属性写入。
- [x] **示例宿主**：页头字标与当前导航项改用 `--aeterni-color-brand-text`，绿色 / 橙色品牌下不再落到 4.42:1 的品牌填充色；首页统计直接渲染终值（57 / 9 / 2 / 3），示例画布控制条标注「可调参数」；Upload 拖放区与状态文案中文化，Button / MenuButton 画布控件标签与勾选项中文化（日期与时间控件的中文文案已在更早批次落地，不重复计入本批）；Descriptions 默认变体去掉逐项行线（行线归 bordered 变体），示例换成业务文案；Colors 页验收表校正聚焦环口径并新增无效态聚焦环、语意族文字、语意实心承字与语意墨在柔和底四组（验收表从 5 行扩到 9 行）；Drawer 页删除与正文重复的实现说明。
- [x] **回归断言**：`tests/browser/verify.mjs` 新增 Dialog 进场与退场同步、Drawer 遮罩与面板同步、Slider / Progress 的 transform、FormField 行高、Upload 中文文案、Descriptions 分隔线、页头品牌墨、首页统计终值等检查；`run.sh` 以 1440×900 启动窗口。
- [x] **复核收尾**：`aeterni_ui.css` 的「冷中性」注释改为「墨阶名称、不是文字冷偏」，Colors 页同步该口径并校正验收表覆盖范围；`verify.mjs` 的 P7 连续轨道断言改为按轨道比例 `pressAt` 后读取提交值（轮询到 75% 落定），不再依赖单次拖拽时序。
- 验证：例外：本机 Edge 不支持 CDP `setDeviceMetricsOverride`，4 条几何断言失真（搜索面板居中、遮罩铺满视口、设置浮层与无锚点 Popover 锚定）。
- 按「修复与优化递增第三位」从 `10.38.2` 升到 `10.38.3`（该版本没有单独提交与 tag，改动随 10.38.4 一并发布）：`Directory.Build.props`、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.38.2 组件库本体审核修复：Descriptions 对齐、圆角口径与 Link 语义

对 10.38.1 之后组件库本体审核（57 个组件、组件级 CSS/Token 与组件板）落地的修复批次；不新增公共 API。

- [x] **Descriptions 跨列对齐**：`.aeterni-descriptions__item` 的标签列从 `minmax(7rem, .7fr)` 改为固定 `--aeterni-descriptions-label-width`（默认 `7rem`），值列 `minmax(0, 1fr)`。此前 `Span>1` 的 item 更宽，标签列随 `fr` 放大，`说明` 行的值比普通行右移约 150px；修复后所有行的值起点一致（浏览器断言 spread ≤ 1px）。
- [x] **圆角口径纠偏**：明确 `Button` 使用独立的 `--aeterni-radius-button`（单值 `0.625rem`，三档统一），`Input` / `Textarea` 走 `4 / 6 / 8px` 字段阶梯；修正 `delivered-features` 四处、`engineering-reference` 两处“同档 Button/Input 圆角一致 / 同源”的错误规格（历史 release-history 条目保持原样，不重写历史）。示例站 Colors 页原有说明不变。
- [x] **Link 变体静止态**：`.aeterni-button--link` 常驻下划线（`text-underline-offset: 3px`），禁用时移除；与 Ghost 的区分不再依赖 hover。
- [x] **控件描边对比度口径**：确认 `--aeterni-border*` / `--aeterni-control-border` 是低强度 hairline（浅色对白面约 1.2–1.5:1），为显式例外而非漏检；修正 `check-contrast.mjs` 的 `GRAPHIC_FLOOR` 注释与 `engineering-reference` §5.4 中“边框逐组测量”的表述，并写入例外说明。本轮不把基础描边加黑，避免全库视觉回归；需要更强边界的宿主应新增 `--aeterni-control-border-strong` 一类 Token 另评。
- [x] **Small Segmented 混排约束**：在 `delivered-features` 补“不与 28px Button/Input 同排混用”的使用约束；32px 的几何取舍本身不变。
- [x] **回归断言**：`tests/browser/verify.mjs` 新增 Descriptions“所有 value 的标签列偏移一致（spread ≤ 1px）”检查。
- 验证：例外：`tests/browser/run.sh` 因本机 Chrome CDP `setDeviceMetricsOverride` 环境项未跑通。
- 按「修复与优化递增第三位」从 `10.38.1` 升到 `10.38.2`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.38.1 示例站精修、动效校准与大屏口径修正

`10.38.0` 之后的设计审核（UI / 色彩 / 动效三层）落地的精修批次：只调整示例宿主、示例文案与动效时长，不新增公共 API；组件 Token 与语义色板未变。

- [x] 示例画布：`.component-preview__canvas` 补 `box-sizing: border-box`，最小高度改为 `clamp(96px, 12vh, 140px)`、内边距 24px；单控件示例不再保留 226px 的展示柜高度（Button 页 4,145 → 3,760px，DatePicker 页 5,868 → 5,660px），点阵与 hairline 保留。
- [x] 运营大屏：分组环按条目数降序排列，品牌紫明度档与中性灰交替；图例改为「名称 + 数量 · 百分比」两行；环心由 57 组件改为 64 导航条目，与分段合计及 `aria-label` 口径一致。
- [x] 移动端：Button「变体与语意对照」加行标签（变体 / 语意 / 加载），≤600px 用两列 grid，加载行的忙碌指示不再被误读成字母前缀；首页组件墙预览压到 72px、meta 收成一行（移动端首页 4,754 → 3,548px）。
- [x] 示例内容：Drawer 由「示例面板 / 占位段落 1–6」改为真实的「通知偏好」（三个开关 + 通知邮箱），实现说明留在正文；示例宿主统一 `DatePickerPlaceholder = "选择日期"`，中文页面不再混用 `Select date`。
- [x] 侧栏：新增 `wwwroot/js/sample-nav.js`，路由变化后把当前菜单项滚入侧栏可视范围（DatePicker 等深条目不再停在折叠线以下）；脚本缺失时静默降级。
- [x] 动效：`Segmented` 选中滑块 600 → 200ms（normal 档）；主题/品牌切换 600 → 300ms，且只过渡颜色承载属性（color / background-color / border-color / outline-color / box-shadow / fill / stroke），不再把 transform / opacity 计入整页切换；`ThemeProvider` 的过渡类清理时长改读 slow 档（320ms 上限）；Popover 退场 80ms（进场 100ms）、Dialog 与 Notice 退场 150ms（进场 200ms）、Drawer 退场 210ms（进场 300ms），退场统一用加速曲线。
- [x] 文档：`engineering-reference` §6.4 增补状态切换、主题切换与浮层退出的时长规则；`delivered-features` 更新主题过渡与 Alert 退场时长；`AGENTS.md`、五份版本头文档、README tag 示例与 `Directory.Build.props` 同步 10.38.1。
- 验证：例外：`tests/browser/run.sh` 在本机 Chrome 上因 `Target does not support metrics override` 中断（中断前多条断言 PASS）。

## 10.38.0 `Anchor` 页内锚点导航（优先级 9 第 2 项交付）

`component-plan` §3.2 决策门结论的实现。长页面此前只能手写 `<a href="#id">` 再自己维护 scroll spy，活动项判定、粘性页头留白与 URL 片段处理没有共享实现；本批交付 `Anchor`，只做段导航与当前项标记，不接管路由。

- [x] 组件 `Components/Anchor/`（`.razor` / `.razor.cs` / `.razor.css` / `.razor.js`）：参数 `Items`、`ContainerId`、`Offset`、`UpdateLocation`（默认 `true`）、`Smooth`（默认 `true`）、`AriaLabel`；模型 `AnchorLink(Id, Title)` 与组件同文件（与 `BreadcrumbItem` 同一约定），`Items` 的 null、空 id、重复 id、空标题都在参数设置阶段拒绝。
- [x] 语义：`<nav aria-label>` + `<ol>` + 原生 `<a href="#id">`；当前项唯一带 `aria-current="location"` 与 `is-active`，其余不带；活动项变化不改变 Tab 序、不抢焦点。
- [x] 当前项判定：JS 用 `IntersectionObserver`（root 为容器或文档）触发、按真实滚动偏移取「最后一个越过 `Offset` 线的段」；滚到滚动区间底部时，最后一段直接成为当前项——哪怕它短到顶部从未越过 `Offset` 线、而更早的段仍然越过（点击最后一条链接后 `aria-current` 会立即落到该链接，而不是停在上一段）。结果经 `[JSInvokable] OnActiveSectionChangedAsync` 回传 C#，只在值变化时重渲染，未知或已被新参数移除的 id 归一为空。
- [x] 点击：JS `preventDefault` 后在容器或文档内滚动到「段顶 − `Offset`」，平滑与否按 `Smooth` 与 `prefers-reduced-motion` 收敛；`UpdateLocation` 开启时只用 `history.replaceState` 替换片段（保留当前 path/search，避免 `<base href="/">` 把地址改写成站点根），不产生历史条目、也不走 `NavigationManager`，因此不触发路由或组件重渲染。未知容器 id 与缺失目标段都退化为原生锚点行为。
- [x] `dispose` 释放 observer、click 与 scroll 监听；重复 attach 幂等；无 `IntersectionObserver` 的环境退化为滚动计算。文案 `AnchorLabel` 接入 `AeterniUITextOptions` 与 `FillEmptyFrom`。
- [x] 示例页 `/components/anchor`（文档滚动、内部滚动容器、`Offset` + `UpdateLocation` + `Smooth` 三组 demo）并登记进 `ShowcaseCatalog`；API 表与搜索索引生成物重跑。
- [x] 第一版明确不做：嵌套子级、affix（吸附布局）、路由跳转、自动生成目录内容。
- 验证：例外：`tests/browser/run.sh` 本机复跑 144/150，6 条失败均为既有几何断言（Segmented Home/End 滚动量、Upload 点击落点、设置浮层与 Popover 几何、卡片内边距），随本机无头视口（500×450 / 750×438）变化；CI 按固定环境复跑。
- 按「功能新增递增第二位」（§4.9）从 `10.37.0` 升到 `10.38.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.37.0 `Transfer` 双列表迁移（优先级 9 第 1 项交付）

`component-plan` 决策门结论（§3.1）的实现。`MultiSelect` 把候选藏在弹层里、只回显已选标签，无法同时看清「还剩什么」与「已选什么」；本批交付 `Transfer`，两列并列常驻，勾选后批量迁移，数据与排序仍归宿主。

- [x] 组件 `Components/Transfer/`（`.razor` / `.razor.cs` / `.razor.css` / `.razor.js`）：`Transfer<TItem>` 参数 `Items`、`SelectedValues` + `SelectedValuesChanged` + `SelectedValuesExpression`、`TextSelector`、`DisabledSelector`、`ItemTemplate`、`SourceTitle` / `TargetTitle`、`Searchable` + `SearchPlaceholder`、`Size`、`Invalid`、`Required`、`EmptyContent`、`AriaLabel` / `AriaDescribedBy`。
- [x] 值语义：目标列顺序即 `SelectedValues` 顺序，迁入追加到末尾；`SelectedValues` 必须是 `Items` 子集，参数同步时剔除越界值并回调 `SelectedValuesChanged`（与 `List` 同一约定）；勾选是瞬态集合，不写回宿主，过滤变化按列清空勾选，「全部迁移」只作用于当前可见且未禁用的条目。
- [x] ARIA 与焦点：根是 `role="group"`；两列各是一个 `role="listbox"` + `aria-multiselectable="true"`，列标题同时作为可见标题与可访问名；条目是 `role="option"` + `aria-selected="true|false"`（字符串），禁用条目输出 `aria-disabled` 且不参与导航、勾选与迁移。两列各只有一个 Tab 停留点，焦点留在容器上、活动项由 `aria-activedescendant` 指向，迁移按钮是原生 `<button>` 且不进入选项 Tab 序；`↑↓`/`Home`/`End` 跳过禁用项移动，`Space`/`Enter` 切换勾选。
- [x] JS 只做两件浏览器行为：把列容器上的导航/勾选键默认行为取消并转发给 C# 的 `[JSInvokable]` 入口（`List` 同一模式），以及把活动项滚入所在列视口；业务状态全部留在 C#。两列是滚动容器，滚动条按 `check-scrollbars` 成对声明，列高由 `--aeterni-transfer-list-max-height` 控制、宿主可经根 `style` 覆盖。
- [x] 表单：`FormField` 的 `Disabled` / `Required` / `Invalid` / 描述 id 与实例参数取并集，`SelectedValuesExpression` 接入 `EditContext` 校验；`Size` 非法值抛 `ArgumentOutOfRangeException`。
- [x] 文案 `TransferLabel` / `TransferSourceTitle` / `TransferTargetTitle` / `TransferSearchPlaceholder` / `TransferEmptyText` / `TransferMoveSelectedLabel` / `TransferRemoveSelectedLabel` / `TransferMoveAllLabel` / `TransferRemoveAllLabel` 接入文案表与 `FillEmptyFrom`。
- [x] 示例页 `/components/transfer`（基础用法、搜索与批量迁移、禁用项与表单状态、选项模板四组 demo）并登记进 `ShowcaseCatalog`；API 表与搜索索引生成物重跑。
- [x] 第一版明确不做：虚拟化、拖拽排序、逐列全选复选框、异步搜索、服务端查询。
- 验证：例外：同一轮全量回归的 6 条失败均为既有几何/时序断言，原因见 10.38.0。
- 按「功能新增递增第二位」（§4.9）从 `10.36.0` 升到 `10.37.0`（随后由 Anchor 批升到 `10.38.0`）：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.36.0 日期组件农历叠加：`ShowLunar`

日期族此前只渲染公历，交付边界明确写着“不包含自定义日历系统或复杂本地化历法”。本批增加可选的中国农历副行，默认关闭，主日期体系、绑定值和触发器格式都不变。

- [x] `DatePicker` / `DateRangePicker` / `DateTimePicker` 新增 `ShowLunar`（默认 `false`）并转发给内部 `DateCalendar`；只影响日历格渲染与日格无障碍名称。
- [x] 农历换算不新增数据表：使用 BCL `System.Globalization.ChineseLunisolarCalendar`，覆盖公历 **1901-02-19 – 2101-01-28**；初一显示月名（闰月带 `闰` 前缀），其余显示日名；范围外日期照常渲染公历数字、不显示农历副行。
- [x] 日格无障碍名称追加 `DatePickerLunarLabel`（默认 `Lunar`）+ 完整农历日期（如 `Lunar 九月初十`）；文案表新键接入 `FillEmptyFrom`，示例站覆写为 `农历`。
- [x] `DateCalendar` 新增 `is-lunar` 根状态与两行日格布局；农历行消费 `--aeterni-font-size-xs`，选中 / 跨月 / 禁用分别改用 inverse / tertiary / disabled 墨色，不整块降低透明度。
- [x] 明确不做：节气、干支、生肖、黄历宜忌、历法切换、时区换算；节气需要太阳黄经计算，与本批的日期显示目标无关。
- [x] 示例页 `/components/date-picker` 新增「农历叠加」demo（单日与范围两种选择器），API 表与搜索索引生成物重跑。
- [x] 修复生成器同名成员 `since` 查找：`scripts/generate-api-reference.mjs` 在页面内出现多个同名参数时按 `api-since.json` 约定的「声明类型.成员名」再查一次，避免拼成 `DatePicker.DatePicker.ShowLunar` 而永远为空。
- 验证：例外：`tests/browser/verify.mjs` 全量在本机停在 `/components/upload`（既有环境项）。
- 按「功能新增递增第二位」（§4.9）从 `10.35.0` 升到 `10.36.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.35.0 Cascader：层级单选组件（优先级 9 第 1 项交付）

`component-plan` 优先级 9 的第 1 个候选；决策门结论原先登记在该文件 §3，本批交付后连同计划条目一并撤下。层级数据此前只能用平铺的 `ComboBox`／`MultiSelect` 表达——选「省/市/区」要拆成三个控件并手写级联逻辑。

- [x] 组件 `Components/Cascader/`（`.razor` / `.razor.cs` / `.razor.css` / `.razor.js`）：`Cascader<TItem>`，参数 `Items`、`ChildrenSelector`、`TextSelector`、`Value`+`ValueChanged`+`ValueExpression`、`Placeholder`、`Separator`、`Invalid`、`Required`、`Size`、`ItemTemplate`、`EmptyContent`、`AriaLabel`、`AriaDescribedBy`、`FullWidth`、`OnItemSelected`（与 `ComboBox` 同名同义）。
- [x] 值语义：`Value` 绑定**叶子**，祖先链由组件用 `ChildrenSelector` 反查后按 `Separator`（缺省取文案表 `CascaderPathSeparator`）拼接展示。未采纳「`Value` 用整条路径列表」的方案：它更显式但与 `ComboBox` 的绑定形状不一致，宿主设置初始值还要自己拼路径。
- [x] ARIA：触发器是原生 `<button role="combobox">`（`aria-expanded` / `aria-controls` / `aria-haspopup="listbox"` / `aria-activedescendant` / `aria-required` / `aria-invalid`），面板是**一个** `role="listbox"`，每一列是它的 `role="group"`。决策门原写「每列一个 listbox」，实现时改为分组：`listbox` 里再套 `listbox` 是非法的嵌套角色，列名改由分组的 `aria-label` 承担（第 1 列取文案表，其后各列取父节点文案）。
- [x] 焦点模型：焦点始终留在触发器，活动项用 `aria-activedescendant` 表达，复合控件仍然只有一个 Tab 停留点。
- [x] 键盘：`↑↓` 列内移动、`→` 展开下一列（活动项有子节点时）或移入已展开的下一列、`←` 回到上一列并落在刚展开的节点上、`Home/End` 跳列首末、`Enter/Space` 叶子确认或父节点展开、`Escape` 关闭。
- [x] 浮层复用 `PopupHost` + `Popover`（`aeterni_floating.js` 的定位、翻转、钳制、外部点击与滚动关闭），`Placement=BottomStart`、`CloseOnScroll="true"`，与 `ComboBox` 一致。JS 只做两件浏览器行为：把活动项滚进所在列视口、把活动列滚进面板视口（窄屏横向），并对导航键 `preventDefault`；业务状态全部留在 C#。
- [x] 文案表新增 `CascaderLabel` / `CascaderPlaceholder` / `CascaderColumnLabel` / `CascaderEmptyText` / `CascaderPathSeparator` 并接入 `FillEmptyFrom`。
- [x] 示例页 `/components/cascader`（基础用法、受控值与路径文案、尺寸与状态、选项模板与空列）并登记进 `ShowcaseCatalog`；示例 API 表与搜索索引生成物重跑。
- [x] 第一版明确不做：异步加载子节点、搜索过滤、多选路径、自由输入、虚拟化、逐节点禁用。
- 按「功能新增递增第二位」（§4.9）从 `10.34.5` 升到 `10.35.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与项目索引、五份版本头文档、README tag 示例。

## 10.34.5 焦点环统一与调轻：2px 细环、品牌浅档、全库一套画法

焦点环原来观感偏重（3px 实心品牌色 + 2px 外偏移），且各组件画法不完全一致；本批统一并调轻。

- [x] **几何收细**：`--aeterni-focus-width: 3px` → `2px`、`--aeterni-focus-offset: 2px` → `1px`（`aeterni_ui.css` 的单一定义处）。32 个组件样式表 / 70 处引用都消费这两个 Token，一处改动全库跟随；组件侧没有硬编码环宽。
- [x] **颜色调浅**：亮色主题的 `--aeterni-focus-color` 从 `--aeterni-brand-500`（`#795AD9`）改为 `color-mix(in srgb, var(--aeterni-brand-500) 50%, var(--aeterni-brand-400))`。品牌档位之间没有更浅的可用停靠点：直接换 `brand-400` 会让绿灯品牌掉到 2.72:1，低于 `check-contrast` 的非文字 3:1 下限；混合值停在所有色相层都稳过的地方（三品牌 3.37–3.78:1）。仍是实心不透明色，不是淡染。
- [x] **画法统一**：4 个日期/时间选择器（`DatePicker`、`DateRangePicker`、`DateTimePicker`、`TimePicker`）原本用 `outline: none` + `box-shadow: 0 0 0 …` 贴边画环，与其余组件「`outline` + `outline-offset`」的画法不一致；统一改为标准画法。内收环的两套公式（`calc(-1 * var(--aeterni-focus-width))` 与 `calc(var(--aeterni-focus-offset) * -1)`）也收敛为前者。最终是 25 个外扩环 + 7 个内收环（菜单、标签栏、分段控件等为避免被滚动容器裁切），两套语义清晰。
- [x] **不移除焦点环**：AGENTS.md §4.4 第 5 条要求 `focus-visible` 必须有清晰焦点环；焦点环是键盘用户唯一的焦点位置指示（WCAG 2.4.7）。组件全部走 `:focus-visible`，鼠标点击本就不画环。
- [x] `tests/browser/verify.mjs` 中断言 `--aeterni-focus-width === '3px'` 的那条同步改为 `2px`。
- 按「修复与优化递增第三位」从 `10.34.4` 升到 `10.34.5`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.34.4 第十一轮审阅收口：Segmented 选中态焦点环、Soft 接缝、模态滚动条与焦点归还

第十一轮全库审阅（REV-164～REV-177，14 条）全部收口。范围是 `10.31.1` 之后的新增/变更面（`Upload`、`Segmented`、`Popover` 锚点解耦、滚动条统一、`SplitButton`/`ButtonGroup` 连体接缝）加全库交互状态/ARIA/Token/JS 交叉复查。逐项证据见 [`review-plan.zh-CN.md`](review-plan.zh-CN.md)，本节只记修复结论。

- [x] **P0 `Segmented` 选中项焦点环不可见**：选中项 label 透明、底下是品牌色滑块，品牌色焦点环（负偏移画在 label 内侧）叠在品牌胶囊上等于隐形。修复：选中项焦点环改用 `--aeterni-text-inverse`（on-brand 白，对比度 4.52:1），保持负偏移几何不变。
- [x] **P1 `ButtonGroup` 透明接缝漏掉 `--soft`**：水平/垂直两处 `.aeterni-button` 透明接缝列表补 `--soft`（与 icon-button 列表、split-menu 规则同一 transparent 档），并顺手删掉死选择器 `--text`（`ButtonVariant` 无 text 值）。
- [x] **P1 `Dialog` 模态容器无滚动条声明**：`.aeterni-dialog-provider__dialog` 的 `overflow: hidden auto` 补 `scrollbar-width`/`scrollbar-color` + `::-webkit-scrollbar` 三件；`check-scrollbars.mjs` 正则补双值简写匹配，容器数 12→13。
- [x] **P1 模态 `Popover` 焦点归还双重且忽略 `RestoreFocusOnClose`**：`createFocusTrap` 改传 `restoreFocus: false`，由 `deactivate` 里受 `restoreFocusOnClose` 门控且带守卫的 microtask 独占归还；`Modal` 与 `RestoreFocusOnClose` 文档注释同步。
- [x] **P2/P3 卫生包**：`Upload` 根 div 去掉冗余 `aria-disabled`（`SplitButton` 同类）；`SplitButton.Placement`/`MenuButton.Placement` 补 `Enum.IsDefined`、`Items` 补 `ThrowIfNull`；`Popover` 非模态默认不再输出无名 `region` 地标（仅在提供 `AriaLabel` 时输出）；`ButtonGroup` 删死属性 `data-orientation`；`NoticeCard` 的 `2px` 间距改 `calc(var(--aeterni-border-width) * 2)`。
- 按「修复与优化递增第三位」从 `10.34.3` 升到 `10.34.4`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、五份版本头文档、README tag 示例。

## 10.34.3 连体接缝收敛到一档：SplitButton 的透明档从 20% 提到 45%

`SplitButton` 与 `ButtonGroup` 是同一类构造——两段按钮共享一条 1px 接缝——但透明度各自硬编码：`Button.razor.css` 给 SplitButton 的透明变体写 20%，`ButtonGroup.razor.css` 给同类变体写 45%。百分比是乘在 `--aeterni-button-color` 自带 alpha 上的，Neutral 档该变量为 `rgba(0,0,0,.78)`，实际缝色 alpha 因此只有 0.156，而 ButtonGroup 是 0.351。用无头 Chrome 读计算样式实测：默认档（Ghost + Neutral + Small）的接缝与宿主背景对比**浅色 1.43:1、深色 1.65:1**，低于 `engineering-reference` 给结构分隔线定的 1.7:1（浅色）基线，是库内最弱的一条连体缝——静态下两段读起来像两串独立文本，只有 hover 出现填充块时才看出是一个控件。

- [x] **档位提到 Button 层**：在 `.aeterni-button` 的意图变量旁新增 `--aeterni-button-divider-solid`（自身前景色 32%）与 `--aeterni-button-divider-transparent`（强调色 45%），SplitButton 的接缝直接消费它们，两处不再各写一套数字。
- [x] **ButtonGroup 收敛到同一档位**：容器上内联的 `--aeterni-button-group-divider: color-mix(foreground 32%)` 删除，四条接缝规则改为 `var(--aeterni-button-group-divider, var(--aeterni-button-divider-…))`——组名保留为宿主覆盖钩子，覆盖后实心与透明两档同时改色（原先只有实心档受控）。变量必须在使用它的按钮元素上解析：容器上的转发会被判为 invalid at computed-value time，这也是它不能改成"容器定义、子项继承"的原因。
- [x] **实测结果**：透明档接缝 alpha 0.156 → 0.351，与宿主背景对比浅色 **2.44:1**、深色 **3.65:1**；实心档仍是 32%（`0.249725`）不变；SplitButton 在 Outline 下外框与接缝的落差从 5 倍收到 2.2 倍，与 ButtonGroup 的 1.7 倍同量级。
- [x] 文档：`engineering-reference` 的 ButtonGroup 那条原本写"分隔线由 ButtonGroup 管理，不修改 Button 核心样式"，与实现不符（接缝几何一直住在 Button 的隔离样式里——父组件的隔离样式到不了子组件根元素），改为写明归属与共享档位；`delivered-features` 的 SplitButton 与 ButtonGroup 两处同步档位名与实测对比度。
- [x] 记录一条未采纳的方案：曾考虑改用不透明的 `--aeterni-separator`（浅色 1.7:1 / 深色 1.5:1），但它会在实心按钮上造出与 32% 前景色缝语义不同的第二条线，且对比度反而低于 45% 档，因此不用。

## 示例工程批次 7：版本入口与反馈闭环（版本号未递增）

示例工程信息架构的最后一批（`component-plan` §4 批次 7）收口：两件事都属于示例站自身的「可运行契约」，不新增库侧公共 API，因此版本号不递增。

- [x] **预览状态矩阵（后于本批下线）**：本批曾交付外观设置浮层里的「预览状态」分组（RTL / reduced-motion / 窄屏三个示例级开关，由 scoped 的 `ShowcasePreviewModel` 驱动、浮层与外壳共享）。实测后确认它对示例站只能产生**部分**效果：RTL 在示例 CSS 里逻辑属性与物理属性各半，翻出来是半 RTL 的错位样子；窄屏只覆盖部分容器；减少动效只压得动时长 Token，GSAP 与写死时长的动画照旧。作为「预览」价值不足，遂整组下线——`ShowcasePreviewModel`、设置浮层分组、外壳 `dir`/修饰类与两条 CSS、以及 `sample-docs` 与浏览器断言里的对应检查一并移除。
- [x] **版本入口**：页头品牌名右侧新增版本徽标 `v10.34.3`，从示例程序集的 informational version 读取（SourceLink 追加的 `+commit` 在展示时去掉），指向 GitHub 发布列表。
- [x] **反馈闭环**：`ComponentPage` 正文末尾新增 `ShowcaseFeedback`——「有用/无用」点后回一条 Toast（静态站点没有后端，不伪造「已记录」），「提交 Issue」（按当前页标题与路由预填）与「贡献者」通往仓库。
- [x] **断言**：`tests/sample-docs.test.mjs` 新增骨架断言（版本入口与反馈闭环）；`tests/browser/verify.mjs` 新增浏览器断言覆盖版本徽标文本与发布链接、点「有用」弹 Toast 且 issue/贡献者出口存在。预览矩阵下线后，其三条断言与「设置浮层三分组」检查一并移除，分组断言回到两个。

## 10.34.2 滚动条统一：滑轨透明、十二个可滚动容器一套写法

示例站的搜索面板报“滚动条的白色背景太显眼”：那个容器自己用 `scrollbar-gutter: stable` 预留了滑轨槽，却没有消费 `--aeterni-scrollbar-*`，于是走浏览器默认外观——约 15px 宽的实色滑轨常驻在玻璃面板上。顺带查出库内的滚动条**本来就不统一**：十二个可滚动容器里，三个写了完整的两套声明，五个只有标准属性（Safari 与旧 Chromium 会回落默认外观），四个什么都没写。

- [x] **十二个容器收敛到一套写法**：`Autocomplete`、`ComboBox`、`DateRangePicker` 与 `DateTimePicker` 的面板、`Drawer`、`List`、`MultiSelect`、`Tabs`、`Timeline`、`VirtualList`、通知堆栈统一为「块内 `scrollbar-width: thin` + `scrollbar-color`；块外 `::-webkit-scrollbar`（按滚动主轴只写 `width` 或 `height`）、`-thumb`、`-track` 三条」。
  - `TimeOptionList` 是刻意的例外：它要隐藏滚动条，写法统一为 `scrollbar-width: none` + `::-webkit-scrollbar { display: none }`——原本只有 webkit 那一半，Firefox 上仍会画出滚动条。
- [x] **`scrollbar-width` 不继承**（实测才发现，第一版按“两者都可继承”写，结果只改到根滚动条）：只有 `scrollbar-color` 是继承属性。所以“宿主写一处、全站跟随”只对颜色成立，宽度必须对滚动元素本身声明——示例站 `showcase.css` 因此写成 `:root { scrollbar-color: … }` + `* { scrollbar-width: thin }` 两条，并把这条差异写进了 Token 注释与 `engineering-reference` §5.8。
- [x] **门禁**：新增 `scripts/check-scrollbars.mjs`（CI 的 build job 内，紧跟 CSS 注释检查）。它扫描库内所有 `.razor.css`，逐个检查带 `overflow: auto|scroll` 的规则块是否声明了 `scrollbar-width`、可见型是否同时具备 `scrollbar-color` 与 `::-webkit-scrollbar`（隐藏型则要求 `display: none`），并断言 Token 文件的滑轨仍是透明、示例站仍保留宿主级声明。**负向验证**：临时改坏 `List` 的 `::-webkit-scrollbar` 选择器 → 门禁精确报出“缺少 ::-webkit-scrollbar 规则，Safari 与旧 Chromium 会回落到默认滚动条”。
- [x] 文档：`delivered-features` 的滚动条 Token 条目改写为“十二个容器 + 两类声明成对 + 宿主怎么写”；`engineering-reference` 新增 §5.8（三个 Token、两类声明、继承差异、隐藏型写法、门禁）；`AGENTS.md` §4.8 门禁清单加一行。按“修复与优化递增第三位”（§4.9）从 `10.34.1` 升到 `10.34.2`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与当前版本、六份版本头文档、README tag 示例。

## 示例工程：玻璃与模糊对比页下线（版本号未递增）

`/components/glass` 是玻璃配方早期的实验台——把填充 × 模糊 × 饱和度 × 遮罩摊成八节对照矩阵。配方结论早已落进 Token 层与 `engineering-reference`（填充 `68%`／`72%`、两支 on-glass 墨、四条角色名接管），页面本身只剩历史读数，而填充或墨每改一次都要跟着重测一遍（`release-history` 里那两行「推翻旧读数重测」就是它的维护代价）。示例站不再保留它。

- [x] 删除 `GlassLab.razor`、`GlassLab.razor.css`，以及只为它服务的 `GlassContrast.cs`（其中的 `GlassPalette` 读数一并消失）。
- [x] 目录条目 `glass` 从「设计基础」分组移除；`Colors` 与 `Theme` 两页的「相关组件」不再指向它。
- [x] 门禁同步：`tests/sample-docs.test.mjs` 的 `NON_COMPONENT_PAGES` 与 `tests/browser/verify.mjs` 的非组件页循环都收窄到 `colors`；搜索索引重新生成（482 条 / 61 页）。
- 未改动：库侧零改动。玻璃配方与那两支墨的事实源仍是 `delivered-features` 与 `engineering-reference`；版本号未递增。

## 示例工程组合用法页（批次 6，版本号未递增）

批次 6 在示例工程里开一条新线：不按单个组件、而按“把几个组件串成一条链”组织页面。目录新增「组合用法」分组（`IsComponent = false`，路由沿用 `/components/<id>`，因此侧栏高亮、前后导航、`EditUrl`、搜索索引与门禁全部照旧）。本轮交付第 1 页，并把它做成后续四页的模板。

- [x] **新增组合用法页「表单校验链」（`/components/form-validation`）**：三段 demo——提交时校验（`EditForm` + `DataAnnotationsValidator` + `FormField For`）、输入时校验（`Invalid` / `Error` 直接驱动字段）、失焦时校验（`Input.OnBlur` 里给结论）。三段只消费既有公共 API，页面本身不新增任何 API。
- [x] **顺带修掉一个同源缺陷：`Avatar` 页少了整整一段**。`Description="…Alt="" 表示装饰图片…"` 里的 `""` 被 Razor 当成属性值结束，余下文本变成属性名，`avatar-image` 这张卡片在建树时抛 `does not have a property matching the name …` 整段消失（页面上只剩两段 demo）。文案改为「Alt 传空字符串」后三段全部回来。**`Alt=""` 这种组件参数写法是合法的**（解析出来就是空字符串），出问题的只有属性值里的裸引号——这也正是新门禁要拦的东西。
- [x] **门禁：把这条坑变成断言**（`tests/sample-docs.test.mjs` 新增一项）：扫描每个 `ComponentPage` / `DemoCard` / `ShowcaseCode` 开标签，属性值里不得有落单的双引号。解析器必须同时是引号感知与 `@(...)` 感知：`For="() => Project.Name"` 里的 `>` 不是标签结束，表达式里的引号也不参与配对。**负向验证**：把 `Avatar` 页的文案改回 `Alt=""`，门禁精确点名 `Avatar.razor 的 <DemoCard>`。
- [x] **`FormField Required` 与 `EditForm` 提交互斥**（写这一页时踩到）：字段把必填传给内部控件、控件输出原生 `required`，浏览器于是先用原生气泡拦下提交，框架的 `OnInvalidSubmit` 与字段消息都不会执行（实测：点提交后 DOM 毫无变化）。示例给 `EditForm` 加 `novalidate`，把拦截交给框架；页面的「说明与边界」与代码片段都写明这条取舍。
- [x] **断言**：`tests/browser/verify.mjs` 新增一组「组合用法页」断言（三段 demo 各带可复制代码、提交被拦下时消息落在该字段且控件带 `aria-invalid` / `aria-describedby`、补齐必填后提交通过、输入即标红、失焦才给结论）。形态先在本机用独立 CDP 脚本跑通（**9/9**），全量由 CI 复核。
- [x] **修掉 `InputNumber` 页的整页中断**（用户报障）：第 3 段用 `TValue="int"` 绑了一个**非可空**的 `int` 字段，`@bind-Value` 生成的 `ValueExpression` 因此被编译器包了一层 `Convert`，`FieldIdentifier.Create` 抛 `contains a UnaryExpression which is not supported`，页面渲染停在那一段之前。字段改成 `int?`、预览改用 `@bind-Value`（三件套自动齐全），并在「说明与边界」与代码片段里写明**值类型组件的绑定字段要与 `TValue` 的可空形态一致**——这条对 `DatePicker`、`TimePicker` 同样成立。
- [x] **其余四页落地**（每页 3 段 demo、可复制代码、相关组件与说明与边界，只消费既有公共 API）：
  - **筛选工具栏**（`/components/filter-toolbar`）：`Toolbar` 把检索、状态下拉与清空排成一行；已选条件下沉成可关闭的 `Tag`（关掉一个只去掉那一个条件）；筛空时由 `Empty` 给出出路。机测 **8/8**，含状态下拉的真实选择路径与「关掉标签后列表重算」。
  - **设置抽屉**（`/components/settings-drawer`）：`Drawer` 的模态语义、关闭路径与焦点回归；核心是**草稿语义**——抽屉里改的始终是草稿，放弃只丢草稿、保存才写回并给 Toast；`Placement` / `Modal` 由控件驱动。机测 **8/8**，含「放弃改动后生效值一行未变」这条负向对照。
  - **通知与长任务**（`/components/long-tasks`）：进度 + 取消 + 结束通知的三段式；破坏性动作先 `ConfirmAsync` 拿到同意才动手；三个分片并行、汇总进度取平均、只发一次汇总通知。机测 **6/6**，含「取消停在当前百分比」。为此把 `ShowcasePageBase.Dispose` 改成 `virtual`，页面在销毁时取消还在跑的任务。
  - **大数据列表**（`/components/data-list`）：800 条日志上的检索、多选与批量动作条；选择由宿主保存、靠 `ItemKeySelector` 跨筛选保持，动作条按"有没有选中"出现。机测 **7/7**，含「800 条只渲染 26 行」这条虚拟化证据。
- [x] **示例宿主补齐中文文案**：库的默认文案是英文、由宿主本地化，而示例站此前只覆盖了 `TimePicker` 的 5 项，于是同一页里会同时出现「取消」和 `Cancel`。`Program.cs` 把确认与浮层的可见文案（`ConfirmAcceptLabel` / `ConfirmCancelLabel` / `DialogCloseLabel` / `AlertCloseLabel` / `ToastCloseLabel` / `DrawerLabel` / `DrawerCloseLabel`）一并对到中文。
- [x] **新增整站渲染体检**（`tests/browser/verify.mjs` 最后一条断言）：把页面源码里声明的 `DemoCard` 与真实渲染出的 `.component-demo` 逐页比对，并收集控制台里的框架级异常（`crit:` / `Unhandled` / `FieldIdentifier` 之类）。这一类缺陷编译期完全看不见，症状都是“页面静静地少一段”：`Avatar` 的「图片与回退」消失了很久、`InputNumber` 的这一段整页中断，而段数是目录、右栏 TOC 与搜索索引共同的事实源。体检先在本机用独立脚本跑通（**62/62** 页面段数一致、无未处理异常），全量由 CI 复核。
- 验证：例外：`verify.mjs` 全量受本机 `Emulation.setDeviceMetricsOverride` 限制，由 CI 复核。

## 示例工程内容补齐（批次 5，版本号未递增）

批次 5 把“组件页有骨架、三个非组件页没有”补成 **57 页同一套页面模板**：安装、设计基础与对比实验三页改用 `ComponentPage`，组件页补齐 `<Controls>`，门禁因此可以断言“每个页面都有可复制代码块、写了三段固定段”。

- [x] **三个非组件页并入页面模板**：`Install`、`Colors`、`GlassLab` 从“直接渲染进布局壳 + 自带 eyebrow / h1 / 编辑链接 / 整页卡片”改为 `ComponentPage` 骨架，三页因此同时得到「何时使用」「相关组件」「说明与边界」、右栏目录与搜索索引里的段条目（段锚点由骨架生成，不会漂移）。
  - 它们自己的整页卡片边框（`.showcase-install`、`.showcase-colors`）随之删掉，外层卡片改由骨架的 `.component-preview` 承担——留着会卡片套卡片；`GlassLab` 的 `.glass-lab` 保留为变量宿主，`--glass-lab-*` 与 `data-backdrop` / `data-ink` 仍挂在同一个元素上（隔离样式的 `b-*` scope 实测仍在，22 个实验格照旧渲染）。
- [x] **可复制代码块**：`Install` 的四步从手写 `<pre><code>` 换成共享的 `ShowcaseCode`（复制按钮因此可用），`Colors` 补「在应用里消费」两段（消费 Token 的 CSS、换品牌色层的 C#），`GlassLab` 补「把结论搬回页面」两段（`Surface` 的 `Glass` 变体、宿主背景层）。三页都写明各自只读、候选值住在本页隔离样式里的边界。顺手把 `showcase-install__note` 改名 `showcase-page-note`（它早就是跨页的页级说明类，名字里带 `install` 只会误导），并删掉失去消费者的 `.showcase-install__lead` 与 `.showcase-page-eyebrow`。
- [x] **5 页补齐 `<Controls>`**：`IconButton`（AriaLabel / Variant / Intent / Size）、`Label`（标签文字与目标控件禁用）、`MenuButton`（Label / Variant / Size / Placement）、`SplitButton`（Label / Variant / Intent / Size）、`VirtualList`（Height / ItemSize / SelectionMode）。控件都作用在预览里的真实实例上，卡片说明同步写明“右侧控件即时作用在……”，54 个卡片化页面至此每页都有 02 段。
- [x] **门禁加固**（`tests/sample-docs.test.mjs`）：新增「每个页面都有可复制代码块，且写了三段固定段」（逐页断言 `<WhenToUse>` / `Related="` / `<Notes>`）与「非组件页与组件页共用同一骨架」（骨架锚点等于目录 id、`ShowcaseCode` 锚点唯一且至少两段）；原「每段 demo 都有自己的标题与代码」收窄到组件页，「片段用到了本页记录的组件」从“每段都要点名”放宽为“整页至少一处”——一页可能同时给形态片段与小节片段，逐段要求会逼出注水注释。浏览器检查新增三条真实渲染断言（安装页的骨架与四个代码块、设计基础与实验台两页的骨架与两段片段）。
- [x] 文档同步：`component-plan` §4 的目标结构表按实际取值改写（`<Controls>` 54/54、代码块 57/57、三段固定段 57/57、搜索索引 451 条 / 57 页），批次 5 标为已交付。`Feedback` 页“无组件类型的页也要有 API 区”那条既有断言不受影响：三页在 API 生成物里没有条目，`ComponentPage.HasApi` 为假，因此不渲染 API 段。
- 验证：例外：`verify.mjs` 全量在本机报 `Target does not support metrics override`（同 10.34.0/10.34.1 记录），由 CI 复核。
## 示例工程页头搜索改为居中弹出的模态面板（版本号未递增）

- [x] **入口换成"像输入框的按钮"**：`ShowcaseSearchBox` 不再是库内 `Input` 的宿主，改成一个原生 `button`（`aria-haspopup="dialog"` + `aria-expanded` + `aria-keyshortcuts`），字段外观直接取库内控件 token（`--aeterni-control-*`、`--aeterni-focus-*`、`--aeterni-radius-input`），右侧键位提示按平台写成 ⌘K / Ctrl K。读屏名字来自可见文案，因此不用 `aria-label` 覆盖它（避开"可见标签不在可访问名字里"）。
- [x] **面板是居中的模态对话框**：真正的输入框进 `ShowcaseSearchLayer`，面板只在打开时进入 DOM、固定定位在视口中央，`role="dialog"` + `aria-modal="true"`；点遮罩、Escape（焦点在输入框或关闭按钮上都算）与关闭按钮都收起它。关闭后焦点回到触发器；**从命中项跳走时不还焦点**，留给刚滚到的目标段。
- [x] **滚动锁与焦点陷阱复用库内共享模块**：`wwwroot/js/aeterni_floating.js` 的 `lockScroll()` / `createFocusTrap()`（Popover、Drawer、DialogProvider 用的同一份），示例不另造一套模态机制。全局 Ctrl/Cmd+K 开关由 `wwwroot/js/sample-search.js` 挂在 document 上——键盘事件只有挂在文档上，才能在任何焦点位置都拦得住，而 Blazor 的处理器绑在元素上。
- [x] **输入框按 id 交给 JS，不传元素引用**：父组件的 `OnAfterRenderAsync` 先于子组件执行，面板第一次挂载时 `Input` 的 `ElementChanged` 还没回填，元素引用那一趟必然是空的——第一版正是因此在打开后没把焦点放进输入框，Escape 与 Tab 的断言随后连锁失败。只传面板元素，输入框由 JS 用 `document.getElementById`（公开的 `InputId`）解析。
- [x] **面板渲染在页头玻璃卡片之外**：卡片的 `backdrop-filter` 会成为最近的 backdrop root，卡内浮层的模糊只能采样卡片内部那张透明背景——与 10.33.0 里 `Popover.Anchor` 要解决的是同一件事，只是这次连"锚定"都不需要。
- [x] 断言改写：原来的"就地下拉"一组换成居中面板的开合与几何——触发器的 dialog 契约、关着时面板不在 DOM 且无遮罩无滚动锁、点触发器后 dialog 语义 + 遮罩 + 滚动锁 + 输入框自动聚焦、面板中心与视口中心重合、面板与页面根之间没有别的 backdrop root、遮罩铺满视口、面板层级高于页头、Escape / Ctrl+K / 点遮罩三条关闭路径都把焦点还给触发器、Tab 五次仍留在面板内、Enter 落地后面板与输入框离场且滚动锁释放、关闭后页面重新可滚。REV-118 的注释同步（页头不再有 combobox，改成入口按钮）。
- 验证：例外：`verify.mjs` 唯一 FAIL 是既有的 `P7 PageDown steps...`，与本批无关。
- 本机环境项（非本次改动）：`generate-api-reference --check` 报"仓库 CRLF / 生成器 LF"（逐行内容一致，与 10.34.0 记录的是同一件事）；`generate-search-index.mjs --check` 因目录条目指向尚不存在的页面文件（`PatternDrawer.razor`）抛 `ENOENT`；`node --test tests/*.test.mjs` 有 1 项「目录里的每个条目都有页面文件」失败；`bash scripts/check-docs.sh` 在本机 WSL 下因工作区文件是 CRLF 而提前退出（`grep -E '^## [0-9]+\. Checkbox$'` 匹配不到带 `\r` 的行尾）。
  - `P7` 那一条：Slider 的同类断言通过，InputNumber 的 JS 抑制在探针里 `defaultPrevented:true`，失败只出现在 CDP 真实按键路径上，与本轮改动无关（本次未触碰 InputNumber）。
  - 以上均由 CI 复核。

## 10.34.1 Segmented 的圆角与内缩改成「形状一致 + 三档统一」

`10.34.0` 把滑块圆角从塌成 0 的直角里救回来，但走的是**同心**路线（滑块半径 = 轨道圆角 − 描边 − 内缩，内缩由此推出 1 / 3 / 5px），结果是滑块圆角被压成统一的 2px、看起来比它所在的轨道"小一圈"，而且边框到滑块的间距随 `Size` 漂移。本轮按组件库既定的风格改成：**滑块与选项直接取该档轨道的控件圆角**（4 / 6 / 8px，与同档 `Input` / `Button` 同源），**内缩三档统一 3px**。

- [x] **滑块/选项圆角 = 轨道圆角**：`--aeterni-segmented-inner-radius` 直接指向 `--aeterni-segmented-radius-rounded`，选中块与它所在的轨道是同一个圆角形状，不再是"比轨道小一圈"的内沿半径。
- [x] **内缩三档统一 3px**：`--aeterni-segmented-padding` 不再由圆角反推，改为 `calc(var(--aeterni-spacing-1) - var(--aeterni-border-width))`；`--sm` / `--lg` 都不再重复声明它。边框到滑块之间的间距从此不随 `Size` 变。
- [x] **连带的高度后果（用户知情后选择"统一 3px"）**：`轨道高 = 2(描边) + 2×3 + 24` 对小号是 32px，比 `--aeterni-control-height-sm`（28px）高 4px——它统一内缩后的可用高度只剩 20px，被 24px 热区顶了起来；默认档与大号不受影响（仍是 36 / 44px）。要让它回到 28px 只能把内缩压到 1px 以下，与"三档统一"不可兼得。`delivered-features` 的 `Size` 一条与 `engineering-reference` 的内边距预算段已按此改写。
- [x] **代价写进文档**：滑块圆角与轨道一致意味着两者不同心，描边与滑块之间的留白在角上比四边宽 1.66px（三档同值）。`engineering-reference` 现在把"同心 vs 同形"两条路线与各自代价摆在一起，并保留上一条教训——内缩不能由圆角反推：`Pill` 档会把被减项重指成 `9999px`，算出 `-9994px` 的内缩，`padding` 落到 0、滑块同时被 `inset` 撑到两万像素高。

## 10.34.0 Segmented 的 Home/End、圆角几何与禁用项热区光标

`Segmented` 的定位一直是"方向键与 roving tabindex 交给原生 radio，组件没有键盘 JS"，示例页却写着"Home/End 跳到两端"——实测原生 radio 组不实现这两个键，它们落到浏览器默认动作上把整页滚到顶或底（焦点在组内按 `Home`：`scrollY` 1856 → 0，按 `End` → 4690）。同一个控件里还查出两处几何/状态缺陷：默认档的滑块是直角，禁用选项的热区光标是手型。本轮把这三处一起收口，形状示例也改成圆角/胶囊并排切换。

- [x] **`Home`/`End` 在 C# 里补齐**：`HandleKeyDownAsync` 用 `FirstEnabledIndex()` / `LastEnabledIndex()` 跳到首/尾**可用**项（禁用项不参与，与方向键一致），选中仍走既有的 `SelectAsync` 路径，再用 `ElementReference.FocusAsync(preventScroll: true)` 把真实 DOM 焦点一起带过去——程序化 focus 一个 radio 不会选中它，而方向键是从**聚焦**项而不是选中项出发的，两者必须同步。
- [x] **新增 `Segmented.razor.js`（`Interactive = false`）**：只做一件 C# 做不到的事——`Home`/`End` 的默认动作发生在 DOM 事件里，而 Blazor 的合成处理器跑在它之后，纯 C# 版本会一边改选中、一边让页面跳到底。模块在 root 上挂一个 `keydown` 监听，只对 target 是可用 radio 且无修饰键的 `Home`/`End` 调用 `preventDefault()`；**不碰方向键**，否则原生 radio 的焦点 + 选中导航就没了。不需要 DotNet 回调，所以不参与 interactive init，`sync` 自建实例（`Checkbox` 是同一模式的先例）。
- [x] **默认档滑块的圆角（几何缺陷）**：`--aeterni-segmented-inner-radius` 原本是 `max(0px, 轨道圆角 − 描边 − 内缩)`，而默认档固定内缩 5px，`6 − 1 − 5 = 0` 被下限截成直角——圆角轨道里嵌着一个方角滑块（实测 `sliderRadiusUsed: 0`，而 Small / Large 两档恰好是 2px，三档并不一致）。
  - 修法是把同心等式**反过来解**：滑块与选项的半径先定为共享的一档（`--aeterni-radius-xs`，2px，三档相同），内缩再由 `档位轨道圆角 − 描边 − 滑块半径` 推导，三档因此得 1 / 3 / 5px，描边、滑块与选项三条弧重新同心（角上留白 == 四边留白）。
  - **默认档内缩 5px → 3px 是这条推导的必然结果**，副作用是它的滑块与选项从 24px 变高到 28px（仍在 24px 热区之上，轨道高度保持 36px 不变）。
  - `Pill` 档额外把滑块半径交回 `--aeterni-radius-full`、由半高裁切收成同心胶囊；**内缩公式只读圆角档的那两个变量**（`--aeterni-segmented-radius-rounded` 与 `--aeterni-radius-xs`），不读胶囊档会重指的 `--aeterni-segmented-radius` / `--aeterni-segmented-inner-radius`——第一版实现正是读了后者，`6 − 1 − 9999 = -9994px` 让 `padding` 计算为 0，同一串值又把滑块的 `inset` 撑成两万像素高（实测 `sliderH: 20022`、`gapInline: -9993`），所以两档的滑块宽度、位移与选中位置仍然完全一致这条现在是钉住的。
- [x] **禁用选项的热区光标**：`cursor: not-allowed` 原本只写给 `.aeterni-segmented__input:disabled + .aeterni-segmented__label`（内层文字），真正被鼠标命中的 `.aeterni-segmented__option` 仍从基础规则带 `cursor: pointer`——实测禁用项 `optionCursor:"pointer"` / `innerLabelCursor:"not-allowed"`，即"文字上是禁用、热区上是手型"。
  - 补 `.aeterni-segmented__option.is-disabled { cursor: not-allowed; }`，与 `Radio` 的 `.aeterni-radio:has(... :disabled)`、`ToggleGroup` 的 `__item:disabled` 同一约定；整组禁用时每个选项也带 `is-disabled`，所以这一条同时覆盖两种情况。
- [x] **形状示例改成并排对比**：示例的「形状档位」卡片改为「圆角与胶囊」，上下两排同屏——上排跟随 `Shape` 切换、下排固定是"另一档"，所以任何时刻都能对比两档；`Shape` 的代码片段也改成两档并排。`generate-search-index` 随之重生成（只有一个段标题变更）。
- [x] 文档与示例跟进：`delivered-features` §32 把"组件没有键盘 JS"改为"方向键仍完全归浏览器、组件只补 `Home`/`End` 并交出一个最小模块拦默认动作"，并重写内缩/圆角两条（改为"半径先定、内缩推导"）；`engineering-reference` §5.5 的两段（内边距预算、同心圆角）同步新公式，并记下"内缩不能手写"的教训与"四边留白量不出这类缺陷"的量化方法。按"功能新增递增第二位"（§4.9）从 `10.33.0` 升到 `10.34.0`：`Directory.Build.props` 四处、`AGENTS.md` 版本头与项目索引、六份版本头文档、README tag 示例。
- [x] 两个已知环境项，非本次改动：`generate-api-reference --check` 在本机报"仓库 CRLF / 生成器 LF"，但两个生成物**逐行内容一致**（`git diff` 为空，`git ls-files --eol` 为 `i/lf w/crlf`），是 Windows checkout 的行尾；本机整套 `verify.mjs` 也跑不完——第 1356 行的 `Emulation.setDeviceMetricsOverride` 报 `Target does not support metrics override`，失败点落在本次未触及的段落，`Homepage uses Timeline`（`pageOverflow:392`）与 `REV-146 Search hover`（`:hover` 未命中）两条既有的环境失败同样复现。
  - 均由 CI 复核。

## 10.33.0 Popover 锚点解耦与 Segmented 形状档位

两个公共 API 新增合成一次递增（`10.32.0` → `10.33.0`）：`Popover.Anchor` 与 `Segmented.Shape`，两者都在 `scripts/api-since.json` 里登记为 10.33.0，`Directory.Build.props` 四处齐动，`AGENTS.md` 版本头与项目索引、五份带版本头文档、README 发布示例同步。

- [x] **`Popover.Anchor`（`ElementReference?`）**：把"锚点元素"与"渲染位置"解耦——设了锚点就按它定位、翻转与贴边，浮层可以渲染到别处；"点击外部"判定也把锚点算作层内（否则点触发器会被判成外部先关一次、click 又打开，双触发）。
  - 不设锚点时行为与之前逐字节相同（契约检查里有一条断言比较设与不设锚点的渲染标记）。
  - 实现走新的模块方法 `setAnchor(instanceId, element)` 而不是加长 `setOpen(...)` 的参数表：锚点变化时可以只重排、不动开合状态机，模块内按元素去重所以重渲染不会反复重排；元素脱离文档时回退宿主；锚定模式用 `position: fixed` 并在落位前实测固定定位包含块的原点，因此祖先带 `transform`/`filter`/`backdrop-filter` 时也不会偏。
  - 监听器与 Observer 照旧在实例级 `dispose(instanceId)` 释放。
- [x] **`Segmented.Shape`（`SegmentedShape { Pill, Rounded }`）**：默认 `Rounded`，按"默认档不输出修饰类"的约定不带类名；`Pill` 输出 `aeterni-segmented--pill`。两种形态只重指三处共用的 `--aeterni-segmented-radius`（圆角档跟尺寸走：`--sm` 4px / 默认 6px / `--lg` 8px；胶囊档为 `--aeterni-radius-full`），**不动** `--aeterni-segmented-count/index`，所以选中指示块的宽度与位移不受形状影响。`OnParametersSet` 里做 `Enum.IsDefined` 校验，并在契约检查的非法枚举表里登记。
- [x] 连带效果（用户知情后选择"默认圆角"）：库内 `ThemeSwitch` 是 `Segmented` 的专用用法，默认外观随之从胶囊变为圆角，`delivered-features` 里相应表述已改为与渲染一致（"选中指示块"），并注明形状只改圆角。
- [x] **页头两个下拉的背景模糊**：根因是它们渲染在页头玻璃 `Surface` 内部，而该 Surface 自带 `backdrop-filter`、成为最近的 backdrop root，浮层自身的 `backdrop-filter` 只能采样那张透明卡片 → 面板看起来完全没有模糊。量化指纹是"去掉面板自身模糊前后像素完全一致"（`blurDelta: 0`），而旧套件只用平均色、**模糊保留平均色**，所以这个缺陷对既有断言不可见。修法即上面新增的 `Anchor`：两个浮层改由 `PopupHost` 渲染在页头之外、按触发器元素固定定位，示例顺带去掉为让开裁剪而加的 `Surface Style="overflow: visible"`。

## Segmented 描边与滑块留白，示例预览/代码切换改用 Segmented（版本号未递增）

- [x] **`Segmented` 轨道描边**：从 `--aeterni-border` 换成 `--aeterni-border-strong`（仍是 1px `--aeterni-border-width`）。分段控件读起来是控件而不是表面，强描边把它和所在外壳分开，同时不引出一层填充；禁用档与无效档照旧换描边色。
- [x] **滑块留白**：`--aeterni-segmented-padding` 从 `--aeterni-spacing-1`（4px）改为 `--aeterni-spacing-1` + `--aeterni-border-width`（5px），Small 档仍是 1px。
  - 这一个值同时喂给轨道内边距、滑块内缩与滑块行程，所以滑块仍逐像素落在选项列上（浏览器断言 `sliderWidth === optionWidth`、`offset ≤ 0.5` 保持通过）。
  - **5px 是上限而不是随手取的值**：默认档可用高度只有 `36 - 2(border) - 2(padding)`，选项的 `min-height` 是 24px 热区，5px 时两者恰好相等（实测轨道仍 36px、选项 24px、滑块四边留白 6px）；取 6px 会把轨道顶到 **38px**，与同档 Input / Button 相差 2px——这条权衡写进了 `engineering-reference` §5.5，并由新的浏览器断言钉住（高度档位、热区下限、滑块四边留白相等）。
  - Small 档若跟随 5px 同样会涨到 38px，因此按档位单独保留 1px。
- [x] **示例 demo 卡的预览/代码切换改用库内 `Segmented`**：删掉手写的 `role="tablist"` + 两个按钮 + `role="tabpanel"` 与 `HandleTabKeyDown`，换成 `Segmented<TValue="bool">`（`Items = [false, true]`、`TextSelector` 给"预览 / 代码"、`Size.Small`、`AriaLabel="{标题}视图"`）。单选语义、方向键与"整组一个 Tab 停留点"因此由浏览器对原生 radio 提供，示例不再自维护 tab 键盘模型；代码面板的折叠仍是真正不渲染（`hidden`），没有代码的卡片不渲染分段控件（一个选项的开关不叫开关）。
- [x] **结果列表行距**：页头搜索的命中列表加 `gap: var(--aeterni-spacing-1)`（与库内 `List` 同档）。每行自带圆角背景，贴在一起时活动项的底色与邻行连成一片，读者分不出是两行还是一个色块；这属于"示例自绘列表绕开了库内 List 的既有外观"，不是新设计。
- [x] 测试与门禁跟进：`.component-demo__tab` 断言改为按分段控件定位（点第二个选项开代码）、代码面板改按 `.component-demo__panel--code` 数 `hidden`；`showcase.css` 里随之失去消费者的 `.component-demo__tab*` 规则全部删除。
  - 新增两条真实渲染断言：**「分段控件切到「代码」后面板真的换过来」**（原生 radio 的 `change` → Blazor 重渲染这条路径静态检查看不见，实测 `codeHidden:false / previewHidden:true / checked:"代码"`）与**「搜索结果行之间留出 List 同档的间隙」**（实测 `gap:4px`，未修版本实测 `gap:0px`，构成负向对照）；Segmented 组另加**「默认档保持控件高度档位，且滑块四边留白相等」**（实测 `track=36/36px option=24/24px inline=6 block=6`）。
- 验证：例外：`verify.mjs` 116/117，唯一失败项「浮层的模糊作用在背后页面内容」(`blurredSharpness: 0`) 在未改动 HEAD 基线同样失败（`--disable-gpu` 无头环境的 `backdrop-filter` 限制）。

## 示例工程页内目录的两个报障修复（版本号未递增）

- [x] **点右侧目录会跳到首页**：目录链接原本写成 `href="#{段 id}"`，而 `index.html` 的 `<base href="/" />` 会让浏览器把它解析成 `/#{段 id}`——Blazor 的路由于是判定目标不是本页，单击目录就导航到首页路由。改为"当前页 + 片段"的完整地址（`ComponentPage.SectionHref`，按 `NavigationManager.Uri` 拼接并剥掉已有片段；站点挂在子路径下同样成立）。附注：`NavigationManager.GetUriWithHash` 在 .NET 10 **不存在**（引用程序集里只有 `GetUriWithQueryParameter(s)`），所以是手拼地址，不是调用框架 API。
- [x] **滚到页面底部时「相关组件」「说明与边界」不被选中**：滚动高亮的观察带是视口顶部三成（`rootMargin: '0px 0px -70% 0px'`），页尾的段永远进不了这条带；原断言当时是**绕开**了这一点（改用页中段，并在注释里写明"拿它断言等于在测浏览器能不能把页尾滚到顶部"），但绕开不等于读者不该在页尾看到正确高亮。`sample-docs.js` 现在在"滚动到底"这个状态上单独判定：到底时把最后一段判为当前段（页面一屏放得下时不适用）。
- [x] 浏览器检查：目录断言的 `deadLinks` 跟随新地址形态取值；`waitForCurrentSection` 按段 id 比较；新增三条断言——目录链接必须落在本页片段、**真实点击**目录链接后仍留在 `/components/upload` 且目标段滚进视口、滚到页面底部时最后一段（`upload-notes-title`）成为当前段并带 `aria-current`。

- [x] 新增 `scripts/generate-search-index.mjs`：解析 `Pages/Components/*.razor`，产出页头搜索框的数据源 `ShowcaseSearchIndex.generated.cs`（441 条 = 57 个页面条目 + 54 个组件页上真实存在的段）。
  - 段锚点与 `ComponentPage` / `DemoCard` 渲染出来的 id 逐字一致（`-when-title`、demo 卡片 `Id`、`-api-title`、`-related-title`、`-notes-title`），只登记页面上真的存在的段——没有 `<WhenToUse>` 就没有「何时使用」，「参数与事件」读的是 API 生成物（因此生成顺序是 API → 索引）。
  - 生成物提交进仓库，CI 的 build job 新增 `Search index is up to date`（`--check`），`AGENTS.md` §4.8 门禁清单与 §2 必读路由同步。
- [x] 新增数据形状 `ShowcaseSearchEntry`（PageId / PageTitle / Category / SectionId / SectionTitle + `Href`）与目录侧的路由规则 `ShowcaseCatalog.RouteFor`（安装页挂 `/components`），侧栏导航与搜索结果因此共用同一份地址来源。
- [x] 页头改为「常驻搜索框 + 设置入口」：`ShowcaseSearchBox`（库内 `Input` + 搜索图标 + `PopupHost`/`Popover` 就地下拉）与 `ShowcaseSettings`（库内 `Button` 触发器 + 浮层里的 `ThemeBrandSwitch` / `ThemeSwitch`，各带可见标签）。原先并排挂在页头的两个分段开关移进设置浮层，`app.css` 里只为它存在的 `.header-theme-switch` 随之删除；`Surface` 默认 `overflow: hidden` 会剪掉整块下拉，因此页头用公开的 `Style` 参数让开这次裁剪（未覆写任何组件规则）。
- [x] 交互与无障碍照库内 ComboBox / Autocomplete 的模型：输入框 `role="combobox"` + `aria-expanded` + `aria-controls` + `aria-activedescendant`，结果容器 `role="listbox"`、每条 `role="option"` + `aria-selected`；方向键循环、Home/End 到首尾、Enter 打开、Escape 与点击外部关闭且保留输入内容；空查询不展开下拉，无命中时给明确文案。命中后清空输入并跳到 `/components/{页}#{段}`。
- [x] 跨页跳转的滚动由示例自己补：框架只在同文档（路径与查询串都不变）时滚动 hash，从别的页面跳过来只 `history.pushState`。`wwwroot/js/sample-search.js` 负责"等目标段渲染出来再滚"（并返回成败），同时承担导航键默认动作的抑制与活动项滚动——与 `Autocomplete.razor.js` 同一做法，另有 `.component-demo` 等段的 `scroll-margin-top` 让落点停在固定页头下方。
- [x] 浏览器检查新增 15 项：搜索框常驻且声明 combobox 契约、空查询不展开下拉、输入关键词后命中减少（`组件` 416 条 → `时间` 3 条）且包含 Timeline / DateTimePicker 两页、无命中时给出明确文案、ArrowDown 移动活动项、Enter 后 `location.pathname + location.hash` 为 `/components/timeline#timeline-horizontal` 且该 id 的元素真实存在并进入视口、命中后收起并清空、Escape 与真实指针点击外部关闭但保留输入、设置浮层的 `aria-expanded` / 两个开关 / 关闭后焦点归还触发器。
  - **既有断言调整一处**：REV-118 的 `tabUntil` 起点与诊断查询锚定到 `#theme` 范围内——页头新增的搜索框是文档里第一个 `combobox`，不限定的诊断会量到那个没有焦点的输入框（实测 `outline-style=none`），断言测的不再是它声称的被重新指向的消费者。
- [x] 修掉卡片化重构留下的缺口：`Feedback` 页的三张手写表（`FeedbackParameters` / `FeedbackEvents` / `FeedbackContracts`）在页面改成卡片结构后失去了消费者——没有人把它们传给 `ComponentPage`，而该页没有对应组件目录、生成器又对空成员直接跳过，于是**页面上整个 API 区消失了**，门禁还把这页当例外放过。
  - 修法是让生成器支持"纯额外行"的页（成员为空但编辑文件里有行时照样产出条目），把这 21 行收进 `scripts/api-extra-rows.json`，删掉页面里的死数组，并把 `feedback` 从 `tests/sample-docs.test.mjs` 的例外集合里移除。
  - 生成物因此变为 **54 页 / 665 行**（原 53 页 / 644 行），搜索索引也随之多出 `feedback-api-title`（该页从此有「参数与事件」段）。
  - 浏览器检查新增一条真实渲染断言，守的不只是这一页，而是"例外一出现，缺口就没人看得见"这一类问题。

## 示例工程文档模板调整（批次 3 续，版本号未递增）

- [x] 侧栏检索独立成块：与导航分区之间加分隔线，让人一眼看出"上面是筛选、下面是条目"；移除条目计数——它是给开发者的信息，读者只需要筛选结果本身。
- [x] 页头从卡片外移进卡片内：名称（`ComponentPreview__meta`：类型名 + 「在 GitHub 编辑」）随卡片排版，卡片的左右边界因此与侧栏对齐。编辑链接改由目录提供（`ShowcaseCatalog.EditUrl`），布局不再持有它；`<h1>` 保留 `tabindex="-1"` 作为跳转落点与浏览器测试的确定 Tab 起点。
- [x] demo 卡的预览与代码改成 tab：预览默认显示，代码默认折叠（panel 带 `hidden`，真正不渲染，不是用样式藏起来），一组 tab 只有一个 Tab 停留点，方向键与 Home/End 在两个 tab 间切换，焦点随选中项走。
- [x] 卡片内边距统一到 `.showcase-layout` 上的 `--showcase-card-padding`（`clamp(16px, 3vw, 28px)`）：页头、demo 卡、API 段、说明段共用一处，任一段单独写 padding 都会让卡片与侧栏错开；API 表在窄屏改为横向滚动；1000px 以下侧栏叠到内容之上，避免两栏把内容压成窄字。
- [x] 断言跟进：检索块不再断言计数而是断言"计数已移除"；编辑链接改在卡片内断言；代码相关的断言先点开"代码"tab，并新增「代码默认折叠在 tab 后面」（三个页面抽查）。

## 示例工程 demo 卡片化（批次 3 起步，版本号未递增）

- [x] 页面模板换成市面组件文档的形状：新增 `ComponentPage`（标题区 → 何时使用 → demo 卡片 → API → 相关组件 → 说明与边界）与 `DemoCard`（标题、说明、实时预览、可选属性控件、可复制代码、自带锚点）。卡片化的页面不再给段编号——锚点与卡片标题已经表达了结构。
- [x] 把文档排版样式从 `ComponentPreview.razor.css`（CSS 隔离）搬到示例的文档样式表：同一套类名现在要跨 `ComponentPage`、`DemoCard`、`ShowcaseCode`、`ShowcaseApiTable` 四个组件复用，隔离样式只作用于声明它的组件，放在隔离文件里会让新组件丢样式。
- [x] 抽出两个共享件：`ShowcaseCode`（代码块与复制按钮，复制仍走应用级 event delegation）与 `ShowcaseApiTable`（生成式 API 表），`ComponentPreview` 的生成分支改为复用它，手写分支只留给 `Feedback`；基类参数清单移到 `ShowcaseApi`，不再随页面重复。
- [x] 表单组件组 15 页全部卡片化，每页 3～4 段演示、每段带锚点/说明/可复制代码，并补上「何时使用」（决定读者该不该用它、该换哪个）与「相关组件」（下一站指向），Notes 补齐到全组：
  - `Input`、`Textarea`、`InputNumber`（范围与步进）、`Slider`（步长与精度）、`Search`（提交/清空/加载）、`Autocomplete`（加载态与交互开关）、`Label`（原生 label 关联）、`Checkbox`（不确定态是显示态）、`Switch`（即时生效的二态）、`Radio`（组焦点模型与独立值绑定）、`Segmented`（禁选项与非字符串值）、`Rating`（只读展示与范围）、`FormField`（包装原生控件）、`MultiSelect`（批量动作与禁用项）、`Upload`（宿主驱动传输进度）。
  - 编译坑逐条记下：`InputNumber` 与框架同名类型冲突必须写全名并显式 `TValue`，`TValue=decimal` 时数值字面量要写 `@(0.5m)`，绑定字段需可空；示例导入了库的 `List` 与 `System.Collections.Generic.List<T>` 撞名；属性文本里的 `@bind-Value` 要转义。
- [x] 门禁与断言跟进：`tests/sample-docs.test.mjs` 新增「卡片化页面至少两段 demo、锚点唯一、每段都有标题与代码」与「每段代码都声明了、且片段用到了本页记录过的组件」（多组件页面按生成物与额外行的类型前缀判定，`Radio` 与 `RadioGroup` 同页不误报）；浏览器检查新增「组件页按 demo 卡片组织」「带何时使用、相关组件与生成式 API 表」，并对表单组的四个页面做跨页抽查，代码片段断言从单一 id 改为按 `-code` 后缀匹配。拖放断言改为限定到具体卡片，并先等 JS 模块接上监听（一页多个实例时的时序）。
- [x] 顺手修正示例工程的两处编译坑：属性文本里的 `@bind-Value` 需要转义；示例导入了库的 `List`，与 `System.Collections.Generic.List<T>` 撞名需写全限定。

## 示例工程 API 表改为源码生成（批次 2，版本号未递增）

- [x] 新增 `scripts/generate-api-reference.mjs`：解析组件源码的 `[Parameter]` 声明与 XML 文档注释，产出 `ShowcaseApiReference.generated.cs`（53 页 / 644 行，零空说明）。生成物提交进仓库，CI 新增 `--check` 步骤拒绝过期产物——改参数忘记重生成会失败，而不是悄悄漂移。
- [x] 生成器的确定性被 CI 抓出两处并修掉：一是排序用了 `localeCompare(..., 'en')`，ICU 规则随 Node 构建变化；二是同名参数（`Tab.Value` 与 `Tabs.Value` 类型不同）的消解依赖文件系统列举顺序。前者改为代码点排序，后者改为同名跨类型时双方保留并加类型前缀（26 组），并把「解析顺序由源码决定」写进注释。`--check` 同时改为打印逐行差异与行尾差异——起初它只报一句“已过期”，诊断成本全落在人身上。
- [x] 编辑内容与生成物分离：`scripts/api-descriptions.json`（548 条中文说明，从原来的手写表一次性抽出，不丢内容）、`scripts/api-extra-rows.json`（48 行生成器推不出来的行：模型属性、服务方法、`Value / StartDate / EndDate` 这类汇总写法）、`scripts/api-since.json`（「自哪个版本」，只登记 release-history 明确记录过的新增成员，首批为 Upload 的 18 个成员）。`DateRangePicker` 的 `StartDateChanged` 等 4 条说明一并补齐。
- [x] 53 个页面迁移：删除手写 `ShowcaseParameter` / `ShowcaseEvent` / `ShowcaseContract` 表与对应参数，API 段改由生成物渲染，新增「自 x.y.z」列；基类参数由渲染端并入，不随页面重复生成；没有组件的页面（安装、设计基础、实验室）不再输出空的「参数」段。唯一保留手写表的是 `Feedback`（记录服务与选项类型），已在门禁里显式列出。
- [x] `ComponentPreview` 在页面未提供手写表时读取生成物，按 `属性 / 事件回调 / 接口 / 服务` 分组渲染；字段缺失时按空处理，避免手写编辑文件的形状问题变成崩溃。
- [x] 门禁改写：`tests/sample-docs.test.mjs` 第二项由「页面表覆盖组件参数」改为「每个有组件的条目都出现在生成物里」+「页面不再手写表」，覆盖范围与例外都在门禁里显式声明。
- [x] 文档同步：`AGENTS.md` 的必读路由新增「示例工程内容、API 表或生成脚本」一行，门禁清单新增 `--check`；`component-plan` §4 按市面组件文档（三条栏 + 五段式 + 生成式 API 表）重写目标结构与批次。

## 示例工程内容与信息架构（批次 1，版本号未递增）

- [x] 按标准组件文档项目收敛示例页结构：每页固定为「01 外观预览 / 02 调整属性 / 03 示例代码 / 04 参数、事件与接口 / 05 说明与边界」，段号按实际存在的段计算，不再出现跳号；说明区由 API 段内的提示盒改为独立段。
- [x] 文档壳补齐：侧栏检索与匹配计数（`role="status"`）、每页页头的「在 GitHub 编辑」（按目录条目的页面文件名拼装）、页脚的上一页/下一页导航；`ShowcaseNavItem` 新增 `Page`，把 `glass` → `GlassLab.razor` 这个例外从推导逻辑里显式命名出来。
- [x] 示例代码段：`ComponentPreview` 新增 `Code` 参数，渲染等宽代码块与复制按钮；复制走应用级事件委托脚本 `wwwroot/js/sample-docs.js`，`navigator.clipboard` 不可用时退回 `execCommand('copy')`，两条路径都失败时按钮显示「复制失败」而不是谎报成功。
- [x] 参数表对账：补齐 19 行缺失登记（`Autocomplete` 2、`Combobox` 2、`Textarea` 3、`TimePicker` 1、`Upload` 1、`VirtualList` 9），并确认 `DatePicker` 页用 `Value / StartDate / EndDate`、`*Changed`、`*Expression` 这类汇总行覆盖 `DateRangePicker` 的绑定三元组是刻意写法。
- [x] 新增内容门禁 `tests/sample-docs.test.mjs`（3 项）：目录条目 ↔ 页面文件与路由一致；页面参数/事件表覆盖组件全部公共 `[Parameter]`（排除非稳定内部部件 `DateCalendar`/`TimeOptionList`，支持汇总行通配）；提供代码块的页面必须定义 `UsageCode` 且片段用到该组件。已反向验证：删掉 `Upload` 的一行登记即失败并点名。
- [x] 浏览器检查新增「文档壳」5 项：检索与计数、编辑链接指向本页文件且 `target="_blank"`、前后邻居（`upload` 的前后为 `multi-select`/`combobox`）、代码块渲染、复制按钮点击后回报结果；另加侧栏检索过滤断言。
- [x] 试点页：`Input`（补 `Notes` 与用法片段）、`MultiSelect`（补 3 行登记与用法片段）、`Upload`（补 1 行登记与用法片段）。其余页面的代码块与缺失段落登记为批次 2。

## 10.32.0 Upload 组件（优先级 9 第 1 项）

- [x] 新增公共组件 `Upload`（`Components/Upload/`，razor / cs / css / js 四文件）：原生选择器加拖放的文件收集，组件负责选择、可按元数据判定的校验、文件列表与状态渲染；传输、协议与存储归宿主。
- [x] 新增公共模型与枚举：`AeterniUI.Models.UploadFile`（`Content`、`Id`、`Name`、`Size`、`ContentType`、`Status`、`Percent`、`ErrorMessage`，不支持库外构造）与 `AeterniUI.Enums.UploadStatus`（Pending / Uploading / Success / Error）。宿主在收到的实例上直接改状态即可，重渲染不替换对象。
- [x] 新增 10 个公共文案表键：`UploadLabel`、`UploadFileListLabel`、`UploadDropText`、`UploadRemoveLabel`、`UploadStatusPendingLabel`、`UploadStatusUploadingLabel`、`UploadStatusSuccessLabel`、`UploadStatusErrorLabel`、`UploadFileTooLargeErrorFormat`、`UploadTooManyFilesErrorFormat`；已在交付文档 §37 的覆写示例中登记，`check-doc-drift` 的文案键覆盖随之通过。
- [x] 拖放实现为单模块 `Upload.razor.js`：把 `drop` 事件的 `FileList` 写进同一个原生输入再派发 `change`，C# 因此复用与选择器完全一致的路径；模块只负责拖放状态与点击时的输入清空，业务状态仍留在 C#；禁用时仍吞掉拖放，避免浏览器跳转到被拖入的文件。
- [x] 清空时机由 CI 的真实浏览器检查纠正：最初把清空放在 C# 处理完 change 之后，浏览器 job 复现了它与紧随其后的第二次拖放抢同一轮往返、把新选择抹掉的竞态；改为在点击选择器时（对话框打开前）清空，同一文件仍可重复选择，且不再有该竞态。这条断言随后按「行的内容」而不是「输入框的瞬时状态」重写，避免把测试绑在时序细节上。
- [x] 无障碍：`<input type="file">` 铺满拖放区承担点击与键盘激活，可见文案 `aria-hidden`；字段内输入采用 `FormField` 的输入 id（标签 `for` 解析到输入而非根元素），`aria-describedby` 合并宿主值与字段描述 id；`focus-visible` 焦点环画在拖放区上，`invalid` 使用无效态焦点色。
- [x] 交付物齐备：可交互示例页 `/components/upload`（含宿主侧模拟传输，展示 `Status` / `Percent` 回写）、交付文档 §49、组件目录新增 `ShowcaseCatalog` 条目、契约检查新增第 34 组、`tests/upload.test.mjs` 8 项模块断言、浏览器检查 6 项（真实 `DataTransfer` 拖放端到端）随 CI 的 browser job 验证。
- [x] 版本号 `10.31.1` → `10.32.0`（新增公共组件、模型与文案表键，按仓库约定递增第二位）；`Directory.Build.props` 四处齐动，`AGENTS.md` 版本头与项目索引、五份带版本头文档、README 发布示例同步。
- [x] 组件计划：优先级 9 排序中的第 1 项 `Upload` 已交付并从计划移除，排序表与现有能力对照同步；后台数据录入场景的下一候选为 `Cascader`。
- [x] 顺带收口计划文档漂移：`review-plan` 头部已声明第十轮收口、正文却仍留「待修复」表与 `批次 27 ⏳（待开始）`，`component-plan` 仍把优先级 10 登记为「未开始」；两文件统一为「收口即归档」，并在 `check-docs.sh` 增加两条状态一致性断言（反向注入残留时以退出码 1 拒绝）。
- [x] 发布：在 `main` 上打注解 tag `v10.32.0` 触发 `Publish NuGet packages`，Validate release tag → Release 构建 → Pack → NuGet Trusted Publishing（OIDC），`AeterniUI.10.32.0.nupkg` 与 `AeterniUI.Icons.FontAwesome.10.32.0.nupkg` 均被 nuget.org 接收（`Created`，非 `--skip-duplicate` 跳过），44s 全绿。
- 验证：例外：CI browser job 首次 58/59（清空竞态），修复后 59/59。

## 10.30.1 Descriptions 示例列数切换修复

- [x] 修复示例将 `Columns` 切换为 1 或 2 时仍向 `DescriptionItem` 传入 `Span=3` 的参数异常。
- [x] 示例根据当前列数收窄说明项跨度，保留组件对非法 `Span > Columns` 的严格参数校验。
- [x] 增加真实浏览器回归，验证 `Columns=1` 与 `Columns=2` 均可切换且页面没有异常或横向溢出。

## 10.31.1 示例缺口包（REV-159）与第十轮收口

- [x] REV-159 逐项补齐，示例页新增或修正的演示均有浏览器断言覆盖：
  - `Progress` 从静态两条变成可交互演示：`Value` 滑块、`Color` / `Size` 选择、`ShowValue` 与 `Indeterminate` 开关，另加「每个语意色一条」与「大号 + 不确定」两组状态对照。
  - `Radio` 参数表按归属重写：`RadioGroup.*` 与 `Radio.*` 分别点名，`AriaDescribedBy` 归回独立 `Radio`（此前被挂在小组名下）。
  - `IconButton` 补 `Size`（Small/Large）、`Disabled` 与 `Type=Submit`（放在真实 `<form>` 里，提交经 `@onsubmit:preventDefault` 拦截并回显）。
  - `MenuButton` 补受控 `Open`/`OpenChanged`、`Disabled`、`Loading` 三个变体并说明 `Loading` 只让主动作忙碌。
  - `Segmented` 补 `Required` + `Invalid` 与整组禁用两个状态。
  - `Tag` 补 `StartIcon` / `EndIcon`（含两端同时使用的组合）。
  - `Avatar` 补 `Src` + `Alt`（含 `Alt=""` 装饰用法与故意 404 的回退缩写）。
  - `DateRangePicker` 补宿主 `Presets`（近 7 天 / 本月 / 上季度）与 `PresetsAriaLabel` 演示。
  - `InputNumber` 补 EditContext 链路演示：`EditForm` + `DataAnnotationsValidator` + `FormField.For` + `@bind` 三件套（`Value`/`ValueChanged`/`ValueExpression`）+ `ValidationMessage`。
  - `Rating` 示例事件表在 10.31.0 批次 24 已把已移除的 `OnChange` 改为 `ValueChanged` 并注明移除。
- [x] 版本号 `10.31.0` → `10.31.1`（示例与演示口径调整，公共 API 无变化，按仓库约定递增第三位）；`AGENTS.md` 版本头与项目索引、五份带版本头文档、README 发布示例同步。
- [x] 浏览器检查新增 11 项「示例演示覆盖」断言（每个演示页面按选择器计数），并含「打开范围选择器后出现 ≥3 个预设」一项；这些断言随 10.31.1 的 browser job 一起验证。
- [x] 发布：注解 tag `v10.31.1` 触发 `Publish NuGet packages`，Validate release tag → Release 构建 → Pack → NuGet Trusted Publishing（OIDC）→ 发布 `AeterniUI` 与 `AeterniUI.Icons.FontAwesome`，40s 全绿。
- 至此第十轮 31 条全部收口（批次 23～27 分别归档在上方对应小节）。

## 10.31.0 第十轮审阅修复（REV-133～REV-157 与 REV-160～REV-162）

- [x] 版本号在 `Directory.Build.props` 单一来源递增 `10.30.1` → `10.31.0`（`Version` / `AssemblyVersion` / `FileVersion` / `InformationalVersion` 四处齐动）。
  - 按仓库约定「功能新增递增第二位」：本批新增公共文案表键 `DescriptionsEmptyValueLabel`、`ConfirmAcceptLabel`、`ConfirmCancelLabel`，并把 `AlertOptions.CloseText` / `ConfirmOptions.ConfirmText` / `CancelText` 改为可空 + 回退，属可观察的公共面变化；`AGENTS.md` 的文档版本头与项目索引、四份带版本头文档、README 的发布示例（`v10.31.0`）同步。
- [x] 修复批次：23（REV-133～139 P1：Radio 独立绑定、InputNumber 步进与夹取、MultiSelect 字段关联、Descriptions 文案、IconButton 加载墨、Badge RTL）、24（REV-140～143：Holo 混色定调、OnChange/CloseText 契约、断言可移植性）、25（REV-146～148、153～155：指针反馈、JS 元素引用、退场时序、JSException 降级、月份保持、描述 id 合并）、26（REV-144/145、149～152、156/157：文档与注释对账、宿主别名收敛、令牌纪律、死类死关键帧、校验收口、IconButton 文件职责）、27 卫生包（REV-160）与 REV-158/161/162 的文档与性能项。逐项证据见上方各批次小节。
- [x] 门禁本身的三处缺陷同步修复：`verify.mjs` 失败不再返回 0（此前 browser job 把失败当成功）、主题切换检查的就绪门槛（WASM 冷启动竞态）、`check-docs.sh` 纳入 `AGENTS.md` 版本头校验。
- [x] 契约门禁新增四个批次函数（`CheckReviewBatch23Async`～`CheckReviewBatch26Async`，共 60 余条断言，逐项做过反向验证）与四个 JS 模块单测（REV-163：Checkbox、DialogProvider、Menu、ThemeProvider）；浏览器检查新增 REV-140/146/148 五项。
- [x] 发布：在 `main` 上打注解 tag `v10.31.0` 触发 `Publish NuGet packages`，工作流先由「Validate release tag」比对 tag 与 `Directory.Build.props` 版本，再经 NuGet Trusted Publishing（OIDC）发布 `AeterniUI` 与 `AeterniUI.Icons.FontAwesome`；全流程绿（41s）。
- 已知边界：第十轮仅剩 REV-159（示例缺口包）；`DatePicker` 族的 RTL 方向键映射、`.aeterni-dark` 手写入口、`AlertOptions.CloseOnEscape` 的可达范围已按 REV-162 逐项登记在交付文档中。

## 第十轮审阅修复批次 27（进行中：REV-160 卫生包已收口，版本号未递增）

- [x] REV-160（可执行项全部落地）：`VirtualList` 删除恒等死分支（`_activeIndex < 0 && _rows.Count > 0` 置 -1）；7 处裸 `_ = InvokeAsync(StateHasChanged)` 统一走基类的 `RequestStateHasChanged()`（含禁用/销毁守卫）；`Pagination` 的 `prefers-reduced-motion` 块只保留 `transition: none`，不再重复基类的盒模型声明。
- [x] REV-160（复核后判定无需改动，逐条留证）：`Radio` 的禁用态两组选择器分别覆盖原生 `disabled` 与分组 `is-disabled`，两条规则各有消费者；`Theme` 示例没有死字段（`_feedbackNote` 属 Feedback 示例且被渲染）；`Pagination` 已无「缩放过渡」表述；`ComboBox._visibleItems` 是每次参数设置重建的索引副本，去掉会让每帧重算可见项，收益为负。`TimeOptionList` 传给 JS 的 `ActiveOptionId`／`shouldAlign` 虽在模块内未读取，但它们是模块的**位置参数约定**（浏览器回归按位置传参），删除属破坏性接口变更且不改变行为，故保留并在此记录原因。

## 第十轮审阅修复批次 26（P2，版本号未递增）

- [x] REV-144：文档漂移逐处对账——ButtonGroup 分隔线与 Button 加载的描述复查后与实现一致（透明变体为强调色 45% 染色、加载为内联 Spinner 替换首图标），残留的是 CSS 注释：`Button.razor.css` 的「Loading veil」块与 `ButtonGroup.razor.css` 的「strong border colour」按实现重写。
- [x] REV-145：组件样式不再消费宿主别名，13 个文件 31 处 `--aeterni-padding-*`／`gap-*` 换为等值 `--aeterni-spacing-*`，门禁断言组件 CSS 中三类别名清零。
- [x] REV-149：`Rating` 的无效态补上控件级视觉——未点亮星星改用 `--aeterni-color-danger-text`，`is-invalid` 不再是死类。
- [x] REV-150：`Tag` 的图标尺寸改由 `--aeterni-icon-render-size` 传给子组件 `Icon`（删除永不命中的 `.aeterni-tag__icon > svg`），默认变体配方并入基础规则、不再输出 `aeterni-tag--default`；Soft／Outline 修饰类保持不变。
- [x] REV-151：同角色令牌旁路收敛——Timeline 滚动条改用 `--aeterni-scrollbar-thumb/track`，Segmented 无效边框改走 `--aeterni-state-border-invalid`，日期与时间字段统一到 `--aeterni-control-foreground-placeholder`。
- [x] REV-152：死类与死关键帧清理——MenuButton 的 `is-open`／`is-disabled`、SplitButton／ButtonGroup／IconButton 的 `is-disabled`、RadioGroup 恒输出的 `--horizontal` 修饰类（`aria-orientation` 仍显式输出）以及无引用的 `aeterni-dialog-slide-in` 关键帧删除；`aeterni-{component}__host` 作为宿主样式钩子写入文档（库内不消费，改名视为破坏性变更）。
- [x] REV-156：`Menu`／`Segmented` 的 `Items` 补 `ArgumentNullException.ThrowIfNull`；`ConfirmOptions.ConfirmText`／`CancelText` 改为可空并回退新增的 `AeterniUITextOptions.ConfirmAcceptLabel`／`ConfirmCancelLabel`，与 `AlertOptions.CloseText` 同一约定。
- [x] REV-157：`IconButton` 的 `BuildClass`／`BuildAttributes`／点击处理迁入 `IconButton.razor.cs`；玻璃接管名单复查后交付文档与工程参考均为四角色（`--aeterni-state-color-muted` 早已退出），无需再改。
- [x] 契约门禁新增 `CheckReviewBatch26Async`：宿主别名清零、Rating 无效规则、Tag 图标机制与变体类、三处同角色令牌、死类与死关键帧、PopupHost 钩子成文、`Menu`／`Segmented` 的 null 校验、确认按钮文案回退与显式优先、IconButton 文件职责、玻璃名单为四角色。断言在修复前实测为红（别名残留、DateTimePicker placeholder、RadioGroup `--horizontal`、slide-in 关键帧四项），修复后转绿。

## 第十轮审阅修复批次 25（P2，版本号未递增）

- [x] REV-146：指针反馈补齐两处——`ComboBox` 已选中项悬浮整块深一档（`--aeterni-state-background-active`，与键盘游标同档），`Search`／`Autocomplete`／`InputNumber` 的连体分组补上悬浮边框（排除聚焦、无效、禁用、只读），与独立 `Input`、`ComboBox`、日期触发器一致。
- [x] REV-147：`Toolbar`、`ToggleGroup`、`Menu` 改为把**自己渲染的节点**交给 JS 模块（新增内部 `RenderedRootElement`，并在 `@ref` 上转交 `RootElement`）。此前传 `RootElement`：宿主一旦传入基类 `Element` 参数，`RootElement` 变成外部元素，模块所有权校验失败，方向键与单一 Tab 停留点静默失效。
- [x] REV-148：Dialog/Toast 退场改走实测动画结束信号——服务在条目标记关闭后等待 Provider 回报的动画完成（`waitForExitSignals`，复用 `aeterni_floating.js` 的 `waitForExit`），仅在没有 Provider 时退回到 1 秒上限；原固定 180/220ms 会在 300ms 动画的 60%/73% 处摘掉节点，reduced-motion 下原地冻结。通知条目现在也带稳定 `Id`。
- [x] REV-153：`JSException` 在 `JsModuleManager` 内统一按可恢复失败收编（两个调用路径），JS 侧运行时错误不再以未处理渲染异常终结页面；`Checkbox` 的本地兜底 catch 随之删除。
- [x] REV-154：日期族的可见月份只在绑定值真正变化时重置（`DatePicker`／`DateRangePicker`／`DateTimePicker`），父级重渲染不再把用户翻到的月份拉回。
- [x] REV-155：`MultiSelect` 触发器的 `aria-describedby` 改为合并宿主值与 `FormField` 描述 id，字段不再顶掉宿主值。
- [x] 契约门禁新增 `CheckReviewBatch25Async`（混色/边框 CSS、元素引用来源、JS 失败降级计时、月份保持、描述 id 合并），并逐项做反向验证：回退修复即红（含 `DatePicker` 回退后月份被拉回 2026-01-01 的实测读数）。浏览器检查新增三项：`Search` 分组悬浮边框（真实指针 + `:hover` 命中）、Toast 退场 ≥250ms（实测约 300ms）、reduced-motion 退场 ≤250ms。`ComboBox` 选中项悬浮由契约层的 CSS 断言覆盖：弹层由 `PopupHost` 放置，坐标探针在该环境里无法稳定命中选项（`elementFromPoint` 落到页面容器），而 CDP `CSS.forcePseudoState` 在此无效果，因此不把不可靠的坐标断言留在门禁里。

## 仓库维护记录：发布 tag 与提交署名清理（版本号未递增）

- 全部 27 个 `v10.x` 注解 tag 已删除（本地与远程），仓库内不再有 tag；版本溯源以本文档为准。GitHub 上不存在 Release 条目，因此没有遗留的悬空发布页。
- `main` 的提交不使用工具署名或共创者 trailer；随 tag 历史移除的旧提交仍在本地对象库中，已通过 reflog 过期 + `git gc --prune=now` 清理（悬空提交 134 → 0），本地已无任何带共创行的提交。
- 27 个 PR 的正文都不含共创行；其中 26 个已合并、#26 为已关闭未合并。PR 在 GitHub 上不可删除，`refs/pull/*/head` 亦拒绝改写（实测 `deny updating a hidden ref`），因此 18 个历史 PR 的 head 提交仍保留旧的工具署名行——这属平台保留内容（连同其不可达对象），仓库侧无法清理。

## 第十轮审阅修复批次 24（P1，版本号未递增）

- [x] REV-140：`FlashCard` Holo 拆成两层——彩虹走 `color`（替换色相与饱和度、保留图片明度），白光反射另起 `.aeterni-flash-card__sheen-light` 走 `screen`（径向高光、金属拉丝、整面扫光）。四处同向：实现、`engineering-reference` 的玻璃/光泽硬规则、`delivered-features` §45、示例说明。新增真实浏览器检查：断言两层 `mix-blend-mode` 与层面覆盖，并用「浅色 #DCDCDC / 深色 #1F1F1F 平铺底 + 截图取像素」验证两种底色下光泽都真实改变画面（差分 ≥ 6/255）。
- [x] REV-141：`Rating` / `ComboBox` 的 `OnChange` 文档与示例收口——正文早已改记「已于 10.14.3 移除」，本次把示例事件表里仍把它列为可用回调的两行改成 `ValueChanged` / `OnItemSelected` 并注明移除；`check-doc-drift` 的 §17/§18 锁定继续生效。
- [x] REV-142：`AlertOptions.CloseText` 从死参数改为通知关闭按钮的无障碍名称；留空（默认 `null`）回退 `AeterniUITextOptions.AlertCloseLabel`，宿主显式设置时优先。`DialogProvider` 的 Alert 页脚分支改用同一回退，避免空标签；同步 §26 与 Feedback 示例说明。
- [x] REV-143：`ContractChecks` 的 Timeline 可视时间断言先对期望值做 `HtmlEncoder.Default.Encode`——ICU 72+ 的 U+202F 会被渲染成 `&#x202F;`，未编码的期望值只在旧 ICU 的 CI 上侥幸通过。同批的 Alert 断言也遵循同一规则。
- [x] 门禁收尾：`CloseText` 不再是退役成员，`scripts/check-doc-drift.mjs` 删除该条目（现为 66 个文案键、1 条退役成员、0 条待修复）。
- [x] 浏览器门禁顺带修两处：① `verify.mjs` 在打印 `RESULT: FAIL` 后仍以 0 退出，`run.sh` 与 CI 的 browser job 因此把失败当成功——现在失败会置非零退出码，browser job 才真正成为门禁；② REV-121 的主题切换检查存在冷启动竞态（`waitForApp` 只证明标记渲染完成，首次点击可能早于 WASM 交互运行时挂载，被静默丢弃）：先等待 `ThemeProvider` 通过交互运行时写出的 `<html data-theme>` 作为就绪门槛（最多 60 秒），再以「观察到过渡类之前重试同一选项」兜底（未处理的点击不会改变主题，因此重试安全）；失败信息里保留环境诊断，便于后续定位。

## Agent 指南审计、图标门禁接入与图标脚本大小写修复（版本号未递增）

- [x] 审计 `AGENTS.md`：门禁与索引事实逐条核对通过；补齐 §2 必读路由（图标清单/生成脚本、CI 与工作流）、§3.3 根目录树（`LICENSE`、`.github/workflows` 点名）与组件目录模板的聚合目录例外（`Dialog`／`Popup`／`Theme`）、§4.8 门禁的 token 选择器检查命令。
- [x] 统一文档版本头：`AGENTS.md` 增加「文档版本」头并纳入 `scripts/check-docs.sh` 校验，版本纪律措辞同步。
- [x] CI 接入 `node scripts/generate-fontawesome-icons.mjs --check`：此前 `AGENTS.md` 已把它列为门禁，但没有任何工作流执行；新增 Node 22 与 `/tmp/aeterni-fontawesome-*` 包缓存。
- [x] 修复图标生成脚本的模块名大小写缺陷：原实现拼出 `faalignCenter.js`，而 Font Awesome 包内是 `faAlignCenter.js`；生成时所在的大小写不敏感文件系统掩盖了差异，Linux 上 343 个图标在门禁里全部解析失败。改用既有的 `toPascalCase` 拼接，缓存路径改为脚本实际使用的 `/tmp`。
- [x] 第十轮批次 23～27 的执行计划评审写入审阅计划（前置决策 D1～D3、逐批改动面与验收判据、每批必过门禁、版本定档口径）；D1（独立 `Radio` 值绑定）已定调并随批次 23 落地。该执行计划评审随第十轮收口从审阅计划移除，D1～D3 的结论以本文档对应批次记录为准。

## 第十轮审阅修复批次 23（P1，版本号未递增）

- [x] REV-133：独立 `Radio` 的选中态改由绑定的 `Value` 直接驱动（`bool` 取真值、字符串按非空、其他类型取非默认值），分组模式继续比较分组值；示例页的独立用例改为真实绑定并显示其值。
- [x] REV-134 / REV-135：`InputNumber` 步进改走 `NumericValue.SaturatingAdd` / `SaturatingMultiply`，无界字段在值类型极限处饱和（`int` 不再回绕、`decimal` 不再抛 `OverflowException`）；`ClampAndRound` 改为先按精度取整再夹取，`Max` 不齐精度网格时不再提交越界值，增减禁用判断随之恢复一致。
- [x] REV-136：`MultiSelect` 触发器采用 `FormField` 的输入 ID，字段标签的 `for` 重新解析到触发器本身。
- [x] REV-137：`Descriptions` 空值占位的无障碍名称改由 `AeterniUITextOptions.DescriptionsEmptyValueLabel` 提供，去掉全库唯一硬编码 `aria-label`。
- [x] REV-138：`IconButton` 的禁用规则排除加载态并新增 `--aeterni-icon-button-loading-foreground` 角色，忙碌字形保留变体墨色。
- [x] REV-139：`Badge` 补 `:dir(rtl)` 反向位移，RTL 下角标回到镜像角外侧；同步该处 CSS 注释与交付文档 §31 措辞。
- [x] 契约门禁新增 `CheckReviewBatch23Async`（Radio 独立/分组、多选触发器 `for`、Descriptions 文案表覆写、IconButton 加载角色、无界步进的饱和与越界夹取），并逐项做反向验证：回退修复即红，其中 `InputNumber` 的旧路径直接抛 `OverflowException`。

## 10.30.0 Descriptions 与 Timeline 溢出修复

- [x] 修复 Timeline 横向模式在受限父容器中因自身固有宽度导致的页面级横向溢出；组件继续保留内部横向滚动。
- [x] 新增纯展示 `Descriptions` 与 `DescriptionItem`，使用原生 `<dl>` / `<dt>` / `<dd>` 语义，支持横向/纵向布局、1～4 列、项目跨列、可见性、边框和自定义值模板。
- [x] 增加 `/components/descriptions` 可交互示例，覆盖布局方向、列数、边框和 `ItemTemplate`。
- [x] 增加 Descriptions 渲染契约、模板/跨列/参数校验断言，并同步当前功能、项目索引、设计规范、审阅文档和发布元数据。

## 10.29.2 VirtualList 选择滚动与状态间隔修复

- [x] 点击已可见行只更新活动项与选择值，不再发起索引滚动，避免选中行看起来被移动到视口第一行。
- [x] 键盘导航保留自动显露目标项，但 `scrollToIndex` 改为只在目标越界时对齐最近边缘；Home / End 与跨视口移动不再无条件顶对齐。
- [x] 选中、悬浮和键盘活动背景改由行内装饰表面绘制，上下各内收半个 `spacing-1`，相邻背景间隔实测与 List 的 4px `row-gap` 一致。
- [x] 固定行盒、点击热区、Blazor `Virtualize` spacer 与 `ItemSize` 保持同高，不向虚拟化步距直接添加 CSS gap；补充最近边缘滚动单元回归和真实浏览器点击位置/状态表面几何断言。

## 10.29.1 Stepper / VirtualList 布局修复

- [x] Stepper 纵向 `ItemTemplate` 槽占满按钮可用宽度；示例模板按方向切换为固定标记列与自适应文字列，长文案不再把图标推离连接轴。
- [x] 自定义模板标记继续用独立居中盒约束图标基线，并新增真实浏览器几何断言覆盖图标、标记与连接线中心重合。
- [x] VirtualList 可见行补齐 ListItem 已有的 `spacing-2` 内容间距、透明默认底、起始文字对齐和选中态文字色，同时保持固定 `ItemSize`、虚拟化与键盘模型不变。
- [x] 新增真实浏览器计算样式对照，直接比较 VirtualList 行与 ListItem 的 gap、padding、圆角及文字对齐。

## 10.29.0 Timeline 布局扩展与 Stepper 对齐

- [x] `Timeline.Orientation` 支持 `Vertical`（默认）与 `Horizontal`；横向事件保持可读最小宽度，空间不足时在组件内部滚动，不撑宽页面。
- [x] Timeline 图标增加独立的双轴居中槽，隔离 `Icon` 的行内基线偏移；圆点、图标和连接线在两个方向上使用同一标记中心。
- [x] 首页删除旧的 `doc-timeline` DOM/CSS 与提交时间线说明文案，改用 `Timeline` + `ItemTemplate`；桌面端提交列表占用首屏剩余高度并内部滚动，窄屏不溢出视口。
- [x] Stepper 默认内容重构为与 Timeline 一致的标记/正文网格和连接线几何，横纵向均保留原有受控值、按钮、焦点、模板与 ARIA 契约。
- [x] 示例、当前功能文档、项目索引、渲染契约及真实浏览器布局检查同步更新；未新增 JS 或全局 Token。

## 10.28.0 Timeline（优先级 8，1/2）

- [x] 新增纯展示 `Timeline` 与 `TimelineItem`：宿主按展示顺序提供事件，组件不排序、不推断状态；支持标题、说明、`DateTimeOffset`、可见时间文案、装饰图标、语义色与可见性。
- [x] 根元素使用原生 `<ol>`，事件使用 `<li>`；有时间戳时输出机器可读 `<time datetime>`，标记与连接线保持装饰语义，无按钮、额外 role 或 Tab 停留点。
- [x] `ItemTemplate` 只替换标题与说明区域，时间、标记和纵向节奏仍由组件维护；隐藏项不参与 DOM 与连接线。
- [x] 新增 `/components/timeline` 可交互示例、目录入口、第 32 组渲染契约、当前功能文档与项目索引；组件只消费既有 Token，不引入 JS 或新全局 Token。

## 10.27.0 Slider 无级调节

- [x] 移除 `ShowTicks` 公共参数、刻度 DOM、刻度样式及相关渲染契约，轨道只保留填充与滑块。
- [x] `Step=null` 改为无级模式：指针不吸附；显式正数继续让指针和键盘吸附到以 `Min` 为起点的网格。
- [x] 无级模式保留完整键盘模型：方向键移动量程的 1%，PageUp/PageDown 移动 10%，Home/End 仍到边界；整数类型受自身可表示精度限制。
- [x] 示例、当前功能文档、项目索引与 Slider 的 C#/JS 契约测试同步更新。

## 10.26.0 表单族数值输入（优先级 7）

- [x] 新增 `InputNumber<TValue>`：受控 `TValue?` 绑定、`Min`/`Max`/`Step`/`Precision`、方向键与 PageUp/PageDown 步进、默认关闭的 `ShowControls` 连体增减按钮、`FormField` 状态继承与 `EditContext` 校验；构建在 `Input` 与 `Button` 之上，内部输入输出 `role="spinbutton"` 与不变的 `aria-value*` 字符串（不使用原生 `type="number"`）。
- [x] 新增 `Slider<TValue>`：单值、横向与竖向两向布局、Home/End/PageUp/PageDown 与方向键（RTL 自动互换、竖向忽略左右）、指针拖拽与步进吸附、`role="slider"` 与 `aria-valuenow/min/max/valuetext`。
- [x] 两者共用 `Components/NumericValue.cs`（文化解析、定点格式化、小数位推导、按类型取整与步进吸附）与新增核心字形 `Plus`/`Minus`；文案表新增 `InputNumberIncrementLabel`、`InputNumberDecrementLabel` 和 `SliderLabel`。
- [x] 按键默认行为收口：两个组件各新增一个只做 keydown 拦截的模块（`Rating.razor.js` 先例），PageUp/PageDown 在步进时不再同时滚动页面；C# 与 JS 的按键集合由契约检查交叉锁定。边界随之修正：`InputNumber`「不需要 JS」改为「唯一 JS 是键位默认行为拦截」。
- [x] **命名事实与决定**：.NET 8+ 的 `Microsoft.AspNetCore.Components.Forms` 自带同名 `InputNumber<TValue>`，实测无论 `@using` 顺序（页面级、`_Imports` 前后）`<InputNumber>` 都解析到框架类型，只有完全限定标签能命中本组件。决定保留路线图原名，示例与文档一律使用完全限定标签，限制写进当时的功能清单实现边界。
- [x] 示例新增 `/components/input-number` 与 `/components/slider` 并注册导航；`tests/input-number.test.mjs`、`tests/slider.test.mjs` 与第 30、31 组渲染契约覆盖按键拦截、指针吸附、RTL/竖向几何、边界校验与资源释放。
- 本批由路线图优先级 7 直接转化，两者都只消费现有 Token，未新增全局 Token。

## List 长列表尺寸修复

- [x] `ListItem` 保持固有行高，不再因父级 flex 尺寸协商被压缩。
- [x] `List.MaxHeight` 提供可选的内部滚动视口；设置后滚动条归 List 所有，默认短列表仍保持内容自适应。
- [x] 同步 List 示例与当前功能文档；解决方案构建、契约检查和文档门禁通过。

## VirtualList<TItem> 实现

- [x] 新增 `VirtualList<TItem>`，面向大数据量场景按可视区域和 overscan 渲染，不改变现有 `List<TItem>` 的静态/交互契约。
- [x] 明确固定行高、`Height`、`ItemSize`、键盘活动项、选中状态、动态重排及 `aria-setsize` / `aria-posinset` 语义。
- [x] 基于 Blazor `Virtualize<TItem>` 实现，远程数据、可变高度和缓存仍留在后续规划。

## 10.25.0 VirtualList 触底加载

- [x] `VirtualList<TItem>` 增加 `HasMoreItems`、`LoadingMore`、`LoadMoreThreshold` 与 `OnLoadMore`，由内部滚动视口在接近底部时通知宿主追加下一批 `Items`。
- [x] 触底通知由组件负责并发抑制、禁用/不可见/无更多数据跳过和 `aria-busy` 状态；数据请求、分页、缓存和是否还有更多项目仍由宿主控制。
- [x] 示例改为分批追加数据并展示触底加载；同步当前功能文档与渲染契约，保留固定行高、键盘导航、选中状态和 `aria-posinset` / `aria-setsize` 语义。
- [x] `dotnet build aeterni_ui.slnx`、`AeterniUI.ContractChecks` 与文档门禁通过；`.razor.js` 语法检查需在具备 Node.js 的环境执行。

## 10.25.1 Stepper 状态与无障碍收口

- [x] 统一状态优先级为“禁用 > 当前 > 完成 > 未完成”：禁用步骤覆盖当前/完成的文字、标记和连接线视觉，已完成的当前步骤仍正确填充通往下一步的连接线。
- [x] 默认与模板模式下每个步骤按钮都使用 `Label` 作为稳定 `aria-label`；默认描述仅通过 `aria-describedby` 提供，避免描述同时进入按钮名称造成重复播报。
- [x] 扩展 Stepper 渲染契约，覆盖默认步骤名称、当前 + 完成 + 禁用组合；构建、契约、CSS 注释和文档门禁通过。

## 10.22.3 Search / Autocomplete 连体按钮组

- [x] Search 的清除/搜索按钮和 Autocomplete 的清除按钮统一使用 `ButtonGroup Connected`，与输入框共用边界，不再显示间隙或重复圆角。
- [x] ButtonGroup 支持 `IconButton` 子项并继承禁用状态，保留相邻按钮分隔线、焦点层级和 RTL 逻辑边角。
- [x] 操作按钮随 Search / Autocomplete 的 `Size` 同步高度；`Loading` 暂时禁用整个组合。
- [x] 示例、功能文档和渲染契约同步；构建与 21 组契约检查通过。

## 10.22.2 Search 内置搜索按钮

- [x] 将 Search 的文字提交按钮收进输入框，改为始终可见的紧凑搜索图标按钮；Enter 与点击继续共用 `OnSearch`。
- [x] 新增核心 `AeterniIcons.MagnifyingGlass` 字形；`SubmitLabel` 继续提供可本地化无障碍名称，Loading 在按钮内显示 Spinner。
- [x] `Clearable` 只控制清除动作，关闭后仍保留内置搜索按钮；同步示例、功能清单和渲染契约。

## 10.22.1 Search / Autocomplete 清除操作优化

- [x] 清除按钮统一放入输入框内部，不再作为输入框外的并列操作占位。
- [x] `Clearable` 统一控制自定义清除按钮，并隐藏 `type="search"` 的浏览器原生清除入口，避免重复图标。
- [x] 清除图标使用紧凑尺寸并保留独立焦点与无障碍名称；构建、21 组渲染契约和文档检查通过。

## 10.20.2 FlashCardGroup 牌堆布局修复

- [x] 修复 FlashCardGroup 隔离 CSS 对子组件根元素的作用域匹配，牌堆卡片现在能正确显示宽度、叠放和扇形展开布局。
- [x] 优化牌堆交互：收拢状态增加轻微错位与旋转，展开后悬浮或聚焦的卡片抬升到最上层并完整显示卡面。
- [x] 用无头浏览器验证 `/components/flash-card-group` 实际预览已显示卡片；构建、契约检查和 CSS/文档门禁通过。
- [x] 已完成最终差异审查、Release 构建、双包打包和 19 组契约检查；`v10.20.2` 已推送，NuGet 发布工作流成功完成，包含 10.20.0 与 10.20.1 的累计变更。

## 10.20.1 FlashCardGroup 示例补充

- [x] 新增 `/components/flash-card-group` 独立示例页，并在 ShowcaseCatalog 注册导航入口。示例包含悬浮/聚焦展开、保持展开、禁用、卡片宽度/间距调整和“抽一张卡”宿主交互。
- [x] 修正 FlashCard 示例页职责：只展示 FlashCard，自成页面的牌堆示例移至 FlashCardGroup 页面。
- [x] 通过构建与文档一致性检查；版本属性更新为 10.20.1。
- [x] 已并入 `v10.20.2`，不单独创建 `v10.20.1` tag。

## 10.20.0 FlashCard 光泽优化与 FlashCardGroup

- [x] 保留 `FlashCard` 的 Click/Hover 翻面 API 与 3D 跟随契约；翻面改用独立强调缓动，光泽增加随指针相位变化的高光与扫光层，扫光位移与旋转使用同一套 JS 指针变量。
- [x] 新增 `FlashCardGroup`：多张 FlashCard 默认折叠叠放，悬浮或 `focus-within` 扇形展开；支持 `Expanded`、`HoverExpand`、`CardWidth`、`ExpandedSpacing`、`CollapsedOffset` 与根级无障碍/禁用语义。
- [x] `/components/flash-card` 增加抽卡牌堆交互示例；契约检查扩展为第 19 组，保留 `tests/flash-card.test.mjs` 指针跟随与资源释放回归。
- [x] `aeterni_ui.slnx` 构建、`AeterniUI.ContractChecks`、`node --check`、`node --test tests/*.test.mjs`、`check-css-comments.mjs`、`check-contrast.mjs` 与 `check-docs.sh` 通过；版本属性统一为 10.20.0。
- [x] 已并入 `v10.20.2`，不单独创建 `v10.20.0` tag。

## 10.18.0 Stepper 模板与布局扩展

- [x] 新增 `StepperItem.Icon`：单个步骤可指定标记图标，完成步骤仍显示勾选字形；未指定时回落到序号。
- [x] 新增 `Stepper.ItemTemplate`（`RenderFragment<StepperItem>`）：每步唯一的内容区，接管按钮内全部内容（含图标），可按步骤渲染不同内容；模板模式下 `Label` 继续作为按钮的无障碍名称，`Description` 不再关联 `aria-describedby`。
- [x] 横向布局改为标记在上、标题与描述在下方居中，连线沿标记行贯通且不再穿过文字；纵向布局保持标记在左、文字在右，两种情况共用同一套连线端点（两端各留一个 `--aeterni-spacing-1`）。
- [x] `aeterni_ui.slnx` 构建、`AeterniUI.ContractChecks`（新增单步图标与模板渲染断言）、`check-css-comments.mjs`、`check-contrast.mjs` 与 `check-docs.sh` 均通过；版本属性统一为 10.18.0，同步当时的相关文档与 README 发布 tag 示例。
- [x] 已发布：通过最终差异审查后推送 `v10.18.0`。

## 10.17.3 Stepper 修复发布

- [x] 修复 Stepper `Items` null 参数异常、补齐参数与 ARIA 渲染契约，并使示例 Previous/Next 跳过隐藏和禁用步骤。
- [x] 收窄 Stepper hover 反馈，仅对可激活的已完成步骤提供 hover/active 状态；同步当前功能、设计规范、审阅待办、项目索引与 README 版本示例。
- [x] 已发布：通过构建、ContractChecks、文档一致性和最终差异审查后推送 `v10.17.3`。

## 10.17.2 补发完整发布

- [x] `10.17.1` 的发布 tag 曾打在侧分支 `fd9c606` 上，其基线为 #17 之后的代码，未包含 #18 的 Breadcrumb / Stepper 修复；现从 main 补发 10.17.2，使 NuGet 上的最新版本与 main 一致。
- [x] 版本属性统一为 10.17.2；同步当前功能、设计规范、审阅待办、项目索引与 README 发布 tag 示例，并通过构建、文档、CSS 注释与品牌对比度检查。

## 10.17.1 组件审查收口

- [x] 修复 `Breadcrumb.IsCurrent` 在渲染循环中重复展开可见项列表的问题，渲染改用已缓存的可见项计数。
- [x] 补齐 `BreadcrumbItem.Target`，并在取 `_blank` 时自动输出 `rel="noopener noreferrer"`，对齐 `Menu` 的既有约定。
- [x] 移除无角色 `<span>` 上无效的 `aria-disabled`：被禁用层级保留链接语义、标记 `aria-disabled="true"`、移出 Tab 序列，并由样式阻断指针导航。
- [x] 移除 `Stepper` 当前步骤的 hover/active 换档与 `pointer` 光标，避免为不可重新激活的步骤提供可点击反馈；步骤描述文字色从 `--aeterni-text-tertiary` 改为 `--aeterni-text-secondary`。
- [x] 删除原项目索引中重复的 Breadcrumb 模块章节，并把误置于其中的 `tests/toolbar.test.mjs` 归回 Toolbar 模块；扩展渲染契约覆盖 `Target`/`rel`、禁用链接的 Tab 序列位置与 `aria-disabled` 唯一性。
- [x] 修复 `List` 多选与 `AllowClear` 下取消选中后遗留底色：点击在切换选中之前先把目标设为活动项，而活动项不随指针移开清除，于是 `.is-active` 的背景留在了已取消选中的行上；现把该提示限定为列表持有键盘焦点时生效，未选中与取消选中的行不再有底色。
- [x] 交付后 P0 收口：Breadcrumb 现在拒绝 null `Items`、null item 和空白 `Label`；禁用项、自定义分隔符与 `aria-hidden` 已纳入契约检查；Stepper 的 `ValueChanged` → `OnStepClick` 顺序、受控值和禁用分支已纳入检查，示例同步展示事件序列。
- [x] 版本属性统一为 10.17.1；同步当前功能、设计规范、审阅待办、项目索引与 README 发布 tag 示例，并通过构建、文档、CSS 注释与品牌对比度检查。
- [x] 契约收口：Stepper 保留每个步骤一个原生按钮 Tab 停留点，不引入 Toolbar 式 roving-focus；命名 `div role="group"` 包裹原生 `ol/li`，避免把流程状态误标成导航或复合选择控件。
- [x] 位置语义收口：`aria-posinset` / `aria-setsize` 继续挂在原生 `li` 列表项上，与可见步骤序列一致；渲染契约覆盖横向/纵向根类、位置计数和隐藏项过滤。
- [x] 补齐 Stepper `Items`、null item、空白 `Id`/`Label` 和重复 Id 的参数契约，并增加 `aria-describedby`、根级/项级禁用语义的渲染检查；示例 Previous/Next 跳过隐藏和禁用步骤。
- [x] 补齐 Breadcrumb / Stepper 横向布局契约：Breadcrumb 保留可换行的原生有序路径，Stepper 保留等分横向步骤和可见位置计数；契约检查覆盖两者的有序结构。
- 本批只收口审查确认的缺陷，除 `BreadcrumbItem.Target` 外不新增公共参数、事件或枚举；Stepper 的原生按钮键盘模型已明确为稳定契约。

## 10.16.0 Stepper 功能发布

- [x] 新增 `Stepper` / `StepperItem`，交付受控当前步骤、宿主显式完成/禁用状态、横纵向布局和位置 ARIA 语义。
- [x] 新增 `/components/stepper` 可操作示例、组件渲染契约和导航模块索引；不内置路由、异步编排或业务流程状态管理。
- [x] 版本属性统一为 10.16.0；同步当前功能、设计规范、项目索引、README 发布 tag 示例，并通过构建、文档、CSS 检查。

## 10.15.0 Breadcrumb 功能发布

- [x] 新增 `Breadcrumb` / `BreadcrumbItem`，交付原生链接、当前页 ARIA 语义、禁用/隐藏过滤和可替换分隔符。
- [x] 新增 `/components/breadcrumb` 可操作示例、组件渲染契约和导航模块索引；不内置路由、自动折叠或异步导航。
- [x] 版本属性统一为 10.15.0；同步当前功能、设计规范、项目索引、README 发布 tag 示例，并通过构建、文档、CSS 检查。

## 宿主范围收敛（已完成）

- [x] 组件库仅面向 Blazor Server 与 Blazor WebAssembly；删除桌面示例工程、窗口背景与启动恢复脚本及专用样式。
- [x] 主题模块仅使用浏览器 API，移除原生窗口主题同步及专用 JS 互操作参数；保留系统主题监听、明暗/品牌切换与本地持久化。
- [x] 更新包描述与标签、示例介绍、中文文档及开发门禁；保留浏览器交互示例和 GitHub Pages 静态发布流程。
- 本批已收敛宿主支持范围，C# 组件参数与事件不变；包版本沿用当前发布批次，不再保留独立宿主发布状态。

## 10.14.3 全组件质量修复

- [x] P1：统一 `Visible=false` 隐藏契约；修复 ComboBox/Rating 键盘默认滚动与焦点；修复 Drawer/Popover `CloseOnEscape=false`；补齐 ComboBox、Radio/RadioGroup 的 FormField 状态继承。
- [x] P2：允许 ComboBox 展示 EmptyContent；修复 DateCalendar 在 `DateOnly.MinValue/MaxValue` 边界溢出；Tooltip 每次参数更新重新绑定；Tabs 动态注销与隐藏过滤；Accordion 禁用项不再保持展开布局；修正 Popup 示例参数。
- [x] P3：Drawer/Popover 增加退出过渡并等待实际 CSS 动画，适配 reduced-motion、快速重开和释放；不改变 Dialog/Notice。TimePicker 映射按 Min/Max 限定候选范围，并按实例复用无自定义委托、参数未变的映射。
- 构建与静态门禁：Debug / Release 解决方案构建均 0 警告/0 错误，15 组组件契约、30 项 Node 回归、组件/共享浮层 JS 语法、57 个 CSS 文件注释、99 项品牌对比度与文档一致性检查通过。
- 验证：例外：真实读屏与触控设备验收不在自动化覆盖范围。
- 发布范围包含下述 10.14.2 List 修复批次；统一使用 `v10.14.3` 触发现有 NuGet Trusted Publishing 工作流，发布核心包与 Font Awesome 图标包，不单独发布 10.14.2，也不另建 GitHub Release。

## 10.14.2 List 键盘修复（合入 10.14.3）

- [x] AllowClear 仅允许主动激活清空；重复 Home/End、单项循环导航不再清空选择。
- [x] 新增单一 List JS module，按当前可见 DOM 顺序导航，覆盖 keyed 重排；隐藏或禁用项不参与选择，隐藏活动项时清理引用。补齐 List/ListItem 的 hidden 隔离样式。
- [x] 只拦截容器自身已处理的导航/选择键，阻止默认页面滚动；保留 Tab、组合快捷键、输入法和后代控件行为，串行处理快速按键并释放监听。
- [x] 补充 C# 渲染契约、Node 键盘回归及可操作的重排/隐藏示例；其他组件仅进行审核，不在本批修复。
- 验证：差异：本批合入 10.14.3 统一发布（未单独发版）。

## 10.14.1 List 选中样式修复

- [x] 普通行与 CardMode 共用选中、悬停与活动项样式；Card 内层透明并继承文字色，移除卡片模式外围留白及独有活动项描边，选中底色覆盖整个卡片且禁用文字保持一致。
- [x] 示例页增加保留选择的 CardMode 切换，覆盖声明式单选/多选与 Items 数据模式；不更改选择和键盘业务逻辑。
- 验证：例外：浏览器视觉复核尚未执行。
- 使用 `v10.14.1` 触发现有 NuGet Trusted Publishing 工作流，发布核心包与 Font Awesome 图标包；不另建 GitHub Release。

## 10.14.0 List 泛型功能发布

- [x] List<TItem> / ListItem<TItem>、Items 自动推断、ItemTemplate、CardMode/CardTemplate；Card 外壳统一渲染，无模板只回退文本。
- [x] 声明式显式 TItem，保留 object 选择 API；修正集合名称冲突、动态注册 Value、禁用继承、活动项和移除数据的选择清理。
- [x] 交互字符串/模型/卡片/动态示例与泛型迁移说明；`Directory.Build.props` 四个版本属性统一为 10.14.0（程序集/文件版本为 10.14.0.0），同步中文文档、索引和 README，保留 10.13.0 历史发布记录。
- 验证：差异：两个 10.14.0 NuGet 包完成本地打包验证；浏览器视觉/辅助技术实机验收未执行（见下条）。
- 使用 `v10.14.0` 触发现有 NuGet Trusted Publishing 工作流，发布核心包与 Font Awesome 图标包；不另建 GitHub Release。
- 尚无浏览器视觉/辅助技术实机验收；本次发布不扩大上述验收边界。

## 10.13.0 功能发布

- [x] 将已实现的 ToggleGroup / ToggleGroupItem 与 SplitButton 纳入本次功能发布；v0.6 当时累计完成 3/5，Breadcrumb 在后续 10.15.0 批次交付。
- [x] 纳入字段尺寸一致性收口、ToggleGroup 连体分段外观、SplitButton 紧凑默认外观、Toolbar 间距与 MenuButton 专属菜单留白优化，以及菜单 ID 关联和非模态关闭焦点回归修复。
- [x] `Directory.Build.props` 四个版本属性统一为 10.13.0（程序集/文件版本为 10.13.0.0），同步功能清单、设计规范、路线图、审阅待办、项目索引与 README 发布 tag 示例；使用 `v10.13.0` 触发现有 NuGet Trusted Publishing 工作流，发布核心包与 Font Awesome 图标包。
- 验证：差异：两个 10.13.0 NuGet 包均完成本地打包验证。
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
- 验证：例外：当前工作区无可用浏览器自动化依赖（Playwright 亦无可用项目），未完成浏览器视觉验收。

## 阶段路线图归档（v0.1–v0.4）

计划期逐项写下的目标、验收重点与任务勾选已随阶段完成而收口，不再保留在现行文档中；现行 API 见 [`delivered-features.zh-CN.md`](delivered-features.zh-CN.md)，未完成能力见 [`component-plan.zh-CN.md`](component-plan.zh-CN.md)。以下只保留阶段结论与发布归属：

| 阶段 | 版本/tag | 交付组件 | 关键决策 | 是否单独发版 |
| --- | --- | --- | --- | --- |
| 第一阶段落地范围 | 未单独发布 | Button、ButtonGroup、Surface、Input、FormField | 以 Button 作为第一份参考实现验证 Intent/Variant/Size 与 Token 用法；`Stack`/`Flex` 曾列入但从未实现，布局由 `Surface`/`Card`/宿主样式承担 | 随当时发布批次 |
| v0.1 表单与选择基础 | `10.14.0` 前后逐步交付 | FormField、Label、Checkbox、Switch、Tag、List、Rating、ComboBox | 先统一字段语义（FormField/Label），再叠选择控件；ComboBox 最终交付为纯下拉选择，输入筛选未实现 | 随各自版本发布 |
| v0.2 基础控件与浮层 | `10.15.0`–`10.22.x` | Textarea、Radio/RadioGroup、Progress、Tooltip、PopupHost/Popover、Tabs、Menu | 浮层能力收敛到共享 `aeterni_floating.js`（定位/翻转、焦点陷阱、滚动锁），后续组件不再各写一套 | 随各自版本发布 |
| v0.3 展示与分段控件 | v10.6/v10.7 提前交付 | Divider、Empty、Spinner、Skeleton、Badge、Segmented、Drawer | `Segmented` 重构 `ThemeSwitch`；焦点陷阱与滚动锁先抽取再给 Drawer 复用 | 随 v10.6 / v10.7 发布 |
| v0.4 日期与时间 | `10.10.0` 起 | DatePicker、DateRangePicker、TimePicker、DateTimePicker | 基于 `DateOnly`，复用 Popover/FormField；`DateCalendar` 仅内部部件、非稳定 API | 随 v10.10 系列发布 |
| 宿主范围收敛 | 版本号未递增 | 移除桌面示例宿主，仅保留 Blazor Server / WebAssembly | 主题模块只用浏览器 API，保留系统主题监听与本地持久化 | 随当时发布批次 |
| 优先级 9 新组件 | `10.32.0`–`10.41.0` | Upload、Cascader、Transfer、Anchor、Splitter、NavMenu | 后台数据录入优先；NavMenu 替换 NavTree（破坏性）；组件计划只保留未交付候选 | 随各自版本发布（10.41.0 为破坏性 minor） |

阶段内逐项验收证据（契约断言、浏览器检查、图标/对比度门禁）保留在对应版本条目与提交历史中，不在此重复。

### 早期交付与审计记录（v0.2–v10.11，已完成）

早期版本的逐项记录是色彩体系、图标适配、玻璃配方与日期时间组件的唯一历史来源，保留如下（原表格已改为条目以便 diff）。

- **v0.2 计划**：高频基础控件与浮层能力已交付
- **Textarea**：原生 `<textarea>`、标准绑定、Rows/Resize、表单校验和无障碍状态；示例已同步
- **Radio / RadioGroup**：原生 radio/fieldset 语义、泛型绑定、禁用/必填/无效状态；示例已同步
- **Progress**：线性 progressbar、确定/不确定状态、语意色、尺寸和 reduced-motion
- **PopupHost / Popover**：定位（`PopupPlacement` 四方位）、视口翻转与交叉轴贴边、Escape/外部指针/遮罩关闭（通过 `OpenChanged`）、模态遮罩与 `aria-modal`、Tab 焦点陷阱、背景滚动锁与焦点回归
- **Tooltip**：四方位 hover/focus-visible 提示、`aria-describedby` 关联、翻转与贴边改为复用共享浮层模块
- **Tabs**：`tablist`/`tab`/`tabpanel` 三件套与按注册顺序派生的成对 id、roving tabindex（含选中项被禁用时回退到首个可用项）、自动激活模型 + 方向键/Home/End（RTL 自动互换）、禁用标签、窄屏横向滚动与细滚动条、`Value` 未匹配任何标签时默认显示首个可用项
- **Menu**：分组折叠、Accordion、选中项、图标/描述/禁用项；已接入组件示例侧边栏并新增示例专区；键盘模型、`aria-expanded`/`aria-controls`、折叠可见性、`Href` 链接项、受控 `OpenKeys`、分组级禁用/可见性、`ul`/`li` 语义、RTL 方向键与方向键滚动抑制已补齐
- **文档与代码一致性审计**：修正 delivered-features 标题编号（§21 重复）并补 Label、DialogProvider 章节；补齐 List/Menu/Rating 参数与枚举类型名；示例页补 Progress、Tooltip、Popover 参数表与 Radio 无障碍参数；清理规范中的陈旧组件名（Select→ComboBox、Stack/Flex、Toolbar/ToggleGroup）；把 check-docs.sh 写入 根目录入口与当时的开发规范
- **组件审阅修复批次 1（P0）**：Switch 关闭态滑块改用 `--aeterni-state-background-thumb`；Button 加载遮罩按变体取值、实心语意色改用 `--aeterni-color-on-semantic`；Alert 与 Toast 合并到同一位置容器
- **组件审阅修复批次 2（P1）**：Input/Textarea 焦点环；FormField 标签关联（ComboBox/Rating/RadioGroup 补 `aria-labelledby`）；List 选项改 `role="option"` 并移除选项焦点；Rating roving tabindex 与唯一 checked；ComboBox 选项非聚焦化；Radio 状态补齐与 Size 档位；Tooltip `aria-describedby` 与四方位翻转偏移；Tag 关闭热区 24×24；Popup 死 CSS 清理并在文档中明确基础版边界
- **组件审阅修复批次 3（P2）**：补齐浮层/紧凑表面度量 Token 并把组件裸像素收敛到 Token；删除死类、死 CSS 与死标记；统一圆角阶梯与类名映射（`ComponentClass`）；文案集中到 `AeterniUIOptions.Text`；CSS 与参数声明格式统一；滚动条改为细样式；主题系统偏好兜底与预渲染脚本；字体栈标注为可选依赖
- **组件审阅修复批次 4（P3）**：Tooltip 支持四个方位；RadioGroup 增加 `Orientation` 与垂直布局；通知数量上限写入文档；Rating 自定义图标与内置星形同尺寸；对话框打开时补偿滚动条宽度；Button 禁用态变量与 ButtonGroup 分隔线对齐注释
- **色彩体系优化（v10.3）**：品牌色按 OKLCH 重建（紫罗兰 `#795AD9`／深色 `#9985ED`）：峰值 chroma 0.237→0.186、色相漂移 7.8°→1.7°、明度阶梯拉直；语意色沿用 Apple 系统色，色阶以 500 档为锚点重建（浅端统一 L 0.976、深端 L 0.30），消除 `Info` 的 400→500 断层与色相漂移；中性文字从紫灰改为冷中性并换掉紫调阴影墨色；新增 `-text` 强调文字形态 Token（修掉亮色档当文字只有 2.2:1 的老问题）；实心控件字色改为「按填充明度分两层、三态不换字」，删掉 `--aeterni-color-on-semantic-strong`；Tag 改浅底深字（2.5~3.5:1 → 5.0~6.9:1）；全系统 44 项对比度校验通过
- **图标库完善**：新增核心 `AeterniIcons`（组件内字形全部改为图标渲染）；Font Awesome 适配包升级到 7.3.1 精选 343 个图标并提供 `Categories`/`TryGet`；新增生成脚本和 `/icons` 图标浏览页
- **全组件色彩与结构复核（v10.4）**：把「填充档不当 ink 用」的规则落实到全部组件：Icon 命名色、Alert/Toast 徽标与弹窗头部图标、FormField 反馈文字、Menu 选中项、Progress 填充/轨道、Rating 星形共六处换上文字形态或按色相染色（最低组合从 1.78:1 提到 3.15:1，ink 类从 1.91:1 提到 3.65:1）；浅色三级文字锚到最暗中性面；中性填充按钮 hover 方向改为两个主题一致；拆出通知卡片的填充/ink 双 accent 角色，消除 `Severity.Default` 死类。
  - 结构侧：`NoticeCard`/`ListItem` 重新接入基类属性契约并合并重复标记，静态 `Card`/展示型 `List` 不再注册 DOM 事件，`Progress` 去掉 inline `width` 与 `!important`，`Tag`/`Icon`/`Radio` 的第二套映射收敛到 `ComponentClass`，图标尺寸统一走 `--aeterni-icon-render-size`，六个组件补 `@ref`，`ThemeSwitch` 选项改单一模板，五处布尔型 ARIA 状态改为字符串输出；示例新增 `/components/colors` 验收页
- **样式体系一致性（v10.4）**：中性容器表面去除彩偏移：浅色四个容器背景改为完全无色（v10.3 的 +2~+4 蓝紫偏移在大面积上会被读成品牌色底，且磨砂层的 `saturate()` 会放大它），深色统一 +3，规则写入 AGENTS.md、规范与提交清单（容器背景只能是黑/白/灰/毛玻璃）；Menu 禁用态由整行 `opacity` 改为 `--aeterni-state-color-disabled`；组件内间距收敛到 `--aeterni-spacing-*`（gap/padding/margin 降为宿主别名）；新增 `--aeterni-transition-control` 消除字段控件三处重复的过渡声明；`--aeterni-opacity-muted` 与 `--aeterni-radius-badge` 获得真实消费者，交互态别名 `bg-hover/-active` 补语义注释；浮层半径层级与 `surface-soft`/`bg-secondary` 分工写入规范
- **文字色阶与观感清理（v10.4）**：文字改为「单一墨色 + 不透明度阶梯」（Apple label 模型）：浅色 `rgba(0,0,0,.78/.62/.56/.52/.36)`、深色 `rgba(255,255,255,.86/.56/.48/.40/.28)`，主色从 17:1 降到 11.7:1（不再发硬），二级/三级不再只差 1.16:1，且文字会随所在表面自动调和；`tertiary` 降为图标/装饰级，原先用它做正文的 Menu 分组标题、Menu 描述、ThemeSwitch 未选中项与示例页元信息改用 `secondary`/`muted`；Tag 标签文字改用 accent 的 text 形态（85% + 15% 正文墨）而不是「亮填充档 52% + 近黑 48%」，绿/黄 chip 不再浑浊（4.95~7.15:1）；新增不透明的 `--aeterni-separator` 作为卡片/对话框头尾分界（1.71:1 / 1.53:1，原 7% alpha 像污渍）；`--aeterni-color-on-semantic` 从蓝黑 `#0B0F19` 改为无色 `#141414`
- **Size 档位补齐（Switch / ComboBox / Rating）**：三个控件补 `Size`（`Small`/`Default`/`Large`）：ComboBox 触发器与选项共用一套档位 Token（Small 28px，与 `Input Size="Small"` 等高）；Switch 轨道由旋钮尺寸推导（跑道 = 旋钮×2 + 内边距×2 + 边框×2，滑块行程恰好一个旋钮宽，默认档与旧值一致，Small 34×20 / Large 46×26）；Rating Small 16px 星形、热区至少 24px、Large 24px + 12px
- **示例页拆分（v10.5）**：把原来的单页 + `switch` 拆成「一个组件一个页面」：`Pages/Components/` 下 25 个页面各自带 `@page`/`@layout`/`PageTitle` 与自己的 `@code` 状态，路由 `/components/{id}` 可直接分享；新增 `Layout/ComponentsLayout.razor`（侧栏 + 路由高亮，内嵌 MainLayout）与 `Components/Showcase/ShowcasePageBase.cs`（仅保留跨页共享的注入服务、交互提示与主题订阅）；作用域 CSS 无法跨页，展示样式迁到示例全局表 `wwwroot/css/showcase.css`；安装页改为垂直单列步骤（每步独占整行代码块）；规范 §10 与项目索引同步为「一个组件一个页面」
- **表面归属定案（v10.5）**：把「组件是否自带表面」写成规范 §5.3 两行表格（自带表面 vs 内容控件，判断依据是“能不能单独构成一块界面”）并进提交清单；`delivered-features` 的 Menu/Tabs 实现边界各补一条；示例预览把 Menu/Tabs 放进 `Surface` 宿主容器（原来它们在网格画布上裸奔，看起来像漏了背景）。
  - 结论是 Menu/Tabs 按内容控件保持透明，`List` 保持自带表面
- **共享浮层能力（v10.4）**：新增 `wwwroot/js/aeterni_floating.js`：视口贴合计算（`fitsSide`/`oppositeSide`/`clampCenteredShift`/`clampAlignedShift`）、`createFocusTrap`、引用计数的 `lockScroll`；Tooltip 改为复用，DialogProvider 改用共享滚动锁与可聚焦元素列表（Tab/Escape 仍由对话框堆栈自己持有，因为只有最顶层应响应）
- **多品牌色相层与切换**：① 交互态着色不再写字面量紫色，改由 `color-mix()` 从色阶推导；② 品牌色阶从裸 `:root` 拆成独立可覆盖的色相层（`:root, [data-aeterni-brand="purple"]`）；③ 新增第二套板 `[data-aeterni-brand="green"]`（500 档 `#2B7B33`）。
  - 两套板在浅色／深色／系统深色下逐条通过对比度门禁（绿色最紧 margin 1.151，紫罗兰 1.091，均达标；绿色锚点随后在「绿色板提亮重锚」一行重定），512 个 Token × 4 场景中紫罗兰与改造前**数值等价**（最大通道误差 0.0001，为 float32 量化），绿色场景 **0 处残留紫色**且语义停靠档自动跟随（浅色 500/600/700、深色 400/300/200）。
  - 配方侧新增两条硬规则进规范 §5.4：chroma 剖面必须用绝对值（相对色域比例跨色相不可迁移）、500 档明度按该色相的对比度行为枚举扫描定档。
  - ④ 切换链路打通：新建 `ThemeBrand` 枚举（不复用 Button 的 `Color` 语意色枚举，避免与语意色取值空间混淆）；`ThemeService` 增加 `Brand` 与 `SetBrand()`（`Enum.IsDefined` 校验，非法值抛 `ArgumentOutOfRangeException`，同值不触发）以及 `SetPurple()`／`SetGreen()`，并新增独立的 `BrandChanged` 事件——品牌与明暗正交，复用 `ThemeChanged` 会连带重新解析系统偏好；`AeterniUIOptions.DefaultBrand` 决定首访默认品牌并在 `AddAeterniUI` 里做枚举校验；`ThemeProvider` 首渲染恢复已存品牌、事件里应用并持久化到新键 `aeterni.theme.brand`，JS 侧新增 `applyBrand()`／`getStoredBrand()`／`persistBrand()` 并把 `applyTheme` 与 `applyBrand` 的过渡逻辑抽成共享的 `beginTransition`／`endTransition`；示例宿主 `index.html` 预渲染脚本同步读品牌键并写 `data-aeterni-brand`，示例 `/components/theme` 增加品牌切换控件。
  - 浏览器端到端 22 项断言全部通过：冷启动默认紫罗兰、经真实控件切到绿色后属性落在 `<html>`、语义别名整体换色（含深色停靠档）、写入 `localStorage`、**刷新首帧即为绿色**（属性变更轨迹 `[null,"green"]`，全程未出现 `purple`）、明暗切换不干扰品牌、非法存储值回落默认且色彩仍可解析、清空存储回到默认、切换复用 `aeterni-theme-transitioning` 过渡并在 620ms 后清理。
  - ⑤ 示例宿主 4 处直接消费原始色阶（`.preview-icon-wrap`、`.component-preview__eyebrow`、`.component-preview__api-row code`、`.doc-intro__eyebrow`）改为语义别名 `--aeterni-color-brand-text`，原先写死 `--aeterni-brand-500/600` 既不会跟随品牌也不会跟随明暗。
  - ⑥ 新增 `ThemeBrandSwitch` 组件（与 `ThemeSwitch` 同构的 `Segmented` 专用用法，订阅 `BrandChanged` 同步选中态）与对应文案 `ThemeBrandSwitchLabel`／`ThemeBrandPurpleLabel`／`ThemeBrandGreenLabel`，并放进示例宿主顶栏与明暗开关并列，任意页面都能直接看到整站换色。
  - **待做**：品牌色块预览型选项（会要求每个色相层再声明一份「别人的颜色」Token，等于把第二套色阶写进 CSS，暂不做）
- **品牌色板对比度门禁脚本化**：新增 `scripts/check-contrast.mjs`，把此前「靠人工校对」的对比度门禁变成构建期拦截（此前该项挂在多品牌行的待做里）。
  - 脚本不读注释而是重放真实级联：自带极简 CSS 扫描器切出规则（含 `@media (prefers-color-scheme: dark)` 的系统深色块，其选择器是 `:root:not([data-theme])…`，需要单独识别才能覆盖），按「品牌 × 浅色／深色／系统深色」三种 `<html>` 属性状态合并同特异性声明（文件顺序即优先级），再跟随 `var()` 链解析出实际颜色（`#hex`／`rgb()`／`rgba()`，遇无法解析的值或环状引用直接报错而不是静默跳过），半透明前景先与背景合成再测——因此 78% 墨色这类 Token 也按用户真实看到的像素计算。
  - 门禁集合 11 组／状态：实心填充承字（default／hover／active）、品牌文字（surface／tertiary／page）、链接（surface／page）按 4.5:1，边框／图标与焦点环按 3:1，当前 **2 色相层 × 3 状态 × 11 组 = 66 项全部通过**。
  - 另加两条结构性检查：色相层必须声明完整且仅为 50~900 十档；**余量低于 1.1× 的色相层必须在自己上方的注释块里写出该对比度**（`4.91:1` 这样的定值字符串），把规范 §5.4 新加的「显式记账」规则变成可执行约束——否则改色阶的人可以悄悄把预算花光。
  - 为此紫罗兰注释补记 4.91:1（margin 1.091，原本未记录）。
  - 已做反向验证：把绿色 500 提到 `#35A052`（3.33:1）与 `#228A40`（4.40:1）时脚本以 2 条错误失败，只提到 `#1C863B`（4.64:1，过线但花掉余量）时仅记账规则失败，把色相层改名成 `teal` 时无需改脚本即自动纳入检查。
  - 已接入 `.github/workflows/build.yml`，并同步 当时的入口、开发规范、项目索引与设计规范 §5.4
- **绿色板提亮重锚**：绿色色阶整体重锚到外部参考绿 `#1F883D`：色相 145° → 148.2°，500 档 L 0.517 → 0.552、C 0.132 → 0.145，整条阶梯按同一 ΔL 节奏平移 +0.035，chroma 仍按绝对值剖面（50:0.081 … 900:0.522）缩放，浅端升到 L 0.957、深端升到 L 0.301，比旧板更贴近规范里「浅端 ~0.976 / 深端 0.30」的统一端点。
  - 观感上浅色主题的填充、hover、链接、选中态都明显更亮更饱和，深色主题 300/400 停靠档亮度同步上抬。
  - **代价记账**：500 承白字 5.27:1 → 4.52:1，余量 1.004×，成为全库最紧的一条对比度门禁——仍满足 4.5:1，但任何进一步提亮或提高 chroma 都会击穿它；若要恢复余量，把 500 退回 L 0.545（`#1C863B`，4.65:1）或 L 0.540（`#19843A`，4.75:1）即可，其余档位随 ΔL 平移。
  - **验证**：重锚后按「品牌（紫／绿）× 浅色／深色」重跑 8 条对比度门禁共 32 项全部通过，深色侧最紧为绿色 `text-link` 承深色页面 5.95:1（余量 1.323，比旧板更宽松——深色停靠档整体上抬反而放宽了明度约束），浅色侧最紧仍是绿色 500 承白字 4.52:1；浏览器端到端 11 项断言按新色值更新后全通过（浅色 `default`/`hover` = `rgb(31, 136, 61)` / `rgb(5, 113, 44)`，深色 `text` = `rgb(144, 195, 151)`），并逐像素核验渲染结果与 Token 一致（示例页 4 处品牌实心填充采样主色 `#1F883D` 占 81.7%）。
  - 规范 §5.4 新增「余量低于 1.1× 的锚点必须显式记账」一条。
- **焦橙第三套品牌色相层**：在紫色与绿色之后新增 `[data-aeterni-brand="orange"]`，配方与前两套逐档一致（同一组相对 ΔL、同一 chroma 剖面、同一浅端色相漂移），500 档锚定焦橙 `#C2410C`（L 0.5534、C 0.1739、色相 38.40°）。
  - **为什么不是鲜橙**：`#E8590C` / `#EA580C` 这类亮橙承白字只有 3.58:1 / 3.56:1，低于 4.5:1，要做实心填充就得改成 `warning` 那种近黑前景，第三套板会脱离「白字填充」这条家族约定；焦橙承白字 5.18:1（余量 1.151×），三套板同一停靠档可直接互换。
  - **代价记账**：与语意 `danger` 红的 OKLab ΔE 只有 12.1，是全库最紧的一对品牌／语意间距（`warning` 22.4，`success` 34.8，`info` 37.3）；橙红在近明度下本就是邻居，正常色觉下仍清晰可分，但后续任何把 500 档往暖端推的改动都会继续压缩这段距离。
  - **接线**：`ThemeBrand` 新增 `Orange`、`ThemeService` 新增 `SetOrange()`、`ThemeProvider` 的存储恢复 switch 新增 `"orange"`、文案表新增 `ThemeBrandOrangeLabel`、`ThemeBrandSwitch` 的 `Brands` 数组由两项扩到三项（指示块行程由选项数量推导，组件无需其他改动）、示例 `/components/theme` 的 `BrandText` 同步。
  - JS 与预渲染脚本无需改动：`applyBrand()` 原样透传属性值，未识别值匹配不到任何色相层而回落 `:root` 的紫罗兰。
  - **验证**：`scripts/check-contrast.mjs` 自动发现新色相层，从「2 色相层 × 3 状态 × 11 组」扩到 **3 色相层 × 3 状态 × 11 组 = 99 项全部通过**，最紧一条仍是绿色 500 承白字 4.52:1（margin 1.004）；`dotnet build aeterni_ui.slnx` 0 警告 0 错误。
- **交互态品牌色阶复原与全组件状态色审计**：① **复原**：`--aeterni-state-background-hover/-active/-selected`、`--aeterni-state-border-hover/-active`、`--aeterni-state-color-*` 与 `--aeterni-button-intent-default-soft` 全部回到从 `--aeterni-brand-*` 推导（`color-mix()` 取档），撤销上一版「选择态中性化」——中性墨让表单控件与选中态读起来发死，品牌按钮的 soft 变体还会退成灰色；容器与页面背景仍严格保持无彩色。
  - ② **新增两档**：`--aeterni-state-background-checked-hover` / `-active`（浅色 500→600→700、深色 400→300→200），填满的勾选块在指针下整块沿色阶换档，墨仍用 `--aeterni-text-inverse`。
  - ③ **审计并修复 8 类**：Checkbox/Radio/Switch 的 checked hover 曾出现「边框换档、填充不动」与「填充换档、圆点/圆环不动」两种劈裂；三者的无效态悬浮会被 `--aeterni-state-border-hover` 覆写而丢掉危险色，Switch 的无效 + 选中还被拆成危险边框裹品牌填充；Pagination/Tabs/Menu/ListItem/ComboBox/DateCalendar 的 `:hover`（0,2,0）与 `.is-selected`／`.is-current`（同为 0,2,0）特异性相同，写在后面就把选中提示整块压掉；`IconButton` 生成的是 `aeterni-button--*` 而不是 CSS 里定义的 `aeterni-icon-button--*`，四种非 default intent 的形状与状态样式从未命中，且缺 `--aeterni-button-color-active`；`DatePicker.razor.css` 复制了一整份日历规则（101 → 42 行，删掉的 61 行与 `DateCalendar.razor.css` 逐条重复且已是后者子集）。
  - ④ **验证**：`dotnet build aeterni_ui.slnx` 0 警告 0 错误、`check-contrast.mjs` 99 项通过（最紧仍是绿色 500 承白字 4.52:1）、`check-css-comments.mjs` 48 文件通过、`check-docs.sh` 通过；用 headless Chrome（CDP `forcePseudoState` + 真鼠标 `mouseMoved`，读 computed style）在「紫/绿/橙 × 浅色/深色」下逐控件复验修复前后的每一档色值；48 张截图逐像素分色相统计显示每个品牌的功能页**跨品牌彩色像素 0.00%**（唯一例外是展示全部语义色的 `/components/colors`），彩色像素占比 0.8~2.2%，即三套色相层完全隔离且状态反馈仍在。
  - **复核结论**：现有 DatePicker、DateRangePicker 与 MenuButton 均由自身根元素规则负责宿主布局，仓库中已无 `__host` 选择器；PopupHost 只负责定位容器，不再重复承载这些组件的外观规则
- **示例页与首屏加载器重做**：① 「色彩与语义」页（`/components/colors`）从「摆组件」改为不引用任何组件的 Token 展台：色族色卡（每族三档色阶）、文字墨阶、字体与字号、中性容器表面、圆角阶梯、阴影层级、间距节奏与对比度验收组合；颜色全部消费 `--aeterni-*`，样式里零 hex/rgb 字面量，因此换主题与换品牌色相层都不需要改这一页。
  - ② 顺带修掉一个静态缺陷：`.component-preview__eyebrow` 的规则写在 `ComponentPreview.razor.css` 的作用域里（选择器带 `b-*` 属性），其它页面引用该类名永远不会命中，改用 `showcase.css` 的全局 `.showcase-page-eyebrow`，`Install` 页同步修正。
  - ③ 首屏加载器删掉 MVC 样板留下的灰色/蓝色 SVG 进度环与 `100%` 文案（`stroke:#e0e0e0` / `#1b6ec2` 是示例静态表里最后一处硬编码颜色），改为复刻 `Spinner` 规格的 `.sample-boot` 光环：走组件公开的 `--aeterni-spinner-size` 扩展点、3px 描边、右侧缺口、`--aeterni-duration-slower × 1.4` 线性周期、`opacity .92`，颜色取 `--aeterni-color-brand-text`，文案继续读运行时的 `--blazor-load-percentage-text`；`prefers-reduced-motion` 下由 Token 层清零时长并显式停转。
  - ④ 验证：`dotnet build aeterni_ui.slnx` 0 警告 0 错误；`check-docs.sh`、`check-css-comments.mjs`（48 文件）、`check-contrast.mjs`（99 项，最紧仍为绿色 500 承白字 4.52:1）全部通过；headless Chrome 在限速首屏下读到光环 48px / 3px 描边 / 右侧缺口透明 / 动画运行，浅色 `rgb(102,70,187)` 与深色 `rgb(183,173,247)` 自动换档，`prefers-reduced-motion: reduce` 下 `animation: none`，挂载后加载器标记消失，684px 窄屏无横向溢出。
  - **取舍**：加载器不可能真的实例化 `Spinner`（首屏没有组件树，组件的隔离样式也尚未加载），是刻意的视觉复刻，`app.css` 的注释写明了双向同步要求；色族按「每族一张卡、卡内三档」而不是参考图的「每档一张卡」排布
- **玻璃配方落地（三轴解耦）**：起因：iOS 27 桌面的背景模糊观感评估（只要模糊，不要液态玻璃的折射／高光边框）。
  - 分析结论是模糊半径本身够用（`--aeterni-blur-lg` 的 24px），短板在「填充 82% 把糊过的背景又盖回去」与「缺少可采样的背景内容」。
  - 落地方案拆成三根互不耦合的轴：**①填充**——Token 层新增三个玻璃角色别名 `--aeterni-bg-glass`（= `--aeterni-bg-elevated`，浅深同为 `.82`）、`--aeterni-blur-glass`（= `blur-md` 16px/135%）、`--aeterni-blur-scrim`（= `blur-sm` 8px/120%），填充刻意停在 `.82` 而不是提案里的 `.60`：更淡会把标准次要墨拽到 4.5:1 以下，等于逼全库说明文字换墨。
  - **随后补上决策 A**：`--aeterni-text-on-glass-secondary`（`color-mix(in srgb, var(--aeterni-text) 93%, transparent)`，只声明一次所以浅深两主题同余量）接在 `Card`／`Surface` 的磨砂态上，补掉深色「次要 · 标准墨」在 `.82` 上只有 4.35:1 的缺口，实测升到 7.52:1（浅）/ 7.01:1（深）；接管的是次要墨的四个角色名（`--aeterni-text-secondary`／`--aeterni-text-muted`／`--aeterni-state-color-readonly`／`--aeterni-state-color-muted`），因为别名在主题块里就完成了 `var()` 替换，只改基名会留下用 `--aeterni-text-muted` 写的说明文字仍在标准墨上；`--blur-none` 与不透明表面不接管，其余六个磨砂消费者（Dialog／Drawer／Popover／Tooltip／NoticeCard）当时暂未接管，已在「浮层接管玻璃墨」一行补齐。
  - **②半径**——新增公共枚举 `SurfaceBlur`（`Default`／`None`／`Small`／`Medium`／`Large`），`Card` 与 `Surface` 各加 `Blur` 参数；`Default` 沿用既有约定不输出修饰类（宿主可用全局 CSS 调默认档），`None` 在 Glass 变体上同时退回 `--aeterni-bg-solid`——只摘掉 `backdrop-filter` 会留一块 82% 的半透明色斑，比不透明更糟。
  - **③边框**——`.aeterni-card--glass`／`.aeterni-surface--glass` 里硬编码的 `border-color` 删除，玻璃边框改由既有的 `is-bordered` + `--aeterni-border-default` 决定，`Bordered="false"` 从此生效（此前追不上）。
  - 九处磨砂消费者（Dialog 面板与遮罩、Drawer、Popover 含遮罩、Tooltip、NoticeCard、Card／Surface 的 Glass 变体）改为引用新别名；`NoticeCard` 经 `--aeterni-notice-fill` 间接层换基（背景是 `color-mix(accent 9%, …)`，够不着 `background` 关键字），`is-no-blur` 重设该别名切实心。
  - 降级集中一处：文件尾两块 `@media (prefers-reduced-transparency: reduce)` 与 `@supports not (…)` 把 `--aeterni-bg-glass` 退 `--aeterni-bg-solid`、两支模糊退 `none`（遮罩保留压暗），因此 `Popover`／`Tooltip` 原有的组件级降级块可删——这是本轮唯一的删除。
  - 示例项目：`Surface`／`Card` 演示页加 `Blur` 控制项与 `.preview-glass-scene` 玻璃场景（背景层是与面板同级的兄弟节点，`aria-hidden`；`Surface` 的 `_surfaceBordered` 由 `true` 改 `false` 以对齐文档记载的默认值），新增只读对比页 `/components/glass`（`GlassLab` + `GlassContrast` + `GlassPalette`）。
  - 验证：`dotnet build aeterni_ui.slnx` 0 警告 0 错误；`check-docs.sh`、`check-css-comments.mjs`（49 文件）、`check-contrast.mjs`（99 项，最紧仍为绿色 500 承白字 4.52:1）、token-only grep（无 `.aeterni-*` 选择器）全部通过；headless Chrome 实测：`Blur=Large` 落到 `blur(24px) saturate(1.5)`、`Blur=None` 在 Glass 上退 `rgb(255,255,255)`、`Bordered=false` 后边框 `rgba(0,0,0,0)`、`--aeterni-bg-glass` 浅色 `rgba(255,255,255,.82)` / 深色 `rgba(26,26,29,.82)`、`prefers-reduced-transparency: reduce` 下 token 退 `#FFFFFF` / `none`（Dialog 遮罩保留 42% 压暗）。
  - **遗留**：`check-contrast.mjs` 的 `contrast()` 忽略背景 alpha，半透明 token 当背景会假通过，未修。
  - 版本号未递增，留给发布批次统一走 minor。
  - **后续**：②半径这条轴已被「模糊轴撤销」一行删除，理由是该轴与①填充争夺同一个 `background`
- **模糊轴撤销（回归 Glass 变体）**：起因：`Blur` 与 `Variant` 冲突——`Card.razor.css`／`Surface.razor.css` 里的 `.aeterni-*-blur-small/medium/large` 各自重写 `background`，而这三条规则声明在 `--subtle`／`--elevated` 之后，所以任何非 `Default` 档位都静默压过 `Variant`：`Variant` 参数看上去失效，两个参数争夺同一个属性，且失败是无声的（`Elevated + Blur=Large` 得到的是玻璃填充）。
  - 删除 `Enums/SurfaceBlur.cs`、`Card`／`Surface` 的 `Blur` 参数与 `BlurClass` 映射、四组 `--blur-*` 修饰类（含 `--glass.--blur-none` 的实心回退），模糊回归 `Glass` 变体本身（`--aeterni-bg-glass` + `--aeterni-blur-glass`）；ink 规则从 `.aeterni-card--glass:not(.aeterni-card--blur-none), .aeterni-card--blur-small, …` 收成 `.aeterni-card--glass`，`--blur-none` 不再存在所以排除项一并去掉。
  - **为什么不保留轴**：实心状态已由 `Default`／`Subtle`／`Elevated` 表达，「玻璃填充 + 不模糊」等于 `Elevated`，该轴没有带来变体表达不了的能力；而 `Blur=None` 与 `Variant=Default` 是同一个结果的两条路径，保留只会让参数面继续撒谎。
  - 示例页 `Card`／`Surface` 的 `Blur` 控制项与参数表行同步删除，`GlassLab` 页四处「模糊档位交给 `SurfaceBlur`」改为「模糊固定由 Glass 变体承担」；`docs/delivered-features` 的「模糊与实心」小节重写并记下为什么不要再引入正交模糊轴，`engineering-reference` 的同项改成硬规则。
  - 验证：`dotnet build aeterni_ui.slnx` 0 警告 0 错误；四门禁通过；headless Chrome 实测 `Card` 与 `Surface` 页面四种 `Variant` 各自呈现自身填充（此前四种变体中心像素相同、只有 `Glass` 会显示，根因是示例页预览框的层叠，已在另一行修掉）。
  - 版本号未递增
- **浮层接管玻璃墨**：起因：磨砂配方统一之后，五类浮层（Dialog 面板、Drawer 面板、Popover 表面、Tooltip 内容、Alert／Toast 卡片）仍在用标准次要墨 `--aeterni-text-secondary`（0.56 倍），深色主题在 82% 填充上只有约 4.35:1，低于 4.5:1 文字底线——只有 `Card`／`Surface` 的 `Glass` 变体切到了 `--aeterni-text-on-glass-secondary`，于是同一块玻璃上出现两种墨。
  - 给这五处各加一段与 `Card.razor.css` 同构的四角色重设（`--aeterni-text-secondary`／`--aeterni-text-muted`／`--aeterni-state-color-readonly`／`--aeterni-state-color-muted`），四个必须一起改：别名在主题块里就完成了 `var()` 替换，只改基名会留下用 `--aeterni-text-muted` 写的说明文字仍在旧墨上。
  - 两类刻意排除：Dialog／Drawer／Popover 的**遮罩**不承字，换成玻璃墨只会把压暗加深；通知卡片的 `is-no-blur` 是实心回退，保持标准墨。
  - 填充与模糊未动，所以这是一次纯墨替换，没有新增 token、没有新档位。
  - 验证：`dotnet build aeterni_ui.slnx` 0 警告 0 错误；四门禁通过；headless Chrome 实测浅／深两主题下 Dialog 面板、Drawer 面板、Popover 表面、Tooltip 内容、Alert／Toast 卡片与 `Card`／`Surface` 的 `Glass` 变体解析出同一支 `--aeterni-text-secondary`（浅 `color(srgb 0 0 0 / 0.725765)`、深 `color(srgb 1 1 1 / 0.798706)`），`--aeterni-text-muted`／`--aeterni-state-color-readonly`／`--aeterni-state-color-muted` 三者同值；`is-no-blur` 与 Dialog／Drawer／Popover 三类遮罩仍为 `rgba(0, 0, 0, .62)`／`rgba(255, 255, 255, .56)`。
  - 版本号未递增
- **玻璃三轴对照矩阵（模糊／饱和度／遮罩）**：起因：上一轮只读审计里，「模糊半径没有梯度」「`saturate` 让玻璃带色偏」「遮罩带 8px 模糊」三条都只是静态判断，没有实测依据。
  - 做法：把 `/components/glass` 从六节扩到八节——04「模糊半径」由空壳变成真布景（8／16／24／32px 四档），新增 05「饱和度」（100／120／135／150%）与 06「遮罩」（none／8／16px）两行，遮罩那一行用「整块视口 + 浮在中间的面板」的装置。
  - 三行一律只改一根轴：04 的填充停在库现值 `.82`、`saturate` 钉 `135%`；05 的填充同上、模糊钉 16px；06 的三格压暗值（`--aeterni-overlay`）完全相同。
  - 结论（1440×1000、DPR 2 的无头 Chromium 逐像素实测）：**①模糊半径在 `.82` 的填充上几乎不可测**——壁纸／细网格／12px 正文上从 8px 直接跳到 32px 只有约 4% 的像素变化（噪声底约 2%，全部落在文字抗锯齿），只有「App 图标墙」分得出，面板内部梯度 0.249／0.064／0.061／0.057，即 8→16 一次降掉四分之三、16 以上只剩 5% 与 7%，所以上轮审计「给大面板补 24px」降级为可拍板的取舍而非缺口，`--aeterni-blur-lg` 继续 0 处使用；**②`saturate` 是可测的真轴**——壁纸上面板区域彩度均值 5.46／6.50／7.26／8.06，135% 比 100% 把透出的彩度抬高 33%、150% 抬高 48%，而在无彩色背景（细网格／12px 正文）上是逐像素相同的空操作，这就是「让玻璃带色偏」的量化依据；**③遮罩 8px 已经够**——面板外那片区域的梯度 1.29 → 0.19 → 0.18，16px 只再降 3%，观感上没有收益，收益只在每帧一次的更大核卷积。
  - 本行只改示例页与文档，库侧零改动；读数方法（亮度权重、2/255 计数阈值、噪声底）写在 08 节与 `GlassLab.razor.css` 的注释里。
  - **本条读数随后被下一行「磨砂填充降档与玻璃墨阶梯」推翻重测**——旧读数全部基于 `.82` 的填充，填充降到 `.68`／`.72` 之后口径与数字都变了：①**模糊半径由死转活**，面板内残余细节（亮度标准差）`0.0508／0.0323／0.0184／0.0139`，8→16 降 37%、16→24 降 43%、24→32 降 24%，相邻档位的逐像素差异 92%／85%／39%，所以 24px 不再是缺口，而是四步里收益最大的一档；原「其余三档背景分不出四格」的定性结论不变（壁纸档 8→32 只降 25%、单步变化像素 < 1%，细网格档 8px 起残余已在 0.001 量级，12px 正文档过了 16px 就不再变）
    - ②**饱和度读数改写**为彩度均值 `13.14／15.73／17.73／19.69`（仍严格正比于 100／120／135／150%，即 135% 抬高 35%、150% 抬高 50%），无彩色背景上是逐像素相同空操作的结论不变
    - ③**遮罩梯度改写**为 `0.0026／0.00033／0.00026`（8px 一次抹掉 87% 的高频，16px 只再多抹 21%，绝对值只剩 0.00007），结论「8px 已经够、再往上只买到更大的卷积核」不变
    - ④04／05 的读数改为取「截图时把文字遮住」之后的面板区域统计（残余细节用亮度标准差、彩度用 max − min(R,G,B)），因为旧口径的噪声底就落在文字抗锯齿上
- **磨砂填充降档与玻璃墨阶梯（on-glass tertiary）**：起因：上一轮把「填充不能低于 82%」归因于三级墨的图形门槛，但那条约束只是因为玻璃表面仍在继承标准墨阶梯——该改的是墨，不是填充。
  - 做法：①把 `--aeterni-bg-glass` 从 `--aeterni-bg-elevated` 解耦成独立 token，三个主题块各自声明（浅 `.68`／深 `.72`／系统深 `.72`），因为「升起的表面」与「磨砂表面」答的不是同一道题：前者只要求落在页面上，后者要按最坏背景（纯黑／纯白）保证墨可读，共用一个值正是填充偏厚、糊不进背景的原因
    - ②新增 `--aeterni-text-on-glass-tertiary`（`color-mix(in srgb, var(--aeterni-text) 67%, transparent)`），把三级墨也纳入 on-glass 阶梯——三级墨在玻璃上只承图标与装饰（Dialog／Drawer／NoticeCard 的关闭按钮、日期日历的 `.is-outside`），所以按 3:1 图形门槛定档
    - ③七处磨砂内容表面（`Card`／`Surface` 的 `Glass` 变体、Dialog 面板、Drawer 面板、Popover 表面、Tooltip 内容、NoticeCard）各补一条 `--aeterni-text-tertiary`，并同步注释里「深色 4.35:1」这类旧读数
    - ④删除三个编码被否决方向（液态玻璃高光边）的孤儿 token：`--aeterni-backdrop-saturate`、`--aeterni-glass-border-alpha`、`--aeterni-glass-highlight`，全库 0 引用
    - ⑤两个降级块（`prefers-reduced-transparency: reduce` 与 `@supports not (backdrop-filter)`）把两支玻璃专用墨退回标准墨——墨的存在理由是顶住透出来的背景，填充一旦是实心表面就没有背景要顶
    - ⑥修 `Bordered="false"` 的玻璃卡 hover 浮出边框：hover 规则 4 个类压过 `.is-bordered` 的 2 个类，把改边框那一条拆到 `.is-interactive.is-bordered` 上，位移与阴影仍对所有变体生效。
  - 实测口径（填充合成后的底色，最坏背景，1.1× 会计余量）：深色 `.82` → 标准次要墨 4.35:1 FAIL、标准三级墨 3.02:1 thin，`.72` → 3.41 FAIL／2.51 FAIL，接管 on-glass 后 5.11／3.51；浅色 `.82` → 5.33／3.86，`.68` → 4.51／3.43，接管后 5.94／3.46。
  - 下限由最紧的那支墨决定，两个主题不同：浅色是三级墨（1.15×）、深色是次要墨（1.14×），所以 `.68` 与 `.72` 分别是两个主题的边界值，再淡一档就掉出余量。
  - **本条推翻的旧结论**：上一轮「填充已被三级墨钉死在 82% 以上」不成立——那是标准墨的约束。
  - **未覆盖面（如实记录）**：品牌链接墨不在 on-glass 覆盖内，也不在任何门禁里，它本来就撑不住磨砂——深色最坏背景 `.82` 已是 3.25:1 FAIL、`.72` 掉到 2.27:1，浅色 `.82` 4.36:1 FAIL、`.68` 2.98:1；降填充把「链接可读的背景窗口」进一步收窄（浅色填充要求底色亮于 18/255 → 103/255，深色要求暗于 134/255 → 95/255），这是既有缺口而非本轮引入，已写进组件设计指南的硬规则。
  - 验证：`dotnet build aeterni_ui.slnx`、`check-docs.sh`、`check-css-comments.mjs`、`check-contrast.mjs`（99 项）、token-only grep 全过；headless Chrome 逐像素核对七处磨砂表面的墨与两个降级块。
  - 示例项目：`GlassLab` 对照页同步新填充（`GlassPalette` 的 `.68`／`.72`）、新增第五支读数「三级 · on-glass」、04／05／06 三节按新填充重测并把基于 `.82` 的旧数字全部替换（口径也改为「截图时把文字遮住」之后的面板区域统计），其中模糊半径轴由死转活、24px 从缺口变成收益最大的一档（更正见上一行）。
  - 文档同步三处：`delivered-features` 的玻璃角色别名条目（含十处消费者与填充下限的归因）与「模糊与实心」小节、`engineering-reference` 的玻璃硬规则（含新增的品牌墨警示：品牌墨浅色最坏 2.98:1／深色 2.27:1，不在门禁覆盖内）与提交检查清单新增一条「新增磨砂表面须一次接管五个角色名」、`release-history` 本行。
  - 项目索引 无需改动：本轮没有新增或移动项目、目录、入口文件、宿主、构建脚本或开发命令。
  - 版本号未递增
- **v10.9.0 发布（磨砂配方解耦与填充降档）**：版本号在 `Directory.Build.props` 单一来源递增 `10.8.0` → `10.9.0` （`Version`／`AssemblyVersion`／`FileVersion`／`InformationalVersion` 四处齐动）， 四份门禁文档的「文档版本」与 项目索引 的「当前版本」同步， `review-plan` 一并跟进，README 的发布示例改为 `v10.9.0`； `bash scripts/check-docs.sh`（版本格式 `^10\.[0-9]+\.[0-9]+$` 与四文档一致性）与 `dotnet build aeterni_ui.slnx` 通过； 在 main 上打注解 tag `v10.9.0` 触发 `Publish NuGet packages`，工作流先由「Validate release tag」比对 tag 与 props 版本， 再用 NuGet Trusted Publishing（OIDC）发布 `AeterniUI` 与 `AeterniUI.Icons.FontAwesome`。
  - 按仓库约定「功能新增递增次版本号」，本批（玻璃配方三轴解耦、on-glass 墨阶梯与磨砂填充降档）为 minor： 相对已发布的 `10.8.0` 无破坏性公共 API 变更——被撤销的 `Blur` 轴是同一批内引入、从未出现在任何已发布版本里，故不递增主版本号
- **v10.10.0 / v0.5 日期与时间输入**：新增 `TimePicker`、`DateTimePicker`、`TimeFormat` 与 `DateRangePreset`；`DateRangePicker` 补充宿主定义快捷范围和 1～3 个月视图；示例、文案、功能事实源和项目索引同步。
  - 版本单一来源由 `10.9.0` 递增为 `10.10.0`。
  - 验证：`dotnet build aeterni_ui.slnx --no-restore` 0 警告/0 错误，`check-docs.sh`、`check-css-comments.mjs`（52 个 CSS 文件）、`check-contrast.mjs`（99 项）与 Font Awesome 生成一致性检查全部通过
- **v10.10.1 / v0.5.1 质量收口**：完成 REV-64～REV-73、REV-75～REV-76：恢复 DatePicker/MenuButton 根契约，补齐 Accordion、DateCalendar、TimeOptionList 焦点与 ARIA，统一日期字段状态、Segmented 选中态、文案与尺寸 Token，扩充示例，并新增 `AeterniUI.ContractChecks` CI 门禁。
  - 按项目版本约定，本批属于现有能力修复与优化，版本单一来源由 `10.10.0` 递增为 `10.10.1`；首位继续与 .NET 10 对齐。
  - 验证：`dotnet build aeterni_ui.slnx` 0 警告/0 错误，6 组渲染契约检查、全部 `.razor.js` 语法、文档、52 个 CSS 文件注释、99 项品牌对比度、Font Awesome 343 图标一致性和 Token-only 检查全部通过；无头 Chrome 复核 1440px 浅色/深色与 390px 窄屏布局。
- **DateTimePicker 组合弹层布局修复**（已完成／静态与契约门禁验证，视觉待复核）：弹层按双栏内容定宽并受视口约束，时间列最小宽度与实际滚轮一致；操作行使用自然高度，空状态居中且保留滚轮占位，避免按钮拉高及溢出。
  - 按用户确认保留现有提交行为，不给 DatePicker / DateRangePicker 新增确认模式。
- **单值选择器宽度统一与示例合成优化**（已完成／契约检查覆盖，模糊效果待宿主实测）：DatePicker、TimePicker、DateTimePicker 统一 160px 最小宽度与 FullWidth 行为，DateTimePicker 示例新增交互开关；示例容器入场动画不再保留 forwards 状态，避免 opacity 动画残留合成层干扰后代毛玻璃采样，不修改模糊强度与透明度。
- **日期与时间选择器布局调整**（已完成／契约检查覆盖）：DateRangePicker 弹层按内容定宽并限制在视口内，修正多月内容溢出导致的左右留白不一致；TimePicker 默认最小宽度使用 160px Token；DatePicker 新增 FullWidth，示例提供交互开关，根布局类纳入契约检查。
- **TimePicker 滚动流畅性修复**（已完成／自动化回归覆盖）：时/分/秒滚轮改为停稳后回调草稿，取消逐项强制停止；相同选中值的重渲染不再重新居中，联动列继续同步。
  - 新增 `tests/time-wheel.test.mjs` 覆盖连续滚动、回调去重、重渲染、联动及释放；继续修复反向输入与旧吸附动画争抢滚动位置：移除 CSS 强制吸附，输入时取消旧 JS 动画，并通过 ResizeObserver 在隐藏弹层展开时重新居中；回归覆盖隐藏展开与反向中断。
  - 实际设备惯性手感仍需人工验证。
- **v10.11.0 / TimePicker 时分秒滚轮**：`TimePicker` / `DateTimePicker` 默认步长由 30 分钟调整为 1 秒，默认显示策略改为 24 小时制和 `HH:mm:ss`；保留 `Step` / `TimeStep` 的 `TimeSpan` API，并支持 1 秒到小于一天的整秒步长。
  - 内部选择面从全天扁平列表改为类似系统计时器的时/分/秒三列滚轮，具有五行视窗、顶部中文单位、无圆角且无左右边框的固定中心选择带、平滑吸附、上下渐隐和近大远小反馈，并提供取消/确定按钮与不污染绑定值的草稿状态；1 秒步长最多渲染 24 + 60 + 60 个当前列选项而不是 86,400 个时间点。
  - 触发器默认按时间文本与图标紧凑显示，新增 `FullWidth` 作为显式铺满入口；DateRangePicker 示例移除“最近/未来 7 天、30 天”业务预设，仅保留直接范围选择，通用 `Presets` API 仍由宿主按业务需要提供。
  - 左右键切列，上下键/Home/End 改值，Enter/Space 确认。
  - 按版本规范属于同一批功能扩展，版本保持 `10.11.0`。
  - 验证：解决方案构建 0 警告/0 错误，6 组组件契约、全部 JS/CSS/文档/对比度/图标门禁通过；无头 Chrome 实测三列中心与选择带中心完全重合，`00:00:00`、`23:59:59` 均可滚动选中，取消不回写、确定后关闭并更新触发器。
