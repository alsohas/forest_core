using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace forest_core.Forest
{
    [Serializable]
    public class RegionalNode
    {
        public HashSet<int> Children;
        public HashSet<int> Parents;

        public RegionalNode(Node node, HashSet<int> parents)
        {
            Node = node;
            NodeID = node.NodeID;
            Children = new HashSet<int>();
            Parents = new HashSet<int>();

            var taskList = new List<Task>();
            var parentsTask = Task.Factory.StartNew(() => AddParents(node, parents));
            taskList.Add(parentsTask);
            var childrenTask = Task.Factory.StartNew(() => AddChildren(node));
            taskList.Add(childrenTask);

            Task.WaitAll(taskList.ToArray());
        }

        public Node Node { get; }
        public int NodeID { get; private set; }

        private void AddChildren(Node node)
        {
            foreach (var kv in node.OutgoingEdges) Children.Add(kv.Key.NodeID);
        }

        private void AddParents(Node node, HashSet<int> parents)
        {
            if (parents == null) // this would imply we're in the first region therefore no parents
                return;
            foreach (var kv in node.IncomingEdges)
            {
                var nodeID = kv.Key.NodeID;
                if (parents.Contains(nodeID)) // only add valid parents
                {
                    Parents.Add(kv.Key.NodeID);
                }
            }
        }
    }
}