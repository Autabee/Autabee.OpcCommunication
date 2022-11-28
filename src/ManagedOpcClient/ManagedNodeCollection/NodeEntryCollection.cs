using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autabee.Communication.OpcCommunicator;

namespace Autabee.Communication.ManagedOpcClient.ManagedNodeCollection
{
    public class NodeEntryCollection : ICollection
    {
        protected List<ValueNodeEntry> nodeEntries = new List<ValueNodeEntry>();
        protected NodeIdCollection nodeIds = new NodeIdCollection();
        protected NodeIdCollection optimizedIds = new NodeIdCollection();
        protected List<Type> types = new List<Type>();

        public ValueNodeEntry this[int index] { get => nodeEntries[index]; }

        public int Count => nodeEntries.Count;

        public bool IsSynchronized { get; }

        public object SyncRoot { get; }

        public void Add(ValueNodeEntry node)
        {
            ValidateNode(node);

            nodeEntries.Add(node);
            nodeIds.Add(node.NodeString);

            if (node.RegisteredNodeId != null)
            {
                optimizedIds.Add(node.RegisteredNodeId);
            }
        }

        protected void ValidateNode(ValueNodeEntry node)
        {
            if (nodeIds.FirstOrDefault(o => o.ToString() == node.NodeString) == null)
            {
                ValidateType(node.Type);
            }
            else
            {
                throw new Exception("Known nodeString already in collection");
            }
        }

        private void ValidateType(Type type)
        {
            if (type.GetInterface("IEncodeable") != null)
            {
                types.Add(null);
            }
            else if (IsAcceptedType(type)
                )
            {
                types.Add(type);
            }
            else if (type.IsArray)
            {
                ValidateType(type.GetElementType());
            }
            else
            {
                throw new Exception("Not an allowed Type deffinition");
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
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        public List<ValueNodeEntry> NodeEntries => nodeEntries;

        public NodeIdCollection NodeIds => nodeIds;

        public List<Type> Types => types;
        public NodeIdCollection PreparedNodes
        {
            get { return optimizedIds; }
            set
            {
                optimizedIds = value;
                //for (int i = 0; i < value.Count; i++)
                //{
                //    nodeEntries[i].RegisterdNodeId = value[i];
                //}
            }
        }


        public NodeIdCollection GetNodeIds()
        {
            if (optimizedIds.Count == nodeEntries.Count)
            {
                return optimizedIds;
            }
            else
            {
                return nodeIds;
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

        internal NodeId ConnectedSessionId { get; set; }
        internal void SessionDisconnected(object sender, EventArgs args) {
            if (sender is Session session && session.SessionId == ConnectedSessionId)
            {
                ConnectedSessionId = null;
                for (int i = 0; i < nodeEntries.Count; i++)
                {
                    nodeEntries[i].SessionDisconnected(sender, args);
                }
                optimizedIds.Clear();
            }
            
        }
        internal void NewSessionEstablished(object sender, EventArgs args)
        {
            if (ConnectedSessionId == null && sender is OpcUaClientHelperApi communicator)
            {
                communicator.RegisterNodeIds(this);
            }
        }
    }
}