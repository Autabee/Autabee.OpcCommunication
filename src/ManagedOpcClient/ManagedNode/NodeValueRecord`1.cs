﻿using System;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{

    public class NodeValueRecord<T> : NodeValueRecord
    {
        public NodeValueRecord(ValueNodeEntry<T> nodeEntry, T value, DateTime timeStamp) : base(nodeEntry, value, timeStamp)
        {
        }

        public NodeValueRecord(ValueNodeEntry nodeEntry, T value, DateTime timeStamp) : base(nodeEntry, value, timeStamp)
        {
        }
        public NodeValueRecord(string nodeId, T value, DateTime timeStamp) : base(new ValueNodeEntry(nodeId,value.GetType()), value, timeStamp)
        {
        }

        public new ValueNodeEntry<T> NodeEntry { get => base.NodeEntry as ValueNodeEntry<T>; }
        public new T Value { get => (T)base.Value; }
    }
}