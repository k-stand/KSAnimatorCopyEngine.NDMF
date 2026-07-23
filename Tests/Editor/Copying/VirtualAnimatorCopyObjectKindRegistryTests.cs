using NUnit.Framework;
using nadena.dev.ndmf.animator;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests.Copying
{
    public class VirtualAnimatorCopyObjectKindRegistryTests
    {
        [Test]
        public void Resolve_ReturnsRegisteredKindForExactType()
        {
            IVirtualAnimatorCopyObjectKind kind = VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(VirtualLayer));
            Assert.IsNotNull(kind);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers, kind.SingleClipSetType);
        }

        [Test]
        public void Resolve_ReturnsNullForUnregisteredType()
        {
            IVirtualAnimatorCopyObjectKind kind = VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(string));
            Assert.IsNull(kind);
        }

        [Test]
        public void Resolve_WalksBaseTypeForStateMachineBehaviourSubclass()
        {
            IVirtualAnimatorCopyObjectKind kind = VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(DummyStateMachineBehaviour));
            Assert.IsNotNull(kind);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours, kind.SingleClipSetType);
        }

        [Test]
        public void Resolve_WalksBaseTypeToGenericNodeFallbackForUnregisteredVirtualNodeSubclass()
        {
            IVirtualAnimatorCopyObjectKind kind = VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(VirtualAvatarMask));
            Assert.IsNotNull(kind);
            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Other, kind.SingleClipSetType);
        }

        [Test]
        public void Normalize_ConvertsVirtualStateToVirtualChildState()
        {
            VirtualState state = VirtualState.Create("State1");

            object normalized = VirtualAnimatorCopyObjectKindRegistry.Shared.Normalize(state);

            Assert.IsInstanceOf<VirtualStateMachine.VirtualChildState>(normalized);
            Assert.AreEqual(state, ((VirtualStateMachine.VirtualChildState)normalized).State);
        }

        [Test]
        public void Normalize_ConvertsVirtualStateMachineToVirtualChildStateMachine()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(new CloneContext(GenericPlatformAnimatorBindings.Instance), "Root");

            object normalized = VirtualAnimatorCopyObjectKindRegistry.Shared.Normalize(sm);

            Assert.IsInstanceOf<VirtualStateMachine.VirtualChildStateMachine>(normalized);
            Assert.AreEqual(sm, ((VirtualStateMachine.VirtualChildStateMachine)normalized).StateMachine);
        }

        [Test]
        public void Normalize_ReturnsSameObjectWhenNoNormalizerRegistered()
        {
            VirtualLayer layer = VirtualLayer.Create(new CloneContext(GenericPlatformAnimatorBindings.Instance), "Layer1");

            object normalized = VirtualAnimatorCopyObjectKindRegistry.Shared.Normalize(layer);

            Assert.AreEqual(layer, normalized);
        }

        [Test]
        public void Normalize_ReturnsNullForNullInput()
        {
            Assert.IsNull(VirtualAnimatorCopyObjectKindRegistry.Shared.Normalize(null));
        }
    }
}
