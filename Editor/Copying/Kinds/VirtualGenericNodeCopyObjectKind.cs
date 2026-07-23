using System;
using System.Collections.Generic;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying
{
    internal sealed class VirtualGenericNodeCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(VirtualNode);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Other;

        public bool IsInStateMachineObject => false;

        public IEnumerable<object> GetCloneScope(object wrappedObject) =>
            wrappedObject is VirtualNode node ? new object[] { node } : Array.Empty<object>();
    }
}
