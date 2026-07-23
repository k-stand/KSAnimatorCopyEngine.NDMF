using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests
{
    public class VirtualReferenceRemapperTests : VirtualAnimatorClipboardTestFixtureBase
    {
        [Test]
        public void RemappingRecursively_UnifiesDuplicateBlendTreeClones_FromIndependentCloneOperations()
        {
            VirtualBlendTree origBlendTree = VirtualBlendTree.Create("Shared");

            VirtualAnimatorCloner cloner1 = new(CloneContext);
            cloner1.SetClonePolicy(origBlendTree, VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualBlendTree clone1 = cloner1.CloneVirtualBlendTree(origBlendTree, out Dictionary<object, object> map1);

            VirtualAnimatorCloner cloner2 = new(CloneContext);
            cloner2.SetClonePolicy(origBlendTree, VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualBlendTree clone2 = cloner2.CloneVirtualBlendTree(origBlendTree, out Dictionary<object, object> map2);

            Assert.AreNotSame(clone1, clone2);

            VirtualState state1 = VirtualState.Create("State1");
            state1.Motion = clone1;
            VirtualState state2 = VirtualState.Create("State2");
            state2.Motion = clone2;

            VirtualReferenceRemapper remapper = new();
            remapper.AddClonedMap(map1);
            remapper.AddClonedMap(map2);
            remapper.RemappingRecursively(new object[] { state1, state2 });

            Assert.AreSame(state1.Motion, state2.Motion);
        }

        [Test]
        public void GetOrigRoot_ResolvesThroughMultiHopCloneChain()
        {
            VirtualBlendTree origA = VirtualBlendTree.Create("A");

            VirtualAnimatorCloner cloner1 = new(CloneContext);
            cloner1.SetClonePolicy(origA, VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualBlendTree cloneB = cloner1.CloneVirtualBlendTree(origA, out Dictionary<object, object> map1);

            VirtualAnimatorCloner cloner2 = new(CloneContext);
            cloner2.SetClonePolicy(cloneB, VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualBlendTree cloneC = cloner2.CloneVirtualBlendTree(cloneB, out Dictionary<object, object> map2);

            VirtualReferenceRemapper remapper = new();
            remapper.AddClonedMap(map1);
            remapper.AddClonedMap(map2);

            object origRoot = remapper.GetOrigRoot(cloneC);

            Assert.AreSame(origA, origRoot);
        }

        [Test]
        public void RemappingRecursively_ReachesNestedMotionReferences_ThroughStateMachineTraversal()
        {
            VirtualBlendTree origBlendTree = VirtualBlendTree.Create("Shared");

            VirtualAnimatorCloner cloner1 = new(CloneContext);
            cloner1.SetClonePolicy(origBlendTree, VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualBlendTree clone1 = cloner1.CloneVirtualBlendTree(origBlendTree, out Dictionary<object, object> map1);

            VirtualAnimatorCloner cloner2 = new(CloneContext);
            cloner2.SetClonePolicy(origBlendTree, VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualBlendTree clone2 = cloner2.CloneVirtualBlendTree(origBlendTree, out Dictionary<object, object> map2);

            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualState state1 = sm.AddState("State1");
            state1.Motion = clone1;
            VirtualState state2 = sm.AddState("State2");
            state2.Motion = clone2;

            VirtualReferenceRemapper remapper = new();
            remapper.AddClonedMap(map1);
            remapper.AddClonedMap(map2);
            remapper.RemappingRecursively(sm);

            Assert.AreSame(state1.Motion, state2.Motion);
        }

        [Test]
        public void RemappingRecursively_ReachesNestedChildBlendTreeMotionReferences()
        {
            VirtualBlendTree origInnerTree = VirtualBlendTree.Create("SharedInner");

            VirtualAnimatorCloner cloner1 = new(CloneContext);
            cloner1.SetClonePolicy(origInnerTree, VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualBlendTree innerClone1 = cloner1.CloneVirtualBlendTree(origInnerTree, out Dictionary<object, object> map1);

            VirtualAnimatorCloner cloner2 = new(CloneContext);
            cloner2.SetClonePolicy(origInnerTree, VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualBlendTree innerClone2 = cloner2.CloneVirtualBlendTree(origInnerTree, out Dictionary<object, object> map2);

            VirtualBlendTree outerTree1 = VirtualBlendTree.Create("Outer1");
            outerTree1.Children = ImmutableList.Create(new VirtualBlendTree.VirtualChildMotion { Motion = innerClone1 });
            VirtualBlendTree outerTree2 = VirtualBlendTree.Create("Outer2");
            outerTree2.Children = ImmutableList.Create(new VirtualBlendTree.VirtualChildMotion { Motion = innerClone2 });

            VirtualState state1 = VirtualState.Create("State1");
            state1.Motion = outerTree1;
            VirtualState state2 = VirtualState.Create("State2");
            state2.Motion = outerTree2;

            VirtualReferenceRemapper remapper = new();
            remapper.AddClonedMap(map1);
            remapper.AddClonedMap(map2);
            remapper.RemappingRecursively(new object[] { state1, state2 });

            // 注意: outerTree1/outerTree2自体はクローンされていない別名(Outer1/Outer2)のBlendTreeであり、
            // トップレベルのMotion参照(state1.Motion/state2.Motion)自体が同一インスタンスになることはない。
            // このテストが検証すべきは、outerTree1/outerTree2それぞれのChildren経由でネストされた
            // innerClone1/innerClone2(同名"SharedInner"かつ複製元が同一origInnerTree)が、
            // RemappingRecursivelyのBlendTree.Children再帰によって単一インスタンスへ統合されることである。
            // ブリーフのコードはAssert.AreSame(state1.Motion, state2.Motion)となっていたが、これは
            // outerTree1/outerTree2という別名の異なるインスタンス同士を比較しており、
            // 実装が正しく動作していても常に失敗する(テストコード自体の誤り)。
            // テスト名(ReachesNestedChildBlendTreeMotionReferences)が示す意図に沿って、
            // ネストされた子Motion参照同士を比較するよう修正した。
            VirtualBlendTree resultTree1 = (VirtualBlendTree)state1.Motion;
            VirtualBlendTree resultTree2 = (VirtualBlendTree)state2.Motion;
            Assert.AreSame(resultTree1.Children[0].Motion, resultTree2.Children[0].Motion);
        }
    }
}
