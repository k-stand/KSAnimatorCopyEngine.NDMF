using System;
using System.Collections.Generic;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.Copying
{
    internal sealed class VirtualTransitionCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(VirtualTransition);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Transition;

        public bool IsInStateMachineObject => true;

        public IEnumerable<object> GetCloneScope(object wrappedObject) => new object[] { (VirtualTransition)wrappedObject };
    }
}
