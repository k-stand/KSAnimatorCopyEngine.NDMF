using UnityEditor;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor.CrossController;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.vrchatavatars.editor
{
    [InitializeOnLoad]
    internal static class VRChatVirtualResolverRegistrar
    {
        static VRChatVirtualResolverRegistrar()
        {
            VirtualParameterReferenceResolverRegistry.Shared.Register(new VRCAvatarParameterDriverVirtualParameterReferenceResolver());
        }
    }
}
