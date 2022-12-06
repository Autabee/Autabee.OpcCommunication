using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
    public sealed class ValueNodeEntry<T> : ValueNodeEntry
    {
        public ValueNodeEntry(NodeId nodeId) : base(nodeId, typeof(T)) { }
        public ValueNodeEntry(string nodeId) : base(new NodeId(nodeId), typeof(T)) { }
        public NodeValueRecord<T> CreateRecord(T value) { return new NodeValueRecord<T>(this, value); }
    }
}