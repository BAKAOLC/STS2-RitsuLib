namespace STS2RitsuLib.Ui
{
    internal static class RitsuUiLayer
    {
        // CanvasLayer draw order is global to the viewport, even when a layer is attached below another layer.
        internal const int CombatOverlay = 20;
        internal const int Workspace = 100;
        internal const int Modal = 120;
        internal const int BlockingProgress = 128;
        internal const int Dialog = 132;
        internal const int Toast = 160;
    }
}
