namespace forest_core
{
    internal class Parameters
    {
        #region files

        public static readonly string BaseFolder = "C:/Users/abdul/Desktop/forest_2020/data/";
        public static string AllTripsFolder = $"{BaseFolder}map_matched_all/";
        public static string TripsFolder = $"{BaseFolder}map_matched/";
        public static string BinFolder = $"{BaseFolder}bin_folder/";

        public static string DictFolder = $"{BaseFolder}dicts/";
        //public static string VectorFolder = $"{BaseFolder}vectors/";
        //public static string ModelsFolder = $"{BaseFolder}models/";


        public static string NodesFile = $"{BaseFolder}chengdu_nodes.json";
        public static string EdgesFile = $"{BaseFolder}chengdu_edges.json";

        #endregion

        #region experiments

        public static int MaxPredictiveDepth = 6;
        public static int MinPredictiveDepth = 1;
        public static int PredictiveDepthIncrement = 1;

        public static double MaxRegionSize = 150;
        public static double MinRegionSize = 50;
        public static double RegionSizeIncrement = 50;

        private static readonly string ResultsFolder = "/Experiments/Results/";

        public static string PredictiveAccuracyFile = $"{ResultsFolder}predictive_results_no_obsolete.csv";
        public static string ContinuousPredictiveAccuracyFile = $"{ResultsFolder}continuous_predictive_results.csv";

        public static string PresentAccuracyFile = $"{ResultsFolder}present_results.csv";
        public static string HistoricalAccuracyFile = $"{ResultsFolder}past_results.csv";

        public static string PerformanceFile = $"{ResultsFolder}performance_results.csv";

        public static int Offset = 3;

        public static int FuncID = 2;

        #endregion
    }
}