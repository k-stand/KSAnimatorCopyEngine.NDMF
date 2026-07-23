using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying
{
    internal sealed class VirtualChildStateCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(VirtualStateMachine.VirtualChildState);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState;

        public bool IsInStateMachineObject => true;

        public IEnumerable<object> GetCloneScope(object wrappedObject)
        {
            VirtualState state = ((VirtualStateMachine.VirtualChildState)wrappedObject).State;
            if (state == null) return Array.Empty<object>();

            return new object[] { state }.Concat(state.Transitions).Concat(state.Behaviours);
        }
    }
}
