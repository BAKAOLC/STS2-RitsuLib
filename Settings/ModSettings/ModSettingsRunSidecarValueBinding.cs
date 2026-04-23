using STS2RitsuLib.Settings.RunSidecar;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Binds a settings field to JSON stored under <see cref="ModRunSidecarStore" />, validated against the active
    ///     run fingerprint. Safe for mod reload; does not alter vanilla synchronized save payloads.
    /// </summary>
    public sealed class ModSettingsRunSidecarValueBinding<TModel, TValue>(
        string modId,
        string dataKey,
        Func<TModel, TValue> getter,
        Action<TModel, TValue> setter)
        : IModSettingsValueBinding<TValue>, IRunSidecarModSettingsBinding, IModSettingsBindingSemantics
        where TModel : class, new()
    {
        /// <inheritdoc />
        public ModSettingsValueSemantics Semantics => ModSettingsValueSemantics.RunSnapshot;

        /// <inheritdoc />
        public string ModId { get; } = modId;

        /// <inheritdoc />
        public string DataKey { get; } = dataKey;

        /// <inheritdoc />
        public SaveScope Scope => SaveScope.Global;

        /// <inheritdoc />
        public TValue Read()
        {
            var status = ModRunSidecarStore.TryReadModel<TModel>(ModId, out var model);
            if (status != ModRunSidecarReadStatus.Ok)
                model = new();

            return getter(model);
        }

        /// <inheritdoc />
        public void Write(TValue value)
        {
            var status = ModRunSidecarStore.TryReadModel<TModel>(ModId, out var model);
            if (status != ModRunSidecarReadStatus.Ok)
                model = new();

            setter(model, value);
            ModRunSidecarStore.TryWriteModel(ModId, model);
        }

        /// <inheritdoc />
        public void Save()
        {
        }
    }
}
