using System;
using System.Linq;

namespace Autabee.OpcToClass
{
    public record Field
    {
        public string Name;
        public string TypeName;
        public string Type;
        public string Value;
        public string ClassFieldName;
        public int ArrayDimensions;
    }
}
