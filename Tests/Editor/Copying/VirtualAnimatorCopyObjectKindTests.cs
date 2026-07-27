using System.Linq;
using NUnit.Framework;
using UnityEngine;
using nadena.dev.ndmf.animator;
using com.github.k_stand.ksanimatorcopyengine.ndmf.editor;
using com.github.k_stand.ksanimatorcopyengine.ndmf.editor.Copying;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests.Copying
{
    public class VirtualAnimatorCopyObjectKindTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void VirtualLayerCopyObjectKind_HasExpectedProperties()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualLayerCopyObjectKind();
            Assert.AreEqual(typeof(VirtualLayer), kind.ObjectType);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers, kind.SingleClipSetType);
            Assert.IsFalse(kind.IsInStateMachineObject);
        }

        [Test]
        public void VirtualChildStateCopyObjectKind_HasExpectedProperties()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualChildStateCopyObjectKind();
            Assert.AreEqual(typeof(VirtualStateMachine.VirtualChildState), kind.ObjectType);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState, kind.SingleClipSetType);
            Assert.IsTrue(kind.IsInStateMachineObject);
        }

        [Test]
        public void VirtualChildStateMachineCopyObjectKind_HasExpectedProperties()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualChildStateMachineCopyObjectKind();
            Assert.AreEqual(typeof(VirtualStateMachine.VirtualChildStateMachine), kind.ObjectType);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildStateMachine, kind.SingleClipSetType);
            Assert.IsTrue(kind.IsInStateMachineObject);
        }

        [Test]
        public void VirtualTransitionCopyObjectKind_HasExpectedProperties()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualTransitionCopyObjectKind();
            Assert.AreEqual(typeof(VirtualTransition), kind.ObjectType);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Transition, kind.SingleClipSetType);
            Assert.IsTrue(kind.IsInStateMachineObject);
        }

        [Test]
        public void VirtualStateTransitionCopyObjectKind_HasExpectedProperties()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualStateTransitionCopyObjectKind();
            Assert.AreEqual(typeof(VirtualStateTransition), kind.ObjectType);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.StateTransition, kind.SingleClipSetType);
            Assert.IsTrue(kind.IsInStateMachineObject);
        }

        [Test]
        public void VirtualStateMachineBehaviourCopyObjectKind_HasExpectedProperties()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualStateMachineBehaviourCopyObjectKind();
            Assert.AreEqual(typeof(StateMachineBehaviour), kind.ObjectType);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours, kind.SingleClipSetType);
            Assert.IsFalse(kind.IsInStateMachineObject);
        }

        [Test]
        public void VirtualGenericNodeCopyObjectKind_HasExpectedProperties()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualGenericNodeCopyObjectKind();
            Assert.AreEqual(typeof(VirtualNode), kind.ObjectType);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Other, kind.SingleClipSetType);
            Assert.IsFalse(kind.IsInStateMachineObject);
        }

        [Test]
        public void VirtualLayerCopyObjectKind_GetCloneScope_MatchesListupObjectsInLayer()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "Root");
            sm.AddState("State1");
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer");
            layer.StateMachine = sm;

            IVirtualAnimatorCopyObjectKind kind = new VirtualLayerCopyObjectKind();

            CollectionAssert.AreEquivalent(VirtualAnimatorGraphTraversal.ListupObjectsInLayer(layer), kind.GetCloneScope(layer).ToArray());
        }

        [Test]
        public void VirtualLayerCopyObjectKind_GetCloneScope_ReturnsEmpty_WhenStateMachineIsNull()
        {
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer");
            layer.StateMachine = null;

            IVirtualAnimatorCopyObjectKind kind = new VirtualLayerCopyObjectKind();

            Assert.IsEmpty(kind.GetCloneScope(layer));
        }

        [Test]
        public void VirtualChildStateCopyObjectKind_GetCloneScope_ReturnsStateTransitionsAndBehaviours()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualStateTransition transition = VirtualStateTransition.Create();
            transition.SetDestination(state);
            state.Transitions = state.Transitions.Add(transition);
            StateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();
            state.Behaviours = state.Behaviours.Add(behaviour);
            VirtualStateMachine.VirtualChildState childState = new() { State = state };

            IVirtualAnimatorCopyObjectKind kind = new VirtualChildStateCopyObjectKind();

            CollectionAssert.AreEquivalent(new object[] { state, transition, behaviour }, kind.GetCloneScope(childState).ToArray());
        }

        [Test]
        public void VirtualChildStateCopyObjectKind_GetCloneScope_ReturnsEmpty_WhenStateIsNull()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualChildStateCopyObjectKind();

            Assert.IsEmpty(kind.GetCloneScope(new VirtualStateMachine.VirtualChildState { State = null }));
        }

        [Test]
        public void VirtualChildStateMachineCopyObjectKind_GetCloneScope_MatchesStateMachineAndListupObjectsInStateMachine()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "Root");
            sm.AddState("State1");
            VirtualStateMachine.VirtualChildStateMachine childStateMachine = new() { StateMachine = sm };

            IVirtualAnimatorCopyObjectKind kind = new VirtualChildStateMachineCopyObjectKind();

            object[] expected = new object[] { sm }.Concat(VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(sm)).ToArray();
            CollectionAssert.AreEquivalent(expected, kind.GetCloneScope(childStateMachine).ToArray());
        }

        [Test]
        public void VirtualChildStateMachineCopyObjectKind_GetCloneScope_ReturnsEmpty_WhenStateMachineIsNull()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualChildStateMachineCopyObjectKind();

            Assert.IsEmpty(kind.GetCloneScope(new VirtualStateMachine.VirtualChildStateMachine { StateMachine = null }));
        }

        [Test]
        public void VirtualTransitionCopyObjectKind_GetCloneScope_ReturnsTransitionOnly()
        {
            VirtualTransition transition = VirtualTransition.Create();

            IVirtualAnimatorCopyObjectKind kind = new VirtualTransitionCopyObjectKind();

            CollectionAssert.AreEquivalent(new object[] { transition }, kind.GetCloneScope(transition).ToArray());
        }

        [Test]
        public void VirtualStateTransitionCopyObjectKind_GetCloneScope_ReturnsStateTransitionOnly()
        {
            VirtualStateTransition transition = VirtualStateTransition.Create();

            IVirtualAnimatorCopyObjectKind kind = new VirtualStateTransitionCopyObjectKind();

            CollectionAssert.AreEquivalent(new object[] { transition }, kind.GetCloneScope(transition).ToArray());
        }

        [Test]
        public void VirtualStateMachineBehaviourCopyObjectKind_GetCloneScope_ReturnsBehaviourOnly()
        {
            StateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();

            IVirtualAnimatorCopyObjectKind kind = new VirtualStateMachineBehaviourCopyObjectKind();

            CollectionAssert.AreEquivalent(new object[] { behaviour }, kind.GetCloneScope(behaviour).ToArray());
        }

        [Test]
        public void VirtualGenericNodeCopyObjectKind_GetCloneScope_ReturnsNodeItself()
        {
            VirtualState state = VirtualState.Create("State1");

            IVirtualAnimatorCopyObjectKind kind = new VirtualGenericNodeCopyObjectKind();

            CollectionAssert.AreEquivalent(new object[] { state }, kind.GetCloneScope(state).ToArray());
        }

        [Test]
        public void VirtualGenericNodeCopyObjectKind_GetCloneScope_ReturnsEmpty_WhenObjectIsNotVirtualNode()
        {
            IVirtualAnimatorCopyObjectKind kind = new VirtualGenericNodeCopyObjectKind();

            Assert.IsEmpty(kind.GetCloneScope(new object()));
        }
    }
}
