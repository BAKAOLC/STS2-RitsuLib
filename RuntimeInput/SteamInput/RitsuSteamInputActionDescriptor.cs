namespace STS2RitsuLib.RuntimeInput
{
    internal sealed record RitsuSteamInputActionDescriptor(
        string InputActionName,
        string SteamActionId,
        RuntimeHotkeyText DisplayName,
        RuntimeHotkeyText? Description,
        string? RegistrationId);
}
