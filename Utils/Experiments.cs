using System;
using CsvHelper;
using forest_core.Forest;
using forest_core.MovingObject;
using ShellProgressBar;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;

namespace forest_core.Utils
{
    class Experiments
    {
        private RoadNetwork RN;

        public Experiments()
        {
            RN = new RoadNetwork();
            RN.BuildNetwork();
            // try loading trips
            TLoader = new TripsLoader(RN);
            TLoader.LoadTrips();

            Options = new ProgressBarOptions
            {
                DisplayTimeInRealTime = false
            };
        }

        public TripsLoader TLoader { get; set; }

        public ProgressBarOptions Options { get; set; }

        public void Execute()
        {
            {
                var sw = Stopwatch.StartNew();
                while (!TLoader.AllTripNodes.IsEmpty)
                {
                    TLoader.AllTripNodes.TryPop(out var nodes);
                    if (nodes.Count < 5) continue;
                    var e = new ManualResetEvent(false);
                    lock (Events)
                    {
                        Events.Add(e);
                    }
                    ThreadPool.QueueUserWorkItem(state => EvalTrip(nodes, e));
                
                }
            }
            int eCount = Events.Count;
            using (var p = new ProgressBar(eCount, "Trips", Options))
            {
                var progress = p.AsProgress<double>();
                while (Events.Count >= 64)
                {
                    System.Threading.Thread.Sleep(5000);
                    double _eCount = Events.Count;
                    progress.Report((eCount - _eCount) / eCount);

                    Console.WriteLine("\r{0}% ", ((eCount - _eCount) / (eCount)) * 100);
                }
            }

            try
            {
                lock (Events)
                {
                    WaitHandle.WaitAll(Events.ToArray());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Finished");
            }
            lock (Results)
            {
                using (var streamWriter = new StreamWriter($"/Experiments/evaluations.csv", false, Encoding.UTF8))
                {
                    using (CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                    {
                        csvWriter.WriteRecords(Results);
                    }
                }
            }
        }

        private void EvalTrip(List<Node> nodes, ManualResetEvent e)
        {
            //var sw = new Stopwatch();
            foreach (var rSize in RegionSizes)
            
            {
                for (int step = 1; step <= MaxStep; step++)
                {
                    var r = new PredictiveForest(RN, step);
                    for (var index = 0; index < nodes.Count; index++)
                    {
                        if (index >= 5) break;
                        var node = nodes[index];
                        //sw.Reset();
                        //sw.Start();
                        r.Update(node.Location, rSize);
                        //sw.Stop();
                        var exists = r.PredictiveRegions.TryGetValue(step, out var pRegion);
                        if (!exists) continue;
                        exists = r.MRegion.Regions.TryGetValue(r.MRegion.RegionCount - 1, out var region);
                        if (!exists) continue;

                        var result = new Result
                        {
                            predictive_nodes = pRegion.Count,
                            region_size = rSize,
                            //update_time = sw.Elapsed.TotalMilliseconds * 1000,
                            historic_nodes = region.Count,
                            predictive_step = step,
                            current_step = index + 1
                        };
                        lock (Results)
                        {
                            Results.Add(result);
                        }

                        //Console.WriteLine($"Latest region node count: {region.Count}");
                        //Console.WriteLine($"Predictive region node count at step {step}: {pRegion.Count}");
                        //Console.WriteLine($"Time elapsed: {sw.Elapsed.TotalMilliseconds * 1000} micro-seconds");
                    }
                }
            }
            e.Set();
            lock (Events)
            {
                Events.Remove(e);
            }
        }

        private HashSet<ManualResetEvent> Events = new HashSet<ManualResetEvent>();
        public List<dynamic> Results = new List<dynamic>();
        public static int MaxStep = 5;
        public static int[] RegionSizes = new[] { 25, 50, 75, 100 };

    }

}

