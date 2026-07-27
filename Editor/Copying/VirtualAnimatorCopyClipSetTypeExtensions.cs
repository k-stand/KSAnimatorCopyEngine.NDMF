namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.Copying
{
    internal static class VirtualAnimatorCopyClipSetTypeExtensions
    {
        internal static bool IsInStateMachineCategory(this VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType setType) =>
            setType is VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState
                or VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildStateMachine
                or VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Transition
                or VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.StateTransition
                or VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.InStateMachineObjects;
    }
}
