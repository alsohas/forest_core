using CsvHelper;
using forest_core.Forest;
using forest_core.MovingObject;
using ShellProgressBar;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace forest_core
{
    public class Result
    {
        public int step { get; set; }
        public int region_size { get; set; }
        public int historic_nodes { get; set; }
        public int predictive_nodes { get; set; }
        public double update_time { get; set; }
    }
    internal class Program
    {
        private static void Main(string[] args)
        {
            // generate dict mapping for all nodes
            //DictGenerator.GenerateDict();

            // generate dict mapped trip row structs with prior vectors
            //RowParser.Read();

            //load road network
            var rn = new RoadNetwork();
            rn.BuildNetwork();
            // try loading trips
            var tLoader = new TripsLoader(rn);
            tLoader.LoadTrips();
            List<dynamic> Results = new List<dynamic>();
            var maxStep = 5;
            var regionSizes = new int[] { 25, 50, 75, 100 };
            var options = new ProgressBarOptions
            {
                DisplayTimeInRealTime = false
            };
            using (var pbar = new ProgressBar(tLoader.AllTripNodes.Count, $"Processing trips", options))
            {
                var sw = Stopwatch.StartNew();
                while (!tLoader.AllTripNodes.IsEmpty)
                {
                    tLoader.AllTripNodes.TryPop(out var nodes);
                    {
                        foreach (var rSize in regionSizes)
                        {
                            {
                                for (int step = 1; step <= maxStep; step++)
                                {
                                    var r = new PredictiveForest(rn, step);
                                    foreach (var node in nodes)
                                    {
                                        sw.Reset();
                                        sw.Start();
                                        r.Update(node.Location, rSize);
                                        sw.Stop();
                                        var exists = r.PredictiveRegions.TryGetValue(step, out var pRegion);
                                        if (!exists) continue;
                                        exists = r.MRegion.Regions.TryGetValue(r.MRegion.RegionCount - 1, out var region);
                                        if (!exists) continue;

                                        var result = new Result();
                                        result.predictive_nodes = pRegion.Count;
                                        result.region_size = rSize;
                                        result.update_time = sw.Elapsed.TotalMilliseconds * 1000;
                                        result.historic_nodes = region.Count;
                                        result.step = step;
                                        Results.Add(result);
                                        //Console.WriteLine($"Latest region node count: {region.Count}");
                                        //Console.WriteLine($"Predictive region node count at step {step}: {pRegion.Count}");
                                        //Console.WriteLine($"Time elapsed: {sw.Elapsed.TotalMilliseconds * 1000} micro-seconds");
                                    }
                                }
                            }
                        }
                    }
                    pbar.Tick($"{tLoader.AllTripNodes.Count} files remain.");
                }
            }

            using (var streamWriter = new StreamWriter($"/Experiments/evaluations.csv", false, Encoding.UTF8))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(Results);
                }
            }
        }
    }
}