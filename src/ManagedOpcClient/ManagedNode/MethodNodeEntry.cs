using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
    public class MethodNodeEntry : NodeEntry
    {
        public MethodArguments Arguments { get; set; } = new MethodArguments();

        public MethodNodeEntry(string nodeId, MethodArguments arguments) : this(
            new NodeId(nodeId), arguments)
        {

        }
        public MethodNodeEntry(NodeId nodeId, MethodArguments arguments) : base(
            nodeId)
        {
            Arguments = arguments;
        }
    }
}