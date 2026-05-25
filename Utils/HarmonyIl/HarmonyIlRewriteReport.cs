namespace STS2RitsuLib.Utils.HarmonyIl
{
    /// <summary>
    ///     Result metadata for an IL rewrite operation.
    ///     IL 改写操作的结果元数据。
    /// </summary>
    public readonly record struct HarmonyIlRewriteReport(
        string Operation,
        int Matches,
        int Applied,
        int BeforeCount,
        int AfterCount,
        bool AlreadySatisfied = false)
    {
        /// <summary>
        ///     True when this operation emitted a changed instruction list.
        ///     当此操作实际改写了指令列表时为 true。
        /// </summary>
        public bool Changed => Applied > 0;

        /// <summary>
        ///     True when this operation either changed IL or detected that the requested IL is already present.
        ///     当此操作实际改写了 IL，或检测到目标 IL 已存在时为 true。
        /// </summary>
        public bool Succeeded => Changed || AlreadySatisfied;

        /// <summary>
        ///     Throws when the operation did not change IL and did not detect equivalent existing IL.
        ///     当操作既未改写 IL，也未检测到等价已有 IL 时抛出异常。
        /// </summary>
        public void RequireSucceeded()
        {
            if (Succeeded)
                return;

            throw new InvalidOperationException(
                $"{Operation} did not match the target IL. before={BeforeCount}, after={AfterCount}.");
        }

        /// <summary>
        ///     Throws when the number of applied rewrites is outside the expected range.
        ///     当实际改写次数不在预期范围内时抛出异常。
        /// </summary>
        public void RequireApplied(int min = 1, int? max = null)
        {
            if (Applied >= min && (max == null || Applied <= max.Value))
                return;

            var maxText = max?.ToString() ?? "unbounded";
            throw new InvalidOperationException(
                $"{Operation} applied {Applied} rewrite(s), expected range [{min}, {maxText}]. " +
                $"matches={Matches}, alreadySatisfied={AlreadySatisfied}, before={BeforeCount}, after={AfterCount}.");
        }

        /// <summary>
        ///     Throws when the number of applied rewrites is not exactly <paramref name="count" />.
        ///     当实际改写次数不等于 <paramref name="count" /> 时抛出异常。
        /// </summary>
        public void RequireExactly(int count)
        {
            RequireApplied(count, count);
        }

        /// <summary>
        ///     Returns a compact diagnostic string.
        ///     返回紧凑诊断字符串。
        /// </summary>
        public string Describe()
        {
            return
                $"{Operation}: matches={Matches}, applied={Applied}, alreadySatisfied={AlreadySatisfied}, " +
                $"before={BeforeCount}, after={AfterCount}";
        }
    }
}
