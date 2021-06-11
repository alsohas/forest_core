using System;
using System.Collections.Generic;
using KDBush;

namespace forest_core
{
    [Serializable]
    public class Node : Point<UInt16>
    {
        public Dictionary<Node, Edge> IncomingEdges;

        [NonSerialized] public Coordinate Location;

        public UInt16 NodeID;
        public Dictionary<Node, Edge> OutgoingEdges;

        public Node(UInt16 nodeID, double lng, double lat) : base(lng, lat, nodeID)
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

        public double GetDistanceTo(Node other)
        {
                double latitude = this.Location.Latitude * 0.0174532925199433;
                double longitude = this.Location.Longitude * 0.0174532925199433;
                double num = other.Location.Latitude * 0.0174532925199433;
                double longitude1 = other.Location.Longitude * 0.0174532925199433;
                double num1 = longitude1 - longitude;
                double num2 = num - latitude;
                double num3 = Math.Pow(Math.Sin(num2 / 2), 2) + Math.Cos(latitude) * Math.Cos(num) * Math.Pow(Math.Sin(num1 / 2), 2);
                double num4 = 2 * Math.Atan2(Math.Sqrt(num3), Math.Sqrt(1 - num3));
                double num5 = 6376500 * num4;
                return num5;
        }
    }
}