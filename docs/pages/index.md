---
title:
  en: RitsuLib
  zh-CN: RitsuLib

features:
  title:
    en: Mod authoring for Slay the Spire 2
    zh-CN: 《杀戮尖塔 2》Mod 编写工具集
  subtitle:
    en: Registry-first content, lifecycle hooks, persistence, settings UI, localization, audio, and UI helpers.
    zh-CN: 以注册器为核心，覆盖内容、生命周期、持久化、设置界面、本地化、音频与 UI 扩展。
  text:
    en: >-
      RitsuLib keeps common mod tasks explicit: register the content you own, give it stable ids, bind player-facing
      settings to persistent data, and subscribe to lifecycle events instead of scattering compatibility patches through
      every mod.
    zh-CN: >-
      RitsuLib 让常见 Mod 流程保持清晰：注册自己拥有的内容、生成稳定 ID、把玩家设置直接绑定到持久化数据，
      并通过生命周期事件复用游戏时机，而不是在每个 Mod 里重复写兼容补丁。

  cards:
    - title:
        en: Start building
        zh-CN: 开始编写
      details:
        en: Add the NuGet package, declare the runtime dependency, and create your first content pack.
        zh-CN: 添加 NuGet 包，声明运行时依赖，并创建第一个内容包。
    - title:
        en: Register content
        zh-CN: 注册内容
      details:
        en: Cards, relics, potions, characters, events, epochs, keywords, card tags, and custom piles share one flow.
        zh-CN: 卡牌、遗物、药水、角色、事件、Epoch、关键词、卡牌标签与自定义卡堆共享同一套注册习惯。
    - title:
        en: Keep state safely
        zh-CN: 安全保存状态
      details:
        en: Use scoped JSON stores, migrations, profile switching support, and settings bindings.
        zh-CN: 使用带作用域的 JSON 存储、迁移、档位切换支持与设置绑定。
    - title:
        en: Work with the game
        zh-CN: 连接游戏流程
      details:
        en: Lifecycle events, Harmony patch helpers, Godot script registration, and compatibility notes.
        zh-CN: 生命周期事件、Harmony 补丁辅助、Godot 脚本注册与兼容注意事项。
    - title:
        en: Add polish
        zh-CN: 增加表现层能力
      details:
        en: FMOD helpers, top-bar buttons, card piles, toast messages, runtime hotkeys, and shell themes.
        zh-CN: FMOD 辅助、顶栏按钮、卡堆、Toast、运行时快捷键与 Shell 主题。
---
