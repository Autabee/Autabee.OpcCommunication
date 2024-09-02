using System;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
    public class NodeValueRecord
    {
        public NodeValueRecord(ValueNodeEntry nodeEntry, object value, DateTime dateTime = default)
        {
            

            NodeEntry = nodeEntry;
            Value = value;
            TimeStamp = dateTime == default ? DateTime.UtcNow : dateTime;
        }

        public NodeValueRecord(string nodeId, object value, DateTime dateTime = default)
        {
            NodeEntry = new ValueNodeEntry(nodeId, value.GetType());
            Value = value;
            TimeStamp = dateTime == default ? DateTime.UtcNow : dateTime;
        }

        public ValueNodeEntry NodeEntry { get; protected set; }
        public object Value { get; set; }
        public DateTime TimeStamp { get; set; }

        public Type ValueType { get => NodeEntry.Type; }

        public override string ToString()
        {
            return $"[{TimeStamp:O} | {NodeEntry}] {Value}";
        }
    }
}