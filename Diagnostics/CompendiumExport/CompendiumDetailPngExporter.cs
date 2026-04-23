using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Diagnostics.CardExport;
using STS2RitsuLib.Settings;

namespace STS2RitsuLib.Diagnostics.CompendiumExport
{
    /// <summary>
    ///     Renders compendium-style detail panels in an offscreen <see cref="SubViewport" />: relic inspect
    ///     (same <c>inspect_relic_screen</c> <c>Popup</c> subtree) and potion lab focus view
    ///     (<see cref="NPotion" /> at 1.2x + hover tips, matching focus + tips layout).
    /// </summary>
    public static class CompendiumDetailPngExporter
    {
        private const string InspectRelicScene = "screens/inspect_relic_screen/inspect_relic_screen";
        private const string PotionScenePath = "potions/potion";

        private const float PotionLabFocusScale = 1.2f;
        private const float PotionToTipsGap = 32f;
        private const float ExportViewportFramePad = 6f;
        private const int InitialLayoutViewportSide = 4096;
        private const float RelicToHoverColumnSep = 18f;
        private const float NPotionExportMinSide = 60f;
        private const float RelicPopupBaseWidth = 864f;
        private const float RelicPopupBaseHeight = 864f;
        private const float RelicPopupHorizontalTrimEachSide = 30f;
        private const float PotionIconDownshiftPx = 10f;

        private const int FramesAfterHostAdded = 2;
        private const int FramesAfterLayout = 2;
        private const int FramesAfterRefCardVisuals = 2;
        private const int FramesAfterRenderOnce = 5;
        private const int FramesAfterSaveBeforeTeardown = 1;
        private const int FramesFlushBeforeSyncDispose = 1;
        private const int FramesAfterSyncTeardown = 2;
        private const int FramesBetweenItems = 1;
        private const int FramesBeforeRetry = 8;
        private const int MaxCaptureAttemptsPerFile = 2;

        private static readonly Vector2 RelicInspectMinUnscaledFloor = Vector2.Zero;
        private static readonly Vector2 PotionRowMinUnscaledFloor = Vector2.Zero;

        /// <summary>
        ///     Starts a batch export for the requested <see cref="CompendiumPngExportRequest" />.
        /// </summary>
        public static void BeginExport(CompendiumPngExportRequest request, Action<string>? log = null)
        {
            if (NGame.Instance == null)
            {
                log?.Invoke("Cannot export: the game is not loaded yet.");
                return;
            }

            var req = request;
            var lg = log;
            Callable.From(() => RunExportOnMainThreadEntry(req, lg)).CallDeferred();
        }

        private static async void RunExportOnMainThreadEntry(CompendiumPngExportRequest request, Action<string>? log)
        {
            try
            {
                await RunExportAsync(request, log);
            }
            catch (Exception ex)
            {
                log?.Invoke($"Compendium detail export stopped: {ex.Message}");
                GD.PushError($"Compendium detail PNG export: {ex}");
            }
        }

