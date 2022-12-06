using System;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{

    public class NodeValueRecord<T> : NodeValueRecord
    {
        public NodeValueRecord(ValueNodeEntry<T> nodeEntry, T value) : base(nodeEntry, value)
        {
        }

        public NodeValueRecord(ValueNodeEntry nodeEntry, T value) : base(nodeEntry, value)
        {
        }

        public new ValueNodeEntry<T> NodeEntry { get => base.NodeEntry as ValueNodeEntry<T>; }
        public new T Value { get => (T)base.Value; }
    }
}