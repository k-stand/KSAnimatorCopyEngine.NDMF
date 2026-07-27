using System.Linq;
using NUnit.Framework;
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorGraphSchemaTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void GetChildren_VirtualAnimatorController_ListsAllLayers()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");
            VirtualLayer layer = controller.AddLayer(LayerPriority.Default, "Layer1");

            var children = VirtualAnimatorGraphSchema.GetChildren(controller).ToList();

            Assert.That(children.Select(c => c.Child), Does.Contain(layer));
        }

        [Test]
        public void GetChildren_VirtualLayer_YieldsNullStateMachineForNullDetection()
        {
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer");
            layer.StateMachine = null;

            var children = VirtualAnimatorGraphSchema.GetChildren(layer).ToList();

            Assert.That(children.Any(c => c.MemberName == nameof(VirtualLayer.StateMachine) && c.Child == null), Is.True);
        }

        [Test]
        public void GetChildren_VirtualStateMachine_ListsStatesStateMachinesDefaultStateAndBehaviours()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state = sm.AddState("StateA");
            sm.DefaultState = state;
            sm.Behaviours = sm.Behaviours.Add(ScriptableObject.CreateInstance<DummyStateMachineBehaviour>());

            var children = VirtualAnimatorGraphSchema.GetChildren(sm).ToList();

            Assert.That(children.Select(c => c.Child), Does.Contain(state));
            Assert.That(children.Count(c => c.MemberName.StartsWith(nameof(VirtualStateMachine.Behaviours))), Is.EqualTo(1));
        }

        [Test]
        public void GetChildren_VirtualTransition_YieldsNullDestinationsForNullDetection()
        {
            VirtualTransition transition = VirtualTransition.Create();

            var children = VirtualAnimatorGraphSchema.GetChildren(transition).ToList();

            Assert.That(children, Has.Count.EqualTo(2));
            Assert.That(children.All(c => c.Child == null), Is.True);
        }

        [Test]
        public void GetChildren_VirtualStateTransition_SkipsDestinationsWhenIsExit()
        {
            VirtualStateTransition transition = VirtualStateTransition.Create();
            transition.SetExitDestination();

            var children = VirtualAnimatorGraphSchema.GetChildren(transition).ToList();

            Assert.That(children, Is.Empty);
        }

        [Test]
        public void GetChildren_VirtualStateTransition_YieldsNullDestinationsWhenNotExit()
        {
            VirtualStateTransition transition = VirtualStateTransition.Create();

            var children = VirtualAnimatorGraphSchema.GetChildren(transition).ToList();

            Assert.That(children, Has.Count.EqualTo(2));
            Assert.That(children.All(c => c.Child == null), Is.True);
        }

        [Test]
        public void GetChildren_VirtualStateMachine_HandlesNullChildStateMachine()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state = sm.AddState("StateA");

            // nullな子StateMachineを追加(StateMachineTransitionsでTryGetValueを呼ぶことになるケース)
            sm.StateMachines = sm.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine
            {
                StateMachine = null,
                Position = UnityEngine.Vector3.zero
            });

            // 例外が投げられないことを確認
            var children = VirtualAnimatorGraphSchema.GetChildren(sm).ToList();

            // nullなStateMachineもyieldされていることを確認
            Assert.That(children.Any(c => c.MemberName.Contains(nameof(VirtualStateMachine.StateMachines)) && c.Child == null), Is.True);
            // stateも含まれていることを確認(nullなStateMachineの処理で例外が発生したら、stateもyieldされない)
            Assert.That(children.Select(c => c.Child), Does.Contain(state));
        }
    }
}
