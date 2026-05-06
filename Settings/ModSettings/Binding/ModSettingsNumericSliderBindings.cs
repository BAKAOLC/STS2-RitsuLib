using Godot;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Adapts a <see cref="float" /> binding to <see cref="double" /> when you intentionally use the
    ///     <see cref="double" /> slider API with <see cref="float" /> storage (may introduce precision drift; prefer
    ///     <see cref="double" /> fields or the obsolete <c>AddSlider(..., IModSettingsValueBinding&lt;float&gt;, ...)</c>
    ///     overload for a float-native UI path).
    /// </summary>
    [Obsolete(
        "Prefer double-backed settings and AddSlider(double), or rely on the obsolete AddSlider(float) overload. This adapter can worsen float/double rounding in the slider UI.")]
    public sealed class ModSettingsDoubleFromFloatBinding(IModSettingsValueBinding<float> inner)
        : IModSettingsValueBinding<double>, IModSettingsBindingSaveDispatch
    {
        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [inner];

        /// <inheritdoc />
        public string ModId => inner.ModId;

        /// <inheritdoc />
        public string DataKey => inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => inner.Scope;

        /// <inheritdoc />
        public double Read()
        {
            return inner.Read();
        }

        /// <inheritdoc />
        public void Write(double value)
        {
            inner.Write((float)value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            inner.Save();
        }
    }

    /// <summary>
    ///     Adapts an <see cref="int" /> binding to <see cref="double" /> for continuous floating sliders on
    ///     <see cref="ModSettingsSectionBuilder" /> (writes round to integer).
    /// </summary>
    public sealed class ModSettingsDoubleFromIntBinding(IModSettingsValueBinding<int> inner)
        : IModSettingsValueBinding<double>, IModSettingsBindingSaveDispatch
    {
        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [inner];

        /// <inheritdoc />
        public string ModId => inner.ModId;

        /// <inheritdoc />
        public string DataKey => inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => inner.Scope;

        /// <inheritdoc />
        public double Read()
        {
            return inner.Read();
        }

        /// <inheritdoc />
        public void Write(double value)
        {
            inner.Write(Mathf.RoundToInt(value));
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            inner.Save();
        }
    }

    /// <summary>
    ///     Adapts a <see cref="double" /> binding to <see cref="ModSettingsSectionBuilder.AddIntSlider" /> by rounding.
    /// </summary>
    public sealed class ModSettingsIntFromDoubleBinding(IModSettingsValueBinding<double> inner)
        : IModSettingsValueBinding<int>, IModSettingsBindingSaveDispatch
    {
        IReadOnlyList<IModSettingsBinding> IModSettingsBindingSaveDispatch.ImmediateSaveTargets => [inner];

        /// <inheritdoc />
        public string ModId => inner.ModId;

        /// <inheritdoc />
        public string DataKey => inner.DataKey;

        /// <inheritdoc />
        public SaveScope Scope => inner.Scope;

        /// <inheritdoc />
        public int Read()
        {
            return Mathf.RoundToInt(inner.Read());
        }

        /// <inheritdoc />
        public void Write(int value)
        {
            inner.Write(value);
            ModSettingsBindingWriteEvents.NotifyValueWritten(this);
        }

        /// <inheritdoc />
        public void Save()
        {
            inner.Save();
        }
    }
}
