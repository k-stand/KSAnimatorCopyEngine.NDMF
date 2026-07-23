using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf.animator;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor
{
    // VirtualNodeグラフの「形」(どのノードがどの子を持つか)を1箇所に集約する。
    // VirtualAnimatorCloner.ValidateRegistration*とVirtualAnimatorCloneResultValidator.Validate*CloneResultが
    // 個別に手書きする子要素の列挙を共通化する。生API版AnimatorGraphSchemaの列挙範囲(Behaviours込み、
    // null要素も検証目的で無条件yieldする)を忠実に踏襲する。VirtualState.Motionは生API版のいずれの
    // 実装も辿っていなかったため対象外のまま(生API版AnimatorGraphSchema.csのコメント参照)。
    internal static class VirtualAnimatorGraphSchema
    {
        internal static IEnumerable<(string MemberName, object Child)> GetChildren(VirtualAnimatorController target)
        {
            VirtualLayer[] layers = target.Layers.ToArray();
            for (int i = 0; i < layers.Length; i++)
            {
                yield return ($"{nameof(target.Layers)}[{i}]", layers[i]);
            }
        }

        internal static IEnumerable<(string MemberName, object Child)> GetChildren(VirtualLayer target)
        {
            yield return (nameof(target.StateMachine), target.StateMachine);

            foreach (KeyValuePair<VirtualState, VirtualMotion> pair in target.SyncedLayerMotionOverrides)
            {
                yield return ($"{nameof(target.SyncedLayerMotionOverrides)}.Key", pair.Key);
                yield return ($"{nameof(target.SyncedLayerMotionOverrides)}.Value", pair.Value);
            }

            foreach (KeyValuePair<VirtualState, ImmutableList<StateMachineBehaviour>> pair in target.SyncedLayerBehaviourOverrides)
            {
                yield return ($"{nameof(target.SyncedLayerBehaviourOverrides)}.Key", pair.Key);
                foreach (StateMachineBehaviour behaviour in pair.Value)
                {
                    yield return ($"{nameof(target.SyncedLayerBehaviourOverrides)}.Value", behaviour);
                }
            }
        }

        internal static IEnumerable<(string MemberName, object Child)> GetChildren(VirtualStateMachine target)
        {
            VirtualStateMachine.VirtualChildState[] states = target.States.ToArray();
            for (int i = 0; i < states.Length; i++)
            {
                yield return ($"{nameof(target.States)}[{i}].{nameof(VirtualStateMachine.VirtualChildState.State)}", states[i].State);
            }

            VirtualStateMachine.VirtualChildStateMachine[] stateMachines = target.StateMachines.ToArray();
            for (int i = 0; i < stateMachines.Length; i++)
            {
                yield return ($"{nameof(target.StateMachines)}[{i}].{nameof(VirtualStateMachine.VirtualChildStateMachine.StateMachine)}", stateMachines[i].StateMachine);
            }

            yield return (nameof(target.DefaultState), target.DefaultState);

            VirtualTransition[] entryTransitions = target.EntryTransitions.ToArray();
            for (int i = 0; i < entryTransitions.Length; i++)
            {
                yield return ($"{nameof(target.EntryTransitions)}[{i}]", entryTransitions[i]);
            }

            VirtualStateTransition[] anyStateTransitions = target.AnyStateTransitions.ToArray();
            for (int i = 0; i < anyStateTransitions.Length; i++)
            {
                yield return ($"{nameof(target.AnyStateTransitions)}[{i}]", anyStateTransitions[i]);
            }

            foreach (VirtualStateMachine.VirtualChildStateMachine curCSM in stateMachines)
            {
                if (curCSM.StateMachine != null && target.StateMachineTransitions.TryGetValue(curCSM.StateMachine, out ImmutableList<VirtualTransition> transitions))
                {
                    for (int i = 0; i < transitions.Count; i++)
                    {
                        yield return ($"StateMachineTransitions[{curCSM.StateMachine?.Name}][{i}]", transitions[i]);
                    }
                }
            }

            StateMachineBehaviour[] behaviours = target.Behaviours.ToArray();
            for (int i = 0; i < behaviours.Length; i++)
            {
                yield return ($"{nameof(target.Behaviours)}[{i}]", behaviours[i]);
            }
        }

        internal static IEnumerable<(string MemberName, object Child)> GetChildren(VirtualState target)
        {
            VirtualStateTransition[] transitions = target.Transitions.ToArray();
            for (int i = 0; i < transitions.Length; i++)
            {
                yield return ($"{nameof(target.Transitions)}[{i}]", transitions[i]);
            }

            StateMachineBehaviour[] behaviours = target.Behaviours.ToArray();
            for (int i = 0; i < behaviours.Length; i++)
            {
                yield return ($"{nameof(target.Behaviours)}[{i}]", behaviours[i]);
            }
        }

        internal static IEnumerable<(string MemberName, object Child)> GetChildren(VirtualTransition target)
        {
            yield return (nameof(target.DestinationState), target.DestinationState);
            yield return (nameof(target.DestinationStateMachine), target.DestinationStateMachine);
        }

        // IsExit時はDestinationState/DestinationStateMachineが未設定(null)で正常なため子として列挙しない。
        internal static IEnumerable<(string MemberName, object Child)> GetChildren(VirtualStateTransition target)
        {
            if (target.IsExit) yield break;

            yield return (nameof(target.DestinationState), target.DestinationState);
            yield return (nameof(target.DestinationStateMachine), target.DestinationStateMachine);
        }
    }
}
