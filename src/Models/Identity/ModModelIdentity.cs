using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Identity
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Runtime-only identity assigned to mutable models when they enter synchronized base-game ownership.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         可变模型进入游戏原版的同步所有关系时，为其分配的仅限运行时身份。
    ///     </para>
    /// </summary>
    public readonly record struct ModModelIdentity(uint Value)
    {
        /// <summary>
        ///     <para xml:lang="en">Represents an empty identity.</para>
        ///     <para xml:lang="zh-CN">表示空身份。</para>
        /// </summary>
        public static readonly ModModelIdentity None = new(0);

        /// <summary>
        ///     <para xml:lang="en">Gets whether this identity can be used to resolve a model.</para>
        ///     <para xml:lang="zh-CN">获取此身份是否可用于解析模型。</para>
        /// </summary>
        public bool IsValid => Value != 0;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Wire token that resolves a model identity while validating the expected model ID.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         用于解析模型身份并验证预期模型 ID 的传输令牌。
    ///     </para>
    /// </summary>
    public readonly record struct ModModelIdentityToken(ModModelIdentity Identity, ModelId ModelId)
    {
        /// <summary>
        ///     <para xml:lang="en">Gets whether this token can be used to resolve a model.</para>
        ///     <para xml:lang="zh-CN">获取此令牌是否可用于解析模型。</para>
        /// </summary>
        public bool IsValid => Identity.IsValid;
    }
}
