# AeterniUI Agent/Contributor 开发入口

> 本文档只记录任务开始前的阅读顺序、开发流程、文件定位和提交前检查。项目硬性约束以根目录 `AGENTS.md` 为准。
>
> 供**任何开发代理（agent）与人类协作者**使用。仓库一致性不依赖自觉，而是依赖
> “开工先读这几份文档 + 提交前跑门禁 + CI 兜底”这三层。

## 1. 开工前必读（按此顺序，缺一不可）

1. `AGENTS.md` —— 项目硬性约束（宿主、架构、验证、修改纪律）。
2. `docs/component-design-guidelines.zh-CN.md` —— 组件契约：参数命名、`BuildClass`/`BuildAttributes`、Token、ARIA/键盘、JS isolation、提交检查清单。
3. `docs/component-roadmap.zh-CN.md` —— 当前任务、依赖、验收，以及“每项完成的固定交付物”。
4. `docs/current-features.zh-CN.md` —— 公共表面事实源，先确认目标能力是否已存在，避免重复造轮子。

涉及项目结构、入口、宿主、构建命令或模块定位时，额外阅读 `docs/project-index.zh-CN.md`；它只负责导航，不替代组件契约、功能清单或路线图。

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
- [ ] 变更了 `.css` 时 `node scripts/check-css-comments.mjs` 通过（CI 也会做）。
- [ ] 没有向 `aeterni_ui.css` 添加组件选择器。
- [ ] 示例页没有新的 `::deep` 或全局组件样式覆盖（除非页面自身布局必需）。
- [ ] 没有新增“第二个尺寸/颜色/圆角体系”。
- [ ] 公共 API/行为变更已同步 `current-features`，示例可操作演示，roadmap 勾选/记录同步。
- [ ] 未提交 `bin/ obj/ target/ dist/ .sample-publish/` 与 IDE/OS 文件。

## 4. 版本与变更记录

组件库版本采用 .NET SDK 版本属性统一管理，来源为根目录 `Directory.Build.props`。

- 功能新增：递增次版本号，例如 `10.0.0` → `10.1.0`。
- 问题修复：递增补丁版本号，例如 `10.0.0` → `10.0.1`。
- 破坏性公共 API 变更：递增主版本号，例如 `10.0.0` → `11.0.0`。
- 文档、重构或内部实现变更：根据是否影响公共行为决定是否递增版本。
- 提交功能或修复时，在提交说明或发布说明中标记版本影响；不要在各个项目文件中分别维护版本。

## 5. 常用命令

```bash
dotnet build aeterni_ui.slnx                       # 静态检查主门禁
bash scripts/check-docs.sh                         # 文档一致性门禁（CI 同款）
./scripts/sample-publish.sh Debug                  # 重新生成 dist/（Tauri/静态预览用）
cargo check --manifest-path src-tauri/Cargo.toml   # Rust 宿主编译检查
node --check <file>.razor.js                       # 组件 JS 语法检查
node scripts/check-css-comments.mjs                # CSS 注释提前闭合检查（CI 同款）
node scripts/generate-fontawesome-icons.mjs --check # 图标清单与生成文件是否一致
```

## 6. 新增组件文档清单

新增或修改公共组件时，按变更范围同步：

- 组件源码、样式和示例页。
- `docs/current-features.zh-CN.md`：新增或修改公共 API / 行为时必须更新。
- `docs/component-roadmap.zh-CN.md`：完成路线图任务或改变规划时更新。
- `docs/project-index.zh-CN.md`：仅在新增目录、服务、入口、宿主或命令时更新。
- `README.md`：仅在安装、启动或对外入口变化时更新。
- 版本：功能新增递增次版本，问题修复递增补丁版本；破坏性 API 递增主版本；内部文档或重构按是否影响公共行为决定。

## 7. CI 兜底（.github/workflows/build.yml）

推送/PR 时会自动执行：

- `dotnet build aeterni_ui.slnx`
- 对所有 `.razor.js` 做 `node --check`
- `node scripts/check-css-comments.mjs`（CSS 注释提前闭合检查）
- 校验 `aeterni_ui.css` 不含组件选择器（仅允许 Token 级 `.aeterni-dark` 别名）

如果本地通过但 CI 失败，先看 CI 日志；通常原因是文件/换行/编码差异或忘了同步提交新文件。