        private static async Task RunExportAsync(CompendiumPngExportRequest request, Action<string>? log)
        {
            if (!CardPngExporter.TryValidateExportEnvironment(out var err))
            {
                log?.Invoke(err);
                return;
            }

            if (request is { Relics: false, Potions: false })
            {
                log?.Invoke("Nothing to export: enable Relics and/or Potions in the request.");
                return;
            }

            var scale = Mathf.Max(0.25f, (float)request.Scale);
            var outDir = ProjectSettings.GlobalizePath(request.OutputDirectory.Trim());
            try
            {
                Directory.CreateDirectory(outDir);
            }
            catch (Exception ex)
            {
                log?.Invoke($"Could not create output folder: {ex.Message}");
                return;
            }

            var tree = NGame.Instance!.GetTree();
            if (tree == null)
            {
                log?.Invoke("Scene tree is not available.");
                return;
            }

            var idFilter = string.IsNullOrEmpty(request.IdFilterSubstring)
                ? null
                : request.IdFilterSubstring;
            var includeRelicHover = request.IncludeRelicHoverTips;
            IReadOnlyList<RelicModel> relicList =
                request.Relics ? BuildRelicExportList(idFilter) : Array.Empty<RelicModel>();
            IReadOnlyList<PotionModel> potionList =
                request.Potions ? BuildPotionExportList(idFilter) : Array.Empty<PotionModel>();
            if (request.Potions && potionList.Count == 0)
            {
                var d = GetPotionModelDbDiagnostics();
                log?.Invoke(
                    "Potion export: no entries after filter (empty ModelDb or filter matched nothing). " +
                    $"AllPotions enumerations: {d.allPotions} (including deprecated {d.deprecated}), " +
                    $"same source as the in-game potion lab; pool slot iterations: {d.poolEntries}.");
            }

            if (request.Relics)
                log?.Invoke($"Relic export: {relicList.Count} item(s) after filter.");

            var steps = relicList.Count + potionList.Count;
            CompendiumPngExportProgressOverlay? progressUi = null;
            var title = request is { Relics: true, Potions: true }
                ? ModSettingsLocalization.Get("ritsulib.compendiumPngExport.progress.title.both",
                    "Exporting relic and potion detail images…")
                : request.Relics
                    ? ModSettingsLocalization.Get("ritsulib.compendiumPngExport.progress.title.relics",
                        "Exporting relic detail images…")
                    : ModSettingsLocalization.Get("ritsulib.compendiumPngExport.progress.title.potions",
                        "Exporting potion detail images…");

            CompendiumPngExportSession.ResetForNewRun();
            try
            {
                progressUi = CompendiumPngExportProgressOverlay.Attach(NGame.Instance, Math.Max(1, steps), title);
                progressUi.SetProgress(0, null);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

                var done = 0;
                var saved = 0;
                var failed = 0;
                var userStopped = false;

                if (request.Relics)
                    foreach (var relic in relicList)
                    {
                        if (CompendiumPngExportSession.IsStopRequested)
                        {
                            userStopped = true;
                            break;
                        }

                        progressUi.SetProgress(done, relic.Id.Entry);
                        var fileName = SanitizeFilePart(relic.Id.Entry) + "_relic.png";
                        var filePath = Path.Combine(outDir, fileName);
                        if (await TryCaptureRelicWithRetriesAsync(tree, relic, filePath, scale, includeRelicHover, log,
                                fileName))
                        {
                            saved++;
                            log?.Invoke($"Saved {fileName}");
                        }
                        else
                        {
                            failed++;
                            log?.Invoke($"Could not save {fileName} after {MaxCaptureAttemptsPerFile} attempts.");
                        }

                        done++;
                        progressUi.SetProgress(done, relic.Id.Entry);
                        await WaitMainThreadFrames(tree, FramesBetweenItems);
                    }

                if (!userStopped && request.Potions)
                {
                    if (potionList.Count > 0)
                        log?.Invoke(
                            $"Potion export: processing {potionList.Count} potion(s) from all potion pools.");

                    foreach (var potion in potionList)
                    {
                        if (CompendiumPngExportSession.IsStopRequested)
                        {
                            userStopped = true;
                            break;
                        }

                        progressUi.SetProgress(done, potion.Id.Entry);
                        var fileName = SanitizeFilePart(potion.Id.Entry) + "_potion.png";
                        var filePath = Path.Combine(outDir, fileName);
                        if (await TryCapturePotionWithRetriesAsync(tree, potion, filePath, scale, log, fileName))
                        {
                            saved++;
                            log?.Invoke($"Saved {fileName}");
                        }
                        else
                        {
                            failed++;
                            log?.Invoke($"Could not save {fileName} after {MaxCaptureAttemptsPerFile} attempts.");
                        }

                        done++;
                        progressUi.SetProgress(done, potion.Id.Entry);
                        await WaitMainThreadFrames(tree, FramesBetweenItems);
                    }
                }

                progressUi.SetProgress(Math.Max(1, steps), null);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                log?.Invoke(
                    userStopped
                        ? ModSettingsLocalization.Get("ritsulib.compendiumPngExport.stopped", "Export stopped by user.")
                        : $"Compendium detail export finished. {saved} file(s) saved, {failed} failed. Output: {outDir}");
            }
            finally
            {
                CompendiumPngExportSession.ResetForNewRun();
                progressUi?.Detach();
            }
        }

