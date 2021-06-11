using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace forest_core.Forest
{
    [Serializable]
    public class RegionalNode
    {
        public HashSet<UInt16> Children;
        public HashSet<UInt16> Parents;

        public RegionalNode(Node node, HashSet<UInt16> parents)
        {
            Node = node;
            NodeID = node.NodeID;
            Children = new HashSet<UInt16>();
            Parents = new HashSet<UInt16>();

            var taskList = new List<Task>();
            var parentsTask = Task.Factory.StartNew(() => AddParents(node, parents));
            taskList.Add(parentsTask);
            var childrenTask = Task.Factory.StartNew(() => AddChildren(node));
            taskList.Add(childrenTask);

            Task.WaitAll(taskList.ToArray());
        }

        public Node Node { get; }
        public UInt16 NodeID { get; private set; }

        private void AddChildren(Node node)
        {
            foreach (var kv in node.OutgoingEdges) Children.Add(kv.Key.NodeID);
        }

        private void AddParents(Node node, HashSet<UInt16> parents)
        {
            if (parents == null) // this would imply we're in the first region therefore no parents
                return;
            foreach (var kv in node.IncomingEdges)
            {
                var nodeID = kv.Key.NodeID;
                if (parents.Contains(nodeID)) // only add valid parents
                    Parents.Add(kv.Key.NodeID);
            }
        }
    }
}