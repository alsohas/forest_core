using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace forest_core.MovingObject
{
    internal class TripsLoader
    {
        private readonly RoadNetwork RN;
        private List<string> TripFiles;
        private readonly string[] TripFolders;
        public ConcurrentStack<List<Node>> AllTripNodes;

        public TripsLoader(RoadNetwork rn)
        {
            TripFolders = Directory.GetDirectories(Parameters.TripsFolder);
            TripFiles = new List<string>();
            AllTripNodes = new ConcurrentStack<List<Node>>();
            RN = rn;
        }

        public void LoadTrips()
        {
            var stopwatch = Stopwatch.StartNew();
            foreach (var tripFolder in TripFolders)
                foreach (var tripFile in Directory.GetFiles(tripFolder))
                    TripFiles.Add(tripFile);
            TripFiles = TripFiles.ToArray()[1..800].ToList();
            foreach (var tripFile in TripFiles)
            {
                var content = File.ReadAllText(tripFile);
                var trips = JArray.Parse(content);
                foreach (var trip in trips)
                {
                    var tripNodes = new List<Node>();
                    foreach (dynamic point in trip)
                    {
                        var pointIntID = RoadNetwork.GetMappedID(point.ToString(), RN.ReverseMappedDictionary);
                        var exists = RN.Nodes.TryGetValue(pointIntID, out Node node);
                        // if the next node is not in the road network
                        // simply break and use what you have
                        if (!exists) break;
                        tripNodes.Add(node);
                    }

                    AllTripNodes.Push(tripNodes);
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Elapsed time to read trip: {stopwatch.ElapsedMilliseconds}");
            Console.WriteLine($"Read {AllTripNodes.Count} trips");
        }
    }
}