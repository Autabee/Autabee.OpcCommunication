using Autabee.Communication.ManagedOpc.ManagedNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Autabee.Communication.ManagedOpc.ManagedNodeCollection
{
    public class NodeValueCollection : NodeEntryCollection
    {
        private List<NodeValue> nodeValues = new List<NodeValue>();
        private List<object> values = new List<object>();

        public List<object> Values { get => values; set => values = value; }

        public new NodeValue this[int index]
        {
            get
            {
                var node = nodeValues[index];
                node.Value = values[index];
                return node;
            }
        }

        public void Add(NodeValue nodeValue)
        {
            Add(nodeValue.NodeEntry);
            nodeValues.Add(nodeValue);
            values.Add(nodeValue.Value);
        }

        public void UpdateValues(List<object> Values)
        {
            if (Values is null)
            {
                throw new ArgumentNullException(nameof(Values));
            }

            if (Values.Count != values.Count)
            {
                throw new Exception("Value Array size mis match");
            }

            for (int i = 0; i < Values.Count; i++)
            {
                if (Values[i] == null)
                {
                    throw new ArgumentNullException(nameof(Values), $"Argument [{i}] in array is null");
                }
            }

            values = Values;
        }

        //private static bool IsAcceptedType(ValueNodeEntry node)
        //{
        //    return node.Type.IsPrimitive
        //                        || node.Type == typeof(string)
        //                        || node.Type == typeof(Guid)
        //                        || node.Type == typeof(decimal)
        //                        || node.Type == typeof(DateTime)
        //                        || node.Type == typeof(TimeSpan);
        //}

        public void AddRange(NodeValue[] nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        public void CopyTo(Array array, int index)
        {
            for (int i = 0; i < nodeEntries.Count; i++)
            {
                array.SetValue(nodeEntries[i], i);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return nodeEntries.GetEnumerator();
        }
    }
}