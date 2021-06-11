using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using forest_core.MovingObject;
using ShellProgressBar;

namespace forest_core.Managers
{
    internal class StpManager : ForestManager
    {
        public StpManager(double radius, int predictiveStep, RoadNetwork rn, TripsLoader tLoader,
            int maxForests, bool naive) : base(
            radius, predictiveStep, rn, tLoader, maxForests, naive)
        {
        }

        public new void MemoryBenchmark()
        {
            FillTrips(); // fill trips
            var sw = new Stopwatch();
            double elapsed = 0;
            GC.Collect();
            var baseMemory = GC.GetTotalMemory(true);
            using var p = new ProgressBar(PredictiveStep, "Steps", Options);
            for (var i = PredictiveStep; i > 0; i--) // grow each forest to n-step
            {
                var _p = p.Spawn(Forests.Count, "Trips", Options);
                var _progress = _p.AsProgress<double>();
                var count = 0;
                foreach (var f in Forests)
                {
                    count++;
                    sw.Reset();
                    sw.Start();
                    f.Update(Radius);
                    sw.Stop();
                    elapsed += sw.Elapsed.TotalMilliseconds * 1000;
                    if (count % 100 == 0) _p.Tick(count, $"{count}/{Forests.Count}");
                }

                GC.Collect();
                var memory = GC.GetTotalMemory(true) - baseMemory;
                var result = new Result
                {
                    region_size = Radius,
                    update_time = elapsed / Forests.Count,
                    predictive_step = PredictiveStep,
                    current_step = i,
                    memory = memory / 1000
                    //historic_nodes = historicNodes,
                    //predictive_nodes = predictiveNodes
                };
                Results.Add(result);
                p.Tick(PredictiveStep - i + 1);
            }

            Console.WriteLine("Finished Benchmark");
            using var streamWriter =
                new StreamWriter("/Experiments/naive_forest_evaluations.csv", false, Encoding.UTF8);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            csvWriter.WriteRecords(Results);
        }
    }
}