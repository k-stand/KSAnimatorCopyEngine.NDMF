using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.CrossController
{
    /// <summary>
    /// IVirtualParameterReferenceResolverの登録・解決を行うレジストリです。
    /// 本パッケージ内部でのみ利用され、外部パッケージからは拡張できません。
    /// </summary>
    internal sealed class VirtualParameterReferenceResolverRegistry
    {
        /// <summary>
        /// プロセス全体で共有されるデフォルトインスタンスを取得します。本パッケージ内のVRChatAvatars対応モジュール(Editor/VRChatAvatars)が、このインスタンスにResolverを登録します。
        /// </summary>
        public static VirtualParameterReferenceResolverRegistry Shared { get; } = CreateDefault();

        private readonly Dictionary<Type, IVirtualParameterReferenceResolver> _resolvers = new();

        /// <summary>
        /// IVirtualParameterReferenceResolverを登録します。同じBehaviourTypeが既に登録済みの場合は上書きされます。
        /// </summary>
        /// <param name="resolver">登録するresolver。</param>
        /// <exception cref="ArgumentNullException">resolverがnullの場合。</exception>
        public void Register(IVirtualParameterReferenceResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _resolvers[resolver.BehaviourType] = resolver;
        }

        /// <summary>
        /// 指定した型に対応するIVirtualParameterReferenceResolverの登録を解除します。
        /// </summary>
        /// <param name="behaviourType">登録を解除するBehaviourType。</param>
        public void Unregister(Type behaviourType) => _resolvers.Remove(behaviourType);

        internal IVirtualParameterReferenceResolver Resolve(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (_resolvers.TryGetValue(current, out IVirtualParameterReferenceResolver resolver))
                {
                    return resolver;
                }
            }

            return null;
        }

        private static VirtualParameterReferenceResolverRegistry CreateDefault() => new();
    }
}
