using System;
using NUnit.Framework;
using UnityEngine;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorCopyEngineTryCopyTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void TryCopy_Layers_ReturnsFalse_WhenGivenEmptyCollection()
        {
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");

            bool success = VirtualAnimatorCopyEngine.TryCopy(Array.Empty<VirtualLayer>(), parentController, out VirtualAnimatorCopyClipSet result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryCopy_StateMachineCategory_ReturnsFalse_WhenGivenEmptyCollection()
        {
            VirtualStateMachine ancestorStateMachine = VirtualStateMachine.Create(CloneContext, "Root");

            bool success = VirtualAnimatorCopyEngine.TryCopy(Array.Empty<object>(), ancestorStateMachine, out VirtualAnimatorCopyClipSet result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryCopy_Behaviours_ReturnsFalse_WhenGivenEmptyCollection()
        {
            bool success = VirtualAnimatorCopyEngine.TryCopy(Array.Empty<StateMachineBehaviour>(), out VirtualAnimatorCopyClipSet result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryCopy_Layers_ReturnsTrueAndPopulatesResult_WhenValidLayerGiven()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = sm;
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");

            bool success = VirtualAnimatorCopyEngine.TryCopy(layer, parentController, out VirtualAnimatorCopyClipSet result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers, result.Type);
        }

        [Test]
        public void Copy_Layers_StillThrows_WhenGivenEmptyCollection()
        {
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");

            Assert.Throws<ArgumentException>(() => VirtualAnimatorCopyEngine.Copy(Array.Empty<VirtualLayer>(), parentController));
        }

        [Test]
        public void TryCopy_Behaviour_ReturnsTrueAndPopulatesResult_WhenValidBehaviourGiven()
        {
            StateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();

            bool success = VirtualAnimatorCopyEngine.TryCopy(behaviour, out VirtualAnimatorCopyClipSet result);

            Assert.IsTrue(success);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours, result.Type);
        }

        [Test]
        public void Copy_ObjectWithoutAncestorValidation_ReturnsClipSet()
        {
            VirtualState state = VirtualState.Create("State1");

            VirtualAnimatorCopyClipSet result = VirtualAnimatorCopyEngine.Copy((object)state);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState, result.Type);
        }
    }
}
