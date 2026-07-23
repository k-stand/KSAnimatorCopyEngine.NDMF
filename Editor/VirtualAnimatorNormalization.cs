using System.Collections.Generic;
using System.Collections.Immutable;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor
{
    /// <summary>
    /// VirtualAnimatorControllerの構造をコピー&貼り付け処理が扱いやすい形へ正規化するためのユーティリティです。
    /// </summary>
    public static class VirtualAnimatorNormalization
    {
        /// <summary>
        /// VirtualAnimatorController内の全レイヤーに対してNormalizeAnyStateTransitionsを行います。
        /// </summary>
        public static void NormalizeAnimator(VirtualAnimatorController animator)
        {
            foreach (VirtualLayer layer in animator.Layers)
            {
                NormalizeAnyStateTransitions(layer);
            }

            // アセット整理部分(生API版AnimatorAssetPersistence.AddObjectToAssetRecursively/
            // RemoveUnusedSubAssets相当)は不要。NDMFのAssetSaver/CommitContextが到達可能性ベースで
            // 自動的に代替する(対応表§7/§8参照)。
        }

        /// <summary>
        /// レイヤー内の子VirtualStateMachineが持つAnyStateTransitionsを、すべてレイヤー直下のAnyStateTransitionsへ集約します。
        /// </summary>
        public static void NormalizeAnyStateTransitions(VirtualLayer layer)
        {
            if (layer.StateMachine == null) return;

            VirtualStateMachine[] innerStateMachines = GetAllStateMachineRecursively(layer.StateMachine);

            List<VirtualStateTransition> anyStateTransitions = new();
            anyStateTransitions.AddRange(layer.StateMachine.AnyStateTransitions);
            foreach (VirtualStateMachine curStateMachine in innerStateMachines)
            {
                anyStateTransitions.AddRange(curStateMachine.AnyStateTransitions);
                curStateMachine.AnyStateTransitions = ImmutableList<VirtualStateTransition>.Empty;
            }

            layer.StateMachine.AnyStateTransitions = anyStateTransitions.ToImmutableList();
        }

        private static VirtualStateMachine[] GetAllStateMachineRecursively(VirtualStateMachine stateMachine)
        {
            List<VirtualStateMachine> stateMachines = new();
            foreach (VirtualStateMachine.VirtualChildStateMachine childStateMachine in stateMachine.StateMachines)
            {
                // VirtualStateMachine.VirtualChildStateMachine.StateMachineはnull許容フィールドであり、
                // 実際にnullを許容する子StateMachineエントリを含むケースがこのポート先コードベースで
                // 既知・テスト済みである(VirtualAnimatorGraphSchema/VirtualAnimatorGraphTraversalのnull子
                // StateMachine対応と同様)。生API版のChildAnimatorStateMachine.stateMachineは
                // Unity内部の参照整合性維持によりこのケースが実質発生しないためガードが不要だったが、
                // Virtual API版はネイティブ実体を持たない構造体でありnullを許容するため、再帰呼び出しでの
                // NullReferenceExceptionを防ぐために技術的に必要な最小限のnullガードを追加している。
                if (childStateMachine.StateMachine == null) continue;

                stateMachines.Add(childStateMachine.StateMachine);
                stateMachines.AddRange(GetAllStateMachineRecursively(childStateMachine.StateMachine));
            }
            return stateMachines.ToArray();
        }
    }
}
