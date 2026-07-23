using System.Collections.Generic;
using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests
{
    public class VirtualAnimatorClonerClonePolicyTests : VirtualAnimatorClipboardTestFixtureBase
    {
        [Test]
        public void SetClonePolicy_And_TryGetClonePolicy_RoundTrips()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);
            VirtualState state = VirtualState.Create("State1");

            cloner.SetClonePolicy(state, VirtualAnimatorCloner.ClonePolicy.Clone);

            Assert.IsTrue(cloner.TryGetClonePolicy(state, out VirtualAnimatorCloner.ClonePolicy policy));
            Assert.AreEqual(VirtualAnimatorCloner.ClonePolicy.Clone, policy);
        }

        [Test]
        public void SetClonePolicyIfAbsent_DoesNotOverrideHigherPriorityPolicy()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);
            VirtualState state = VirtualState.Create("State1");
            cloner.SetClonePolicy(state, VirtualAnimatorCloner.ClonePolicy.Clone);

            cloner.SetClonePolicyIfAbsent(state, VirtualAnimatorCloner.ClonePolicy.KeepReference);

            cloner.TryGetClonePolicy(state, out VirtualAnimatorCloner.ClonePolicy policy);
            Assert.AreEqual(VirtualAnimatorCloner.ClonePolicy.Clone, policy);
        }

        [Test]
        public void SetClonePolicyIfAbsent_OverridesLowerPriorityPolicy()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);
            VirtualState state = VirtualState.Create("State1");
            cloner.SetClonePolicy(state, VirtualAnimatorCloner.ClonePolicy.Detach);

            cloner.SetClonePolicyIfAbsent(state, VirtualAnimatorCloner.ClonePolicy.Clone);

            cloner.TryGetClonePolicy(state, out VirtualAnimatorCloner.ClonePolicy policy);
            Assert.AreEqual(VirtualAnimatorCloner.ClonePolicy.Clone, policy);
        }

        [Test]
        public void SetRangeClonePolicy_AppliesPolicyToAllObjects()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);
            VirtualState state1 = VirtualState.Create("State1");
            VirtualState state2 = VirtualState.Create("State2");

            cloner.SetRangeClonePolicy(new object[] { state1, state2 }, VirtualAnimatorCloner.ClonePolicy.Clone);

            cloner.TryGetClonePolicy(state1, out VirtualAnimatorCloner.ClonePolicy policy1);
            cloner.TryGetClonePolicy(state2, out VirtualAnimatorCloner.ClonePolicy policy2);
            Assert.AreEqual(VirtualAnimatorCloner.ClonePolicy.Clone, policy1);
            Assert.AreEqual(VirtualAnimatorCloner.ClonePolicy.Clone, policy2);
        }

        [Test]
        public void RemoveClonePolicy_ClearsRegisteredPolicy()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);
            VirtualState state = VirtualState.Create("State1");
            cloner.SetClonePolicy(state, VirtualAnimatorCloner.ClonePolicy.Clone);

            cloner.RemoveClonePolicy(state);

            Assert.IsFalse(cloner.TryGetClonePolicy(state, out _));
        }

        [Test]
        public void GetAllClonePolicy_ReturnsCopyOfRegisteredPolicies()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);
            VirtualState state = VirtualState.Create("State1");
            cloner.SetClonePolicy(state, VirtualAnimatorCloner.ClonePolicy.Clone);

            Dictionary<object, VirtualAnimatorCloner.ClonePolicy> all = cloner.GetAllClonePolicy();

            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(VirtualAnimatorCloner.ClonePolicy.Clone, all[state]);
        }

        [Test]
        public void DefaultPolicy_IsDetachByDefault()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);

            Assert.AreEqual(VirtualAnimatorCloner.ClonePolicy.Detach, cloner.DefaultPolicy);
        }

        [Test]
        public void NameTransformer_ReturnsOriginalNameByDefault()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);

            Assert.AreEqual("Foo", cloner.NameTransformer("Foo"));
            Assert.AreEqual("", cloner.NameTransformer(""));
        }

        [Test]
        public void RemoveClonePolicy_WithNull_DoesNotThrow()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);

            Assert.DoesNotThrow(() => cloner.RemoveClonePolicy(null));
        }

        [Test]
        public void TryGetClonePolicy_WithNull_ReturnsFalseWithoutThrowing()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);

            bool result = false;
            Assert.DoesNotThrow(() => result = cloner.TryGetClonePolicy(null, out _));
            Assert.IsFalse(result);
        }
    }
}
