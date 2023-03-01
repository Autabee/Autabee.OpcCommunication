using Opc.Ua;

namespace Autabee.OpcScout.RazorControl.Browse
{
    public enum NodeImageId
    {
        None,
        Loading,
        Object,
        Variable,
        Method,
        ObjectType,
        VariableType,
        ReferenceType,
        DataType,
        View,
        Unknown,

        Folder
    }

    public static class NodeImageExtension
    {
        public static NodeImageId GetNodeImage(this ReferenceDescription nodeClass)
        {
            if (nodeClass.TypeDefinition.IdType == IdType.Numeric)
            {
                switch ((uint)nodeClass.TypeDefinition.Identifier)
                {
                    default:
                        break;

                    case 61:
                    //FunctionalGroupType
                    case 1005:
                        return NodeImageId.Folder;
                        //case 0:
                        //return NodeImageId.DataType;

                }
            }
            switch (nodeClass.NodeClass)
            {
                default:
                    return NodeImageId.None;

                case NodeClass.Object:
                    return NodeImageId.Object;

                case NodeClass.Variable:
                    return NodeImageId.Variable;

                case NodeClass.Method:
                    return NodeImageId.Method;

                case NodeClass.ObjectType:
                    return NodeImageId.ObjectType;

                case NodeClass.VariableType:
                    return NodeImageId.VariableType;

                case NodeClass.ReferenceType:
                    return NodeImageId.ReferenceType;

                case NodeClass.DataType:
                    return NodeImageId.DataType;

                case NodeClass.View:
                    return NodeImageId.View;
            }
        }
    }
}