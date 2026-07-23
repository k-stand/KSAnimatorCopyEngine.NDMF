using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using nadena.dev.ndmf.animator;
using com.github.k_stand.ksanimatorclipboard.ndmf.editor.Copying;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor
{
    /// <summary>
    /// VirtualAnimatorClipboard.Copy系メソッドの戻り値として、コピーされたVirtual Animator関連オブジェクトの集合を保持します。
    /// </summary>
    public sealed partial class VirtualAnimatorCopyClipSet
    {
        /// <summary>
        /// コピーされた個々のオブジェクトを表すクリップの一覧を取得します。
        /// </summary>
        public ReadOnlyCollection<VirtualAnimatorCopyClip> Clips { get; private set; }

        private VirtualAnimatorCopyClipSetType type = VirtualAnimatorCopyClipSetType.None;

        /// <summary>
        /// Clipsの内容から判定される、このVirtualAnimatorCopyClipSetの種別を取得します。
        /// </summary>
        public VirtualAnimatorCopyClipSetType Type
        {
            get
            {
                if (type == VirtualAnimatorCopyClipSetType.None) { type = GetClipSetType(); }
                return type;
            }
        }

        /// <summary>
        /// Layers種別でコピーされた場合の、コピー元レイヤーが属していた親VirtualAnimatorControllerを取得します。
        /// </summary>
        public VirtualAnimatorController ParentController { get; private set; }

        /// <summary>
        /// VirtualStateMachine配下のオブジェクトとしてコピーされた場合の、コピー元の共通の祖先VirtualStateMachineを取得します。
        /// </summary>
        public VirtualStateMachine AncestorStateMachine { get; private set; }

        /// <summary>
        /// コピー時に指定した親VirtualAnimatorController/祖先VirtualStateMachineと、実際のコピー対象が一致しなかった場合にtrueになります。
        /// </summary>
        public bool IsAncestorMismatched { get; private set; }

        internal VirtualAnimatorCopyClipSet(VirtualLayer layer, VirtualAnimatorController parentController) : this(new VirtualLayer[] { layer }, parentController) { }

        internal VirtualAnimatorCopyClipSet(IEnumerable<VirtualLayer> layers, VirtualAnimatorController parentController)
        {
            ClipSetInit(layers);
            if (Type != VirtualAnimatorCopyClipSetType.Layers) return;

            AncestorSetting(layers, parentController);
            ContextsSetting(parentController);
        }

        internal VirtualAnimatorCopyClipSet(object obj, VirtualLayer parentLayer) : this(new object[] { obj }, parentLayer.StateMachine) { }

        internal VirtualAnimatorCopyClipSet(IEnumerable<object> objs, VirtualLayer parentLayer) : this(objs, parentLayer.StateMachine) { }

        internal VirtualAnimatorCopyClipSet(object obj, VirtualStateMachine ancestorStateMachine) : this(new object[] { obj }, ancestorStateMachine) { }

        internal VirtualAnimatorCopyClipSet(IEnumerable<object> objs, VirtualStateMachine ancestorStateMachine)
        {
            ClipSetInit(objs);
            if (!Type.IsInStateMachineCategory()) return;

            AncestorSetting(objs, ancestorStateMachine);
            ContextsSetting(ancestorStateMachine);
        }

        internal VirtualAnimatorCopyClipSet(StateMachineBehaviour behaviour) : this(new StateMachineBehaviour[] { behaviour }) { }

        internal VirtualAnimatorCopyClipSet(IEnumerable<StateMachineBehaviour> behaviours)
        {
            ClipSetInit(behaviours);
            if (Type != VirtualAnimatorCopyClipSetType.Behaviours) return;

            ContextsSetting();
        }

        internal VirtualAnimatorCopyClipSet(object obj) : this(new object[] { obj }) { }

        internal VirtualAnimatorCopyClipSet(IEnumerable<object> objs)
        {
            ClipSetInit(objs);

            ContextsSetting();
        }

        private VirtualAnimatorCopyClipSet(
            IEnumerable<VirtualAnimatorCopyClip> clips,
            VirtualAnimatorController parentController,
            VirtualStateMachine ancestorStateMachine)
        {
            Clips = new(clips.ToList());
            ParentController = parentController;
            AncestorStateMachine = ancestorStateMachine;
        }

        /// <summary>
        /// Clipsに含まれる全てのオブジェクトを複製した、新しいVirtualAnimatorCopyClipSetを作成します。
        /// </summary>
        public VirtualAnimatorCopyClipSet Clone(CloneContext context) => Clone(context, out var _);

        /// <summary>
        /// Clipsに含まれる全てのオブジェクトを複製した、新しいVirtualAnimatorCopyClipSetを作成します。
        /// </summary>
        public VirtualAnimatorCopyClipSet Clone(CloneContext context, out Dictionary<object, object> clonedMap)
        {
            VirtualAnimatorCloner cloner = new(context) { DefaultPolicy = VirtualAnimatorCloner.ClonePolicy.KeepReference };
            cloner.SetRangeClonePolicy(Clips.SelectMany(GetCloneScope), VirtualAnimatorCloner.ClonePolicy.Clone);
            VirtualAnimatorCopyClipSet cloneClipSet = Clone(cloner);
            clonedMap = cloner.GetClonedMap();
            return cloneClipSet;
        }

        private static IEnumerable<object> GetCloneScope(VirtualAnimatorCopyClip clip) =>
            VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(clip.Type)?.GetCloneScope(clip.Object) ?? Array.Empty<object>();

        /// <summary>
        /// 指定したVirtualAnimatorClonerを使ってClipsに含まれる全てのオブジェクトを複製した、新しいVirtualAnimatorCopyClipSetを作成します。
        /// クローン対象の範囲やClonePolicyの設定は、呼び出し側が事前にclonerへ設定しておく必要があります。
        /// </summary>
        public VirtualAnimatorCopyClipSet Clone(VirtualAnimatorCloner cloner)
        {
            List<VirtualAnimatorCopyClip> cloneClips = new();
            foreach (VirtualAnimatorCopyClip clip in Clips)
            {
                VirtualAnimatorCopyClip cloneClip = clip.Clone(cloner);
                cloneClips.Add(cloneClip);
            }

            VirtualAnimatorController assignParentController = cloner.TryCloneObject(ParentController, out object cloneParentController) ? (VirtualAnimatorController)cloneParentController : ParentController;
            VirtualStateMachine assignAncestorStateMachine = cloner.TryCloneObject(AncestorStateMachine, out object cloneAncestorStateMachine) ? (VirtualStateMachine)cloneAncestorStateMachine : AncestorStateMachine;
            VirtualAnimatorCopyClipSet cloneClipSet = new(cloneClips, assignParentController, assignAncestorStateMachine);

            return cloneClipSet;
        }

        private void ClipSetInit(IEnumerable<object> objs)
        {
            Clips = new(objs.Select(o => CreateClipBase(o)).ToList());
        }

        private void AncestorSetting(IEnumerable<VirtualLayer> layers, VirtualAnimatorController parentController)
        {
            if (parentController != null)
            {
                List<VirtualLayer> parentLayers = parentController.Layers.ToList();
                if (layers.All(l => parentLayers.Any(pcl => l.StateMachine == pcl.StateMachine)))
                {
                    ParentController = parentController;
                }
                else
                {
                    IsAncestorMismatched = true;
                    Debug.LogWarning("指定された親VirtualAnimatorControllerに含まれていないVirtualLayerがコピーされました。\n親VirtualAnimatorControllerは未指定状態になります");
                }
            }
        }

        private void AncestorSetting(IEnumerable<object> objs, VirtualStateMachine ancestorStateMachine)
        {
            if (ancestorStateMachine != null)
            {
                HashSet<object> descendantObjs = new() { ancestorStateMachine };
                descendantObjs.UnionWith(VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(ancestorStateMachine));

                if (
                    objs.All(o => descendantObjs.Contains(o) ||
                        (o is VirtualStateMachine.VirtualChildState cas && cas.State != null && descendantObjs.Contains(cas.State)) ||
                        (o is VirtualStateMachine.VirtualChildStateMachine casm && descendantObjs.Contains(casm.StateMachine))
                    )
                )
                {
                    AncestorStateMachine = ancestorStateMachine;
                }
                else
                {
                    IsAncestorMismatched = true;
                    Debug.LogWarning("指定されたVirtualStateMachineの子孫に含まれていないオブジェクトがコピーされました。\n先祖VirtualStateMachineは未指定状態になります");
                }
            }
        }

        private void ContextsSetting(VirtualAnimatorController parentController)
        {
            HashSet<object> relatedObjs = new(parentController.Layers.Cast<object>()) { parentController };
            ContextsSettingInternal(relatedObjs);
        }

        private void ContextsSetting(VirtualStateMachine ancestorStateMachine)
        {
            HashSet<object> relatedObjs = new() { ancestorStateMachine };
            relatedObjs.UnionWith(VirtualAnimatorGraphTraversal.ListupObjectsInStateMachine(ancestorStateMachine));
            ContextsSettingInternal(relatedObjs);
        }

        private void ContextsSetting()
        {
            ContextsSettingInternal(Array.Empty<object>());
        }

        private void ContextsSettingInternal(IEnumerable<object> relatedObjs)
        {
            // Clipsを型ごとに仕分ける
            var groupedClips = Clips.GroupBy(c => c.Type);
            VirtualAnimatorCopyClip[] transitionClips = groupedClips.Where(g => g.Key == typeof(VirtualTransition)).SelectMany(g => g.Select(cb => (VirtualAnimatorCopyClip)cb)).ToArray();
            VirtualAnimatorCopyClip[] stateTransitionClips = groupedClips.Where(g => g.Key == typeof(VirtualStateTransition)).SelectMany(g => g.Select(cb => (VirtualAnimatorCopyClip)cb)).ToArray();
            VirtualAnimatorCopyClip[] layerClips = groupedClips.Where(g => g.Key == typeof(VirtualLayer)).SelectMany(g => g.Select(cb => (VirtualAnimatorCopyClip)cb)).ToArray();

            // Clipsの中身を取り出す
            IEnumerable<object> clipObjs = Clips.Select(static x => x.Object switch
                {
                    VirtualStateMachine.VirtualChildState cas => (object)cas.State,
                    VirtualStateMachine.VirtualChildStateMachine csam => csam.StateMachine,
                    _ => x.Object,
                });
            // Clipsの中身を含めた全ての関連性のあるオブジェクト
            HashSet<object> totalRelatedObjHashSet = clipObjs.Where(x => x != null).Union(relatedObjs.Where(x => x != null)).ToHashSet();
            var groupedObjs = totalRelatedObjHashSet.GroupBy(c => c.GetType());

            VirtualState[] stateObjs = groupedObjs.Where(g => g.Key == typeof(VirtualState)).SelectMany(g => g.Select(cb => (VirtualState)cb)).ToArray();
            VirtualStateMachine[] stateMachineObjs = groupedObjs.Where(g => g.Key == typeof(VirtualStateMachine)).SelectMany(g => g.Select(cb => (VirtualStateMachine)cb)).ToArray();
            VirtualAnimatorController[] animatorControllerObjs = groupedObjs.Where(g => g.Key == typeof(VirtualAnimatorController)).SelectMany(g => g.Select(cb => (VirtualAnimatorController)cb)).ToArray();

            // 各Clipsに関連のあるオブジェクトや情報をコンテキストとして登録する
            // VirtualTransition → (親StateMachine, PropertyName) のインデックス
            var transitionParentIndex = new Dictionary<VirtualTransition, (VirtualStateMachine Parent, VirtualAnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (VirtualStateMachine asm in stateMachineObjs)
            {
                foreach (VirtualTransition at in asm.EntryTransitions)
                {
                    transitionParentIndex[at] = (asm, VirtualAnimatorCopyClip.ContextValue.PropertyName.m_EntryTransitions);
                }
                foreach (VirtualStateMachine.VirtualChildStateMachine csm in asm.StateMachines)
                {
                    // csm.StateMachineはnullになりうる(構造体VirtualChildStateMachineの未初期化フィールド)。
                    // ImmutableDictionary<TKey,TValue>.TryGetValueはnullキーを受け付けずArgumentNullExceptionを
                    // 投げるため(Task2/5/6で確認済みの既知パターン)、事前にnullガードする。
                    if (csm.StateMachine != null && asm.StateMachineTransitions.TryGetValue(csm.StateMachine, out ImmutableList<VirtualTransition> transitions))
                    {
                        foreach (VirtualTransition at in transitions)
                        {
                            transitionParentIndex[at] = (asm, VirtualAnimatorCopyClip.ContextValue.PropertyName.m_StateMachineTransitions);
                        }
                    }
                }
            }

            // VirtualStateTransition → 親(StateMachine or State) のインデックス
            var stateTransitionParentIndex = new Dictionary<VirtualStateTransition, (object Parent, VirtualAnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (VirtualStateMachine asm in stateMachineObjs)
            {
                foreach (VirtualStateTransition ast in asm.AnyStateTransitions)
                {
                    stateTransitionParentIndex[ast] = (asm, VirtualAnimatorCopyClip.ContextValue.PropertyName.m_AnyStateTransitions);
                }
            }
            foreach (VirtualState state in stateObjs)
            {
                foreach (VirtualStateTransition ast in state.Transitions)
                {
                    stateTransitionParentIndex[ast] = (state, VirtualAnimatorCopyClip.ContextValue.PropertyName.m_Transitions);
                }
            }

            // VirtualLayer → 親VirtualAnimatorController のインデックス
            var layerParentIndex = new Dictionary<VirtualLayer, (VirtualAnimatorController Parent, VirtualAnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (VirtualAnimatorController ac in animatorControllerObjs)
            {
                foreach (VirtualLayer acl in ac.Layers)
                {
                    layerParentIndex[acl] = (ac, VirtualAnimatorCopyClip.ContextValue.PropertyName.m_AnimatorLayers);
                }
            }

            foreach (VirtualAnimatorCopyClip transitionClip in transitionClips)
            {
                if (transitionParentIndex.TryGetValue((VirtualTransition)transitionClip.Object, out var entry))
                {
                    transitionClip.SetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    transitionClip.SetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }

            foreach (VirtualAnimatorCopyClip stateTransitionClip in stateTransitionClips)
            {
                if (stateTransitionParentIndex.TryGetValue((VirtualStateTransition)stateTransitionClip.Object, out var entry))
                {
                    stateTransitionClip.SetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    stateTransitionClip.SetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }

            foreach (VirtualAnimatorCopyClip layerClip in layerClips)
            {
                if (layerParentIndex.TryGetValue((VirtualLayer)layerClip.Object, out var entry))
                {
                    layerClip.SetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    layerClip.SetAnimatorContext(VirtualAnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }
        }

        private VirtualAnimatorCopyClip CreateClipBase(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            object normalized = VirtualAnimatorCopyObjectKindRegistry.Shared.Normalize(obj);
            if (VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(normalized.GetType()) == null)
            {
                throw new ArgumentException($"コピー対象として未対応の型です: {normalized.GetType().FullName}", nameof(obj));
            }

            return new VirtualAnimatorCopyClip(normalized);
        }

        private VirtualAnimatorCopyClipSetType GetClipSetType()
        {
            IVirtualAnimatorCopyObjectKind[] kinds = Clips
                .Select(x => VirtualAnimatorCopyObjectKindRegistry.Shared.Resolve(x.Type))
                .ToArray();

            if (Array.Exists(kinds, k => k == null))
            {
                return VirtualAnimatorCopyClipSetType.Other;
            }

            IVirtualAnimatorCopyObjectKind[] distinctKinds = kinds.Distinct().ToArray();

            if (distinctKinds.Length == 1)
            {
                IVirtualAnimatorCopyObjectKind kind = distinctKinds[0];
                if (kind.IsInStateMachineObject && Clips.Count >= 2)
                {
                    return VirtualAnimatorCopyClipSetType.InStateMachineObjects;
                }

                return kind.SingleClipSetType;
            }

            if (distinctKinds.Length > 0 && Array.TrueForAll(distinctKinds, k => k.IsInStateMachineObject))
            {
                return VirtualAnimatorCopyClipSetType.InStateMachineObjects;
            }

            return VirtualAnimatorCopyClipSetType.Other;
        }

        /// <summary>
        /// VirtualAnimatorCopyClipSetが表しているコピー対象オブジェクトの種別です。
        /// </summary>
        public enum VirtualAnimatorCopyClipSetType
        {
            /// <summary>種別が未計算であることを示す内部初期状態。Typeプロパティがこの値を返すことはありません。</summary>
            None,
            /// <summary>VirtualLayerのコピー。</summary>
            Layers,
            /// <summary>VirtualTransitionのコピー。</summary>
            Transition,
            /// <summary>VirtualStateTransitionのコピー。</summary>
            StateTransition,
            /// <summary>VirtualChildStateのコピー。</summary>
            ChildState,
            /// <summary>VirtualChildStateMachineのコピー。</summary>
            ChildStateMachine,
            /// <summary>VirtualStateMachine配下のオブジェクトを、複数件または複数種別にまたがってコピーした場合。</summary>
            InStateMachineObjects,
            /// <summary>StateMachineBehaviourのコピー。</summary>
            Behaviours,
            /// <summary>上記いずれにも該当しない、またはコピー対象として未対応の型を含む場合。</summary>
            Other
        }
    }
}
