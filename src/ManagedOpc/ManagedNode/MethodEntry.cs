using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.Communication.ManagedOpc.ManagedNode
{
    public class MethodEntry : NodeEntry
    {
        public MethodArguments Arguments { get; set; } = new MethodArguments();

        public MethodEntry(string nodeId, MethodArguments arguments) : this(
            new NodeId(nodeId), arguments)
        {

        }
        public MethodEntry(NodeId nodeId, MethodArguments arguments) : base(
            nodeId)
        {
            Arguments = arguments;
        }
    }
}