using System;
using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests
{
    public class VirtualAnimatorCopyClipSetTypeResolutionTests : VirtualAnimatorClipboardTestFixtureBase
    {
        [Test]
        public void SingleLayer_ResolvesToLayers()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = sm;

            VirtualAnimatorCopyClipSet clipSet = new(layer, controller);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers, clipSet.Type);
        }

        [Test]
        public void MultipleLayers_ResolvesToLayers()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");
            VirtualLayer layer1 = VirtualLayer.Create(CloneContext, "Layer1");
            VirtualLayer layer2 = VirtualLayer.Create(CloneContext, "Layer2");

            VirtualAnimatorCopyClipSet clipSet = new(new[] { layer1, layer2 }, controller);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers, clipSet.Type);
        }

        [Test]
        public void SingleVirtualChildState_ResolvesToChildState()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualStateMachine.VirtualChildState childState = new() { State = state };

            VirtualAnimatorCopyClipSet clipSet = new((object)childState);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState, clipSet.Type);
        }

        [Test]
        public void PlainVirtualState_IsNormalizedAndResolvesToChildState()
        {
            VirtualState state = VirtualState.Create("State1");

            VirtualAnimatorCopyClipSet clipSet = new((object)state);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildState, clipSet.Type);
        }

        [Test]
        public void PlainVirtualStateMachine_IsNormalizedAndResolvesToChildStateMachine()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");

            VirtualAnimatorCopyClipSet clipSet = new((object)sm);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.ChildStateMachine, clipSet.Type);
        }

        [Test]
        public void TwoVirtualChildStates_ResolvesToInStateMachineObjects()
        {
            VirtualStateMachine.VirtualChildState childState1 = new() { State = VirtualState.Create("State1") };
            VirtualStateMachine.VirtualChildState childState2 = new() { State = VirtualState.Create("State2") };

            VirtualAnimatorCopyClipSet clipSet = new(new object[] { childState1, childState2 });

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.InStateMachineObjects, clipSet.Type);
        }

        [Test]
        public void MixedChildStateAndTransition_ResolvesToInStateMachineObjects()
        {
            VirtualStateMachine.VirtualChildState childState = new() { State = VirtualState.Create("State1") };
            VirtualTransition transition = VirtualTransition.Create();

            VirtualAnimatorCopyClipSet clipSet = new(new object[] { childState, transition });

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.InStateMachineObjects, clipSet.Type);
        }

        [Test]
        public void SingleTransition_ResolvesToTransition()
        {
            VirtualTransition transition = VirtualTransition.Create();

            VirtualAnimatorCopyClipSet clipSet = new((object)transition);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Transition, clipSet.Type);
        }

        [Test]
        public void SingleStateTransition_ResolvesToStateTransition()
        {
            VirtualStateTransition stateTransition = VirtualStateTransition.Create();

            VirtualAnimatorCopyClipSet clipSet = new((object)stateTransition);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.StateTransition, clipSet.Type);
        }

        [Test]
        public void EmptyClips_ResolvesToOther()
        {
            VirtualAnimatorCopyClipSet clipSet = new(Array.Empty<object>());

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Other, clipSet.Type);
        }

        [Test]
        public void UnsupportedType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new VirtualAnimatorCopyClipSet(new object()));
        }

        [Test]
        public void NullObject_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new VirtualAnimatorCopyClipSet((object)null));
        }

        [Test]
        public void UnregisteredVirtualNodeType_ResolvesToOther()
        {
            // ブリーフ原文は VirtualAvatarMask.Clone(CloneContext, ScriptableObject.CreateInstance<AvatarMask>())
            // でインスタンスを取得していたが、VirtualAvatarMask のコンストラクタ・Clone ファクトリはいずれも
            // internal であり、nadena.dev.ndmf の Editor アセンブリには本テストアセンブリ
            // (com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests) 向けの InternalsVisibleTo が
            // 付与されていないため、そのままではコンパイルエラー(CS0122)になる。
            // テストの意図(未登録のVirtualNode派生型がOtherへフォールバックすること)を保ったまま、
            // 公開APIから生成できる別のVirtualNode派生型(VirtualClip)に差し替える。VirtualClipもレジストリに
            // 個別登録されておらず、Resolveは基底型のVirtualNode(VirtualGenericNodeCopyObjectKind)まで
            // 遡ってフォールバックするため、期待する挙動は変わらない。
            VirtualClip clip = VirtualClip.Create("Clip1");

            VirtualAnimatorCopyClipSet clipSet = new((object)clip);

            Assert.AreEqual(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Other, clipSet.Type);
        }
    }
}
