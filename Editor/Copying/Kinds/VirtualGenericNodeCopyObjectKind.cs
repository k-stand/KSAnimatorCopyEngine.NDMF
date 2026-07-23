using System;
using System.Collections.Generic;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying
{
    internal sealed class VirtualGenericNodeCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        // 他のKindが対応しない任意のVirtualNode派生型に対するフォールバック。
        // VirtualAnimatorCopyObjectKindRegistry.Resolveの基底型探索により、専用Kindが未登録の型はここに解決される。
        public Type ObjectType => typeof(VirtualNode);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Other;

        public bool IsInStateMachineObject => false;

        public IEnumerable<object> GetCloneScope(object wrappedObject) =>
            wrappedObject is VirtualNode node ? new object[] { node } : Array.Empty<object>();
    }
}
