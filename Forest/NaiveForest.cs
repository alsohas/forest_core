using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace forest_core.Forest
{
    internal class NaiveForest
    {
        public int CurrentStep;

        [NonSerialized] private readonly List<Node> Locations;

        private readonly Region MRegion;

        public ConcurrentDictionary<int, ConcurrentDictionary<UInt16, List<PredictiveNode>>> PredictiveRegions;

        [NonSerialized] private RoadNetwork roadNetwork;

        public NaiveForest(RoadNetwork roadNetwork, int depth)
        {
            SetRoadNetwork(roadNetwork);
            Depth = depth;
        }

        public NaiveForest(RoadNetwork roadNetwork, int depth, List<Node> locations)
        {
            SetRoadNetwork(roadNetwork);
            Depth = depth;
            Locations = locations;
            MRegion = new Region();
        }

        public int Depth { get; }

        public void Update(double radius)
        {
            Update(Locations[CurrentStep].Location, radius);
        }

        public void Update(Coordinate center, double radius)
        {
            // gathering new nodes within latest region
            var currentNodes = new HashSet<UInt16>();
            foreach (var n in GetRoadNetwork().GetNodesWithinRange(center, radius)) currentNodes.Add(n.NodeID);

            ExpandPredictiveTrees(currentNodes); // Populate and expand predictive trees for new region
            CurrentStep += 1; // increment how many steps we've received updates from
        }

        private void ExpandPredictiveTrees(HashSet<UInt16> newRegion)
        {
            PredictiveRegions = new ConcurrentDictionary<int, ConcurrentDictionary<UInt16, List<PredictiveNode>>>();
            foreach (var nodeID in newRegion)
            {
                GetRoadNetwork().Nodes.TryGetValue(nodeID, out var node);
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