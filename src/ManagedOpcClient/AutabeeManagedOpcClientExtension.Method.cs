using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Autabee.Communication.ManagedOpcClient
{
    public static partial class AutabeeManagedOpcClientExtension
    {
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
    }
}