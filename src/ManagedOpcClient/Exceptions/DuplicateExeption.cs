using Autabee.Communication.ManagedOpcClient.ManagedNode;
using System;

namespace Autabee.Communication.ManagedOpcClient.Exceptions
{
    public class DuplicateException : Exception
    {
        public NodeEntry Node { get; }
        public DuplicateException(string str, NodeEntry node): base(str)
        {
            Node = node;
        }
        public bool Equals(object obj)
        {
            throw new NotImplementedException();
        }
        public int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
