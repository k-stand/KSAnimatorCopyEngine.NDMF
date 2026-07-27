using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorcopyengine.ndmf.editor
{
    /// <summary>
    /// Virtual Animator関連オブジェクトのクローンを行うエンジンです。
    /// オブジェクトごとにClonePolicyを設定することで、クローンする/元の参照を保持する/切り離す(null化)を制御できます。
    /// </summary>
    public sealed partial class VirtualAnimatorCloner
    {
        // VirtualLayer.Create/VirtualAnimatorController.Createが仮想レイヤーインデックスの採番等で
        // CloneContextを要求するため保持する。CloneContext.Clone/GetOrClone(重複排除キャッシュ
        // ベースのクローン)は使わない。ClonePolicy制御(_policyMap/_parentMap/_cloneMap)は
        // 完全に独自実装であり、このフィールドは新規VirtualLayer/VirtualAnimatorController
        // インスタンスを生成する際にのみ使用する。
        private readonly CloneContext _context;

        /// <summary>
        /// 指定したCloneContextを使ってVirtualAnimatorClonerを初期化します。
        /// </summary>
        /// <param name="context">新規VirtualLayer/VirtualAnimatorControllerインスタンスの生成に使うCloneContext。</param>
        public VirtualAnimatorCloner(CloneContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 個別にClonePolicyが設定されていないオブジェクトに適用される既定のポリシーを取得または設定します。初期値はDetachです。
        /// </summary>
        public ClonePolicy DefaultPolicy { get; set; } = ClonePolicy.Detach;

        private readonly Dictionary<object, ClonePolicy> _policyMap = new();

        private readonly Dictionary<object, object> _parentMap = new();

        private readonly Dictionary<object, object> _cloneMap = new();

        /// <summary>
        /// このクラスがクローン対象として認識しているVirtual Animator関連の型の一覧です。
        /// </summary>
        public static readonly IReadOnlyCollection<Type> CloneableTypes = new HashSet<Type>
        {
            typeof(VirtualAnimatorController),
            typeof(AnimatorControllerParameter),
            typeof(VirtualLayer),
            typeof(VirtualStateMachine.VirtualChildStateMachine),
            typeof(VirtualStateMachine),
            typeof(VirtualStateMachine.VirtualChildState),
            typeof(VirtualState),
            typeof(VirtualTransition),
            typeof(VirtualStateTransition),
            typeof(AnimatorCondition),
            typeof(StateMachineBehaviour),
            typeof(VirtualClip),
            typeof(VirtualBlendTree),
        };

        /// <summary>
        /// クローン後のオブジェクトの名前を、元の名前から生成する関数を取得または設定します。既定では元の名前をそのまま使用します。
        /// </summary>
        public Func<string, string> NameTransformer { get; set; } = static origName => string.IsNullOrEmpty(origName) ? "" : origName;

        /// <summary>
        /// 指定したオブジェクトのClonePolicyを設定します。あわせてそのオブジェクトの子要素を、ポリシー継承のために内部登録します。
        /// </summary>
        /// <param name="obj">ポリシーを設定するオブジェクト。nullの場合は何もしません。</param>
        /// <param name="policy">設定するClonePolicy。</param>
        public void SetClonePolicy(object obj, ClonePolicy policy)
        {
            if (obj == null) return;
            _policyMap[obj] = policy;
            RegisterChildrenRecursively(obj);
        }

        /// <summary>
        /// 複数のオブジェクトに対して、まとめてSetClonePolicyを行います。
        /// </summary>
        /// <param name="objs">ポリシーを設定するオブジェクトの列挙。</param>
        /// <param name="policy">設定するClonePolicy。</param>
        public void SetRangeClonePolicy(IEnumerable<object> objs, ClonePolicy policy)
        {
            foreach (object obj in objs) SetClonePolicy(obj, policy);
        }

        /// <summary>
        /// 指定したオブジェクトに、まだ設定されていないか、より優先度の低いClonePolicyしか設定されていない場合にのみ、ClonePolicyを設定します。
        /// 既に同等以上の優先度のポリシーが設定済みの場合は何もしません。
        /// </summary>
        /// <param name="obj">ポリシーを設定するオブジェクト。nullの場合は何もしません。</param>
        /// <param name="policy">設定するClonePolicy。</param>
        public void SetClonePolicyIfAbsent(object obj, ClonePolicy policy)
        {
            if (obj != null && (!_policyMap.TryGetValue(obj, out ClonePolicy current) || current < policy))
            {
                _policyMap[obj] = policy;
                RegisterChildrenRecursively(obj);
            }
        }

        /// <summary>
        /// 複数のオブジェクトに対して、まとめてSetClonePolicyIfAbsentを行います。
        /// </summary>
        /// <param name="objs">ポリシーを設定するオブジェクトの列挙。</param>
        /// <param name="policy">設定するClonePolicy。</param>
        public void SetRangeClonePolicyIfAbsent(IEnumerable<object> objs, ClonePolicy policy)
        {
            foreach (object obj in objs) SetClonePolicyIfAbsent(obj, policy);
        }

        /// <summary>
        /// 指定したオブジェクトに個別設定されているClonePolicyを削除します。以降はDefaultPolicyまたは親からの継承が適用されます。
        /// </summary>
        /// <param name="obj">設定を削除するオブジェクト。nullの場合は何もしません。</param>
        public void RemoveClonePolicy(object obj)
        {
            if (obj == null) return;
            _policyMap.Remove(obj);
        }

        /// <summary>
        /// 指定したオブジェクトに個別設定されているClonePolicyの取得を試みます。親からの継承やDefaultPolicyは考慮しません。
        /// </summary>
        /// <param name="obj">ClonePolicyを取得するオブジェクト。nullの場合は常にfalseを返します。</param>
        /// <param name="policy">個別設定されている場合はそのClonePolicy、されていない場合は既定値。</param>
        /// <returns>個別設定が存在する場合はtrue。</returns>
        public bool TryGetClonePolicy(object obj, out ClonePolicy policy)
        {
            if (obj == null)
            {
                policy = default;
                return false;
            }
            return _policyMap.TryGetValue(obj, out policy);
        }

        private ClonePolicy GetClonePolicy(object obj)
        {
            // 手動設定があればそれを使う
            if (_policyMap.TryGetValue(obj, out ClonePolicy policy)) return policy;

            // 親を辿って継承
            if (_parentMap.TryGetValue(obj, out object parent))
                return GetClonePolicy(parent); // 再帰

            // どこにも設定がなければDefaultPolicy
            return DefaultPolicy;
        }

        /// <summary>
        /// 個別設定されている全てのClonePolicyのコピーを取得します。
        /// </summary>
        /// <returns>オブジェクトをキー、設定されているClonePolicyを値とするマップ。</returns>
        public Dictionary<object, ClonePolicy> GetAllClonePolicy() => new(_policyMap);

        private void RegisterChildrenRecursively(object obj)
        {
            switch (obj)
            {
                case VirtualAnimatorController ac:
                    foreach (VirtualLayer layer in ac.Layers)
                    {
                        if (layer.StateMachine != null && !(_parentMap.TryGetValue(layer.StateMachine, out object registeredParent) && registeredParent == ac))
                        {
                            _parentMap[layer.StateMachine] = ac;
                            RegisterChildrenRecursively(layer.StateMachine);
                        }
                    }
                    break;
                case VirtualStateMachine asm:
                    foreach (VirtualStateTransition ast in asm.AnyStateTransitions)
                    {
                        if (ast != null && !(_parentMap.TryGetValue(ast, out object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[ast] = asm;
                            RegisterChildrenRecursively(ast);
                        }
                    }
                    foreach (VirtualTransition at in asm.EntryTransitions)
                    {
                        if (at != null && !(_parentMap.TryGetValue(at, out object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[at] = asm;
                            RegisterChildrenRecursively(at);
                        }
                    }
                    foreach (VirtualStateMachine.VirtualChildState cas in asm.States)
                    {
                        if (cas.State != null && !(_parentMap.TryGetValue(cas.State, out object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[cas.State] = asm;
                            RegisterChildrenRecursively(cas.State);
                        }
                    }
                    foreach (VirtualStateMachine.VirtualChildStateMachine casm in asm.StateMachines)
                    {
                        if (casm.StateMachine != null && !(_parentMap.TryGetValue(casm.StateMachine, out object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[casm.StateMachine] = asm;
                            RegisterChildrenRecursively(casm.StateMachine);
                        }
                    }
                    foreach (StateMachineBehaviour behaviour in asm.Behaviours)
                    {
                        if (behaviour != null && !(_parentMap.TryGetValue(behaviour, out object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[behaviour] = asm;
                        }
                    }
                    break;
                case VirtualState state:
                    foreach (VirtualStateTransition transition in state.Transitions)
                    {
                        if (transition != null && !(_parentMap.TryGetValue(transition, out object registeredParent) && registeredParent == state))
                        {
                            _parentMap[transition] = state;
                        }
                    }
                    foreach (StateMachineBehaviour behaviour in state.Behaviours)
                    {
                        if (behaviour != null && !(_parentMap.TryGetValue(behaviour, out object registeredParent) && registeredParent == state))
                        {
                            _parentMap[behaviour] = state;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// これまでにクローンされたオブジェクトの、元オブジェクトから複製後オブジェクトへのマップを取得します。
        /// ClonePolicy.KeepReference等により元と同一のまま返されたものは含まれません。
        /// </summary>
        /// <returns>元オブジェクトをキー、複製後オブジェクトを値とするマップ。</returns>
        public Dictionary<object, object> GetClonedMap() => _cloneMap.Where(kvp => kvp.Key != kvp.Value).ToDictionary(x => x.Key, x => x.Value);

        /// <summary>
        /// これまでにクローンされたVirtualClipのみを抽出した、元クリップから複製後クリップへのマップを取得します。
        /// </summary>
        public Dictionary<VirtualClip, VirtualClip> GetClonedVirtualClips() => _cloneMap
            .Where(kvp => kvp.Key is VirtualClip origClip &&
                          kvp.Value is VirtualClip cloneClip &&
                          origClip != cloneClip)
            .ToDictionary(kvp => (VirtualClip)kvp.Key, kvp => (VirtualClip)kvp.Value);

        /// <summary>
        /// これまでにクローンされたVirtualBlendTreeのみを抽出した、元ツリーから複製後ツリーへのマップを取得します。
        /// </summary>
        public Dictionary<VirtualBlendTree, VirtualBlendTree> GetClonedVirtualBlendTrees() => _cloneMap
            .Where(kvp => kvp.Key is VirtualBlendTree origTree &&
                          kvp.Value is VirtualBlendTree cloneTree &&
                          origTree != cloneTree)
            .ToDictionary(kvp => (VirtualBlendTree)kvp.Key, kvp => (VirtualBlendTree)kvp.Value);

        /// <summary>
        /// GetClonedMap()の内容のうち、指定した型に一致する元オブジェクトと複製後オブジェクトの組み合わせごとにactionを呼び出します。
        /// </summary>
        public void ForEachCloned<T>(Action<T, T> action) where T : class
            => ForEachCloned(GetClonedMap(), action);

        /// <summary>
        /// 指定した元オブジェクトから複製後オブジェクトへのマップのうち、指定した型に一致する組み合わせごとにactionを呼び出します。
        /// </summary>
        public static void ForEachCloned<T>(IReadOnlyDictionary<object, object> clonedMap, Action<T, T> action) where T : class
        {
            foreach (KeyValuePair<object, object> kvp in clonedMap)
            {
                if (kvp.Key is T orig && kvp.Value is T clone && orig != clone)
                {
                    action(orig, clone);
                }
            }
        }

        private TResult CloneWithMap<TArg, TResult>(TArg orig, Func<TArg, TResult> cloneInternal, out Dictionary<object, object> clonedMap)
        {
            TResult clone = cloneInternal(orig);
            clonedMap = GetClonedMap();
            return clone;
        }

        /// <summary>
        /// オブジェクトの実際の型を判定してクローンします。CloneableTypesに含まれない型の場合はnullを返します。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public object CloneObject(object orig) => CloneObject(orig, out _);

        /// <summary>
        /// オブジェクトの実際の型を判定してクローンします。CloneableTypesに含まれない型の場合はnullを返します。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public object CloneObject(object orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneObjectInternal, out clonedMap);

        /// <summary>
        /// オブジェクトの実際の型を判定してクローンを試みます。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <param name="clone">成功した場合はクローンされたオブジェクト、失敗した場合はnull。</param>
        /// <returns>origがnullでなく、かつCloneableTypesに含まれる型でクローンに成功した場合はtrue。</returns>
        public bool TryCloneObject(object orig, out object clone)
        {
            object tempClone;
            if (orig == null || (tempClone = CloneObjectInternal(orig)) == null)
            {
                clone = null;
                return false;
            }

            clone = tempClone;
            return true;
        }

        private object CloneObjectInternal(object orig) => orig switch
        {
            VirtualAnimatorController castedOrig => CloneVirtualAnimatorControllerInternal(castedOrig),
            AnimatorControllerParameter castedOrig => CloneAnimatorControllerParameter(castedOrig),
            VirtualLayer castedOrig => CloneVirtualLayerInternal(castedOrig),
            VirtualStateMachine.VirtualChildStateMachine castedOrig => CloneVirtualChildStateMachineInternal(castedOrig),
            VirtualStateMachine castedOrig => CloneVirtualStateMachineInternal(castedOrig),
            VirtualStateMachine.VirtualChildState castedOrig => CloneVirtualChildStateInternal(castedOrig),
            VirtualState castedOrig => CloneVirtualStateInternal(castedOrig),
            VirtualTransition castedOrig => CloneVirtualTransitionInternal(castedOrig),
            VirtualStateTransition castedOrig => CloneVirtualStateTransitionInternal(castedOrig),
            AnimatorCondition castedOrig => CloneAnimatorCondition(castedOrig),
            StateMachineBehaviour castedOrig => CloneStateMachineBehaviourInternal(castedOrig),
            VirtualClip castedOrig => CloneVirtualClipInternal(castedOrig),
            VirtualBlendTree castedOrig => CloneVirtualBlendTreeInternal(castedOrig),
            _ => null,
        };

        /// <summary>
        /// VirtualAnimatorControllerをクローンします。Parameters/Layersを含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualAnimatorController。</param>
        /// <returns>クローンされたVirtualAnimatorController。</returns>
        public VirtualAnimatorController CloneVirtualAnimatorController(VirtualAnimatorController orig) => CloneVirtualAnimatorController(orig, out _);

        /// <summary>
        /// VirtualAnimatorControllerをクローンします。Parameters/Layersを含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualAnimatorController。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたVirtualAnimatorController。</returns>
        public VirtualAnimatorController CloneVirtualAnimatorController(VirtualAnimatorController orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualAnimatorControllerInternal, out clonedMap);

        private VirtualAnimatorController CloneVirtualAnimatorControllerInternal(VirtualAnimatorController orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, () => VirtualAnimatorController.Create(_context, GetCloneObjName(orig.Name)), out VirtualAnimatorController clone);
            if (!isCreated) return clone;

            clone.Parameters = orig.Parameters.Values.Select(CloneAnimatorControllerParameter).ToImmutableDictionary(p => p.name);
            ThrowIfKeepReferenceChildren(orig.Layers.Select(x => (object)x.StateMachine));
            // VirtualAnimatorController.Layersのsetterは全レイヤーをLayerPriority(0)として設定する
            // (VirtualAnimatorController自体の仕様、元の優先度情報は保持できない)。
            clone.Layers = CloneVirtualLayersInternal(orig.Layers);

            return clone;
        }

        /// <summary>
        /// 複数のAnimatorControllerParameterをまとめてクローンします。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public AnimatorControllerParameter[] CloneAnimatorControllerParameters(IEnumerable<AnimatorControllerParameter> origs)
        {
            return origs.Select(orig => CloneAnimatorControllerParameter(orig)).ToArray();
        }

        /// <summary>
        /// AnimatorControllerParameterをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorControllerParameter。</param>
        /// <returns>クローンされたAnimatorControllerParameter。</returns>
        public AnimatorControllerParameter CloneAnimatorControllerParameter(AnimatorControllerParameter orig)
        {
            return new()
            {
                defaultBool = orig.defaultBool,
                defaultFloat = orig.defaultFloat,
                defaultInt = orig.defaultInt,
                name = GetCloneObjName(orig.name),
                type = orig.type
            };
        }

        /// <summary>
        /// 複数のVirtualLayerをまとめてクローンします。
        /// レイヤー自体は常に新規クローンされます(ClonePolicyの対象外)。内部のStateMachine以下はClonePolicyに従います。
        /// </summary>
        /// <param name="origs">クローン元のレイヤーの列挙。</param>
        /// <returns>クローンされたレイヤーの列挙。</returns>
        public IEnumerable<VirtualLayer> CloneVirtualLayers(IEnumerable<VirtualLayer> origs) => CloneVirtualLayers(origs, out _);

        /// <summary>
        /// 複数のVirtualLayerをまとめてクローンします。
        /// レイヤー自体は常に新規クローンされます(ClonePolicyの対象外)。内部のStateMachine以下はClonePolicyに従います。
        /// </summary>
        /// <param name="origs">クローン元のレイヤーの列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたレイヤーの列挙。</returns>
        public IEnumerable<VirtualLayer> CloneVirtualLayers(IEnumerable<VirtualLayer> origs, out Dictionary<object, object> clonedMap) => CloneWithMap(origs, CloneVirtualLayersInternal, out clonedMap);

        /// <summary>
        /// VirtualLayerをクローンします。StateMachineを含め再帰的に複製されます。
        /// レイヤー自体は常に新規クローンされます(ClonePolicyの対象外)。内部のStateMachine以下はClonePolicyに従います。
        /// </summary>
        /// <param name="orig">クローン元のレイヤー。</param>
        /// <returns>クローンされたレイヤー。</returns>
        public VirtualLayer CloneVirtualLayer(VirtualLayer orig) => CloneVirtualLayer(orig, out _);

        /// <summary>
        /// VirtualLayerをクローンします。StateMachineを含め再帰的に複製されます。
        /// レイヤー自体は常に新規クローンされます(ClonePolicyの対象外)。内部のStateMachine以下はClonePolicyに従います。
        /// </summary>
        /// <param name="orig">クローン元のレイヤー。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたレイヤー。</returns>
        public VirtualLayer CloneVirtualLayer(VirtualLayer orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualLayerInternal, out clonedMap);

        private ImmutableList<VirtualLayer> CloneVirtualLayersInternal(IEnumerable<VirtualLayer> origs)
        {
            return origs.Select(orig => CloneVirtualLayerInternal(orig)).ToImmutableList();
        }

        private VirtualLayer CloneVirtualLayerInternal(VirtualLayer orig)
        {
            // 他の全公開Clone*(orig)メソッドはTryGetOrCreateCloneInstance経由で
            // 「nullを渡すとnullを返す」契約を持つ。手動キャッシュチェックに置き換えた
            // 以下のコードは同じ契約を維持するため、まずnullガードを行う。
            if (orig == null) return null;

            // VirtualLayerは生API版のAnimatorControllerLayer(struct)に相当するが、Virtual API側では
            // classとして表現されている。生API版CloneAnimatorControllerLayerInternalはレイヤー自体を
            // ClonePolicyの対象にせず常に新規構造体を生成し、ClonePolicyで制御されるのは内部の
            // stateMachine等の参照のみだった(呼び出し元のThrowIfKeepReferenceChildren(orig.layers
            // .Select(x => x.stateMachine))も同様にstateMachineのみを見ている)。Virtual API版でも
            // この挙動を踏襲し、レイヤー自体はTryGetOrCreateCloneInstanceによるClonePolicyゲートの
            // 対象にせず、常にクローンを生成する(内部のStateMachine/オーバーライド先Stateのみ
            // ClonePolicyに従う)。
            if (_cloneMap.TryGetValue(orig, out object cached) && cached is VirtualLayer cachedClone)
            {
                return cachedClone;
            }

            VirtualLayer clone = VirtualLayer.Create(_context, GetCloneObjName(orig.Name));
            _cloneMap[orig] = _cloneMap[clone] = clone;

            // AvatarMaskはCloneableTypesに含めない(生API版がKeepReference相当で扱っていたのを踏襲、
            // 2026-07-20-ndmf-animator-api-migration-implementation-approach-v3.md §4参照)。
            clone.AvatarMask = orig.AvatarMask;
            clone.BlendingMode = orig.BlendingMode;
            clone.DefaultWeight = orig.DefaultWeight;
            clone.IKPass = orig.IKPass;
            clone.SyncedLayerAffectsTiming = orig.SyncedLayerAffectsTiming;
            clone.SyncedLayerIndex = orig.SyncedLayerIndex;
            clone.StateMachine = CloneVirtualStateMachineInternal(orig.StateMachine);

            // キー(VirtualState)のClonePolicyがKeepReference未満(Detach/UnSetting)の場合、
            // CloneVirtualStateInternalの戻り値がnullになる。ImmutableDictionaryはnullキーを
            // 許容しないため(TryGetValue等で確認済みのArgumentNullExceptionパターンと同様)、
            // 事前に除外する。
            clone.SyncedLayerMotionOverrides = orig.SyncedLayerMotionOverrides
                .Where(kvp => GetClonePolicy(kvp.Key) >= ClonePolicy.KeepReference)
                .ToImmutableDictionary(
                    kvp => CloneVirtualStateInternal(kvp.Key),
                    kvp => CloneMotionInternal(kvp.Value));

            clone.SyncedLayerBehaviourOverrides = orig.SyncedLayerBehaviourOverrides
                .Where(kvp => GetClonePolicy(kvp.Key) >= ClonePolicy.KeepReference)
                .ToImmutableDictionary(
                    kvp => CloneVirtualStateInternal(kvp.Key),
                    kvp => CloneStateMachineBehavioursInternal(kvp.Value).ToImmutableList());

            return clone;
        }

        /// <summary>
        /// 複数のVirtualChildStateMachineをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualStateMachine.VirtualChildStateMachine> CloneVirtualChildStateMachines(IEnumerable<VirtualStateMachine.VirtualChildStateMachine> origs) => CloneVirtualChildStateMachines(origs, out _);

        /// <summary>
        /// 複数のVirtualChildStateMachineをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualStateMachine.VirtualChildStateMachine> CloneVirtualChildStateMachines(IEnumerable<VirtualStateMachine.VirtualChildStateMachine> origs, out Dictionary<object, object> clonedMap) => CloneWithMap(origs, CloneVirtualChildStateMachinesInternal, out clonedMap);

        /// <summary>
        /// VirtualChildStateMachineをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public VirtualStateMachine.VirtualChildStateMachine CloneVirtualChildStateMachine(VirtualStateMachine.VirtualChildStateMachine orig) => CloneVirtualChildStateMachine(orig, out _);

        /// <summary>
        /// VirtualChildStateMachineをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public VirtualStateMachine.VirtualChildStateMachine CloneVirtualChildStateMachine(VirtualStateMachine.VirtualChildStateMachine orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualChildStateMachineInternal, out clonedMap);

        private ImmutableList<VirtualStateMachine.VirtualChildStateMachine> CloneVirtualChildStateMachinesInternal(IEnumerable<VirtualStateMachine.VirtualChildStateMachine> origs)
        {
            return origs.Where(orig => orig.StateMachine == null || GetClonePolicy(orig.StateMachine) >= ClonePolicy.KeepReference).Select(orig => CloneVirtualChildStateMachineInternal(orig)).ToImmutableList();
        }

        private VirtualStateMachine.VirtualChildStateMachine CloneVirtualChildStateMachineInternal(VirtualStateMachine.VirtualChildStateMachine orig)
        {
            return new VirtualStateMachine.VirtualChildStateMachine
            {
                Position = orig.Position,
                StateMachine = CloneVirtualStateMachineInternal(orig.StateMachine)
            };
        }

        /// <summary>
        /// VirtualStateMachineをクローンします。States/StateMachines/Transitions/Behaviours等を含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualStateMachine。</param>
        /// <returns>クローンされたVirtualStateMachine。</returns>
        public VirtualStateMachine CloneVirtualStateMachine(VirtualStateMachine orig) => CloneVirtualStateMachine(orig, out _);

        /// <summary>
        /// VirtualStateMachineをクローンします。States/StateMachines/Transitions/Behaviours等を含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualStateMachine。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたVirtualStateMachine。</returns>
        public VirtualStateMachine CloneVirtualStateMachine(VirtualStateMachine orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualStateMachineInternal, out clonedMap);

        private VirtualStateMachine CloneVirtualStateMachineInternal(VirtualStateMachine orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, () => VirtualStateMachine.Create(_context, GetCloneObjName(orig.Name)), out VirtualStateMachine clone);
            if (!isCreated) return clone;

            clone.AnyStatePosition = orig.AnyStatePosition;
            clone.EntryPosition = orig.EntryPosition;
            clone.ExitPosition = orig.ExitPosition;
            clone.ParentStateMachinePosition = orig.ParentStateMachinePosition;

            ThrowIfKeepReferenceChildren(orig.States.Where(x => x.State != null).Select(x => (object)x.State));
            clone.States = CloneVirtualChildStatesInternal(orig.States);
            ThrowIfKeepReferenceChildren(orig.StateMachines.Select(x => (object)x.StateMachine));
            clone.StateMachines = CloneVirtualChildStateMachinesInternal(orig.StateMachines);
            clone.DefaultState = CloneVirtualStateInternal(orig.DefaultState);

            ThrowIfKeepReferenceChildren(orig.AnyStateTransitions);
            clone.AnyStateTransitions = CloneVirtualStateTransitionsInternal(orig.AnyStateTransitions);
            ThrowIfKeepReferenceChildren(orig.EntryTransitions);
            clone.EntryTransitions = CloneVirtualTransitionsInternal(orig.EntryTransitions);

            ImmutableDictionary<VirtualStateMachine, ImmutableList<VirtualTransition>>.Builder stateMachineTransitionsBuilder = ImmutableDictionary.CreateBuilder<VirtualStateMachine, ImmutableList<VirtualTransition>>();
            foreach (VirtualStateMachine.VirtualChildStateMachine curCSM in orig.StateMachines)
            {
                VirtualStateMachine cloneStateMachine = CloneVirtualStateMachineInternal(curCSM.StateMachine);

                if (curCSM.StateMachine != null && cloneStateMachine != null && orig.StateMachineTransitions.TryGetValue(curCSM.StateMachine, out ImmutableList<VirtualTransition> transitions))
                {
                    ThrowIfKeepReferenceChildren(transitions);
                    stateMachineTransitionsBuilder[cloneStateMachine] = CloneVirtualTransitionsInternal(transitions);
                }
            }
            clone.StateMachineTransitions = stateMachineTransitionsBuilder.ToImmutable();

            ThrowIfKeepReferenceChildren(orig.Behaviours);
            clone.Behaviours = CloneStateMachineBehavioursInternal(orig.Behaviours).ToImmutableList();

            return clone;
        }

        /// <summary>
        /// 複数のVirtualChildStateをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualStateMachine.VirtualChildState> CloneVirtualChildStates(IEnumerable<VirtualStateMachine.VirtualChildState> origs) => CloneVirtualChildStates(origs, out _);

        /// <summary>
        /// 複数のVirtualChildStateをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualStateMachine.VirtualChildState> CloneVirtualChildStates(IEnumerable<VirtualStateMachine.VirtualChildState> origs, out Dictionary<object, object> clonedMap) => CloneWithMap(origs, CloneVirtualChildStatesInternal, out clonedMap);

        /// <summary>
        /// VirtualChildStateをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public VirtualStateMachine.VirtualChildState CloneVirtualChildState(VirtualStateMachine.VirtualChildState orig) => CloneVirtualChildState(orig, out _);

        /// <summary>
        /// VirtualChildStateをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public VirtualStateMachine.VirtualChildState CloneVirtualChildState(VirtualStateMachine.VirtualChildState orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualChildStateInternal, out clonedMap);

        private ImmutableList<VirtualStateMachine.VirtualChildState> CloneVirtualChildStatesInternal(IEnumerable<VirtualStateMachine.VirtualChildState> origs)
        {
            return origs.Where(orig => orig.State == null || GetClonePolicy(orig.State) >= ClonePolicy.KeepReference).Select(orig => CloneVirtualChildStateInternal(orig)).ToImmutableList();
        }

        private VirtualStateMachine.VirtualChildState CloneVirtualChildStateInternal(VirtualStateMachine.VirtualChildState orig)
        {
            return new VirtualStateMachine.VirtualChildState
            {
                Position = orig.Position,
                State = CloneVirtualStateInternal(orig.State)
            };
        }

        /// <summary>
        /// VirtualStateをクローンします。Transitions/Behaviours/Motion(VirtualClip/VirtualBlendTree)を含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualState。</param>
        /// <returns>クローンされたVirtualState。</returns>
        public VirtualState CloneVirtualState(VirtualState orig) => CloneVirtualState(orig, out _);

        /// <summary>
        /// VirtualStateをクローンします。Transitions/Behaviours/Motion(VirtualClip/VirtualBlendTree)を含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualState。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたVirtualState。</returns>
        public VirtualState CloneVirtualState(VirtualState orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualStateInternal, out clonedMap);

        private VirtualState CloneVirtualStateInternal(VirtualState orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, () => VirtualState.Create(GetCloneObjName(orig.Name)), out VirtualState clone);
            if (!isCreated) return clone;

            clone.CycleOffset = orig.CycleOffset;
            clone.CycleOffsetParameter = orig.CycleOffsetParameter;
            clone.IKOnFeet = orig.IKOnFeet;
            clone.Mirror = orig.Mirror;
            clone.MirrorParameter = orig.MirrorParameter;
            clone.Motion = CloneMotionInternal(orig.Motion);
            clone.Speed = orig.Speed;
            clone.SpeedParameter = orig.SpeedParameter;
            clone.Tag = orig.Tag;
            clone.TimeParameter = orig.TimeParameter;
            clone.WriteDefaultValues = orig.WriteDefaultValues;

            ThrowIfKeepReferenceChildren(orig.Transitions);
            clone.Transitions = CloneVirtualStateTransitionsInternal(orig.Transitions);

            ThrowIfKeepReferenceChildren(orig.Behaviours);
            clone.Behaviours = CloneStateMachineBehavioursInternal(orig.Behaviours).ToImmutableList();

            return clone;
        }

        /// <summary>
        /// 複数のVirtualTransitionをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualTransition> CloneVirtualTransitions(IEnumerable<VirtualTransition> origs) => CloneVirtualTransitions(origs, out _);

        /// <summary>
        /// 複数のVirtualTransitionをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualTransition> CloneVirtualTransitions(IEnumerable<VirtualTransition> origs, out Dictionary<object, object> clonedMap) => CloneWithMap(origs, CloneVirtualTransitionsInternal, out clonedMap);

        /// <summary>
        /// VirtualTransitionをクローンします。DestinationState/DestinationStateMachine/Conditionsを含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualTransition。</param>
        /// <returns>クローンされたVirtualTransition。</returns>
        public VirtualTransition CloneVirtualTransition(VirtualTransition orig) => CloneVirtualTransition(orig, out _);

        /// <summary>
        /// VirtualTransitionをクローンします。DestinationState/DestinationStateMachine/Conditionsを含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualTransition。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたVirtualTransition。</returns>
        public VirtualTransition CloneVirtualTransition(VirtualTransition orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualTransitionInternal, out clonedMap);

        private ImmutableList<VirtualTransition> CloneVirtualTransitionsInternal(IEnumerable<VirtualTransition> origs)
        {
            return origs.Where(orig => GetClonePolicy(orig) >= ClonePolicy.KeepReference).Select(orig => CloneVirtualTransitionInternal(orig)).ToImmutableList();
        }

        private VirtualTransition CloneVirtualTransitionInternal(VirtualTransition orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, VirtualTransition.Create, out VirtualTransition clone);
            if (!isCreated) return clone;

            clone.Mute = orig.Mute;
            clone.Solo = orig.Solo;

            VirtualState cloneDestState = orig.DestinationState != null ? CloneVirtualStateInternal(orig.DestinationState) : null;
            VirtualStateMachine cloneDestSM = orig.DestinationStateMachine != null ? CloneVirtualStateMachineInternal(orig.DestinationStateMachine) : null;

            if (cloneDestState != null) clone.SetDestination(cloneDestState);
            else if (cloneDestSM != null) clone.SetDestination(cloneDestSM);
            else if (orig.IsExit) clone.SetExitDestination();

            clone.Conditions = CloneAnimatorConditions(orig.Conditions).ToImmutableList();

            return clone;
        }

        /// <summary>
        /// 複数のVirtualStateTransitionをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualStateTransition> CloneVirtualStateTransitions(IEnumerable<VirtualStateTransition> origs) => CloneVirtualStateTransitions(origs, out _);

        /// <summary>
        /// 複数のVirtualStateTransitionをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualStateTransition> CloneVirtualStateTransitions(IEnumerable<VirtualStateTransition> origs, out Dictionary<object, object> clonedMap) => CloneWithMap(origs, CloneVirtualStateTransitionsInternal, out clonedMap);

        /// <summary>
        /// VirtualStateTransitionをクローンします。DestinationState/DestinationStateMachine/Conditionsを含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualStateTransition。</param>
        /// <returns>クローンされたVirtualStateTransition。</returns>
        public VirtualStateTransition CloneVirtualStateTransition(VirtualStateTransition orig) => CloneVirtualStateTransition(orig, out _);

        /// <summary>
        /// VirtualStateTransitionをクローンします。DestinationState/DestinationStateMachine/Conditionsを含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualStateTransition。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたVirtualStateTransition。</returns>
        public VirtualStateTransition CloneVirtualStateTransition(VirtualStateTransition orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualStateTransitionInternal, out clonedMap);

        private ImmutableList<VirtualStateTransition> CloneVirtualStateTransitionsInternal(IEnumerable<VirtualStateTransition> origs)
        {
            return origs.Where(orig => GetClonePolicy(orig) >= ClonePolicy.KeepReference).Select(orig => CloneVirtualStateTransitionInternal(orig)).ToImmutableList();
        }

        private VirtualStateTransition CloneVirtualStateTransitionInternal(VirtualStateTransition orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, VirtualStateTransition.Create, out VirtualStateTransition clone);
            if (!isCreated) return clone;

            clone.CanTransitionToSelf = orig.CanTransitionToSelf;
            clone.Duration = orig.Duration;
            clone.ExitTime = orig.ExitTime;
            clone.HasFixedDuration = orig.HasFixedDuration;
            clone.InterruptionSource = orig.InterruptionSource;
            clone.Mute = orig.Mute;
            clone.Offset = orig.Offset;
            clone.OrderedInterruption = orig.OrderedInterruption;
            clone.Solo = orig.Solo;

            VirtualState cloneDestState = orig.DestinationState != null ? CloneVirtualStateInternal(orig.DestinationState) : null;
            VirtualStateMachine cloneDestSM = orig.DestinationStateMachine != null ? CloneVirtualStateMachineInternal(orig.DestinationStateMachine) : null;

            if (cloneDestState != null) clone.SetDestination(cloneDestState);
            else if (cloneDestSM != null) clone.SetDestination(cloneDestSM);
            else if (orig.IsExit) clone.SetExitDestination();

            clone.Conditions = CloneAnimatorConditions(orig.Conditions).ToImmutableList();

            return clone;
        }

        /// <summary>
        /// 複数のAnimatorConditionをまとめてクローンします。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public AnimatorCondition[] CloneAnimatorConditions(IEnumerable<AnimatorCondition> origs)
        {
            return origs.Select(orig => CloneAnimatorCondition(orig)).ToArray();
        }

        /// <summary>
        /// AnimatorConditionをクローンします。AnimatorConditionは値型のため、実質的には元の値をそのまま返します。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorCondition。</param>
        /// <returns>クローンされたAnimatorCondition。</returns>
        public AnimatorCondition CloneAnimatorCondition(AnimatorCondition orig)
        {
            return orig;
        }

        /// <summary>
        /// 複数のStateMachineBehaviourをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<StateMachineBehaviour> CloneStateMachineBehaviours(IEnumerable<StateMachineBehaviour> origs) => CloneStateMachineBehaviours(origs, out _);

        /// <summary>
        /// 複数のStateMachineBehaviourをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<StateMachineBehaviour> CloneStateMachineBehaviours(IEnumerable<StateMachineBehaviour> origs, out Dictionary<object, object> clonedMap) => CloneWithMap(origs, CloneStateMachineBehavioursInternal, out clonedMap);

        /// <summary>
        /// StateMachineBehaviourをクローンします。実際の具象型のインスタンスが生成され、シリアライズ内容がコピーされます。
        /// </summary>
        /// <param name="orig">クローン元のStateMachineBehaviour。</param>
        /// <returns>クローンされたStateMachineBehaviour。</returns>
        public StateMachineBehaviour CloneStateMachineBehaviour(StateMachineBehaviour orig) => CloneStateMachineBehaviour(orig, out _);

        /// <summary>
        /// StateMachineBehaviourをクローンします。実際の具象型のインスタンスが生成され、シリアライズ内容がコピーされます。
        /// </summary>
        /// <param name="orig">クローン元のStateMachineBehaviour。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたStateMachineBehaviour。</returns>
        public StateMachineBehaviour CloneStateMachineBehaviour(StateMachineBehaviour orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneStateMachineBehaviourInternal, out clonedMap);

        private ImmutableList<StateMachineBehaviour> CloneStateMachineBehavioursInternal(IEnumerable<StateMachineBehaviour> origs)
        {
            return origs.Where(orig => GetClonePolicy(orig) >= ClonePolicy.KeepReference).Select(orig => CloneStateMachineBehaviourInternal(orig)).ToImmutableList();
        }

        private StateMachineBehaviour CloneStateMachineBehaviourInternal(StateMachineBehaviour orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, () => (StateMachineBehaviour)ScriptableObject.CreateInstance(orig.GetType()), out StateMachineBehaviour clone);
            if (!isCreated) return clone;

            EditorUtility.CopySerialized(orig, clone);
            clone.hideFlags = orig.hideFlags;

            return clone;
        }

        /// <summary>
        /// VirtualClipをクローンします。orig.Clone()(NDMF標準の複製メソッド)を使い、カーブ・設定を完全コピーします。
        /// </summary>
        /// <param name="orig">クローン元のVirtualClip。</param>
        /// <returns>クローンされたVirtualClip。</returns>
        public VirtualClip CloneVirtualClip(VirtualClip orig) => CloneVirtualClip(orig, out _);

        /// <summary>
        /// VirtualClipをクローンします。orig.Clone()(NDMF標準の複製メソッド)を使い、カーブ・設定を完全コピーします。
        /// </summary>
        /// <param name="orig">クローン元のVirtualClip。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたVirtualClip。</returns>
        public VirtualClip CloneVirtualClip(VirtualClip orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualClipInternal, out clonedMap);

        private VirtualClip CloneVirtualClipInternal(VirtualClip orig)
        {
            TryGetOrCreateVirtualMotionCloneInstance(orig, out VirtualMotion clone);
            return (VirtualClip)clone;
        }

        /// <summary>
        /// VirtualBlendTreeをクローンします。Childrenを含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualBlendTree。</param>
        /// <returns>クローンされたVirtualBlendTree。</returns>
        public VirtualBlendTree CloneVirtualBlendTree(VirtualBlendTree orig) => CloneVirtualBlendTree(orig, out _);

        /// <summary>
        /// VirtualBlendTreeをクローンします。Childrenを含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualBlendTree。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたVirtualBlendTree。</returns>
        public VirtualBlendTree CloneVirtualBlendTree(VirtualBlendTree orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualBlendTreeInternal, out clonedMap);

        private VirtualBlendTree CloneVirtualBlendTreeInternal(VirtualBlendTree orig)
        {
            bool isCreated = TryGetOrCreateVirtualMotionCloneInstance(orig, out VirtualMotion motionClone);
            VirtualBlendTree clone = (VirtualBlendTree)motionClone;
            if (!isCreated) return clone;

            clone.BlendParameter = orig.BlendParameter;
            clone.BlendParameterY = orig.BlendParameterY;
            clone.BlendType = orig.BlendType;
            clone.Children = CloneVirtualChildMotionsInternal(orig.Children);
            clone.MaxThreshold = orig.MaxThreshold;
            clone.MinThreshold = orig.MinThreshold;
            clone.UseAutomaticThresholds = orig.UseAutomaticThresholds;

            return clone;
        }

        // 生API版のAnimatorState.motion/BlendTree.childrenのクローン(clone.motion = orig.motion switch
        // { AnimationClip => CloneAnimationClipInternal(...), BlendTree => CloneBlendTreeInternal(...), ... })
        // は常に「型ごとの完全クローンメソッド」を呼んでいた。Virtual API版でもこれを踏襲し、
        // TryGetOrCreateVirtualMotionCloneInstance(シェル生成のみ、Children等は未設定)を直接使わず、
        // 型ごとの完全クローンメソッド(CloneVirtualClipInternal/CloneVirtualBlendTreeInternal)へ
        // 委譲する。VirtualClipはfactory(orig.Clone())が既に内容を完全コピーするため実質差はないが、
        // VirtualBlendTreeはCloneVirtualBlendTreeInternalを経由しないとChildren/BlendType等が
        // 一切コピーされないまま空のシェルが返ってしまう。
        private VirtualMotion CloneMotionInternal(VirtualMotion orig) => orig switch
        {
            VirtualClip origClip => CloneVirtualClipInternal(origClip),
            VirtualBlendTree origTree => CloneVirtualBlendTreeInternal(origTree),
            _ => orig,
        };

        /// <summary>
        /// 複数のVirtualChildMotionをまとめてクローンします。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualBlendTree.VirtualChildMotion> CloneVirtualChildMotions(IEnumerable<VirtualBlendTree.VirtualChildMotion> origs) => CloneVirtualChildMotions(origs, out _);

        /// <summary>
        /// 複数のVirtualChildMotionをまとめてクローンします。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた列挙。</returns>
        public IEnumerable<VirtualBlendTree.VirtualChildMotion> CloneVirtualChildMotions(IEnumerable<VirtualBlendTree.VirtualChildMotion> origs, out Dictionary<object, object> clonedMap) => CloneWithMap(origs, CloneVirtualChildMotionsInternal, out clonedMap);

        /// <summary>
        /// VirtualChildMotionをクローンします。Motion(VirtualClip/VirtualBlendTree)を含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualChildMotion。</param>
        /// <returns>クローンされたVirtualChildMotion。</returns>
        public VirtualBlendTree.VirtualChildMotion CloneVirtualChildMotion(VirtualBlendTree.VirtualChildMotion orig) => CloneVirtualChildMotion(orig, out _);

        /// <summary>
        /// VirtualChildMotionをクローンします。Motion(VirtualClip/VirtualBlendTree)を含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のVirtualChildMotion。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたVirtualChildMotion。</returns>
        public VirtualBlendTree.VirtualChildMotion CloneVirtualChildMotion(VirtualBlendTree.VirtualChildMotion orig, out Dictionary<object, object> clonedMap) => CloneWithMap(orig, CloneVirtualChildMotionInternal, out clonedMap);

        private ImmutableList<VirtualBlendTree.VirtualChildMotion> CloneVirtualChildMotionsInternal(IEnumerable<VirtualBlendTree.VirtualChildMotion> origs)
        {
            return origs.Select(orig => CloneVirtualChildMotionInternal(orig)).ToImmutableList();
        }

        private VirtualBlendTree.VirtualChildMotion CloneVirtualChildMotionInternal(VirtualBlendTree.VirtualChildMotion orig)
        {
            return new VirtualBlendTree.VirtualChildMotion
            {
                Motion = CloneMotionInternal(orig.Motion),
                CycleOffset = orig.CycleOffset,
                DirectBlendParameter = orig.DirectBlendParameter,
                Mirror = orig.Mirror,
                Threshold = orig.Threshold,
                Position = orig.Position,
                TimeScale = orig.TimeScale
            };
        }

        private void ThrowIfKeepReferenceChildren(IEnumerable<object> objs)
        {
            foreach (object obj in objs)
            {
                if (obj != null && GetClonePolicy(obj) == ClonePolicy.KeepReference)
                {
                    string name = obj switch
                    {
                        VirtualNode node => node.Name,
                        UnityEngine.Object unityObj => unityObj.name,
                        _ => obj.ToString()
                    };
                    throw new InvalidOperationException(
                        $"親がCloneのオブジェクトの子に、KeepReferenceが設定されています。" +
                        $"対象: {name} ({obj.GetType().Name})");
                }
            }
        }

        private bool TryGetOrCreateCloneInstance<T>(T orig, Func<T> factory, out T clone) where T : class
        {
            if (orig == null)
            {
                clone = default;
                return false;
            }
            if (_cloneMap.TryGetValue(orig, out object cached) && cached is T tCached)
            {
                clone = tCached;
                return false;
            }

            ClonePolicy policy = GetClonePolicy(orig);

            return TryGetOrCreateCloneInstanceInternal(orig, factory, out clone, policy);
        }

        private bool TryGetOrCreateVirtualMotionCloneInstance(VirtualMotion orig, out VirtualMotion clone)
        {
            if (orig == null)
            {
                clone = default;
                return false;
            }
            if (_cloneMap.TryGetValue(orig, out object cached) && cached is VirtualMotion tCached)
            {
                clone = tCached;
                return false;
            }

            // 通常のGetClonePolicy(親を辿る継承)ではなく_policyMapの直接エントリのみを見る。
            // Motionは RegisterChildrenRecursively の対象外(親子関係が_parentMapに登録されない)
            // であるため、継承チェーンに乗らない。UnSetting(未設定)の場合、DefaultPolicyが
            // Clone/KeepReferenceならそれを使い、Detach/UnSettingならKeepReferenceに自動昇格する
            // (Detachにはならない)。これは明示的に指定しない限りMotionが誤ってnull化されない
            // ための安全設計(生API版TryGetOrCreateMotionCloneInstanceを参照)。
            _policyMap.TryGetValue(orig, out ClonePolicy policy);
            ClonePolicy resolvedPolicy = policy switch
            {
                ClonePolicy.Clone or ClonePolicy.KeepReference or ClonePolicy.Detach => policy,
                _ => DefaultPolicy switch
                {
                    ClonePolicy.Clone or ClonePolicy.KeepReference => DefaultPolicy,
                    _ => ClonePolicy.KeepReference
                }
            };

            // VirtualMotionは抽象クラスのため、具象型ごとにTryGetOrCreateCloneInstanceInternal<T>へ振り分ける。
            // VirtualClip/VirtualBlendTreeはVirtualMotionの唯一の具象型。
            switch (orig)
            {
                case VirtualClip clipOrig:
                    // VirtualClip.Create(name)は空クリップしか作れないため、orig.Clone()
                    // (NDMF標準の複製メソッド、カーブ・設定を完全コピー)をfactoryにする。
                    bool clipCreated = TryGetOrCreateCloneInstanceInternal(clipOrig, () => clipOrig.Clone(), out VirtualClip clipClone, resolvedPolicy);
                    if (clipCreated) clipClone.Name = GetCloneObjName(clipOrig.Name);
                    clone = clipClone;
                    return clipCreated;
                case VirtualBlendTree treeOrig:
                    bool treeCreated = TryGetOrCreateCloneInstanceInternal(treeOrig, () => VirtualBlendTree.Create(GetCloneObjName(treeOrig.Name)), out VirtualBlendTree treeClone, resolvedPolicy);
                    clone = treeClone;
                    return treeCreated;
                default:
                    throw new InvalidOperationException($"未対応のMotion派生型です: {orig.GetType().FullName}");
            }
        }

        private bool TryGetOrCreateCloneInstanceInternal<T>(T orig, Func<T> factory, out T clone, ClonePolicy policy) where T : class
        {
            switch (policy)
            {
                case ClonePolicy.Clone:
                    clone = factory();
                    _cloneMap[orig] = _cloneMap[clone] = clone;
                    return true;

                case ClonePolicy.KeepReference:
                    _cloneMap[orig] = clone = orig;
                    return false;

                case ClonePolicy.Detach:
                default:
                    _cloneMap[orig] = clone = default;
                    return false;

                case ClonePolicy.UnSetting:
                    throw new InvalidOperationException("ClonePolicyが未設定のオブジェクトをクローンしようとしました");
            }
        }

        private string GetCloneObjName(string origName) => NameTransformer(origName);

        /// <summary>
        /// ClonePolicyの登録漏れを検出する
        /// </summary>
        public IReadOnlyCollection<InvalidEntry> ValidateRegistration(object target) => ValidateRegistrationInternal(target);

        /// <summary>
        /// ClonePolicyの登録漏れを検出する
        /// </summary>
        public IReadOnlyCollection<InvalidEntry> ValidateRegistrations(IEnumerable<object> targets) => targets.SelectMany(t => ValidateRegistration(t)).ToHashSet();

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationInternal(object target)
        {
            if (target == null)
            {
                return new List<InvalidEntry>();
            }

            HashSet<object> visitedObjSet = new();

            return ValidateRegistrationDispatch(target, null, "", ref visitedObjSet);
        }

        // VirtualAnimatorGraphSchema.GetChildrenが列挙した子要素を、実際の型に応じて対応する
        // ValidateRegistrationXxxへ振り分ける。トップレベルのValidateRegistrationInternalと、
        // 各ノードの子要素再帰の両方から使う共通の入口。
        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationDispatch(object target, object parent, string memberName, ref HashSet<object> visitedObjSet) => target switch
        {
            null => Array.Empty<InvalidEntry>(),
            VirtualAnimatorController castedObj => ValidateRegistrationVirtualAnimatorController(castedObj, parent, memberName, ref visitedObjSet),
            VirtualLayer castedObj => ValidateRegistrationVirtualLayer(castedObj, parent, memberName, ref visitedObjSet),
            VirtualStateMachine.VirtualChildStateMachine castedObj => ValidateRegistrationVirtualChildStateMachine(castedObj, parent, memberName, ref visitedObjSet),
            VirtualStateMachine castedObj => ValidateRegistrationVirtualStateMachine(castedObj, parent, memberName, ref visitedObjSet),
            VirtualStateMachine.VirtualChildState castedObj => ValidateRegistrationVirtualChildState(castedObj, parent, memberName, ref visitedObjSet),
            VirtualState castedObj => ValidateRegistrationVirtualState(castedObj, parent, memberName, ref visitedObjSet),
            VirtualTransition castedObj => ValidateRegistrationVirtualTransition(castedObj, parent, memberName, ref visitedObjSet),
            VirtualStateTransition castedObj => ValidateRegistrationVirtualStateTransition(castedObj, parent, memberName, ref visitedObjSet),
            StateMachineBehaviour castedObj => ValidateRegistrationStateMachineBehaviour(castedObj, parent, memberName, ref visitedObjSet),
            VirtualClip castedObj => ValidateRegistrationVirtualClip(castedObj, parent, memberName, ref visitedObjSet),
            VirtualBlendTree castedObj => ValidateRegistrationVirtualBlendTree(castedObj, parent, memberName, ref visitedObjSet),
            _ => Array.Empty<InvalidEntry>(),
        };

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualAnimatorController(VirtualAnimatorController target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return entries;
        }

        // 複数形の一括検証版。内部の再帰からは呼ばれないが、既存の利用者(テスト等)向けにinternalとして残す。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualLayers(IEnumerable<VirtualLayer> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (VirtualLayer target in targets)
            {
                entries.UnionWith(ValidateRegistrationVirtualLayer(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualLayer(VirtualLayer target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationVirtualLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualChildStateMachines(IEnumerable<VirtualStateMachine.VirtualChildStateMachine> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (VirtualStateMachine.VirtualChildStateMachine target in targets)
            {
                entries.UnionWith(ValidateRegistrationVirtualChildStateMachine(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualChildStateMachine(VirtualStateMachine.VirtualChildStateMachine target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            return ValidateRegistrationVirtualStateMachine(target.StateMachine, parent, $"{memberName}.{nameof(target.StateMachine)}", ref visitedObjSet);
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualStateMachine(VirtualStateMachine target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();

            // Policy未登録の検証
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            // 不正なPolicy登録(親Clone、子KeepReference)の検証
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.States.Where(x => x.State != null).Select(x => (object)x.State), target, nameof(target.States)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.StateMachines.Select(x => (object)x.StateMachine), target, nameof(target.StateMachines)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.EntryTransitions, target, nameof(target.EntryTransitions)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.AnyStateTransitions, target, nameof(target.AnyStateTransitions)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.Behaviours, target, nameof(target.Behaviours)));
            foreach (VirtualStateMachine.VirtualChildStateMachine curCSM in target.StateMachines)
            {
                if (curCSM.StateMachine != null && target.StateMachineTransitions.TryGetValue(curCSM.StateMachine, out ImmutableList<VirtualTransition> transitions))
                {
                    entries.UnionWith(ValidateKeepReferenceChildRegistrations(transitions, target, $"StateMachineTransitions()[{curCSM.StateMachine?.Name}]"));
                }
            }

            return entries;
        }

        /// <summary>
        /// 不正なPolicy登録(親Clone、子KeepReference)の検証
        /// </summary>
        private IReadOnlyCollection<InvalidEntry> ValidateKeepReferenceChildRegistrations(IEnumerable<object> targets, object parent, string memberName)
        {
            HashSet<InvalidEntry> entries = new();
            int i = 0;
            foreach (object curObj in targets)
            {
                // 手動設定で子がKeepReferenceになっていないか確認
                if (curObj != null && GetClonePolicy(curObj) == ClonePolicy.KeepReference)
                {
                    entries.Add(new(InvalidType.KeepReferenceChild, curObj, parent, $"{memberName}[{i}]"));
                }
                i++;
            }
            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationVirtualLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualChildStates(IEnumerable<VirtualStateMachine.VirtualChildState> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (VirtualStateMachine.VirtualChildState target in targets)
            {
                entries.UnionWith(ValidateRegistrationVirtualChildState(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualChildState(VirtualStateMachine.VirtualChildState target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            // 生API版のChildAnimatorState.stateと異なりState(VirtualState?)はnullableなため、
            // nullの場合は検証をスキップする(エラーとしない)。
            if (target.State == null) return Array.Empty<InvalidEntry>();
            return ValidateRegistrationVirtualState(target.State, parent, $"{memberName}.{nameof(target.State)}", ref visitedObjSet);
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualState(VirtualState target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.Transitions, target, nameof(target.Transitions)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.Behaviours, target, nameof(target.Behaviours)));

            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationVirtualLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualTransitions(IEnumerable<VirtualTransition> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (VirtualTransition target in targets)
            {
                entries.UnionWith(ValidateRegistrationVirtualTransition(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualTransition(VirtualTransition target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationVirtualLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualStateTransitions(IEnumerable<VirtualStateTransition> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (VirtualStateTransition target in targets)
            {
                entries.UnionWith(ValidateRegistrationVirtualStateTransition(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualStateTransition(VirtualStateTransition target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in VirtualAnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationVirtualLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationStateMachineBehaviours(IEnumerable<StateMachineBehaviour> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (StateMachineBehaviour target in targets)
            {
                entries.UnionWith(ValidateRegistrationStateMachineBehaviour(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationStateMachineBehaviour(StateMachineBehaviour target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            return Array.Empty<InvalidEntry>();
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualClip(VirtualClip target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            return Array.Empty<InvalidEntry>();
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualBlendTree(VirtualBlendTree target, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            entries.UnionWith(ValidateRegistrationVirtualChildMotions(target.Children, target, nameof(target.Children), ref visitedObjSet));
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualChildMotions(IEnumerable<VirtualBlendTree.VirtualChildMotion> targets, object parent, string memberName, ref HashSet<object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (VirtualBlendTree.VirtualChildMotion target in targets)
            {
                entries.UnionWith(ValidateRegistrationVirtualChildMotion(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationVirtualChildMotion(VirtualBlendTree.VirtualChildMotion target, object parent, string memberName, ref HashSet<object> visitedObjSet) => target.Motion switch
        {
            VirtualClip clip => ValidateRegistrationVirtualClip(clip, parent, $"{memberName}.{nameof(target.Motion)}", ref visitedObjSet),
            VirtualBlendTree tree => ValidateRegistrationVirtualBlendTree(tree, parent, $"{memberName}.{nameof(target.Motion)}", ref visitedObjSet),
            _ => Array.Empty<InvalidEntry>(),
        };

        private bool ValidateAndCreateUnregisteredEntry(object target, object parent, string memberName, out InvalidEntry entry, out ClonePolicy policy)
        {
            entry = null;
            policy = default;
            // DefaultPolicyを加味したPolicy設定を確認
            if (target == null || (policy = GetClonePolicy(target)) != ClonePolicy.UnSetting) return true;
            entry = new(InvalidType.UnregisteredEntry, target, parent, memberName);
            return false;
        }

        /// <summary>
        /// ValidateRegistration/ValidateRegistrationsで検出された、ClonePolicyの登録に関する問題1件を表します。
        /// </summary>
        public record InvalidEntry
        {
            /// <summary>問題の種別を取得します。</summary>
            public InvalidType InvalidType { get; }
            /// <summary>問題のあるオブジェクトを取得します。</summary>
            public object InvalidEntryObject { get; }
            /// <summary>InvalidEntryObjectを参照していた親オブジェクトを取得します。</summary>
            public object ReferencedFrom { get; }
            /// <summary>InvalidEntryObjectが参照されていたメンバー名を取得します。</summary>
            public string MemberName { get; }

            /// <summary>
            /// InvalidEntryの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="invalidType">問題の種別。</param>
            /// <param name="invalidEntryObject">問題のあるオブジェクト。</param>
            /// <param name="referencedFrom">invalidEntryObjectを参照していた親オブジェクト。</param>
            /// <param name="memberName">invalidEntryObjectが参照されていたメンバー名。</param>
            public InvalidEntry(InvalidType invalidType, object invalidEntryObject, object referencedFrom, string memberName)
            {
                InvalidType = invalidType;
                InvalidEntryObject = invalidEntryObject;
                ReferencedFrom = referencedFrom;
                MemberName = memberName;
            }
        }

        /// <summary>
        /// ValidateRegistrationで検出される、ClonePolicy登録に関する問題の種別です。
        /// </summary>
        public enum InvalidType
        {
            /// <summary>ClonePolicyが一切登録されていない(UnSettingのままの)オブジェクトが見つかった場合。</summary>
            UnregisteredEntry,
            /// <summary>親にCloneが設定されているにもかかわらず、子にKeepReferenceが設定されている場合。</summary>
            KeepReferenceChild
        }

        /// <summary>
        /// 値が大きいほど優先度が高い。
        /// SetPolicyIfAbsentは現在の設定より低い優先度のポリシーを無視する。
        /// 新しいポリシーを追加する際は優先度順に並べること。
        /// </summary>
        public enum ClonePolicy
        {
            /// <summary>
            /// 未設定(このポリシーのオブジェクトをクローンしようとした場合、例外を吐く)。
            /// ただしVirtualMotion(VirtualClip/VirtualBlendTree)は例外で、UnSettingのままでも例外にはならず、
            /// DefaultPolicyに基づき自動的にKeepReference以上へ昇格する(詳細はTryGetOrCreateVirtualMotionCloneInstance参照)。
            /// </summary>
            UnSetting,
            /// <summary>nullとして扱う(切り離す)</summary>
            Detach,
            /// <summary>元のオブジェクトへの参照を保持する</summary>
            KeepReference,
            /// <summary>クローンを生成する</summary>
            Clone,
        }
    }
}
