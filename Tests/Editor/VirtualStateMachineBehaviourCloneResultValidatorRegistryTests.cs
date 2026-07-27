using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualStateMachineBehaviourCloneResultValidatorRegistryTests
    {
        private sealed class DerivedDummyStateMachineBehaviour : DummyStateMachineBehaviour { }

        private sealed class StubValidator : IVirtualStateMachineBehaviourCloneResultValidator
        {
            public Type BehaviourType => typeof(DummyStateMachineBehaviour);

            public IEnumerable<(string MemberName, object Child)> GetChildren(StateMachineBehaviour behaviour) => Array.Empty<(string, object)>();
        }

        [Test]
        public void Resolve_ReturnsNullForUnregisteredType()
        {
            VirtualStateMachineBehaviourCloneResultValidatorRegistry registry = new();
            Assert.IsNull(registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Register_ThenResolve_ReturnsRegisteredValidatorForExactType()
        {
            VirtualStateMachineBehaviourCloneResultValidatorRegistry registry = new();
            StubValidator validator = new();
            registry.Register(validator);

            Assert.AreSame(validator, registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Resolve_WalksBaseTypeForSubclass()
        {
            VirtualStateMachineBehaviourCloneResultValidatorRegistry registry = new();
            StubValidator validator = new();
            registry.Register(validator);

            Assert.AreSame(validator, registry.Resolve(typeof(DerivedDummyStateMachineBehaviour)));
        }

        [Test]
        public void Register_ThrowsArgumentNullException_WhenValidatorIsNull()
        {
            VirtualStateMachineBehaviourCloneResultValidatorRegistry registry = new();
            Assert.Throws<ArgumentNullException>(() => registry.Register(null));
        }

        [Test]
        public void Unregister_RemovesRegisteredValidator()
        {
            VirtualStateMachineBehaviourCloneResultValidatorRegistry registry = new();
            registry.Register(new StubValidator());
            registry.Unregister(typeof(DummyStateMachineBehaviour));

            Assert.IsNull(registry.Resolve(typeof(DummyStateMachineBehaviour)));
        }

        [Test]
        public void Shared_HasNoValidatorsRegisteredByDefault()
        {
            Assert.IsNull(VirtualStateMachineBehaviourCloneResultValidatorRegistry.Shared.Resolve(typeof(DummyStateMachineBehaviour)));
        }
    }
}
