using System.Collections.Generic;
using System.IO;
using forest_core.Utils;
using Newtonsoft.Json;
using ShellProgressBar;

namespace forest_core.PredictionModels
{
    internal class DictGenerator
    {
        private DictGenerator()
        {
        }

        /// <summary>
        ///     Load reverse mapping of dictionary. NodeID -> Integer
        /// </summary>
        /// <returns></returns>
        public static Dictionary<long, int> LoadReverseMappedDict()
        {
            var fileName = $"{Parameters.DictFolder}rev_mapped.dict.bin";
            var reverseMappedDict = BinaryIO.ReadFromBinaryFile<Dictionary<long, int>>(fileName);
            return reverseMappedDict;
        }

        /// <summary>
        ///     Loads integer mapping of NodeIDs.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<int, long> LoadMappedDict()
        {
            var fileName = $"{Parameters.DictFolder}mapped.dict.bin";
            var reverseMappedDict = BinaryIO.ReadFromBinaryFile<Dictionary<int, long>>(fileName);
            return reverseMappedDict;
        }

        public static void GenerateDict()
        {
            var mappedDict = new Dictionary<int, long>();
            var reverseMappedDict = new Dictionary<long, int>();
            mappedDict[0] = 0;
            reverseMappedDict[0] = 0;

            var gpsFolders = Directory.GetDirectories(Parameters.AllTripsFolder);
            using (var pbar = new ProgressBar(gpsFolders.Length, "Parsing gps folder", ProgressBarOptions.Default))
            {
                foreach (var gpsFolder in gpsFolders)
                {
                    var gpsFiles = Directory.GetFiles(gpsFolder);
                    using (var pbar2 = pbar.Spawn(gpsFiles.Length, "Parsing files", ProgressBarOptions.Default))
                    {
                        foreach (var gpsFile in gpsFiles)
                        {
                            var json = File.ReadAllText(gpsFile);
                            var trips = JsonConvert.DeserializeObject<List<List<long>>>(json);
                            foreach (var allTrip in trips)
                            foreach (var trip in trips)
                            foreach (var node in trip)
                            {
                                if (reverseMappedDict.ContainsKey(node)) continue;
                                mappedDict[mappedDict.Count] = node;
                                reverseMappedDict[node] = reverseMappedDict.Count;
                            }

                            pbar2.Tick();
                        }
                    }

                    pbar.Tick();
                }
            }

            BinaryIO.WriteToBinaryFile($"{Parameters.DictFolder}mapped.dict.bin", mappedDict);
            BinaryIO.WriteToBinaryFile($"{Parameters.DictFolder}rev_mapped.dict.bin", reverseMappedDict);
        }
    }
}