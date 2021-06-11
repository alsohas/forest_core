using System;
using System.Collections.Generic;
using System.Linq;
using forest_core.Forest;
using SharpLearning.Containers;
using SharpLearning.Containers.Matrices;
using SharpLearning.DecisionTrees.Learners;
using SharpLearning.DecisionTrees.Models;
using ShellProgressBar;

namespace forest_core.PredictionModels
{
    internal class DecisionTreeModel : ForestPredictionModel
    {
        public DecisionTreeModel(int step)
        {
            if (step > Parameters.MaxPredictiveDepth)
                throw new Exception(
                    $"Predictive step exceed maximum allowed steps in Parameters.cs file: {Parameters.MaxPredictiveDepth}");
            Steps = step;
        }

        private List<DictTripStruct> TripRows { get; set; }

        public int Steps { get; set; }

        //public ClassificationForestModel Model { get; set; }
        public ClassificationDecisionTreeModel Model { get; set; }

        //public ClassificationRandomForestLearner Learner { get; set; }
        public ClassificationDecisionTreeLearner Learner { get; set; }

        public F64Matrix Features { get; set; }

        public double[] TargetVector { get; set; }

        public long Predict(Region region, long[] possibleNodes)
        {
            var possibleTrajectories = GetTrajectories(region);
            var lastNodeCount = new Dictionary<long, int>();

            // figure out which leaf node had highest occurrence
            foreach (var possibleTrajectory in possibleTrajectories)
            {
                var lastNode = possibleTrajectory.ElementAt(possibleTrajectory.Length - 1);
                if (lastNodeCount.ContainsKey(lastNode))
                {
                    lastNodeCount[lastNode] += 1;
                    continue;
                }

                lastNodeCount[lastNode] = 1;
            }

            var maxOccurrence = -1;
            var highestOccurringNode = -1L;
            // select the highest occurring leaf node
            foreach (var (node, count) in lastNodeCount)
            {
                if (count <= maxOccurrence) continue;
                maxOccurrence = count;
                highestOccurringNode = node;
            }

            var predictions = new Dictionary<long, ProbabilityPrediction>();
            // select the trajectories with highest occurring leaf node
            // and get their predictions
            foreach (var possibleTrajectory in possibleTrajectories.Where(possibleTrajectory =>
                possibleTrajectory[^1] == highestOccurringNode))
                predictions.Add(predictions.Count,
                    Model.PredictProbability(Array.ConvertAll<UInt16, double>(possibleTrajectory, x => x)));

            var aggregateProbabilities = new Dictionary<long, double>();
            // for each probability table per trajectory, 
            // find the probability(s) for each of possible nodes
            // and add them together.
            foreach (var possibleNode in possibleNodes)
            {
                if (!aggregateProbabilities.ContainsKey(possibleNode)) aggregateProbabilities[possibleNode] = 0;

                foreach (var (_, probabilities) in predictions)
                {
                    var exists = probabilities.Probabilities.TryGetValue(possibleNode, out var probability);
                    if (!exists)
                    {
                        aggregateProbabilities[possibleNode] += 0;
                        continue;
                    }

                    aggregateProbabilities[possibleNode] += probability;
                }
            }

            var bestNode = 0L;
            var highestProbability = 0.0;
            // select the possible node with highest probability score
            foreach (var (node, probability) in aggregateProbabilities)
            {
                if (!(probability > highestProbability)) continue;
                bestNode = node;
                highestProbability = probability;
            }

            return bestNode;
        }

        /// <summary>
        ///     Generates the vectors from binaries and trains model.
        /// </summary>
        public void Generate()
        {
            if (Model != null) return;

            TripRows = RowParser.Read(Steps);
            Console.WriteLine("Generating vectors");
            GenerateVector();
            Console.WriteLine($"Training model on step {Steps}");
            TrainModel();
            Console.WriteLine("Finished training model.");
            GC.Collect();
        }

        /// <summary>
        ///     trains model.
        /// </summary>
        private void TrainModel()
        {
            //Learner = new ClassificationRandomForestLearner(maximumTreeDepth: 80, trees: 15); //54
            Learner = new ClassificationDecisionTreeLearner(100);
            Model = Learner.Learn(Features, TargetVector);
            Console.WriteLine("Finished training");
        }

        /// <summary>
        ///     Generates F64 vectors used by SharpLearner package.
        /// </summary>
        private void GenerateVector()
        {
            TargetVector = new double[TripRows.Count];
            Features = new F64Matrix(TripRows.Count, TripRows[0].priors.Length);
            using (var pbar = new ProgressBar(TripRows.Count, $"Parsing rows of {Steps}", new ProgressBarOptions()))
            {
                for (var i = 0; i < TripRows.Count; i++)
                {
                    TargetVector[i] = TripRows[i].destination[0];
                    for (var i1 = 0; i1 < TripRows[i].priors.Length; i1++) Features[i, i1] = TripRows[i].priors[i1];
                    pbar.Tick();
                }
            }

            Console.WriteLine("Finished Loading");
        }

        /// <summary>
        ///     Returns if a prediction is correct given trajectory and destination
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsCorrectOn(double[] feature, double target)
        {
            var prediction = Model.Predict(feature);
            return target.CompareTo(prediction) == 0;
        }

        // gets all trajectories from the region and 0 pads it if necessary
        // if the number of steps if lower than Parameters.Offset.
        private List<UInt16[]> GetTrajectories(Region region)
        {
            var trajectoryList = new List<UInt16[]>();
            var offset = Parameters.Offset;
            // region count is always 1 more than number of index.
            // because it is zero indexed.
            var trajectories = GetTrajectories(region, region.RegionCount - 1);
            foreach (var trajectory in trajectories)
            {
                if (trajectory.Count >= offset)
                {
                    trajectoryList.Add(trajectory.ToArray()[^offset..]);
                    continue;
                }

                var tArray = new UInt16[offset];
                for (var i = 0; i < trajectory.Count; i++) tArray[i] = trajectory[i];
                trajectoryList.Add(tArray);
            }

            return trajectoryList;
        }

        // helper method to recursively build trajectory from the
        // historical regional node structures. Upwards depth-first
        // traversal.
        private List<List<UInt16>> GetTrajectories(Region region, int regionIndex)
        {
            var trajectories = new List<List<UInt16>>();
            if (regionIndex < 0) return trajectories;

            var _trajectories = GetTrajectories(region, regionIndex - 1);
            region.Regions.TryGetValue(regionIndex, out var possibleNodes);

            // meaning nothing was returned from previous regions
            // e.g empty parent region
            if (_trajectories.Count == 0)
            {
                foreach (var possibleNodesKey in possibleNodes.Keys)
                    trajectories.Add(new List<UInt16> {possibleNodesKey});

                return trajectories;
            }

            foreach (var trajectory in _trajectories)
            {
                var parentID = trajectory.ElementAt(trajectory.Count - 1);
                foreach (var (key, node) in possibleNodes)
                {
                    // add trajectories if parent-child relation is present
                    if (!node.Parents.Contains(parentID)) continue;

                    var newTrajectory = trajectory.Concat(new List<UInt16> {key}).ToList();
                    trajectories.Add(newTrajectory);
                }
            }

            return trajectories;
        }

        public int Predict(Region region, int[] possibleNodes)
        {
            return 0;
        }
    }
}