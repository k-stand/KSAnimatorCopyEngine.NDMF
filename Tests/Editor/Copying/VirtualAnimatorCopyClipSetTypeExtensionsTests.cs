using NUnit.Framework;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests.Copying
{
    public class VirtualAnimatorCopyClipSetTypeExtensionsTests
    {
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState, true)]
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildStateMachine, true)]
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Transition, true)]
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.StateTransition, true)]
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.InStateMachineObjects, true)]
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers, false)]
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours, false)]
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Other, false)]
        [TestCase(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.None, false)]
        public void IsInStateMachineCategory_ReturnsExpectedValue(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType setType, bool expected)
        {
            Assert.AreEqual(expected, setType.IsInStateMachineCategory());
        }
    }
}
