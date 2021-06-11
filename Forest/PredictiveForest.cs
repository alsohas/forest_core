using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace forest_core.Forest
{
    [Serializable]
    public class PredictiveForest
    {
        public int CurrentStep;

        [NonSerialized] private List<Node> Locations;

        public ConcurrentDictionary<int, ConcurrentDictionary<UInt16, List<PredictiveNode>>> PredictiveRegions;

        [NonSerialized] private RoadNetwork roadNetwork;

        public PredictiveForest(RoadNetwork roadNetwork, int depth)
        {
            SetRoadNetwork(roadNetwork);
            Depth = depth;
            MRegion = new Region();
        }

        public PredictiveForest(RoadNetwork roadNetwork, int depth, List<Node> locations)
        {
            SetRoadNetwork(roadNetwork);
            Depth = depth;
            MRegion = new Region();
            Locations = locations;
        }

        public int Depth { get; private set; }
        public Region MRegion { get; private set; }

        public void Update(double radius)
        {
            Update(Locations[CurrentStep].Location, radius);
        }

        public void Update(Coordinate center, double radius)
        {
            if (CurrentStep == 0)
            {
                MRegion.Update(GetRoadNetwork().GetNodesWithinRange(center, radius));
                MRegion.Regions.TryGetValue(CurrentStep, out var region);
                ExpandPredictiveTrees(region);
                CurrentStep += 1;
                return;
            }

            // retrieve all nodes from previous region
            MRegion.Regions.TryGetValue(CurrentStep - 1, out var pastNodes);

            // gathering child nodes from previous region
            var children = new HashSet<UInt16>();
            foreach (var kv in pastNodes) children.UnionWith(kv.Value.Children);

            // gathering new nodes within latest region
            var currentNodes = new HashSet<UInt16>();
            foreach (var n in GetRoadNetwork().GetNodesWithinRange(center, radius)) currentNodes.Add(n.NodeID);

            // only keep the nodes from newest region which intersects the previous region's children
            currentNodes.IntersectWith(children);

            // pruning children from the nodes of previous region
            // for each node in previous region, intersect its set of children with current nodes
            // if the resulting set is empty, the previous node is obsolete
            var obsoleteParents = new HashSet<UInt16>();
            var validParents = new HashSet<UInt16>();
            foreach (var kv in pastNodes)
            {
                var pastNode = kv.Value;
                pastNode.Children.IntersectWith(currentNodes);
                if (pastNode.Children.Count == 0)
                {
                    obsoleteParents.Add(pastNode.NodeID);
                    continue;
                }

                validParents.Add(pastNode.NodeID);
            }

            // adding all valid nodes to the latest region
            // Note: the first region is initialized in <see cref="Region.Update(IEnumerable{Node})"/>
            //
            var newRegion = new ConcurrentDictionary<UInt16, RegionalNode>();
            foreach (var nodeID in currentNodes) // note that current node has been cleared of all dead-end nodes
            {
                GetRoadNetwork().Nodes.TryGetValue(nodeID, out var node);
                var currentNode = new RegionalNode(node, validParents);
                newRegion.TryAdd(nodeID, currentNode);
            }

            // add newest region to the Region buffer
            // make sure the new region is added to buffer before pruning obsolete parents
            // because the pruning function needs reference to this new region
            MRegion.Regions.TryAdd(CurrentStep, newRegion);
            PruneRegions(CurrentStep - 1, obsoleteParents);


            ExpandPredictiveTrees(newRegion); // Populate and expand predictive trees for new region
            CurrentStep += 1; // increment how many steps we've received updates from
        }

        /// <summary>
        ///     Prunes nodes from the previous region, if at the top most region, removes nodes entirely
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="obsoleteNodes"></param>
        /// <returns></returns>
        private int PruneRegions(int steps, HashSet<UInt16> obsoleteNodes)
        {
            MRegion.ObsoleteNodes.UnionWith(obsoleteNodes);
            if (obsoleteNodes.Count == 0) return steps;

            var obsoleteParents = new HashSet<UInt16>();

            MRegion.Regions.TryGetValue(steps, out var region); // get all nodes from the specified region
            MRegion.Regions.TryGetValue(steps - 1, out var parentalRegion); // get all nodes from parental region

            foreach (var nodeID in obsoleteNodes)
            {
                region.TryGetValue(nodeID, out var node);
                region.TryRemove(nodeID, out _); // remove the node from region after getting its parents
                var parents = node.Parents;
                if (parents == null) continue;
                foreach (var pNodeID in parents)
                {
                    parentalRegion.TryGetValue(pNodeID, out var parent);
                    parent.Children.Remove(nodeID);
                    if (parent.Children.Count == 0) obsoleteParents.Add(pNodeID);
                }
            }

            return PruneRegions(steps - 1, obsoleteParents);
        }

        private void ExpandPredictiveTrees(ConcurrentDictionary<UInt16, RegionalNode> newRegion)
        {
            PredictiveRegions = new ConcurrentDictionary<int, ConcurrentDictionary<UInt16, List<PredictiveNode>>>();
            foreach (var kv in newRegion)
            {
                if (MRegion.ObsoleteNodes.Contains(kv.Value.NodeID)) continue;
                GetRoadNetwork().Nodes.TryGetValue(kv.Value.NodeID, out var node);
                var predictiveNode = new PredictiveNode(GetRoadNetwork(), node, 0, Depth, Depth, null,
                    PredictiveRegions, MRegion);
                predictiveNode.Expand();
            }
        }

        public RoadNetwork GetRoadNetwork()
        {
            return roadNetwork;
        }

        private void SetRoadNetwork(RoadNetwork value)
        {
            roadNetwork = value;
        }
    }
}