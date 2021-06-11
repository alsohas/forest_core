using forest_core.Forest;

namespace forest_core.PredictionModels
{
    internal interface ForestPredictionModel
    {
        /// <summary>
        ///     Returns the most likely node from a list of nodes after prediction.
        ///     Assumes the node IDs are in dict converted form.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="possibleNodes"></param>
        /// <returns>The predicted node</returns>
        public long Predict(Region region, long[] possibleNodes);
    }
}