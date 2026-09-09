# 参与 RitsuLib 开发

[English](CONTRIBUTING.md)

欢迎提 issue、修 bug、贡献可复用的 API、文档和翻译。每次贡献聚焦于一个明确的问题或使用场景。

## 问题反馈与功能建议

先搜索已有的 issue，再按仓库的 issue 模板提交。Bug 报告请带上：游戏和 RitsuLib 的版本、相关 Mod、复现步骤、预期行为、日志。运行时日志在游戏用户数据目录下的 `logs/godot.log`：

- Windows：`%AppData%\SlayTheSpire2\logs\godot.log`
- macOS：`~/Library/Application Support/SlayTheSpire2/logs/godot.log`
- Linux：`~/.local/share/SlayTheSpire2/logs/godot.log`

分享前把敏感信息删掉。

新增 API 或做较大调整时，先说明使用方 Mod 的实际需求，并与维护者讨论预期行为。

## 开发环境

需要准备：

- 支持 `net9.0` 的 .NET SDK；
- Node.js 和 npm（内置日志查看器要用）；
- 与目标 API 匹配的游戏程序集。

用 Rider（或带 ReSharper 的 Visual Studio）打开 `STS2-RitsuLib.sln`，即可使用仓库配置的格式化与检查。

项目会自动定位本地游戏安装。需要手动指定路径时，参照 [local.props.template](local.props.template) 创建 `local.props`：

- `Sts2Dir`：本地测试用的游戏安装目录。
- `Sts2ApiSignatureRoot`：版本化 API signature 的根目录。所选目标和公共模块基线目标的 `<root>/<api-version>/` 目录均需包含 `sts2.dll`、`0Harmony.dll` 和 `SmartFormat.dll`。可复现的兼容构建应使用此配置；不设置时会引用已安装游戏的程序集，不能据此确认兼容旧版 API。

先关闭游戏，然后在仓库根目录执行：

```powershell
dotnet build STS2-RitsuLib.sln
```

默认构建会生成 manifest 和构建查看器，并把 RitsuLib 安装到 `<Sts2Dir>/mods/STS2-RitsuLib/`。如果那里已装过变体包，会被本地单 API 构建替换。RitsuLib 是 DLL-only Mod，无需导出 PCK。

开发使用 Debug，分发使用 Release。解决方案及所有组件统一这两种配置，旧 Godot 导出配置与项目文件已移除。
构建设置和输出路径集中定义于 `build/RitsuLib.Build.props`，GodotSharp 与源码生成器由 `Directory.Build.targets` 显式引用。

```powershell
uv run python scripts/build_cli.py build
uv run python scripts/build_cli.py build --compat-targets all
uv run python scripts/build_cli.py pack --compat-targets 0.109.0
uv run python scripts/build_cli.py bundle
```

`build` 默认 Debug 和最新目标；`pack` 默认 Release 和最新目标；`bundle` 默认 Release 和全部声明目标。
均支持 `--configuration`、`--compat-targets`、`--signature-root` 与 `--game-dir`。build 保留最新目标正常复制到游戏的行为；
pack 和 bundle 只生成分发产物，不安装。兼容目标始终顺序执行。

| 产物 | 位置 |
| --- | --- |
| 兼容入口及完整编译依赖 | `artifacts/bin/<配置>/<API>/` |
| 各组件构建输出 | `artifacts/modules/<配置>/shared/<项目>/` 和 `compat/<API>/` |
| 中间文件 | `artifacts/obj/<项目>/<配置>/<API>/` |
| 完整单目标安装目录 | `artifacts/runtime/<配置>/<API>/` |
| 完整多目标合并目录 | `artifacts/bundle/<配置>/` |
| NuGet 与符号包 | `artifacts/packages/<配置>/nuget/` |
| 单目标与 Bundle ZIP | `artifacts/packages/<配置>/github/` |

编译其他目标不会覆盖已有单目标输出。公共模块固定使用基线 API，合并安装目录仅保留一份。
编译目录中的兼容入口 DLL 与安装根目录中的 loader 各有用途：子 Mod 使用 NuGet 或安装目录中的
`RitsuLib.References.props` 引用；玩家安装完整运行时目录。

## 代码与 API 设计

遵守 [.editorconfig](.editorconfig)，与相邻代码保持风格一致。标识符和实现注释用英文；需要翻译的界面文本放到本地化系统里。游戏行为通过 Mod 自己的代码、补丁和资源实现；游戏文件与恢复源码仅作只读参考。

public 和 protected API 是长期的兼容承诺：新增 API 要对应使用方 Mod 的实际需求，且不能破坏现有源码与二进制兼容性。提供中英文 XML 文档，说明用法、输入、输出，以及生命周期和所有权方面的要求。

对输入做校验，失败时的行为要写清楚。共享 API 要考虑多 Mod 并存、重复调用和资源生命周期；注册冲突如何处理、回调按什么顺序触发，都要明确说明。

## 验证

改过的 C# 文件用仓库配置的 ReSharper 格式化，并把检查报出的问题全部解决。确认构建通过，在游戏里实际验证受影响的行为，并检查日志。修改公共 API 时，还要构建并测试使用方 Mod；Bug 修复应覆盖原始复现步骤和相关的边界情况。

改动涉及游戏 API 兼容性或打包/manifest 生成时，把 [build/RitsuLib.Compatibility.props](build/RitsuLib.Compatibility.props) 中 `RitsuLibCompatTargets` 声明的全部目标逐个构建一遍：用 `/p:Sts2ApiCompat=<version>` 选择目标，准备匹配的引用程序集。只有最新 API 目标会安装到游戏目录。

模块归属集中定义在 [build/RitsuLib.Modules.items](build/RitsuLib.Modules.items)。Shared 提供稳定契约和基础设施；Ui 依赖 Shared；Settings 依赖 Shared 与 Ui；Runtime 提供版本相关的游戏集成。根项目是自动生成类型转发的兼容入口。Shared、Ui、Settings 与加载器固定针对 `RitsuLibSharedApiCompat` 编译，不随 Runtime 的目标变化。变化的游戏 API 调用应通过内部宿主契约留在 Runtime，通用 UI 不应依赖设置注册体系。

普通构建部署完整的单目标模块目录。产物构建与 CI 打包同一目录结构，仅在公共模块逐字节一致时合并多版本，并验证清单、哈希和 XML 文档。可运行 `uv run python scripts/ci/ci_build.py --signature-root <root>` 在本地验证产物流；该命令不安装到实际游戏目录。

改文档要检查链接、示例，以及中英文是否一致。改文档站时，工具版本以 [docs/package.json](docs/package.json) 和 [文档工作流](.github/workflows/gh-pages.yml) 中的为准，然后在 `docs/` 目录执行：

```powershell
pnpm install --frozen-lockfile
pnpm build
```

预览改过的页面，检查排版和导航。

## Pull Request

PR 的合并目标（base 分支）应优先选择 `dev`，除非维护者明确指定其他目标分支。

按 [PR 模板](.github/PULL_REQUEST_TEMPLATE.md) 说明解决了什么问题、修改后的行为和验证结果；对评审有帮助的示例或截图也一并附上。写明在哪些游戏/API 版本上测试过，没完成的检查如实说明。

保持改动聚焦，中英文文档同步更新。本地配置、游戏二进制文件和生成产物不要提交进仓库。版本与发布元数据的调整请先与维护者协调。
