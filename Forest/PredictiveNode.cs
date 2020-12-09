using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace forest_core.Forest
{
    [Serializable]
    public class PredictiveNode
    {
        private Region MRegion;
        public Node Parent;
        public double Probability;

        public PredictiveNode(RoadNetwork roadNetwork, Node root, double cost, int depth, int maxDepth, Node parent,
            ConcurrentDictionary<int, ConcurrentDictionary<int, List<PredictiveNode>>> predictiveRegions,
            Region region)
        {
            if (parent != null) Parent = parent;
            MRegion = region;
            RoadNetwork = roadNetwork;
            Root = root;
            Cost = cost;
            Depth = depth;
            MaxDepth = maxDepth;
            Level = MaxDepth - Depth;

            PredictiveRegions = predictiveRegions;
            Children = new Dictionary<int, PredictiveNode>();

            AddRegionReference();
        }

        public RoadNetwork RoadNetwork { get; }
        public Node Root { get; private set; }
        public double Cost { get; }
        public int Depth { get; private set; }
        public int MaxDepth { get; }
        public int Level { get; }
        public ConcurrentDictionary<int, ConcurrentDictionary<int, List<PredictiveNode>>> PredictiveRegions { get; }
        public Dictionary<int, PredictiveNode> Children { get; private set; }

        private void AddRegionReference()
        {
            PredictiveRegions.TryGetValue(Level, out var region);
            if (region == null)
            {
                region = new ConcurrentDictionary<int, List<PredictiveNode>>();
                PredictiveRegions.TryAdd(Level, region);
            }

            region.TryGetValue(Root.NodeID, out var nodeList);
            if (nodeList == null)
            {
                nodeList = new List<PredictiveNode>();
                region.TryAdd(Root.NodeID, nodeList);
            }

            lock (nodeList)
            {
                nodeList.Add(this);
            }
        }

        public void Expand()
        {
            if (Depth == 0 || Level == MaxDepth) return;

            var taskList = new List<Task>();

            foreach (var kvPair in Root.OutgoingEdges)
            {
                var node = kvPair.Key;
                if (MRegion.ObsoleteNodes.Contains(node.NodeID)) continue;
                if (Parent != null && Parent.NodeID == node.NodeID) // avoid cyclic relations
                    continue;

                var child = new PredictiveNode(RoadNetwork, node, kvPair.Value.Cost, Depth - 1,
                    MaxDepth, Root, PredictiveRegions, MRegion);
                var distance = kvPair.Value.Distance;

                Children.Add(child.Root.NodeID, child);
                var task = Task.Factory.StartNew(() => child.Expand());
                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());
        }
    }
}