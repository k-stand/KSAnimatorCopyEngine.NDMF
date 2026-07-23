using System;
using NUnit.Framework;
using UnityEngine;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests
{
    public class VirtualAnimatorClipboardTryCopyTests : VirtualAnimatorClipboardTestFixtureBase
    {
        [Test]
        public void TryCopy_Layers_ReturnsFalse_WhenGivenEmptyCollection()
        {
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");

            bool success = VirtualAnimatorClipboard.TryCopy(Array.Empty<VirtualLayer>(), parentController, out VirtualAnimatorCopyClipSet result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryCopy_StateMachineCategory_ReturnsFalse_WhenGivenEmptyCollection()
        {
            VirtualStateMachine ancestorStateMachine = VirtualStateMachine.Create(CloneContext, "Root");

            bool success = VirtualAnimatorClipboard.TryCopy(Array.Empty<object>(), ancestorStateMachine, out VirtualAnimatorCopyClipSet result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryCopy_Behaviours_ReturnsFalse_WhenGivenEmptyCollection()
        {
            bool success = VirtualAnimatorClipboard.TryCopy(Array.Empty<StateMachineBehaviour>(), out VirtualAnimatorCopyClipSet result);

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

            bool success = VirtualAnimatorClipboard.TryCopy(layer, parentController, out VirtualAnimatorCopyClipSet result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers, result.Type);
        }

        [Test]
        public void Copy_Layers_StillThrows_WhenGivenEmptyCollection()
        {
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");

            Assert.Throws<ArgumentException>(() => VirtualAnimatorClipboard.Copy(Array.Empty<VirtualLayer>(), parentController));
        }

        [Test]
        public void TryCopy_Behaviour_ReturnsTrueAndPopulatesResult_WhenValidBehaviourGiven()
        {
            StateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();

            bool success = VirtualAnimatorClipboard.TryCopy(behaviour, out VirtualAnimatorCopyClipSet result);

            Assert.IsTrue(success);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours, result.Type);
        }

        [Test]
        public void Copy_ObjectWithoutAncestorValidation_ReturnsClipSet()
        {
            VirtualState state = VirtualState.Create("State1");

            VirtualAnimatorCopyClipSet result = VirtualAnimatorClipboard.Copy((object)state);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState, result.Type);
        }
    }
}
