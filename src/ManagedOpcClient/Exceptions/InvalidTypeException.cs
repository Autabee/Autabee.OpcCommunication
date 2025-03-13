using System;

namespace Autabee.Communication.ManagedOpcClient.Exceptions
{
    public class InvalidTypeException : Exception
    {
        public Type Type { get; }
        public InvalidTypeException(string str, Type type) : base(str)
        {
            Type = type;
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
