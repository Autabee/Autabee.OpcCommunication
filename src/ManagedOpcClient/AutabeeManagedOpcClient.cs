using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Communication.ManagedOpcClient.Utilities;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Export;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Autabee.Communication.ManagedOpcClient
{
    public delegate void MonitoredNodeValueRecordEventHandler(MonitoredItem sender, NodeValueRecord e);
    public delegate void MonitoredNodeValueEventHandler(MonitoredItem sender, object e);

    public class AutabeeManagedOpcClient
    {
        private bool closing;
        private IUserIdentity mUserIdentity;
        private ApplicationConfiguration mApplicationConfig;
        private ConfiguredEndpoint mEndpoint;

        private Session session;
        private string sessionName;
        private List<Subscription> subscriptions = new List<Subscription>();
        private Logger logger;

        public List<XmlDocument> Xmls { get; private set; } = new List<XmlDocument>();
        public Dictionary<string, string> PreparedNodeTypes { get; private set; } = new Dictionary<string, string>();
        public Dictionary<string, NodeTypeData> PreparedTypes { get; private set; } = new Dictionary<string, NodeTypeData>();

        private Dictionary<string, NodeId> nodeIdCache = new Dictionary<string, NodeId>();

        /// <summary>
        /// Provides the event handling for server certificates.
        /// </summary>
        public event CertificateValidationEventHandler CertificateValidationNotification;
        internal event EventHandler ClearNodeEntries;
        public event EventHandler<OpcConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        public event EventHandler ConnectionUpdated;

        /// <summary>
        /// Provides the event for value changes of a monitored item.
        /// </summary>
        public event MonitoredItemNotificationEventHandler ItemChangedNotification;

        /// <summary>
        /// Provides the event for KeepAliveNotifications.
        /// </summary>
        public event KeepAliveEventHandler KeepAliveNotification;
        public event MonitoredNodeValueRecordEventHandler NodeChangedNotification;
        internal event EventHandler ReInstateNodeEntries;

        public ApplicationDescription ApplicationDescription { get; set; }
        public bool Connected
        {
            get
            {
                if (session == null)
                    return false;
                if (connectionState == OpcConnectionStatus.Connected
                    || connectionState == OpcConnectionStatus.Reconnecting
                    || session != null && session.Connected)
                    return true;
                else
                    return false;
            }
        }

        public Session Session { get => session; }
        //public Dictionary<NodeId, DataDictionary> NodeDictonary { get; set; }

        #region Construction
        public AutabeeManagedOpcClient(string company, string product, string directory, Logger logger = null)
        {
            this.logger = logger;
            // Create's the application configuration (containing the certificate) on construction
            mApplicationConfig = AutabeeManagedOpcClientExtension.GetClientConfiguration(
                                     company,
                                     product,
                                     directory,
                                     logger);
        }
        public AutabeeManagedOpcClient(Stream stream, Logger logger = null)
        {
            this.logger = logger;
            // Create's the application configuration (containing the certificate) on construction
            mApplicationConfig = AutabeeManagedOpcClientExtension.CreateDefaultClientConfiguration(stream);
        }

        public AutabeeManagedOpcClient(ApplicationConfiguration opcAppConfig, Logger logger = null)
        {
            this.logger = logger;
            mApplicationConfig = opcAppConfig;
        }
        #endregion Construction

        #region Registration

        public void RegisterNodeIds(ValueNodeEntryCollection preparedCollection, bool AutoReRegister = true)
        {
            var nodeList = preparedCollection.RegisteredNodeIds;
            if (nodeList.Count != 0) { UnregisterNodeIds(nodeList); }
            nodeList.Clear();

            nodeList.AddRange(RegisterNodeIds(preparedCollection.NodeIds));
            ClearNodeEntries -= preparedCollection.SessionDisconnected;
            ClearNodeEntries += preparedCollection.SessionDisconnected;
            if (AutoReRegister)
            {
                ReInstateNodeEntries -= preparedCollection.NewSessionEstablished;
                ReInstateNodeEntries += preparedCollection.NewSessionEstablished;
            }
        }

        /// <summary>
        /// Registers Node Ids to the server
        /// </summary>
        /// <param name="nodeIdStrings">The node Ids as strings</param>
        /// <returns>The registered Node Ids as strings</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public NodeIdCollection RegisterNodeIds(NodeIdCollection nodesToRegister)
        {
            NodeIdCollection registeredNodes = new NodeIdCollection();
            NodeIdCollection newNodesToRegister = new NodeIdCollection();

            for (int i = 0; i < nodesToRegister.Count; i++)
            {
                if (nodeIdCache.TryGetValue(nodesToRegister[i].ToString(), out NodeId nodeId))

                {
                    registeredNodes.Add(nodeId);
                }
                else
                {
                    registeredNodes.Add(new NodeId("temp"));
                    newNodesToRegister.Add(nodesToRegister[i]);
                }
            }

            if (newNodesToRegister.Count > 0)
            {
                var newRegister = new Stack<NodeId>(RegisterUnCashed(newNodesToRegister));

                for (int i = 0; i < registeredNodes.Count; i++)
                {
                    if (registeredNodes[i].Identifier.ToString() == "temp")
                    {
                        registeredNodes[i] = newRegister.Pop();
                    }
                }
            }

            return registeredNodes;
        }

        private NodeIdCollection RegisterUnCashed(NodeIdCollection nodesToRegister)
        {
            NodeIdCollection registeredNodes;
            try
            {
                if (NoSession()) return new NodeIdCollection();
                //Register nodes
                var response = session.RegisterNodes(null, nodesToRegister, out registeredNodes);
                bool failRegister = false;
                List<Exception> exceptions = new List<Exception>();


                for (int i = 0; i < registeredNodes.Count; i++)
                {
                    if (registeredNodes[i].IdType == IdType.String)
                    {
                        failRegister = true;
                        logger?.Error("Failed to register node: " + registeredNodes[i].ToString());
                        exceptions.Add(new Exception("Failed to register node: " + registeredNodes[i].ToString()));
                    }
                    else
                    {
                        nodeIdCache.Add(nodesToRegister[i].ToString(), registeredNodes[i]);
                    }
                }

                if (failRegister)
                {
                    throw new AggregateException("Failed to register the following nodes", exceptions);
                }
                //response.ServiceResult;
                return registeredNodes;
            }
            catch (AggregateException)
            {
                throw;
            }
            catch (Exception ex)
            {
                //handle Exception here
                throw new Exception("Error registering nodes: " + ex.Message);
            }
        }


        private bool NoSession() => session == null || !session.Connected || session.Disposed;


        public void RegisterNodeId(NodeEntry nodeToRegister, bool AutoReRegister = true)
        {
            var unregistered = new NodeIdCollection() { nodeToRegister.UnregisteredNodeId };
            nodeToRegister.RegisteredNodeId = RegisterNodeIds(unregistered)[0];
            nodeToRegister.ConnectedSessionId = session.SessionId;
            ClearNodeEntries -= nodeToRegister.SessionDisconnected;
            ClearNodeEntries += nodeToRegister.SessionDisconnected;
            if (AutoReRegister)
            {
                ReInstateNodeEntries -= nodeToRegister.NewSessionEstablished;
                ReInstateNodeEntries += nodeToRegister.NewSessionEstablished;
            }

        }

        public NodeId RegisterNodeId(NodeId nodeToRegister)
         => RegisterNodeIds(new NodeIdCollection() { nodeToRegister })[0];


        public void UnregisterNodeIds(NodeIdCollection nodesToUnregister)
         => session.UnregisterNodes(null, nodesToUnregister);

        #endregion Registration

        #region Discovery
        public ApplicationDescription GetConnectedServer()
            => session == null ? null : FindServers(session.ConfiguredEndpoint.EndpointUrl.AbsoluteUri)[0];


        /// <summary>
        /// Finds Servers based on a discovery url
        /// </summary>
        /// <param name="discoveryUrl">The discovery url</param>
        /// <returns>ApplicationDescriptionCollection containing found servers</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public static ApplicationDescriptionCollection FindServers(string discoveryUrl)
        {
            Uri uri = new Uri(discoveryUrl);
            DiscoveryClient client = DiscoveryClient.Create(uri);
            ApplicationDescriptionCollection servers = client.FindServers(null);
            return servers;
        }

        /// <summary>
        /// Finds Endpoints based on a server's url
        /// </summary>
        /// <param name="discoveryUrl">The server's url</param>
        /// <returns>EndpointDescriptionCollection containing found Endpoints</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public static EndpointDescriptionCollection GetEndpoints(string serverUrl)
        {
            Uri uri = new Uri(serverUrl);
            DiscoveryClient client = DiscoveryClient.Create(uri);
            EndpointDescriptionCollection endpoints = client.GetEndpoints(null);

            return endpoints;
        }

        public static async Task<EndpointDescriptionCollection> GetEndpointsAsync(string serverUrl, CancellationToken token)
        {
            Uri uri = new Uri(serverUrl);
            DiscoveryClient client = DiscoveryClient.Create(uri);
            var endpoints = await client.GetEndpointsAsync(null, string.Empty, null, null, token);

            return endpoints.Endpoints;
        }
        #endregion Discovery

        #region Connect/Disconnect
        /// <summary>
        /// Establishes the connection to an OPC UA server and creates a session using an EndpointDescription.
        /// </summary>
        /// <param name="endpointDescription">The EndpointDescription of the server's endpoint</param>
        /// <param name="userAuth">Authenticate anonymous or with username and password</param>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public async Task Connect(EndpointDescription endpointDescription, IUserIdentity userIdentity = null)
        {
            try
            {
                if (mApplicationConfig == null)
                {
                    throw new Exception("Application Configuration is not set");
                }
                if (userIdentity == null)
                {
                    userIdentity = new UserIdentity(new AnonymousIdentityToken());
                }
                mUserIdentity = userIdentity;
                var endpointConfiguration = EndpointConfiguration.Create(mApplicationConfig);
                mEndpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
                mApplicationConfig.CertificateValidator.CertificateValidation += Notification_CertificateValidation;

                //Create a session name
                sessionName =
                    mApplicationConfig.ApplicationName +
                    "_" +
                    Guid.NewGuid().GetHashCode().ToString().Substring(0, 4);

                //Update certificate store before connection attempt
                await mApplicationConfig.CertificateValidator.Update(mApplicationConfig);

                if (session != null && session.Disposed != false) { Disconnect(); }

                //Create and connect session
                session = await Session.Create(
                    mApplicationConfig,
                    mEndpoint,
                    false,
                    true,
                    sessionName,
                    //5_000,
                    60_000,
                    userIdentity,
                    null);

                await InitManagedConnection();
            }
            catch (Exception)
            {
                mApplicationConfig.CertificateValidator.CertificateValidation -= Notification_CertificateValidation;
                throw;
            }
        }

        /// <summary>
        /// Connection
        /// </summary>
        /// <param name="applicationConfig"></param>
        /// <param name="endpoint"></param>
        /// <param name="userIdentity"></param>
        /// <returns></returns>
        public async Task Connect(
            string url = "",
            ApplicationConfiguration applicationConfiguration = null,
            UserIdentity userIdentity = null,
            bool fallBackAnonymous = false)
        {
            try
            {
                var endpoints = GetEndpoints(url);
                EndpointDescription endpoint;

                if (userIdentity == null) { userIdentity = new UserIdentity(); }

                if (userIdentity.TokenType == UserTokenType.Anonymous)
                {
                    endpoint = endpoints.Find(o => o.SecurityMode == MessageSecurityMode.None);
                    if (endpoint == null)
                    {
                        throw new Exception("Server endpoint does not know an anonymous endpoint");
                    }
                    userIdentity = new UserIdentity();
                }
                else
                {
                    endpoint = endpoints.FindLast(
                        o => o.SecurityMode != MessageSecurityMode.None
                        && o.SecurityMode != MessageSecurityMode.Invalid);

                    if (endpoint == null && fallBackAnonymous)
                    {
                        endpoint = endpoints.FindLast(o => o.SecurityMode == MessageSecurityMode.None);
                    }
                    if (endpoint == null)
                    {
                        throw new Exception("Server endpoint does not know an Sign-in endpoint");
                    }
                }
                if (applicationConfiguration == null)
                {
                    await Connect(endpoint, userIdentity);
                }
                else
                {
                    await Connect(applicationConfiguration, endpoint, userIdentity);
                }
                
            }
            catch (Exception)
            {
                //logger?.Error("New session creation failed", e);
                //handle Exception here
                throw;
            }
        }

        /// <summary>
        /// Connection
        /// </summary>
        /// <param name="applicationConfig"></param>
        /// <param name="endpoint"></param>
        /// <param name="userIdentity"></param>
        /// <returns></returns>
        public async Task Connect(
            ApplicationConfiguration applicationConfiguration,
            EndpointDescription endpoint,
            UserIdentity userIdentity)
        {
            try
            {
                mApplicationConfig = applicationConfiguration;
                await Connect(endpoint, userIdentity);
            }
            catch (Exception e)
            {
                logger?.Error("New session creation failed", e);
                //handle Exception here
                throw;
            }
        }

        private void Session_SessionClosing(object sender, EventArgs e)
        {
            if (closing)
            {
                return;
            }
            if (sender is Session session1)
            {
                if (session == session1)
                {
                    CloseSession();
                }
            }
        }

        private void CloseSession()
        {
            session.KeepAlive -= Notification_KeepAlive;
            session.Dispose();
            session = null;
            connectionState = OpcConnectionStatus.Disconnected;
            ConnectionStatusChanged?.Invoke(this, new OpcConnectionStatusChangedEventArgs(OpcConnectionStatus.Disconnected, null, "Session Closed"));
            ConnectionUpdated?.Invoke(this, null);
            nodeIdCache.Clear();
            connectionState = OpcConnectionStatus.Disconnected;
            ClearNodeEntries?.Invoke(this, null);
            ConnectionUpdated?.Invoke(this, null);
            closing = false;
            timer?.Dispose();
        }

        /// <summary>
        /// Closes an existing session and disconnects from the server.
        /// </summary>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public void Disconnect()
        {
            // Close the session.
            try
            {
                if (session != null && !closing)
                {
                    closing = true;
                    var status = session.Close(10000);
                    CloseSession();
                    closing = false;
                }
            }
            catch (Exception)
            {
                //handle Exception here
                throw;
            }
        }


        public async void Reconnect()
        {
            if (Connected) return;

            try
            {
                if (mApplicationConfig == null
                    || mEndpoint == null
                    || mUserIdentity == null)
                {
                    throw new Exception("No connection information available");
                }

                //Create a session name
                sessionName =
                mApplicationConfig.ApplicationName +
                "_" +
                Guid.NewGuid().GetHashCode().ToString().Substring(0, 4);

                //Create and connect session
                session = await Session.Create(
                    mApplicationConfig,
                    mEndpoint,
                    false,
                    true,
                    sessionName,
                    //5_000,
                    60_000,
                    mUserIdentity,
                    null);
                await InitManagedConnection();
            }
            catch (Exception)
            {
                //handle Exception here
                throw;
            }
        }

        private async Task InitManagedConnection()
        {
            try
            {
                await session.LoadDataTypeSystem();
            }
            catch
            {

            }
            ApplicationDescription = FindServers(session.ConfiguredEndpoint.EndpointUrl.AbsoluteUri)[0];
            session.KeepAlive += Notification_KeepAlive;
            ConnectionUpdated?.Invoke(this, null);
            session.SessionClosing += Session_SessionClosing;
            ReInstateNodeEntries?.Invoke(this, null);
            wasConnected = true;
            subscriptions.Clear();

            UpdateNodeTypeDataCache(session, PreparedTypes, Xmls);
        }


        #endregion Connect/Disconnect

        #region EventHandling

        /// <summary>
        /// Eventhandler to validate the server certificate forwards this event
        /// </summary>
        private void Notification_CertificateValidation(
            CertificateValidator certificate,
            CertificateValidationEventArgs e)
        { CertificateValidationNotification?.Invoke(certificate, e); }

        /// <summary>
        /// Eventhandler for MonitoredItemNotifications forwards this event
        /// </summary>
        private void Notification_MonitoredItem(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        { ItemChangedNotification?.Invoke(monitoredItem, e); }

        private DateTime disconnectionTime;
        private bool wasConnected = false;
        private TimeSpan reconnectPeriod = TimeSpan.FromSeconds(30);
        SessionReconnectHandler reconnectHandler;
        private OpcConnectionStatus connectionState;
        private System.Threading.Timer timer;
        void Server_ReconnectComplete(object sender, EventArgs e)
        {
            if (sender == reconnectHandler)
            {
                CompleteReconnect();
            }

        }

        private void CompleteReconnect()
        {
            if (reconnectHandler.Session != null && reconnectHandler.Session.Connected) Reconnected();
            reconnectHandler.Dispose();
            reconnectHandler = null;

        }

        /// <summary>
        /// Eventhandler for KeepAlive forwards this event
        /// </summary>
        private void Notification_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            //Console.WriteLine("KeepAlivePing");
            if (KeepAliveNotification != null)
            {
                KeepAliveNotification.Invoke(session, e);
                return;
            }
            else
            {
                ReconnectCycle(session, e.Status);
            }
        }


        void KeepAliveStillActiveCheck(object state)
        {
            if (state is Session session)
            {
                if (session.LastKeepAliveTime.AddMilliseconds(session.KeepAliveInterval * 2).ToUniversalTime() <= DateTime.UtcNow)
                    ReconnectCycle(session, new ServiceResult(StatusCodes.Bad));
            }
        }
        private void ReconnectCycle(ISession session, ServiceResult e)
        {
            if (session != this.session)
            {
                session.Close();
                return;
            }
            if (ServiceResult.IsNotBad(e))
            {
                Reconnected();
                return;
            }

            if (wasConnected)
            {
                timer = new System.Threading.Timer(KeepAliveStillActiveCheck, session, session.KeepAliveInterval, session.KeepAliveInterval);

                wasConnected = false;
                disconnectionTime = DateTime.Now;
            }
            if (reconnectPeriod <= DateTime.Now - disconnectionTime)
            {
                reconnectHandler.Dispose();
                reconnectHandler = null;
                timer.Dispose();
                timer = null;
                Disconnect();
                ConnectionStatusChanged?.Invoke(this, new OpcConnectionStatusChangedEventArgs(OpcConnectionStatus.Disconnected, e, "Disconnected"));
                return;
            }

            connectionState = OpcConnectionStatus.Reconnecting;
            ConnectionStatusChanged?.Invoke(this, new OpcConnectionStatusChangedEventArgs(OpcConnectionStatus.Reconnecting, e, $"Reconnecting: {(DateTime.Now - disconnectionTime).TotalSeconds:N0}/{reconnectPeriod.TotalSeconds} seconds"));

            if (reconnectHandler == null)
            {
                reconnectHandler = new SessionReconnectHandler(true);
                //var temp = new ReverseConnectManager();
                //temp.AddEndpoint(new Uri(session.Endpoint.EndpointUrl));
                //temp.StartService(this.mApplicationConfig);
                reconnectHandler.BeginReconnect(session, 10000, Server_ReconnectComplete);
            }

            return;
        }

        private void Reconnected()
        {
            if (wasConnected == false)
            {
                ConnectionStatusChanged?.Invoke(this, new OpcConnectionStatusChangedEventArgs(OpcConnectionStatus.Connected, null, $"Connected"));
            }
            timer?.Dispose();
            connectionState = OpcConnectionStatus.Connected;
            wasConnected = true;
        }


        #endregion EventHandling

        #region Browse

        /// <summary>
        /// Browses a node ID provided by a ReferenceDescription
        /// </summary>
        /// <param name="refDesc">The ReferenceDescription</param>
        /// <returns>ReferenceDescriptionCollection of found nodes</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public BrowseResultCollection BrowseNodes(BrowseDescriptionCollection nodesToBrowse)
        {
            try
            {
                if (session == null || session.Disposed) throw new Exception("No session available");
                BrowseResultCollection browseResults = new BrowseResultCollection();

                while (nodesToBrowse.Count > 0)
                {
                    session.Browse(
                        null,
                        null,
                        0,
                        nodesToBrowse,
                        out BrowseResultCollection tmpResults,
                        out DiagnosticInfoCollection diagnosticInfos);

                    OpcValidation.ValidateResponse(nodesToBrowse, tmpResults, diagnosticInfos);
                    browseResults.AddRange(Browse.GetDoneBrowseResults(tmpResults));
                    var (unprocessedOperations, continuationPoints) = Browse.GetContinuationPoints(nodesToBrowse, tmpResults);

                    while (continuationPoints.Count > 0)
                    {
                        // continue browse operation.
                        session.BrowseNext(null, false, continuationPoints, out tmpResults, out diagnosticInfos);

                        OpcValidation.ValidateResponse(nodesToBrowse, tmpResults, diagnosticInfos);
                        browseResults.AddRange(Browse.GetDoneBrowseResults(tmpResults));
                        continuationPoints = Browse.GetNewContinuationPoints(continuationPoints, tmpResults);
                    }

                    // check if unprocessed results exist.
                    nodesToBrowse = new BrowseDescriptionCollection();
                    nodesToBrowse.AddRange(unprocessedOperations);
                }

                // return complete list.
                return browseResults;
            }
            catch (Exception e)
            {
                logger?.Error(e.Message, e);
                throw;
            }
        }
        public async Task<BrowseResultCollection> AsyncBrowseNodes(
            BrowseDescriptionCollection nodesToBrowse,
            CancellationToken token)
        {

            try
            {
                BrowseResultCollection references = new BrowseResultCollection();

                while (nodesToBrowse.Count > 0)
                {
                    if (token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                    var response = await session.BrowseAsync(null, null, 0, nodesToBrowse, token);

                    OpcValidation.ValidateResponse(nodesToBrowse, response);


                    references.AddRange(Browse.GetDoneBrowseResults(response));
                    var (unprocessedOperations, continuationPoints) = Browse.GetContinuationPoints(nodesToBrowse, response.Results);

                    while (continuationPoints.Count > 0)
                    {
                        if (token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }
                        var nextResponse = await session.BrowseNextAsync(null, false, continuationPoints, token);

                        OpcValidation.ValidateResponse(nodesToBrowse, response);

                        references.AddRange(Browse.GetDoneBrowseResults(nextResponse));
                        continuationPoints = Browse.GetNewContinuationPoints(continuationPoints, nextResponse.Results);
                    }

                    // check if unprocessed results exist.
                    nodesToBrowse = new BrowseDescriptionCollection();
                    nodesToBrowse.AddRange(unprocessedOperations);
                }

                // return complete list.
                return references;
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                throw;
            }
        }

        #endregion Browse

        #region BrowseTranslation
        public NodeId TranslateBrowsePathsToNodeId(NodeId baseNodeIdString, string relativeBrowse)
            => TranslateBrowsePathsToNodeIds(
                baseNodeIdString,
                new string[1] { relativeBrowse }
                //uris
                )[0];

        public NodeId TranslateBrowsePathsToNodeId(string baseNodeIdString, string relativeBrowse)
            => TranslateBrowsePathsToNodeIds(
                new NodeId(baseNodeIdString),
                new string[1] { relativeBrowse }
                //uris
                )[0];

        public NodeIdCollection TranslateBrowsePathsToNodeIds(string baseNodeIdString, string[] relativeBrowseCollection)
         => TranslateBrowsePathsToNodeIds(new NodeId(baseNodeIdString), relativeBrowseCollection
             //uris
             );


        public NodeIdCollection TranslateBrowsePathsToNodeIds(NodeId baseNodeId, string[] relativeBrowseCollection)
        {
            BrowsePathCollection result = new BrowsePathCollection();
            result.AddRange(relativeBrowseCollection.Select(o => new BrowsePath()
            {
                StartingNode = new NodeId(baseNodeId),
                RelativePath = RelativePath.Parse(o, session.TypeTree)
            }));
            return TranslateBrowsePathsToNodeIds(result);
        }
        public NodeIdCollection TranslateBrowsePathsToNodeIds(BrowsePathCollection browsePaths)
        {
            session.TranslateBrowsePathsToNodeIds(null, browsePaths, out var results, out var diagnostics);
            OpcValidation.ValidateResponse(diagnostics);
            NodeIdCollection nodes = new NodeIdCollection();
            #region From Opc Sample Client Util TranslateBrowsePaths
            for (int ii = 0; ii < results.Count; ii++)
            {
                // check if the start node actually exists.
                if (StatusCode.IsBad(results[ii].StatusCode))
                {
                    nodes.Add(null);
                    continue;
                }

                // an empty list is returned if no node was found.
                if (results[ii].Targets.Count == 0)
                {
                    nodes.Add(null);
                    continue;
                }

                // Multiple matches are possible, however, the node that matches the type model is the
                // one we are interested in here. The rest can be ignored.
                BrowsePathTarget target = results[ii].Targets[0];

                if (target.RemainingPathIndex != UInt32.MaxValue)
                {
                    nodes.Add(null);
                    continue;
                }

                // The targetId is an ExpandedNodeId because it could be node in another server. 
                // The ToNodeId function is used to convert a local NodeId stored in a ExpandedNodeId to a NodeId.
                nodes.Add(ExpandedNodeId.ToNodeId(target.TargetId, session.NamespaceUris));
            }
            #endregion
            return nodes;
        }
        #endregion

        #region Typing
        public NodeTypeData GetNodeTypeEncoding(string nodeIdString)
        {
            if (PreparedNodeTypes.TryGetValue(nodeIdString, out string parseString))
            {
                return PreparedTypes[parseString];
            }
            try
            {
                parseString = TypeExtraction.GetEncodedTypeName(session, nodeIdString);
                PreparedNodeTypes.Add(nodeIdString, parseString);
                return GetTypeEncoding(parseString);
            }
            catch (Exception)
            {
                var readValue = session.ReadValue(nodeIdString);
                if (readValue == null) throw new Exception("read value is null so can't interprect its type");
                var readType = readValue.GetValue(null).GetType();
                parseString = readType.FullName;
                var nodeTypeData = new NodeTypeData(readType);
                if (!PreparedTypes.ContainsKey(parseString))
                {
                    PreparedTypes.Add(parseString, nodeTypeData);
                }
                PreparedNodeTypes.Add(nodeIdString, parseString);
                return nodeTypeData;
            }
        }

        public NodeTypeData GetTypeEncoding(string parseString)
        {
            NodeTypeData value;
            if (PreparedTypes.TryGetValue(parseString, out value))
            {
                return value;
            }

            throw new Exception("Type not found");
        }

        private static void UpdateNodeTypeDataCache(Session session, Dictionary<string, NodeTypeData> PreparedTypes, List<XmlDocument> xmls)
        {
            Dictionary<string, NodeTypeData> dict = TypeExtraction.GetNodeTypeDataCashe(session, xmls);
            foreach (var o in dict)
            {
                if (PreparedTypes.ContainsKey(o.Key)) PreparedTypes.Remove(o.Key);
                PreparedTypes.Add(o.Key, o.Value);
            }
        }



        private static string? GetContFieldEnum(int id, Type type)
        {
            var fields = type.GetFields();
            FieldInfo field = null;
            var values = fields.Select(o => o.GetRawConstantValue()).OrderBy(o => o).ToArray();
            foreach (var item in fields)
            {
                var checkId = (uint)item.GetRawConstantValue();
                if (checkId == id)
                {
                    field = item;
                    break;
                }
            }

            return field?.Name;
        }

        public object GetCorrectValue(object value)
        {
            if (value is ExtensionObject eoValue)
            {
                return FormatObject(eoValue);
            }
            else if (value is ExtensionObject[] eoValues)
            {
                return eoValues.Select(FormatObject).ToArray();
            }
            return value;
        }

        public object FormatObject(ExtensionObject eoValue)
        {
            if (eoValue.Encoding == ExtensionObjectEncoding.EncodeableObject
                || eoValue.Encoding == ExtensionObjectEncoding.None)
                return eoValue.Body;

            var type = GetTypeEncoding(GetCorrectedTypeName(eoValue));
            return eoValue.Encoding switch
            {
                ExtensionObjectEncoding.Binary => type.Decode(new BinaryDecoder((byte[])eoValue.Body, session.MessageContext)),
                ExtensionObjectEncoding.Xml => type.Decode(new XmlDecoder((XmlElement)eoValue.Body, session.MessageContext)),
                ExtensionObjectEncoding.Json => type.Decode(new JsonDecoder((string)eoValue.Body, session.MessageContext)),
                _ => throw new Exception("Unknown encoding"),
            };
        }



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
        #endregion Typing

        #region Entry Read

        public NodeValueRecord ReadValue(ValueNodeEntry nodeEntry)
        {
            if (nodeEntry == null) throw new ArgumentNullException(nameof(nodeEntry));
            var body = (session.ReadValue(nodeEntry.GetNodeId())).Value;
            return CreateNodeValue(nodeEntry, body);
        }

        public NodeValueRecord[] ReadValues(ValueNodeEntryCollection list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return new NodeValueRecord[0];
            var tempResult = ReadValues(list.GetNodeIds(), list.Types);
            return CreateNodeValueArray(list, tempResult);
        }

        public async Task<NodeValueRecord[]> ReadValuesAsync(ValueNodeEntryCollection list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return new NodeValueRecord[0];
            var tempResult = await AsyncReadValues(list.GetNodeIds(), list.Types);

            return CreateNodeValueArray(list, tempResult);
        }
        public async Task<List<object>> ReadValuesAsync(NodeIdCollection list, List<Type> types = null)
        {
            if (list == null || list.Count == 0) return Array.Empty<object>().ToList();
            if (types == null) types = new Type[list.Count].ToList();
            else if (types.Count != list.Count) throw new ArgumentException("The number of types must match the number of node ids");

            return await AsyncReadValues(list, types);
        }

        public NodeValueRecord[] CreateNodeValueArray(ValueNodeEntryCollection list, List<object> tempResult)
        {
            try
            {
                var nodeValues = new NodeValueRecord[tempResult.Count];
                for (int i = 0; i < tempResult.Count; i++) { nodeValues[i] = CreateNodeValue(list[i], tempResult[i]); }
                return nodeValues;
            }
            catch (Exception)
            {
                throw AggregateAllRecordCreationErrors(list, tempResult);
            }
        }
        public AggregateException AggregateAllRecordCreationErrors(ValueNodeEntryCollection list, List<object> tempResult)
        {
            List<Exception> exps = new List<Exception>();
            for (int i = 0; i < tempResult.Count; i++)
            {
                try
                {
                    CreateNodeValue(list[i], tempResult[i]);
                }
                catch (Exception ex)
                {
                    exps.Add(ex);
                }
            }
            return new AggregateException(exps);
        }
        public AggregateException AggregateAllRecordCreationErrors(ValueNodeEntryCollection list, DataValueCollection tempResult)
        {
            List<Exception> exps = new List<Exception>();
            for (int i = 0; i < tempResult.Count; i++)
            {
                try
                {
                    CreateNodeValue(list[i], tempResult[i]);
                }
                catch (Exception ex)
                {
                    exps.Add(ex);
                }
            }
            return new AggregateException(exps);
        }
        public NodeValueRecord[] CreateNodeValueArray(ValueNodeEntryCollection list, DataValueCollection tempResult)
        {
            try
            {
                var nodeValues = new NodeValueRecord[tempResult.Count];
                for (int i = 0; i < tempResult.Count; i++) { nodeValues[i] = CreateNodeValue(list[i], tempResult[i]); }
                return nodeValues;
            }
            catch (Exception)
            {
                throw AggregateAllRecordCreationErrors(list, tempResult);
            }
        }

        public NodeValueRecord CreateNodeValue(ValueNodeEntry entry, DataValue tempResult)
        {
            if (entry.IsUDT && (tempResult is DataValue dvValue2))
            {
                return CreateNodeValue(entry, (ExtensionObject)dvValue2.Value);
            }
            else if (entry.IsUDT) throw new Exception("Unknown type");
            else if (tempResult is DataValue dvValue1)
            {
                return entry.CreateRecord(dvValue1.Value);
            }
            else return entry.CreateRecord(tempResult);

        }
        public NodeValueRecord CreateNodeValue(ValueNodeEntry entry, ExtensionObject tempResult)
        {
            return entry.CreateRecord(ConstructEncodable(entry, (byte[])tempResult.Body));
        }
        public NodeValueRecord CreateNodeValue(ValueNodeEntry entry, object tempResult)
        {
            return tempResult switch
            {
                DataValue dvValue => CreateNodeValue(entry, dvValue),
                ExtensionObject eoValue => CreateNodeValue(entry, eoValue),
                _ => entry.CreateRecord(tempResult),
            };
        }

        private async Task<List<object>> AsyncReadValues(NodeIdCollection nodeCollection, List<Type> types)
        {
            if (nodeCollection is null) { throw new ArgumentNullException(nameof(nodeCollection)); }
            if (types == null)
            {
                types = new List<Type>(new Type[nodeCollection.Count]);
            }
            else if (nodeCollection.Count != types.Count)
            {
                throw new Exception("List count of types mismatches with that of the NodeCollection.");
            }

            string message = string.Empty;


            Func<Task<object>> task = async delegate ()
            {
                var (values, serviceResults) = await session.ReadValuesAsync(nodeCollection);
                OpcValidation.ValidateResponse(serviceResults.Select(o => o.StatusCode));

                return values.Select(v => v.Value).ToList();
            };

            return (List<object>)await HandleTask(task);
        }

        public IEncodeable ConstructEncodable(ValueNodeEntry entry, byte[] encodedData)
        {
            IEncodeable objResult = (IEncodeable)entry.Constructor.Invoke(new object[0]);
            objResult.Decode(new BinaryDecoder(encodedData, session.MessageContext));
            return objResult;
        }

        #endregion Entry Read

        #region Entry Write
        public void WriteValue(NodeValueRecord nodeEntry)
        {
            if (nodeEntry == null) throw new ArgumentNullException(nameof(nodeEntry));
            WriteValue(nodeEntry.NodeEntry.GetNodeId(), nodeEntry.Value);
        }
        public void WriteValue(NodeEntry nodeEntry, object value)
        {
            WriteValue(nodeEntry.GetNodeId(), value);
        }
        public void WriteValue(NodeId nodeId, object value)
        {
            WriteValueCollection writeCollection = new WriteValueCollection();
            writeCollection.Add(CreateWriteValue(nodeId, value));
            WriteValues(writeCollection);
        }



        [Obsolete("Use a non dictonary value write or better a IEncodable object write for structs to remove dictionary reformating.")]
        public void WriteValue(NodeId nodeId, Dictionary<string, object> value)
        {
            WriteValueCollection writeCollection;
            if (value.Count > 1)
            {
                Session.ReadValues(new List<NodeId>() { nodeId }, new List<Type>() { null }, out var values, out var serviceResults);
                var result = new ExtensionObject();
                var data = OpcObjectEncoder.Binary(session.MessageContext, value);
                result.Body = data;
                result.TypeId = ((ExtensionObject)values[0]).TypeId;

                //var task = Session.LoadDataTypeSystem();
                //task.Wait();

                writeCollection = new WriteValueCollection
                {
                    new WriteValue()
                    {
                        NodeId = nodeId,
                        Value = new DataValue(new Variant(result)),
                        AttributeId = Attributes.Value
                    }
                };
                WriteValues(writeCollection);
            }
            else
            {
                WriteValue(nodeId, value.First().Value);
            }
        }

        [Obsolete("Use a non dictonary value write or better a IEncodable object write for structs to remove dictionary reformating.")]
        public void WriteValue(NodeId nodeId, Dictionary<string, object>[] values)
        {
            Session.ReadValues(new List<NodeId>() { nodeId }, new List<Type>() { null }, out var read_values, out var serviceResults);

            var result = new ExtensionObject[((ExtensionObject[])read_values[0]).Count()];
            var counter = 0;
            foreach (var item in values)
            {
                result[counter] = new ExtensionObject();
                result[counter].Body = (Object)OpcObjectEncoder.Binary(session.MessageContext, item);
                result[counter++].TypeId = ((ExtensionObject[])read_values[0])[0].TypeId;
            }

            WriteValue(nodeId, new DataValue(result));
        }

        public void WriteValues(NodeValueRecordCollection list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return;
            WriteValues(CreateWriteCollection(list));
        }

        public async Task WriteValuesAsync(NodeValueRecordCollection list, CancellationToken ct = default)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return;
            await WriteValuesAsync(CreateWriteCollection(list), ct);
        }


        private static WriteValue CreateWriteValue(NodeId nodeId, object value)
        {

            try
            {
                if (value is DataValue dvalue)
                {
                    return new WriteValue()
                    {
                        NodeId = nodeId,
                        Value = dvalue,
                        AttributeId = Attributes.Value
                    };

                }
                else
                {
                    return new WriteValue()
                    {
                        NodeId = nodeId,
                        Value = new DataValue(new Variant(value)),
                        AttributeId = Attributes.Value
                    };
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private static WriteValue CreateWriteValue(NodeValueRecord record)
        {
            return new WriteValue()
            {
                NodeId = record.NodeEntry.GetNodeId(),
                Value = new DataValue(new Variant(record.Value)),
                AttributeId = Attributes.Value
            };
        }

        private static WriteValueCollection CreateWriteCollection(NodeValueRecordCollection list)
            => new WriteValueCollection(list.nodeValueRecords.Select(o => CreateWriteValue(o)));

        #endregion Entry Read

        #region Read

        public object ReadValue(NodeId nodeId, Type type = null)
        {
            var value = type == null ? (session.ReadValue(nodeId)).Value : session.ReadValue(nodeId, type);
            return GetCorrectValue(value);
        }
        public T ReadValue<T>(NodeId nodeId) => (T)session.ReadValue(nodeId, typeof(T));
        public Node ReadNode(NodeId nodeId) => session.ReadNode(nodeId);




        public NodeCollection ReadNodes(NodeIdCollection nodeIdCollection)
        {
            if (nodeIdCollection is null || nodeIdCollection.Count == 0) return new NodeCollection();
            Func<NodeCollection> task = delegate ()
            {
                session.ReadNodes(nodeIdCollection, out var nodes, out IList<ServiceResult> statusResults);
                OpcValidation.ValidateResponse(statusResults.Select(o => o.StatusCode));
                var nodes2 = new NodeCollection();
                nodes2.AddRange(nodes);
                return nodes2;
            };
            return HandleTask(task);
        }

        public async Task<NodeCollection> AsyncReadNodes(NodeIdCollection nodeIdCollection, CancellationToken token)
        {
            if (nodeIdCollection is null || nodeIdCollection.Count == 0) return new NodeCollection();
            Func<Task<NodeCollection>> task = async delegate ()
            {
                var (nodes, statusResults) = await session.ReadNodesAsync(nodeIdCollection, true, token).ConfigureAwait(false);
                OpcValidation.ValidateResponse(statusResults.Select(o => o.StatusCode));
                var nodes2 = new NodeCollection();
                nodes2.AddRange(nodes);
                return nodes2;
            };
            return await HandleTask(task);
        }


        public async Task<NodeCollection> AsyncReadNodes(
            ReferenceDescriptionCollection referenceDescriptions,
            CancellationToken token)
        {
            if (referenceDescriptions is null || referenceDescriptions.Count == 0) return new NodeCollection();
            Func<Task<NodeCollection>> task = async delegate ()
            {
                NodeIdCollection nodeIds = new NodeIdCollection();
                nodeIds.AddRange(
                    referenceDescriptions.Select(o => ExpandedNodeId.ToNodeId(o.NodeId, session.NamespaceUris)));
                var (nodes, statusResults) = await session.ReadNodesAsync(nodeIds, true, token).ConfigureAwait(false);
                OpcValidation.ValidateResponse(statusResults.Select(o => o.StatusCode));
                var nodes2 = new NodeCollection();
                nodes2.AddRange(nodes);
                return nodes2;
            };
            return await HandleTask(task);
        }
        #endregion Read

        #region NodeCollection Read / Write
        public DataValueCollection ReadValues(NodeIdCollection nodeCollection, List<Type> types = null)
        {
            if (nodeCollection is null || nodeCollection.Count == 0) return new DataValueCollection();
            if (!session.Connected) throw new ServiceResultException(StatusCodes.BadSessionNotActivated);
            if (types is null) types = new List<Type>(new Type[nodeCollection.Count]);
            else if (nodeCollection.Count != types.Count) throw new Exception("List count of types mismatches with that of the NodeCollection.");

            Func<DataValueCollection> task = delegate ()
            {
                session.ReadValues(
                    nodeCollection,
                    out DataValueCollection values,
                    out IList<ServiceResult> serviceResults);
                OpcValidation.ValidateResponse(serviceResults);
                return values;
            };

            return HandleTask(task);
        }

        public void WriteValues(WriteValueCollection nodeCollection)
        {
            if (nodeCollection is null || nodeCollection.Count == 0) return;
            if (!session.Connected) throw new ServiceResultException(StatusCodes.BadSessionNotActivated);

            Action task = delegate ()
            {
                session.Write(null, nodeCollection,
                    out StatusCodeCollection serviceResults,
                    out DiagnosticInfoCollection diag);
                OpcValidation.ValidateResponse(serviceResults);
                OpcValidation.ValidateResponse(diag);
            };

            HandleTask(task);
        }

        public async Task WriteValuesAsync(WriteValueCollection nodeCollection, CancellationToken ct = default)
        {
            if (nodeCollection is null || nodeCollection.Count == 0) return;
            if (!session.Connected) throw new ServiceResultException(StatusCodes.BadSessionNotActivated);

            Func<Task<object>> task = async delegate ()
            {
                var response = await session.WriteAsync(null, nodeCollection, ct).ConfigureAwait(true);
                OpcValidation.ValidateResponse(response.Results);
                return null;
            };

            await HandleTask(task);
        }


        #endregion

        #region Methods
        /// <summary>
        /// Get information about a method's input and output arguments
        /// </summary>
        /// <param name="nodeIdString">The node Id of a method as strings</param>
        /// <returns>Argument information's as strings</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>

        public MethodArguments GetMethodArguments(NodeId nodeId)
        {
            var method = new MethodArguments();

            try
            {
                Node methodNode = ReadNode(nodeId);

                if (methodNode.NodeClass != NodeClass.Method)
                {
                    throw new ServiceResultException(StatusCodes.BadNodeClassInvalid);
                }

                //Start browsing
                //Browse from starting point for properties (input and output)
                var referenceDescriptionCollection = BrowseNodes(new BrowseDescriptionCollection() { Browse.GetMethodArgumentsBrowseDescription(nodeId) });

                //Guard Clause
                if (referenceDescriptionCollection == null || referenceDescriptionCollection.Count <= 0)
                {
                    return method;
                }

                foreach (ReferenceDescription refDesc in Browse.GetDescriptions(referenceDescriptionCollection))
                {
                    ArgumentCollection arguments;

                    //Get correct collection
                    if (refDesc.NodeClass != NodeClass.Variable)
                        continue;
                    if (refDesc.BrowseName.Name == "InputArguments")
                        arguments = method.InputArguments;
                    else if (refDesc.BrowseName.Name == "OutputArguments")
                        arguments = method.OutputArguments;
                    else
                        continue;

                    List<NodeId> nodeIds = new List<NodeId>()
                    {
                        ExpandedNodeId.ToNodeId(refDesc.NodeId, session.NamespaceUris)
                    };
                    List<Type> types = new List<Type>() { null };

                    //Read the input/output arguments
                    session.ReadValues(nodeIds, types, out List<object> values, out List<ServiceResult> serviceResults);

                    OpcValidation.ValidateResponse(serviceResults);

                    //Extract arguments
                    foreach (object result in values.Where(o => o != null))
                    {
                        //Cast object to ExtensionObject because input and output arguments are always extension objects                        
                        if (result is ExtensionObject encodeable)
                            arguments.Add(encodeable.Body as Argument);
                        else
                            arguments
                                .AddRange((result as ExtensionObject[])
                                .Select(exOb => exOb.Body as Argument));
                    }
                    Type[] argumentTypes;
                    if (refDesc.BrowseName.Name == "InputArguments")
                    {
                        method.InputArgumentTypes = new Type[arguments.Count];
                        argumentTypes = method.InputArgumentTypes;
                    }
                    else
                    {
                        method.OutputArgumentTypes = new Type[arguments.Count];
                        argumentTypes = method.OutputArgumentTypes;
                    }
                    for (int i = 0; i < arguments.Count; i++)
                    {
                        argumentTypes[i] = Opc.Ua.TypeInfo.GetSystemType(
                                                                   arguments[i].DataType,
                                                                   session.Factory);
                    }
                }

                return method;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public IList<object> CallMethod(NodeId objectNodeId, NodeId methodNodeId, object[] inputArguments)
        {
            if (inputArguments is null) inputArguments = new object[0];
            return Session.Call(
                objectNodeId,
                methodNodeId,
                inputArguments);
        }
        public CallMethodResultCollection CallMethods(CallMethodRequestCollection methodRequests)
        {
            Func<CallMethodResultCollection> task = delegate ()
            {
                RequestHeader requestHeader = new RequestHeader();
                var responseHeader = Session.Call(requestHeader, methodRequests, out var results, out var diagnostics);
                OpcValidation.ValidateResponse(diagnostics);
                //OpcValidation.ValidateResponse(results);

                //CallOutput[] output = new CallOutput[results.Count];


                return results;
            };

            return HandleTask(task);
        }
        #endregion

        #region Subscription

        /// <summary>
        /// Creates a Subscription object to a server
        /// </summary>
        /// <param name="publishingInterval">The publishing interval</param>
        /// <returns>Subscription</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public Subscription CreateSubscription(int publishingInterval)
        {
            Subscription subscription = new Subscription(Session.DefaultSubscription);
            subscription.PublishingEnabled = true;
            subscription.PublishingInterval = publishingInterval;
            try
            {
                Session.AddSubscription(subscription);
                subscription.Create();
                subscriptions.Add(subscription);
                return subscription;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Ads a monitored item to an existing subscription
        /// </summary>
        /// <param name="subscription">The subscription</param>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public MonitoredItem AddMonitoredItem(
            Subscription subscription,
            ValueNodeEntry nodeEntry,
            MonitoredNodeValueRecordEventHandler handler = null)
        {
            MonitoredItem monitoredItem = CreateMonitoredItem(nodeEntry, handler: handler);
            try
            {
                subscription.AddItem(monitoredItem);
                subscription.ApplyChanges();
            }
            catch (Exception)
            {
                throw;
            }
            return monitoredItem;
        }
        public MonitoredItem AddMonitoredItem(
                Subscription subscription,
                NodeId nodeEntry,
                MonitoredNodeValueEventHandler handler = null)
        {
            MonitoredItem monitoredItem = CreateMonitoredItem(nodeEntry, handler: handler);
            AddMonitoredItem(subscription, monitoredItem);
            return monitoredItem;
        }
        public IEnumerable<MonitoredItem> AddMonitoredItems(Subscription subscription, ValueNodeEntryCollection nodeEntrys)
        {
            IEnumerable<MonitoredItem> items = nodeEntrys.NodeEntries.Select(o => CreateMonitoredItem(o));
            AddMonitoredItems(subscription, items);
            return items;
        }
        public void AddMonitoredItem(
                Subscription subscription,
                MonitoredItem item)
        {
            try
            {
                subscription.AddItem(item);
                subscription.ApplyChanges();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void AddMonitoredItems(
                Subscription subscription,
                IEnumerable<MonitoredItem> item)
        {
            try
            {
                subscription.AddItems(item);
                subscription.ApplyChanges();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public Subscription GetSubscription(int publishingIntervalMilliSec)
        {
            var subscription = subscriptions.FirstOrDefault(o => o.PublishingInterval == publishingIntervalMilliSec);
            return subscription != null ? subscription : CreateSubscription(publishingIntervalMilliSec);
        }



        public MonitoredItem CreateMonitoredItem(
            ValueNodeEntry nodeEntry,
            int samplingInterval = 1,
            uint queueSize = 1,
            bool discardOldest = true,
            bool globalCall = false,
            MonitoredNodeValueRecordEventHandler handler = null)
        {
            if (handler == null && !globalCall)
            {
                throw new ArgumentNullException(nameof(handler), "Either handler or globalCall must be set");
            }
            MonitoredItem monitoredItem = CreateMonitoredItem(nodeEntry, samplingInterval, queueSize, discardOldest);
            if (handler != null)
            {
                monitoredItem.Notification += (sender, arg) => MonitoredNode(sender, nodeEntry, arg, handler);
            }
            if (globalCall)
            {
                monitoredItem.Notification += (sender, arg) => MonitoredNode(sender, nodeEntry, arg, NodeChangedNotification);
            }

            return monitoredItem;
        }
        public MonitoredItem CreateMonitoredItem(
                NodeId nodeId,
                int samplingInterval = 1,
                uint queueSize = 1,
                bool discardOldest = true,
                bool globalCall = false,
                MonitoredNodeValueEventHandler handler = null)
        {
            if (handler == null && !globalCall)
            {
                throw new ArgumentNullException(nameof(handler), "Either handler or globalCall must be set");
            }
            MonitoredItem monitoredItem = CreateMonitoredItem(nodeId, samplingInterval, queueSize, discardOldest);
            if (handler != null)
            {
                monitoredItem.Notification += (sender, arg) => MonitoredNode(sender, arg, handler);
            }
            if (globalCall)
            {
                monitoredItem.Notification += (sender, arg) => MonitoredNode(sender, arg, NodeChangedNotification);
            }

            return monitoredItem;
        }
        public static MonitoredItem CreateMonitoredItem(ValueNodeEntry nodeEntry, int samplingInterval, uint queueSize, bool discardOldest)
        {
            MonitoredItem monitoredItem = new MonitoredItem();
            monitoredItem.DisplayName = nodeEntry.NodeString;
            monitoredItem.StartNodeId = nodeEntry.UnregisteredNodeId;
            monitoredItem.AttributeId = Attributes.Value;
            monitoredItem.MonitoringMode = MonitoringMode.Reporting;
            monitoredItem.SamplingInterval = samplingInterval;
            monitoredItem.QueueSize = queueSize;
            monitoredItem.DiscardOldest = discardOldest;
            return monitoredItem;
        }

        static public MonitoredItem CreateMonitoredItem(NodeId nodeId, int samplingInterval, uint queueSize, bool discardOldest)
        {
            MonitoredItem monitoredItem = new MonitoredItem();
            monitoredItem.DisplayName = nodeId.ToString();
            monitoredItem.StartNodeId = nodeId;
            monitoredItem.AttributeId = Attributes.Value;
            monitoredItem.MonitoringMode = MonitoringMode.Reporting;
            monitoredItem.SamplingInterval = samplingInterval;
            monitoredItem.QueueSize = queueSize;
            monitoredItem.DiscardOldest = discardOldest;
            return monitoredItem;
        }

        private void MonitoredNode(
                MonitoredItem monitorItem,
                MonitoredItemNotificationEventArgs arg,
                MonitoredNodeValueEventHandler handler)
        {
            var value = GetCorrectValue(((MonitoredItemNotification)arg.NotificationValue).Value.Value);
            handler?.Invoke(monitorItem, value);
        }

        private void MonitoredNode(
                MonitoredItem monitorItem,
                MonitoredItemNotificationEventArgs arg,
                MonitoredNodeValueRecordEventHandler handler)
        {
            var value = GetCorrectValue(((MonitoredItemNotification)arg.NotificationValue).Value.Value);
            handler?.Invoke(monitorItem, new NodeValueRecord(new ValueNodeEntry(monitorItem.StartNodeId, value.GetType()), value));
        }

        private void MonitoredNode(
            MonitoredItem monitorItem,
            ValueNodeEntry entry,
            MonitoredItemNotificationEventArgs arg,
            MonitoredNodeValueRecordEventHandler handler)
        {
            if (!entry.IsUDT)
            {
                handler?.Invoke(monitorItem, CreateNodeValue(entry, ((MonitoredItemNotification)arg.NotificationValue).Value.Value));
            }
            else
            {
                // item.Decode(arg.NotificationValue);
                ExtensionObject obj = ((ExtensionObject)((MonitoredItemNotification)arg.NotificationValue).Value.Value);
                if (obj.Encoding == ExtensionObjectEncoding.Binary)
                {
                    handler.Invoke(monitorItem, entry.CreateRecord(ConstructEncodable(entry, (byte[])obj.Body)));
                }
                else
                {
                    logger.Error($"No Decoding method for monitored item {entry.NodeString}");
                }
            }
        }

        /// <summary>
        /// Removes a monitored item from an existing subscription
        /// </summary>
        /// <param name="subscription">The subscription</param>
        /// <param name="monitoredItem">The item</param>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public void RemoveMonitoredItem(Subscription subscription, MonitoredItem monitoredItem)
        {
            if (subscription.MonitoredItems.Contains(monitoredItem))
            {
                subscription.RemoveItem(monitoredItem);
                subscription.ApplyChanges();
            }
        }

        public void RemoveMonitoredItem(int publishingInterval, ValueNodeEntry entry)
        {
            var subscription = GetSubscription(publishingInterval);
            var monitoredItem = subscription.MonitoredItems.FirstOrDefault(o => o.StartNodeId == entry.NodeString);

            if (monitoredItem != null)
            {
                try
                {
                    subscription.RemoveItem(monitoredItem);
                    subscription.ApplyChanges();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public void RemoveMonitoredItem(ValueNodeEntry entry)
        {
            foreach (var subscription in this.subscriptions)
            {
                var monitoredItem = subscription.MonitoredItems.FirstOrDefault(o => o.StartNodeId == entry.NodeString);

                if (monitoredItem != null)
                {
                    try
                    {
                        subscription.RemoveItem(monitoredItem);
                        subscription.ApplyChanges();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        public void RemoveMonitoredItems(ValueNodeEntryCollection entrys)
        {
            foreach (var entry in entrys.NodeEntries) RemoveMonitoredItem(entry);
        }
        #endregion

        #region CallHandlers
        private void HandleTask(Action task)
        {
            try
            {
                task.Invoke();
            }
            catch (ServiceResultException se)
            {
                HandleServiceResultException(se);
                logger?.Error("Failed executing task", se, null);
                throw;
            }
            catch (Exception e)
            {
                if (e is ServiceResultException se) HandleServiceResultException(se);
                logger?.Error("Failed executing task", e, null);
                throw;
            }
        }

        private T HandleTask<T>(Func<T> task)
        {
            try
            {
                return task.Invoke();
            }
            catch (ServiceResultException se)
            {
                HandleServiceResultException(se);
                logger?.Error("Failed executing task", se, null);
                throw;
            }
            catch (Exception e)
            {
                if (e is ServiceResultException se) HandleServiceResultException(se);
                logger?.Error("Failed executing task", e, null);
                throw;
            }
        }
        private async Task<T> HandleTask<T>(Func<Task<T>> task)
        {
            try
            {
                return await task.Invoke();
            }
            catch (ServiceResultException se)
            {
                HandleServiceResultException(se);
                logger?.Error("Failed executing task", se, null);
                throw;
            }
            catch (Exception e)
            {
                if (e is ServiceResultException se) HandleServiceResultException(se);
                logger?.Error("Failed executing task", e, null);
                throw;
            }
        }
        private void HandleServiceResultException(ServiceResultException e)
        {
            if (e.StatusCode == StatusCodes.BadSessionIdInvalid
                || e.StatusCode == StatusCodes.BadSecureChannelClosed)
            {
                Disconnect();
            }
        }
        #endregion

        #region Scanning
        public bool[] ScanNodeExistences(NodeIdCollection nodeIdCollection)
        {
            if (nodeIdCollection is null || nodeIdCollection.Count == 0) return new bool[0];
            Func<bool[]> task = delegate ()
            {
                session.ReadNodes(nodeIdCollection, out var nodes, out IList<ServiceResult> statusResults);
                return statusResults.Select(o => StatusCode.IsGood(o.Code)).ToArray();
            };
            return HandleTask(task);
        }

        public bool[] ScanTypeNodeExistences(NodeIdCollection nodeIdCollection, NodeClass nodeClass)
        {
            if (nodeIdCollection is null || nodeIdCollection.Count == 0) return new bool[0];
            Func<bool[]> task = delegate ()
            {
                var results = new bool[nodeIdCollection.Count];
                session.ReadNodes(nodeIdCollection, out var nodes, out IList<ServiceResult> statusResults);
                for (int i = 0; i < nodeIdCollection.Count; i++)
                {
                    results[i] = StatusCode.IsGood(statusResults[i].StatusCode) && nodes[i].NodeClass == nodeClass;
                }
                return results;
            };
            return HandleTask(task);
        }

        public bool ScanNodeExistance(NodeId nodeId)
        {
            if (nodeId == null) return false;
            try
            {
                session.ReadNode(nodeId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ScanTypeNodeExistance(NodeId nodeId, NodeClass nodeClass)
        {
            if (nodeId == null) return false;
            try
            {
                var node = session.ReadNode(nodeId);
                return node.NodeClass == nodeClass;
            }
            catch (Exception)
            {
                return false;
            }
        }


        #endregion
    }
}