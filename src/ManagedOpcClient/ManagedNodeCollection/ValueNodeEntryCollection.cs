using Autabee.Communication.ManagedOpcClient.Exceptions;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Autabee.Communication.ManagedOpcClient.ManagedNodeCollection
{
    public class ValueNodeEntryCollection : IEnumerable<ValueNodeEntry>
    {
        protected List<ValueNodeEntry> nodeEntries = new List<ValueNodeEntry>();
        protected NodeIdCollection nodeIds = new NodeIdCollection();
        protected NodeIdCollection registeredNodeIds = new NodeIdCollection();
        protected List<Type> types = new List<Type>();

        public ValueNodeEntry this[int i]
        {
            get => nodeEntries[i];
            set
            {
                nodeIds[i] = value.UnregisteredNodeId;
                registeredNodeIds[i] = value.RegisteredNodeId;
                nodeEntries[i] = value;
                types[i] = value.Type;
            }
        }

        public int Count => nodeEntries.Count;

        public void Add(ValueNodeEntry node)
        {
            ValidateNode(node);

            nodeEntries.Add(node);
            nodeIds.Add(node.NodeString);

            if (node.RegisteredNodeId != null)
            {
                registeredNodeIds.Add(node.RegisteredNodeId);
            }
        }

        protected void ValidateNode(ValueNodeEntry node)
        {
            if (nodeIds.FirstOrDefault(o => o.ToString() == node.NodeString) != null)
            {
                throw new DuplicateException("Duplicate Node being inserted into the collection. This is not allowed.", node);
            }
            ValidateType(node.Type);
        }

        private void ValidateType(Type type)
        {
            if (type.GetInterface("IEncodeable") != null || IsAcceptedType(type))
            {
                types.Add(type);
            }
            else if (type.IsArray)
            {
                ValidateType(type.GetElementType());
            }
            else
            {
                throw new InvalidTypeException(string.Format("Type {0} is not an IEncodeable or a primitive. So not allowed to be used in this collection.", type.FullName), type);
            }
        }

        private static bool IsAcceptedType(Type type)
        {
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(Guid)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(TimeSpan)
                || type == typeof(object);
        }

        public void AddRange(ValueNodeEntry[] nodes)
            =>  nodes.ToList().ForEach(Add);

        public void AddRange(ValueNodeEntryCollection nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                Add(nodes[i]);
            }
        }

        public List<ValueNodeEntry> NodeEntries => nodeEntries;

        public NodeIdCollection NodeIds => nodeIds;

        public List<Type> Types => types;
        public NodeIdCollection RegisteredNodeIds
        {
            get { return registeredNodeIds; }
            set
            {
                if (value.Count != nodeIds.Count)
                {
                    throw new Exception("NodeIdCollection size mis match");
                }
                for (int i = 0; i < value.Count; i++)
                {
                    nodeEntries[i].RegisteredNodeId = value[i];
                }
                registeredNodeIds = value;
            }
        }


        public NodeIdCollection GetNodeIds()
        {
            if (registeredNodeIds.Count == nodeEntries.Count)
            {
                return registeredNodeIds;
            }
            else
            {
                return nodeIds;
            }
        }

        public void CopyTo(Array array, int index)
        {
            array.SetValue(nodeEntries[index], index);
        }


        internal NodeId ConnectedSessionId { get; set; }
        internal void SessionDisconnected(object sender, EventArgs args)
        {
            if (sender is Session session && session.SessionId == ConnectedSessionId)
            {
                ConnectedSessionId = null;
                for (int i = 0; i < nodeEntries.Count; i++)
                {
                    nodeEntries[i].SessionDisconnected(sender, args);
                }
                registeredNodeIds.Clear();
            }
        }
        internal void NewSessionEstablished(object sender, EventArgs args)
        {
            if (ConnectedSessionId == null && sender is AutabeeManagedOpcClient communicator)
            {
                communicator.RegisterNodeIds(this);
            }
        }
        public NodeValueRecordCollection CreateRecords(IEnumerable<object> values, DateTime[] dateTimes = default)
        {
            if (dateTimes == default)
            {
                dateTimes = new DateTime[Count];
                var now = DateTime.UtcNow;
                dateTimes = dateTimes.Select(o => now).ToArray();
            }

            if (values.Count() != nodeEntries.Count)
            {
                throw new Exception("Values count does not match nodeEntries count");
            }
            try
            {
                var records = new NodeValueRecordCollection();
                for (int i = 0; i < Count; i++)
                {
                    records.Add(this[i].CreateRecord(values.ElementAt(i)));
                }

                return records;
            }
            catch (Exception)
            {
                List<Exception> exps = new List<Exception>();
                for (int i = 0; i < Count; i++)
                {
                    try
                    {
                        this[i].CreateRecord(values.ElementAt(i));
                    }
                    catch (Exception ex)
                    {
                        exps.Add(ex);
                    }
                }
                throw new AggregateException(exps);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return nodeEntries.GetEnumerator();
        }

        IEnumerator<ValueNodeEntry> IEnumerable<ValueNodeEntry>.GetEnumerator()
        {
            return nodeEntries.GetEnumerator();
        }
    }
}