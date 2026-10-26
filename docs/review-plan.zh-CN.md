# AeterniUI 审阅计划

文档状态：第十轮 31 条（REV-133～REV-163）随 `10.31.1` 归档，第十一轮 14 条（REV-164～REV-177）随 `10.34.4` 归档；第十二轮随 `10.46.0`、第十三轮随 `10.47.0`、第十四轮随 `10.47.1` 归档，剩余条目见下表。版本以 `Directory.Build.props` 为准。

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

## 活动条目（第十二～十四轮遗留）

第十二轮随 `10.46.0`、第十三轮随 `10.47.0`、第十四轮随 `10.47.1` 归档；下表只保留仍未完成、已取证的条目。

| 优先级 | 条目 | 证据位置 | 验收条件 |
| --- | --- | --- | --- |
| P2 | `Size.Default`≡`Medium`，控件面板里选不出差别，仍待处理。`Color.Default` 已收口：内容组件默认中性、`Primary` 才出品牌；`Icon` / `Spinner` 的 `Default` 是 `currentColor`；`Progress` 是活动指示、默认品牌（例外）。消除 `Size` 重复档位会改变公开枚举与默认值，需要单独一次破坏性设计 | `Enums/Size.cs`、`ComponentClass.cs`、Tag/Avatar/Badge CSS | 定一套 `Size` 档位语义并给迁移说明；示例/API 表同步 |
| P3 | API 表支持矩阵在“同页多个组件全部支持”时会重复列出同一组公共参数，可做页级合并减少视觉重复 | `ShowcaseApiTable.razor`、`ShowcaseApiReference.generated.cs` | 一致时合并为页级公共参数，不一致时仍按组件拆分 |
| P3 | `app.css` 仍有 Blazor/Bootstrap 模板残留（`.valid.modified`/`.invalid`/`.form-floating`/`--bs-*`）与全局 `h1:focus{outline:none}`；断点注释仍引用不存在的 token；`gray`/`neutral` 两套灰阶未收敛 | `src/AeterniUI.Sample/wwwroot/css/app.css`、`DateRangePicker.razor.css`、`aeterni_ui.css` | 清理死规则、统一断点/灰阶；焦点规则只保留 FocusOnNavigate 专用类 |

## 已归档轮次

第十轮全库组件审核（REV-133～REV-163，31 条）随 `10.31.1` 归档，第十一轮全库审阅（REV-164～REV-177，14 条）随 `10.34.4` 归档；逐项证据、批次划分与验证结果见 [`release-history.zh-CN.md`](release-history.zh-CN.md)，本文件不保留已收口条目，也不复述其结论。
