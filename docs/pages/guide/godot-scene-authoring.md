---
title:
  en: Godot Scene Authoring
  zh-CN: Godot 场景编写
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Register C# Scripts{lang="en"}

::: en

If your mod ships `.tscn` scenes with C# scripts, call this once from the mod initializer:

```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(
    Assembly.GetExecutingAssembly(),
    Logger);
```

Without this, Godot can load the scene file but fail to resolve the attached C# script type.

:::

## 注册 C# 脚本{lang="zh-CN"}

::: zh-CN

如果 Mod 的 `.tscn` 场景挂了 C# 脚本，在初始化入口调用一次：

```csharp
RitsuLibFramework.EnsureGodotScriptsRegistered(
    Assembly.GetExecutingAssembly(),
    Logger);
```

否则 Godot 可能能加载场景文件，但找不到挂在节点上的 C# 脚本类型。

:::

## Load Scenes From Profiles{lang="en"}

::: en

For content visuals, prefer the model's asset profile over manual scene loading:

```csharp
public override EventAssetProfile AssetProfile => new()
{
    LayoutScenePath = "res://MyMod/scenes/events/my_event.tscn"
};
```

Use code-created scenes only when the scene must depend on runtime state or when you are intentionally avoiding a `.tscn` file.

:::

## 通过 Profile 加载场景{lang="zh-CN"}

::: zh-CN

内容视觉优先通过模型的 asset profile 指定，不要手动到处加载场景：

```csharp
public override EventAssetProfile AssetProfile => new()
{
    LayoutScenePath = "res://MyMod/scenes/events/my_event.tscn"
};
```

只有场景必须依赖运行时状态，或你明确不想维护 `.tscn` 文件时，才用代码创建场景。

:::

## Scene Path Checklist{lang="en"}

::: en

- Use `res://` paths that exist in the packaged mod.
- Keep script classes `public` and in the compiled mod assembly.
- Avoid editor-only resource paths.
- If a scene is used by a model profile, register the model early through the content pack.
- For scenes that instantiate custom nodes, register all script assemblies before any model or UI asks for the scene.

:::

## 场景路径检查{lang="zh-CN"}

::: zh-CN

- 使用打包后真实存在的 `res://` 路径。
- 脚本类保持 `public`，并编进 Mod 程序集。
- 不要使用编辑器专用资源路径。
- 场景由模型 profile 使用时，尽早通过 content pack 注册该模型。
- 如果场景会实例化自定义节点，在任何模型或 UI 请求场景前注册脚本程序集。

:::

## When To Use Factories{lang="en"}

::: en

Some templates expose protected factory methods such as `TryCreateLayoutPackedScene()` or `TryCreateCreatureVisuals()`. Override them when a static path is not enough:

```csharp
protected override PackedScene? TryCreateLayoutPackedScene()
{
    return BuildSceneForCurrentMode();
}
```

Return `null` to let the normal asset profile path load.

:::

## 何时使用 Factory{lang="zh-CN"}

::: zh-CN

部分模板提供 `TryCreateLayoutPackedScene()`、`TryCreateCreatureVisuals()` 等 protected factory。静态路径不够用时再覆写：

```csharp
protected override PackedScene? TryCreateLayoutPackedScene()
{
    return BuildSceneForCurrentMode();
}
```

返回 `null` 表示继续走普通 asset profile 路径。

:::
