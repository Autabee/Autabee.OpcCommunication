using Autabee.Communication.ManagedOpcClient.ManagedNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Autabee.Communication.ManagedOpcClient.ManagedNodeCollection
{
    public class NodeValueRecordCollection : ValueNodeEntryCollection, IEnumerable<NodeValueRecord>
    {
        internal List<NodeValueRecord> nodeValueRecords = new List<NodeValueRecord>();
        public IEnumerable<object> Values
        {
            get => nodeValueRecords.Select(o => o.Value);
            set => UpdateValues(value);
        }

        public new NodeValueRecord this[int index]
        {
            get
            {
                return nodeValueRecords[index];
            }
            set
            {
                nodeValueRecords[index] = value;
                base[index] = value.NodeEntry;
            }
        }


        public void Add(NodeValueRecord nodeValue)
        {
            base.Add(nodeValue.NodeEntry);
            nodeValueRecords.Add(nodeValue);
        }

        public void UpdateValues(IEnumerable<object> Values)
        {
            if (Values is null)
            {
                throw new ArgumentNullException(nameof(Values));
            }

            if (Values.Count() != nodeEntries.Count)
            {
                throw new Exception("Value Array size mis match");
            }

            try
            {
                List<NodeValueRecord> newRecords = new List<NodeValueRecord>();
                for (int i = 0; i < nodeValueRecords.Count(); i++)
                {
                    newRecords.Add(nodeEntries[i].CreateRecord(Values.ElementAt(i), DateTime.UtcNow));
                }
                nodeValueRecords = newRecords;
            }
            catch
            {
                List<Exception> excptions = new List<Exception>();
                for (int i = 0; i < nodeValueRecords.Count(); i++)
                {
                    try
                    {
                        base[i].CreateRecord(Values.ElementAt(i), DateTime.UtcNow);
                    }
                    catch (Exception e)
                    {
                        excptions.Add(e);
                    }
                }
                throw new AggregateException(excptions);
            }
        }

        public void AddRange(NodeValueRecord[] nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        public void AddRange(NodeValueRecordCollection nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                Add(nodes[i]);
            }
        }

        public new IEnumerator GetEnumerator()
        {
            return nodeValueRecords.GetEnumerator();
        }

        IEnumerator<NodeValueRecord> IEnumerable<NodeValueRecord>.GetEnumerator()
        {
            return nodeValueRecords.GetEnumerator();
        }
    }
}