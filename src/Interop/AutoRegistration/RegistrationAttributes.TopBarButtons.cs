using STS2RitsuLib.TopBar;

namespace STS2RitsuLib.Interop.AutoRegistration
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Declaratively registers a mod-owned top-bar button through
    ///         <see cref="ModTopBarButtonRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         通过 <see cref="ModTopBarButtonRegistry" /> 声明式注册归属于模组的顶部栏按钮。
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Place on any concrete class inside your mod assembly. The annotated class must implement
    ///         <see cref="IModTopBarButtonHandler" />. RitsuLib creates one instance through its public
    ///         parameterless constructor and maps <see cref="IModTopBarButtonHandler.OnClick" />,
    ///         <see cref="IModTopBarButtonHandler.IsVisible" />, <see cref="IModTopBarButtonHandler.IsOpen" />,
    ///         and <see cref="IModTopBarButtonHandler.GetCount" /> to the corresponding
    ///         <see cref="ModTopBarButtonSpec" /> callbacks.
    ///     </para>
    ///     <para xml:lang="en">
    ///         The hover-tip title and description use <c>static_hover_tips</c> keys
    ///         <c>"{id}.title"</c> and <c>"{id}.description"</c>, where <c>id</c> is the qualified button ID.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         请将此特性用于模组程序集内任何实现 <see cref="IModTopBarButtonHandler" /> 的具体类。RitsuLib
    ///         会通过公共无参构造函数创建一个实例，并将 <see cref="IModTopBarButtonHandler.OnClick" />、
    ///         <see cref="IModTopBarButtonHandler.IsVisible" />、
    ///         <see cref="IModTopBarButtonHandler.IsOpen" /> 和
    ///         <see cref="IModTopBarButtonHandler.GetCount" /> 分别接入
    ///         <see cref="ModTopBarButtonSpec" /> 的对应回调。
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         悬停提示标题和描述使用 <c>static_hover_tips</c> 中的 <c>"{id}.title"</c> 和
    ///         <c>"{id}.description"</c> 键，其中 <c>id</c> 是完全限定的按钮 ID。
    ///     </para>
    /// </remarks>
    /// <param name="localButtonStem">
    ///     <para xml:lang="en">Local button stem within the owning mod's namespace.</para>
    ///     <para xml:lang="zh-CN">归属模组命名空间内的本地按钮词干。</para>
    /// </param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterOwnedTopBarButtonAttribute(string localButtonStem) : AutoRegistrationAttribute
    {
        /// <summary>
        ///     <para xml:lang="en">Local button stem within the owning mod's namespace.</para>
        ///     <para xml:lang="zh-CN">归属模组命名空间内的本地按钮词干。</para>
        /// </summary>
        public string LocalButtonStem { get; } = localButtonStem;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Godot resource path for the icon (for example, <c>res://my_mod/icons/recipes.png</c>).
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         图标的 Godot 资源路径（例如 <c>res://my_mod/icons/recipes.png</c>）。
        ///     </para>
        /// </summary>
        public string? IconPath { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Global sort order among mod top-bar buttons. Lower values appear closer to the vanilla deck button.
        ///     </para>
        ///     <para xml:lang="zh-CN">模组顶部栏按钮之间的全局排序值；值越小越靠近原版牌组按钮。</para>
        /// </summary>
        public int ButtonOrder { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Additional horizontal offset from the automatically arranged slot, in pixels.
        ///     </para>
        ///     <para xml:lang="zh-CN">相对于自动排列槽位的额外水平像素偏移。</para>
        /// </summary>
        public float OffsetX { get; set; }

        /// <summary>
        ///     <para xml:lang="en">
        ///         Additional vertical offset from the automatically arranged slot, in pixels.
        ///     </para>
        ///     <para xml:lang="zh-CN">相对于自动排列槽位的额外垂直像素偏移。</para>
        /// </summary>
        public float OffsetY { get; set; }
    }
}
