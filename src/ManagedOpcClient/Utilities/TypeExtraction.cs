using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    public static class TypeExtraction
    {
        static string GetCorrectedName(XmlNode value)
        {
            return value.Attributes["Name"].Value.Replace("&quot;", string.Empty)
                    .Replace("\"", string.Empty);
        }
        static string GetCorrectedTypeName(XmlNode value)
        {
            return GetCorrectedTypeName(value.Attributes["TypeName"].Value);
        }

        static string GetCorrectedTypeName(ExtensionObject eoValue)
        {
            return eoValue.TypeId.Identifier.ToString().Replace("\"", string.Empty).Replace("TE_", string.Empty);
        }

        static string GetCorrectedTypeName(string value)
        {
            if (value.Contains("tns:"))
            {
                return value
                    .Replace("&quot;", string.Empty)
                    .Replace("\"", string.Empty)
                    .Substring(4);
            }

            return value;
        }




        public static object FormatObject(this AutabeeManagedOpcClient autabeeManagedOpcClient, ExtensionObject eoValue)
        {
            if (eoValue.Encoding == ExtensionObjectEncoding.EncodeableObject
                || eoValue.Encoding == ExtensionObjectEncoding.None)
                return eoValue.Body;

            var type = autabeeManagedOpcClient.GetTypeEncoding(GetCorrectedTypeName(eoValue));
            return eoValue.Encoding switch
            {
                ExtensionObjectEncoding.Binary => type.Decode(new BinaryDecoder((byte[])eoValue.Body, autabeeManagedOpcClient.Session.MessageContext)),
                ExtensionObjectEncoding.Xml => type.Decode(new XmlDecoder((XmlElement)eoValue.Body, autabeeManagedOpcClient.Session.MessageContext)),
                ExtensionObjectEncoding.Json => type.Decode(new JsonDecoder((string)eoValue.Body, autabeeManagedOpcClient.Session.MessageContext)),
                _ => throw new Exception("Unknown encoding"),
            };
        }

        public static Dictionary<string, NodeTypeData> UpdateNodeType(List<XmlDocument> xmls)
        {
            var dict = new Dictionary<string, NodeTypeData>();

            foreach (var item in xmls)
            {
                foreach (XmlNode child in item.GetElementsByTagName("opc:StructuredType"))
                {
                    var nodeType = new NodeTypeData();
                    nodeType.Name = GetCorrectedName(child);
                    nodeType.TypeName = GetCorrectedName(child);
                    nodeType.ChildData = new List<NodeTypeData>();
                    foreach (XmlNode child2 in child.ChildNodes)
                    {
                        if (child2.Name == "opc:Field")
                        {
                            var childNode = new NodeTypeData();
                            childNode.Name = GetCorrectedName(child2);
                            childNode.TypeName = GetCorrectedTypeName(child2);
                            childNode.ChildData = new List<NodeTypeData>();
                            nodeType.ChildData.Add(childNode);
                        }
                    }
                    dict.Add(nodeType.Name, nodeType);
                }
            }

            foreach (var item in dict)
            {
                var type = item.Value;
                AddChildren(dict, type);
            }
            return dict;
        }

        public static void AddChildren(Dictionary<string, NodeTypeData> dict, NodeTypeData type)
        {
            for (int i = 0; i < type.ChildData.Count; i++)
            {
                var child = type.ChildData[i];
                if (dict.ContainsKey(child.TypeName))
                {
                    type.ChildData[i].ChildData = dict[child.TypeName].ChildData;
                    foreach (var item in type.ChildData[i].ChildData)
                    {
                        AddChildren(dict, item);
                    }
                }
            }
        }

        public static object GetCorrectValue(this AutabeeManagedOpcClient autabeeManagedOpcClient, object value)
        {
            if (value is ExtensionObject eoValue)
            {
                return FormatObject(autabeeManagedOpcClient ,eoValue);
            }
            else if (value is ExtensionObject[] eoValues)
            {
                return eoValues.Select(o=> FormatObject(autabeeManagedOpcClient,o)).ToArray();
            }
            return value;
        }



        public static string GetTypeDictionary(string nodeIdString, Session session)
        {
            //Read the desired node first and check if it's a variable
            Node node = session.ReadNode(nodeIdString);
            if (node.NodeClass != NodeClass.Variable)
            {
                Exception ex = new Exception("Selected node is not a variable");
                throw ex;
            }
            //Get the node id of node's data type
            VariableNode variableNode = (VariableNode)node.DataLock;
            NodeId nodeId = (NodeId)variableNode.DataType;

            //Browse for HasEncoding
            ReferenceDescriptionCollection refDescCol;
            //byte[] continuationPoint;
            session.Browse(null, null, nodeId, 0u, BrowseDirection.Forward, ReferenceTypeIds.HasEncoding, true, 0, out _, out refDescCol);

            //Check For found reference
            if (refDescCol.Count == 0)
            {
                Exception ex = new Exception("No data type to encode. Could be a build-in data type you want to read.");
                throw ex;
            }

            //Check for HasEncoding reference with name "Default Binary"
            var tmp = refDescCol.FirstOrDefault(o => o.DisplayName.Text == "Default Binary");
            if (tmp == null)
                tmp = refDescCol.FirstOrDefault(o => o.DisplayName.Text == "Default XML");
            if (tmp == null)
                tmp = refDescCol.FirstOrDefault(o => o.DisplayName.Text == "Default JSON");

            if (tmp == null)
                throw new Exception("No encoding found.");
            nodeId = (NodeId)tmp.NodeId;

            //Browse for HasDescription
            ReferenceDescriptionCollection refDescCol2;
            session.Browse(null, null, nodeId, 0u, BrowseDirection.Forward, ReferenceTypeIds.HasDescription, true, 0, out _, out refDescCol2);

            //Check For found reference
            if (refDescCol2.Count > 0)
            {
                nodeId = new NodeId(refDescCol2[0].NodeId.Identifier, refDescCol2[0].NodeId.NamespaceIndex);
                DataValue resultValue = session.ReadValue(nodeId);
                return resultValue.Value.ToString();

            }
            else
            {
                //var resultValue = session.ReadValue();
                return nodeId.ToString();
            }

        }


        public static Dictionary<string, NodeTypeData> GetOpcNodeTypeData(List<XmlDocument> xmls)
        {
            var dict = new Dictionary<string, NodeTypeData>();

            foreach (var item in xmls)
            {
                foreach (XmlNode child in item.GetElementsByTagName("opc:StructuredType"))
                {
                    var nodeType = new NodeTypeData();
                    nodeType.Name = GetCorrectedName(child);
                    nodeType.TypeName = GetCorrectedName(child);
                    nodeType.ChildData = new List<NodeTypeData>();
                    foreach (XmlNode child2 in child.ChildNodes)
                    {
                        if (child2.Name == "opc:Field")
                        {
                            var childNode = new NodeTypeData();
                            childNode.Name = GetCorrectedName(child2);
                            childNode.TypeName = GetCorrectedTypeName(child2);
                            childNode.ChildData = new List<NodeTypeData>();
                            nodeType.ChildData.Add(childNode);
                        }
                    }
                    dict.Add(nodeType.Name, nodeType);
                }
            }

            foreach (var item in dict)
            {
                var type = item.Value;
                AddOpcNodeTypeDataChildren(dict, type);
            }
            return dict;
        }

        public static void AddOpcNodeTypeDataChildren(Dictionary<string, NodeTypeData> dict, NodeTypeData type)
        {
            for (int i = 0; i < type.ChildData.Count; i++)
            {
                var child = type.ChildData[i];
                if (dict.ContainsKey(child.TypeName))
                {
                    type.ChildData[i].ChildData = dict[child.TypeName].ChildData;
                    foreach (var item in type.ChildData[i].ChildData)
                    {
                        AddChildren(dict, item);
                    }
                }
            }
        }


        public static string[] GetServerTypeSchema(this Session session, bool all = false)
        {
            if (!session.Connected) throw new Exception("Not Connected");
            ReferenceDescriptionCollection refDescColBin;
            ReferenceDescriptionCollection refDescColXml;
            byte[] continuationPoint;

            ResponseHeader BinaryNodes = session.Browse(
                null,
                null,
                ObjectIds.OPCBinarySchema_TypeSystem,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                0,
                out continuationPoint,
                out refDescColBin);

            ResponseHeader XMLNodes = session.Browse(
                null,
                null,
                ObjectIds.XmlSchema_TypeSystem,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                0,
                out continuationPoint,
                out refDescColXml);

            NodeIdCollection nodeIds = new NodeIdCollection();
            foreach (var item in refDescColBin)
            {
                if (!item.DisplayName.Text.StartsWith("Opc.Ua") || all) nodeIds.Add((NodeId)item.NodeId);
            }

            foreach (var xmlItem in refDescColXml)
            {
                if (refDescColBin.FirstOrDefault(o => o.DisplayName.Text == xmlItem.DisplayName.Text) == null)
                {
                    nodeIds.Add((NodeId)xmlItem.NodeId);
                }
            }

            var result = new List<string>();
            session.ReadValues(nodeIds, out var values, out var errors);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] != null && values[i].Value != null) { result.Add(Encoding.ASCII.GetString((byte[])values[i].Value)); }
            }
            //result.RemoveAll(o => string.IsNullOrEmpty(o));

            return result.ToArray();
        }


        public static Dictionary<string, NodeTypeData> GetNodeTypeDataCashe(Session session, List<XmlDocument> xmls)
        {
            xmls.Clear();
            xmls.AddRange(session.GetServerTypeSchema(true).Select(o => { var temp = new XmlDocument(); temp.LoadXml(o); return temp; }));
            var dict = TypeExtraction.UpdateNodeType(xmls);
            return dict;
        }
    }
}
