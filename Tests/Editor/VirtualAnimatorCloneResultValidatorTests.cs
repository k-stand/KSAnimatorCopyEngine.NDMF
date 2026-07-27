using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorCloneResultValidatorTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        private sealed class NullChildStubValidator : IVirtualStateMachineBehaviourCloneResultValidator
        {
            public Type BehaviourType => typeof(DummyStateMachineBehaviour);

            public IEnumerable<(string MemberName, object Child)> GetChildren(StateMachineBehaviour behaviour) => new (string, object)[] { ("StubChild", null) };
        }

        [Test]
        public void ValidateCloneResult_ReturnsEmpty_ForFullyValidStateMachine()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            // 生API版のUnityEditor.Animations.AnimatorStateMachine.AddStateと異なり、
            // VirtualStateMachine.AddStateは追加したStateを自動的にDefaultStateへ設定しない
            // (nadena.dev.ndmf.animator.VirtualStateMachine.AddStateの実装を確認済み)。
            // VirtualAnimatorGraphSchema.GetChildrenはDefaultStateを無条件yieldするため、
            // 未設定のままだと「完全に有効なStateMachine」にならずnull検出されてしまう。
            // そのため明示的にDefaultStateを設定する。
            VirtualState state = sm.AddState("State1");
            sm.DefaultState = state;

            IReadOnlyCollection<VirtualAnimatorCloneResultValidator.InvalidNullMember> result = VirtualAnimatorCloneResultValidator.ValidateCloneResult(sm);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ValidateCloneResult_DetectsNullDestination_OnStateTransition()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualStateTransition transition = VirtualStateTransition.Create();
            // SetDestination/SetExitDestinationのどちらも呼ばない = destinationState/destinationStateMachine/isExitが全て未設定
            state.Transitions = state.Transitions.Add(transition);

            IReadOnlyCollection<VirtualAnimatorCloneResultValidator.InvalidNullMember> result = VirtualAnimatorCloneResultValidator.ValidateCloneResult(state);

            Assert.IsNotEmpty(result);
        }

        [Test]
        public void ValidateCloneResult_InvalidNullMember_ExposesParentAndMemberName()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualStateTransition transition = VirtualStateTransition.Create();
            state.Transitions = state.Transitions.Add(transition);

            IReadOnlyCollection<VirtualAnimatorCloneResultValidator.InvalidNullMember> result = VirtualAnimatorCloneResultValidator.ValidateCloneResult(state);

            Assert.IsTrue(result.Any(m => ReferenceEquals(m.Parent, transition) && m.MemberName == nameof(VirtualStateTransition.DestinationState)));
        }

        [Test]
        public void ValidateCloneResult_ReturnsEmpty_ForNullTarget()
        {
            IReadOnlyCollection<VirtualAnimatorCloneResultValidator.InvalidNullMember> result = VirtualAnimatorCloneResultValidator.ValidateCloneResult(null);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ValidateCloneResult_DoesNotRevalidateSharedStateMachine_ReachedViaMultiplePaths()
        {
            VirtualStateMachine shared = VirtualStateMachine.Create(CloneContext, "Shared");
            VirtualStateMachine branchA = VirtualStateMachine.Create(CloneContext, "BranchA");
            VirtualStateMachine branchB = VirtualStateMachine.Create(CloneContext, "BranchB");
            branchA.StateMachines = branchA.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine { StateMachine = shared });
            branchB.StateMachines = branchB.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine { StateMachine = shared });

            VirtualStateMachine root = VirtualStateMachine.Create(CloneContext, "Root");
            root.StateMachines = root.StateMachines.AddRange(new[]
            {
                new VirtualStateMachine.VirtualChildStateMachine { StateMachine = branchA },
                new VirtualStateMachine.VirtualChildStateMachine { StateMachine = branchB },
            });

            Assert.DoesNotThrow(() => VirtualAnimatorCloneResultValidator.ValidateCloneResult(root));
        }

        [Test]
        public void ValidateCloneResult_SkipsStateMachineBehaviour_WhenNoValidatorRegistered()
        {
            DummyStateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();

            IReadOnlyCollection<VirtualAnimatorCloneResultValidator.InvalidNullMember> result = VirtualAnimatorCloneResultValidator.ValidateCloneResult(behaviour);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ValidateCloneResult_DetectsNullChild_WhenValidatorRegistered()
        {
            DummyStateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();
            VirtualStateMachineBehaviourCloneResultValidatorRegistry.Shared.Register(new NullChildStubValidator());
            try
            {
                IReadOnlyCollection<VirtualAnimatorCloneResultValidator.InvalidNullMember> result = VirtualAnimatorCloneResultValidator.ValidateCloneResult(behaviour);

                Assert.IsNotEmpty(result);
            }
            finally
            {
                VirtualStateMachineBehaviourCloneResultValidatorRegistry.Shared.Unregister(typeof(DummyStateMachineBehaviour));
            }
        }
    }
}
