using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests
{
    public class VirtualAnimatorCopyClipSetCloneTests : VirtualAnimatorClipboardTestFixtureBase
    {
        [Test]
        public void Clone_ChildState_ProducesIndependentStateAndTransitionAndPopulatesClonedMap()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualStateTransition transition = VirtualStateTransition.Create();
            transition.SetDestination(state);
            state.Transitions = state.Transitions.Add(transition);
            VirtualStateMachine.VirtualChildState childState = new() { State = state };
            VirtualAnimatorCopyClipSet clipSet = new(childState);

            VirtualAnimatorCopyClipSet cloneClipSet = clipSet.Clone(CloneContext, out var clonedMap);

            Assert.IsTrue(clonedMap.TryGetValue(state, out object clonedStateObj));
            VirtualState cloneState = (VirtualState)clonedStateObj;
            Assert.AreNotSame(state, cloneState);
            Assert.AreEqual(1, cloneState.Transitions.Count);
            Assert.AreNotSame(transition, cloneState.Transitions[0]);
            Assert.AreSame(cloneState, ((VirtualStateMachine.VirtualChildState)cloneClipSet.Clips[0].Object).State);
        }
    }
}
