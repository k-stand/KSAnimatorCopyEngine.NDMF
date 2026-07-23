using System;
using System.Collections.Generic;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying
{
    internal sealed class VirtualStateTransitionCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(VirtualStateTransition);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.StateTransition;

        public bool IsInStateMachineObject => true;

        public IEnumerable<object> GetCloneScope(object wrappedObject) => new object[] { (VirtualStateTransition)wrappedObject };
    }
}
