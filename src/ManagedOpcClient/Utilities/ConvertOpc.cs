using Opc.Ua;
using System;
using System.Linq;
using System.Xml;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    public static class ConvertOpc
    {
        public static object Read(this IDecoder decoder, string type, string name)
        {
            return type switch
            {
                "opc:Boolean" => decoder.ReadBoolean(name),
                "opc:Byte" => decoder.ReadByte(name),
                "opc:SByte" => decoder.ReadSByte(name),

                "opc:UInt16" => decoder.ReadUInt16(name),
                "opc:UInt32" => decoder.ReadUInt32(name),
                "opc:UInt64" => decoder.ReadUInt64(name),

                "opc:Int16" => decoder.ReadInt16(name),
                "opc:Int32" => decoder.ReadInt32(name),
                "opc:Int64" => decoder.ReadInt64(name),

                "opc:Float" => decoder.ReadFloat(name),
                "opc:Double" => decoder.ReadDouble(name),

                "opc:String" => decoder.ReadString(name),
                "opc:ByteString" => decoder.ReadByteString(name),

                "opc:Guid" => decoder.ReadGuid(name),
                "opc:DateTime" => decoder.ReadDateTime(name),
                "opc:LocalizedText" => decoder.ReadLocalizedText(name),
                "opc:NodeId" => decoder.ReadNodeId(name),
                "opc:ExpandedNodeId" => decoder.ReadExpandedNodeId(name),
                "opc:StatusCode" => decoder.ReadStatusCode(name),
                "opc:XmlElement" => decoder.ReadXmlElement(name),
                "opc:ExtensionObject" => decoder.ReadExtensionObject(name),
                "opc:DataValue" => decoder.ReadDataValue(name),
                "opc:Variant" => decoder.ReadVariant(name),
                "opc:DiagnosticInfo" => decoder.ReadDiagnosticInfo(name),
                "opc:QualifiedName" => decoder.ReadQualifiedName(name),

                //arrays

                "opc:BooleanArray" => decoder.ReadBooleanArray(name),
                "opc:SByteArray" => decoder.ReadSByteArray(name),
                "opc:ByteArray" => decoder.ReadByteArray(name),

                "opc:UInt16Array" => decoder.ReadUInt16Array(name),
                "opc:UInt32Array" => decoder.ReadUInt32Array(name),
                "opc:UInt64Array" => decoder.ReadUInt64Array(name),

                "opc:Int16Array" => decoder.ReadInt16Array(name),
                "opc:Int32Array" => decoder.ReadInt32Array(name),
                "opc:Int64Array" => decoder.ReadInt64Array(name),

                "opc:FloatArray" => decoder.ReadFloatArray(name),
                "opc:DoubleArray" => decoder.ReadDoubleArray(name),

                "opc:StringArray" => decoder.ReadStringArray(name),
                "opc:ByteStringArray" => decoder.ReadByteStringArray(name),

                "opc:GuidArray" => decoder.ReadGuidArray(name),
                "opc:DateTimeArray" => decoder.ReadDateTimeArray(name),
                "opc:LocalizedTextArray" => decoder.ReadLocalizedTextArray(name),
                "opc:NodeIdArray" => decoder.ReadNodeIdArray(name),
                "opc:ExpandedNodeIdArray" => decoder.ReadExpandedNodeIdArray(name),
                "opc:StatusCodeArray" => decoder.ReadStatusCodeArray(name),
                "opc:XmlElementArray" => decoder.ReadXmlElementArray(name),
                "opc:ExtensionObjectArray" => decoder.ReadExtensionObjectArray(name),
                "opc:DataValueArray" => decoder.ReadDataValueArray(name),
                "opc:VariantArray" => decoder.ReadVariantArray(name),
                "opc:DiagnosticInfoArray" => decoder.ReadDiagnosticInfoArray(name),
                "opc:QualifiedNameArray" => decoder.ReadQualifiedNameArray(name),


                _ => throw new Exception("Unknown type"),
            };

        }
        public static void Write(this IEncoder encode, string type, string name, object value)
        {
            switch (type)
            {
                case "opc:Boolean": encode.WriteBoolean(name, (bool)value); break;
                case "opc:Byte": encode.WriteByte(name, (byte)value); break;
                case "opc:SByte": encode.WriteSByte(name, (sbyte)value); break;

                case "opc:UInt16": encode.WriteUInt16(name, (ushort)value); break;
                case "opc:UInt32": encode.WriteUInt32(name, (uint)value); break;
                case "opc:UInt64": encode.WriteUInt64(name, (ulong)value); break;

                case "opc:Int16": encode.WriteInt16(name, (short)value); break;
                case "opc:Int32": encode.WriteInt32(name, (int)value); break;
                case "opc:Int64": encode.WriteInt64(name, (long)value); break;

                case "opc:Float": encode.WriteFloat(name, (float)value); break;
                case "opc:Double": encode.WriteDouble(name, (double)value); break;

                case "opc:String": encode.WriteString(name, (string)value); break;
                case "opc:ByteString": encode.WriteByteString(name, (byte[])value); break;

                case "opc:Guid": encode.WriteGuid(name, (Guid)value); break;
                case "opc:DateTime": encode.WriteDateTime(name, (DateTime)value); break;
                case "opc:LocalizedText": encode.WriteLocalizedText(name, (LocalizedText)value); break;
                case "opc:NodeId": encode.WriteNodeId(name, (NodeId)value); break;
                case "opc:ExpandedNodeId": encode.WriteExpandedNodeId(name, (ExpandedNodeId)value); break;
                case "opc:StatusCode": encode.WriteStatusCode(name, (StatusCode)value); break;
                case "opc:XmlElement": encode.WriteXmlElement(name, (XmlElement)value); break;
                case "opc:ExtensionObject": encode.WriteExtensionObject(name, (ExtensionObject)value); break;
                case "opc:DataValue": encode.WriteDataValue(name, (DataValue)value); break;
                case "opc:Variant": encode.WriteVariant(name, (Variant)value); break;
                case "opc:DiagnosticInfo": encode.WriteDiagnosticInfo(name, (DiagnosticInfo)value); break;
                case "opc:QualifiedName": encode.WriteQualifiedName(name, (QualifiedName)value); break;

                //arrays

                case "opc:BooleanArray": encode.WriteBooleanArray(name, (bool[])value); break;
                case "opc:ByteArray": encode.WriteByte(name, (byte)value); break;
                case "opc:SByteArray": encode.WriteSByte(name, (sbyte)value); break;

                case "opc:UInt16Array": encode.WriteUInt16(name, (ushort)value); break;
                case "opc:UInt32Array": encode.WriteUInt32(name, (uint)value); break;
                case "opc:UInt64Array": encode.WriteUInt64(name, (ulong)value); break;

                case "opc:Int16Array": encode.WriteInt16(name, (short)value); break;
                case "opc:Int32Array": encode.WriteInt32(name, (int)value); break;
                case "opc:Int64Array": encode.WriteInt64(name, (long)value); break;

                case "opc:FloatArray": encode.WriteFloat(name, (float)value); break;
                case "opc:DoubleArray": encode.WriteDouble(name, (double)value); break;

                case "opc:StringArray": encode.WriteString(name, (string)value); break;
                case "opc:ByteStringArray": encode.WriteByteString(name, (byte[])value); break;

                case "opc:GuidArray": encode.WriteGuid(name, (Guid)value); break;
                case "opc:DateTimeArray": encode.WriteDateTime(name, (DateTime)value); break;
                case "opc:LocalizedTextArray": encode.WriteLocalizedText(name, (LocalizedText)value); break;
                case "opc:NodeIdArray": encode.WriteNodeId(name, (NodeId)value); break;
                case "opc:ExpandedNodeIdArray": encode.WriteExpandedNodeId(name, (ExpandedNodeId)value); break;
                case "opc:StatusCodeArray": encode.WriteStatusCode(name, (StatusCode)value); break;
                case "opc:XmlElementArray": encode.WriteXmlElement(name, (XmlElement)value); break;
                case "opc:ExtensionObjectArray": encode.WriteExtensionObject(name, (ExtensionObject)value); break;
                case "opc:DataValueArray": encode.WriteDataValue(name, (DataValue)value); break;
                case "opc:VariantArray": encode.WriteVariant(name, (Variant)value); break;
                case "opc:DiagnosticInfoArray": encode.WriteDiagnosticInfo(name, (DiagnosticInfo)value); break;
                case "opc:QualifiedNameArray": encode.WriteQualifiedName(name, (QualifiedName)value); break;

                default: throw new Exception("Unknown type");
            }

        }



        public static object StringToObject(string type, string value)
        {
            if (type.StartsWith("opc:"))
            {
                switch (type)
                {
                    case "opc:Boolean": return bool.Parse(value);
                    case "opc:Byte": return byte.Parse(value);
                    case "opc:SByte": return sbyte.Parse(value);

                    case "opc:UInt16": return ushort.Parse(value);
                    case "opc:UInt32": return uint.Parse(value);
                    case "opc:UInt64": return ulong.Parse(value);

                    case "opc:Int16": return short.Parse(value);
                    case "opc:Int32": return int.Parse(value);
                    case "opc:Int64": return long.Parse(value);

                    case "opc:Float": return float.Parse(value);
                    case "opc:Double": return double.Parse(value);

                    case "opc:String": return value;
                    case "opc:ByteString": return value;

                    case "opc:Guid": return Guid.Parse(value);
                    case "opc:DateTime": return DateTime.Parse(value);
                    case "opc:LocalizedText": return new LocalizedText(value);
                    case "opc:NodeId": return NodeId.Parse(value);
                    case "opc:ExpandedNodeId": return ExpandedNodeId.Parse(value);
                    case "opc:StatusCode": return new StatusCode(uint.Parse(value));
                    //case "opc:XmlElement": return new XmlElement();
                    //case "opc:ExtensionObject": return ExtensionObject.Parse(value);
                    //case "opc:DataValue": return DataValue.Parse(value);
                    //case "opc:Variant": return Variant.Parse(value);
                    //case "opc:DiagnosticInfo": return DiagnosticInfo.Parse(value);
                    case "opc:QualifiedName": return QualifiedName.Parse(value);

                    //arrays

                    case "opc:BooleanArray": return value.Split(';').Select(x => bool.Parse(x)).ToArray();
                    case "opc:ByteArray": return value.Split(';').Select(x => byte.Parse(x)).ToArray();
                    case "opc:SByteArray": return value.Split(';').Select(x => sbyte.Parse(x)).ToArray();

                    case "opc:UInt16Array": return value.Split(';').Select(x => ushort.Parse(x)).ToArray();
                    case "opc:UInt32Array": return value.Split(';').Select(x => uint.Parse(x)).ToArray();
                    case "opc:UInt64Array": return value.Split(';').Select(x => ulong.Parse(x)).ToArray();

                    case "opc:Int16Array": return value.Split(';').Select(x => short.Parse(x)).ToArray();
                    case "opc:Int32Array": return value.Split(';').Select(x => int.Parse(x)).ToArray();
                    case "opc:Int64Array": return value.Split(';').Select(x => long.Parse(x)).ToArray();

                    case "opc:FloatArray": return value.Split(';').Select(x => float.Parse(x)).ToArray();
                    case "opc:DoubleArray": return value.Split(';').Select(x => double.Parse(x)).ToArray();

                    case "opc:StringArray": return value.Split(';');
                    case "opc:ByteStringArray": return value.Split(';').Select(x => x.ToCharArray()).ToArray();

                    case "opc:GuidArray": return value.Split(';').Select(x => Guid.Parse(x)).ToArray();
                    case "opc:DateTimeArray": return value.Split(';').Select(x => DateTime.Parse(x)).ToArray();
                    case "opc:LocalizedTextArray": return value.Split(';').Select(x => new LocalizedText(x)).ToArray();
                    case "opc:NodeIdArray": return value.Split(';').Select(x => NodeId.Parse(x)).ToArray();
                    case "opc:ExpandedNodeIdArray": return value.Split(';').Select(x => ExpandedNodeId.Parse(x)).ToArray();
                    case "opc:StatusCodeArray": return value.Split(';').Select(x => new StatusCode(uint.Parse(value))).ToArray();
                    //case "opc:XmlElementArray": return value.Split(';').Select(x => XmlElement.Parse(x)).ToArray();
                    //case "opc:ExtensionObjectArray": return value.Split(';').Select(x => ExtensionObject.Parse(x)).ToArray();
                    //case "opc:DataValueArray": return value.Split(';').Select(x => DataValue.Parse(x)).ToArray();
                    //case "opc:VariantArray": return value.Split(';').Select(x => Variant.Parse(x)).ToArray();
                    //case "opc:DiagnosticInfoArray": return value.Split(';').Select(x => DiagnosticInfo.Parse(x)).ToArray();
                    case "opc:QualifiedNameArray": return value.Split(';').Select(x => QualifiedName.Parse(x)).ToArray();

                    default: throw new Exception("Unknown type");
                }
            }
            else
            {
                return value;
            }
        }

        public static object StringToObject(Type type, string value)
        {
            return type.FullName switch
            {
                "System.Boolean" => bool.Parse(value),
                "System.Byte" => byte.Parse(value),
                "System.SByte" => sbyte.Parse(value),

                "System.UInt16" => ushort.Parse(value),
                "System.UInt32" => uint.Parse(value),
                "System.UInt64" => ulong.Parse(value),

                "System.Int16" => short.Parse(value),
                "System.Int32" => int.Parse(value),
                "System.Int64" => long.Parse(value),

                "System.Single" => float.Parse(value),
                "System.Double" => double.Parse(value),

                "System.String" => value,
                "System.Char[]" => value.ToCharArray(),

                "System.Guid" => Guid.Parse(value),
                "System.DateTime" => DateTime.Parse(value),
                "Opc.Ua.LocalizedText" => new LocalizedText(value),
                "Opc.Ua.NodeId" => NodeId.Parse(value),
                "Opc.Ua.ExpandedNodeId" => ExpandedNodeId.Parse(value),
                "Opc.Ua.StatusCode" => new StatusCode(uint.Parse(value)),
                //"opc:XmlElement" =>  new XmlElement(),
                //"opc:ExtensionObject"=>  ExtensionObject.Parse(value),
                //"opc:DataValue"=> DataValue.Parse(value),
                //"opc:Variant"=>  Variant.Parse(value),
                //"opc:DiagnosticInfo"=> new DiagnosticInfo(value),
                "Opc.Ua.QualifiedName" => QualifiedName.Parse(value),

                //arrays

                "System.Boolean[]" => value.Split(';').Select(x => bool.Parse(x)).ToArray(),
                "System.Byte[]" => value.Split(';').Select(x => byte.Parse(x)).ToArray(),
                "System.SByte[]" => value.Split(';').Select(x => sbyte.Parse(x)).ToArray(),

                "System.UInt16[]" => value.Split(';').Select(x => ushort.Parse(x)).ToArray(),
                "System.UInt32[]" => value.Split(';').Select(x => uint.Parse(x)).ToArray(),
                "System.UInt64[]" => value.Split(';').Select(x => ulong.Parse(x)).ToArray(),

                "System.Int16[]" => value.Split(';').Select(x => short.Parse(x)).ToArray(),
                "System.Int32[]" => value.Split(';').Select(x => int.Parse(x)).ToArray(),
                "System.Int64[]" => value.Split(';').Select(x => long.Parse(x)).ToArray(),

                "System.Single[]" => value.Split(';').Select(x => float.Parse(x)).ToArray(),
                "System.Double[]" => value.Split(';').Select(x => double.Parse(x)).ToArray(),

                "System.String[]" => value.Split(';'),
                "System.Char[][]" => value.Split(';').Select(x => x.ToCharArray()).ToArray(),

                "System.Guid[]" => value.Split(';').Select(x => Guid.Parse(x)).ToArray(),
                "System.DateTime[]" => value.Split(';').Select(x => DateTime.Parse(x)).ToArray(),
                "Opc.Ua.LocalizedText[]" => value.Split(';').Select(x => new LocalizedText(x)).ToArray(),
                "Opc.Ua.NodeId[]" => value.Split(';').Select(x => NodeId.Parse(x)).ToArray(),
                "Opc.Ua.ExpandedNodeId[]" => value.Split(';').Select(x => ExpandedNodeId.Parse(x)).ToArray(),
                "Opc.Ua.StatusCode[]" => value.Split(';').Select(x => new StatusCode(uint.Parse(x))).ToArray(),
                //"opc:XmlElementArray"=> value.Split(';').Select(x => XmlElement.Parse(x)).ToArray(),
                //"opc:ExtensionObjectArray"=> value.Split(';').Select(x => ExtensionObject.Parse(x)).ToArray(),
                //"opc:DataValueArray"=> value.Split(';').Select(x => DataValue.Parse(x)).ToArray(),
                //"opc:VariantArray"=> value.Split(';').Select(x => Variant.Parse(x)).ToArray(),
                //"opc:DiagnosticInfoArray"=> value.Split(';').Select(x => DiagnosticInfo.Parse(x)).ToArray(),
                "Opc.Ua.QualifiedName[]" => value.Split(';').Select(x => QualifiedName.Parse(x)).ToArray(),

                _ => throw new Exception("Unknown type"),
            };
        }
        public static object StringToObject(NodeId nodeId, string value)
        {
            if (nodeId.IdType == IdType.Numeric)
            {
                switch ((uint)nodeId.Identifier)
                {
                    case DataTypes.Boolean: return bool.Parse(value);
                    case DataTypes.Byte: return byte.Parse(value);
                    case DataTypes.SByte: return sbyte.Parse(value);

                    case DataTypes.UInt16: return ushort.Parse(value);
                    case DataTypes.UInt32: return uint.Parse(value);
                    case DataTypes.UInt64: return ulong.Parse(value);

                    case DataTypes.Int16: return short.Parse(value);
                    case DataTypes.Int32: return int.Parse(value);
                    case DataTypes.Int64: return long.Parse(value);

                    case DataTypes.Float: return float.Parse(value);
                    case DataTypes.Double: return double.Parse(value);

                    case DataTypes.String: return value;
                    case DataTypes.ByteString: return value;

                    case DataTypes.Guid: return Guid.Parse(value);
                    case DataTypes.DateTime: return DateTime.Parse(value);
                    case DataTypes.LocalizedText: return new LocalizedText(value);
                    case DataTypes.NodeId: return NodeId.Parse(value);
                    case DataTypes.ExpandedNodeId: return ExpandedNodeId.Parse(value);
                    case DataTypes.StatusCode: return new StatusCode(uint.Parse(value));
                    //case "opc:XmlElement": return new XmlElement();
                    //case "opc:ExtensionObject": return ExtensionObject.Parse(value);
                    //case "opc:DataValue": return DataValue.Parse(value);
                    //case "opc:Variant": return Variant.Parse(value);
                    //case "opc:DiagnosticInfo": return DiagnosticInfo.Parse(value);
                    case DataTypes.QualifiedName: return QualifiedName.Parse(value);

                    default: throw new Exception("Unknown type");
                }
            }
            return value;
        }
        public static string NodeTypeString(Node node)
        {
            string data = "unkwon";
            if (node is VariableNode varNode)
            {
                data = NodeTypeString(varNode.DataType);
            }
            else if (node is VariableTypeNode varTypeNode)
            {
                data = NodeTypeString(varTypeNode.NodeId);
            }
            return data;
        }
        public static string NodeTypeString(ExpandedNodeId nodeTypeId)
            => NodeTypeString((NodeId)nodeTypeId);
        public static string NodeTypeString(NodeId nodeTypeId)
        {
            if (nodeTypeId.IdType == IdType.Numeric)
            {
                switch ((uint)nodeTypeId.Identifier)
                {
                    case DataTypes.Boolean: return "Boolean";
                    case DataTypes.Byte: return "Byte";
                    case DataTypes.SByte: return "SByte";

                    case DataTypes.UInt16: return "UInt16";
                    case DataTypes.UInt32: return "UInt32";
                    case DataTypes.UInt64: return "UInt64";

                    case DataTypes.Int16: return "Int16";
                    case DataTypes.Int32: return "Int32";
                    case DataTypes.Int64: return "Int64";

                    case DataTypes.Float: return "Float";
                    case DataTypes.Double: return "Double";

                    case DataTypes.String: return "String";
                    case DataTypes.ByteString: return "ByteString";

                    case DataTypes.Guid: return "Guid";
                    case DataTypes.DateTime: return "DateTime";
                    case DataTypes.LocalizedText: return "LocalizedText";
                    case DataTypes.NodeId: return "NodeId";
                    case DataTypes.ExpandedNodeId: return "ExpandedNodeId";
                    case DataTypes.StatusCode: return "StatusCode";

                    case DataTypes.XmlElement: return "XmlElement";
                    //case "opc:ExtensionObject": return ExtensionObject.Parse(value);
                    case DataTypes.DataValue: return "DataValue";
                    case DataTypes.VariableNode: return "VariableNode";
                    case DataTypes.DiagnosticInfo: return "DiagnosticInfo";
                    //case "opc:XmlElement": return new XmlElement();

                    //case "opc:DataValue": return DataValue.Parse(value);
                    //case "opc:Variant": return Variant.Parse(value);
                    //
                    case DataTypes.QualifiedName: return "QualifiedName";
                }
            }

            return "unkown";
        }
    }
}