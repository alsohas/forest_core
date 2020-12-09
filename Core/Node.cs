using KDBush;
using System;
using System.Collections.Generic;

namespace forest_core
{
    [Serializable]
    public class Node : Point<int>
    {
        public Dictionary<Node, Edge> IncomingEdges;

        [NonSerialized] public Coordinate Location;

        public int NodeID;
        public Dictionary<Node, Edge> OutgoingEdges;

        public Node(int nodeID, double lng, double lat) : base(lng, lat, nodeID)
        {
            NodeID = nodeID;
            Location = new Coordinate(lng, lat);
            OutgoingEdges = new Dictionary<Node, Edge>();
            IncomingEdges = new Dictionary<Node, Edge>();
        }

        public void AddOutgoingEdge(Node node, Edge edge)
        {
            if (!OutgoingEdges.ContainsKey(node)) OutgoingEdges.Add(node, edge);
            if (node.IncomingEdges.ContainsKey(this)) return;
            node.AddIncomingEdge(this, edge);
        }

        public void AddIncomingEdge(Node node, Edge edge)
        {
            if (!IncomingEdges.ContainsKey(node)) IncomingEdges.Add(node, edge);
            if (node.OutgoingEdges.ContainsKey(this)) return;
            node.AddOutgoingEdge(this, edge);
        }

        public override string ToString()
        {
            return $"ID: {NodeID}"; //\tLongitude: {Location.Longitude}\tLatitude: {Location.Latitude}";
        }
    }
}