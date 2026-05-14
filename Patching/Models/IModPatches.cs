using STS2RitsuLib.Patching.Core;

namespace STS2RitsuLib.Patching.Models
{
    /// <summary>
    ///     Interface for a class that contains patches for the mod.
    ///     This is used to group patches together and make it easier to add them to the patcher.
    ///     表示包含此 mod patch 的类的接口。
    ///     用于将 patch 分组，并使它们更容易添加到 patcher。
    /// </summary>
    public interface IModPatches
    {
        /// <summary>
        ///     Adds the patches to the patcher.
        ///     将 patch 添加到 patcher。
        /// </summary>
        static abstract void AddTo(ModPatcher patcher);
    }
}
