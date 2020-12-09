using forest_core.PredictionModels;
using KDBush;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace forest_core
{
    [Serializable]
    public class RoadNetwork
    {
        public Dictionary<(int, int), Edge> Edges;
        private KDBush<int> Index;
        public bool IsBuilt;
        public Dictionary<int, Node> Nodes;

        public RoadNetwork()
        {
            ReverseMappedDictionary = DictGenerator.LoadReverseMappedDict();
            Nodes = new Dictionary<int, Node>();
            Edges = new Dictionary<(int, int), Edge>();
        }

        public Dictionary<long, int> ReverseMappedDictionary { get; set; }

        /// <summary>
        ///     Find nearest neighbors within radius (in meters) around the given center.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius">Radius in meters</param>
        internal IEnumerable<Node> GetNodesWithinRange(Coordinate center, double radius)
        {
            // convert to KM
            radius = radius / 1000;
            var r = 6378.1; // radius of earth
            var bearings = new[] { ToRadian(0), ToRadian(90), ToRadian(180), ToRadian(270) };
            var bbox = new List<Coordinate>();
            var originLat = ToRadian(center.Latitude);
            var originLng = ToRadian(center.Longitude);

            foreach (var bearing in bearings)
            {
                var lat = Math.Asin(Math.Sin(originLat) * Math.Cos(radius / r) +
                                    Math.Cos(originLat) * Math.Sin(radius / r) * Math.Cos(bearing));
                var lng = originLng + Math.Atan2(Math.Sin(bearing) * Math.Sin(radius / r) * Math.Cos(originLat),
                    Math.Cos(radius / r) - Math.Sin(originLat) * Math.Sin(lat));
                var destination = new Coordinate(ToDegrees(lng), ToDegrees(lat));
                bbox.Add(destination);
            }

            var maxLng = double.MinValue;
            var minLng = double.MaxValue;
            var maxLat = double.MinValue;
            var minLat = double.MaxValue;

            foreach (var coordinate in bbox)
            {
                if (maxLng < coordinate.Longitude) maxLng = coordinate.Longitude;
                if (maxLat < coordinate.Latitude) maxLat = coordinate.Latitude;
                if (minLng > coordinate.Longitude) minLng = coordinate.Longitude;
                if (minLat > coordinate.Latitude) minLat = coordinate.Latitude;
            }

            var nodesInRange = Index.Query(minLng, minLat, maxLng, maxLat);
            return nodesInRange.Cast<Node>().ToList();
        }

        private double ToRadian(double n)
        {
            return Math.PI / 180 * n;
        }

        private double ToDegrees(double n)
        {
            return 180 / Math.PI * n;
        }


        private void BuildIndex()
        {
            Index = new KDBush<int>();
            Index.Index(new List<Node>(Nodes.Values));
        }

        public void BuildNetwork()
        {
            if (IsBuilt) return;
            InitializeNodes();
            InitializeEdges();
            BuildRoadNetwork();
            BuildIndex();
            IsBuilt = true;
            Console.WriteLine("Finished building network.");
        }

        private void BuildRoadNetwork()
        {
            foreach (var kv in Edges)
            {
                var edge = kv.Value;
                var sourceNode = edge.Source;
                var destinationNode = edge.Destination;
                sourceNode.AddOutgoingEdge(destinationNode, edge);
                destinationNode.AddIncomingEdge(sourceNode, edge);
            }
        }

        private void InitializeEdges()
        {
            var jsonString = File.ReadAllText(Parameters.EdgesFile);
            var edges = JArray.Parse(jsonString);
            var count = 0;
            var notFound = 0;
            foreach (dynamic edge in edges)
            {
                count++;
                string sourceString = edge[0];
                string destinationString = edge[1];
                int source;
                int destination;
                try
                {
                    source = GetMappedID(sourceString, ReverseMappedDictionary);
                    destination = GetMappedID(destinationString, ReverseMappedDictionary);
                }
                catch (Exception)
                {
                    notFound++;
                    continue;
                }

                var sourceExists = Nodes.TryGetValue(source, out var sourceNode);
                var destinationExists = Nodes.TryGetValue(destination, out var destinationNode);

                if (!sourceExists || !destinationExists) continue;
                if (!Edges.ContainsKey((source, destination)))
                    Edges.Add((source, destination), new Edge(sourceNode, destinationNode, 1, 1));
            }

            Console.WriteLine($"Read {count} edges.");
            Console.WriteLine($"Missed {notFound} edges.");
        }

        public static int GetMappedID(string nodeIdString, Dictionary<long, int> rmd)
        {
            return rmd[long.Parse(nodeIdString)];
        }

        private void InitializeNodes()
        {
            var jsonString = File.ReadAllText(Parameters.NodesFile);
            var count = 0;
            var notFound = 0;
            var nodes = JArray.Parse(jsonString);
            foreach (dynamic node in nodes)
            {
                count++;
                int id;
                try
                {
                    id = GetMappedID(node.id.ToString(), ReverseMappedDictionary);
                }
                catch (Exception)
                {
                    notFound++;
                    continue;
                }

                double lng = node.coordinate.lng;
                double lat = node.coordinate.lat;
                var parsed_node = new Node(id, lng, lat);
                Nodes.Add(id, parsed_node);
            }

            Console.WriteLine($"Read {count} nodes.");
            Console.WriteLine($"Missed {notFound} nodes.");
        }
    }
}