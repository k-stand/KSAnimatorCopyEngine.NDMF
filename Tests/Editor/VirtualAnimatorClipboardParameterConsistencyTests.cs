using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using nadena.dev.ndmf.animator;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor.CrossController;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests
{
    public class VirtualAnimatorClipboardParameterConsistencyTests : VirtualAnimatorClipboardTestFixtureBase
    {
        [Test]
        public void FindMissingParameters_ThrowsArgumentNullException_WhenClipSetIsNull()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");
            Assert.Throws<ArgumentNullException>(() => VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(null, controller));
        }

        [Test]
        public void FindMissingParameters_ThrowsArgumentNullException_WhenDestControllerIsNull()
        {
            VirtualStateTransition transition = VirtualStateTransition.Create();
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)transition);
            Assert.Throws<ArgumentNullException>(() => VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(clipSet, null));
        }

        [Test]
        public void FindMissingParameters_ReturnsEmpty_WhenAllParametersExist()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");
            controller.SetParameter("Speed", new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float });

            VirtualStateTransition transition = VirtualStateTransition.Create();
            transition.Conditions = transition.Conditions.Add(new AnimatorCondition { mode = AnimatorConditionMode.Greater, threshold = 0f, parameter = "Speed" });
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)transition);

            IReadOnlyList<string> missing = VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(clipSet, controller);

            Assert.IsEmpty(missing);
        }

        [Test]
        public void FindMissingParameters_DetectsMissingParameter_FromStateTransitionCondition()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");

            VirtualStateTransition transition = VirtualStateTransition.Create();
            transition.Conditions = transition.Conditions.Add(new AnimatorCondition { mode = AnimatorConditionMode.Greater, threshold = 0f, parameter = "Speed" });
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)transition);

            IReadOnlyList<string> missing = VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(clipSet, controller);

            CollectionAssert.AreEquivalent(new[] { "Speed" }, missing);
        }

        [Test]
        public void FindMissingParameters_DetectsMissingParameter_FromNestedStateMachineEntryTransition()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");

            VirtualStateMachine childStateMachine = VirtualStateMachine.Create(CloneContext, "Child");
            VirtualState innerState = childStateMachine.AddState("Inner");
            VirtualTransition entryTransition = VirtualTransition.Create();
            entryTransition.SetDestination(innerState);
            entryTransition.Conditions = entryTransition.Conditions.Add(new AnimatorCondition { mode = AnimatorConditionMode.If, threshold = 0f, parameter = "Grounded" });
            childStateMachine.EntryTransitions = childStateMachine.EntryTransitions.Add(entryTransition);

            VirtualStateMachine.VirtualChildStateMachine childVirtualStateMachine = new() { StateMachine = childStateMachine };
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)childVirtualStateMachine);

            IReadOnlyList<string> missing = VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(clipSet, controller);

            CollectionAssert.AreEquivalent(new[] { "Grounded" }, missing);
        }

        [Test]
        public void FindMissingParameters_IgnoresStateMachineBehaviour_WhenNoResolverRegistered()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");
            DummyStateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();
            VirtualState state = VirtualState.Create("State1");
            state.Behaviours = state.Behaviours.Add(behaviour);
            VirtualStateMachine.VirtualChildState childState = new() { State = state };
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)childState);

            IReadOnlyList<string> missing = VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(clipSet, controller);

            Assert.IsEmpty(missing);
        }

        [Test]
        public void FindMissingParameters_DetectsMissingParameter_ViaRegisteredResolver()
        {
            VirtualParameterReferenceResolverRegistry.Shared.Register(new StubBehaviourResolver());
            try
            {
                VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");
                DummyStateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();
                VirtualState state = VirtualState.Create("State1");
                state.Behaviours = state.Behaviours.Add(behaviour);
                VirtualStateMachine.VirtualChildState childState = new() { State = state };
                VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)childState);

                IReadOnlyList<string> missing = VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(clipSet, controller);

                CollectionAssert.AreEquivalent(new[] { "StubParam" }, missing);
            }
            finally
            {
                VirtualParameterReferenceResolverRegistry.Shared.Unregister(typeof(DummyStateMachineBehaviour));
            }
        }

        [Test]
        public void FindMissingParameters_DoesNotDuplicateParameterNamesReferencedMultipleTimes()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");

            VirtualStateTransition transition1 = VirtualStateTransition.Create();
            transition1.Conditions = transition1.Conditions.Add(new AnimatorCondition { mode = AnimatorConditionMode.Greater, threshold = 0f, parameter = "Speed" });
            VirtualStateTransition transition2 = VirtualStateTransition.Create();
            transition2.Conditions = transition2.Conditions.Add(new AnimatorCondition { mode = AnimatorConditionMode.Less, threshold = 1f, parameter = "Speed" });
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy(new object[] { transition1, transition2 });

            IReadOnlyList<string> missing = VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(clipSet, controller);

            CollectionAssert.AreEquivalent(new[] { "Speed" }, missing);
        }

        // VirtualStateMachine.StateMachinesは、構造体VirtualChildStateMachineの未初期化(StateMachineがnullの)要素を
        // 含みうる(実際にVirtualAnimatorCopyClipSet/VirtualAnimatorGraphTraversalで既知のバグパターンとして扱われている)。
        // FindMissingParametersのBFS探索(CollectFromStateMachine)がこのケースで例外を投げないことを回帰確認する。
        [Test]
        public void FindMissingParameters_DoesNotThrow_WhenNestedStateMachinesContainNullEntry()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");

            VirtualStateMachine rootStateMachine = VirtualStateMachine.Create(CloneContext, "Root");
            rootStateMachine.StateMachines = rootStateMachine.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine());

            VirtualStateMachine.VirtualChildStateMachine rootVirtualStateMachine = new() { StateMachine = rootStateMachine };
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)rootVirtualStateMachine);

            IReadOnlyList<string> missing = null;
            Assert.DoesNotThrow(() => missing = VirtualAnimatorClipboardParameterConsistency.FindMissingParameters(clipSet, controller));
            Assert.IsEmpty(missing);
        }

        private sealed class StubBehaviourResolver : IVirtualParameterReferenceResolver
        {
            public Type BehaviourType => typeof(DummyStateMachineBehaviour);

            public IEnumerable<string> GetReferencedParameterNames(StateMachineBehaviour behaviour) => new[] { "StubParam" };
        }
    }
}
