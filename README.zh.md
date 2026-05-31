# STS2-RitsuLib

English README: [README.md](README.md)

面向《杀戮尖塔 2》Mod 作者的共享框架库。

RitsuLib 提供内容注册、模型身份、生命周期、持久化、设置界面、本地化、音频、UI 扩展与兼容辅助 API。它不替代游戏原生 API，也不要求放弃 [BaseLib](https://github.com/Alchyr/BaseLib-StS2)；它更像一层为常见 Mod 编写流程准备好的工具集。

文档站：https://sts2-ritsulib.ritsukage.com/

中文《杀戮尖塔 2》模组制作教程：
https://tutorials.sts2modding.com/（[GitHub](https://github.com/GlitchedReme/SlayTheSpire2ModdingTutorials)）

## 相关库

如果 Mod 需要随从、召唤物、组件卡牌、守护等机制，推荐优先使用
[MinionLib](https://github.com/FuYnAloft/MinionLib)。它专门处理随从创建与召唤、随从主动行动、随从相关卡牌交互、守护机制、自定义目标和随从位置系统。RitsuLib 保持为通用框架层，不试图替代这类专用库。

## 安装

在 Mod 项目中引用 NuGet 包：

```xml
<PackageReference Include="STS2.RitsuLib" />
```

然后在 `mod_manifest.json` 声明运行时依赖。游戏 API 0.105.x 及之后使用对象写法：

```json
{
  "dependencies": [
    { "id": "STS2-RitsuLib" }
  ]
}
```

旧游戏 API 分支使用旧的字符串写法；旧版 manifest 解析器可能无法解析 dependency 对象，甚至直接报错：

```json
{
  "dependencies": [
    "STS2-RitsuLib"
  ]
}
```

如果项目没有使用 Central Package Management，请让包管理器或 IDE 选择当前兼容的包版本，不要从 README 复制固定版本号。旧游戏 API 分支使用对应的
`STS2.RitsuLib.Compat.<api-version>` 包。

## 运行时包选择

Mod 开发时，项目里只需要引用一个 NuGet 包：

- `STS2.RitsuLib`：用于当前支持的游戏 API 分支。
- `STS2.RitsuLib.Compat.<api-version>`：用于明确面向旧版《杀戮尖塔 2》API 分支的 Mod。

给玩家安装时，[GitHub Release](https://github.com/BAKAOLC/STS2-RitsuLib/releases) 可能还会提供
`STS2-RitsuLib.<version>.variant-pack.zip`。如果希望只安装一个 `mods/STS2-RitsuLib/` 文件夹，并让它按当前运行的游戏选择对应 RitsuLib 构建，就使用这个资产，而不是各 compat 分支的
`*.github.zip`。根目录的 `STS2-RitsuLib.dll` 是加载器，真正按 API 区分的构建在 `lib/<api-version>/` 下。

下游 Mod 仍按 Mod id 声明运行时依赖。具体 manifest 格式要匹配目标游戏 API 分支。

0.105.x 及之后：

```json
{
  "dependencies": [
    { "id": "STS2-RitsuLib" }
  ]
}
```

旧分支：

```json
{
  "dependencies": [
    "STS2-RitsuLib"
  ]
}
```

变体包不会改变你的编译期 NuGet 引用；它只影响玩家安装运行时 RitsuLib Mod 的方式。

## 常用入口

- `RitsuLibFramework.CreateContentPack(modId)`：注册内容、关键词、时间线、卡堆和顶栏按钮。
- `RitsuLibFramework.CreatePatcher(modId, patcherName)`：创建带诊断日志的 Harmony patcher。
- `RitsuLibFramework.SubscribeLifecycle<TEvent>(...)`：订阅框架和游戏生命周期事件。
- `RitsuLibFramework.GetDataStore(modId)` 配合 `BeginModDataRegistration(modId)`：注册 JSON 持久化数据。
- `RitsuLibFramework.RegisterModSettings(modId, configure)`：注册玩家可编辑的设置页面。

建议从快速入门开始，再按正在编写的功能阅读对应专题。

## 可选分析器

旧的配套分析器
[STS2-ModAnalyzers-RitsuLib](https://github.com/BAKAOLC/STS2-ModAnalyzers-RitsuLib)
（包名：`STS2.ModAnalyzers.RitsuLib`）已经归档，不再维护。

RitsuLib 风格项目的推荐可选分析器是
[STS2RitsuLibModAnalyzers](https://github.com/alkaid616/STS2RitsuLibModAnalyzers)
（包名：`Nothing.STS2RitsuLib.ModAnalyzers`）。它提供 RitsuLib 本地化与资源路径相关的 Roslyn 诊断，并且包内
`buildTransitive` 会自动把常见项目文件传给 analyzer。该分析器由第三方提供、维护和支持；RitsuLib 不保证它与
当前 RitsuLib 能力完全对齐，也不保证所有分析器行为都正确。

## 致谢

感谢在开发过程中帮助 RitsuLib 的人们，以及所有使用者。完整名单见 [ACKNOWLEDGEMENTS.md](ACKNOWLEDGEMENTS.md)。

## 许可证

MIT
