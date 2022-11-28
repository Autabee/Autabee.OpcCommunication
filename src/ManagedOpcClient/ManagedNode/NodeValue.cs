using System;
using Autabee.Communication.OpcCommunicator.ManagedNode;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
    public class NodeValue
    {
        public NodeValue(ValueNodeEntry nodeEntry, object value)
        {
            NodeEntry = nodeEntry;
            Value = value;
        }

        public ValueNodeEntry NodeEntry { get; protected set; }
        public object Value { get; set; }

        public Type ValueType { get => NodeEntry.Type; }

        public override string ToString()
        {
            return $"[{NodeEntry}] {Value}";
        }
    }

    public sealed class NodeValue<T> : NodeValue
    {
        public NodeValue(ValueNodeEntry<T> nodeEntry, T value) : base(nodeEntry, value)
        {
        }

        public NodeValue(ValueNodeEntry nodeEntry, T value) : base(nodeEntry, value)
        {
        }

        public new ValueNodeEntry<T> NodeEntry { get => base.NodeEntry as ValueNodeEntry<T>; }
        public new T Value { get => (T)base.Value; }
    }
}