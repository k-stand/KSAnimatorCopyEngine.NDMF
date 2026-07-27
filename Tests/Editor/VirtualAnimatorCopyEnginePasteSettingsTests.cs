using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorCopyEnginePasteSettingsTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void TryPasteBehaviours_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy((object)state);
            VirtualStateMachine destStateMachine = VirtualStateMachine.Create(CloneContext, "Dest");

            bool success = VirtualAnimatorCopyEngine.TryPasteBehaviours(clipSet, destStateMachine, CloneContext, out StateMachineBehaviour[] result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryPasteBehaviours_ReturnsTrueAndAppliesBehaviours_WhenClipSetTypeMatches()
        {
            DummyStateMachineBehaviour behaviour = ScriptableObject.CreateInstance<DummyStateMachineBehaviour>();
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy(behaviour);
            VirtualStateMachine destStateMachine = VirtualStateMachine.Create(CloneContext, "Dest");

            bool success = VirtualAnimatorCopyEngine.TryPasteBehaviours(clipSet, destStateMachine, CloneContext, out StateMachineBehaviour[] result);

            Assert.IsTrue(success);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, destStateMachine.Behaviours.Count);
            Assert.IsInstanceOf<DummyStateMachineBehaviour>(destStateMachine.Behaviours[0]);
        }

        [Test]
        public void TryPasteSettings_VirtualState_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            VirtualTransition transition = VirtualTransition.Create();
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy((object)transition);
            VirtualState destState = VirtualState.Create("Dest");

            bool success = VirtualAnimatorCopyEngine.TryPasteSettings(clipSet, destState);

            Assert.IsFalse(success);
        }

        [Test]
        public void TryPasteSettings_VirtualState_ReturnsTrueAndAppliesSettings_WhenClipSetTypeMatches()
        {
            VirtualState srcState = VirtualState.Create("Src");
            srcState.Speed = 2.5f;
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy((object)srcState);
            VirtualState destState = VirtualState.Create("Dest");
            destState.Speed = 1f;

            bool success = VirtualAnimatorCopyEngine.TryPasteSettings(clipSet, destState);

            Assert.IsTrue(success);
            Assert.AreEqual(2.5f, destState.Speed);
            Assert.AreEqual("Dest", destState.Name);
        }

        [Test]
        public void PasteSettings_VirtualState_StillThrows_WhenClipSetTypeMismatches()
        {
            VirtualTransition transition = VirtualTransition.Create();
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy((object)transition);
            VirtualState destState = VirtualState.Create("Dest");

            Assert.Throws<VirtualAnimatorCopyClipSetTypeMismatchException>(() => VirtualAnimatorCopyEngine.PasteSettings(clipSet, destState));
        }

        [Test]
        public void TryPasteSettings_VirtualStateTransition_ReturnsTrueAndAppliesSettings_WhenClipSetTypeMatches()
        {
            VirtualStateTransition srcStateTransition = VirtualStateTransition.Create();
            srcStateTransition.Duration = 2.5f;
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy((object)srcStateTransition);
            VirtualStateTransition destStateTransition = VirtualStateTransition.Create();
            destStateTransition.Duration = 1f;

            bool success = VirtualAnimatorCopyEngine.TryPasteSettings(clipSet, destStateTransition);

            Assert.IsTrue(success);
            Assert.AreEqual(2.5f, destStateTransition.Duration);
        }

        [Test]
        public void TryPasteConditions_VirtualTransition_ReturnsTrueAndAppliesConditions_WhenClipSetTypeMatches()
        {
            VirtualTransition srcTransition = VirtualTransition.Create();
            srcTransition.Conditions = srcTransition.Conditions.Add(new AnimatorCondition { parameter = "TestParam", mode = AnimatorConditionMode.If });
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy((object)srcTransition);
            VirtualTransition destTransition = VirtualTransition.Create();

            bool success = VirtualAnimatorCopyEngine.TryPasteConditions(clipSet, destTransition);

            Assert.IsTrue(success);
            Assert.AreEqual(1, destTransition.Conditions.Count);
            Assert.AreEqual("TestParam", destTransition.Conditions[0].parameter);
        }

        [Test]
        public void TryPasteSettingsAndConditions_VirtualStateTransition_ReturnsTrueAndAppliesBoth_WhenClipSetTypeMatches()
        {
            VirtualStateTransition srcStateTransition = VirtualStateTransition.Create();
            srcStateTransition.Duration = 3f;
            srcStateTransition.Conditions = srcStateTransition.Conditions.Add(new AnimatorCondition { parameter = "AnotherParam", mode = AnimatorConditionMode.If });
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorCopyEngine.Copy((object)srcStateTransition);
            VirtualStateTransition destStateTransition = VirtualStateTransition.Create();

            bool success = VirtualAnimatorCopyEngine.TryPasteSettingsAndConditions(clipSet, destStateTransition);

            Assert.IsTrue(success);
            Assert.AreEqual(3f, destStateTransition.Duration);
            Assert.AreEqual(1, destStateTransition.Conditions.Count);
        }
    }
}
