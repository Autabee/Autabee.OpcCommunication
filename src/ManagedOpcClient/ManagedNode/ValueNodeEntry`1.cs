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

            // last resort expectation of null as default of T, only if T is a reference type or nullable value type
            T a = default;
            if (value == null && a == null)
            {
                return new NodeValueRecord<T>(this, default, dateTime);
            }

            // failed to cast value to T. create failure message.

            if ((value is ExtensionObject extensionObject) && typeof(T).GetInterfaces().Any(o => o == typeof(IEncodeable)))
            {

                var element = typeof(T).GetConstructor(Type.EmptyTypes).Invoke(null) as IEncodeable;


                throw new ArgumentException($"Extension node mismatch for {NodeString}: {typeof(T)} != {value?.GetType()},\n TypeId: {extensionObject.TypeId} ?= {element.TypeId}");
            }
            if ((value is ExtensionObject[] extensionObjects) && typeof(T).IsArray && typeof(T).GetElementType().GetInterfaces().Any(o => o == typeof(IEncodeable)))
            {
                var elementtype = typeof(T).GetElementType();
                var constructor = elementtype.GetConstructor(Type.EmptyTypes);
                IEncodeable element = constructor.Invoke(null) as IEncodeable;
                if (extensionObjects.Length > 0)
                {
                    throw new ArgumentException($"Extension[] node mismatch for {NodeString}: {typeof(T)} != {value?.GetType()},\n TypeId: {extensionObjects[0].TypeId} ?= {element.TypeId}");
                }
                else
                {

                }
            }
            throw new ArgumentException($"Type mismatch for {NodeString}: {typeof(T)} != {value?.GetType()}");

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