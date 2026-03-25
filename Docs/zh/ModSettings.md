# Mod 设置界面

RitsuLib 提供了一套面向玩家的设置 API，用来把“玩家应该在游戏里修改的配置”从 `ModDataStore` 持久化层中清晰地抽出来。

这套系统适合用来：

- 暴露一小部分真正面向玩家的配置
- 按 page / section / subpage 组织设置内容
- 统一处理标签、描述与本地化文本
- 支持结构化列表、嵌套列表与可排序编辑器

它不是一个“自动把数据模型生成 UI”的系统。
每个设置项都需要显式注册，这是有意设计。

---

## 心智模型

建议始终把这几层职责分开：

- `ModDataStore`：持久化、作用域、迁移、默认值
- `IModSettingsValueBinding<T>`：UI 与数据之间的读写桥接
- 设置页 builder：页面结构与玩家看到的组织方式
- `ModSettingsText`：标签与描述的文本来源抽象

这样可以避免把内部状态、缓存、运行时镜像和玩家配置混成一团。

---

## 主要 API

| API | 作用 |
|---|---|
| `RitsuLibFramework.RegisterModSettings(...)` | 注册一个设置页 |
| `ModSettingsBindings.Global(...)` / `Profile(...)` | 将字段绑定到持久化数据 |
| `ModSettingsBindings.InMemory(...)` | 创建仅预览或临时 binding |
| `ModSettingsText.Literal(...)` | 纯文本 |
| `ModSettingsText.LocString(...)` | 游戏原生本地化文本 |
| `ModSettingsText.I18N(...)` | 基于 `I18N` 的辅助文本 |
| `WithModDisplayName(...)` | 覆盖左侧 sidebar 中显示的 Mod 名称 |
| `AddToggle(...)`、`AddSlider(...)`、`AddIntSlider(...)`、`AddChoice(...)`、`AddEnumChoice(...)` | 标准值编辑项 |
| `AddButton(...)`、`AddHeader(...)`、`AddParagraph(...)` | 动作项与说明结构 |
| `AddSubpage(...)` | 进入子页面 |
| `AddList(...)` | 结构化、可排序、可嵌套的列表编辑器 |

---

## 推荐流程

1. 先在 `ModDataStore` 注册完整持久化模型
2. 只为真正要暴露给玩家的字段建立 binding
3. 用 page / section / entry 注册设置页
4. 为所有可见标签、描述和选项名称补全本地化

这样做可以明确控制“哪些值能改”和“这些值该怎么呈现”。

---

## 最小示例

先注册持久化数据：

```csharp
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

public sealed class MyModSettings
{
    public bool EnableFancyVfx { get; set; } = true;
    public float ScreenShakeScale { get; set; } = 1.0f;
    public MyDifficultyMode DifficultyMode { get; set; } = MyDifficultyMode.Normal;
}

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");

    store.Register<MyModSettings>(
        key: "settings",
        fileName: "settings.json",
        scope: SaveScope.Global,
        defaultFactory: () => new MyModSettings(),
        autoCreateIfMissing: true);
}
```

然后创建 binding 并注册设置页：

```csharp
using STS2RitsuLib.Settings;

var settingsLoc = RitsuLibFramework.CreateModLocalization(
    modId: "MyMod",
    instanceName: "MyMod-Settings",
    resourceFolders: ["MyMod.Localization.Settings"]);

var fancyVfx = ModSettingsBindings.Global<MyModSettings, bool>(
    "MyMod",
    "settings",
    model => model.EnableFancyVfx,
    (model, value) => model.EnableFancyVfx = value);

var shakeScale = ModSettingsBindings.Global<MyModSettings, float>(
    "MyMod",
    "settings",
    model => model.ScreenShakeScale,
    (model, value) => model.ScreenShakeScale = value);

var difficulty = ModSettingsBindings.Global<MyModSettings, MyDifficultyMode>(
    "MyMod",
    "settings",
    model => model.DifficultyMode,
    (model, value) => model.DifficultyMode = value);

RitsuLibFramework.RegisterModSettings("MyMod", page => page
    .WithModDisplayName(ModSettingsText.I18N(settingsLoc, "mod.display_name", "My Fancy Mod"))
    .WithTitle(ModSettingsText.I18N(settingsLoc, "page.title", "Settings"))
    .WithDescription(ModSettingsText.I18N(settingsLoc, "page.description", "Player-facing options for this mod."))
    .AddSection("general", section => section
        .WithTitle(ModSettingsText.I18N(settingsLoc, "general.title", "General"))
        .AddToggle(
            "fancy_vfx",
            ModSettingsText.I18N(settingsLoc, "fancy_vfx.label", "Fancy VFX"),
            fancyVfx,
            ModSettingsText.I18N(settingsLoc, "fancy_vfx.desc", "Enable additional visual polish."))
        .AddSlider(
            "screen_shake_scale",
            ModSettingsText.I18N(settingsLoc, "screen_shake.label", "Screen Shake Scale"),
            shakeScale,
            minValue: 0.0f,
            maxValue: 2.0f,
            step: 0.05f,
            valueFormatter: value => $"{value:0.00}x")
        .AddEnumChoice(
            "difficulty_mode",
            ModSettingsText.I18N(settingsLoc, "difficulty.label", "Difficulty"),
            difficulty,
            value => ModSettingsText.I18N(settingsLoc, $"difficulty.{value}", value.ToString()))));
```

