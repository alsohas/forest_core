using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace forest_core.Forest
{
    [Serializable]
    public class Region
    {
        public HashSet<UInt16> ObsoleteNodes = new HashSet<UInt16>();

        public ConcurrentDictionary<int, ConcurrentDictionary<UInt16, RegionalNode>> Regions =
            new ConcurrentDictionary<int, ConcurrentDictionary<UInt16, RegionalNode>>();

        //private static readonly object myLock = new object();
        //private static Region region = null;
        public int RegionCount => Regions.Count;


        //private static ThreadLocal<Region> instances = new ThreadLocal<Region>(() => new Region());

        //public static Region Instance
        //{
        //    get { return instances.Value; }
        //}

        /// <summary>
        ///     This method only add regional nodes to the first region.
        ///     Subsequent node additions to latter regions are done inside of
        ///     <see cref="PredictiveForest.Update(System.Device.Location.GeoCoordinate, double)" />.
        /// </summary>
        /// <param name="nodes"></param>
        public void Update(IEnumerable<Node> nodes)
        {
            var newRegion = new ConcurrentDictionary<UInt16, RegionalNode>();
            foreach (var node in nodes)
            {
                var regionalNode = new RegionalNode(node, null);
                newRegion.TryAdd(node.NodeID, regionalNode);
            }

            Regions.TryAdd(RegionCount, newRegion);
        }

        public void Reset()
        {
            Regions.Clear();
        }

        public override string ToString()
        {
            return GetHashCode().ToString();
        }
    }
}