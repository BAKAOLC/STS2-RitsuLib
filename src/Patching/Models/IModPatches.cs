using STS2RitsuLib.Patching.Core;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Defines a group of mod patches that can register themselves with a <see cref="ModPatcher" />.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         定义一组可自行注册到 <see cref="ModPatcher" /> 的模组补丁。
    ///     </para>
    /// </summary>
    public interface IModPatches
    {
        /// <summary>
        ///     <para xml:lang="en">Adds the patches to <paramref name="patcher" />.</para>
        ///     <para xml:lang="zh-CN">将补丁添加到 <paramref name="patcher" />。</para>
        /// </summary>
        static abstract void AddTo(ModPatcher patcher);
    }
}
