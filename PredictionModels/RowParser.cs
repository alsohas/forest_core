using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using forest_core.Utils;
using ShellProgressBar;

namespace forest_core.PredictionModels
{
    [Serializable]
    internal struct TripStruct
    {
        public long destination;
        public long[] priors;
    }

    [Serializable]
    internal struct DictTripStruct
    {
        public int[] destination;
        public int[] priors;

        public void SetDestination(int val)
        {
            destination = new[] {val};
        }
    }

    [Serializable]
    public class TripRow
    {
        private long[] _PriorsFormatted;

        public long destination { get; set; }
        public string priors { get; set; }

        public long[] GetPriors()
        {
            return _PriorsFormatted;
        }

        public void FormatPriors()
        {
            var priorsFormatted = new List<long>();
            var firstPass = priors.Split(",");
            foreach (var item in firstPass)
            {
                var parsed = long.TryParse(item, out var itemParsed);
                if (!parsed)
                {
                    var _item = item.Trim();
                    _item = _item.Trim(']', '[', '\"');
                    itemParsed = long.Parse(_item);
                }

                priorsFormatted.Add(itemParsed);
            }

            var __PriorsFormatted = priorsFormatted.ToArray()[^Math.Min(priorsFormatted.Count, RowParser.MaxOffset)..];
            _PriorsFormatted = new long[RowParser.MaxOffset];
            for (var i = 0; i < __PriorsFormatted.Length; i++) _PriorsFormatted[i] = __PriorsFormatted[i];
            priors = null;
        }

        public override string ToString()
        {
            return $"Destination: {destination}\nPriors: {string.Join(", ", _PriorsFormatted)}";
        }
    }

    internal class RowParser
    {
        public static int MaxOffset = 8;

        private RowParser()
        {
        }

        /// <summary>
        ///     Reads the files inside each csv file for the steps.
        ///     Generates TripRows (dictionary mapping must be done prior)
        ///     and saves the binary. This Binary
        ///     can be easily converted to whatever vector format.
        /// </summary>
        public static void Read()
        {
            for (var i = Parameters.MaxPredictiveDepth; i > 0; i--)
            {
                Read(i);
                Console.WriteLine($"Finished step {i}");
            }
        }

        private static void ReadSave(int step)
        {
            var reverseMappedDict = DictGenerator.LoadReverseMappedDict();
            var allCSV = new List<string>();
            var allRecord = new List<DictTripStruct>();

            var baseFolderName = $"{Parameters.BaseFolder}map_matched_csv_{step}";
            var innerFolders = Directory.GetDirectories(baseFolderName);
            foreach (var folder in innerFolders) allCSV = allCSV.Concat(Directory.GetFiles(folder)).ToList();

            using (var pbar = new ProgressBar(allCSV.Count, $"Step {step} CSV Files", new ProgressBarOptions()))
            {
                foreach (var csvFile in allCSV)
                {
                    using (var reader = new StreamReader(csvFile))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<TripRow>();
                        foreach (var record in records)
                        {
                            record.FormatPriors();
                            var s = new TripStruct {priors = record.GetPriors(), destination = record.destination};
                            var d = GetDictFromRow(s, reverseMappedDict);
                            allRecord.Add(d);
                        }
                    }

                    pbar.Tick();
                }
            }

            GC.Collect();
            BinaryIO.WriteToBinaryFile($"{Parameters.BinFolder}trips_rows_{step}.bin", allRecord);
        }

        public static List<DictTripStruct> Read(int step)
        {
            var reverseMappedDict = DictGenerator.LoadReverseMappedDict();
            var allCSV = new List<string>();
            var allRecord = new List<DictTripStruct>();

            var baseFolderName = $"{Parameters.BaseFolder}map_matched_csv_{step}";
            var innerFolders = Directory.GetDirectories(baseFolderName);
            foreach (var folder in innerFolders) allCSV = allCSV.Concat(Directory.GetFiles(folder)).ToList();

            using (var pbar = new ProgressBar(allCSV.Count, $"Step {step} CSV Files", new ProgressBarOptions()))
            {
                foreach (var csvFile in allCSV)
                {
                    using (var reader = new StreamReader(csvFile))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<TripRow>();
                        foreach (var record in records)
                        {
                            record.FormatPriors();
                            var s = new TripStruct {priors = record.GetPriors(), destination = record.destination};
                            var d = GetDictFromRow(s, reverseMappedDict);
                            allRecord.Add(d);
                        }
                    }

                    pbar.Tick();
                }
            }

            GC.Collect();
            return allRecord;
        }

        private static DictTripStruct GetDictFromRow(TripStruct tripStruct, Dictionary<long, int> dictionary)
        {
            var d = new DictTripStruct();
            dictionary.TryGetValue(tripStruct.destination, out var destination);
            d.SetDestination(destination);
            d.priors = new int[tripStruct.priors.Length];
            for (var i = 0; i < tripStruct.priors.Length; i++) d.priors[i] = dictionary[tripStruct.priors[i]];
            return d;
        }
    }
}