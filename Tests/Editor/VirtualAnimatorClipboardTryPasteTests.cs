using System.Linq;
using NUnit.Framework;
using UnityEngine;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.tests
{
    public class VirtualAnimatorClipboardTryPasteTests : VirtualAnimatorClipboardTestFixtureBase
    {
        [Test]
        public void TryPasteLayers_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)state);
            VirtualAnimatorController destController = VirtualAnimatorController.Create(CloneContext, "Dest");

            bool success = VirtualAnimatorClipboard.TryPasteLayers(clipSet, destController, CloneContext, out VirtualLayer[] result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void TryPasteIntoStateMachine_ReturnsFalse_WhenClipSetTypeMismatches()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = sm;
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy(layer, parentController);
            VirtualStateMachine destStateMachine = VirtualStateMachine.Create(CloneContext, "Dest");

            bool success = VirtualAnimatorClipboard.TryPasteIntoStateMachine(clipSet, destStateMachine, CloneContext, out object[] result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [Test]
        public void PasteLayers_StillThrows_WhenClipSetTypeMismatches()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)state);
            VirtualAnimatorController destController = VirtualAnimatorController.Create(CloneContext, "Dest");

            Assert.Throws<VirtualAnimatorCopyClipSetTypeMismatchException>(() => VirtualAnimatorClipboard.PasteLayers(clipSet, destController, CloneContext));
        }

        [Test]
        public void TryPasteLayers_ReturnsTrueAndAddsLayerToDestController()
        {
            VirtualStateMachine sm = VirtualStateMachine.Create(CloneContext, "SM1");
            VirtualLayer layer = VirtualLayer.Create(CloneContext, "Layer1");
            layer.StateMachine = sm;
            VirtualAnimatorController parentController = VirtualAnimatorController.Create(CloneContext, "Controller");
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy(layer, parentController);
            VirtualAnimatorController destController = VirtualAnimatorController.Create(CloneContext, "Dest");

            bool success = VirtualAnimatorClipboard.TryPasteLayers(clipSet, destController, CloneContext, out VirtualLayer[] result);

            Assert.IsTrue(success);
            Assert.AreEqual(1, result.Length);
            Assert.AreNotSame(layer, result[0]);
            Assert.AreEqual(1, destController.Layers.Count());
            Assert.AreSame(result[0], destController.Layers.First());
        }

        [Test]
        public void TryPasteIntoStateMachine_ReturnsTrueAndPastesObjects_WhenClipSetTypeMatches()
        {
            VirtualState state = VirtualState.Create("State1");
            VirtualStateMachine.VirtualChildState childState = new() { State = state };
            VirtualTransition transition = VirtualTransition.Create();
            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy(new object[] { childState, transition });

            VirtualStateMachine destStateMachine = VirtualStateMachine.Create(CloneContext, "Dest");

            bool success = VirtualAnimatorClipboard.TryPasteIntoStateMachine(clipSet, destStateMachine, CloneContext, out object[] result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, destStateMachine.States.Count);
        }

        // 遷移を持つVirtualStateを、自分自身が属するStateMachine(祖先の子孫として自分自身を含むスコープ)へ
        // 貼り付けるケース。v2ではChildAnimatorState相当のClonePolicy登録がstate本体のみに留まっていたため、
        // 貼り付け先スコープ側の一括KeepReference登録がstateのtransitionsを先に捕捉してしまい、
        // クローン時に「親がCloneのオブジェクトの子にKeepReferenceが設定されている」例外が発生していた
        // (v2アーカイブ、.superpowers/sdd/archive-ndmf-v2-20260719/参照)。このテストで再発を防ぐ。
        [Test]
        public void TryPasteIntoStateMachine_PastesStateWithTransitions_WhenDestinationIsWithinSameAncestorScope()
        {
            VirtualStateMachine ancestorStateMachine = VirtualStateMachine.Create(CloneContext, "Ancestor");
            VirtualState state = ancestorStateMachine.AddState("State1");
            VirtualStateTransition selfTransition = VirtualStateTransition.Create();
            selfTransition.SetDestination(state);
            state.Transitions = state.Transitions.Add(selfTransition);

            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)state, ancestorStateMachine);

            bool success = VirtualAnimatorClipboard.TryPasteIntoStateMachine(clipSet, ancestorStateMachine, CloneContext, out object[] result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }

        // entryTransition単体を、Parent+PropertyNameインデックス(Task7で構築)を使ってEntryTransitionsコレクションへ
        // 正しくルーティングできるかを検証するテスト。
        //
        // 実装前検証で判明した注意点: 当初のシナリオ案(ancestorStateMachineの子孫ではない完全に無関係な
        // destStateMachineへ貼り付ける)は、以下の理由でテストとして成立しない。
        // - VirtualTransitionCopyObjectKind.GetCloneScopeはentryTransition自身のみを返し、DestinationState
        //   (state)をクローンスコープに含めない。
        // - VirtualAnimatorCloner.RegisterChildrenRecursivelyのswitch文にはVirtualTransition/
        //   VirtualStateTransitionのcaseが無く、DestinationStateへClonePolicyを伝播しない。
        // - destStateMachineがancestorStateMachineの子孫でない場合、TryPasteIntoStateMachineのelse分岐
        //   (destStateMachineとその子孫のみKeepReference)ではstateにポリシーが設定されず、
        //   DefaultPolicy(Detach)に解決される。
        // - その結果、クローン後のentryTransitionはDestinationState/DestinationStateMachineが両方null、
        //   IsExitもfalse(VirtualTransition.Create()直後のデフォルト、NDMF VirtualTransitionBase.cs参照)に
        //   なり、TryPasteIntoStateMachine内の「Transition先が設定できていないなら」ガード(早期continue)に
        //   該当してしまい、EntryTransitionsへの追加処理へ到達する前にスキップされる。
        // 生API版AnimatorClipboard.cs(456-460行目)・TransitionCopyObjectKind.cs・AnimatorCloner.csの
        // RegisterChildrenRecursivelyも全く同一の構造であり、これはVirtual版固有の不具合ではなく、
        // 生API版から忠実に移植した結果そのまま再現された生API版自体の挙動特性である
        // (詳細はtask-9-report.md参照)。そのため貼り付け先は、DestinationStateが解決可能な
        // 「ancestorStateMachineの子孫であるStateMachine」に変更し、EntryTransitionsへのルーティング
        // ロジックそのものを検証できるようにしている。
        [Test]
        public void TryPasteIntoStateMachine_PastesEntryTransition_ToEntryTransitionsCollection()
        {
            VirtualStateMachine ancestorStateMachine = VirtualStateMachine.Create(CloneContext, "Ancestor");
            VirtualState state = ancestorStateMachine.AddState("State1");
            VirtualTransition entryTransition = VirtualTransition.Create();
            entryTransition.SetDestination(state);
            ancestorStateMachine.EntryTransitions = ancestorStateMachine.EntryTransitions.Add(entryTransition);

            VirtualAnimatorCopyClipSet clipSet = VirtualAnimatorClipboard.Copy((object)entryTransition, ancestorStateMachine);

            VirtualStateMachine destStateMachine = VirtualStateMachine.Create(CloneContext, "Dest");
            ancestorStateMachine.StateMachines = ancestorStateMachine.StateMachines.Add(new VirtualStateMachine.VirtualChildStateMachine { StateMachine = destStateMachine });

            bool success = VirtualAnimatorClipboard.TryPasteIntoStateMachine(clipSet, destStateMachine, CloneContext, out object[] result);

            Assert.IsTrue(success);
            // destStateMachineはancestorStateMachineの子孫(同一祖先スコープ内)なので、entryTransitionの
            // DestinationState(state)はKeepReferenceで元の参照が維持され、Parent+PropertyNameインデックス
            // によりEntryTransitionsコレクションへ正しくルーティングされる。
            Assert.AreEqual(1, destStateMachine.EntryTransitions.Count);
        }
    }
}