        private static async Task WaitMainThreadFrames(SceneTree tree, int count)
        {
            for (var i = 0; i < count; i++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        private static async Task<bool> TryCaptureRelicWithRetriesAsync(SceneTree tree, RelicModel relic, string path,
            float scale, bool includeRelicHover, Action<string>? log, string fileLabel)
        {
            for (var attempt = 1; attempt <= MaxCaptureAttemptsPerFile; attempt++)
            {
                var prefix = MaxCaptureAttemptsPerFile > 1 ? $"[{attempt}/{MaxCaptureAttemptsPerFile}] " : null;
                if (await TryCaptureRelicAsync(tree, relic, path, scale, includeRelicHover, log, prefix, fileLabel))
                    return true;
                if (attempt >= MaxCaptureAttemptsPerFile)
                    break;
                log?.Invoke(
                    $"{prefix}Capture failed for {fileLabel}; waiting {FramesBeforeRetry} frames before retry.");
                await WaitMainThreadFrames(tree, FramesBeforeRetry);
            }

            return false;
        }

        private static async Task<bool> TryCapturePotionWithRetriesAsync(SceneTree tree, PotionModel potion,
            string path,
            float scale, Action<string>? log, string fileLabel)
        {
            for (var attempt = 1; attempt <= MaxCaptureAttemptsPerFile; attempt++)
            {
                var prefix = MaxCaptureAttemptsPerFile > 1 ? $"[{attempt}/{MaxCaptureAttemptsPerFile}] " : null;
                if (await TryCapturePotionAsync(tree, potion, path, scale, log, prefix, fileLabel))
                    return true;
                if (attempt >= MaxCaptureAttemptsPerFile)
                    break;
                log?.Invoke(
                    $"{prefix}Capture failed for {fileLabel}; waiting {FramesBeforeRetry} frames before retry.");
                await WaitMainThreadFrames(tree, FramesBeforeRetry);
            }

            return false;
        }

        private static SubViewport? BuildRelicInspectViewport(RelicModel relic, float scale, bool includeHoverTips,
            out List<NCard> refHoverCards)
        {
            refHoverCards = [];
            if (TestMode.IsOn)
                return null;

            var scene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath(InspectRelicScene));
            var inst = scene.Instantiate<Control>();
            var popup = inst.GetNode<Control>("%Popup");
            popup.GetParent()!.RemoveChild(popup);
            inst.QueueFree();

            var rarity = relic.Rarity;
            var nameLabel = popup.GetNode<MegaLabel>("%RelicName");
            var rarityLabel = popup.GetNode<MegaLabel>("%Rarity");
            var desc = popup.GetNode<MegaRichTextLabel>("%RelicDescription");
            var flavor = popup.GetNode<MegaRichTextLabel>("%FlavorText");
            var relicImage = popup.GetNode<TextureRect>("%RelicImage");
            var frame = popup.GetNode<Control>("%Frame");
            if (frame.Material is ShaderMaterial mat)
                CompendiumDetailPngExportLayout.SetRelicInspectRarityFrame(rarity, mat);

            CompendiumDetailPngExportLayout.SetRelicInspectRarityLabelColor(rarity, rarityLabel);
            nameLabel.SetTextAutoSize(relic.Title.GetFormattedText());
            var rarityLoc = new LocString("gameplay_ui", "RELIC_RARITY." + rarity.ToString().ToUpperInvariant());
            rarityLabel.SetTextAutoSize(rarityLoc.GetFormattedText());
            relicImage.SelfModulate = Colors.White;
            desc.SetTextAutoSize(relic.DynamicDescription.GetFormattedText());
            desc.Visible = true;
            flavor.SetTextAutoSize(relic.Flavor.GetFormattedText());
            flavor.Visible = true;
            relicImage.Texture = relic.BigIcon;

            CompendiumDetailPngExportLayout.StripToTopLeftUnstretched(popup);
            popup.CustomMinimumSize = new(RelicPopupBaseWidth, RelicPopupBaseHeight);
            popup.Size = popup.CustomMinimumSize;
            var popupSlot = new Control
            {
                Name = "CompendiumRelicPopupSlot",
                ClipContents = true,
                CustomMinimumSize = new(
                    RelicPopupBaseWidth - 2f * RelicPopupHorizontalTrimEachSide,
                    RelicPopupBaseHeight),
            };
            popup.Position = new(-RelicPopupHorizontalTrimEachSide, 0f);
            popupSlot.AddChild(popup);
            var scaleRoot = new Control { Name = "CompendiumRelicScaleRoot" };
            scaleRoot.Position = new(ExportViewportFramePad, ExportViewportFramePad);
            scaleRoot.Scale = Vector2.One * scale;
            if (includeHoverTips)
            {
                var refCol = new VBoxContainer { Name = "RefHoverCol" };
                refCol.AddThemeConstantOverride("separation", 4);
                var textCol = new VBoxContainer { Name = "TextHoverCol" };
                textCol.AddThemeConstantOverride("separation", 4);
                var row = new HBoxContainer { Name = "CompendiumRelicExportRow" };
                row.AddThemeConstantOverride("separation", (int)RelicToHoverColumnSep);
                row.AddChild(refCol);
                row.AddChild(popupSlot);
                row.AddChild(textCol);
                CompendiumDetailPngExportLayout.StripToTopLeftUnstretched(row);
                CompendiumDetailPngExportLayout.PopulateHoverRow(textCol, refCol, relic.HoverTipsExcludingRelic,
                    refHoverCards);
                scaleRoot.AddChild(row);
            }
            else
            {
                scaleRoot.AddChild(popupSlot);
            }

            var contentRoot = new Control { Name = "ContentRoot" };
            contentRoot.AddChild(scaleRoot);
            var vp = new SubViewport
            {
                Name = "RitsuCompendiumRelicSubViewport",
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            vp.AddChild(contentRoot);
            return vp;
        }

        private static SubViewport? BuildPotionLabDetailViewport(PotionModel potion, float scale,
            out List<NCard> refCards)
        {
            refCards = [];
            if (TestMode.IsOn)
                return null;

            var path = SceneHelper.GetScenePath(PotionScenePath);
            var nPotion = PreloadManager.Cache.GetScene(path)
                .Instantiate<NPotion>();
            nPotion.Model = potion.ToMutable();
            CompendiumDetailPngExportLayout.ApplyLabPotionVisibleStyle(nPotion, potion);
            CompendiumDetailPngExportLayout.StripToTopLeftUnstretched(nPotion);
            nPotion.CustomMinimumSize = new(NPotionExportMinSide, NPotionExportMinSide);
            nPotion.Size = nPotion.CustomMinimumSize;
            nPotion.Position = new(0f, PotionIconDownshiftPx);
            nPotion.PivotOffset = nPotion.Size * 0.5f;
            nPotion.Scale = Vector2.One * PotionLabFocusScale;
            nPotion.Name = "CompendiumExportPotionNode";

            var row = CompendiumDetailPngExportLayout.CreateHoverRowForPotionExport();
            CompendiumDetailPngExportLayout.StripToTopLeftUnstretched(row);
            row.AddThemeConstantOverride("separation", (int)PotionToTipsGap);
            var (textCol, refCol) = CompendiumDetailPngExportLayout.CreatePotionHoverColumns();
            var slot = new Control { Name = "PotionSlot" };
            const float slotSide = NPotionExportMinSide * PotionLabFocusScale;
            slot.CustomMinimumSize = new(slotSide, slotSide);
            slot.AddChild(nPotion);
            row.AddChild(refCol);
            row.AddChild(slot);
            row.AddChild(textCol);
            CompendiumDetailPngExportLayout.PopulateHoverRow(textCol, refCol, potion.HoverTips, refCards);

            var scaleRoot = new Control { Name = "CompendiumPotionScaleRoot" };
            scaleRoot.Position = new(ExportViewportFramePad, ExportViewportFramePad);
            scaleRoot.Scale = Vector2.One * scale;
            scaleRoot.AddChild(row);
            var contentRoot = new Control { Name = "ContentRoot" };
            contentRoot.AddChild(scaleRoot);
            var vp = new SubViewport
            {
                Name = "RitsuCompendiumPotionSubViewport",
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            vp.AddChild(contentRoot);
            return vp;
        }

        private static async Task<bool> TryCaptureRelicAsync(SceneTree tree, RelicModel relic, string absolutePath,
            float scale, bool includeRelicHover, Action<string>? log, string? logLinePrefix, string logFileTag)
        {
            var host = new Control { Name = "RitsuCompendiumRelicExportHost", Position = new(-5000, -5000) };
            var ok = false;
            var vp = BuildRelicInspectViewport(relic, scale, includeRelicHover, out var refList);
            if (vp == null)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: NRelic/inspect build failed (TestMode).");
                return false;
            }

            try
            {
                host.AddChild(vp);
                NGame.Instance!.AddChild(host);
                await WaitMainThreadFrames(tree, FramesAfterHostAdded);
                if (GodotObject.IsInstanceValid(host))
                    foreach (var c in refList.Where(GodotObject.IsInstanceValid))
                        CompendiumDetailPngExportLayout.ApplyRefCardExportVisuals(c);

                await WaitMainThreadFrames(tree, FramesAfterRefCardVisuals);
                ok = await ReadViewportPngAndSaveAsync(host, vp, log, logLinePrefix, logFileTag, tree, absolutePath,
                    RelicInspectMinUnscaledFloor);
            }
            catch (Exception ex)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: {ex.Message}");
                ok = false;
            }

            await WaitMainThreadFrames(tree, FramesFlushBeforeSyncDispose);
            if (GodotObject.IsInstanceValid(host))
                DisposeExportHost(host);
            await WaitMainThreadFrames(tree, FramesAfterSyncTeardown);
            return ok;
        }

        private static async Task<bool> TryCapturePotionAsync(SceneTree tree, PotionModel potion, string absolutePath,
            float scale, Action<string>? log, string? logLinePrefix, string logFileTag)
        {
            var host = new Control { Name = "RitsuCompendiumPotionExportHost", Position = new(-5000, -5000) };
            var ok = false;
            var vp = BuildPotionLabDetailViewport(potion, scale, out var refList);
            if (vp == null)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: potion build failed (TestMode).");
                return false;
            }

            try
            {
                host.AddChild(vp);
                NGame.Instance!.AddChild(host);
                await WaitMainThreadFrames(tree, FramesAfterHostAdded);
                if (GodotObject.IsInstanceValid(host))
                    foreach (var c in refList.Where(GodotObject.IsInstanceValid))
                        CompendiumDetailPngExportLayout.ApplyRefCardExportVisuals(c);

                await WaitMainThreadFrames(tree, FramesAfterRefCardVisuals);
                ok = await ReadViewportPngAndSaveAsync(host, vp, log, logLinePrefix, logFileTag, tree, absolutePath,
                    PotionRowMinUnscaledFloor);
            }
            catch (Exception ex)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: {ex.Message}");
                ok = false;
            }

