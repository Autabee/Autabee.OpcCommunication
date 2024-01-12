using System;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
    public class NodeValueRecord
    {
        public NodeValueRecord(ValueNodeEntry nodeEntry, object value)
        {
            NodeEntry = nodeEntry;
            Value = value;
        }

        public NodeValueRecord(string nodeId, object value)
        {
            NodeEntry = new ValueNodeEntry(nodeId, value.GetType());
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
}