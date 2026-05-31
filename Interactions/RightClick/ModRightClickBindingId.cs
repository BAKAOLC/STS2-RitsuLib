namespace STS2RitsuLib.Interactions.RightClick
{
    /// <summary>
    ///     Stable identity for a registered right-click binding.
    ///     已注册右键绑定的稳定身份。
    /// </summary>
    public readonly record struct ModRightClickBindingId(string Id)
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return Id;
        }
    }
}
