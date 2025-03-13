using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
    public sealed class ValueNodeEntry<T> : ValueNodeEntry
    {
        public ValueNodeEntry(NodeId nodeId) : base(nodeId, typeof(T)) { }
        public ValueNodeEntry(string nodeId) : base(new NodeId(nodeId), typeof(T)) { }

        public override NodeValueRecord CreateRecord(object value, DateTime dateTime = default)
        {
            if (value is T wrappedValue)
            {
                return new NodeValueRecord<T>(this, wrappedValue, dateTime);
            }
            T a = default;
            if (value == null && a == null)
            {
                return new NodeValueRecord<T>(this, default, dateTime);
            }
            else throw new ArgumentException($"Type mismatch for {NodeString}: {typeof(T)} != {value?.GetType()}");
           
        }
        public NodeValueRecord CreateRecord<K>(K value, DateTime dateTime)
        {
            if (value is T wrappedValue)
            {
                return new NodeValueRecord<T>(this, wrappedValue, dateTime);
            }
            T a = default;
            if (value == null && a == null)
            {
                return new NodeValueRecord<T>(this, default, dateTime);
            }
            else throw new ArgumentException($"Type mismatch for {NodeString}: {typeof(T)} != {typeof(K)}");

        }
    }
}