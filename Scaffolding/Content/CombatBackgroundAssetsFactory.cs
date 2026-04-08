using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Rooms;

namespace STS2RitsuLib.Scaffolding.Content
{
    /// <summary>
    ///     Builds <see cref="BackgroundAssets" /> without using the vanilla constructor that reads a fixed
    ///     <c>res://scenes/backgrounds/&lt;id&gt;/layers</c> tree. Use with
    ///     <see cref="ModEncounterTemplate.UseProgrammaticCombatBackground" /> /
    ///     <see cref="ModEncounterTemplate.BuildProgrammaticCombatBackground" />.
    /// </summary>
    public static class CombatBackgroundAssetsFactory
    {
        /// <summary>
        ///     Creates combat background assets from explicit scene and layer paths (same semantics as vanilla
        ///     <see cref="BackgroundAssets" />: main scene, parallax <c>_bg_</c> layers, optional <c>_fg_</c>).
        /// </summary>
        public static BackgroundAssets Create(string backgroundScenePath, IReadOnlyList<string> bgLayers,
            string? fgLayer = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backgroundScenePath);
            ArgumentNullException.ThrowIfNull(bgLayers);

            var layers = bgLayers as List<string> ?? [..bgLayers];
            return Construct(backgroundScenePath, layers, fgLayer);
        }

        internal static BackgroundAssets Construct(string backgroundScenePath, List<string> bgLayers,
            string? fgLayer)
        {
            var instance = (BackgroundAssets)RuntimeHelpers.GetUninitializedObject(typeof(BackgroundAssets));
            SetReadOnlyAutoProperty(instance, nameof(BackgroundAssets.BackgroundScenePath), backgroundScenePath);
            SetReadOnlyAutoProperty(instance, nameof(BackgroundAssets.BgLayers), bgLayers);
            SetReadOnlyAutoProperty(instance, nameof(BackgroundAssets.FgLayer), fgLayer);
            return instance;
        }

        private static void SetReadOnlyAutoProperty<T>(BackgroundAssets target, string propertyName, T value)
        {
            var field = typeof(BackgroundAssets).GetField(
                            $"<{propertyName}>k__BackingField",
                            BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(typeof(BackgroundAssets).FullName,
                            $"<{propertyName}>k__BackingField");

            field.SetValue(target, value);
        }
    }
}
