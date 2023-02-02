using Opc.Ua;

namespace Autabee.Communication.ManagedOpcClient
{
    static public class NodeExtension
    {
        public static bool HasCurrentWriteAcces(this Node node)
            => node is VariableNode variableNode 
            ? (variableNode.UserAccessLevel & AccessLevels.CurrentWrite) == AccessLevels.CurrentWrite 
            : false;
        

        public static bool IsCurrentWritable(this Node node)
            => node is VariableNode variableNode 
            ? (variableNode.AccessLevel & AccessLevels.CurrentWrite) == AccessLevels.CurrentWrite 
            : false;

    }
}
