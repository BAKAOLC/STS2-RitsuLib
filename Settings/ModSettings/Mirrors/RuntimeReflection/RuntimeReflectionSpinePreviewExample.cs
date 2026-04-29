using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Utils;

namespace STS2RitsuLib.Settings
{
    /// <summary>
    ///     Minimal attribute reflection page example showing vanilla Ironclad spine visuals.
    /// </summary>
    [ModSettingsPage(Const.ModId, "runtime-reflection-spine-example",
        Title = "Runtime Reflection Spine Example",
        TitleKey = "ritsulib.runtimeReflection.spine.page.title",
        Description = "Minimal attribute-driven example using CustomEntry to preview vanilla character spine visuals.",
        DescriptionKey = "ritsulib.runtimeReflection.spine.page.description",
        I18NProviderUsing = nameof(GetI18NProvider),
        ParentPageId = Const.ModId, SortOrder = 20_100)]
    [ModSettingsSection("spine",
        Title = "Spine Preview",
        TitleKey = "ritsulib.runtimeReflection.spine.section.title",
        Description = "Select a vanilla character and browse all available spine animations.",
        DescriptionKey = "ritsulib.runtimeReflection.spine.section.description")]
    internal sealed class RuntimeReflectionSpinePreviewExample
    {
        private int _manualBindingSavedValue = 3;

        [ModSettingsToggle("spine_profile_auto_save", "spine",
            Label = "Profile auto-save demo",
            LabelKey = "ritsulib.runtimeReflection.spine.binding.profileAuto.label",
            Description = "Profile-scoped binding with default Auto save policy.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.binding.profileAuto.description",
            Order = -20)]
        [ModSettingsBinding(
            Source = ModSettingsReflectionBindingSource.Profile,
            DataKey = "runtime_reflection_spine_profile_auto")]
        public bool ProfileAutoSaveDemo { get; set; }

        [ModSettingsIntSlider("spine_callback_manual", "spine", 0, 10,
            Label = "Callback manual-save value",
            LabelKey = "ritsulib.runtimeReflection.spine.binding.callbackManual.label",
            Description = "Callback source with manual save policy. Change value, then press Save callback value.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.binding.callbackManual.description",
            Order = -19)]
        [ModSettingsBinding(
            Source = ModSettingsReflectionBindingSource.Callback,
            SavePolicy = ModSettingsReflectionSavePolicy.Manual,
            DataKey = "runtime_reflection_spine_callback_manual",
            ReadUsing = nameof(ReadManualBindingValue),
            WriteUsing = nameof(WriteManualBindingValue),
            SaveUsing = nameof(SaveManualBindingValue))]
        public int CallbackManualSaveDemo { get; set; } = 3;

        [ModSettingsParagraph("spine_callback_manual_state", "spine",
            Description = "Current callback value and latest saved snapshot.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.binding.callbackState.description",
            Order = -18)]
        public string BuildManualBindingStateText()
        {
            return string.Format(
                L("ritsulib.runtimeReflection.spine.binding.callbackState.text", "Current: {0} | Saved: {1}"),
                CallbackManualSaveDemo,
                _manualBindingSavedValue);
        }

        [ModSettingsButton("spine_callback_manual_save", "spine",
            Label = "Persist callback value",
            LabelKey = "ritsulib.runtimeReflection.spine.binding.callbackSave.label",
            ButtonText = "Save callback value",
            ButtonTextKey = "ritsulib.runtimeReflection.spine.binding.callbackSave.button",
            Description = "Calls the callback save method once.",
            DescriptionKey = "ritsulib.runtimeReflection.spine.binding.callbackSave.description",
            Order = -17)]
        public void SaveManualBindingFromButton()
        {
            SaveManualBindingValue();
        }

        [ModSettingsCustomEntry("ironclad_spine_preview", "spine",
            Label = "Vanilla Character Spine Preview",
            LabelKey = "ritsulib.runtimeReflection.spine.entry.label",
            Description = "This control directly instantiates the selected vanilla character visuals scene.",
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
            var characterPicker = new OptionButton
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(260, 44),
            };
            AddCharacterOptions(characterPicker, availableCharacters);
            characterRow.AddChild(characterPicker);
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
                Text = L("ritsulib.runtimeReflection.spine.animations.title", "Available Animations"),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            root.AddChild(animationsTitle);

            var animationsList = new ItemList
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new(0, 220),
                SelectMode = ItemList.SelectModeEnum.Single,
            };
            root.AddChild(animationsList);

            NCreatureVisuals? currentVisuals = null;
            var currentCharacter = availableCharacters[0];

            characterPicker.ItemSelected += idx =>
            {
                var selectedIndex = characterPicker.GetItemId((int)idx);
                if (selectedIndex < 0 || selectedIndex >= availableCharacters.Count)
                    return;
                currentCharacter = availableCharacters[selectedIndex];
                RefreshPreview();
            };

            animationsList.ItemSelected += idx =>
            {
                if (currentVisuals?.SpineBody == null)
                    return;
                var animationName = animationsList.GetItemText((int)idx);
                currentVisuals.SpineAnimation.SetAnimation(animationName);
            };

            RefreshPreview();
            return root;

            void RefreshPreview()
            {
                foreach (var child in viewport.GetChildren())
                    child.QueueFree();

                currentVisuals = currentCharacter.CreateVisuals();
                currentVisuals.Position = new(320, 310);
                viewport.AddChild(currentVisuals);

                var animationNames = EnumerateAnimations(currentVisuals);
                animationsList.Clear();
                foreach (var animationName in animationNames)
                    animationsList.AddItem(animationName);

                if (animationNames.Count == 0)
                    return;

                var preferred = animationNames.FirstOrDefault(name =>
                    string.Equals(name, "idle_loop", StringComparison.OrdinalIgnoreCase));
                var selected = preferred ?? animationNames[0];
                var selectedIndex = animationNames.IndexOf(selected);
                animationsList.Select(selectedIndex);
                currentVisuals.SpineAnimation.SetAnimation(selected);
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

        private static void AddCharacterOptions(OptionButton picker, IReadOnlyList<CharacterModel> characters)
        {
            for (var i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                picker.AddItem(ResolveCharacterName(character), i);
            }

            picker.Select(0);
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
            return CallbackManualSaveDemo;
        }

        private void WriteManualBindingValue(int value)
        {
            CallbackManualSaveDemo = value;
        }

        private void SaveManualBindingValue()
        {
            _manualBindingSavedValue = CallbackManualSaveDemo;
        }
    }
}
