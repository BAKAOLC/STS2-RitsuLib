using Godot;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Adapts a <see cref="float" /> binding to the <see cref="double" /> slider API. Converting writes back to
    ///         <see cref="float" /> can introduce precision drift; prefer double-backed settings or the obsolete
    ///         float-native <c>AddSlider</c> overload.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="float" /> 绑定适配到 <see cref="double" /> 滑块 API。写回
    ///         <see cref="float" /> 时可能产生精度漂移；应优先使用以 <see cref="double" /> 存储的设置，
    ///         或已弃用但原生使用 <see cref="float" /> 的 <c>AddSlider</c> 重载。
    ///     </para>
    /// </summary>
    /// <param name="inner">
    ///     <para xml:lang="en">The float binding to adapt.</para>
    ///     <para xml:lang="zh-CN">要适配的单精度浮点绑定。</para>
    /// </param>
    [Obsolete(
        "Prefer double-backed settings and AddSlider(double), or rely on the obsolete AddSlider(float) overload. This adapter can worsen float/double rounding in the slider UI.")]
    public sealed class ModSettingsDoubleFromFloatBinding(IModSettingsValueBinding<float> inner)
        : IModSettingsValueBinding<double>, IModSettingsBindingSaveDispatch
    {
        private readonly IModSettingsValueBinding<float> _inner =
            ModSettingsBindingValidation.RequireNonNull(inner, nameof(inner));

        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [_inner];

        /// <inheritdoc />
        public string ModId => _inner.ModId;

        /// <inheritdoc />
        public string DataKey => _inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => _inner.Scope;

        /// <inheritdoc />
        public double Read()
        {
            return _inner.Read();
        }

        /// <inheritdoc />
        public void Write(double value)
        {
            _inner.Write((float)value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            _inner.Save();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Adapts an <see cref="int" /> binding to the <see cref="double" /> slider API on
    ///         <see cref="ModSettingsSectionBuilder" />, rounding each written value to an integer.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="int" /> 绑定适配到 <see cref="ModSettingsSectionBuilder" /> 的
    ///         <see cref="double" /> 滑块 API，并将每个写入值舍入为整数。
    ///     </para>
    /// </summary>
    /// <param name="inner">
    ///     <para xml:lang="en">The integer binding to adapt.</para>
    ///     <para xml:lang="zh-CN">要适配的整数绑定。</para>
    /// </param>
    public sealed class ModSettingsDoubleFromIntBinding(IModSettingsValueBinding<int> inner)
        : IModSettingsValueBinding<double>, IModSettingsBindingSaveDispatch
    {
        private readonly IModSettingsValueBinding<int> _inner =
            ModSettingsBindingValidation.RequireNonNull(inner, nameof(inner));

        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [_inner];

        /// <inheritdoc />
        public string ModId => _inner.ModId;

        /// <inheritdoc />
        public string DataKey => _inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => _inner.Scope;

        /// <inheritdoc />
        public double Read()
        {
            return _inner.Read();
        }

        /// <inheritdoc />
        public void Write(double value)
        {
            _inner.Write(Mathf.RoundToInt(value));
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            _inner.Save();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Adapts a <see cref="double" /> binding to
    ///         <see cref="ModSettingsSectionBuilder.AddIntSlider" />, rounding reads to an integer and writing integer
    ///         values back as doubles.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         将 <see cref="double" /> 绑定适配到 <see cref="ModSettingsSectionBuilder.AddIntSlider" />；
    ///         读取时舍入为整数，写入时将整数值作为双精度值传回。
    ///     </para>
    /// </summary>
    /// <param name="inner">
    ///     <para xml:lang="en">The double binding to adapt.</para>
    ///     <para xml:lang="zh-CN">要适配的双精度浮点绑定。</para>
    /// </param>
    public sealed class ModSettingsIntFromDoubleBinding(IModSettingsValueBinding<double> inner)
        : IModSettingsValueBinding<int>, IModSettingsBindingSaveDispatch
    {
        private readonly IModSettingsValueBinding<double> _inner =
            ModSettingsBindingValidation.RequireNonNull(inner, nameof(inner));

        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [_inner];

        /// <inheritdoc />
        public string ModId => _inner.ModId;

        /// <inheritdoc />
        public string DataKey => _inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => _inner.Scope;

        /// <inheritdoc />
        public int Read()
        {
            return Mathf.RoundToInt(_inner.Read());
        }

        /// <inheritdoc />
        public void Write(int value)
        {
            _inner.Write(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            _inner.Save();
        }
    }
}
