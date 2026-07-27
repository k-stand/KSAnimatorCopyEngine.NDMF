using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor
{
    /// <summary>
    /// クローン結果のVirtual Animator関連オブジェクトが、本来クローンされているべき箇所にnull参照を持っていないかを検証します。
    /// </summary>
    public static class VirtualAnimatorCloneResultValidator
    {
        /// <summary>
        /// 指定したオブジェクトを起点に、無効なnull参照を持つメンバーを再帰的に検出します。
        /// StateMachineBehaviourの子要素の検証はVirtualStateMachineBehaviourCloneResultValidatorRegistryに登録されたvalidatorに委ねられ、
        /// 未登録の型は検証対象外として無視されます。
        /// </summary>
        public static IReadOnlyCollection<InvalidNullMember> ValidateCloneResult(object target) => ValidateCloneResultInternal(target);

        /// <summary>
        /// 複数のオブジェクトに対して、まとめてValidateCloneResultを行います。
        /// </summary>
        public static IReadOnlyCollection<InvalidNullMember> ValidateCloneResults(IEnumerable<object> targets) => targets.SelectMany(t => ValidateCloneResult(t)).ToHashSet();

        private static IReadOnlyCollection<InvalidNullMember> ValidateCloneResultInternal(object target)
        {
            if (target == null)
            {
                return new List<InvalidNullMember>();
            }

            HashSet<object> visitedObjSet = new();

            return ValidateCloneResultDispatch(target, null, "", ref visitedObjSet);
        }

        // VirtualAnimatorGraphSchema.GetChildrenが列挙した子要素を、実際の型に応じて対応する
        // ValidateXxxCloneResultへ振り分ける。トップレベルのValidateCloneResultInternalと、
        // 各ノードの子要素再帰の両方から使う共通の入口。
        private static IReadOnlyCollection<InvalidNullMember> ValidateCloneResultDispatch(object target, object parent, string memberName, ref HashSet<object> visitedObjSet) => target switch
        {
            null => new InvalidNullMember[] { new(parent, memberName) },
            VirtualAnimatorController castedObj => ValidateVirtualAnimatorControllerCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            VirtualLayer castedObj => ValidateVirtualLayerCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            VirtualStateMachine.VirtualChildStateMachine castedObj => ValidateVirtualChildStateMachineCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            VirtualStateMachine castedObj => ValidateVirtualStateMachineCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            VirtualStateMachine.VirtualChildState castedObj => ValidateVirtualChildStateCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            VirtualState castedObj => ValidateVirtualStateCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            VirtualTransition castedObj => ValidateVirtualTransitionCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            VirtualStateTransition castedObj => ValidateVirtualStateTransitionCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            StateMachineBehaviour castedObj => ValidateStateMachineBehaviourCloneResult(castedObj, parent, memberName, ref visitedObjSet),
            _ => Array.Empty<InvalidNullMember>(),
        };

        private static IReadOnlyCollection<InvalidNullMember> ValidateVirtualAnimatorControllerCloneResult(VirtualAnimatorController target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return invalidNullMembers;
        }

        // 複数形の一括検証版。ValidateVirtualLayerCloneResult自体はVirtualAnimatorGraphSchema経由の
        // 再帰(ValidateVirtualAnimatorControllerCloneResult)で完結するため内部からは呼ばれないが、
        // 生API版AnimatorCloneResultValidator.ValidateAnimatorControllerLayersCloneResultおよび
        // 同ファイル内のVirtualAnimatorCloner.ValidateRegistrationVirtualLayersに合わせ、
        // 既存の利用者(テスト等)向けにinternalとして残す。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateVirtualLayersCloneResult(IEnumerable<VirtualLayer> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (VirtualLayer target in targets)
            {
                invalidNullMembers.UnionWith(ValidateVirtualLayerCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateVirtualLayerCloneResult(VirtualLayer target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateVirtualLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateVirtualChildStateMachinesCloneResult(IEnumerable<VirtualStateMachine.VirtualChildStateMachine> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (VirtualStateMachine.VirtualChildStateMachine target in targets)
            {
                invalidNullMembers.UnionWith(ValidateVirtualChildStateMachineCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateVirtualChildStateMachineCloneResult(VirtualStateMachine.VirtualChildStateMachine target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            return ValidateVirtualStateMachineCloneResult(target.StateMachine, parent, $"{memberName}.{nameof(target.StateMachine)}", ref visitedObjSet);
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateVirtualStateMachineCloneResult(VirtualStateMachine target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateVirtualLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateVirtualChildStatesCloneResult(IEnumerable<VirtualStateMachine.VirtualChildState> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (VirtualStateMachine.VirtualChildState target in targets)
            {
                invalidNullMembers.UnionWith(ValidateVirtualChildStateCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateVirtualChildStateCloneResult(VirtualStateMachine.VirtualChildState target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            return ValidateVirtualStateCloneResult(target.State, parent, $"{memberName}.{nameof(target.State)}", ref visitedObjSet);
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateVirtualStateCloneResult(VirtualState target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateVirtualLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateVirtualTransitionsCloneResult(IEnumerable<VirtualTransition> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (VirtualTransition target in targets)
            {
                invalidNullMembers.UnionWith(ValidateVirtualTransitionCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateVirtualTransitionCloneResult(VirtualTransition target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateVirtualLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateVirtualStateTransitionsCloneResult(IEnumerable<VirtualStateTransition> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (VirtualStateTransition target in targets)
            {
                invalidNullMembers.UnionWith(ValidateVirtualStateTransitionCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        private static IReadOnlyCollection<InvalidNullMember> ValidateVirtualStateTransitionCloneResult(VirtualStateTransition target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            // IsExit時はDestinationState/DestinationStateMachineが未設定(null)で正常なため、
            // VirtualAnimatorGraphSchema.GetChildrenはこの場合子要素を返さない。
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            return invalidNullMembers;
        }

        // 複数形の一括検証版。用途はValidateVirtualLayersCloneResultと同様(内部の再帰からは呼ばれない)。
        internal static IReadOnlyCollection<InvalidNullMember> ValidateStateMachineBehavioursCloneResult(IEnumerable<StateMachineBehaviour> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach (StateMachineBehaviour target in targets)
            {
                invalidNullMembers.UnionWith(ValidateStateMachineBehaviourCloneResult(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return invalidNullMembers;
        }

        // コアはStateMachineBehaviourの具象型を知らないため、検証すべき子要素は
        // VirtualStateMachineBehaviourCloneResultValidatorRegistry経由のプラグインに委ねる。
        // 未登録の型は(プラグイン導入前と同じく)無害な素通りとする。
        private static IReadOnlyCollection<InvalidNullMember> ValidateStateMachineBehaviourCloneResult(StateMachineBehaviour target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (target == null) return new InvalidNullMember[] { new(parent, memberName) };
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidNullMember>();
            visitedObjSet.Add(target);

            IVirtualStateMachineBehaviourCloneResultValidator validator = VirtualStateMachineBehaviourCloneResultValidatorRegistry.Shared.Resolve(target.GetType());
            if (validator == null) return Array.Empty<InvalidNullMember>();

            HashSet<InvalidNullMember> invalidNullMembers = new();
            foreach ((string childMemberName, object child) in validator.GetChildren(target))
            {
                invalidNullMembers.UnionWith(ValidateCloneResultDispatch(child, target, $"{memberName}.{childMemberName}", ref visitedObjSet));
            }
            return invalidNullMembers;
        }

        /// <summary>
        /// ValidateCloneResultで検出された、無効なnull参照を持つメンバー1件を表します。
        /// </summary>
        public record InvalidNullMember
        {
            /// <summary>無効なnull参照を持っていたメンバーの親オブジェクトを取得します。</summary>
            public object Parent { get; }
            /// <summary>無効なnull参照を持っていたメンバー名を取得します。</summary>
            public string MemberName { get; }

            public InvalidNullMember(object parent, string memberName)
            {
                Parent = parent;
                MemberName = memberName;
            }
        }
    }
}
