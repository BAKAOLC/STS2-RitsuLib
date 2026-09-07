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
- `Sts2ApiSignatureRoot`（可选）：版本化 API signature 的根目录。每个 `<root>/<Sts2ApiCompat>/` 目录需包含 `sts2.dll`、`0Harmony.dll` 和 `SmartFormat.dll`；不设置此属性时引用已安装游戏的程序集。

先关闭游戏，然后在仓库根目录执行：

```powershell
dotnet build STS2-RitsuLib.sln
```

默认构建会生成 manifest 和构建查看器，并把 RitsuLib 安装到 `<Sts2Dir>/mods/STS2-RitsuLib/`。如果那里已装过变体包，会被本地单 API 构建替换。RitsuLib 是 DLL-only Mod，无需导出 PCK。

## 代码与 API 设计

遵守 [.editorconfig](.editorconfig)，与相邻代码保持风格一致。标识符和实现注释用英文；需要翻译的界面文本放到本地化系统里。游戏行为通过 Mod 自己的代码、补丁和资源实现；游戏文件与恢复源码仅作只读参考。

public 和 protected API 是长期的兼容承诺：新增 API 要对应使用方 Mod 的实际需求，且不能破坏现有源码与二进制兼容性。提供中英文 XML 文档，说明用法、输入、输出，以及生命周期和所有权方面的要求。

对输入做校验，失败时的行为要写清楚。共享 API 要考虑多 Mod 并存、重复调用和资源生命周期；注册冲突如何处理、回调按什么顺序触发，都要明确说明。

## 验证

改过的 C# 文件用仓库配置的 ReSharper 格式化，并把检查报出的问题全部解决。确认构建通过，在游戏里实际验证受影响的行为，并检查日志。修改公共 API 时，还要构建并测试使用方 Mod；Bug 修复应覆盖原始复现步骤和相关的边界情况。

改动涉及游戏 API 兼容性或打包/manifest 生成时，把 [STS2-RitsuLib.csproj](STS2-RitsuLib.csproj) 中 `RitsuLibCompatTargets` 声明的全部目标逐个构建一遍：用 `/p:Sts2ApiCompat=<version>` 选择目标，准备匹配的引用程序集。只有最新 API 目标会安装到游戏目录。

改文档要检查链接、示例，以及中英文是否一致。改文档站时，工具版本以 [docs/package.json](docs/package.json) 和 [文档工作流](.github/workflows/gh-pages.yml) 中的为准，然后在 `docs/` 目录执行：

```powershell
pnpm install --frozen-lockfile
pnpm build
```

预览改过的页面，检查排版和导航。

## Pull Request

按 [PR 模板](.github/PULL_REQUEST_TEMPLATE.md) 说明解决了什么问题、修改后的行为和验证结果；对评审有帮助的示例或截图也一并附上。写明在哪些游戏/API 版本上测试过，没完成的检查如实说明。

保持改动聚焦，中英文文档同步更新。本地配置、游戏二进制文件和生成产物不要提交进仓库。版本与发布元数据的调整请先与维护者协调。
