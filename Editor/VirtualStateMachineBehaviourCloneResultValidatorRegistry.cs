using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor
{
    internal sealed class VirtualStateMachineBehaviourCloneResultValidatorRegistry
    {
        internal static VirtualStateMachineBehaviourCloneResultValidatorRegistry Shared { get; } = new();

        private readonly Dictionary<Type, IVirtualStateMachineBehaviourCloneResultValidator> _validators = new();

        internal void Register(IVirtualStateMachineBehaviourCloneResultValidator validator)
        {
            if (validator == null) throw new ArgumentNullException(nameof(validator));
            _validators[validator.BehaviourType] = validator;
        }

        internal void Unregister(Type behaviourType) => _validators.Remove(behaviourType);

        internal IVirtualStateMachineBehaviourCloneResultValidator Resolve(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (_validators.TryGetValue(current, out IVirtualStateMachineBehaviourCloneResultValidator validator))
                {
                    return validator;
                }
            }

            return null;
        }
    }
}
