using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.Copying
{
    internal interface IVirtualAnimatorCopyObjectKind
    {
        Type ObjectType { get; }

        VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType { get; }

        bool IsInStateMachineObject { get; }

        // wrappedObjectをコピー範囲のルートとしてClone登録する際に、明示的にClonePolicy.Cloneを
        // 登録すべきVirtualNode/StateMachineBehaviour一式を返す。所有関係(VirtualAnimatorCloner.
        // RegisterChildrenRecursivelyが辿る範囲)に限定し、Transition/StateTransitionの
        // DestinationState/DestinationStateMachineのような参照先は含めない。
        IEnumerable<object> GetCloneScope(object wrappedObject);
    }
}
