# STS2-RitsuLib

English README: [README.md](README.md)

面向 Slay the Spire 2 Mod 的个人共享框架库。

本库主要是为了自己使用而开发，因此开发进度比较随缘，功能以个人需求为驱动。

因为不喜欢 [BaseLib](https://github.com/Alchyr/BaseLib-StS2) 的设计以及编码习惯，因此自己实现了该库。
目前与 BaseLib 没有冲突。

文档入口: [Docs/README.md](Docs/README.md)

## Mod 设置 API

RitsuLib 现在内置了一套专门的 Mod 设置 API，以及对应的设置 submenu。

- 通过 `RitsuLibFramework.RegisterModSettings(...)` 显式注册设置页
- UI 绑定直接复用 `ModDataStore`，不再额外造一套配置后端
- 文本既可以接 `I18N`，也可以接游戏原生 `LocString`
- 菜单层负责显式定义玩家设置，不再把更宽泛的持久化模型直接等同于设置界面

说明文档: [Docs/zh/ModSettings.md](Docs/zh/ModSettings.md)

## Debug 兼容模式

总开关 `debug_compatibility_mode` 默认**关闭**：此时 RitsuLib **不会**软化 `LocTable` 缺键、**不会**跳过无效 Epoch 授予（将走原版或抛出显式异常）、**不会**注入 `THE_ARCHITECT` 对话兜底。

总开关**开启**后，游戏内设置页会展开**子开关**（默认均为**开启**，以兼容过去“只开一个总开关”的体验）：

| 子项 | 开启时 |
|---|---|
| LocTable 缺键 | 占位 + 一次性 `[Localization][DebugCompat]` 警告 |
| 无效解锁 Epoch | 跳过授予 + 一次性 `[Unlocks][DebugCompat]` 警告 |
| 建筑师缺对话 | 对 `ModContentRegistry` 角色注入空 Lines 占位 |

总开关保持开启但**关闭某个子项**时，仅该子系统恢复**原版式**行为。

Windows 下 settings 文件路径:

%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json

## 许可证

MIT
