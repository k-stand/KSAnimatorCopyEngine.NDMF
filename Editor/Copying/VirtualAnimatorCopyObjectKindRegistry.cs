using System;
using System.Collections.Generic;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying
{
    internal sealed class VirtualAnimatorCopyObjectKindRegistry
    {
        internal static VirtualAnimatorCopyObjectKindRegistry Shared { get; } = CreateDefault();

        private readonly Dictionary<Type, IVirtualAnimatorCopyObjectKind> _kinds = new();
        private readonly Dictionary<Type, Func<object, object>> _normalizers = new();

        internal void Register(IVirtualAnimatorCopyObjectKind kind) => _kinds[kind.ObjectType] = kind;

        // Normalizeは登録型のexact matchのみで、Resolveのように基底型を辿らない。
        // 現状はVirtualState/VirtualStateMachineが対象で、いずれもsealedクラスのため問題にならないが、
        // 将来サブクラスを持つ型を正規化対象に加える場合はここを見直すこと。
        internal void RegisterNormalizer(Type sourceType, Func<object, object> normalize) => _normalizers[sourceType] = normalize;

        internal IVirtualAnimatorCopyObjectKind Resolve(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (_kinds.TryGetValue(current, out IVirtualAnimatorCopyObjectKind kind))
                {
                    return kind;
                }
            }

            return null;
        }

        internal object Normalize(object obj)
        {
            if (obj == null) return null;

            return _normalizers.TryGetValue(obj.GetType(), out Func<object, object> normalize) ? normalize(obj) : obj;
        }

        private static VirtualAnimatorCopyObjectKindRegistry CreateDefault()
        {
            VirtualAnimatorCopyObjectKindRegistry registry = new();

            registry.Register(new VirtualLayerCopyObjectKind());
            registry.Register(new VirtualChildStateCopyObjectKind());
            registry.Register(new VirtualChildStateMachineCopyObjectKind());
            registry.Register(new VirtualTransitionCopyObjectKind());
            registry.Register(new VirtualStateTransitionCopyObjectKind());
            registry.Register(new VirtualStateMachineBehaviourCopyObjectKind());
            registry.Register(new VirtualGenericNodeCopyObjectKind());

            registry.RegisterNormalizer(typeof(VirtualState), obj => new VirtualStateMachine.VirtualChildState { State = (VirtualState)obj });
            registry.RegisterNormalizer(typeof(VirtualStateMachine), obj => new VirtualStateMachine.VirtualChildStateMachine { StateMachine = (VirtualStateMachine)obj });

            return registry;
        }
    }
}
