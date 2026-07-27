using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using com.github.k_stand.ksanimatorcopyengine.ndmf.editor.CrossController;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.vrchatavatars.editor
{
    internal sealed class VRCAvatarParameterDriverVirtualParameterReferenceResolver : IVirtualParameterReferenceResolver
    {
        public Type BehaviourType => typeof(VRCAvatarParameterDriver);

        public IEnumerable<string> GetReferencedParameterNames(StateMachineBehaviour behaviour)
        {
            VRCAvatarParameterDriver driver = (VRCAvatarParameterDriver)behaviour;
            foreach (VRC_AvatarParameterDriver.Parameter parameter in driver.parameters)
            {
                if (!string.IsNullOrEmpty(parameter.name))
                {
                    yield return parameter.name;
                }

                // sourceはCopyモードの場合のみ参照される(Set/Random等では未使用のため対象外)
                if (parameter.type == VRC_AvatarParameterDriver.ChangeType.Copy && !string.IsNullOrEmpty(parameter.source))
                {
                    yield return parameter.source;
                }
            }
        }
    }
}
