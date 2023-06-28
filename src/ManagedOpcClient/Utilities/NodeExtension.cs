using Opc.Ua;
using System.Collections.Generic;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    static public class NodeExtension
    {
        /// <summary>
        /// Returns if the node is has a current user write access
        /// </summary>
        public static bool HasCurrentWriteAccess(this Node node)
            => node is VariableNode variableNode
            ? (variableNode.UserAccessLevel & AccessLevels.CurrentWrite) == AccessLevels.CurrentWrite
            : false;

        /// <summary>
        /// Returns if the node is has a current user read access
        /// </summary>
        public static bool HasCurrentReadAccess(this Node node)
            => node is VariableNode variableNode
            ? (variableNode.UserAccessLevel & AccessLevels.CurrentRead) == AccessLevels.CurrentRead
            : false;

        /// <summary>
        /// Returns if the node is has a current user execute access
        /// </summary>
        public static bool HasCurrentExecuteAccess(this Node node)
            => node is MethodNode variableNode
            ? variableNode.UserExecutable
            : false;

        /// <summary>
        /// Returns if the node is a variable node and is writable (not access)
        /// </summary>
        public static bool IsWritable(this Node node)
            => node is VariableNode variableNode
            ? (variableNode.AccessLevel & AccessLevels.CurrentWrite) == AccessLevels.CurrentWrite
            : false;
        /// <summary>
        /// Returns if the node is a variable node and is readable (not access)
        /// </summary>
        public static bool IsReadable(this Node node)
            => node is VariableNode variableNode
            ? (variableNode.AccessLevel & AccessLevels.CurrentRead) == AccessLevels.CurrentRead
            : false;

        public static bool IsExecutable(this Node node)
            => node is MethodNode variableNode
            ? variableNode.Executable
            : false;


        public static string Access(this Node node)
        {

            string access = "-";
            access += node.IsReadable() ? "r" : "-";
            access += node.IsWritable() ? "w" : "-";
            access += node.IsExecutable() ? "x" : "-";

            access += node.HasCurrentReadAccess() ? "r" : "-";
            access += node.HasCurrentWriteAccess() ? "w" : "-";
            access += node.HasCurrentExecuteAccess() ? "x" : "-";
            return access;

        }


        public struct NodeAccess
        {
            public bool Readable;
            public bool Writable;
            public bool Executable;
            public bool CurrentReadable;
            public bool CurrentWritable;
            public bool CurrentExecutable;

            public NodeAccess(Node node)
            {
                Readable = node.IsReadable();
                Writable = node.IsWritable();
                Executable = node.IsExecutable();
                CurrentReadable = node.HasCurrentReadAccess();
                CurrentWritable = node.HasCurrentWriteAccess();
                CurrentExecutable = node.HasCurrentExecuteAccess();
            }

            public string AccessString()
            {
                string access = "-";
                access += Readable ? "r" : "-";
                access += Writable ? "w" : "-";
                access += Executable ? "x" : "-";

                access += CurrentReadable ? "r" : "-";
                access += CurrentWritable ? "w" : "-";
                access += CurrentExecutable ? "x" : "-";
                return access;
            }
            public string DetailedAccessString()
            {
                string accessStr = "";
                if (Readable || Writable || Executable)
                {
                    accessStr += "Sever node: ";
                    List<string> access = new List<string>();
                    if (Readable) access.Add("Readable");
                    if (Writable) access.Add("Writable");
                    if (Executable) access.Add("Executable");
                    accessStr += string.Join(", ", access);
                }

                if (CurrentReadable || CurrentWritable || CurrentExecutable)
                {
                    accessStr += ". Current user can: ";
                    List<string> access = new List<string>();
                    if (Readable) access.Add("Read");
                    if (Writable) access.Add("Writ");
                    if (Executable) access.Add("Execute");
                    accessStr += string.Join(", ", access);
                }
                return accessStr.Length > 0 ? accessStr + "." : string.Empty;
            }
        }
    }
}
