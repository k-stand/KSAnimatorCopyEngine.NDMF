using NUnit.Framework;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.tests
{
    public class VirtualAnimatorNormalizationTests : VirtualAnimatorCopyEngineTestFixtureBase
    {
        [Test]
        public void NormalizeAnyStateTransitions_HoistsNestedAnyStateTransitionsToTopLevel()
        {
            VirtualStateMachine rootStateMachine = VirtualStateMachine.Create(CloneContext, "Root");
            VirtualStateMachine childStateMachine = VirtualStateMachine.Create(CloneContext, "Child");
            rootStateMachine.StateMachines = rootStateMachine.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine { StateMachine = childStateMachine });

            VirtualState targetState = childStateMachine.AddState("Target");
            VirtualStateTransition nestedAnyStateTransition = VirtualStateTransition.Create();
            nestedAnyStateTransition.SetDestination(targetState);
            childStateMachine.AnyStateTransitions = childStateMachine.AnyStateTransitions.Add(nestedAnyStateTransition);

            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = rootStateMachine;

            VirtualAnimatorNormalization.NormalizeAnyStateTransitions(layer);

            CollectionAssert.Contains(rootStateMachine.AnyStateTransitions, nestedAnyStateTransition);
            Assert.IsEmpty(childStateMachine.AnyStateTransitions);
        }

        [Test]
        public void NormalizeAnimator_DoesNotThrow_ForInMemoryController()
        {
            VirtualAnimatorController controller = VirtualAnimatorController.Create(CloneContext, "Controller");
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualLayer layer = controller.AddLayer(LayerPriority.Default, "Layer1");
            layer.StateMachine = sm;

            Assert.DoesNotThrow(() => VirtualAnimatorNormalization.NormalizeAnimator(controller));
        }

        [Test]
        public void NormalizeAnyStateTransitions_DoesNotThrow_WhenChildStateMachineEntryIsNull()
        {
            // VirtualStateMachine.VirtualChildStateMachine.StateMachineはnull許容フィールドであり、
            // nullな子StateMachineエントリを持つケースはこのポート先コードベースで既知・テスト済みである
            // (VirtualAnimatorGraphSchemaTests/VirtualAnimatorGraphTraversalTestsのnull子StateMachine対応テストを参照)。
            // GetAllStateMachineRecursivelyがこのnullエントリをガードせず再帰呼び出しすると、
            // NullReferenceExceptionが発生することを防止対象とする。
            VirtualStateMachine rootStateMachine = VirtualStateMachine.Create(CloneContext, "Root");
            rootStateMachine.StateMachines = rootStateMachine.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine { StateMachine = null });

            VirtualState targetState = rootStateMachine.AddState("Target");
            VirtualStateTransition rootAnyStateTransition = VirtualStateTransition.Create();
            rootAnyStateTransition.SetDestination(targetState);
            rootStateMachine.AnyStateTransitions = rootStateMachine.AnyStateTransitions.Add(rootAnyStateTransition);

            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = rootStateMachine;

            Assert.DoesNotThrow(() => VirtualAnimatorNormalization.NormalizeAnyStateTransitions(layer));

            CollectionAssert.Contains(rootStateMachine.AnyStateTransitions, rootAnyStateTransition);
        }
    }
}
