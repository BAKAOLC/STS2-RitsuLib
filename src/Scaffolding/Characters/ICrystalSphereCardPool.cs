using Godot;
using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Scaffolding.Characters
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Allows a card pool to replace the crystal-sphere card-reward back texture and material.
    ///     </para>
    ///     <para xml:lang="zh-CN">允许卡池替换水晶球事件中卡奖励格的底图和材质。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///         Properties are read when each reward view becomes ready. Returned resources are used directly and
    ///         must remain valid while the view uses them; disposed resources are ignored.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         属性在每个奖励视图就绪时读取。返回的资源会被直接使用，须在视图使用期间保持有效；已释放的资源会被忽略。
    ///     </para>
    /// </remarks>
    public interface ICrystalSphereCardPool
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the crystal-sphere card-reward back texture, or <see langword="null" /> to keep the vanilla back.
        ///     </para>
        ///     <para xml:lang="zh-CN">获取水晶球卡奖励格底图；返回 <see langword="null" /> 时保留原版纹理。</para>
        /// </summary>
        Texture2D? CrystalSphereCardTexture => null;

        /// <summary>
        ///     <para xml:lang="en">
        ///         Gets the crystal-sphere card-reward back material, or <see langword="null" /> to keep
        ///         <see cref="CardPoolModel.FrameMaterial" />.
        ///     </para>
        ///     <para xml:lang="zh-CN">
        ///         获取水晶球卡奖励格材质；返回 <see langword="null" /> 时保留
        ///         <see cref="CardPoolModel.FrameMaterial" />。
        ///     </para>
        /// </summary>
        Material? CrystalSphereCardMaterial => null;
    }
}
