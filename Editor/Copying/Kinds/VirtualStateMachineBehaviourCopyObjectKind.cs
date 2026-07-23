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

        // Behaviour自身のみをクローン範囲とし、Behaviourが内部で保持する参照先(パラメーター名など)は対象外とする。
        // それらの参照の妥当性検証は、別のプラグイン機構(IVirtualStateMachineBehaviourCloneResultValidator)が担う。
        public IEnumerable<object> GetCloneScope(object wrappedObject) => new object[] { (StateMachineBehaviour)wrappedObject };
    }
}
