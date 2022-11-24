using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.Communication.ManagedOpc.ManagedNode
{
    public class MethodArguments
    {
        public Type[] InputArgumentTypes { get; internal set; }

        public Type[] OutputArgumentTypes { get; internal set; }

        public ArgumentCollection InputArguments { get; private set; }

        public ArgumentCollection OutputArguments { get; private set; }

        public MethodArguments(ArgumentCollection inputArgument = null, ArgumentCollection outputArgument = null)
        {
            InputArguments = inputArgument == null ? new ArgumentCollection() : inputArgument;
            OutputArguments = outputArgument == null ? new ArgumentCollection() : outputArgument;
        }
    }
}