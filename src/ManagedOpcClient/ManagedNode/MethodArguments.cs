using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
#if NET5_0_OR_GREATER
    public record MethodArguments
#else
    public class MethodArguments
#endif

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