using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorCopyClipSetAncestorTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void Layer_ContainedInParentController_IsNotAncestorMismatched()
        {
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");
            VirtualLayer layer = parentController.AddLayer(LayerPriority.Default, "Layer1");

            VirtualAnimatorCopyClipSet clipSet = new(layer, parentController);

            Assert.IsFalse(clipSet.IsAncestorMismatched);
            Assert.AreSame(parentController, clipSet.ParentController);
        }

        [Test]
        public void Layer_NotContainedInParentController_SetsIsAncestorMismatched_ButCopyStillSucceeds()
        {
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");
            parentController.AddLayer(LayerPriority.Default, "OtherLayer");

            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");

            VirtualAnimatorCopyClipSet clipSet = new(layer, parentController);

            Assert.IsTrue(clipSet.IsAncestorMismatched);
            Assert.IsNull(clipSet.ParentController);
        }

        [Test]
        public void ChildState_DescendantOfAncestorStateMachine_IsNotAncestorMismatched()
        {
            VirtualStateMachine ancestorStateMachine = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state = ancestorStateMachine.AddState("State1");
            VirtualStateMachine.VirtualChildState childState = ancestorStateMachine.States[0];

            VirtualAnimatorCopyClipSet clipSet = new(childState, ancestorStateMachine);

            Assert.IsFalse(clipSet.IsAncestorMismatched);
            Assert.AreSame(ancestorStateMachine, clipSet.AncestorStateMachine);
        }

        [Test]
        public void ChildState_NotDescendantOfAncestorStateMachine_SetsIsAncestorMismatched_ButCopyStillSucceeds()
        {
            VirtualStateMachine ancestorStateMachine = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state = VirtualState.Create("State1");
            VirtualStateMachine.VirtualChildState childState = new() { State = state };
            // ancestorStateMachineの子孫としては登録しない

            VirtualAnimatorCopyClipSet clipSet = new(childState, ancestorStateMachine);

            Assert.IsTrue(clipSet.IsAncestorMismatched);
            Assert.IsNull(clipSet.AncestorStateMachine);
        }

        [Test]
        public void Copy_WithAncestorStateMachine_DoesNotThrow_WhenDescendantContainsNullChildStateMachine()
        {
            VirtualStateMachine ancestorStateMachine = VirtualStateMachine.Create(CloneContext, "Ancestor");
            VirtualState state = ancestorStateMachine.AddState("State1");
            // nullな子StateMachineエントリを追加(ContextsSettingInternalのGroupBy(c => c.GetType())でNRE化するケース)
            ancestorStateMachine.StateMachines = ancestorStateMachine.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine
            {
                StateMachine = null,
                Position = UnityEngine.Vector3.zero
            });

            VirtualAnimatorCopyClipSet clipSet = null;
            Assert.DoesNotThrow(() => clipSet = VirtualAnimatorCopyEngine.Copy((object)state, ancestorStateMachine));

            Assert.IsNotNull(clipSet);
            Assert.IsFalse(clipSet.IsAncestorMismatched);
            Assert.AreSame(ancestorStateMachine, clipSet.AncestorStateMachine);
        }
    }
}