`WithModDisplayName(...)` 控制左侧导航里 mod 分组显示的名称。
如果不设置，RitsuLib 会依次回退到 manifest 名称，再回退到 mod id。

---

## 文本来源

`ModSettingsText` 的意义，是让页面定义本身不依赖具体文本加载方式。

- `Literal(...)`：简单硬编码文本或快速原型
- `I18N(...)`：Mod 自己维护的设置说明文本
- `LocString(...)`：已经属于游戏原生本地化管线的文本

推荐分工：

- 游戏内容和表格名称 -> `LocString`
- 设置页专用的标签与描述 -> `I18N`

---

## 当前支持的设置项类型

- `AddToggle(...)`：`bool`
- `AddSlider(...)`：`float`
- `AddIntSlider(...)`：`int`
- `AddChoice(...)`：任意类型的候选列表
- `AddEnumChoice(...)`：面向 enum 的便捷封装
- `AddButton(...)`：Reset / Sync / Import / Export 等动作按钮
- `AddSubpage(...)`：进入子页面
- `AddList(...)`：可排序结构化集合
- `AddHeader(...)` / `AddParagraph(...)`：说明与结构辅助项

---

## 结构化列表

`AddList(...)` 是结构化列表编辑器的框架入口。

它支持：

- 新增 / 删除 / 排序
- 嵌套列表编辑
- item 级结构化复制 / 粘贴 / 创建副本
- 通过 `ModSettingsListItemContext<TItem>` 自定义 item 编辑器

典型场景：

- 权重池
- 有序规则链
- 多条目配置块
- 嵌套预设或标签集合

如果 item 是结构化类型，建议提供 item adapter，保证复制粘贴和副本操作能正确克隆与序列化。

---

## 页面结构建议

当前 UI 结构是：

- mod 分组
- page
- section
- entry

对大多数 Mod 来说，一个 root page 配多个 section 就足够清晰。
只有在内容确实属于不同主题时，才建议继续拆 page。

适合使用的场景：

- 多个 page：大型功能区分离
- `AddSubpage(...)`：钻取式设置流程
- 可折叠 section：低频选项收纳
- 列表：编辑集合而不是单个值

---

## 作用域建议

binding 会继承底层持久化值的作用域。

- `SaveScope.Global`：所有档位共享
- `SaveScope.Profile`：按玩家档位区分

常见例子：

- `Global`：画面、辅助功能、调试开关、机器级默认项
- `Profile`：按档位变化的玩法偏好或流程相关设置

---

## 哪些内容适合暴露给玩家

适合进入设置界面的：

- 功能开关
- 外观与表现偏好
- 辅助功能调整项
- 玩家本来就应该能调的玩法参数

不适合直接进入设置界面的：

- 缓存
- 迁移和 schema 元数据
- 运行时镜像状态
- 纯内部实现字段

推荐模式始终是：先完整持久化，再有选择地暴露真正面向玩家的那部分。

---

## 内置参考

RitsuLib 自己注册了一页参考设置，作为实际行为示例。

当前内置参考会演示：

- 会持久化的前置库选项
- 仅预览的临时控件
- 可折叠 section
- 嵌套结构化列表编辑
- copy / paste / duplicate 的 item 工作流

如果你在设计自己的设置页，建议直接把它当成行为参考来对照。

---

## 相关文档

- [持久化设计](PersistenceGuide.md)
- [本地化与关键词](LocalizationAndKeywords.md)
- [生命周期事件](LifecycleEvents.md)
