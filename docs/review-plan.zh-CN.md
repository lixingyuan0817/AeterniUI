# AeterniUI 审阅计划

文档状态：第十轮 31 条（REV-133～REV-163）随 `10.31.1` 归档，第十一轮 14 条（REV-164～REV-177）随 `10.34.4` 归档；第十二轮修复随 `10.46.0`、第十三轮修复随 `10.47.0` 归档，剩余条目见下表。版本以 `Directory.Build.props` 为准。

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

## 活动条目（第十二/十三轮遗留）

第十二轮修复随 `10.46.0`、第十三轮修复随 `10.47.0` 归档；下表只保留仍未完成、已取证的条目。

| 优先级 | 条目 | 证据位置 | 验收条件 |
| --- | --- | --- | --- |
| P2 | `MultiSelect.ReadOnly` 仍是 `tabindex="-1"` 的不可聚焦只读，而 ComboBox/Cascader/日期族是“可聚焦、可复制”的只读；同族语义不齐 | `MultiSelect.razor(.cs)` | 统一只读的焦点与复制模型，并同步契约/示例 |
| P2 | `Autocomplete.Loading` 沿用旧契约（禁用输入与清除），而 ComboBox/Cascader/MultiSelect 的 Loading 是“面板忙碌、触发器可开关”；同一状态四种行为 | `Autocomplete.razor(.cs)`、四个选择器契约 | 定一条 Loading 语义并统一，更新 ContractChecks |
| P2 | `DataList` 示例宣称“筛选把行移出列表后选择仍然保留”，但 `VirtualList.PruneSelectionAsync` 会剔除不在当前 Items 里的选中值 | `DataList.razor`、`VirtualList.razor.cs` | 二选一：修 prune 语义（像受控组件一样不 prune），或改示例文案 |
| P2 | `Size.Default`≡`Medium`、`Color.Default`≡`Primary`，控件面板里选不出差别；`Default` 在 Icon/Spinner 上又是 `currentColor` | `Enums/Size.cs`、`ComponentClass.cs`、Tag/Avatar/Badge CSS | 明确档位语义并消除重复渲染 |
| P2 | 示例站存在第二套排版：27 处裸 px 字号、16 处 clamp、13 种字距字面值，与颜色页自述的 12–60px 阶梯不符 | `showcase.css`、`Home.razor.css`、`Screen.razor.css` | 收敛到 `--aeterni-font-size-*` / `--aeterni-letter-spacing-*`，展示字例外需新增 display 档 |
| P2 | 深色主题 Token 块与 `prefers-color-scheme` 回退块整段重复（约 150 行），修改需两处同步 | `aeterni_ui.css` 的 dark 块与媒体块 | 合并为单一来源或加生成/校验步骤 |
| P2 | `--aeterni-radius-button` 为 10px（唯一 rem 半径、阶梯外），与同排 Input 6px 不同源 | `aeterni_ui.css`、Button/Input/ToggleGroup/Pagination CSS | 收进 2/4/6/8/12/16/24 阶梯或统一控件档 |
| P3 | 搜索面板无退场动画；Toast 超过上限时最旧一条瞬删；Popover 位移方向不随 placement；ComboBox/Cascader 复制 Popover 关键帧 | `ShowcaseSearchLayer.razor`、`DialogService.cs`、`Popover.razor.css`、`ComboBox.razor.css`、`Cascader.razor.css` | 统一退场与方向，删除重复关键帧 |
| P3 | Tooltip 无显示延迟；菜单触发器不支持 ArrowDown/ArrowUp 打开并聚焦首项；触屏 hover 粘滞无 `(hover: hover)` 守卫 | `Tooltip.razor.css`、`MenuButton.razor.cs`、`FlashCard.razor.css` | 补延迟与键盘习惯，按指针能力分流 hover |
| P3 | `app.css` 残留 Blazor/Bootstrap 模板规则与全局 `h1:focus{outline:none}`；展示字号超 60px；断点注释引用不存在的 token；`gray`/`neutral` 两套灰阶 | `app.css`、`Home.razor.css`、`Screen.razor.css`、`DateRangePicker.razor.css`、`aeterni_ui.css` | 清理死规则、统一 display/断点/灰阶 |
| P3 | API 表支持矩阵在“同页多个组件全部支持”时会重复列出同一组公共参数，可做页级合并减少视觉重复 | `ShowcaseApiTable.razor`、`ShowcaseApiReference.generated.cs` | 一致时合并为页级公共参数，不一致时仍按组件拆分 |

## 已归档轮次

第十轮全库组件审核（REV-133～REV-163，31 条）随 `10.31.1` 归档，第十一轮全库审阅（REV-164～REV-177，14 条）随 `10.34.4` 归档；逐项证据、批次划分与验证结果见 [`release-history.zh-CN.md`](release-history.zh-CN.md)，本文件不保留已收口条目，也不复述其结论。
