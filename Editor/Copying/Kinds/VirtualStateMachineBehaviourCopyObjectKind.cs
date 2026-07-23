using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying
{
    internal sealed class VirtualStateMachineBehaviourCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(StateMachineBehaviour);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours;

        public bool IsInStateMachineObject => false;

        public IEnumerable<object> GetCloneScope(object wrappedObject) => new object[] { (StateMachineBehaviour)wrappedObject };
    }
}
