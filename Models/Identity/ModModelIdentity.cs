using MegaCrit.Sts2.Core.Models;

namespace STS2RitsuLib.Models.Identity
{
    /// <summary>
    ///     Runtime-only identity assigned to mutable models when they enter synchronized vanilla model ownership.
    ///     当 mutable model 进入原版同步所有权时分配的仅运行时身份。
    /// </summary>
    public readonly record struct ModModelIdentity(uint Value)
    {
        /// <summary>
        ///     Empty identity value.
        ///     空身份值。
        /// </summary>
        public static readonly ModModelIdentity None = new(0);

        /// <summary>
        ///     True when this identity can be used for model resolution.
        ///     当该身份可用于 model 解析时为 true。
        /// </summary>
        public bool IsValid => Value != 0;
    }

    /// <summary>
    ///     Wire token for resolving a model identity while also validating the expected model id.
    ///     用于解析 model identity 的传输令牌，同时验证预期 model id。
    /// </summary>
    public readonly record struct ModModelIdentityToken(ModModelIdentity Identity, ModelId ModelId)
    {
        /// <summary>
        ///     True when this token can be used for model resolution.
        ///     当该令牌可用于 model 解析时为 true。
        /// </summary>
        public bool IsValid => Identity.IsValid;
    }
}
