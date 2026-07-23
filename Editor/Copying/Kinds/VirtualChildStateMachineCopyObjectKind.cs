using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying
{
    internal sealed class VirtualChildStateMachineCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(VirtualStateMachine.VirtualChildStateMachine);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildStateMachine;

        public bool IsInStateMachineObject => true;

        public IEnumerable<object> GetCloneScope(object wrappedObject)
        {
            VirtualStateMachine stateMachine = ((VirtualStateMachine.VirtualChildStateMachine)wrappedObject).StateMachine;
            // stateMachineが未設定の場合は例外を出さず空スコープを返し、呼び出し元は静かに無登録で終わる
            if (stateMachine == null) return Array.Empty<object>();

            return new object[] { stateMachine }.Concat(VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(stateMachine));
        }
    }
}
