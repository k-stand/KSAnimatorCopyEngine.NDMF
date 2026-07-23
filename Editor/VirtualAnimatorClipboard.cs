using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEngine;
using nadena.dev.ndmf.animator;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor
{
    /// <summary>
    /// Virtual Animator関連オブジェクトのコピー・貼り付け機能を提供する、このパッケージの主な入口となる静的クラスです。
    /// 各操作にはTry接頭辞を持つ失敗許容版と、失敗時に例外を送出する版が対になって用意されています。
    /// </summary>
    public static partial class VirtualAnimatorClipboard
    {
        /// <summary>
        /// 単一のVirtualLayerをコピーします。
        /// </summary>
        /// <param name="layer">コピー対象のレイヤー。</param>
        /// <param name="parentController">layerが属している親VirtualAnimatorController。</param>
        /// <param name="result">成功した場合はコピー結果のVirtualAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(VirtualLayer layer, VirtualAnimatorController parentController, out VirtualAnimatorCopyClipSet result)
            => TryCopy(new[] { layer }, parentController, out result);

        /// <summary>
        /// 単一のVirtualLayerをコピーします。
        /// </summary>
        /// <param name="layer">コピー対象のレイヤー。</param>
        /// <param name="parentController">layerが属している親VirtualAnimatorController。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static VirtualAnimatorCopyClipSet Copy(VirtualLayer layer, VirtualAnimatorController parentController)
        {
            if (!TryCopy(layer, parentController, out VirtualAnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// 複数のVirtualLayerをまとめてコピーします。
        /// </summary>
        /// <param name="layers">コピー対象のレイヤーの列挙。</param>
        /// <param name="parentController">layersが属している親VirtualAnimatorController。</param>
        /// <param name="result">成功した場合はコピー結果のVirtualAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(IEnumerable<VirtualLayer> layers, VirtualAnimatorController parentController, out VirtualAnimatorCopyClipSet result)
        {
            VirtualAnimatorCopyClipSet clipSet = new(layers, parentController);
            if (clipSet.Type != VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers)
            {
                result = null;
                return false;
            }
            result = clipSet;
            return true;
        }

        /// <summary>
        /// 複数のVirtualLayerをまとめてコピーします。
        /// </summary>
        /// <param name="layers">コピー対象のレイヤーの列挙。</param>
        /// <param name="parentController">layersが属している親VirtualAnimatorController。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static VirtualAnimatorCopyClipSet Copy(IEnumerable<VirtualLayer> layers, VirtualAnimatorController parentController)
        {
            if (!TryCopy(layers, parentController, out VirtualAnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// VirtualStateMachine配下の単一オブジェクト(State/StateMachine/Transition等)を、その親レイヤーを祖先としてコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <param name="parentLayer">objが属している親VirtualLayer。</param>
        /// <param name="result">成功した場合はコピー結果のVirtualAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(object obj, VirtualLayer parentLayer, out VirtualAnimatorCopyClipSet result)
            => TryCopy(new[] { obj }, parentLayer.StateMachine, out result);

        /// <summary>
        /// VirtualStateMachine配下の単一オブジェクト(State/StateMachine/Transition等)を、その親レイヤーを祖先としてコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <param name="parentLayer">objが属している親VirtualLayer。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static VirtualAnimatorCopyClipSet Copy(object obj, VirtualLayer parentLayer)
        {
            if (!TryCopy(obj, parentLayer, out VirtualAnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// VirtualStateMachine配下の複数オブジェクトを、その親レイヤーを祖先としてまとめてコピーします。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <param name="parentLayer">objsが属している親VirtualLayer。</param>
        /// <param name="result">成功した場合はコピー結果のVirtualAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(IEnumerable<object> objs, VirtualLayer parentLayer, out VirtualAnimatorCopyClipSet result)
            => TryCopy(objs, parentLayer.StateMachine, out result);

        /// <summary>
        /// VirtualStateMachine配下の複数オブジェクトを、その親レイヤーを祖先としてまとめてコピーします。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <param name="parentLayer">objsが属している親VirtualLayer。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static VirtualAnimatorCopyClipSet Copy(IEnumerable<object> objs, VirtualLayer parentLayer)
        {
            if (!TryCopy(objs, parentLayer, out VirtualAnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// VirtualStateMachine配下の単一オブジェクト(State/StateMachine/Transition等)を、指定した祖先VirtualStateMachineを基準にコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <param name="ancestorStateMachine">objの祖先となるVirtualStateMachine。</param>
        /// <param name="result">成功した場合はコピー結果のVirtualAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(object obj, VirtualStateMachine ancestorStateMachine, out VirtualAnimatorCopyClipSet result)
            => TryCopy(new[] { obj }, ancestorStateMachine, out result);

        /// <summary>
        /// VirtualStateMachine配下の単一オブジェクト(State/StateMachine/Transition等)を、指定した祖先VirtualStateMachineを基準にコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <param name="ancestorStateMachine">objの祖先となるVirtualStateMachine。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static VirtualAnimatorCopyClipSet Copy(object obj, VirtualStateMachine ancestorStateMachine)
        {
            if (!TryCopy(obj, ancestorStateMachine, out VirtualAnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// VirtualStateMachine配下の複数オブジェクトを、指定した祖先VirtualStateMachineを基準にまとめてコピーします。
        /// 対象が全てancestorStateMachineの子孫でない場合は失敗します。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <param name="ancestorStateMachine">objsの祖先となるVirtualStateMachine。</param>
        /// <param name="result">成功した場合はコピー結果のVirtualAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(IEnumerable<object> objs, VirtualStateMachine ancestorStateMachine, out VirtualAnimatorCopyClipSet result)
        {
            VirtualAnimatorCopyClipSet clipSet = new(objs, ancestorStateMachine);
            if (!clipSet.Type.IsInStateMachineCategory())
            {
                result = null;
                return false;
            }
            result = clipSet;
            return true;
        }

        /// <summary>
        /// VirtualStateMachine配下の複数オブジェクトを、指定した祖先VirtualStateMachineを基準にまとめてコピーします。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <param name="ancestorStateMachine">objsの祖先となるVirtualStateMachine。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static VirtualAnimatorCopyClipSet Copy(IEnumerable<object> objs, VirtualStateMachine ancestorStateMachine)
        {
            if (!TryCopy(objs, ancestorStateMachine, out VirtualAnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// 単一のStateMachineBehaviourをコピーします。
        /// </summary>
        /// <param name="behaviour">コピー対象のStateMachineBehaviour。</param>
        /// <param name="result">成功した場合はコピー結果のVirtualAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(StateMachineBehaviour behaviour, out VirtualAnimatorCopyClipSet result)
            => TryCopy(new[] { behaviour }, out result);

        /// <summary>
        /// 単一のStateMachineBehaviourをコピーします。
        /// </summary>
        /// <param name="behaviour">コピー対象のStateMachineBehaviour。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static VirtualAnimatorCopyClipSet Copy(StateMachineBehaviour behaviour)
        {
            if (!TryCopy(behaviour, out VirtualAnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// 複数のStateMachineBehaviourをまとめてコピーします。
        /// </summary>
        /// <param name="behaviours">コピー対象のStateMachineBehaviourの列挙。</param>
        /// <param name="result">成功した場合はコピー結果のVirtualAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(IEnumerable<StateMachineBehaviour> behaviours, out VirtualAnimatorCopyClipSet result)
        {
            VirtualAnimatorCopyClipSet clipSet = new(behaviours);
            if (clipSet.Type != VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours)
            {
                result = null;
                return false;
            }
            result = clipSet;
            return true;
        }

        /// <summary>
        /// 複数のStateMachineBehaviourをまとめてコピーします。
        /// </summary>
        /// <param name="behaviours">コピー対象のStateMachineBehaviourの列挙。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static VirtualAnimatorCopyClipSet Copy(IEnumerable<StateMachineBehaviour> behaviours)
        {
            if (!TryCopy(behaviours, out VirtualAnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// 祖先や親コンテキストの妥当性検証を行わずに、単一のオブジェクトをコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        public static VirtualAnimatorCopyClipSet Copy(object obj) => new(obj);

        /// <summary>
        /// 祖先や親コンテキストの妥当性検証を行わずに、複数のオブジェクトをまとめてコピーします。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <returns>コピー結果のVirtualAnimatorCopyClipSet。</returns>
        public static VirtualAnimatorCopyClipSet Copy(IEnumerable<object> objs) => new(objs);

        /// <summary>
        /// clipSetの内容(Layers種別)を、destAnimatorControllerへ新しいレイヤーとして貼り付けます。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。Layers種別である必要があります。</param>
        /// <param name="destAnimatorController">貼り付け先のVirtualAnimatorController。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <param name="result">成功した場合は貼り付けられたレイヤーの配列、失敗した場合はnull。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteLayers(
            VirtualAnimatorCopyClipSet clipSet,
            VirtualAnimatorController destAnimatorController,
            CloneContext context,
            out VirtualLayer[] result)
        {
            result = null;
            if (clipSet.Type != VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers)
            {
                return false;
            }

            VirtualAnimatorCloner cloner = new(context);
            foreach (VirtualAnimatorCopyClip clip in clipSet.Clips)
            {
                cloner.SetRangeClonePolicy(GetCloneScope(clip), VirtualAnimatorCloner.ClonePolicy.Clone);
            }

            foreach (VirtualLayer layer in destAnimatorController.Layers)
            {
                cloner.SetRangeClonePolicyIfAbsent(VirtualAnimatorGraphTraversal.ListupObjectsInLayer(layer), VirtualAnimatorCloner.ClonePolicy.KeepReference);
            }

            VirtualLayer[] cloneLayers = cloner.CloneVirtualLayers(clipSet.Clips.Select(x => (VirtualLayer)x.Object)).ToArray();

            // ICrossControllerPostProcessor相当は不要。VirtualLayer.SyncedLayerIndexの変換は
            // CloneContext.CloneSourceToVirtualLayerIndexが自動対応する(対応表§11参照)。

            foreach (VirtualLayer cloneLayer in cloneLayers)
            {
                destAnimatorController.AddLayer(LayerPriority.Default, cloneLayer);
            }

            // AnimatorAssetPersistence相当(実アセット保存)は不要。NDMFのAssetSaver/CommitContextが
            // 到達可能性ベースで自動保存する(対応表§7参照)。

            result = cloneLayers;
            return true;
        }

        /// <summary>
        /// clipSetの内容(Layers種別)を、destAnimatorControllerへ新しいレイヤーとして貼り付けます。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。Layers種別である必要があります。</param>
        /// <param name="destAnimatorController">貼り付け先のVirtualAnimatorController。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <returns>貼り付けられたレイヤーの配列。</returns>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetがLayers種別でない場合。</exception>
        public static VirtualLayer[] PasteLayers(
            VirtualAnimatorCopyClipSet clipSet,
            VirtualAnimatorController destAnimatorController,
            CloneContext context)
        {
            if (!TryPasteLayers(clipSet, destAnimatorController, context, out VirtualLayer[] result))
            {
                ThrowInvalidClipSetTypeException(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Layers, clipSet.Type);
            }
            return result;
        }

        /// <summary>
        /// clipSetの内容(VirtualStateMachine配下のオブジェクト)を、destLayer直下のVirtualStateMachineへ貼り付けます。
        /// 実体はTryPasteIntoStateMachine(destLayer.StateMachine)への委譲です。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。</param>
        /// <param name="destLayer">貼り付け先のVirtualLayer。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <param name="result">成功した場合は貼り付けられたオブジェクトの配列、失敗した場合はnull。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteIntoLayer(
            VirtualAnimatorCopyClipSet clipSet, VirtualLayer destLayer, CloneContext context,
            out object[] result)
            => TryPasteIntoStateMachine(clipSet, destLayer.StateMachine, context, out result);

        /// <summary>
        /// clipSetの内容(VirtualStateMachine配下のオブジェクト)を、destLayer直下のVirtualStateMachineへ貼り付けます。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。</param>
        /// <param name="destLayer">貼り付け先のVirtualLayer。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <returns>貼り付けられたオブジェクトの配列。</returns>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetがVirtualStateMachine配下のオブジェクトを表す種別でない場合。</exception>
        public static object[] PasteIntoLayer(
            VirtualAnimatorCopyClipSet clipSet, VirtualLayer destLayer, CloneContext context)
            => PasteIntoStateMachine(clipSet, destLayer.StateMachine, context);

        /// <summary>
        /// clipSetの内容(VirtualStateMachine配下のオブジェクト)を、destStateMachineへ貼り付けます。
        /// 貼り付け先がコピー元の祖先の子孫である場合はコピー元との参照を保持し、そうでない場合は貼り付け先の子孫のみ参照を保持します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。VirtualStateMachine配下のオブジェクトを表す種別である必要があります。</param>
        /// <param name="destStateMachine">貼り付け先のVirtualStateMachine。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <param name="result">成功した場合は貼り付けられた全オブジェクトの配列、失敗した場合はnull。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteIntoStateMachine(
            VirtualAnimatorCopyClipSet clipSet, VirtualStateMachine destStateMachine, CloneContext context,
            out object[] result)
        {
            result = null;
            if (!clipSet.Type.IsInStateMachineCategory())
            {
                return false;
            }

            HashSet<object> inScopeObjs = new(VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(clipSet.AncestorStateMachine));
            inScopeObjs.Add(clipSet.AncestorStateMachine);

            VirtualAnimatorCloner cloner = new(context);
            foreach (VirtualAnimatorCopyClip clip in clipSet.Clips)
            {
                cloner.SetRangeClonePolicy(GetCloneScope(clip), VirtualAnimatorCloner.ClonePolicy.Clone);
            }

            // 貼り付け先がコピー元の祖先自身、もしくはその子孫であるかを確認
            if (inScopeObjs.Contains(destStateMachine))
            {
                // 同レイヤー間でのコピペのはずなので、コピー元との参照を保持できる
                cloner.SetRangeClonePolicyIfAbsent(inScopeObjs, VirtualAnimatorCloner.ClonePolicy.KeepReference);
            }
            else
            {
                // 同レイヤー間のコピペである保証が無いので貼り付け先及びその子孫のみを参照を保持する
                cloner.SetClonePolicyIfAbsent(destStateMachine, VirtualAnimatorCloner.ClonePolicy.KeepReference);
                cloner.SetRangeClonePolicyIfAbsent(new HashSet<object>(VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(destStateMachine)), VirtualAnimatorCloner.ClonePolicy.KeepReference);
            }

            // クリップとそのデータのクローン
            List<VirtualAnimatorCopyClip> cloneChildStateClips = new();
            List<VirtualAnimatorCopyClip> cloneChildStateMachineClips = new();
            List<VirtualAnimatorCopyClip> cloneTransitionClips = new();
            List<VirtualAnimatorCopyClip> cloneStateTransitionClips = new();
            foreach (VirtualAnimatorCopyClip clip in clipSet.Clips)
            {
                VirtualAnimatorCopyClip cloneClip = clip.Clone(cloner);
                if (clip.Type == typeof(VirtualStateMachine.VirtualChildState)) cloneChildStateClips.Add(cloneClip);
                else if (clip.Type == typeof(VirtualStateMachine.VirtualChildStateMachine)) cloneChildStateMachineClips.Add(cloneClip);
                else if (clip.Type == typeof(VirtualTransition)) cloneTransitionClips.Add(cloneClip);
                else if (clip.Type == typeof(VirtualStateTransition)) cloneStateTransitionClips.Add(cloneClip);
            }

            // ペースト処理
            List<object> pastedObjs = new();

            List<VirtualStateMachine.VirtualChildStateMachine> cloneChildStateMachines = cloneChildStateMachineClips.Select(x => (VirtualStateMachine.VirtualChildStateMachine)x.Object).ToList();
            destStateMachine.StateMachines = destStateMachine.StateMachines.AddRange(cloneChildStateMachines);
            pastedObjs.AddRange(cloneChildStateMachines.Where(x => x.StateMachine != null).Select(x => (object)x.StateMachine));

            List<VirtualStateMachine.VirtualChildState> cloneChildStates = cloneChildStateClips.Select(x => (VirtualStateMachine.VirtualChildState)x.Object).ToList();
            destStateMachine.States = destStateMachine.States.AddRange(cloneChildStates);
            pastedObjs.AddRange(cloneChildStates.Where(x => x.State != null).Select(x => (object)x.State));

            foreach (VirtualAnimatorCopyClip cloneClip in cloneTransitionClips)
            {
                VirtualTransition cloneT = (VirtualTransition)cloneClip.Object;
                if (cloneT.DestinationState == null && cloneT.DestinationStateMachine == null && !cloneT.IsExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.PropertyName, out object objPropName))
                {
                    VirtualAnimatorCopyClip.ContextValue.PropertyName propName = (VirtualAnimatorCopyClip.ContextValue.PropertyName)objPropName;

                    if (propName == VirtualAnimatorCopyClip.ContextValue.PropertyName.m_StateMachineTransitions &&
                        cloneClip.TryGetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.Parent, out object parent) &&
                        destStateMachine.StateMachines.Select(x => (object)x.StateMachine).Contains(parent))
                    {
                        // 元がm_StateMachineTransitionsに登録されていたものなら同様に設定する
                        VirtualStateMachine parentSM = (VirtualStateMachine)parent;
                        ImmutableList<VirtualTransition> smTranss = destStateMachine.StateMachineTransitions.TryGetValue(parentSM, out ImmutableList<VirtualTransition> existing) ? existing : ImmutableList<VirtualTransition>.Empty;
                        destStateMachine.StateMachineTransitions = destStateMachine.StateMachineTransitions.SetItem(parentSM, smTranss.Add(cloneT));

                        pastedObjs.Add(cloneT);
                    }
                    else if (propName == VirtualAnimatorCopyClip.ContextValue.PropertyName.m_EntryTransitions)
                    {
                        // 元がEntryTransitionなら同様に登録する
                        destStateMachine.EntryTransitions = destStateMachine.EntryTransitions.Add(cloneT);

                        pastedObjs.Add(cloneT);
                        continue;
                    }
                }
            }

            foreach (VirtualAnimatorCopyClip cloneClip in cloneStateTransitionClips)
            {
                VirtualStateTransition cloneST = (VirtualStateTransition)cloneClip.Object;
                if (cloneST.DestinationState == null && cloneST.DestinationStateMachine == null && !cloneST.IsExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.Parent, out object parent) && parent != null)
                {
                    if (parent is VirtualState parentState)
                    {
                        // 親がStateなら通常のTransitionと解釈
                        if (!parentState.Transitions.Contains(cloneST))
                        {
                            parentState.Transitions = parentState.Transitions.Add(cloneST);

                            pastedObjs.Add(cloneST);
                        }
                    }
                    else if (parent is VirtualStateMachine)
                    {
                        // 親がStateMachineならAnyStateTransitionsと解釈
                        if (!destStateMachine.AnyStateTransitions.Contains(cloneST))
                        {
                            destStateMachine.AnyStateTransitions = destStateMachine.AnyStateTransitions.Add(cloneST);

                            pastedObjs.Add(cloneST);
                        }
                    }
                }
                else if (cloneClip.TryGetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.PropertyName, out object propName) &&
                    (VirtualAnimatorCopyClip.ContextValue.PropertyName)propName == VirtualAnimatorCopyClip.ContextValue.PropertyName.m_AnyStateTransitions)
                {
                    // 親が取得できない(親のClonePolicyがDetachの場合)かつ、
                    // 元のプロパティがAnyStateTransitionだった場合
                    destStateMachine.AnyStateTransitions = destStateMachine.AnyStateTransitions.Add(cloneST);

                    pastedObjs.Add(cloneST);
                }
            }

            // Virtual API版ではAnimatorAssetPersistence相当(実アセット保存)は不要。NDMFのAssetSaver/
            // CommitContextが到達可能性ベースで自動保存するため、resultは生API版の「実際にアセットへ
            // 追加されたオブジェクト」ではなく「貼り付けられた全オブジェクト」を意味する
            // (2026-07-19-raw-to-virtual-api-mapping.md §7参照)。

            result = pastedObjs.ToArray();
            return true;
        }

        /// <summary>
        /// clipSetの内容(VirtualStateMachine配下のオブジェクト)を、destStateMachineへ貼り付けます。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。VirtualStateMachine配下のオブジェクトを表す種別である必要があります。</param>
        /// <param name="destStateMachine">貼り付け先のVirtualStateMachine。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <returns>貼り付けられた全オブジェクトの配列。</returns>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetがVirtualStateMachine配下のオブジェクトを表す種別でない場合。</exception>
        public static object[] PasteIntoStateMachine(
            VirtualAnimatorCopyClipSet clipSet, VirtualStateMachine destStateMachine, CloneContext context)
        {
            if (!TryPasteIntoStateMachine(clipSet, destStateMachine, context, out object[] result))
            {
                ThrowInvalidClipSetTypeException(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.InStateMachineObjects, clipSet.Type);
            }
            return result;
        }

        /// <summary>
        /// clipSetの内容(Behaviours種別)をクローンし、destStateMachineのBehavioursへ追加します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。Behaviours種別である必要があります。</param>
        /// <param name="destStateMachine">貼り付け先のVirtualStateMachine。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <param name="result">成功した場合は貼り付けられたStateMachineBehaviourの配列、失敗した場合はnull。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteBehaviours(VirtualAnimatorCopyClipSet clipSet, VirtualStateMachine destStateMachine, CloneContext context, out StateMachineBehaviour[] result)
        {
            if (!TryCloneBehaviours(clipSet, context, out result)) return false;
            destStateMachine.Behaviours = destStateMachine.Behaviours.AddRange(result);
            return true;
        }

        /// <summary>
        /// clipSetの内容(Behaviours種別)をクローンし、destStateMachineのBehavioursへ追加します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。Behaviours種別である必要があります。</param>
        /// <param name="destStateMachine">貼り付け先のVirtualStateMachine。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <returns>貼り付けられたStateMachineBehaviourの配列。</returns>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetがBehaviours種別でない場合。</exception>
        public static StateMachineBehaviour[] PasteBehaviours(VirtualAnimatorCopyClipSet clipSet, VirtualStateMachine destStateMachine, CloneContext context)
        {
            if (!TryPasteBehaviours(clipSet, destStateMachine, context, out StateMachineBehaviour[] result))
            {
                ThrowInvalidClipSetTypeException(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours, clipSet.Type);
            }
            return result;
        }

        /// <summary>
        /// clipSetの内容(Behaviours種別)をクローンし、destStateのBehavioursへ追加します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。Behaviours種別である必要があります。</param>
        /// <param name="destState">貼り付け先のVirtualState。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <param name="result">成功した場合は貼り付けられたStateMachineBehaviourの配列、失敗した場合はnull。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteBehaviours(VirtualAnimatorCopyClipSet clipSet, VirtualState destState, CloneContext context, out StateMachineBehaviour[] result)
        {
            if (!TryCloneBehaviours(clipSet, context, out result)) return false;
            destState.Behaviours = destState.Behaviours.AddRange(result);
            return true;
        }

        /// <summary>
        /// clipSetの内容(Behaviours種別)をクローンし、destStateのBehavioursへ追加します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のVirtualAnimatorCopyClipSet。Behaviours種別である必要があります。</param>
        /// <param name="destState">貼り付け先のVirtualState。</param>
        /// <param name="context">クローン処理に使用するCloneContext。</param>
        /// <returns>貼り付けられたStateMachineBehaviourの配列。</returns>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetがBehaviours種別でない場合。</exception>
        public static StateMachineBehaviour[] PasteBehaviours(VirtualAnimatorCopyClipSet clipSet, VirtualState destState, CloneContext context)
        {
            if (!TryPasteBehaviours(clipSet, destState, context, out StateMachineBehaviour[] result))
            {
                ThrowInvalidClipSetTypeException(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours, clipSet.Type);
            }
            return result;
        }

        private static bool TryCloneBehaviours(VirtualAnimatorCopyClipSet clipSet, CloneContext context, out StateMachineBehaviour[] result)
        {
            result = null;
            if (clipSet.Type != VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType.Behaviours)
            {
                return false;
            }

            VirtualAnimatorCloner cloner = new(context);
            foreach (VirtualAnimatorCopyClip clip in clipSet.Clips)
            {
                cloner.SetRangeClonePolicy(GetCloneScope(clip), VirtualAnimatorCloner.ClonePolicy.Clone);
            }

            List<StateMachineBehaviour> cloneBehaviours = new();
            foreach (VirtualAnimatorCopyClip clip in clipSet.Clips)
            {
                if (clip.Object != null)
                {
                    StateMachineBehaviour clone = cloner.CloneStateMachineBehaviour((StateMachineBehaviour)clip.Object);
                    cloneBehaviours.Add(clone);
                }
            }

            result = cloneBehaviours.ToArray();
            return true;
        }

        /// <summary>
        /// clipSet(単一のVirtualChildStateを表すもの)のVirtualStateとしての設定値を、destStateへ上書きコピーします。
        /// Name/Behaviours/Transitionsはdest側の値が維持されます。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualStateMachine.VirtualChildStateを表す種別である必要があります。</param>
        /// <param name="destState">コピー先のVirtualState。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettings(VirtualAnimatorCopyClipSet clipSet, VirtualState destState)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out VirtualStateMachine.VirtualChildState srcChildState)) return false;
            PasteSettings(srcChildState, destState);
            return true;
        }

        /// <summary>
        /// clipSet(単一のVirtualChildStateを表すもの)のVirtualStateとしての設定値を、destStateへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualStateMachine.VirtualChildStateを表す種別である必要があります。</param>
        /// <param name="destState">コピー先のVirtualState。</param>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetが単一のVirtualStateMachine.VirtualChildStateを表す種別でない場合。</exception>
        public static void PasteSettings(VirtualAnimatorCopyClipSet clipSet, VirtualState destState)
        {
            if (!TryPasteSettings(clipSet, destState))
            {
                ThrowInvalidClipSetTypeException(typeof(VirtualStateMachine.VirtualChildState), clipSet.Type);
            }
        }

        private static void PasteSettings(VirtualStateMachine.VirtualChildState srcChildState, VirtualState destState) => PasteSettings(srcChildState.State, destState);

        // Name/Behaviours/Transitionsはdest側の値が維持される(生API版と同様)。EditorUtility.
        // CopySerialized相当がVirtualStateに存在しないため、対象プロパティを個別コピーする
        // (Virtual API化に伴う技術的に必須な変更)。
        private static void PasteSettings(VirtualState srcState, VirtualState destState)
        {
            destState.CycleOffset = srcState.CycleOffset;
            destState.CycleOffsetParameter = srcState.CycleOffsetParameter;
            destState.IKOnFeet = srcState.IKOnFeet;
            destState.Mirror = srcState.Mirror;
            destState.MirrorParameter = srcState.MirrorParameter;
            destState.Motion = srcState.Motion;
            destState.Speed = srcState.Speed;
            destState.SpeedParameter = srcState.SpeedParameter;
            destState.Tag = srcState.Tag;
            destState.TimeParameter = srcState.TimeParameter;
            destState.WriteDefaultValues = srcState.WriteDefaultValues;
        }

        /// <summary>
        /// clipSet(単一のVirtualTransitionを表すもの)のMute/Solo設定を、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettings(VirtualAnimatorCopyClipSet clipSet, VirtualTransition destTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out VirtualTransition srcTransition)) return false;
            PasteSettings(srcTransition, destTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のVirtualTransitionを表すもの)のMute/Solo設定を、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetが単一のVirtualTransitionを表す種別でない場合。</exception>
        public static void PasteSettings(VirtualAnimatorCopyClipSet clipSet, VirtualTransition destTransition)
        {
            if (!TryPasteSettings(clipSet, destTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(VirtualTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcTransitionのMute/Solo設定を、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcTransition">コピー元のVirtualTransition。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        // hideFlagsはVirtual API側に対応する概念がないため省略する(技術的に必須な変更)。
        public static void PasteSettings(VirtualTransition srcTransition, VirtualTransition destTransition)
        {
            destTransition.Mute = srcTransition.Mute;
            destTransition.Solo = srcTransition.Solo;
        }

        /// <summary>
        /// clipSet(単一のVirtualTransitionを表すもの)のConditionsを、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteConditions(VirtualAnimatorCopyClipSet clipSet, VirtualTransition destTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out VirtualTransition srcTransition)) return false;
            PasteConditions(srcTransition, destTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のVirtualTransitionを表すもの)のConditionsを、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetが単一のVirtualTransitionを表す種別でない場合。</exception>
        public static void PasteConditions(VirtualAnimatorCopyClipSet clipSet, VirtualTransition destTransition)
        {
            if (!TryPasteConditions(clipSet, destTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(VirtualTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcTransitionのConditionsを、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcTransition">コピー元のVirtualTransition。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        public static void PasteConditions(VirtualTransition srcTransition, VirtualTransition destTransition) => destTransition.Conditions = srcTransition.Conditions;

        /// <summary>
        /// clipSet(単一のVirtualTransitionを表すもの)の設定値とConditionsを、まとめてdestTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettingsAndConditions(VirtualAnimatorCopyClipSet clipSet, VirtualTransition destTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out VirtualTransition srcTransition)) return false;
            PasteSettingsAndConditions(srcTransition, destTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のVirtualTransitionを表すもの)の設定値とConditionsを、まとめてdestTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetが単一のVirtualTransitionを表す種別でない場合。</exception>
        public static void PasteSettingsAndConditions(VirtualAnimatorCopyClipSet clipSet, VirtualTransition destTransition)
        {
            if (!TryPasteSettingsAndConditions(clipSet, destTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(VirtualTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcTransitionの設定値とConditionsを、まとめてdestTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcTransition">コピー元のVirtualTransition。</param>
        /// <param name="destTransition">コピー先のVirtualTransition。</param>
        public static void PasteSettingsAndConditions(VirtualTransition srcTransition, VirtualTransition destTransition)
        {
            PasteSettings(srcTransition, destTransition);
            PasteConditions(srcTransition, destTransition);
        }

        /// <summary>
        /// clipSet(単一のVirtualStateTransitionを表すもの)の設定値を、destStateTransitionへ上書きコピーします。
        /// Conditions/DestinationState/DestinationStateMachine/IsExit/Nameはdest側の値が維持されます。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettings(VirtualAnimatorCopyClipSet clipSet, VirtualStateTransition destStateTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out VirtualStateTransition srcStateTransition)) return false;
            PasteSettings(srcStateTransition, destStateTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のVirtualStateTransitionを表すもの)の設定値を、destStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetが単一のVirtualStateTransitionを表す種別でない場合。</exception>
        public static void PasteSettings(VirtualAnimatorCopyClipSet clipSet, VirtualStateTransition destStateTransition)
        {
            if (!TryPasteSettings(clipSet, destStateTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(VirtualStateTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcStateTransitionの設定値を、destStateTransitionへ上書きコピーします。
        /// Conditions/DestinationState/DestinationStateMachine/IsExit/Nameはdest側の値が維持されます。
        /// </summary>
        /// <param name="srcStateTransition">コピー元のVirtualStateTransition。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        public static void PasteSettings(VirtualStateTransition srcStateTransition, VirtualStateTransition destStateTransition)
        {
            destStateTransition.CanTransitionToSelf = srcStateTransition.CanTransitionToSelf;
            destStateTransition.Duration = srcStateTransition.Duration;
            destStateTransition.ExitTime = srcStateTransition.ExitTime;
            destStateTransition.HasFixedDuration = srcStateTransition.HasFixedDuration;
            destStateTransition.InterruptionSource = srcStateTransition.InterruptionSource;
            destStateTransition.Mute = srcStateTransition.Mute;
            destStateTransition.Offset = srcStateTransition.Offset;
            destStateTransition.OrderedInterruption = srcStateTransition.OrderedInterruption;
            destStateTransition.Solo = srcStateTransition.Solo;
        }

        /// <summary>
        /// clipSet(単一のVirtualStateTransitionを表すもの)のConditionsを、destStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteConditions(VirtualAnimatorCopyClipSet clipSet, VirtualStateTransition destStateTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out VirtualStateTransition srcStateTransition)) return false;
            PasteConditions(srcStateTransition, destStateTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のVirtualStateTransitionを表すもの)のConditionsを、destStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetが単一のVirtualStateTransitionを表す種別でない場合。</exception>
        public static void PasteConditions(VirtualAnimatorCopyClipSet clipSet, VirtualStateTransition destStateTransition)
        {
            if (!TryPasteConditions(clipSet, destStateTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(VirtualStateTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcStateTransitionのConditionsを、destStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcStateTransition">コピー元のVirtualStateTransition。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        public static void PasteConditions(VirtualStateTransition srcStateTransition, VirtualStateTransition destStateTransition) => destStateTransition.Conditions = srcStateTransition.Conditions;

        /// <summary>
        /// clipSet(単一のVirtualStateTransitionを表すもの)の設定値とConditionsを、まとめてdestStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettingsAndConditions(VirtualAnimatorCopyClipSet clipSet, VirtualStateTransition destStateTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out VirtualStateTransition srcStateTransition)) return false;
            PasteSettingsAndConditions(srcStateTransition, destStateTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のVirtualStateTransitionを表すもの)の設定値とConditionsを、まとめてdestStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のVirtualAnimatorCopyClipSet。単一のVirtualStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        /// <exception cref="VirtualAnimatorCopyClipSetTypeMismatchException">clipSetが単一のVirtualStateTransitionを表す種別でない場合。</exception>
        public static void PasteSettingsAndConditions(VirtualAnimatorCopyClipSet clipSet, VirtualStateTransition destStateTransition)
        {
            if (!TryPasteSettingsAndConditions(clipSet, destStateTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(VirtualStateTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcStateTransitionの設定値とConditionsを、まとめてdestStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcStateTransition">コピー元のVirtualStateTransition。</param>
        /// <param name="destStateTransition">コピー先のVirtualStateTransition。</param>
        public static void PasteSettingsAndConditions(VirtualStateTransition srcStateTransition, VirtualStateTransition destStateTransition)
        {
            PasteSettings(srcStateTransition, destStateTransition);
            PasteConditions(srcStateTransition, destStateTransition);
        }

        private static bool TryValidateAndGetSingleClipObjectType<T>(VirtualAnimatorCopyClipSet clipSet, out T result)
        {
            IVirtualAnimatorCopyObjectKind kind = VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(T));
            if (kind != null && clipSet.Type != kind.SingleClipSetType)
            {
                result = default;
                return false;
            }

            result = (T)clipSet.Clips.First().Object;
            return true;
        }

        private static IEnumerable<object> GetCloneScope(VirtualAnimatorCopyClip clip) =>
            VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(clip.Type)?.GetCloneScope(clip.Object) ?? Array.Empty<object>();

        private static void ThrowInvalidClipSetTypeException(Type requestType, VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType clipSetType) => throw new VirtualAnimatorCopyClipSetTypeMismatchException($"要求された型({requestType.FullName})に対して、ClipSetのデータのタイプ({nameof(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType)}.{clipSetType})が一致しません");

        private static void ThrowInvalidClipSetTypeException(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType requestClipSetType, VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType clipSetType) => throw new VirtualAnimatorCopyClipSetTypeMismatchException($"要求されたClipSetのデータのタイプ({nameof(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType)}.{requestClipSetType})に対して、ClipSetのデータのタイプ({nameof(VirtualAnimatorCopyClipSet.VirtualAnimatorCopyClipSetType)}.{clipSetType})が一致しません");
    }
}
