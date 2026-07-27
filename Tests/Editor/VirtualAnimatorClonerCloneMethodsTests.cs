using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorClonerCloneMethodsTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void CloneVirtualState_ReturnsDistinctClone_WithoutOutParam()
        {
            VirtualState orig = VirtualState.Create("OrigState");
            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(orig, VirtualAnimatorCloner.ClonePolicy.Clone);

            VirtualState clone = cloner.CloneVirtualState(orig);

            Assert.AreNotSame(orig, clone);
            Assert.AreEqual("OrigState", clone.Name);
        }

        [Test]
        public void CloneVirtualState_PopulatesClonedMap_WithOutParam()
        {
            VirtualState orig = VirtualState.Create("OrigState");
            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(orig, VirtualAnimatorCloner.ClonePolicy.Clone);

            VirtualState clone = cloner.CloneVirtualState(orig, out Dictionary<object, object> clonedMap);

            Assert.IsTrue(clonedMap.ContainsKey(orig));
            Assert.AreEqual(clone, clonedMap[orig]);
        }

        [Test]
        public void CloneVirtualLayers_ReturnsClonesForAllElements_WithoutOutParam()
        {
            VirtualStateMachine sm1 = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualStateMachine sm2 = VirtualStateMachine.Create(CloneContext, "SM2");
            VirtualLayer layer1 = VirtualLayer.Create(CloneContext, "Layer1");
            layer1.StateMachine = sm1;
            VirtualLayer layer2 = VirtualLayer.Create(CloneContext, "Layer2");
            layer2.StateMachine = sm2;

            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(sm1, VirtualAnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(sm2, VirtualAnimatorCloner.ClonePolicy.Clone);

            List<VirtualLayer> clones = new(cloner.CloneVirtualLayers(new[] { layer1, layer2 }));

            Assert.AreEqual(2, clones.Count);
            Assert.AreEqual("Layer1", clones[0].Name);
            Assert.AreEqual("Layer2", clones[1].Name);
        }

        [Test]
        public void CloneVirtualLayers_PopulatesClonedMap_WithOutParam()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = sm;

            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(sm, VirtualAnimatorCloner.ClonePolicy.Clone);

            List<VirtualLayer> clones = new(cloner.CloneVirtualLayers(new[] { layer }, out Dictionary<object, object> clonedMap));

            Assert.IsTrue(clonedMap.ContainsKey(sm));
            Assert.AreEqual(clones[0].StateMachine, clonedMap[sm]);
        }

        [Test]
        public void CloneVirtualLayer_WithNull_ReturnsNullWithoutThrowing()
        {
            VirtualAnimatorCloner cloner = new(CloneContext);

            VirtualLayer result = null;
            Assert.DoesNotThrow(() => result = cloner.CloneVirtualLayer(null));
            Assert.IsNull(result);
        }

        [Test]
        public void CloneObject_ReturnsCorrectlyTypedClone_ForVirtualState()
        {
            VirtualState orig = VirtualState.Create("OrigState");
            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(orig, VirtualAnimatorCloner.ClonePolicy.Clone);

            object clone = cloner.CloneObject(orig);

            Assert.IsInstanceOf<VirtualState>(clone);
            Assert.AreNotSame(orig, clone);
        }

        [Test]
        public void ForEachCloned_InstanceOverload_InvokesCallbackForClonedPairsOfMatchingType()
        {
            VirtualClip origClip = VirtualClip.Create("Clip1");
            VirtualState origState = VirtualState.Create("State1");
            origState.Motion = origClip;

            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(origState, VirtualAnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(origClip, VirtualAnimatorCloner.ClonePolicy.Clone);

            VirtualState cloneState = cloner.CloneVirtualState(origState);

            List<(VirtualState Orig, VirtualState Clone)> calls = new();
            cloner.ForEachCloned<VirtualState>((orig, clone) => calls.Add((orig, clone)));

            Assert.AreEqual(1, calls.Count);
            Assert.AreSame(origState, calls[0].Orig);
            Assert.AreSame(cloneState, calls[0].Clone);
        }

        [Test]
        public void ForEachCloned_StaticOverload_ExcludesSelfPairs()
        {
            VirtualState realOrig = VirtualState.Create("Orig");
            VirtualState realClone = VirtualState.Create("Clone");
            VirtualState selfPaired = VirtualState.Create("Self");

            Dictionary<object, object> clonedMap = new()
            {
                [realOrig] = realClone,
                [selfPaired] = selfPaired,
            };

            List<(VirtualState Orig, VirtualState Clone)> calls = new();
            VirtualAnimatorCloner.ForEachCloned<VirtualState>(clonedMap, (orig, clone) => calls.Add((orig, clone)));

            Assert.AreEqual(1, calls.Count);
            Assert.AreSame(realOrig, calls[0].Orig);
            Assert.AreSame(realClone, calls[0].Clone);
        }

        [Test]
        public void CloneVirtualState_Throws_WhenClonePolicyIsUnsetAndDefaultPolicyIsUnSetting()
        {
            VirtualAnimatorCloner cloner = new(CloneContext) { DefaultPolicy = VirtualAnimatorCloner.ClonePolicy.UnSetting };
            VirtualState orig = VirtualState.Create("OrigState");

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => cloner.CloneVirtualState(orig));
            Assert.AreEqual("ClonePolicyが未設定のオブジェクトをクローンしようとしました", ex.Message);
        }

        [Test]
        public void CloneVirtualState_WithUnregisteredVirtualClipMotion_KeepsOriginalReference()
        {
            VirtualClip motionClip = VirtualClip.Create("Clip1");
            VirtualState orig = VirtualState.Create("OrigState");
            orig.Motion = motionClip;
            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(orig, VirtualAnimatorCloner.ClonePolicy.Clone);

            VirtualState clone = cloner.CloneVirtualState(orig);

            Assert.AreSame(motionClip, clone.Motion);
        }

        [Test]
        public void CloneVirtualState_WithUnregisteredVirtualBlendTreeMotion_KeepsOriginalReference()
        {
            VirtualBlendTree motionTree = VirtualBlendTree.Create("Tree1");
            VirtualState orig = VirtualState.Create("OrigState");
            orig.Motion = motionTree;
            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(orig, VirtualAnimatorCloner.ClonePolicy.Clone);

            VirtualState clone = cloner.CloneVirtualState(orig);

            Assert.AreSame(motionTree, clone.Motion);
        }

        [Test]
        public void CloneVirtualState_WithExplicitlyClonedVirtualBlendTreeMotion_ReturnsDistinctClone()
        {
            VirtualBlendTree motionTree = VirtualBlendTree.Create("OrigTree");
            VirtualState orig = VirtualState.Create("OrigState");
            orig.Motion = motionTree;
            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(orig, VirtualAnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(motionTree, VirtualAnimatorCloner.ClonePolicy.Clone);

            VirtualState clone = cloner.CloneVirtualState(orig);

            Assert.AreNotSame(motionTree, clone.Motion);
            Assert.AreEqual("OrigTree", clone.Motion.Name);
        }

        [Test]
        public void CloneVirtualState_WithExplicitlyClonedVirtualClipMotion_CopiesCurves()
        {
            VirtualClip motionClip = VirtualClip.Create("OrigClip");
            motionClip.SetFloatCurve("Transform", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0, 1, 1f));
            VirtualState orig = VirtualState.Create("OrigState");
            orig.Motion = motionClip;
            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(orig, VirtualAnimatorCloner.ClonePolicy.Clone);
            cloner.SetClonePolicy(motionClip, VirtualAnimatorCloner.ClonePolicy.Clone);

            VirtualState clone = cloner.CloneVirtualState(orig);

            VirtualClip cloneClip = (VirtualClip)clone.Motion;
            Assert.AreNotSame(motionClip, cloneClip);
            Assert.IsNotNull(cloneClip.GetFloatCurve("Transform", typeof(Transform), "localPosition.x"));
        }

        [Test]
        public void CloneVirtualStateMachine_ClonesNestedStateMachinesStatesAndTransitions()
        {
            VirtualStateMachine root = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualState state1 = root.AddState("State1");
            VirtualStateTransition transition = VirtualStateTransition.Create();
            transition.SetDestination(state1);
            state1.Transitions = state1.Transitions.Add(transition);

            VirtualAnimatorCloner cloner = new(CloneContext);
            cloner.SetClonePolicy(root, VirtualAnimatorCloner.ClonePolicy.Clone);

            VirtualStateMachine clone = cloner.CloneVirtualStateMachine(root);

            Assert.AreNotSame(root, clone);
            Assert.AreEqual(1, clone.States.Count);
            Assert.AreNotSame(state1, clone.States[0].State);
            Assert.AreEqual(1, clone.States[0].State.Transitions.Count);
            Assert.AreSame(clone.States[0].State, clone.States[0].State.Transitions[0].DestinationState);
        }
    }
}
