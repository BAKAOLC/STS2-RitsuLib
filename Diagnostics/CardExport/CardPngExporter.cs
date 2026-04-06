using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace STS2RitsuLib.Diagnostics.CardExport
{
    /// <summary>
    ///     Batch-exports registered cards to PNG via <see cref="NCard" /> (library models; no run required).
    /// </summary>
    public static class CardPngExporter
    {
        private const float HoverTipTargetWidth = 360f;
        private const float HoverTipVerticalGap = 5f;
        private const string HoverTipScenePath = "res://scenes/ui/hover_tip.tscn";
        private const string CardHoverTipScenePath = "res://scenes/ui/card_hover_tip.tscn";
        private const string HoverTipDebuffMaterialPath = "res://materials/ui/hover_tip_debuff.tres";

        private const int HoverRowColumnSeparation = 0;
        private const int RefCardHoverTipVerticalGap = 4;
        private const float CardHoverTipSceneCardScale = 0.75f;

        private const float CardExportHalfExtentX = 190f;

        private const float CardExportHalfExtentY = 240f;

        private const float ExportViewportFramePad = 6f;

        private const int FramesAfterHostAdded = 2;
        private const int FramesAfterSyncVisualApply = 2;
        private const int FramesAfterHoverColumnLayout = 2;
        private const int FramesAfterHoverResize = 2;
        private const int FramesAfterRenderOnce = 5;
        private const int FramesAfterSaveBeforeTeardown = 1;
        private const int FramesFlushBeforeSyncDispose = 1;
        private const int FramesAfterSyncTeardown = 2;

        private const int FramesBetweenCards = 1;
        private const int FramesBetweenVariants = 1;
        private const int FramesBeforeRetry = 8;
        private const int MaxCaptureAttemptsPerFile = 2;

        private const string CardScenePath = "res://scenes/cards/card.tscn";

        /// <summary>
        ///     Starts batch PNG export for <paramref name="request" />.
        /// </summary>
        public static void BeginExport(CardPngExportRequest request, Player? issuingPlayer, Action<string>? log = null)
        {
            if (NGame.Instance == null)
            {
                log?.Invoke("Cannot export: the game is not loaded yet.");
                return;
            }

            var req = request;
            var player = issuingPlayer;
            var lg = log;
            Callable.From(() => RunExportOnMainThreadEntry(req, player, lg)).CallDeferred();
        }

        private static async void RunExportOnMainThreadEntry(CardPngExportRequest request, Player? issuingPlayer,
            Action<string>? log)
        {
            try
            {
                await RunExportAsync(request, issuingPlayer, log);
            }
            catch (Exception ex)
            {
                log?.Invoke($"Export stopped: {ex.Message}");
                GD.PushError($"Card PNG export: {ex}");
            }
        }

        /// <summary>
        ///     Whether export can start (game loaded and cards can be instantiated).
        /// </summary>
        public static bool TryValidateExportEnvironment(out string error)
        {
            if (NGame.Instance == null)
            {
                error = "Game is not ready. Open the main menu or enter a run, then try again.";
                return false;
            }

            if (TestMode.IsOn)
            {
                error = "Card preview cannot be created in this session.";
                return false;
            }

            error = "";
            return true;
        }

        /// <summary>
        ///     Back-compat shim: always sets <paramref name="runState" /> and <paramref name="player" /> to null; use
        ///     <see cref="TryValidateExportEnvironment" /> for new code.
        /// </summary>
        public static bool TryResolveContext(Player? _, out RunState? runState, out Player? player, out string error)
        {
            runState = null;
            player = null;
            return TryValidateExportEnvironment(out error);
        }

        private static async Task RunExportAsync(CardPngExportRequest request, Player? _, Action<string>? log)
        {
            if (!TryValidateExportEnvironment(out var err))
            {
                log?.Invoke(err);
                return;
            }

            var scale = Mathf.Max(0.25f, request.Scale);
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

            var cards = ModelDb.AllCards
                .Where(c => c is not DeprecatedCard)
                .Where(c => request.IncludeCardsHiddenFromLibrary || c.ShouldShowInCardLibrary)
                .Where(c => string.IsNullOrEmpty(request.IdFilterSubstring) ||
                            c.Id.Entry.Contains(request.IdFilterSubstring, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Id.Entry, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var totalSteps = CountTotalExportSteps(cards, request);
            CardPngExportProgressOverlay? progressUi = null;
            try
            {
                progressUi = CardPngExportProgressOverlay.Attach(NGame.Instance!, totalSteps);
                progressUi.SetProgress(0, null);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

                var exportedBase = 0;
                var savedFiles = 0;
                var failures = 0;
                var stepIndex = 0;

                foreach (var canonical in cards)
                {
                    if (request.MaxBaseCards > 0 && exportedBase >= request.MaxBaseCards)
                        break;

                    try
                    {
                        progressUi.SetProgress(stepIndex, canonical.Id.Entry);

                        var baseName = SanitizeFilePart(canonical.Id.Entry) + "_base.png";
                        var basePath = Path.Combine(outDir, baseName);
                        if (await TryCaptureWithRetriesAsync(tree, canonical, basePath, request, scale, log, baseName))
                        {
                            savedFiles++;
                            log?.Invoke($"Saved {baseName}");
                        }
                        else
                        {
                            failures++;
                            log?.Invoke(
                                $"Could not save {baseName} after {MaxCaptureAttemptsPerFile} attempts.");
                        }

                        exportedBase++;
                        stepIndex++;
                        progressUi.SetProgress(stepIndex, canonical.Id.Entry);

                        if (request.IncludeUpgradedVariants && canonical.IsUpgradable)
                        {
                            await WaitMainThreadFrames(tree, FramesBetweenVariants);

                            var upgraded = canonical.ToMutable();
                            upgraded.UpgradeInternal();
                            var upName = SanitizeFilePart(canonical.Id.Entry) + "_upgraded.png";
                            var upPath = Path.Combine(outDir, upName);
                            progressUi.SetProgress(stepIndex, $"{canonical.Id.Entry} (upgraded)");
                            if (await TryCaptureWithRetriesAsync(tree, upgraded, upPath, request, scale, log, upName))
                            {
                                savedFiles++;
                                log?.Invoke($"Saved {upName}");
                            }
                            else
                            {
                                failures++;
                                log?.Invoke(
                                    $"Could not save {upName} after {MaxCaptureAttemptsPerFile} attempts.");
                            }

                            stepIndex++;
                            progressUi.SetProgress(stepIndex, canonical.Id.Entry);
                        }
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        log?.Invoke($"{canonical.Id.Entry}: {ex.Message}");
                    }

                    await WaitMainThreadFrames(tree, FramesBetweenCards);
                }

                progressUi.SetProgress(totalSteps, null);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

                log?.Invoke(
                    $"Finished. {savedFiles} file(s) saved, {failures} failed. Base cards: {exportedBase}. Output: {outDir}");
            }
            finally
            {
                progressUi?.Detach();
            }
        }

        private static int CountTotalExportSteps(IReadOnlyList<CardModel> cards, CardPngExportRequest request)
        {
            var steps = 0;
            var baseCount = 0;
            foreach (var c in cards)
            {
                if (request.MaxBaseCards > 0 && baseCount >= request.MaxBaseCards)
                    break;
                steps++;
                baseCount++;
                if (request.IncludeUpgradedVariants && c.IsUpgradable)
                    steps++;
            }

            return Math.Max(1, steps);
        }

        private static async Task WaitMainThreadFrames(SceneTree tree, int count)
        {
            for (var i = 0; i < count; i++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        private static async Task<bool> TryCaptureWithRetriesAsync(SceneTree tree, CardModel card, string absolutePath,
            CardPngExportRequest request, float scale, Action<string>? log, string fileLabel)
        {
            for (var attempt = 1; attempt <= MaxCaptureAttemptsPerFile; attempt++)
            {
                var prefix = MaxCaptureAttemptsPerFile > 1 ? $"[{attempt}/{MaxCaptureAttemptsPerFile}] " : null;
                if (await TryCaptureAsync(tree, card, absolutePath, request, scale, log, prefix, fileLabel))
                    return true;
                if (attempt >= MaxCaptureAttemptsPerFile)
                    break;
                log?.Invoke(
                    $"{prefix}Capture failed for {fileLabel}; waiting {FramesBeforeRetry} frames before retry.");
                await WaitMainThreadFrames(tree, FramesBeforeRetry);
            }

            return false;
        }

        private static async Task<bool> TryCaptureAsync(SceneTree tree, CardModel card, string absolutePath,
            CardPngExportRequest request, float scale, Action<string>? log, string? logLinePrefix, string logFileTag)
        {
            var host = new Control { Name = "RitsuCardPngExportHost", Position = new(-5000, -5000) };
            BuiltCaptureViewport? built = null;
            var ok = false;
            try
            {
                var viewportBuilt = BuildCaptureViewport(card, request, scale);
                built = viewportBuilt;
                host.AddChild(viewportBuilt.Viewport);
                NGame.Instance!.AddChild(host);

                await WaitMainThreadFrames(tree, FramesAfterHostAdded);

                if (GodotObject.IsInstanceValid(host))
                    ApplyAllExportCardVisuals(viewportBuilt);

                await WaitMainThreadFrames(tree, FramesAfterSyncVisualApply);

                if (viewportBuilt.HoverRow != null && GodotObject.IsInstanceValid(viewportBuilt.HoverRow))
                {
                    var row = viewportBuilt.HoverRow;
                    if (GodotObject.IsInstanceValid(row))
                        row.QueueSort();
                }

                await WaitMainThreadFrames(tree, FramesAfterHoverColumnLayout);

                if (viewportBuilt.HoverRow != null && GodotObject.IsInstanceValid(viewportBuilt.HoverRow))
                    ResizeViewportToHoverRow(viewportBuilt);

                await WaitMainThreadFrames(tree, FramesAfterHoverResize);

                if (GodotObject.IsInstanceValid(viewportBuilt.Viewport))
                {
                    viewportBuilt.Viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
                    await WaitMainThreadFrames(tree, FramesAfterRenderOnce);

                    if (GodotObject.IsInstanceValid(viewportBuilt.Viewport))
                    {
                        var tex = viewportBuilt.Viewport.GetTexture();
                        if (tex != null)
                        {
                            using var imageFromTex = tex.GetImage();
                            if (imageFromTex != null)
                            {
                                using var image = imageFromTex.Duplicate() as Image;
                                if (image != null)
                                {
                                    var saveErr = image.SavePng(absolutePath);
                                    ok = saveErr == Error.Ok;
                                    if (ok)
                                        await WaitMainThreadFrames(tree, FramesAfterSaveBeforeTeardown);
                                    else
                                        log?.Invoke(
                                            $"{logLinePrefix}{logFileTag}: SavePng failed ({saveErr}, code {(int)saveErr}).");
                                }
                                else
                                {
                                    log?.Invoke($"{logLinePrefix}{logFileTag}: duplicate image was null.");
                                }
                            }
                            else
                            {
                                log?.Invoke($"{logLinePrefix}{logFileTag}: viewport image was null.");
                            }
                        }
                        else
                        {
                            log?.Invoke($"{logLinePrefix}{logFileTag}: viewport texture was null.");
                        }
                    }
                    else
                    {
                        log?.Invoke(
                            $"{logLinePrefix}{logFileTag}: viewport became invalid before reading pixels.");
                    }
                }
                else
                {
                    log?.Invoke($"{logLinePrefix}{logFileTag}: viewport was freed before capture.");
                }
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

        /// <summary>
        ///     Frees the capture subtree. Export uses non-pooled <see cref="NCard" /> instances only — never
        ///     <see cref="NCard.Create" /> / <c>NodePool</c> — so teardown does not race the in-game card pool.
        /// </summary>
        private static void DisposeExportHost(Control host)
        {
            if (!GodotObject.IsInstanceValid(host))
                return;

            host.GetParent()?.RemoveChildSafely(host);

            var postOrder = new List<Node>();

            void CollectPostOrder(Node n)
            {
                foreach (var c in n.GetChildren())
                    CollectPostOrder(c);
                postOrder.Add(n);
            }

            CollectPostOrder(host);

            foreach (var node in postOrder)
            {
                if (!GodotObject.IsInstanceValid(node))
                    continue;

                if (node is NCard nCard)
                {
                    nCard.QueueFreeSafelyNoPool();
                    continue;
                }

                node.QueueFreeSafelyNoPool();
            }
        }

        private static Vector2 CardExportContentMinInNCardLocal()
        {
            return new(-CardExportHalfExtentX, -CardExportHalfExtentY);
        }

        private static Vector2I ComputePaddedCardViewportSize(float scale)
        {
            var w = Mathf.CeilToInt(2f * CardExportHalfExtentX * scale);
            var h = Mathf.CeilToInt(2f * CardExportHalfExtentY * scale);
            return new(w, h);
        }

        private static int ComputeHoverRowCardSlotWidth(float scale)
        {
            return Mathf.CeilToInt(2f * CardExportHalfExtentX * scale);
        }

        private static void ApplyAllExportCardVisuals(BuiltCaptureViewport built)
        {
            if (built.MainCard != null && GodotObject.IsInstanceValid(built.MainCard))
                RefreshMainExportCardVisuals(built.MainCard);
            foreach (var n in built.RefHoverTipCards)
                if (GodotObject.IsInstanceValid(n))
                    ApplyCardLibraryStyleExportVisuals(n, PileType.Deck);
        }

        private static void ResizeViewportToHoverRow(BuiltCaptureViewport built)
        {
            var row = built.HoverRow;
            if (row == null || !GodotObject.IsInstanceValid(row))
                return;
            if (!GodotObject.IsInstanceValid(built.Viewport) || !GodotObject.IsInstanceValid(built.Root))
                return;

            var f = Mathf.RoundToInt(ExportViewportFramePad);
            var sz = row.GetCombinedMinimumSize();
            var w = Mathf.CeilToInt(f * 2 + sz.X);
            var h = Mathf.CeilToInt(f * 2 + sz.Y);
            built.Viewport.Size = new(w, h);
            built.Root.CustomMinimumSize = new(w, h);
            built.Root.Size = new(w, h);
        }

        private static BuiltCaptureViewport BuildCaptureViewport(CardModel card, CardPngExportRequest request,
            float scale)
        {
            var padded = ComputePaddedCardViewportSize(scale);
            var cardW = padded.X;
            var cardH = padded.Y;
            var refHoverNcArds = new List<NCard>();

            var vp = new SubViewport
            {
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };

            var root = new Control { Name = "CardExportRoot" };

            if (request.CaptureMode == CardPngExportCaptureMode.CardOnly)
            {
                var frame = Mathf.RoundToInt(ExportViewportFramePad);
                var vpW = cardW + frame * 2;
                var vpH = cardH + frame * 2;
                vp.Size = new(vpW, vpH);
                root.CustomMinimumSize = new(vpW, vpH);
                root.Size = new(vpW, vpH);

                var nCard = CreateAndLayoutCard(card, scale, new(frame, frame));
                root.AddChild(nCard);

                vp.AddChild(root);

                return new()
                {
                    Viewport = vp,
                    Root = root,
                    Background = null,
                    HoverRow = null,
                    MainCard = nCard,
                    RefHoverTipCards = refHoverNcArds,
                    CardWidth = cardW,
                    CardHeight = cardH,
                };
            }

            {
                var frame = Mathf.RoundToInt(ExportViewportFramePad);
                var cardSlotW = ComputeHoverRowCardSlotWidth(scale);
                var initialW = cardSlotW + 800;
                var initialH = cardH + frame * 2 + 120;
                vp.Size = new(initialW, initialH);
                root.CustomMinimumSize = new(initialW, initialH);
                root.Size = new(initialW, initialH);

                var row = new HBoxContainer
                {
                    Position = new(frame, frame),
                    Name = "HoverRow",
                };
                row.AddThemeConstantOverride("separation", HoverRowColumnSeparation);
                root.AddChild(row);

                var refCardsColumn = new VBoxContainer { Name = "RefCardHoverTipsColumn" };
                refCardsColumn.AddThemeConstantOverride("separation", RefCardHoverTipVerticalGap);
                row.AddChild(refCardsColumn);

                var cardSlot = new Control
                {
                    CustomMinimumSize = new(cardSlotW, cardH),
                    Name = "CardSlot",
                };
                row.AddChild(cardSlot);
                var nCard = CreateAndLayoutCard(card, scale, Vector2.Zero);
                cardSlot.AddChild(nCard);

                var textTipsColumn = new VBoxContainer { Name = "TextTipsColumn" };
                textTipsColumn.AddThemeConstantOverride("separation", Mathf.RoundToInt(HoverTipVerticalGap));
                row.AddChild(textTipsColumn);

                PopulateHoverLayouts(refCardsColumn, textTipsColumn, card, refHoverNcArds);

                vp.AddChild(root);

                return new()
                {
                    Viewport = vp,
                    Root = root,
                    Background = null,
                    HoverRow = row,
                    MainCard = nCard,
                    RefHoverTipCards = refHoverNcArds,
                    CardWidth = cardW,
                    CardHeight = cardH,
                };
            }
        }

        private static NCard CreateAndLayoutCard(CardModel card, float scale, Vector2 captureTopLeftInParent)
        {
            var nCard = InstantiateExportNCard(card);
            nCard.Scale = Vector2.One * scale;
            var minLocal = CardExportContentMinInNCardLocal();
            nCard.Position = new(
                Mathf.Round(captureTopLeftInParent.X - minLocal.X * scale),
                Mathf.Round(captureTopLeftInParent.Y - minLocal.Y * scale));
            return nCard;
        }

        /// <summary>
        ///     Instantiates <c>card.tscn</c> without <see cref="NCard.Create" /> so export never competes with the shared
        ///     <c>NodePool</c>.
        /// </summary>
        private static NCard InstantiateExportNCard(CardModel card)
        {
            if (TestMode.IsOn)
                throw new InvalidOperationException("NCard export is unavailable in TestMode.");

            var nCard = PreloadManager.Cache.GetScene(CardScenePath)
                .Instantiate<NCard>();
            nCard.OnInstantiated();
            nCard.Model = card;
            nCard.Visibility = ModelVisibility.Visible;
            return nCard;
        }

        /// <summary>
        ///     Refreshes card visuals (requires the <see cref="NCard" /> to be in the scene tree).
        /// </summary>
        private static void RefreshMainExportCardVisuals(NCard nCard)
        {
            ApplyCardLibraryStyleExportVisuals(nCard, PileType.None);
        }

        private static void ApplyCardLibraryStyleExportVisuals(NCard nCard, PileType pileType)
        {
            nCard.UpdateVisuals(pileType, CardPreviewMode.Normal);
            if (nCard.Model is { IsUpgraded: true })
                nCard.ShowUpgradePreview();
        }

        private static void PopulateHoverLayouts(VBoxContainer refCardsColumn, VBoxContainer textTipsColumn,
            CardModel card, List<NCard> refHoverTipCardNodes)
        {
            foreach (var tip in IHoverTip.RemoveDupes(card.HoverTips))
                switch (tip)
                {
                    case HoverTip hoverTip:
                        AddTextHoverRow(textTipsColumn, hoverTip);
                        break;
                    case CardHoverTip refTip:
                        AddGameCardHoverTip(refCardsColumn, refTip, refHoverTipCardNodes);
                        break;
                }
        }

        private static void AddTextHoverRow(VBoxContainer column, HoverTip hoverTip)
        {
            var control = PreloadManager.Cache.GetScene(HoverTipScenePath)
                .Instantiate<Control>();
            column.AddChild(control);

            var title = control.GetNode<MegaLabel>("%Title");
            if (hoverTip.Title == null)
                title.Visible = false;
            else
                title.SetTextAutoSize(hoverTip.Title);

            var desc = control.GetNode<MegaRichTextLabel>("%Description");
            desc.Text = hoverTip.Description;
            desc.AutowrapMode = hoverTip.ShouldOverrideTextOverflow
                ? TextServer.AutowrapMode.Off
                : TextServer.AutowrapMode.WordSmart;

            control.GetNode<TextureRect>("%Icon").Texture = hoverTip.Icon;
            if (hoverTip.IsDebuff)
                control.GetNode<CanvasItem>("%Bg").Material =
                    PreloadManager.Cache.GetMaterial(HoverTipDebuffMaterialPath);

            control.CustomMinimumSize = new(HoverTipTargetWidth, 0f);
            control.ResetSize();
        }

        /// <summary>
        ///     Adds a referenced-card hover column like the in-game card tooltip.
        /// </summary>
        private static void AddGameCardHoverTip(VBoxContainer refCardsColumn, CardHoverTip refTip,
            List<NCard> refHoverTipCardNodes)
        {
            var control = PreloadManager.Cache.GetScene(CardHoverTipScenePath).Instantiate<Control>();
            refCardsColumn.AddChild(control);

            var s = CardHoverTipSceneCardScale;
            var padded = ComputePaddedCardViewportSize(s);
            control.CustomMinimumSize = new(padded.X, padded.Y);
            control.ResetSize();

            var node = control.GetNode<NCard>("%Card");
            node.Model = refTip.Card;
            node.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            node.Scale = Vector2.One * s;
            var minLocal = CardExportContentMinInNCardLocal();
            node.Position = new(
                Mathf.Round(-minLocal.X * s),
                Mathf.Round(-minLocal.Y * s));
            refHoverTipCardNodes.Add(node);
        }

        private static string SanitizeFilePart(string entry)
        {
            var s = Path.GetInvalidFileNameChars().Aggregate(entry, (current, c) => current.Replace(c, '_'));
            return string.IsNullOrEmpty(s) ? "card" : s;
        }

        private sealed class BuiltCaptureViewport
        {
            public required SubViewport Viewport { get; init; }
            public required Control Root { get; init; }
            public ColorRect? Background { get; init; }
            public HBoxContainer? HoverRow { get; init; }
            public NCard? MainCard { get; init; }
            public required List<NCard> RefHoverTipCards { get; init; }
            public int CardWidth { get; init; }
            public int CardHeight { get; init; }
        }
    }
}
