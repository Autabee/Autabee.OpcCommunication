using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Communication.ManagedOpcClient.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua.Export;
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
using static System.Collections.Specialized.BitVector32;

namespace Autabee.Communication.ManagedOpcClient
{
    public static partial class AutabeeManagedOpcClientExtension
    {
        #region Defaults
        public static ApplicationConfiguration GetClientConfiguration(
            string company, string product, string directory, Serilog.Core.Logger logger = null)
        {
            var error = new System.Collections.Generic.List<Exception>();
            if (string.IsNullOrWhiteSpace(company)) error.Add(new ArgumentNullException(nameof(company)));
            if (string.IsNullOrWhiteSpace(product)) error.Add(new ArgumentNullException(nameof(product)));
            if (string.IsNullOrWhiteSpace(directory)) error.Add(new ArgumentNullException(nameof(directory)));
            if (error.Count() != 0) { throw new AggregateException(error); }

            ApplicationInstance configuration = new ApplicationInstance();
            configuration.ApplicationType = ApplicationType.Client;
            configuration.ConfigSectionName = product;

            var combined = Path.Combine(directory, product + ".Config.xml");

            if (!File.Exists(combined))
            {
                CreateDefaultConfiguration(company, product, directory, logger, combined);
            }

            configuration.LoadApplicationConfiguration(combined, false).Wait();
            configuration.CheckApplicationInstanceCertificate(false, 0).Wait();

            return configuration.ApplicationConfiguration;
        }

        internal static void CreateDefaultConfiguration(string company, string product, string directory, Serilog.Core.Logger logger, string combined)
        {
            logger?.Warning("File {0} not found. recreating it using embedded default.", null, combined);
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Autabee.Communication.ManagedOpcClient.DefaultOpcClient.Config.xml"))
            {
                using (StreamReader reader = new StreamReader(resource))
                {
                    string result = reader.ReadToEnd();
                    result = result.Replace("productref", product);
                    result = result.Replace("companyref", company);
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(combined, result);
                    logger?.Warning("File {0} Created and updated with ({1}, {2}).", null, combined, product, company);

                }
            }
        }

        public static ApplicationConfiguration CreateDefaultClientConfiguration(Stream configStream)
        {

            ApplicationInstance configuration = new ApplicationInstance();
            configuration.ApplicationType = ApplicationType.Client;
            configuration.LoadApplicationConfiguration(configStream, false).Wait();
            configuration.CheckApplicationInstanceCertificate(false, 2048).Wait();

            return configuration.ApplicationConfiguration;
        }

        public static X509Certificate2 CreateDefaultClientCertificate(ApplicationConfiguration configuration)
        {
            // X509Certificate2 clientCertificate;
            ICertificateBuilder builder = CertificateBuilder.Create($"cn={configuration.ApplicationName}");
            builder = builder.SetHashAlgorithm(System.Security.Cryptography.HashAlgorithmName.SHA256);
            builder = (ICertificateBuilder)builder.SetRSAKeySize(2048);
            builder = builder.SetLifeTime(24);
            builder = builder.CreateSerialNumber();
            builder = builder.SetNotBefore(DateTime.Now);

#if NET48_OR_GREATER || NET5_0_OR_GREATER
            builder = builder.AddExtension(GetLocalIpData(configuration.ApplicationUri));
#endif
            
            var clientCertificate = builder.CreateForRSA();

            clientCertificate.AddToStore(
                configuration.SecurityConfiguration.ApplicationCertificate.StoreType,
                configuration.SecurityConfiguration.ApplicationCertificate.StorePath);
            return clientCertificate;
        }
        #endregion Defaults

#if NET48_OR_GREATER || NET6_0_OR_GREATER
        private static X509Extension GetLocalIpData(string applicationUri)
        {
            var abuilder = new SubjectAlternativeNameBuilder();
            List<string> localIps = new List<string>();
            abuilder.AddUri(new Uri(applicationUri));
            abuilder.AddDnsName(Dns.GetHostName());
            var host = Dns.GetHostEntry(Dns.GetHostName());
            bool found = false;
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    abuilder.AddIpAddress(ip);
                    found = true;
                }
            }
            if (!found)
            {
                throw new Exception("Local IP Address Not Found!");
            }
            return abuilder.Build();
        }
