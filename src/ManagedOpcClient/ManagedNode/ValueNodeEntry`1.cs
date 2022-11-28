using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.Communication.OpcCommunicator.ManagedNode
{
    public sealed class ValueNodeEntry<T> : ValueNodeEntry
    {
        public ValueNodeEntry(NodeId nodeId, T value = default) : base(nodeId, typeof(T)) { }
        public ValueNodeEntry(string nodeId, T value = default) : base(new NodeId(nodeId), typeof(T)) { }
        public NodeValue<T> CreateRecord(T value) { return new NodeValue<T>(this, value); }
    }
}