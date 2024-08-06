using Opc.Ua;
using System;
using System.Linq;
using System.Reflection;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
    public class ValueNodeEntry : NodeEntry
    {
        public ValueNodeEntry(string nodeId, Type type) : this(new NodeId(nodeId), type) { }

        public ValueNodeEntry(NodeId nodeId, Type type) : base(nodeId)
        {
            Type = type;
            if (type != null && type.GetInterface(nameof(IEncodeable)) != null)
            {
                IsUDT = true;
                Constructor = type.GetConstructors().FirstOrDefault(o => o.GetParameters().Length == 0);
                if (Constructor == null)
                {
                    throw new TypeInitializationException(
                                              type.FullName,
                                              new Exception("No parameterless constructor for encodable object"));
                }
            }
        }

        public Type Type { get; protected set; }

        public bool IsUDT { get; private set; }

        public ConstructorInfo Constructor { get; private set; }

        public virtual NodeValueRecord CreateRecord(object value) { return new NodeValueRecord(this, value); }
    }
}