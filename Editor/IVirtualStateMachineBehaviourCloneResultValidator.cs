using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor
{
    internal interface IVirtualStateMachineBehaviourCloneResultValidator
    {
        Type BehaviourType { get; }

        IEnumerable<(string MemberName, object Child)> GetChildren(StateMachineBehaviour behaviour);
    }
}
