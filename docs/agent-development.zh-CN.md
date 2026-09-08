# AeterniUI Agent/Contributor 开发入口

> 供**任何开发代理（agent）与人类协作者**使用。仓库一致性不依赖自觉，而是依赖
> “开工先读这几份文档 + 提交前跑门禁 + CI 兜底”这三层。

## 1. 开工前必读（按此顺序，缺一不可）

1. `AGENTS.md` —— 项目硬性约束（宿主、架构、验证、修改纪律）。
2. `docs/component-design-guidelines.zh-CN.md` —— 组件契约：参数命名、`BuildClass`/`BuildAttributes`、Token、ARIA/键盘、JS isolation、提交检查清单。
3. `docs/component-roadmap.zh-CN.md` —— 当前任务、依赖、验收，以及“每项完成的固定交付物”。
4. `docs/current-features.zh-CN.md` —— 公共表面事实源，先确认目标能力是否已存在，避免重复造轮子。

## 2. 高频硬规则（速查）

- 所有组件直接或间接继承 `AeterniComponent`；根元素使用 `BuildAttributes()`，class 用 `BuildClass()`。
- **`wwwroot/css/aeterni_ui.css` 只允许 Token/主题变量**，不允许出现组件样式；组件样式放各自的 `.razor.css`（CI 会校验）。
- 示例页不允许通过 `::deep`/全局规则去覆盖组件原生样式；需要全宽等布局请用组件参数（如 `FullWidth`）或页面级容器。
- 主题只能由 `ThemeProvider` + `ThemeService` 驱动；组件不得自己改 `data-theme`。
- 只消费 `--aeterni-*` Token，不新建第二套颜色/间距/圆角体系。
- 修改公共 API 或组件行为后：同步 `docs/current-features.zh-CN.md`，示例页补可交互展示，并按 roadmap 更新 TODO/任务记录。

## 3. 提交前自检清单

- [ ] `dotnet build aeterni_ui.slnx` 通过（0 error；目标 0 warning）。
- [ ] 变更了 `.razor.js` 时 `node --check` 通过（CI 也会做）。
- [ ] 没有向 `aeterni_ui.css` 添加组件选择器。
- [ ] 示例页没有新的 `::deep` 或全局组件样式覆盖（除非页面自身布局必需）。
- [ ] 没有新增“第二个尺寸/颜色/圆角体系”。
- [ ] 公共 API/行为变更已同步 `current-features`，示例可操作演示，roadmap 勾选/记录同步。
- [ ] 未提交 `bin/ obj/ target/ dist/ .sample-publish/` 与 IDE/OS 文件。

## 4. 常用命令

```bash
dotnet build aeterni_ui.slnx                       # 静态检查主门禁
./scripts/sample-publish.sh Debug                  # 重新生成 dist/（Tauri/静态预览用）
cd tauri/src-tauri && cargo check                  # Rust 宿主编译检查
node --check <file>.razor.js                       # 组件 JS 语法检查
```

## 5. CI 兜底（.github/workflows/build.yml）

推送/PR 时会自动执行：

- `dotnet build aeterni_ui.slnx`
- 对所有 `.razor.js` 做 `node --check`
- 校验 `aeterni_ui.css` 不含组件选择器（仅允许 Token 级 `.aeterni-dark` 别名）

如果本地通过但 CI 失败，先看 CI 日志；通常原因是文件/换行/编码差异或忘了同步提交新文件。
