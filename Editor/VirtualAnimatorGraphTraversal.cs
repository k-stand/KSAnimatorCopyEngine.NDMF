using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf.animator;

namespace com.github.k_stand.ksanimatorclipboard.ndmf.editor
{
    internal static class VirtualAnimatorGraphTraversal
    {
        internal static HashSet<VirtualNode> ListupObjectsInLayer(VirtualLayer layer)
        {
            List<VirtualNode> containObjs = new();
            if (layer.StateMachine != null)
            {
                containObjs.Add(layer.StateMachine);
                containObjs.AddRange(ListupObjectsInStateMachine(layer.StateMachine));
            }
            containObjs.AddRange(layer.SyncedLayerMotionOverrides.Keys);
            containObjs.AddRange(layer.SyncedLayerBehaviourOverrides.Keys);

            return containObjs.ToHashSet();
        }

        internal static HashSet<VirtualNode> ListupObjectsInStateMachine(VirtualStateMachine stateMachine)
        {
            if (stateMachine == null) { return new(); }
            List<VirtualNode> containObjs = new();

            Queue<VirtualStateMachine> searchQueue = new();
            searchQueue.Enqueue(stateMachine);
            List<VirtualStateMachine> searchedList = new();
            while (searchQueue.Count > 0)
            {
                VirtualStateMachine curASM = searchQueue.Dequeue();

                containObjs.AddRange(curASM.EntryTransitions);
                containObjs.AddRange(curASM.AnyStateTransitions);

                List<VirtualState> states = curASM.States.Where(x => x.State != null).Select(x => x.State).ToList();
                containObjs.AddRange(states);
                containObjs.AddRange(states.SelectMany(x => x.Transitions));

                List<VirtualStateMachine> innerStateMachines = curASM.StateMachines.Select(x => x.StateMachine).Where(x => x != null).ToList();
                containObjs.AddRange(innerStateMachines);
                foreach (VirtualStateMachine innerStateMachine in innerStateMachines)
                {
                    if (innerStateMachine != null && curASM.StateMachineTransitions.TryGetValue(innerStateMachine, out var transitions))
                    {
                        containObjs.AddRange(transitions);
                    }
                }

                searchedList.Add(curASM);

                foreach (VirtualStateMachine item in innerStateMachines.Where(x => x != null && !searchedList.Contains(x)))
                {
                    searchQueue.Enqueue(item);
                }
            }

            return containObjs.ToHashSet();
        }
    }
}
