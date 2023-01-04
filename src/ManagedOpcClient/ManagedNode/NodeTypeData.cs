using System;
using System.Collections.Generic;
using System.Text;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{

  public enum OpcNodeTypeId
  {
    opcUnkown,
    opcBoolean,
    opcInt16,
    opcInt32,
    opcFloat,
    opcDouble,
    opcString,
    opcCharArray,
    opcUInt16,
    opcUInt32,
    opcInt64,
    opcUInt64,
    opcByte,
    opcUdt
  }
  public static class OpcNodeTypeIdExtension
  {
    public static int ByteLenght(this OpcNodeTypeId value)
    {
      switch (value)
      {
        case OpcNodeTypeId.opcBoolean:
        case OpcNodeTypeId.opcByte:
          return 1;
        case OpcNodeTypeId.opcInt16:
        case OpcNodeTypeId.opcUInt16:
          return 2;
        case OpcNodeTypeId.opcInt32:
        case OpcNodeTypeId.opcUInt32:
        case OpcNodeTypeId.opcFloat:
        case OpcNodeTypeId.opcString:
        case OpcNodeTypeId.opcCharArray:
          return 4;
        case OpcNodeTypeId.opcDouble:
        case OpcNodeTypeId.opcInt64:
        case OpcNodeTypeId.opcUInt64:
          return 8;
        default:
          throw new System.Exception("Struct or unkown type");
          break;
      }

    }

    public static OpcNodeTypeId GetOpcNodeTypeId(string typeName)
    {
      switch (typeName)
      {
        case "opc:Boolean": return OpcNodeTypeId.opcBoolean;
        case "opc:Byte": return OpcNodeTypeId.opcByte;
        case "opc:Int16": return OpcNodeTypeId.opcInt16;
        case "opc:UInt16": return OpcNodeTypeId.opcUInt16;
        case "opc:Int32": return OpcNodeTypeId.opcInt32;
        case "opc:UInt32": return OpcNodeTypeId.opcUInt32;
        case "opc:Float": return OpcNodeTypeId.opcFloat;
        case "opc:String": return OpcNodeTypeId.opcString;
        case "opc:CharArray": return OpcNodeTypeId.opcCharArray;
        case "opcDouble": return OpcNodeTypeId.opcDouble;
        case "opcInt64": return OpcNodeTypeId.opcInt64;
        case "opcUInt64": return OpcNodeTypeId.opcUInt64;
        default:
          if (typeName.Contains("STRUCT/UDT")) return OpcNodeTypeId.opcUdt;
          else return OpcNodeTypeId.opcUnkown;
      }
    }

  }
  public class NodeTypeData
  {
    public NodeTypeData()
    {
    }

    public NodeTypeData(NodeTypeData nodeTypeData)
    {
      Name = nodeTypeData.Name;
      TypeName = nodeTypeData.TypeName;
      OpcNodeTypeId = nodeTypeData.OpcNodeTypeId;
      ChildData = nodeTypeData.ChildData;
    }

    public OpcNodeTypeId OpcNodeTypeId { get; set; } = OpcNodeTypeId.opcUnkown;
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public List<NodeTypeData> ChildData { get; set; } = new List<NodeTypeData>();

    public NodeTypeData Copy()
    {
      return new NodeTypeData(this);
    }
  }

  //public class NodeValueData : NodeTypeData
  //{
  //  public NodeValueData() : base()
  //  {
  //  }

  //  public NodeValueData(NodeTypeData nodeTypeData) : base(nodeTypeData)
  //  {

  //  }

  //  public object Value { get; set; }

  //  public NodeValueData CopyValue()
  //  {
  //    var temp = new NodeValueData(this);
  //    temp.Value = this.Value;
  //    return temp;
  //  }
  //}

  //public class NodeStructWrite<T>
  //{
  //  public string TagName { get; set; }

  //  public OpcNodeTypeId OpcNodeTypeId { get; set; }

  //  public T Value { get; set; }
  //}

}
