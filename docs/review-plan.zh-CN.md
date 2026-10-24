# AeterniUI 审阅计划

文档状态：第十轮 31 条（REV-133～REV-163）随 `10.31.1` 归档，第十一轮 14 条（REV-164～REV-177）随 `10.34.4` 归档；第十二轮审阅的修复批随 `10.46.0` 归档，剩余条目见下表。版本以 `Directory.Build.props` 为准。

阅读顺序：当前无活动轮次时，只需读「优先级定义」与「已归档轮次」；有活动轮次时再读问题与修复批次。文档职责与证据要求见 [`AGENTS.md`](../AGENTS.md) §2「文档职责」与 §4.7；本文件只记录问题、证据、修复方向和验收条件，不复述交付功能或历史结论。

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

## 活动条目（第十二轮遗留）

第十二轮已修复项随 [`release-history.zh-CN.md`](release-history.zh-CN.md) 的 `10.46.0` 条目归档；下列条目已取证、本轮未修，按优先级继续处理。

| 优先级 | 条目 | 证据位置 | 验收条件 |
| --- | --- | --- | --- |
| P1 | 四个选择器能力不齐：`Autocomplete` 有 `Clearable`/`Loading`/`ReadOnly`，`ComboBox`/`Cascader` 没有；`MultiSelect` 缺 `Loading`；四者弹出位置硬编码 `BottomStart`，而同族日期/菜单控件暴露 `Placement` | `ComboBox.razor`、`Cascader.razor`、`MultiSelect.razor`、`Autocomplete.razor` 与各自 `.razor.cs` | 统一“清空 / 忙碌 / 只读 / 方向”四件套，并补示例与 API 表 |
| P1 | `List<TItem>`/`VirtualList<TItem>` 的选中 API 仍是非泛型 `object?`，宿主每个回调都要转换 | `List.razor.cs`、`VirtualList.razor.cs`、`DataList.razor` | 提供 `TItem` 类型化选择 API（可保留 Obsolete 旧重载过渡） |
| P1 | 日期族缺独立 `AriaDescribedBy` 与 `ReadOnly`：不使用 `FormField` 时无法挂外部描述；只读展示只能退化成 Disabled | `DatePicker.razor.cs`、`DateRangePicker.razor.cs`、`DateTimePicker.razor.cs`、`TimePicker.razor.cs` | 补 `AriaDescribedBy` 合并与 `ReadOnly` 策略；示例覆盖 |
| P2 | `ButtonIntent` 缺 `Success`/`Info`，正向主操作只能自定义 CSS；色轴与 Tag/Badge 的 `Color` 不齐 | `Enums/ButtonIntent.cs`、Button 系列 CSS | 补齐语意档或统一色轴 |
| P2 | `Size.Default`≡`Medium`、`Color.Default`≡`Primary`，控件面板里选不出差别；`Default` 在 Icon/Spinner 上又是 `currentColor` | `Enums/Size.cs`、`ComponentClass.cs`、Tag/Avatar/Badge CSS | 明确档位语义并消除重复渲染 |
| P2 | API 表把公共参数无条件拼到每页（ThemeProvider/FormDialog 也列无效参数），且多组件合页（Dialog/Alert/Confirm/Toast、DatePicker/DateRangePicker）未按组件二级分组 | `ShowcaseApiTable.razor`、`ShowcaseApi.cs`、`ShowcaseApiReference.generated.cs` | 输出“基础参数支持矩阵”并按组件分组 |
| P2 | 示例站存在第二套排版：27 处裸 px 字号、16 处 clamp、13 种字距字面值，与颜色页自述的 12–60px 阶梯不符 | `showcase.css`、`Home.razor.css`、`Screen.razor.css` | 收敛到 `--aeterni-font-size-*` / `--aeterni-letter-spacing-*`，展示字例外需新增 display 档 |
| P2 | 深色主题 Token 块与 `prefers-color-scheme` 回退块整段重复（约 150 行），修改需两处同步 | `aeterni_ui.css` 的 dark 块与媒体块 | 合并为单一来源或加生成/校验步骤 |
| P2 | `--aeterni-radius-button` 为 10px（唯一 rem 半径、阶梯外），与同排 Input 6px 不同源 | `aeterni_ui.css`、Button/Input/ToggleGroup/Pagination CSS | 收进 2/4/6/8/12/16/24 阶梯或统一控件档 |
| P2 | 侧栏长名截断无 title/提示（DatePicker/DateRange…、Dialog/Confirm/Alert…）；同一多组件页在侧栏/标题/首页卡片有三种写法 | `ShowcaseCatalog.cs`、`Menu.razor.css`、`Menu.razor` | 短标签 + 完整名提示，统一命名分隔符 |
| P2 | 侧栏当前项整圈品牌描边与 focus-visible 环相似，页头当前项却只用下划线，全站两套“当前”语言 | `Menu.razor.css`、`showcase.css` | 当前项改为填充/左侧条，描边留给焦点 |
| P2 | 搜索泛化词（如“何时使用”）返回几十条同名结果，无分组/计数/高亮；两个搜索入口分工仍不够显式 | `ShowcaseSearchModel.cs`、`ShowcaseSearchLayer.razor`、`ComponentsLayout.razor` | 按页面分组、显示命中数、高亮匹配词 |
| P2 | 图标页默认全量渲染 343 项，名称不可复制/不可链接，行长与组件文档页不一致 | `Icons.razor`、`Icons.razor.css` | 分组筛选/锚点、名称可复制、统一 68ch 行长 |
| P3 | 搜索面板无退场动画；Toast 超过上限时最旧一条瞬删；Popover 位移方向不随 placement；ComboBox/Cascader 复制 Popover 关键帧 | `ShowcaseSearchLayer.razor`、`DialogService.cs`、`Popover.razor.css`、`ComboBox.razor.css`、`Cascader.razor.css` | 统一退场与方向，删除重复关键帧 |
| P3 | Tooltip 无显示延迟；菜单触发器不支持 ArrowDown/ArrowUp 打开并聚焦首项；触屏 hover 粘滞无 `(hover: hover)` 守卫 | `Tooltip.razor.css`、`MenuButton.razor.cs`、`FlashCard.razor.css` | 补延迟与键盘习惯，按指针能力分流 hover |
| P3 | `app.css` 残留 Blazor/Bootstrap 模板规则与全局 `h1:focus{outline:none}`；展示字号超 60px；断点注释引用不存在的 token；`gray`/`neutral` 两套灰阶 | `app.css`、`Home.razor.css`、`Screen.razor.css`、`DateRangePicker.razor.css`、`aeterni_ui.css` | 清理死规则、统一 display/断点/灰阶 |

## 已归档轮次

第十轮全库组件审核（REV-133～REV-163，31 条）随 `10.31.1` 归档，第十一轮全库审阅（REV-164～REV-177，14 条）随 `10.34.4` 归档；逐项证据、批次划分与验证结果见 [`release-history.zh-CN.md`](release-history.zh-CN.md)，本文件不保留已收口条目，也不复述其结论。
