using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Nodes.Debug;
using STS2RitsuLib.Data;
using STS2RitsuLib.Patching;
using STS2RitsuLib.Patching.Models;
using GameDevConsole = MegaCrit.Sts2.Core.DevConsole.DevConsole;

namespace STS2RitsuLib.Diagnostics.Patches
{
    internal static class DevConsoleHistoryNavigationState
    {
        internal static readonly ConditionalWeakTable<NDevConsole, HistoryState> States = [];

        internal static readonly AccessTools.FieldRef<NDevConsole, GameDevConsole> DevConsoleField =
            AccessTools.FieldRefAccess<NDevConsole, GameDevConsole>("_devConsole");

        internal static readonly AccessTools.FieldRef<NDevConsole, LineEdit> InputBufferField =
            AccessTools.FieldRefAccess<NDevConsole, LineEdit>("_inputBuffer");

        internal static readonly AccessTools.FieldRef<NDevConsole, TabCompletionState> TabCompletionField =
            AccessTools.FieldRefAccess<NDevConsole, TabCompletionState>("_tabCompletion");

        internal static HistoryState Get(NDevConsole console)
        {
            return States.GetValue(console, static _ => new());
        }

        internal static bool TryGetNavigationFields(
            NDevConsole console,
            [NotNullWhen(true)] out GameDevConsole? devConsole,
            [NotNullWhen(true)] out LineEdit? inputBuffer)
        {
            devConsole = DevConsoleField(console);
            inputBuffer = InputBufferField(console);
            return devConsole != null && inputBuffer != null;
        }

        internal static TabCompletionState? GetTabCompletion(NDevConsole console)
        {
            return TabCompletionField(console);
        }

        internal static void ResetVisibilityState(NDevConsole console, bool clearInput)
        {
            var state = Get(console);
            state.Reset();

            if (clearInput && InputBufferField(console) is { } inputBuffer)
            {
                inputBuffer.Text = string.Empty;
                console.MoveInputCursorToEndOfLine();
            }

            if (DevConsoleField(console) is { } devConsole)
                devConsole.historyIndex = 0;
        }

        internal sealed class HistoryState
        {
            public int Cursor { get; set; } = -1;
            public string Draft { get; set; } = string.Empty;

            public void Reset()
            {
                Cursor = -1;
                Draft = string.Empty;
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Extends developer-console input with numeric keypad confirmation, shell-style history cursor movement,
    ///         and restoration of the in-progress draft.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         为开发者控制台输入增加数字小键盘确认、Shell 风格的历史记录光标移动，以及尚未提交输入草稿的恢复能力。
    ///     </para>
    /// </summary>
    internal sealed class DevConsoleHistoryNavigationInputPatch : IPatchMethod
    {
        private static readonly Action<NDevConsole> AcceptSelection =
            PrivateAccess.MethodDelegate<NDevConsole, Action<NDevConsole>>("AcceptSelection");

        private static readonly Action<NDevConsole> ProcessCommand =
            PrivateAccess.MethodDelegate<NDevConsole, Action<NDevConsole>>("ProcessCommand");

        public static string PatchId => "dev_console_history_navigation_input";
        public static bool IsCritical => false;

        public static string Description =>
            "DevConsole input: support numpad Enter, fix history navigation, and restore the in-progress draft";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDevConsole), "_Input", [typeof(InputEvent)])];
        }

        public static bool Prefix(NDevConsole __instance, InputEvent inputEvent)
        {
            if (inputEvent is not InputEventKey { Pressed: not false } keyEvent)
                return true;

            if (!__instance.Visible)
                return true;

            var tabCompletion = DevConsoleHistoryNavigationState.GetTabCompletion(__instance);
            if (tabCompletion == null)
                return true;

            if (IsNumpadEnter(keyEvent))
            {
                if (tabCompletion.InSelectionMode)
                    AcceptSelection(__instance);
                else
                    ProcessCommand(__instance);

                return false;
            }

            var historyPatchEnabled = RitsuLibSettingsStore.IsDevConsoleHistoryNavigationPatchEnabled();
            var clearInputOnVisibilityChange =
                RitsuLibSettingsStore.ShouldClearDevConsoleInputOnVisibilityChange();
            if (!historyPatchEnabled && !clearInputOnVisibilityChange)
                return true;

            if (WillCloseConsole(keyEvent, tabCompletion))
            {
                if (clearInputOnVisibilityChange)
                    DevConsoleHistoryNavigationState.ResetVisibilityState(
                        __instance,
                        true);

                return true;
            }

            if (!historyPatchEnabled)
                return true;

            if (keyEvent.Keycode is not Key.Up and not Key.Down)
                return true;

            if (tabCompletion.InSelectionMode)
                return true;

            if (!Navigate(__instance, keyEvent.Keycode))
                return true;

            __instance.GetViewport().SetInputAsHandled();
            return false;
        }

        private static bool IsNumpadEnter(InputEventKey keyEvent)
        {
            return keyEvent.Keycode == Key.KpEnter
                   || keyEvent.PhysicalKeycode == Key.KpEnter;
        }

