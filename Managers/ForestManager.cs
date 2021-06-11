using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using forest_core.Forest;
using forest_core.MovingObject;
using ShellProgressBar;

namespace forest_core.Managers
{
    internal class ForestManager
    {
        public readonly int MaxForests;
        public readonly List<Result> Results;
        public List<PredictiveForest> Forests;
        public List<NaiveForest> NaiveForests;
        public int PredictiveStep;
        public double Radius;
        public RoadNetwork RN;
        public TripsLoader TLoader;
        public List<List<Node>> Trajectories;
        public bool Naive { get; set; }

        public ForestManager(double radius, int predictiveStep, RoadNetwork rn, TripsLoader tLoader, int maxForests, bool naive)
        {
            Radius = radius;
            PredictiveStep = predictiveStep;
            RN = rn;
            TLoader = tLoader;
            Trajectories = new List<List<Node>>();
            Forests = new List<PredictiveForest>();
            NaiveForests = new List<NaiveForest>();
            MaxForests = maxForests;
            Results = new List<Result>();
            Naive = naive;
        }


        public ProgressBarOptions Options { get; set; }

        public void MemoryBenchmark()
        {
            FillTrips(); // fill trips
            var sw = new Stopwatch();
            double elapsed = 0;
            using var p = new ProgressBar(PredictiveStep, "Steps", Options);

            for (var i = PredictiveStep; i > 0; i--) // grow each forest to n-step
            {
                var _p = p.Spawn(Forests.Count, "Trips", Options);
                var _progress = _p.AsProgress<double>();
                var count = 0;
                dynamic mForests;
                if (Naive)
                {
                    mForests = NaiveForests;
                }
                else
                {
                    mForests = Forests;
                }
                foreach (var f in mForests)
                {
                    count++;
                    sw.Reset();
                    sw.Start();
                    f.Update(Radius);
                    sw.Stop();
                    elapsed += sw.Elapsed.TotalMilliseconds * 1000;
                    
                    if (count % 100 == 0) _p.Tick(count, $"{count}/{Forests.Count}");
                }

                
                var result = new Result
                {
                    region_size = Radius,
                    update_time = elapsed / Forests.Count,
                    predictive_step = PredictiveStep,
                    current_step = i,
                    //memory = memory / 1000
                    //historic_nodes = historicNodes,
                    //predictive_nodes = predictiveNodes
                };
                Results.Add(result);
                p.Tick(PredictiveStep - i + 1);
            }

            Console.WriteLine("Finished Benchmark");
            using var streamWriter = new StreamWriter("/Experiments/10k_forest_evaluations.csv", false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(Results);
        }

        protected void FillTrips()
        {
            while (!TLoader.AllTripNodes.IsEmpty)
            {
                TLoader.AllTripNodes.TryPop(out var nodes);
                if (nodes.Count < PredictiveStep) continue;
                Trajectories.Add(nodes);
                if (Naive)
                {
                    var r = new NaiveForest(RN, PredictiveStep, nodes);
                    NaiveForests.Add(r);

                }
                else
                {
                    var r = new PredictiveForest(RN, PredictiveStep, nodes);
                    Forests.Add(r);
                }

                if (Forests.Count > MaxForests || NaiveForests.Count > MaxForests) break;
            }
            Console.WriteLine($"Initialized {Forests.Count} forests.");
        }
    }
}