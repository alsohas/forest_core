using System;

namespace forest_core
{
    [Serializable]
    public class Edge
    {
        public double Cost;
        public Node Destination;
        public double Distance;
        public Node Source;


        public Edge(Node source, Node destination, double cost, double distance)
        {
            Source = source;
            Destination = destination;
            Cost = cost;
            Distance = distance;
        }
    }
}