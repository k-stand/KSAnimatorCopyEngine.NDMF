using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor.CrossController
{
    /// <summary>
    /// StateMachineBehaviourの具象型ごとに、そのBehaviourが参照しているAnimatorControllerParameter名を取得するための拡張ポイントです。
    /// 標準では解決できないサードパーティ製Behaviour(VRChatのVRCAvatarParameterDriver等)のパラメーター参照を、
    /// 本パッケージ内のVRChatAvatars対応モジュール(Editor/VRChatAvatars)がVirtualParameterReferenceResolverRegistry経由で登録します。
    /// 本パッケージ内部限定の拡張機構であり、外部パッケージからは登録できません。
    /// </summary>
    internal interface IVirtualParameterReferenceResolver
    {
        /// <summary>
        /// このresolverが解決対象とするStateMachineBehaviourの型を取得します。
        /// </summary>
        Type BehaviourType { get; }

        /// <summary>
        /// 指定されたBehaviourが参照しているAnimatorControllerParameter名を列挙します。
        /// </summary>
        /// <param name="behaviour">解決対象のStateMachineBehaviour。BehaviourTypeと同じ型のインスタンスが渡されます。</param>
        /// <returns>参照されているパラメーター名の列挙。</returns>
        IEnumerable<string> GetReferencedParameterNames(StateMachineBehaviour behaviour);
    }
}
