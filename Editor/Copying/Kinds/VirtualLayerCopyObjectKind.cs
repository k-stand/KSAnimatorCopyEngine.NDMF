using System;
using System.Collections.Generic;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.Copying
{
    internal sealed class VirtualLayerCopyObjectKind : IVirtualAnimatorCopyObjectKind
    {
        public Type ObjectType => typeof(VirtualLayer);

        public VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType SingleClipSetType => VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers;

        public bool IsInStateMachineObject => false;

        public IEnumerable<object> GetCloneScope(object wrappedObject)
        {
            VirtualLayer layer = (VirtualLayer)wrappedObject;
            if (layer.StateMachine == null) return Array.Empty<object>();

            return VirtualAnimatorGraphTraversal.ListupObjectsInLayer(layer);
        }
    }
}
