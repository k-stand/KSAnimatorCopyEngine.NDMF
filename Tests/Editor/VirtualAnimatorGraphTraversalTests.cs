using System.Collections.Generic;
using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorGraphTraversalTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void ListupObjectsInStateMachine_ReturnsEmptySet_WhenStateMachineIsNull()
        {
            HashSet<VirtualNode> result = VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(null);

            Assert.IsEmpty(result);
        }

        [Test]
        public void ListupObjectsInStateMachine_CollectsStatesTransitionsAndNestedStateMachines()
        {
            VirtualStateMachine root = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state1 = root.AddState("State1");
            VirtualStateTransition transition = VirtualStateTransition.Create();
            transition.SetDestination(state1);
            state1.Transitions = state1.Transitions.Add(transition);

            VirtualStateMachine childSm = VirtualStateMachine.Create(CloneContext, "Child");
            VirtualState childState = childSm.AddState("ChildState");
            root.StateMachines = root.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine
            {
                StateMachine = childSm,
                Position = UnityEngine.Vector3.zero
            });

            HashSet<VirtualNode> result = VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(root);

            Assert.IsTrue(result.Contains(state1));
            Assert.IsTrue(result.Contains(transition));
            Assert.IsTrue(result.Contains(childSm));
            Assert.IsTrue(result.Contains(childState));
        }

        [Test]
        public void ListupObjectsInLayer_CollectsStateMachineContents()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state = sm.AddState("State1");
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = sm;

            HashSet<VirtualNode> result = VirtualAnimatorGraphTraversal.ListupObjectsInLayer(layer);

            Assert.IsTrue(result.Contains(sm));
            Assert.IsTrue(result.Contains(state));
        }

        [Test]
        public void ListupObjectsInStateMachine_HandlesNullChildStateMachine()
        {
            VirtualStateMachine root = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state = root.AddState("State1");

            // nullな子StateMachineを追加(StateMachineTransitionsでTryGetValueを呼ぶことになるケース)
            root.StateMachines = root.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine
            {
                StateMachine = null,
                Position = UnityEngine.Vector3.zero
            });

            // 例外が投げられないことを確認
            HashSet<VirtualNode> result = VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(root);

            // nullな子StateMachineでも処理が続行できることを確認(stateが含まれている)
            Assert.IsTrue(result.Contains(state));
        }

        [Test]
        public void ListupObjectsInStateMachine_HandlesNullChildStateMachine_InEnqueueLoop()
        {
            VirtualStateMachine root = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state = root.AddState("State1");

            VirtualStateMachine childSm = VirtualStateMachine.Create(CloneContext, "Child");
            VirtualState childState = childSm.AddState("ChildState");

            // 正常な子StateMachineとnullな子StateMachineの両方をrootのStateMachinesに追加する。
            // 修正前のコードでは、root処理時にsearchQueueへ [childSm, null] の順でEnqueueされ、
            // childSmが先にDequeueされて正常に処理された「後」に、nullがDequeueされて
            // curASM.EntryTransitions へのアクセスでNullReferenceExceptionが発生していた
            // (searchQueueにnullが積まれること自体は既存のHandlesNullChildStateMachineテストの
            // シナリオでも起きていたが、そちらはnullの直後に即座に例外が出るだけで、
            // 「正常な子の処理が完了した後に別途キューに残っていたnullで落ちる」という
            // Enqueueループ由来の経路を明示的に踏むものではなかった)。
            root.StateMachines = root.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine
            {
                StateMachine = childSm,
                Position = UnityEngine.Vector3.zero
            });
            root.StateMachines = root.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine
            {
                StateMachine = null,
                Position = UnityEngine.Vector3.zero
            });

            HashSet<VirtualNode> result = null;

            // 例外が投げられないことを確認(修正前はsearchQueue.Enqueue(null)経由でNullReferenceExceptionが発生していた)
            Assert.DoesNotThrow(() => result = VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(root));

            // null混入によって正常な子StateMachineの処理まで巻き込まれて破綻していないことを確認
            Assert.IsTrue(result.Contains(state));
            Assert.IsTrue(result.Contains(childSm));
            Assert.IsTrue(result.Contains(childState));
        }
    }
}
