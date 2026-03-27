# 诊断与兼容层

本文说明 RitsuLib 在游戏原版之外补充的安全与兼容机制。

重点包括：

- 帮助作者尽早发现错误的一次性警告
- 面向调试的缺失本地化与缺失 Epoch 兼容行为
- 原版系统不支持 Mod 内容时的窄桥接补丁

---

## 设计意图

RitsuLib 不会把所有引擎问题藏进隐式魔法。它遵循以下规则：

- 能尽早暴露真实错误，就尽早暴露
- 原版没有安全扩展点时，框架可以补桥
- 某个 shim 会隐藏过多行为时，宁可保持显式

这层能力是刻意收敛的，只处理边缘问题。

---

## 一次性警告策略

RitsuLib 的部分诊断只会对同一个问题警告一次，包括：

- 缺失资源路径
- 调试兼容模式下缺失本地化键

目的是让日志"可行动"：足够明显，又不会每帧刷屏。

---

## 资源路径诊断

显式资源覆写路径由 `AssetPathDiagnostics` 校验。

当资源路径不存在时：

- 输出一次警告（包含宿主类型、模型标识、配置成员名和缺失路径）
- 回退到原始资源路径或原始行为

这对角色资源尤其重要，因为游戏原版对缺失角色资源几乎没有安全兜底。

详见 [资源配置与回退规则](AssetProfilesAndFallbacks.md)。

---

## Debug 兼容模式

> 此功能通过补丁修改游戏原版 `LocTable` 和 RitsuLib 解锁桥的行为，仅用于调试。

当开启 `debug_compatibility_mode` 后：

### 本地化降级（修改游戏原版行为）

- `LocTable.GetLocString(key)` 缺失时返回占位 `LocString`，不再抛异常
- `LocTable.GetRawText(key)` 缺失时返回键本身，不再抛异常
- 同一个缺失键只会警告一次

### Epoch 降级（RitsuLib 兼容桥）

- RitsuLib 的解锁兼容桥在运行时遇到缺失的 Epoch ID 时，会输出一次警告、跳过该次解锁，允许当前跑局继续
- 覆盖范围仅限 RitsuLib 自身接管的兼容桥路径，例如：
  - Mod 角色沿用原版 `ObtainCharUnlockEpoch(...)` 时推导出的 Epoch
  - RitsuLib 注册的 Boss / Elite / Ascension / Post-run Epoch 解锁规则

该模式默认关闭。它的目的是降低调试阶段的中断成本，不是替代正确的本地化内容或时间线注册。

Windows 下设置文件路径：

```text
%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json
```

---

## 注册冲突诊断

RitsuLib 会显式检查以下冲突：

| 冲突类型 | 常见触发场景 |
|---|---|
| 模型 ID 冲突 | 同 Mod / 同类别下两个已注册模型的 CLR 类型名相同 |
| 纪元 ID 冲突 | 两个纪元解析出同一个 `Id` |
| 故事 ID 冲突 | 两个故事解析出同一个故事标识 |

检测到冲突时抛异常或输出错误日志，不会静默接受模糊身份。

---

## Ancient 对话兼容层

> 此功能在游戏原版 `AncientDialogueSet.PopulateLocKeys` 之前注入，扩展原版行为。

RitsuLib 会自动为已注册 Mod 角色追加基于本地化定义的 Ancient 对话。

定位是"兼容便利层"：

- 对话 key 仍由作者编写
- 框架只负责发现并追加，使 Mod 角色也能走与原版角色同一套 Ancient 对话模式

具体 key 结构见 [本地化与关键词](LocalizationAndKeywords.md)。

---

## 解锁兼容桥

> 以下解释原版进度系统对 Mod 角色的局限性与 RitsuLib 的桥接策略。

原版的若干进度检查按原版角色设计。RitsuLib 通过窄桥接补丁让注册的解锁规则在这些节点上对 Mod 角色生效：

| 桥接类型 | 说明 |
|---|---|
| 精英胜场 | 精英击杀计数的纪元判定桥接 |
| Boss 胜场 | Boss 击杀计数的纪元判定桥接 |
| 进阶 1 | 进阶 1 的纪元判定桥接 |
| 局后角色解锁 | 局后角色解锁纪元桥接 |
| 进阶显示 | 进阶显示解锁判定桥接 |

这些补丁不发明第二套进度系统，只是把 RitsuLib 注册的规则转发到原版会忽略 Mod 角色的检查点上。

详见 [时间线与解锁](TimelineAndUnlocks.md)。

---

## Freeze 异常

当内容、时间线或解锁在冻结之后还被注册时，RitsuLib 会直接抛异常。

这是诊断机制：一旦晚注册，往往意味着 ModelDb 缓存已建立、固定身份规则已被使用、解锁过滤已在运行。此时最安全的做法是尽早失败。

---

## 推荐排查思路

1. 先把警告当作配置问题，而不是随机引擎不稳定
2. 缺失资源和缺失本地化优先从源头修复
3. 调试兼容模式只在调试阶段使用
4. 如果已有干净的显式 API，不要反过来依赖兼容层

框架是在帮你"看见问题"，而不是帮你把问题永久藏起来。

---

## 相关文档

- [资源配置与回退规则](AssetProfilesAndFallbacks.md)
- [本地化与关键词](LocalizationAndKeywords.md)
- [时间线与解锁](TimelineAndUnlocks.md)
- [Godot 场景编写说明](GodotSceneAuthoring.md)
- [框架设计](FrameworkDesign.md)
