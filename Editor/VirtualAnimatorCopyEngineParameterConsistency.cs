using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using nadena.dev.ndmf.animator;
using com.github.k_stand.ksanimatorcopyengine.ndmf.editor.CrossController;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor
{
    /// <summary>
    /// コピーしたオブジェクトが参照しているAnimatorControllerParameterのうち、貼り付け先のVirtualAnimatorControllerに存在しないものを検出します。
    /// </summary>
    public static class VirtualAnimatorCopyEngineParameterConsistency
    {
        /// <summary>
        /// clipSetが参照しているパラメーター名のうち、destControllerに存在しないものを列挙します。
        /// StateMachineBehaviourが参照するパラメーターは、本パッケージ内部で固定登録されたresolver(現状VRCAvatarParameterDriver)を
        /// 経由して収集されます(未登録の型は参照なしとして扱われます)。
        /// </summary>
        /// <param name="clipSet">検証対象のVirtualAnimatorCopyClipSet。</param>
        /// <param name="destController">存在確認の基準にする貼り付け先のVirtualAnimatorController。</param>
        /// <returns>destControllerに存在しないパラメーター名の一覧。</returns>
        /// <exception cref="ArgumentNullException">clipSetまたはdestControllerがnullの場合。</exception>
        public static IReadOnlyList<string> FindMissingParameters(VirtualAnimatorCopyClipSet clipSet, VirtualAnimatorController destController)
        {
            if (clipSet == null) throw new ArgumentNullException(nameof(clipSet));
            if (destController == null) throw new ArgumentNullException(nameof(destController));

            HashSet<string> referencedParameterNames = new();
            foreach (VirtualAnimatorCopyClip clip in clipSet.Clips)
            {
                CollectReferencedParameterNames(clip.Object, referencedParameterNames);
            }

            HashSet<string> existingParameterNames = destController.Parameters.Keys.ToHashSet();
            return referencedParameterNames.Where(name => !existingParameterNames.Contains(name)).ToList();
        }

        private static void CollectReferencedParameterNames(object obj, HashSet<string> result)
        {
            switch (obj)
            {
                case VirtualLayer layer:
                    CollectFromStateMachine(layer.StateMachine, result);
                    break;
                case VirtualStateMachine.VirtualChildStateMachine childStateMachine:
                    CollectFromStateMachine(childStateMachine.StateMachine, result);
                    break;
                case VirtualStateMachine.VirtualChildState childState:
                    CollectFromState(childState.State, result);
                    break;
                case VirtualStateTransition stateTransition:
                    CollectFromConditions(stateTransition.Conditions, result);
                    break;
                case VirtualTransition transition:
                    CollectFromConditions(transition.Conditions, result);
                    break;
                case StateMachineBehaviour behaviour:
                    CollectFromBehaviour(behaviour, result);
                    break;
            }
        }

        private static void CollectFromStateMachine(VirtualStateMachine stateMachine, HashSet<string> result)
        {
            if (stateMachine == null) return;

            Queue<VirtualStateMachine> searchQueue = new();
            searchQueue.Enqueue(stateMachine);
            HashSet<VirtualStateMachine> visited = new();

            while (searchQueue.Count > 0)
            {
                VirtualStateMachine current = searchQueue.Dequeue();
                if (!visited.Add(current)) continue;

                foreach (VirtualTransition entryTransition in current.EntryTransitions)
                {
                    CollectFromConditions(entryTransition.Conditions, result);
                }

                foreach (VirtualStateTransition anyStateTransition in current.AnyStateTransitions)
                {
                    CollectFromConditions(anyStateTransition.Conditions, result);
                }

                foreach (VirtualStateMachine.VirtualChildState childState in current.States)
                {
                    CollectFromState(childState.State, result);
                }

                foreach (VirtualStateMachine.VirtualChildStateMachine childStateMachine in current.StateMachines)
                {
                    // childStateMachine.StateMachineはnullになりうる(構造体VirtualChildStateMachineの未初期化フィールド)。
                    // ImmutableDictionary<TKey,TValue>.TryGetValueはnullキーを受け付けずArgumentNullExceptionを
                    // 投げるため事前にnullガードする。さらに、ガードせずEnqueueするとBFSのvisited.Addはnullを受理してしまい
                    // (HashSet<T>はnull格納可能)、後続のDequeueでcurrentがnullになりNullReferenceExceptionが発生するため、
                    // Enqueue自体もnullガードする(Task2/5/6/7、及び直近のVirtualAnimatorGraphTraversalホットフィックスと同型の既知パターン)。
                    if (childStateMachine.StateMachine != null && current.StateMachineTransitions.TryGetValue(childStateMachine.StateMachine, out var subMachineTransitions))
                    {
                        foreach (VirtualTransition subMachineTransition in subMachineTransitions)
                        {
                            CollectFromConditions(subMachineTransition.Conditions, result);
                        }
                    }

                    if (childStateMachine.StateMachine != null)
                    {
                        searchQueue.Enqueue(childStateMachine.StateMachine);
                    }
                }
            }
        }

        private static void CollectFromState(VirtualState state, HashSet<string> result)
        {
            if (state == null) return;

            foreach (VirtualStateTransition transition in state.Transitions)
            {
                CollectFromConditions(transition.Conditions, result);
            }

            foreach (StateMachineBehaviour behaviour in state.Behaviours)
            {
                CollectFromBehaviour(behaviour, result);
            }
        }

        private static void CollectFromConditions(IEnumerable<AnimatorCondition> conditions, HashSet<string> result)
        {
            foreach (AnimatorCondition condition in conditions)
            {
                result.Add(condition.parameter);
            }
        }

        private static void CollectFromBehaviour(StateMachineBehaviour behaviour, HashSet<string> result)
        {
            if (behaviour == null) return;

            IVirtualParameterReferenceResolver resolver = VirtualParameterReferenceResolverRegistry.Shared.Resolve(behaviour.GetType());
            if (resolver == null) return;

            foreach (string parameterName in resolver.GetReferencedParameterNames(behaviour))
            {
                result.Add(parameterName);
            }
        }
    }
}
