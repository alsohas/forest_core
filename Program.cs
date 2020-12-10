using forest_core.Utils;

namespace forest_core
{
    public class Result
    {
        public int predictive_step { get; set; }
        public int current_step { get; set; }
        public int region_size { get; set; }
        public int historic_nodes { get; set; }
        public int predictive_nodes { get; set; }
        //public double update_time { get; set; }
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
            var e = new Experiments();
            e.Execute();
        }
    }
}