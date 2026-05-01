using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Data;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Sample settings page: character spine preview.
    /// </summary>
    [ModSettingsPage(Const.ModId, "runtime-reflection-spine-example",
        Title = "Spine preview (sample)",
        TitleKey = "ritsulib.runtimeReflection.spine.page.title",
        Description = "Try bindings and a simple spine preview.",
        DescriptionKey = "ritsulib.runtimeReflection.spine.page.description",
        I18NProviderUsing = nameof(GetI18NProvider),
        ParentPageId = "debug-showcase", SortOrder = 20_100)]
    [ModSettingsSection("spine",
        Title = "Preview",
        TitleKey = "ritsulib.runtimeReflection.spine.section.title",
        Description = "Pick a character and an animation.",
        DescriptionKey = "ritsulib.runtimeReflection.spine.section.description")]
    internal sealed class RuntimeReflectionSpinePreviewExample
    {
        private const string ManualSnapshotDataKey = "runtime_reflection_spine_callback_manual_profile_snapshot";
        private static bool _manualSnapshotStoreRegistered;
        private int _manualBindingSavedValue = 3;
        private bool _manualSnapshotLoaded;

        [ModSettingsToggle("spine_profile_auto_save", "spine",
            Label = "Profile toggle (sample)",
            LabelKey = "ritsulib.runtimeReflection.spine.binding.profileAuto.label",
            Description = "Saved with your profile.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.binding.profileAuto.description",
            Order = -20)]
        [ModSettingsBinding(
            Source = ModSettingsReflectionBindingSource.Profile,
            DataKey = "runtime_reflection_spine_profile_auto")]
        public bool ProfileAutoSaveDemo { get; set; }

        [ModSettingsIntSlider("spine_callback_manual", "spine", 0, 10,
            Label = "Manual save value (sample)",
            LabelKey = "ritsulib.runtimeReflection.spine.binding.callbackManual.label",
            Description = "Change the value, then use Save below to store the snapshot.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.binding.callbackManual.description",
            Order = -19)]
        [ModSettingsBinding(
            Source = ModSettingsReflectionBindingSource.Callback,
            DataKey = "runtime_reflection_spine_callback_manual",
            ReadUsing = nameof(ReadManualBindingValue),
            WriteUsing = nameof(WriteManualBindingValue),
            SaveUsing = nameof(SaveManualBindingValue))]
        public int CallbackManualSaveDemo { get; set; } = 3;

        [ModSettingsParagraph("spine_callback_manual_state", "spine",
            Description = "Live value vs last saved snapshot.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.binding.callbackState.description",
            Order = -18)]
        public string BuildManualBindingStateText()
        {
            EnsureManualSnapshotLoaded();
            return string.Format(
                L("ritsulib.runtimeReflection.spine.binding.callbackState.text", "Current: {0} | Saved: {1}"),
                CallbackManualSaveDemo,
                _manualBindingSavedValue);
        }

        [ModSettingsButton("spine_callback_manual_save", "spine",
            Label = "Save snapshot",
            LabelKey = "ritsulib.runtimeReflection.spine.binding.callbackSave.label",
            ButtonText = "Save",
            ButtonTextKey = "ritsulib.runtimeReflection.spine.binding.callbackSave.button",
            Description = "Stores the current value as the saved snapshot.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.binding.callbackSave.description",
            Order = -17)]
        public void SaveManualBindingFromButton()
        {
            EnsureManualSnapshotLoaded();
            PersistManualSnapshot(CallbackManualSaveDemo);
        }

        [ModSettingsCustomEntry("ironclad_spine_preview", "spine",
            Label = "Character preview",
            LabelKey = "ritsulib.runtimeReflection.spine.entry.label",
            Description = "Built-in character visuals in a small viewport.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.entry.description",
            Order = 0)]
        // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1822
        public Control CreateIroncladSpinePreview()
#pragma warning restore CA1822
        {
            var availableCharacters = ModelDb.AllCharacters.ToList();
            if (availableCharacters.Count == 0)
                availableCharacters = [ModelDb.Character<Ironclad>()];

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            var characterRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            characterRow.AddThemeConstantOverride("separation", 10);
            var characterLabel = new Label
            {
                Text = L("ritsulib.runtimeReflection.spine.character.label", "Character"),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
                VerticalAlignment = VerticalAlignment.Center,
            };
            characterRow.AddChild(characterLabel);
            root.AddChild(characterRow);

            var viewportContainer = new SubViewportContainer
            {
                CustomMinimumSize = new(640, 360),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            var viewport = new SubViewport
            {
                TransparentBg = true,
                Size = new(640, 360),
            };
            viewportContainer.AddChild(viewport);
            root.AddChild(viewportContainer);

            var animationsTitle = new Label
            {
                Text = L("ritsulib.runtimeReflection.spine.animations.title", "Animations"),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            root.AddChild(animationsTitle);

            NCreatureVisuals? currentVisuals = null;
            var currentCharacter = availableCharacters[0];
            var previewBuildVersion = 0;

            var animationsPicker = new ModSettingsDropdownChoiceControl<string>(
                [],
                string.Empty,
                animationName =>
                {
                    if (currentVisuals?.SpineBody == null)
                        return;
                    if (string.IsNullOrWhiteSpace(animationName))
                        return;

                    currentVisuals.SpineAnimation.SetAnimation(animationName);
                })
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            root.AddChild(animationsPicker);
            var characterOptions = availableCharacters
                .Select(character => (character, ResolveCharacterName(character)))
                .ToArray();

            var characterPicker = new ModSettingsDropdownChoiceControl<CharacterModel>(
                characterOptions,
                currentCharacter,
                character =>
                {
                    currentCharacter = character;
                    RefreshPreview();
                })
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(260, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            characterRow.AddChild(characterPicker);

            root.TreeEntered += () =>
            {
                Callable.From(() =>
                {
                    if (!GodotObject.IsInstanceValid(root))
                        return;
                    Callable.From(RefreshPreview).CallDeferred();
                }).CallDeferred();
            };
            return root;

            void RefreshPreview()
            {
                previewBuildVersion++;
                foreach (var child in viewport.GetChildren())
                    child.QueueFree();

                currentVisuals = currentCharacter.CreateVisuals();
                viewport.AddChild(currentVisuals);
                var visualsForDeferred = currentVisuals;
                var deferredBuildVersion = previewBuildVersion;
                Callable.From(() => InitializePreviewVisuals(visualsForDeferred, deferredBuildVersion)).CallDeferred();
            }

            void ApplyPreviewTransform()
            {
                if (currentVisuals == null)
                    return;

                var bounds = TryComputeCanvasItemBounds(currentVisuals);
                if (bounds == null)
                {
                    currentVisuals.Scale = Vector2.One;
                    currentVisuals.Position = new(viewport.Size.X * 0.5f, viewport.Size.Y * 0.86f);
                    return;
                }

                var rect = bounds.Value;
                var targetHeight = viewport.Size.Y * 0.78f;
                var scale = rect.Size.Y > 0.001f
                    ? Mathf.Clamp(targetHeight / rect.Size.Y, 0.45f, 1.65f)
                    : 1f;

                var centerX = rect.Position.X + rect.Size.X * 0.5f;
                var bottomY = rect.Position.Y + rect.Size.Y;
                currentVisuals.Scale = new(scale, scale);
                currentVisuals.Position = new(
                    viewport.Size.X * 0.5f - centerX * scale,
                    viewport.Size.Y * 0.90f - bottomY * scale);
            }

            void InitializePreviewVisuals(NCreatureVisuals visuals, int buildVersion)
            {
                if (buildVersion != previewBuildVersion)
                    return;
                if (!GodotObject.IsInstanceValid(visuals))
                    return;

                currentVisuals = visuals;
                ApplyPreviewTransform();

                var animationNames = EnumerateAnimations(visuals);
                var animationOptions = animationNames
                    .Select(name => (name, name))
                    .ToArray();

                if (animationNames.Count == 0)
                {
                    animationsPicker.SetOptions([], string.Empty);
                    return;
                }

                var preferred = animationNames.FirstOrDefault(name =>
                    string.Equals(name, "idle_loop", StringComparison.OrdinalIgnoreCase));
                var selected = preferred ?? animationNames[0];
                animationsPicker.SetOptions(animationOptions, selected);
                visuals.SpineAnimation.SetAnimation(selected);
            }
        }

        private static Rect2? TryComputeCanvasItemBounds(Node root)
        {
            var initialized = false;
            var min = Vector2.Zero;
            var max = Vector2.Zero;
            Traverse(root, Transform2D.Identity);
            return initialized ? new Rect2(min, max - min) : null;

            void Traverse(Node node, Transform2D parentTransform)
            {
                var localTransform = parentTransform;
                if (node is Node2D n2D)
                    localTransform = parentTransform * n2D.Transform;

                if (node is CanvasItem canvasItem)
                {
                    var getRect = node.GetType().GetMethod("GetRect", Type.EmptyTypes);
                    if (getRect?.Invoke(node, null) is Rect2 { Size: { X: > 0.001f, Y: > 0.001f } } rect)
                    {
                        Include(localTransform * rect.Position);
                        Include(localTransform * (rect.Position + new Vector2(rect.Size.X, 0f)));
                        Include(localTransform * (rect.Position + new Vector2(0f, rect.Size.Y)));
                        Include(localTransform * (rect.Position + rect.Size));
                    }

                    foreach (var child in canvasItem.GetChildren())
                        if (child != null)
                            Traverse(child, localTransform);

                    return;
                }

                foreach (var child in node.GetChildren())
                    if (child != null)
                        Traverse(child, localTransform);
            }

            void Include(Vector2 point)
            {
                if (!initialized)
                {
                    min = point;
                    max = point;
                    initialized = true;
                    return;
                }

                min = new(Mathf.Min(min.X, point.X), Mathf.Min(min.Y, point.Y));
                max = new(Mathf.Max(max.X, point.X), Mathf.Max(max.Y, point.Y));
            }
        }

        private static List<string> EnumerateAnimations(NCreatureVisuals visuals)
        {
            var data = visuals.SpineBody?.GetSkeleton()?.GetData();
            if (data == null)
                return [];

            var names = data.GetAnimations()
                .Select(animationObject => new MegaAnimation(Variant.From(animationObject)).GetName())
                .Where(name => !string.IsNullOrWhiteSpace(name)).ToList();

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        private static string ResolveCharacterName(CharacterModel character)
        {
            try
            {
                if (character.Title.Exists())
                    return character.Title.GetFormattedText();
            }
            catch
            {
                // ignored
            }

            return character.Id.Entry;
        }

        private static string L(string key, string fallback)
        {
            return ModSettingsLocalization.Get(key, fallback);
        }

        private static I18N GetI18NProvider()
        {
            return ModSettingsLocalization.Instance;
        }

        private int ReadManualBindingValue()
        {
            EnsureManualSnapshotLoaded();
            return CallbackManualSaveDemo;
        }

        private void WriteManualBindingValue(int value)
        {
            EnsureManualSnapshotLoaded();
            CallbackManualSaveDemo = value;
            if (!ProfileAutoSaveDemo)
                return;

            PersistManualSnapshot(value);
        }

        private void SaveManualBindingValue()
        {
            if (!ProfileAutoSaveDemo)
                return;

            EnsureManualSnapshotLoaded();
            PersistManualSnapshot(CallbackManualSaveDemo);
        }

        private void EnsureManualSnapshotLoaded()
        {
            if (_manualSnapshotLoaded)
                return;

            EnsureManualSnapshotStoreRegistered();
            var store = RitsuLibFramework.GetDataStore(Const.ModId);
            var snapshot = store.Get<ManualSnapshotBox>(ManualSnapshotDataKey).Value;
            CallbackManualSaveDemo = snapshot;
            _manualBindingSavedValue = snapshot;
            _manualSnapshotLoaded = true;
        }

        private static void EnsureManualSnapshotStoreRegistered()
        {
            if (_manualSnapshotStoreRegistered)
                return;

            var store = ModDataStore.For(Const.ModId);
            try
            {
                store.Register(
                    ManualSnapshotDataKey,
                    ManualSnapshotDataKey,
                    SaveScope.Profile,
                    () => new ManualSnapshotBox { Value = 3 });
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
            {
            }

            _manualSnapshotStoreRegistered = true;
        }

        private void PersistManualSnapshot(int value)
        {
            var store = RitsuLibFramework.GetDataStore(Const.ModId);
            store.Modify<ManualSnapshotBox>(ManualSnapshotDataKey, box => box.Value = value);
            store.Save(ManualSnapshotDataKey);
            _manualBindingSavedValue = value;
        }

        private sealed class ManualSnapshotBox
        {
            public int Value { get; set; }
        }
    }
}
