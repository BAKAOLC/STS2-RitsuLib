using Godot;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    /// <summary>
    ///     Controls how a registered attachment handles an existing direct child with the configured node name.
    ///     控制注册挂载项如何处理已有的同名直接子节点。
    /// </summary>
    public enum NodeAttachmentDuplicatePolicy
    {
        /// <summary>
        ///     Create and attach the registered node even when another direct child already has the same name.
        ///     即使已有同名直接子节点，也继续创建并挂载注册节点。
        /// </summary>
        AllowDuplicateName,

        /// <summary>
        ///     Reuse the existing direct child when its type is compatible with the registered node type.
        ///     当已有直接子节点类型兼容注册节点类型时复用该节点。
        /// </summary>
        ReuseExistingByName,

        /// <summary>
        ///     Skip this attachment when a direct child with the configured name already exists.
        ///     当已有配置名称的直接子节点时跳过本次挂载。
        /// </summary>
        SkipIfExistingByName,

        /// <summary>
        ///     Remove the existing direct child before creating the registered node.
        ///     创建注册节点前移除已有直接子节点。
        /// </summary>
        ReplaceExistingByName,

        /// <summary>
        ///     Throw when a direct child with the configured name already exists.
        ///     当已有配置名称的直接子节点时抛出异常。
        /// </summary>
        ThrowIfExistingByName,
    }

    /// <summary>
    ///     Controls how a created attachment node is added to its parent.
    ///     控制创建出的挂载节点如何加入父节点。
    /// </summary>
    public enum NodeAttachmentAddMode
    {
        /// <summary>
        ///     Use <see cref="RitsuGodotTreeCompat.AddChildSafely" />.
        ///     使用 <see cref="RitsuGodotTreeCompat.AddChildSafely" />。
        /// </summary>
        AddChildSafely,

        /// <summary>
        ///     Call <see cref="Node.AddChild(Node, bool, Node.InternalMode)" /> immediately.
        ///     立即调用 <see cref="Node.AddChild(Node, bool, Node.InternalMode)" />。
        /// </summary>
        AddChildDirect,
    }

    /// <summary>
    ///     Controls when <see cref="NodeAttachmentDefinition.Setup" /> is invoked.
    ///     控制何时调用 <see cref="NodeAttachmentDefinition.Setup" />。
    /// </summary>
    public enum NodeAttachmentSetupTiming
    {
        /// <summary>
        ///     Run setup after creation and before adding the child to the parent.
        ///     创建后、加入父节点前运行 setup。
        /// </summary>
        BeforeAdd,

        /// <summary>
        ///     Run setup after the child has been added to the parent.
        ///     子节点加入父节点后运行 setup。
        /// </summary>
        AfterAdd,
    }
}