#endif

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

        #region Browsing
        
        public static ReferenceDescriptionCollection BrowseNode(this AutabeeManagedOpcClient client, ReferenceDescription refDesc) 
            =>  client.Session != null
                ? client.BrowseNode(ExpandedNodeId.ToNodeId(refDesc.NodeId, client.Session.NamespaceUris))
                : throw new Exception("BadNotConnected");

        public static ReferenceDescriptionCollection BrowseNode(this AutabeeManagedOpcClient client, NodeId node) => client.BrowseNode(
            new BrowseDescriptionCollection() { BrowseHelperFunctions.GetChildrenBrowseDescription(node) });

        public static async Task<ReferenceDescriptionCollection> AsyncBrowseNode(this AutabeeManagedOpcClient client,
            ReferenceDescription node,CancellationToken token)
            =>  client.Session != null
                ? await client.AsyncBrowseNode(ExpandedNodeId.ToNodeId(node.NodeId, client.Session.NamespaceUris), token)
                : throw new Exception("BadNotConnected");


        public static async Task<ReferenceDescriptionCollection> AsyncBrowseNode(this AutabeeManagedOpcClient client, NodeId node, CancellationToken token)
        {
            BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection()
            {
                BrowseHelperFunctions.GetChildrenBrowseDescription(node)
            };
            return await client.AsyncBrowseNode(nodesToBrowse, token);
        }
        #endregion


        #region 
        public static ReferenceDescriptionCollection BrowseRoot(this AutabeeManagedOpcClient client)
            => client.BrowseNode(new BrowseDescriptionCollection() { BrowseHelperFunctions.GetChildrenBrowseDescription(ObjectIds.RootFolder) });


        public static ReferenceDescription GetParent(this AutabeeManagedOpcClient client, NodeId nodeId)
        {
            BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection()
            {
                BrowseHelperFunctions.GetParentBrowseDescription(nodeId)
            };
            var referenceDescriptionCollection = client.BrowseNode(nodesToBrowse);
            //Guard Clause
            if (referenceDescriptionCollection == null || referenceDescriptionCollection.Count == 0)
            {
                throw new Exception("No Parent Id Found");
            }
            if (referenceDescriptionCollection.Count > 1)
            {
                throw new Exception("Multiple Parent Id Found");
            }
            return referenceDescriptionCollection[0];
        }
        #endregion

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
            if (node is VariableNode varNode1)
            {
                try
                {
                    var type2 = Opc.Ua.TypeInfo.GetSystemType(varNode1.DataType, client.Session.Factory);
                    if (type2 != null)
                    {
                        return new NodeTypeData(type2);
                    }
                }
                catch
                {
                }
            }

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

        #region ReadValue
        public static object ReadValue(this AutabeeManagedOpcClient client, string nodeIdString)
            => client.ReadValue(new NodeId(nodeIdString));

        public static object ReadValue(this AutabeeManagedOpcClient client, ExpandedNodeId nodeId, Type type = null)
            => client.ReadValue((NodeId)nodeId, type);

        public static T ReadValue<T>(this AutabeeManagedOpcClient client, string nodeIdString)
            => client.ReadValue<T>(new NodeId(nodeIdString));
        #endregion

        #region ReadNode
        public static Node ReadNode(this AutabeeManagedOpcClient client, string nodeIdString)
            => client.ReadNode(new NodeId(nodeIdString));
        public static Node ReadNode(this AutabeeManagedOpcClient client, ExpandedNodeId nodeId)
            => client.ReadNode(ExpandedNodeId.ToNodeId(nodeId, client.Session.NamespaceUris));
        public static Node ReadNode(this AutabeeManagedOpcClient client, ReferenceDescription reference)
            => client.ReadNode(ExpandedNodeId.ToNodeId(reference.NodeId, client.Session.NamespaceUris));

        public static NodeCollection ReadNodes(this AutabeeManagedOpcClient client, ReferenceDescriptionCollection referenceDescriptions)
        {
            NodeIdCollection nodeIds = new NodeIdCollection();
            nodeIds.AddRange(referenceDescriptions.Select(o => ExpandedNodeId.ToNodeId(o.NodeId, client.Session.NamespaceUris)));
            return client.ReadNodes(nodeIds);
        }
        #endregion

        #region Methods
        public static IList<object> CallMethod(this AutabeeManagedOpcClient client, string objectNodeString, string methodNodeString, params object[] inputArguments)
            => client.CallMethod(
           new NodeId(objectNodeString),
           new NodeId(methodNodeString),
           inputArguments);

        public static IList<object> CallMethod(this AutabeeManagedOpcClient client, string objectNodeString, MethodNodeEntry methodEntry, params object[] inputArguments)
            => client.CallMethod(
           new NodeId(objectNodeString),
           methodEntry.GetNodeId(),
           inputArguments);

        public static IList<object> CallMethod(this AutabeeManagedOpcClient client, NodeEntry objectEntry, string methodNodeString, params object[] inputArguments)
            => client.CallMethod(
           objectEntry.GetNodeId(),
           new NodeId(methodNodeString),
           inputArguments);

        public static IList<object> CallMethod(this AutabeeManagedOpcClient client, NodeEntry objectEntry, MethodNodeEntry methodEntry, params object[] inputArguments)
            => client.CallMethod(
            objectEntry.GetNodeId(),
            methodEntry.GetNodeId(),
            inputArguments);

        public static IList<object> CallMethod(this AutabeeManagedOpcClient client, NodeId objectNodeId, NodeId methodNodeId, ArgumentCollection inputArguments)
            => client.CallMethod(
            objectNodeId,
            methodNodeId,
            inputArguments == null ? new object[0] : inputArguments.Select(o => o.Value).ToArray());



        [Obsolete("Use CallMethod with parent node and method node instead as this does not require a session browse call.")]
        public static IList<object> CallMethod(this AutabeeManagedOpcClient client, NodeId methodNodeId, params object[] args)
        => client.CallMethod(
            (NodeId)client.GetParent(methodNodeId).NodeId,
            methodNodeId,
            args ?? new object[0]);



        public static IList<object> CallMethods(this AutabeeManagedOpcClient client, IEnumerable<(NodeEntry, MethodNodeEntry, object[])> data)
        {
            var methodRequests = new CallMethodRequestCollection();
            methodRequests.AddRange(
                data.Select(
                    o =>
                    {
                        var collection = new VariantCollection();
                        collection.AddRange(o.Item3.Select(k => new Variant(k)));
                        return new CallMethodRequest()
                        {
                            ObjectId = o.Item1.GetNodeId(),
                            MethodId = o.Item2.GetNodeId(),
                            InputArguments = collection
                        };
                    }));
            return client.CallMethods(methodRequests);
        }
        #endregion




        #region pupsub
        
        public static MonitoredItem AddMonitoredItem(this AutabeeManagedOpcClient client,
                TimeSpan publishingInterval,
                ValueNodeEntry nodeEntry,
                MonitoredNodeValueRecordEventHandler handler = null) => client.AddMonitoredItem(
                client.GetSubscription(publishingInterval),
                nodeEntry,
                handler);

        public static MonitoredItem AddMonitoredItem(this AutabeeManagedOpcClient client,
                TimeSpan publishingInterval,
                NodeId nodeId,
                MonitoredNodeValueEventHandler handler = null) => client.AddMonitoredItem(
                client.GetSubscription(publishingInterval),
                nodeId,
                handler);
        public static MonitoredItem AddMonitoredItem(this AutabeeManagedOpcClient client,
                int publishingIntervalMilliSec,
                ValueNodeEntry nodeEntry,
                MonitoredNodeValueRecordEventHandler handler = null) => client.AddMonitoredItem(
                client.GetSubscription(publishingIntervalMilliSec),
                nodeEntry,
                handler);
        public static MonitoredItem AddMonitoredItem(this AutabeeManagedOpcClient client,
                int publishingIntervalMilliSec,
                NodeId nodeId,
                MonitoredNodeValueEventHandler handler = null) => client.AddMonitoredItem(
                client.GetSubscription(publishingIntervalMilliSec),
                nodeId,
                handler);

        public static IEnumerable<MonitoredItem> AddMonitoredItems(this AutabeeManagedOpcClient client, TimeSpan publishingInterval, ValueNodeEntryCollection nodeEntrys)
            => client.AddMonitoredItems(
                client.GetSubscription(publishingInterval),
                nodeEntrys);

        public static IEnumerable<MonitoredItem> AddMonitoredItems(this AutabeeManagedOpcClient client, int publishingIntervalMilliSec, ValueNodeEntryCollection nodeEntrys)
            => client.AddMonitoredItems(
                client.GetSubscription(publishingIntervalMilliSec),
                nodeEntrys);

        public static Subscription GetSubscription(this AutabeeManagedOpcClient client, TimeSpan publishingInterval) => client.GetSubscription(publishingInterval.Milliseconds);


        #endregion
    }
}