# STS2-RitsuLib

English README: [README.md](README.md)

面向 Slay the Spire 2 Mod 的共享框架库。

RitsuLib 按实际需求演进，主要服务于仓库内 Mod 的内容编写、注册、持久化与设置界面。

该库可与 [BaseLib](https://github.com/Alchyr/BaseLib-StS2) 并存，当前不存在已知冲突。

文档入口: [Docs/README.md](Docs/README.md)

## 可选：配套分析器

面向 RitsuLib 脚手架与本地化的 Roslyn 分析器仓库：
[STS2-ModAnalyzers-RitsuLib](https://github.com/BAKAOLC/STS2-ModAnalyzers-RitsuLib)（NuGet 包名：`STS2.ModAnalyzers.RitsuLib`）。

## Mod 设置

RitsuLib 提供一套用于玩家可编辑配置的设置 UI。

- 通过 `RitsuLibFramework.RegisterModSettings(...)` 显式注册设置页
- 控件绑定直接复用 `ModDataStore`
- 标签与描述可来自 `I18N` 或游戏原生 `LocString`
- 设置页注册与 BaseLib 的配置页注册、文件路径彼此独立

说明文档: [Docs/zh/ModSettings.md](Docs/zh/ModSettings.md)

## Debug 兼容模式

`debug_compatibility_mode` 默认**关闭**。关闭时，相关补丁保持原版行为。

总开关**开启**后，设置页会显示按功能拆分的兼容回退项，且子开关默认**开启**。

| 子项 | 开启时 |
|---|---|
| LocTable 缺键 | 解析为占位 `LocString`，并为每个键记录一次 `[Localization][DebugCompat]` 警告 |
| 无效解锁 Epoch | 跳过无效的 Epoch 授予，并按稳定键记录一次 `[Unlocks][DebugCompat]` 警告 |
| 建筑师缺对话 | 当原版未提供对话时，为 `ModContentRegistry` 角色注入空 `Lines` |

关闭某一子项时，仅移除该项回退逻辑。

Windows 下设置文件路径:

`%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json`

## 许可证

MIT
