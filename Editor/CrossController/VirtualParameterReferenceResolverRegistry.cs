using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor.CrossController
{
    /// <summary>
    /// IVirtualParameterReferenceResolverの登録・解決を行うレジストリです。
    /// </summary>
    public sealed class VirtualParameterReferenceResolverRegistry
    {
        /// <summary>
        /// プロセス全体で共有されるデフォルトインスタンスを取得します。外部パッケージはこのインスタンスにResolverを登録します。
        /// </summary>
        public static VirtualParameterReferenceResolverRegistry Shared { get; } = CreateDefault();

        private readonly Dictionary<Type, IVirtualParameterReferenceResolver> _resolvers = new();

        /// <summary>
        /// IVirtualParameterReferenceResolverを登録します。同じBehaviourTypeが既に登録済みの場合は上書きされます。
        /// </summary>
        public void Register(IVirtualParameterReferenceResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _resolvers[resolver.BehaviourType] = resolver;
        }

        /// <summary>
        /// 指定した型に対応するIVirtualParameterReferenceResolverの登録を解除します。
        /// </summary>
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