        private static bool WillCloseConsole(InputEventKey keyEvent, TabCompletionState tabCompletion)
        {
            if (keyEvent.Keycode == Key.Escape && !tabCompletion.InSelectionMode)
                return true;

            return keyEvent.Keycode
                       is Key.Apostrophe
                       or Key.Asterisk
                       or Key.Asciicircum
                       or Key.Quoteleft
                   || (keyEvent.IsShiftPressed() && keyEvent.Keycode == Key.Key8);
        }

        private static bool Navigate(NDevConsole consoleNode, Key key)
        {
            if (!DevConsoleHistoryNavigationState.TryGetNavigationFields(
                    consoleNode,
                    out var devConsole,
                    out var inputBuffer))
                return false;

            var history = devConsole.history;
            var state = DevConsoleHistoryNavigationState.Get(consoleNode);

            if (history.Count == 0)
            {
                state.Reset();
                devConsole.historyIndex = 0;
                return true;
            }

            if (state.Cursor >= 0
                && (state.Cursor >= history.Count || inputBuffer.Text != history[state.Cursor]))
            {
                state.Cursor = -1;
                state.Draft = inputBuffer.Text;
            }

            var moved = key == Key.Up
                ? NavigateOlder(state, history, inputBuffer.Text)
                : NavigateNewer(state, history);

            if (!moved)
                return true;

            devConsole.historyIndex = Math.Max(state.Cursor, 0);
            var text = state.Cursor >= 0 ? history[state.Cursor] : state.Draft;
            if (DevConsoleHistoryNavigationState.GetTabCompletion(consoleNode) is { } tabCompletion)
                tabCompletion.ProgrammaticTextChange = true;
            inputBuffer.Text = text;
            consoleNode.MoveInputCursorToEndOfLine();
            return true;
        }

        private static bool NavigateOlder(
            DevConsoleHistoryNavigationState.HistoryState state,
            IReadOnlyList<string> history,
            string currentText)
        {
            if (state.Cursor < 0
                || state.Cursor >= history.Count
                || currentText != history[state.Cursor])
            {
                state.Draft = currentText;
                state.Cursor = 0;
                return true;
            }

            if (state.Cursor >= history.Count - 1)
                return false;

            var currentHistoryText = history[state.Cursor];
            do
            {
                state.Cursor++;
            } while (state.Cursor < history.Count - 1 && history[state.Cursor] == currentHistoryText);

            return true;
        }

        private static bool NavigateNewer(
            DevConsoleHistoryNavigationState.HistoryState state,
            IReadOnlyList<string> history)
        {
            switch (state.Cursor)
            {
                case < 0:
                    return false;
                case var cursor when cursor >= history.Count:
                case 0:
                    state.Cursor = -1;
                    return true;
            }

            var currentHistoryText = history[state.Cursor];
            do
            {
                state.Cursor--;
            } while (state.Cursor > 0 && history[state.Cursor] == currentHistoryText);

            return true;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Clears transient history-navigation state after the console processes a command.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         控制台处理命令后清除临时的历史记录导航状态。
    ///     </para>
    /// </summary>
    internal sealed class DevConsoleHistoryNavigationProcessCommandPatch : IPatchMethod
    {
        public static string PatchId => "dev_console_history_navigation_process_command";
        public static bool IsCritical => false;

        public static string Description =>
            "DevConsole history navigation: reset transient draft state after command submission";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDevConsole), "ProcessCommand")];
        }

        public static void Postfix(NDevConsole __instance)
        {
            if (!RitsuLibSettingsStore.IsDevConsoleHistoryNavigationPatchEnabled())
                return;

            DevConsoleHistoryNavigationState.Get(__instance).Reset();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Resets transient history browsing when the console is hidden.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         隐藏控制台时重置临时的历史记录浏览状态。
    ///     </para>
    /// </summary>
    internal sealed class DevConsoleHistoryNavigationHideConsolePatch : IPatchMethod
    {
        public static string PatchId => "dev_console_history_navigation_hide_console";
        public static bool IsCritical => false;

        public static string Description =>
            "DevConsole history navigation: reset transient state when the console is hidden";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDevConsole), "HideConsole")];
        }

        public static void Prefix(NDevConsole __instance)
        {
            if (!RitsuLibSettingsStore.ShouldClearDevConsoleInputOnVisibilityChange())
                return;

            DevConsoleHistoryNavigationState.ResetVisibilityState(
                __instance,
                true);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         Resets transient history browsing before the console is shown.
    ///     </para>
    ///     <para xml:lang="zh-CN">
    ///         显示控制台前重置临时的历史记录浏览状态。
    ///     </para>
    /// </summary>
    internal sealed class DevConsoleHistoryNavigationShowConsolePatch : IPatchMethod
    {
        public static string PatchId => "dev_console_history_navigation_show_console";
        public static bool IsCritical => false;

        public static string Description =>
            "DevConsole history navigation: reset transient state before the console is shown";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDevConsole), "ShowConsole")];
        }

        public static void Prefix(NDevConsole __instance)
        {
            if (!RitsuLibSettingsStore.ShouldClearDevConsoleInputOnVisibilityChange())
                return;

            DevConsoleHistoryNavigationState.ResetVisibilityState(
                __instance,
                true);
        }
    }
}
