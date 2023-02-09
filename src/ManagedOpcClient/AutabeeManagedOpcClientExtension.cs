using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Utility.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Autabee.Communication.ManagedOpcClient
{
    public static partial class AutabeeManagedOpcClientExtension
    {
        #region xml
        public static string[] GetServerTypeSchema(this AutabeeManagedOpcClient client, bool all = false)
        {
            if (!client.Connected) throw new Exception("Not Connected");
            ReferenceDescriptionCollection refDescColBin;
            ReferenceDescriptionCollection refDescColXml;
            byte[] continuationPoint;

            ResponseHeader BinaryNodes = client.Session.Browse(
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
            ResponseHeader XMLNodes = client.Session.Browse(
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
            var values = client.ReadValues(nodeIds, null);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] != null) { result.Add(Encoding.ASCII.GetString((byte[])values[i].Value)); }
            }
            //result.RemoveAll(o => string.IsNullOrEmpty(o));

            return result.ToArray();
        }



        #endregion xml


        #region Typing
        public static NodeTypeData GetNodeTypeEncoding(this AutabeeManagedOpcClient client, ExpandedNodeId nodeId)
        {
            return client.GetNodeTypeEncoding(nodeId.ToString());
        }
        public static NodeTypeData GetNodeTypeEncoding(this AutabeeManagedOpcClient client, NodeId nodeId)
        {
            return client.GetNodeTypeEncoding(nodeId.ToString());
        }

        public static NodeTypeData GetNodeTypeEncoding(this AutabeeManagedOpcClient client, Node node)
        {
            var type = ConvertOpc.NodeTypeString(node);
            if (type == "unkown")
            {
                return client.GetNodeTypeEncoding(node.NodeId.ToString());
            }
            else if (node is VariableNode varNode)
            {
                if (varNode.DataType.IdType == IdType.Numeric)
                {
                    return new NodeTypeData()
                    {
                        TypeName = "opc:" + type,
                        Name = varNode.DataType.Identifier.ToString()
                    };
                    
                }
                else
                {
                    return new NodeTypeData()
                    {
                        TypeName = "tns:" + type,
                        Name = varNode.DataType.Identifier.ToString()
                    };
                }
                
            }
            else
            {
                return new NodeTypeData()
                {
                    TypeName = "opc:" + type,
                    Name = node.NodeId.Identifier.ToString()
                };
            }

        }
        #endregion Typing

        #region Methods
        public static IList<object> CallMethod(this AutabeeManagedOpcClient client, string objectNodeString, string methodNodeString, object[] inputArguments)
            => client.CallMethod(
           new NodeId(objectNodeString),
           new NodeId(methodNodeString),
           inputArguments);

        public static IList<object> CallMethod(this AutabeeManagedOpcClient client, NodeEntry objectEntry, MethodNodeEntry methodEntry, object[] inputArguments)
            => client.CallMethod(
            objectEntry.GetNodeId(),
            methodEntry.GetNodeId(),
            inputArguments);
        #endregion
    }
}