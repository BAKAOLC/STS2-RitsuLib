# Shell 主题（设置 UI 主题系统）

RitsuLib 的设置 UI（`RitsuModSettingsSubmenu`、各类侧栏、面板、列表、控件）使用一套基于
[W3C Design Tokens Format Module](https://www.w3.org/community/design-tokens/) 的主题系统，简称 **DTFM**。

它解决三件事：

1. **结构化色彩 / 字体 / 度量**：所有可视常量都通过 token 命名暴露，避免散落的硬编码值。
2. **可层叠覆盖**：按 `继承链 → scopes → mod 注册的默认值` 顺序合并；同一份 UI 可以被全局主题、Shell 范围、ModSettings 范围、特定 mod 范围分别微调。
3. **可扩展**：mod 既可以通过 `extensions.<modId>` 在主题文件中夹带自定义数据，也可以在运行期向 `RitsuShellThemeRuntime` 注册自己的默认 token，由 RitsuLib 负责合并、引用解析与刷新。

---

## 文件位置

- 内置主题：`sts-2-ritsulib/Ui/Shell/Themes/{default,warm,oled}.theme.json`，作为嵌入资源打包到程序集。
- 用户/Mod 提供的主题：写入 `user://<global save path>/shell_themes/` 目录（运行时由
  `RitsuShellThemePaths.TryEnsureShellThemesDirectory` 创建）；启动时 RitsuLib 会把内置主题
  导出到该目录方便玩家二次编辑。
- 启用的主题 id 持久化在玩家全局存档（`UiShellThemeId` 字段）。
- JSON Schema：`sts-2-ritsulib/schemas/ui/shell/v1/schema.json`，可在 IDE 中通过
  `$schema` 提示进行校验。

---

## 顶层文件结构

```jsonc
{
  "$schema": "https://raw.githubusercontent.com/BAKAOLC/STS2-RitsuLib/main/schemas/ui/shell/v1/schema.json",
  "themeFormatVersion": 1,
  "id": "warm",                // 必填，小写标识符
  "displayName": "Warm",       // 选项中的可读名称
  "inherits": "default",       // 父主题 id；为 null 表示根主题

  "core":       { /* 原语 token */ },
  "semantic":   { /* 语义/别名 token */ },
  "components": { /* 组件 token */ },

  "scopes": {
    "global":      { /* 应用到所有作用域 */ },
    "shell":       { /* 仅 Shell 内部使用 */ },
    "modSettings": { /* 仅 RitsuModSettingsSubmenu 使用 */ },
    "mod:my_mod":  { /* 仅 my_mod 自己消费 */ }
  },

  "extensions": {
    "my_mod": { "anything": { "goes": true } }
  }
}
```

### token 三段式

| 层级         | 角色                                                       | 推荐内容                                       |
| ------------ | ---------------------------------------------------------- | ---------------------------------------------- |
| `core`       | 原语：调色板、尺度、字体家族                               | `core.color.amber.500`、`core.size.2`          |
| `semantic`   | 语义别名：以意图命名，引用 `core` 或直接给值               | `semantic.color.surface.default`               |
| `components` | 组件 token：`组件 → 变体 → 状态`，尽量引用 `semantic`      | `components.button.primary.hover.bg`           |

约定：`core` 不直接被 UI 消费，UI 只消费 `semantic` 与 `components`。当需要扩展时，先在
`core` 加原语，再让上层引用它。

### 叶子节点

DTFM 严格风格：每个叶子 token 都是一个对象，必须有 `$value` 与 `$type`，可选
`$description`、`$extensions`：

```jsonc
{
  "$value": "#F59E0B",
  "$type": "color",
  "$description": "Warm accent base"
}
```

支持的 `$type`：

- `color`：十六进制字符串，可带 alpha（`#RRGGBB` / `#RRGGBBAA`）。
- `dimension`：数值，单位为像素的整数或浮点。
- `fontFamily`：字符串，支持 `res://`、`user://` 与绝对路径。
- `fontWeight`：整数。
- `number`：无单位浮点，例如行高倍率。
- `boolean`：布尔值（用于诸如 `sidebar.showInlinePageCount` 之类的开关 token）。

### 引用语法

`"$value"` 可以写成 `"{a.b.c}"` 引用其它 token；引用允许跨 `core / semantic / components / scopes`，
但禁止循环引用。`RitsuShellThemeReferenceResolver` 会在合并完成后统一解析所有引用。

```jsonc
"semantic": {
  "color": {
    "accent": {
      "default": { "$value": "{core.color.amber.500}", "$type": "color" }
    }
  }
}
```

---

## 合并顺序（继承 + scopes）

构建快照时，`RitsuShellThemeCatalog.TryBuildSnapshot` 按以下顺序写入同一个 token 树：

1. 所有 mod 通过 `RegisterModTokens` 注册的默认 DTFM JSON。
2. 解析继承链 `inherits → ... → 当前主题`，依次合并每份文档的 `core / semantic / components`。
3. 同一份文档内按以下 scope 顺序覆盖：`global → shell → modSettings → mod:<id>`。
4. 合并完成后整体执行引用解析，再交给 `RitsuShellThemeBuilder` 构建不可变的
   `RitsuShellTheme` 快照。

合并规则：

- 组节点是普通 JSON 对象，递归合并键。
- 叶子 token（`$value` / `$type` 形式）视作整体替换，不做字段级合并。
- 数组同样整体替换。

> ⚠️ 当下 RitsuLib 自身只消费 `global` / `shell` / `modSettings` / `mod:<id>` 四个 scope。
> 如果你的 mod 想要自己的 scope 命名，目前推荐使用 `mod:<modId>`，它已经内置在合并管线里。

---

## C# API 概览

### 1. 当前快照与事件

```csharp
using STS2RitsuLib.Ui.Shell.Theme;

var theme = RitsuShellTheme.Current;        // 等价于 RitsuShellThemeRuntime.Current
RitsuShellThemeRuntime.ThemeChanged += () => ApplyMyChrome(RitsuShellTheme.Current);
RitsuShellThemeRuntime.ApplyThemeId("warm");
RitsuShellThemeRuntime.ReapplyActiveTheme(forceReloadCatalog: true); // 重新读盘
```

`RitsuShellTheme` 是不可变快照，每次切主题或注册 mod token 都会构建一个全新的实例。
缓存任何 token 时务必随 `ThemeChanged` 一同刷新。

### 2. 类型化访问（推荐用于 RitsuLib 内置 token）

`RitsuShellTheme` 暴露一组按语义分组的强类型记录：

```csharp
var t = RitsuShellTheme.Current;

// 颜色
t.Color.White
t.Color.Divider
t.Color.Shadow.Ambient

// 文本
t.Text.LabelPrimary
t.Text.HoverHighlight

// 表面
t.Surface.Sidebar
t.Surface.Entry.Bg
t.Surface.Framed.Border

// 组件 & 状态
t.Component.Toggle.On.Bg
t.Component.SidebarBtn.SelectedHover.Border
t.Component.TextButton.Accent.Fg

// 度量
t.Metric.Radius.Default
t.Metric.BorderWidth.Overlay
t.Metric.FontSize.Button
t.Metric.Sidebar.Width
t.Metric.Sidebar.ShowInlinePageCount     // bool

// 字体
t.Font.Body
t.Font.BodyBold
t.Font.Button
```

完整字段定义见
[`Ui/Shell/Theme/Tokens/`](../../Ui/Shell/Theme/Tokens/) 下的 `ColorTokens.cs`、`ComponentTokens.cs`、
`MetricTokens.cs`、`FontTokens.cs`。

### 3. 路径字符串访问（推荐用于 mod 扩展或低频 token）

当 token 来自 mod 自己注册的 DTFM 子树时，没有强类型字段，使用路径字符串读取：

```csharp
var theme = RitsuShellTheme.Current;

Color  bg     = theme.GetColor("components.relicPicker.selected.bg");
float  height = theme.GetDimension("semantic.metrics.relicPicker.rowMinHeight");
int    radius = theme.GetDimensionInt("semantic.metrics.radius.default");
bool   inline = theme.GetBool("semantic.metrics.sidebar.showInlinePageCount");
Font   body   = theme.GetFontFamily("semantic.fontFamily.body");

if (theme.TryGetExtension("my_mod", out var json))
{
    // json 是 System.Text.Json.JsonElement，对应 extensions.my_mod 子树
}
```

读取策略：

- 路径区分大小写，使用合并后的真实键（多段以 `.` 拼接）。
- 缺失 / 类型错误时返回安全默认（`Magenta`、`0`、`false`、共享 fallback 字体）。
- 数值统一通过 `GetDimension*` 系列读取，路径相同。

### 4. mod 注册式 API

`RitsuShellThemeRuntime` 提供注册接口，用来在不修改主题文件的前提下，由 mod 自己提供
默认 token、订阅刷新事件：

```csharp
public static class RitsuShellThemeRuntime
{
    public static void RegisterModTokens(string modId,
        JsonElement? defaults,
        Action<RitsuShellTheme>? onApply = null);

    public static void UnregisterModTokens(string modId);
}
```

- `defaults`：一份 DTFM 风格的 JSON 子树（推荐对象类型，包含 `core / semantic / components / extensions`），会在每次构建快照时**先于**主题文件合并，因此用户主题里写的同名 token 永远胜出。
- `onApply`：每次新快照构建完成后回调一次，参数为最新的 `RitsuShellTheme`。注册时也会触发一次（因为内部会调一次 `ReapplyActiveTheme`）。
- 取消注册会重新构建一次快照，确保该 mod 的默认值消失。

---

## 端到端示例：用 mod 注册式 API 加一个挑选器主题

假设我们有一个 mod `my_mod`，它实现了一个遗物选择器，需要：

- 一组属于自己的 `components.relicPicker.{default,hover,selected}` token；
- 玩家可以通过修改 `default.theme.json` 的 `extensions.my_mod` 调整一个 boolean 开关。

### 第一步：准备默认 DTFM JSON

```csharp
using System.Text.Json;
using STS2RitsuLib.Ui.Shell.Theme;

private const string MyDefaults = """
{
  "core": {
    "color": {
      "myMod": {
        "indigo": {
          "500": { "$value": "#5C5CD6", "$type": "color" }
        }
      }
    }
  },
  "semantic": {
    "color": {
      "myMod": {
        "accent": { "$value": "{core.color.myMod.indigo.500}", "$type": "color" }
      }
    },
    "metrics": {
      "relicPicker": {
        "rowMinHeight": { "$value": 56, "$type": "dimension" }
      }
    }
  },
  "components": {
    "relicPicker": {
      "default":  { "bg": { "$value": "#1B1F2BF5", "$type": "color" } },
      "hover":    { "bg": { "$value": "#27314AFA", "$type": "color" } },
      "selected": { "bg": { "$value": "{semantic.color.myMod.accent}", "$type": "color" } }
    }
  },
  "extensions": {
    "my_mod": {
      "showHotkeyHint": true
    }
  }
}
""";
```

### 第二步：在 mod 启动时注册

```csharp
public sealed class MyModBootstrap : IModInit
{
    public void OnInit()
    {
        using var doc = JsonDocument.Parse(MyDefaults);
        RitsuShellThemeRuntime.RegisterModTokens(
            modId: "my_mod",
            defaults: doc.RootElement.Clone(),
            onApply: ApplyTheme);
    }

    private static void ApplyTheme(RitsuShellTheme theme)
    {
        // 每次主题变更会被回调一次
        var bg = theme.GetColor("components.relicPicker.selected.bg");
        var height = theme.GetDimension("semantic.metrics.relicPicker.rowMinHeight");
        var showHint = theme.TryGetExtension("my_mod", out var ext) &&
                       ext.TryGetProperty("showHotkeyHint", out var p) &&
                       p.ValueKind == JsonValueKind.True;
        // ... 用得到的 token 重新刷新自己的 UI 节点
    }
}
```

### 第三步（可选）：玩家覆盖

玩家在 `user://shell_themes/default.theme.json` 中追加自己的配置即可覆盖你的默认值：

```jsonc
{
  "id": "default",
  "components": {
    "relicPicker": {
      "selected": { "bg": { "$value": "#FF8A00", "$type": "color" } }
    }
  },
  "extensions": {
    "my_mod": { "showHotkeyHint": false }
  }
}
```

由于合并顺序是 `mod 默认 → 主题继承链 → scopes`，玩家的同名 token 总是会覆盖
`RegisterModTokens` 提供的默认值。

---

## 校验与排错

- 主题文件加载失败时，`RitsuShellThemeCatalog` 会跳过该文件并继续使用之前的快照；可以
  通过 `RitsuShellThemeRuntime.ReapplyActiveTheme(forceReloadCatalog: true)` 重新尝试。
- 引用失败、循环引用会被记录为错误（当前在内部列表中），缺失项会回退到安全默认。
- 推荐在主题文件顶部声明 `$schema` 字段，IDE 会按 schema 进行键名补全与类型校验。
- 调试时可读取 `RitsuShellTheme.Current.Id` 确认实际生效的主题 id，或 `ListExtensionModIds()`
  确认哪些 mod 的扩展已经合并进当前快照。

---

## 风格建议

- **优先扩展 `semantic`，再让 `components` 引用之**：让色彩调整在一个地方完成。
- **新增组件 token 时一并记录默认值**：在 mod 中通过 `RegisterModTokens` 提供，玩家无需修改
  主题文件即可看到合理的初始外观。
- **不要在运行时修改 `RitsuShellTheme.Current`**：它是不可变快照，需要变化请通过新主题文件、
  scopes 或 `RegisterModTokens` 重新提供并触发 `ReapplyActiveTheme`。
- **避免 hardcode 颜色 / 字号**：若发现需要的 token 还不存在，扩展 `semantic` 或 `components`
  比直接写常量更利于其它主题二次定制。
