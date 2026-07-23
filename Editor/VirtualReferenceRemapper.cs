using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor
{
    /// <summary>
    /// クローンされたオブジェクトから複製元オブジェクトを逆引きできるようにし、複製後に新規生成されたVirtualClip/VirtualBlendTreeへの
    /// 参照を、名前と複製元のルートオブジェクトを手がかりに一括で付け替えるためのクラスです。
    /// </summary>
    public class VirtualReferenceRemapper
    {
        private readonly Dictionary<object, object> _cloneToOrigMap = new();

        /// <summary>
        /// VirtualAnimatorCloner.GetClonedMap()等で得られる複製元→複製後のマップを、逆方向(複製後→複製元)で登録します。
        /// </summary>
        /// <param name="orig2CloneMap">複製元オブジェクトをキー、複製後オブジェクトを値とするマップ。</param>
        public void AddClonedMap(IReadOnlyDictionary<object, object> orig2CloneMap)
        {
            foreach (KeyValuePair<object, object> pair in orig2CloneMap)
            {
                _cloneToOrigMap[pair.Value] = pair.Key;
            }
        }

        /// <summary>
        /// 登録済みの複製後→複製元マップのコピーを取得します。
        /// </summary>
        /// <returns>複製後オブジェクトをキー、複製元オブジェクトを値とするマップ。</returns>
        public Dictionary<object, object> GetAllClonedMap() => new(_cloneToOrigMap);

        /// <summary>
        /// 登録済みマップを複製元方向へ辿り、指定したオブジェクトの最も根本にある複製元オブジェクトを取得します。
        /// </summary>
        /// <param name="obj">辿り始めるオブジェクト。</param>
        /// <returns>最終的に辿り着いた複製元オブジェクト。マップに登録がない場合はobj自身を返します。</returns>
        public object GetOrigRoot(object obj)
        {
            HashSet<object> visited = new();
            object current = obj;
            while (_cloneToOrigMap.TryGetValue(current, out object origObj))
            {
                if (!visited.Add(current))
                {
                    // 循環を検出。無限ループを避けるため、循環に入る直前まで辿り着いたオブジェクトを返す
                    return current;
                }
                current = origObj;
            }
            return current;
        }

        /// <summary>
        /// 指定したオブジェクトが持つVirtualClip/VirtualBlendTree等の参照を再帰的に辿り、登録済みマップに基づいて新しいオブジェクトへ付け替えます。
        /// </summary>
        /// <param name="obj">参照の付け替えを行う対象オブジェクト。</param>
        public void RemappingRecursively(object obj) => RemappingRecursivelyInternal(obj, new RemapperContext());

        /// <summary>
        /// 複数のオブジェクトに対してまとめてRemappingRecursivelyを行います。処理コンテキストを共有するため、
        /// 対象オブジェクト間で同名の付け替え先が重複して生成されることを防ぎます。
        /// </summary>
        /// <param name="objs">参照の付け替えを行う対象オブジェクトの列挙。</param>
        public void RemappingRecursively(IEnumerable<object> objs)
        {
            RemapperContext context = new();
            foreach (object obj in objs)
            {
                RemappingRecursivelyInternal(obj, context);
            }
        }

        // 生API版はSerializedObjectで全プロパティを汎用走査するが、Virtual API側にはその手段がない
        // ため、Motion参照を持ちうる型(VirtualAnimatorController/VirtualLayer/VirtualStateMachine/
        // VirtualState)ごとに既知のプロパティを辿る実装に置き換える。
        private void RemappingRecursivelyInternal(object obj, RemapperContext context)
        {
            if (obj == null || context.RemappedObjs.Contains(obj)) return;
            context.RemappedObjs.Add(obj);

            switch (obj)
            {
                case VirtualAnimatorController controller:
                    foreach (VirtualLayer layer in controller.Layers)
                    {
                        RemappingRecursivelyInternal(layer, context);
                    }
                    break;

                case VirtualLayer layer:
                    RemappingRecursivelyInternal(layer.StateMachine, context);

                    if (layer.SyncedLayerMotionOverrides.Count > 0)
                    {
                        ImmutableDictionary<VirtualState, VirtualMotion>.Builder overridesBuilder = layer.SyncedLayerMotionOverrides.ToBuilder();
                        foreach (VirtualState key in layer.SyncedLayerMotionOverrides.Keys.ToList())
                        {
                            overridesBuilder[key] = RemapMotion(overridesBuilder[key], context);
                        }
                        layer.SyncedLayerMotionOverrides = overridesBuilder.ToImmutable();
                    }
                    break;

                case VirtualStateMachine stateMachine:
                    foreach (VirtualStateMachine.VirtualChildState childState in stateMachine.States)
                    {
                        RemappingRecursivelyInternal(childState.State, context);
                    }
                    foreach (VirtualStateMachine.VirtualChildStateMachine childStateMachine in stateMachine.StateMachines)
                    {
                        RemappingRecursivelyInternal(childStateMachine.StateMachine, context);
                    }
                    break;

                case VirtualState state:
                    state.Motion = RemapMotion(state.Motion, context);
                    break;
            }
        }

        // 指定したMotionが名前+複製元ルートで既にリマップ済みなら、その代表インスタンスへ差し替えて返す。
        // 未リマップならこのインスタンスを代表として登録し、VirtualBlendTreeの場合はさらにChildrenを
        // 再帰的に辿る(生API版のBlendTree再帰ロジックに対応)。
        private VirtualMotion RemapMotion(VirtualMotion motion, RemapperContext context)
        {
            if (motion == null) return null;

            object origRoot = GetOrigRoot(motion);
            if (context.Remap.TryGetValue((origRoot, motion.Name), out object remapObj))
            {
                return (VirtualMotion)remapObj;
            }

            context.Remap[(origRoot, motion.Name)] = motion;

            if (motion is VirtualBlendTree tree)
            {
                tree.Children = tree.Children.Select(child => new VirtualBlendTree.VirtualChildMotion
                {
                    Motion = RemapMotion(child.Motion, context),
                    CycleOffset = child.CycleOffset,
                    DirectBlendParameter = child.DirectBlendParameter,
                    Mirror = child.Mirror,
                    Threshold = child.Threshold,
                    Position = child.Position,
                    TimeScale = child.TimeScale
                }).ToImmutableList();
            }

            return motion;
        }

        private class RemapperContext
        {
            internal readonly HashSet<object> RemappedObjs = new();
            internal readonly Dictionary<(object OrigRoot, string Name), object> Remap = new();
        }
    }
}
