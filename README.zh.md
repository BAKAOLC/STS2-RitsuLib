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

RitsuLib 提供了一个用于调试阶段的运行时兼容模式，用于处理若干可降级的兼容错误。

- 配置项: debug_compatibility_mode
- 默认值: 关闭（false）
- 开启后行为:
  - `LocTable` 缺失键不再直接抛异常，而是回退为 Key 占位符并输出警告日志
  - RitsuLib 解锁兼容桥遇到缺失 Epoch id 时，会降级为警告并跳过该次解锁，使当前跑局继续执行

Windows 下 settings 文件路径:

%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json

## 许可证

MIT
