using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.Copying
{
    internal sealed class VirtualChildStateCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(VirtualStateMachine.VirtualChildState);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState;

        public bool IsInStateMachineObject => true;

        public IEnumerable<object> GetCloneScope(object wrappedObject)
        {
            VirtualState state = ((VirtualStateMachine.VirtualChildState)wrappedObject).State;
            // stateが未設定の場合は例外を出さず空スコープを返し、呼び出し元は静かに無登録で終わる
            if (state == null) return Array.Empty<object>();

            return new object[] { state }.Concat(state.Transitions).Concat(state.Behaviours);
        }
    }
}
