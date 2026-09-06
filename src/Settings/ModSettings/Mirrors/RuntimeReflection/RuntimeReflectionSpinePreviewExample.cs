using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Data;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Characters.Visuals;
using STS2RitsuLib.Scaffolding.Visuals;
using STS2RitsuLib.Scaffolding.Visuals.Definition;
using STS2RitsuLib.Ui.Shell.Theme;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
#if !STS2_AT_LEAST_0_107_0
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
#endif

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         Demonstrates attribute-based runtime-reflection settings with profile and callback bindings, localized
    ///         text, buttons, and an interactive character animation preview.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         演示基于特性的运行时反射设置，包括用户档与回调绑定、本地化文本、按钮及交互式角色动画预览。
    ///     </para>
    /// </summary>
    [ModSettingsPage(Const.ModId, "runtime-reflection-spine-example",
        Title = "Character animation preview (sample)",
        TitleKey = "ritsulib.runtimeReflection.spine.page.title",
        Description = "Try bindings and preview character Spine animations, frame sequences, and static cues.",
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
            Description = "Preview built-in and mod character combat visuals in a small viewport.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.entry.description",
            Order = 0)]
        public Control CreateIroncladSpinePreview()
        {
            var availableCharacters = ModelDb.AllCharacters.ToList();
            if (availableCharacters.Count == 0)
                availableCharacters = [ModelDb.Character<Ironclad>()];

            var root = new VBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            root.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.page.layout.sectionSeparation", 8));
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
            characterLabel.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            characterLabel.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            characterLabel.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            characterRow.AddChild(characterLabel);
            root.AddChild(characterRow);

            var viewportContainer = new SubViewportContainer
            {
                CustomMinimumSize = new(0, 360),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Stretch = true,
            };
            var viewport = new SubViewport
            {
                TransparentBg = true,
                Size = new(640, 360),
            };
            viewportContainer.AddChild(viewport);
            var previewRoot = new Node2D();
            viewport.AddChild(previewRoot);
            root.AddChild(viewportContainer);

            var animationsTitle = new Label
            {
                Text = L("ritsulib.runtimeReflection.spine.animations.title", "Animations"),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            animationsTitle.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.BodyBold);
            animationsTitle.AddThemeFontSizeOverride("font_size",
                RitsuShellTheme.Current.Metric.FontSize.SettingLineTitle);
            animationsTitle.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelPrimary);
            root.AddChild(animationsTitle);

            var state = new PreviewState { Character = availableCharacters[0] };
            var playbackStatus = new Label
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Visible = false,
            };
            playbackStatus.AddThemeFontOverride("font", RitsuShellTheme.Current.Font.Body);
            playbackStatus.AddThemeFontSizeOverride("font_size", RitsuShellTheme.Current.Metric.FontSize.Secondary);
            playbackStatus.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
            var replayButton = new ModSettingsTextButton(
                L("ritsulib.runtimeReflection.spine.replay", "Replay"), ModSettingsButtonTone.Accent, null)
            {
                Disabled = true,
            };

            state.Picker = new(
                [],
                state.Animation,
                animation =>
                {
                    state.Animation = animation;
                    RefreshPreview();
                })
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            var animationRow = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            animationRow.AddThemeConstantOverride("separation",
                RitsuShellThemeLayoutResolver.ResolveInt("components.page.layout.sectionSeparation", 8));
            animationRow.AddChild(state.Picker);
            animationRow.AddChild(replayButton);
            root.AddChild(animationRow);
            root.AddChild(playbackStatus);
            replayButton.Pressed += RefreshPreview;
            var characterOptions = availableCharacters
                .Select(character => (character, ResolveCharacterName(character)))
                .ToArray();

            var characterPicker = new ModSettingsDropdownChoiceControl<CharacterModel>(
                characterOptions,
                state.Character,
                character =>
                {
                    state.Character = character;
                    state.Animation = new(string.Empty, false);
                    RefreshPreview();
                })
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(260, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight),
            };
            characterRow.AddChild(characterPicker);
            viewportContainer.Resized += () => Callable.From(ApplyPreviewTransform).CallDeferred();
            root.TreeExiting += () => ClearPreview(false);

            root.TreeEntered += () =>
            {
                Callable.From(() =>
                {
                    if (!GodotObject.IsInstanceValid(root) || !root.IsInsideTree())
                        return;
                    Callable.From(() =>
                    {
                        if (GodotObject.IsInstanceValid(root) && GodotObject.IsInstanceValid(viewport))
                            RefreshPreview();
                    }).CallDeferred();
                }).CallDeferred();
            };
            return root;

            void RefreshPreview()
            {
                if (!GodotObject.IsInstanceValid(root) || !root.IsInsideTree() ||
                    !GodotObject.IsInstanceValid(viewport))
                    return;

                ClearPreview();
                replayButton.Disabled = true;
                state.Picker.SetOptions([], state.Animation);
                playbackStatus.Visible = false;
                try
                {
                    state.Visuals = state.Character.CreateVisuals();
                    if (!GodotObject.IsInstanceValid(state.Visuals))
                    {
                        ShowPreviewFailure();
                        return;
                    }

                    previewRoot.AddChild(state.Visuals);
                    var visualsForDeferred = state.Visuals;
                    var deferredBuildVersion = state.BuildVersion;
                    Callable.From(() => { InitializePreviewVisuals(visualsForDeferred, deferredBuildVersion); })
                        .CallDeferred();
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    ClearPreview();
                    ShowPreviewFailure(ex);
                }
            }

            void ClearPreview(bool detach = true)
            {
                state.BuildVersion++;
                if (GodotObject.IsInstanceValid(state.Visuals))
                {
                    CueFrameSequencePlayer.StopUnder(state.Visuals);
                    if (detach)
                        state.Visuals.GetParent()?.RemoveChild(state.Visuals);
                    state.Visuals.QueueFree();
                }

                state.Visuals = null;
            }

            void ShowPreviewFailure(Exception? exception = null)
            {
                playbackStatus.Text = L("ritsulib.runtimeReflection.spine.failed",
                    "Could not preview this animation. Check the character's visual resources and game log.");
                playbackStatus.AddThemeColorOverride("font_color",
                    RitsuShellTheme.Current.Component.TextButton.Danger.Fg);
                playbackStatus.Visible = true;
                if (exception != null)
                    RitsuLibFramework.Logger.Warn(
                        $"[AnimationPreview] Could not preview '{state.Character.Id}' / '{state.Animation.Name}': {exception}");
            }

            void ApplyPreviewTransform()
            {
                if (!GodotObject.IsInstanceValid(root) || !root.IsInsideTree() ||
                    !GodotObject.IsInstanceValid(state.Visuals))
                    return;

                var bounds = TryComputeCanvasItemBounds(state.Visuals);
                if (bounds == null)
                {
                    previewRoot.Scale = Vector2.One;
                    previewRoot.Position = new(viewport.Size.X * 0.5f, viewport.Size.Y * 0.86f);
                    return;
                }

                var rect = bounds.Value;
                var scale = Mathf.Min(1.65f, Mathf.Min(viewport.Size.X * 0.9f / rect.Size.X,
                    viewport.Size.Y * 0.78f / rect.Size.Y));

                var centerX = rect.Position.X + rect.Size.X * 0.5f;
                var bottomY = rect.Position.Y + rect.Size.Y;
                previewRoot.Scale = new(scale, scale);
                previewRoot.Position = new(
                    viewport.Size.X * 0.5f - centerX * scale,
                    viewport.Size.Y * 0.90f - bottomY * scale);
            }

            void InitializePreviewVisuals(NCreatureVisuals visuals, int buildVersion)
            {
                if (buildVersion != state.BuildVersion)
                    return;
                if (!GodotObject.IsInstanceValid(root) || !root.IsInsideTree() ||
                    !GodotObject.IsInstanceValid(viewport) ||
                    !GodotObject.IsInstanceValid(visuals))
                    return;

                try
                {
                    var cues = (state.Character as IModCharacterAssetOverrides)?.VisualCues;
                    var animations = EnumeratePreviewAnimations(visuals, cues);
                    if (animations.Count == 0)
                    {
                        playbackStatus.Text = L("ritsulib.runtimeReflection.spine.empty",
                            "This character has no Spine animations or visual cues to preview.");
                        playbackStatus.AddThemeColorOverride("font_color", RitsuShellTheme.Current.Text.LabelSecondary);
                        playbackStatus.Visible = true;
                        ApplyPreviewTransform();
                        return;
                    }

                    if (!animations.Contains(state.Animation))
                        state.Animation = animations.FirstOrDefault(animation =>
                            animation.Name.Equals(animation.IsCue ? "idle" : "idle_loop",
                                StringComparison.OrdinalIgnoreCase)) ?? animations[0];
                    state.Picker.SetOptions([
                            .. animations.Select(animation =>
                                (animation, string.Format(L(animation.IsCue
                                        ? "ritsulib.runtimeReflection.spine.cueOption"
                                        : "ritsulib.runtimeReflection.spine.spineOption",
                                    animation.IsCue ? "{0} (Cue)" : "{0} (Spine)"), animation.Name))),
                        ],
                        state.Animation);
                    replayButton.Disabled = false;

                    if (state.Animation.IsCue)
                    {
                        if (cues == null || !CanLoadCue(cues, state.Animation.Name) ||
                            visuals.FindChildren("*", "Sprite2D", owned: false).Count == 0 ||
                            !ModCreatureVisualPlayback.TryPlayCue(visuals, state.Character, state.Animation.Name,
                                [state.Animation.Name]))
                            ShowPreviewFailure();
                    }
                    else
                        visuals.SpineAnimation.SetAnimation(state.Animation.Name);

                    ApplyPreviewTransform();
                }
                catch (Exception ex) when (RitsuLibExceptionPolicy.IsRecoverable(ex))
                {
                    CueFrameSequencePlayer.StopUnder(visuals);
                    ShowPreviewFailure(ex);
                }
            }
        }

        private sealed class PreviewState
        {
            internal NCreatureVisuals? Visuals;
            internal int BuildVersion;
            internal PreviewAnimation Animation = new(string.Empty, false);
            internal ModSettingsDropdownChoiceControl<PreviewAnimation> Picker = null!;
            internal required CharacterModel Character { get; set; }
        }

        private sealed record PreviewAnimation(string Name, bool IsCue);

        private static List<PreviewAnimation> EnumeratePreviewAnimations(NCreatureVisuals visuals, VisualCueSet? cues)
        {
            var cueNames = (cues?.FrameSequenceByCue?.Keys ?? [])
                .Concat(cues?.TexturePathByCue?.Keys ?? [])
                .Where(static name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase);
            return
            [
                .. cueNames.Select(static name => new PreviewAnimation(name, true)),
                .. EnumerateAnimations(visuals).Select(static name => new PreviewAnimation(name, false)),
            ];
        }

        private static bool CanLoadCue(VisualCueSet cues, string name)
        {
            if (cues.FrameSequenceByCue != null && cues.FrameSequenceByCue.TryGetValue(name, out var sequence))
                return sequence is { Frames.Count: > 0 } && sequence.Frames.All(static frame =>
                    !string.IsNullOrWhiteSpace(frame.TexturePath) &&
                    ResourceLoader.Load<Texture2D>(frame.TexturePath) != null);

            return cues.TexturePathByCue != null && cues.TexturePathByCue.TryGetValue(name, out var path) &&
                   !string.IsNullOrWhiteSpace(path) && ResourceLoader.Load<Texture2D>(path) != null;
        }

        private static Rect2? TryComputeCanvasItemBounds(Node root)
        {
            var visuals = root as NCreatureVisuals;
            var spineNode = visuals?.GetNodeOrNull<Node2D>("%Visuals");
            var spineBounds = visuals?.SpineBody?.GetSkeleton()?.GetBounds();
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
                    var rectangle = ReferenceEquals(node, spineNode) && spineBounds.HasValue
                        ? spineBounds
                        : node.GetType().GetMethod("GetRect", Type.EmptyTypes)?.Invoke(node, null);
                    if (rectangle is Rect2 { Size: { X: > 0.001f, Y: > 0.001f } } rect)
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

            var names =
#if !STS2_AT_LEAST_0_107_0
                data.GetAnimations()
                    .Select(animationObject => new MegaAnimation(Variant.From(animationObject)).GetName())
#else
                data.GetAnimationNames()
#endif
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
