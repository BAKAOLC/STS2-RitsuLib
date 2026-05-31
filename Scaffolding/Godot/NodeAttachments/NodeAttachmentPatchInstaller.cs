using System.Reflection;
using Godot;
using HarmonyLib;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;

namespace STS2RitsuLib.Scaffolding.Godot.NodeAttachments
{
    internal static class NodeAttachmentPatchInstaller
    {
        private const string ReadyMethodName = "_Ready";
        private static readonly Lock SyncRoot = new();

        private static readonly ModPatcher Patcher =
            RitsuLibFramework.CreatePatcher(Const.ModId, "node-attachments", "node attachments");

        private static readonly Dictionary<MethodBase, string> PatchedReadyMethods = [];

        public static void EnsureReadyPatched(Type parentType)
        {
            ArgumentNullException.ThrowIfNull(parentType);
            if (!typeof(Node).IsAssignableFrom(parentType))
                throw new ArgumentException($"Type must derive from {typeof(Node).FullName}.", nameof(parentType));

            var readyMethod = ResolveReadyMethod(parentType)
                              ?? throw new InvalidOperationException(
                                  $"No {ReadyMethodName} method found for node type {parentType.FullName}.");

            lock (SyncRoot)
            {
                if (PatchedReadyMethods.ContainsKey(readyMethod))
                    return;

                var postfix = typeof(NodeAttachmentReadyPatch).GetMethod(
                                  "Postfix",
                                  BindingFlags.Static | BindingFlags.NonPublic)
                              ?? throw new MissingMethodException(
                                  typeof(NodeAttachmentReadyPatch).FullName,
                                  "Postfix");

                var patchId = BuildPatchId(readyMethod);
                var postfixMethod = new HarmonyMethod(postfix)
                {
                    priority = Priority.First,
                };
                var patch = new DynamicPatchInfo(
                    patchId,
                    readyMethod,
                    postfix: postfixMethod,
                    isCritical: false,
                    description:
                    $"Attach registered RitsuLib child nodes after {readyMethod.DeclaringType?.FullName}.{readyMethod.Name}");

                if (!Patcher.ApplyDynamicPatches([patch]))
                    throw new InvalidOperationException(
                        $"Failed to install node attachment ready patch for {parentType.FullName} via {readyMethod.DeclaringType?.FullName}.{readyMethod.Name}.");

                PatchedReadyMethods[readyMethod] = patchId;
            }
        }

        private static MethodInfo? ResolveReadyMethod(Type parentType)
        {
            for (var current = parentType; current != null; current = current.BaseType)
            {
                var method = current.GetMethod(
                    ReadyMethodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null,
                    Type.EmptyTypes,
                    null);
                if (method != null)
                    return method;
            }

            return null;
        }

        private static string BuildPatchId(MethodBase method)
        {
            var declaringType = method.DeclaringType?.FullName ?? "unknown";
            var moduleId = method.Module.ModuleVersionId.ToString("N");
            return $"ritsulib.node_attachment.ready.{declaringType}.{method.Name}.{moduleId}.{method.MetadataToken}";
        }

        private static class NodeAttachmentReadyPatch
        {
            // ReSharper disable once InconsistentNaming
            private static void Postfix(Node __instance)
            {
                NodeAttachmentRuntime.AttachReadyChildren(__instance);
            }
        }
    }
}
