using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorClonerValidateRegistrationTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void ValidateRegistrationVirtualLayers_DoesNotThrow_ForLayerWithUninitializedOverrides()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = sm;
            VirtualAnimatorCloner cloner = new(CloneContext);
            HashSet<object> visitedObjSet = new();

            Assert.DoesNotThrow(() => cloner.ValidateRegistrationVirtualLayers(new[] { layer }, null, "layers", ref visitedObjSet));
        }

        [Test]
        public void ValidateRegistration_DetectsUnregisteredEntry_WhenClonePolicyIsUnset()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualAnimatorCloner cloner = new(CloneContext) { DefaultPolicy = VirtualAnimatorCloner.ClonePolicy.UnSetting };

            IReadOnlyCollection<VirtualAnimatorCloner.InvalidEntry> entries = cloner.ValidateRegistration(state);

            Assert.AreEqual(1, entries.Count);
            VirtualAnimatorCloner.InvalidEntry entry = entries.First();
            Assert.AreEqual(VirtualAnimatorCloner.InvalidType.UnregisteredEntry, entry.InvalidType);
            Assert.AreSame(state, entry.InvalidEntryObject);
        }

        [Test]
        public void ValidateRegistration_DetectsKeepReferenceChild_WhenParentIsCloneAndChildIsKeepReference()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualState state = sm.AddState("State1");

            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(sm, VirtualAnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(state, VirtualAnimatorCloner.ClonePolicy.KeepReference);

            IReadOnlyCollection<VirtualAnimatorCloner.InvalidEntry> entries = cloner.ValidateRegistration(sm);

            List<VirtualAnimatorCloner.InvalidEntry> keepReferenceEntries = new();
            foreach (VirtualAnimatorCloner.InvalidEntry entry in entries)
            {
                if (entry.InvalidType == VirtualAnimatorCloner.InvalidType.KeepReferenceChild) keepReferenceEntries.Add(entry);
            }
            Assert.AreEqual(1, keepReferenceEntries.Count);
            Assert.AreSame(state, keepReferenceEntries[0].InvalidEntryObject);
        }

        [Test]
        public void ValidateRegistration_ReturnsEmpty_ForFullyRegisteredCloneGraph()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualState state = sm.AddState("State1");

            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(sm, VirtualAnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(state, VirtualAnimatorCloner.ClonePolicy.Clone);

            IReadOnlyCollection<VirtualAnimatorCloner.InvalidEntry> entries = cloner.ValidateRegistration(sm);

            Assert.IsEmpty(entries);
        }

        [Test]
        public void ValidateRegistration_ReturnsEmpty_ForNullTarget()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);

            IReadOnlyCollection<VirtualAnimatorCloner.InvalidEntry> entries = cloner.ValidateRegistration(null);

            Assert.IsEmpty(entries);
        }
    }
}
