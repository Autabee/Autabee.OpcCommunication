using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Linq;
using System.Reflection;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
    public class NodeEntry
    {
        public NodeEntry(string nodeId) : this(new NodeId(nodeId)) { }
        public NodeEntry(NodeId nodeId)
        {
            UnregisteredNodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        }

        public string NodeString { get => UnregisteredNodeId.ToString(); }

        public NodeId UnregisteredNodeId { get; }

        public NodeId RegisteredNodeId { get; set; }

        public bool Registered { get => RegisteredNodeId != null; }
        public NodeId GetNodeId() => Registered ? RegisteredNodeId : UnregisteredNodeId;

        internal NodeId ConnectedSessionId { get; set; }
        internal void SessionDisconnected(object sender, EventArgs args)
        {
            if (sender is Session session && session.SessionId == ConnectedSessionId)
            {
                ConnectedSessionId = null;
                RegisteredNodeId = null;
            }
        }
        internal void NewSessionEstablished(object sender, EventArgs args)
        {
            if (ConnectedSessionId == null && sender is OpcUaClientHelperApi communicator)
            {
                communicator.RegisterNodeId(this);
            }
        }

        public override string ToString() { return UnregisteredNodeId.ToString(); }
    }
}