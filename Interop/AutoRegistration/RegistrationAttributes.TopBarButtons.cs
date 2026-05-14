using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     Declaratively registers a mod-owned top-bar button (see
    ///     <see cref="ModTopBarButtonRegistry" />). Mirrors the ergonomics of
    ///     <see cref="RegisterOwnedCardPileAttribute" /> but targets the generic, pile-independent
    ///     top-bar button system.
    ///     声明式注册一个 mod-owned 顶部栏按钮（参见
    ///     <see cref="ModTopBarButtonRegistry" />）。其易用性对应
    ///     <see cref="RegisterOwnedCardPileAttribute" />，但目标是通用且与牌堆无关的
    ///     顶部栏按钮系统。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Place on any concrete class inside your mod assembly. The annotated class must implement
    ///         <see cref="IModTopBarButtonHandler" /> — ritsulib instantiates it once (parameterless
    ///         constructor required) and wires
    ///         <see cref="IModTopBarButtonHandler.OnClick" /> into <see cref="ModTopBarButtonSpec.OnClick" />
    ///         and <see cref="IModTopBarButtonHandler.IsVisible" /> into
    ///         <see cref="ModTopBarButtonSpec.VisibleWhen" />.
    ///         <see cref="ModTopBarButtonSpec.VisibleWhen" />。
    ///     </para>
    ///     <para>
    ///         Localization follows the vanilla loc-table convention: title / description are resolved
    ///         against <c>static_hover_tips</c> using <c>"{id}.title"</c> and <c>"{id}.description"</c> where
    ///         <c>id</c> is the qualified button id.
    ///     </para>
    ///     <para>
    ///         将其放在你的 mod 程序集中的任意具体类上。带注解的类必须实现
    ///         <see cref="IModTopBarButtonHandler" />；ritsulib 会实例化一次（需要无参
    ///         构造函数），并将
    ///         <see cref="IModTopBarButtonHandler.OnClick" /> 接入 <see cref="ModTopBarButtonSpec.OnClick" />，
    ///         将 <see cref="IModTopBarButtonHandler.IsVisible" /> 接入
    ///         <see cref="ModTopBarButtonSpec.VisibleWhen" />。
    ///         <see cref="ModTopBarButtonSpec.VisibleWhen" />。
    ///     </para>
    ///     <para>
    ///         本地化遵循原版本地化表约定：标题/描述会基于
    ///         <c>static_hover_tips</c> 解析，使用 <c>"{id}.title"</c> 和 <c>"{id}.description"</c>，其中
    ///         <c>id</c> 是限定按钮 id。
    ///     </para>
    /// </remarks>
    /// <param name="localButtonStem">
    ///     Local, mod-scoped stem (matches <c>RegisterOwned(localStem, ...)</c>).
    ///     mod 作用域内的本地 stem（匹配 <c>RegisterOwned(localStem, ...)</c>）。
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedTopBarButtonAttribute(string localButtonStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     Local, mod-scoped button stem.
        ///     mod 作用域内的本地按钮词干。
        /// </summary>
        public string LocalButtonStem { get; } = localButtonStem;

        /// <summary>
        ///     Godot resource path for the icon (e.g. <c>res://my_mod/icons/recipes.png</c>).
        ///     图标的 Godot 资源路径（例如 <c>res://my_mod/icons/recipes.png</c>）。
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     Sort order within this mod's top-bar buttons.
        ///     在此 mod 的顶部栏按钮中的排序。
        /// </summary>
        public int ButtonOrder { get; set; }

        /// <summary>
        ///     Extra pixel offset X on top of the auto-stacked slot.
        ///     在自动堆叠 slot 基础上额外增加的 X 像素偏移。
        /// </summary>
        public float OffsetX { get; set; }

        /// <summary>
        ///     Extra pixel offset Y on top of the auto-stacked slot.
        ///     在自动堆叠 slot 基础上额外增加的 Y 像素偏移。
        /// </summary>
        public float OffsetY { get; set; }
    }
}