            await WaitMainThreadFrames(tree, FramesFlushBeforeSyncDispose);
            if (GodotObject.IsInstanceValid(host))
                DisposeExportHost(host);
            await WaitMainThreadFrames(tree, FramesAfterSyncTeardown);
            return ok;
        }

        private static async Task<bool> ReadViewportPngAndSaveAsync(Control host, SubViewport vp, Action<string>? log,
            string? logLinePrefix, string logFileTag, SceneTree tree, string? savePath, Vector2 minimumUnscaledFloor,
            int layoutSettleFrames = 6)
        {
            if (!GodotObject.IsInstanceValid(host) || !GodotObject.IsInstanceValid(vp))
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: viewport or host was invalid before capture.");
                return false;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: save path is empty.");
                return false;
            }

            var cr0 = vp.GetNode<Control>("ContentRoot");
            if (cr0.GetChildCount() == 0)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: content root is empty.");
                return false;
            }

            if (cr0.GetChild(0) is not Control scaleRoot)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: export block is not a control.");
                return false;
            }

            var s = scaleRoot.Scale;
            if (s == Vector2.Zero)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: export scale is zero.");
                return false;
            }

            if (scaleRoot.GetChild(0) is not Control unscaled0)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: unscaled content is missing.");
                return false;
            }

            var init = new Vector2I(InitialLayoutViewportSide, InitialLayoutViewportSide);
            cr0.CustomMinimumSize = init;
            cr0.Size = init;
            vp.Size = init;

            CompendiumDetailPngExportLayout.StripToTopLeftUnstretched(unscaled0);
            unscaled0.ResetSize();
            scaleRoot.ResetSize();
            for (var j = 0; j < scaleRoot.GetChildCount(); j++)
                if (scaleRoot.GetChild(j) is Control cc)
                    cc.ResetSize();

            for (var f = 0; f < layoutSettleFrames; f++)
            {
                if (!GodotObject.IsInstanceValid(vp) || !GodotObject.IsInstanceValid(host))
                {
                    log?.Invoke(
                        $"{logLinePrefix}{logFileTag}: viewport or host became invalid while sizing layout.");
                    return false;
                }

                await WaitMainThreadFrames(tree, 1);
            }

            if (!GodotObject.IsInstanceValid(host) || !GodotObject.IsInstanceValid(vp))
            {
                log?.Invoke(
                    $"{logLinePrefix}{logFileTag}: viewport or host became invalid after layout settle.");
                return false;
            }

            var cr = vp.GetNode<Control>("ContentRoot");
            if (cr.GetChild(0) is not Control scaleRoot2) return false;
            if (scaleRoot2.GetChild(0) is not Control unscaled)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: unscaled content is missing (second pass).");
                return false;
            }

            CompendiumDetailPngExportLayout.StripToTopLeftUnstretched(unscaled);
            scaleRoot2.ResetSize();
            unscaled.ResetSize();
            for (var j = 0; j < scaleRoot2.GetChildCount(); j++)
                if (scaleRoot2.GetChild(j) is Control cc)
                    cc.ResetSize();

            await WaitMainThreadFrames(tree, FramesAfterLayout);

            var baseMs = unscaled.GetCombinedMinimumSize();
            var safeMs = new Vector2(
                Mathf.Max(baseMs.X, minimumUnscaledFloor.X),
                Mathf.Max(baseMs.Y, minimumUnscaledFloor.Y));
            scaleRoot2.CustomMinimumSize = safeMs;
            scaleRoot2.Size = safeMs;

            var w = Mathf.CeilToInt(2f * ExportViewportFramePad + safeMs.X * s.X);
            var h = Mathf.CeilToInt(2f * ExportViewportFramePad + safeMs.Y * s.Y);
            cr.CustomMinimumSize = new(w, h);
            cr.Size = new(w, h);
            vp.Size = new(w, h);
            await WaitMainThreadFrames(tree, FramesAfterLayout);

            if (!GodotObject.IsInstanceValid(vp)) return false;
            vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            await WaitMainThreadFrames(tree, FramesAfterRenderOnce);
            if (!GodotObject.IsInstanceValid(vp)) return false;
            var tex = vp.GetTexture();
            if (tex == null)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: viewport texture was null.");
                return false;
            }

            using var imageFromTex = tex.GetImage();
            if (imageFromTex == null)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: viewport image was null.");
                return false;
            }

            using var image = imageFromTex.Duplicate() as Image;
            if (image == null)
            {
                log?.Invoke($"{logLinePrefix}{logFileTag}: duplicate image was null.");
                return false;
            }

            var err = image.SavePng(savePath!);
            if (err == Error.Ok)
            {
                await WaitMainThreadFrames(tree, FramesAfterSaveBeforeTeardown);
                return true;
            }

            log?.Invoke($"{logLinePrefix}{logFileTag}: SavePng failed ({err}, code {(int)err}).");
            return false;
        }

        private static void DisposeExportHost(Control host)
        {
            if (!GodotObject.IsInstanceValid(host))
                return;
            host.GetParent()?.RemoveChildSafely(host);
            var postOrder = new List<Node>();
            CollectPostOrder(host);
            foreach (var node in postOrder.Where(GodotObject.IsInstanceValid))
                if (node is NCard nCard)
                    nCard.QueueFreeSafelyNoPool();
                else
                    node.QueueFreeSafelyNoPool();

            return;

            void CollectPostOrder(Node n)
            {
                foreach (var c in n.GetChildren())
                    CollectPostOrder(c);
                postOrder.Add(n);
            }
        }

        private static List<RelicModel> BuildRelicExportList(string? idFilter)
        {
            return ModelDb.AllRelics
                .Where(r => r is not DeprecatedRelic)
                .Where(r => idFilter == null || r.Id.Entry.Contains(idFilter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Id.Entry, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<PotionModel> BuildPotionExportList(string? idFilter)
        {
            return ModelDb.AllPotions
                .Where(p => p is not DeprecatedPotion)
                .Where(p => idFilter == null || p.Id.Entry.Contains(idFilter, StringComparison.OrdinalIgnoreCase))
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderBy(p => p.Id.Entry, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static (int allPotions, int deprecated, int poolEntries) GetPotionModelDbDiagnostics()
        {
            var all = 0;
            var dep = 0;
            foreach (var p in ModelDb.AllPotions)
            {
                all++;
                if (p is DeprecatedPotion) dep++;
            }

            var n = ModelDb.AllPotionPools.SelectMany(pool => pool.AllPotions).Count();
            return (all, dep, n);
        }

        private static string SanitizeFilePart(string entry)
        {
            var s = Path.GetInvalidFileNameChars().Aggregate(entry, (current, c) => current.Replace(c, '_'));
            return string.IsNullOrEmpty(s) ? "export" : s;
        }
    }
}
