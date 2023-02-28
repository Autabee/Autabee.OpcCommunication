using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Utility.Logger;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Autabee.Communication.ManagedOpcClient
{
    public delegate void MonitoredNodeValueRecordEventHandler(object sender, NodeValueRecord e);
    public delegate void MonitoredNodeValueEventHandler(MonitoredItem nodeId, object e);

    public class AutabeeManagedOpcClient
    {
        private bool closing;
        private readonly IAutabeeLogger logger;
        private IUserIdentity mUserIdentity;
        private ApplicationConfiguration mApplicationConfig;
        private ConfiguredEndpoint mEndpoint;

        private Session session;
        private string sessionName;
        private List<Subscription> subscriptions = new List<Subscription>();

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

        #region Construction
        public AutabeeManagedOpcClient(string company, string product, string directory, IAutabeeLogger logger = null)
        {
            this.logger = logger;
            // Create's the application configuration (containing the certificate) on construction
            mApplicationConfig = AutabeeManagedOpcClientExtension.GetClientConfiguration(
                                     company,
                                     product,
                                     directory,
                                     logger);
        }
        public AutabeeManagedOpcClient(Stream stream, IAutabeeLogger logger = null)
        {
            this.logger = logger;
            // Create's the application configuration (containing the certificate) on construction
            mApplicationConfig = AutabeeManagedOpcClientExtension.CreateDefaultClientConfiguration(stream);
        }

        public AutabeeManagedOpcClient(ApplicationConfiguration opcAppConfig, IAutabeeLogger logger = null)
        {
            this.logger = logger;
            mApplicationConfig = opcAppConfig;
        }
        #endregion Construction

        #region Registration

        public void RegisterNodeIds(ValueNodeEntryCollection preparedCollection, bool AutoReregister = true)
        {
            var nodelist = preparedCollection.RegisteredNodeIds;
            if (nodelist.Count != 0) { UnregisterNodeIds(nodelist); }
            nodelist.Clear();

            nodelist.AddRange(RegisterNodeIds(preparedCollection.NodeIds));
            ClearNodeEntries -= preparedCollection.SessionDisconnected;
            ClearNodeEntries += preparedCollection.SessionDisconnected;
            if (AutoReregister)
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
                var responce = session.RegisterNodes(null, nodesToRegister, out registeredNodes);
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
                //responce.ServiceResult;
                return registeredNodes;
            }
            catch (AggregateException _)
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


        public void RegisterNodeId(NodeEntry nodeToRegister, bool AutoreRegister = true)
        {
            var unregistered = new NodeIdCollection() { nodeToRegister.UnregisteredNodeId };
            nodeToRegister.RegisteredNodeId = RegisterNodeIds(unregistered)[0];
            nodeToRegister.ConnectedSessionId = session.SessionId;
            ClearNodeEntries -= nodeToRegister.SessionDisconnected;
            ClearNodeEntries += nodeToRegister.SessionDisconnected;
            if (AutoreRegister)
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
        /// <param name="userAuth">Autheticate anonymous or with username and password</param>
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

                //Creat a session name
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

                ApplicationDescription = FindServers(session.ConfiguredEndpoint.EndpointUrl.AbsoluteUri)[0];
                session.KeepAlive += Notification_KeepAlive;
                ConnectionUpdated?.Invoke(this, null);
                session.SessionClosing += Session_SessionClosing;
                ReInstateNodeEntries?.Invoke(this, null);
                wasConnected = true;
                subscriptions.Clear();

                UpdateTypeData();
            }
            catch (Exception e)
            {
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
                        throw new Exception("Server endpoint does not know an Signin endpoint");
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
            catch (Exception e)
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
                //logger?.Error("New session creation failed", e);
                //handle Exception here
                throw e;
            }
        }

        private void Session_SessionClosing(object sender, EventArgs e)
        {
            //if (!closing)
            //{
            //    logger?.Warning("Reconnection as a closing was detected from outside of the helper");
            //    Session.Reconnect();
            //    ConnectionUpdated?.Invoke(this, null);
            //}
            if (sender is Session session1)
            {
                session1.KeepAlive -= Notification_KeepAlive;

                if (session == session1)
                {
                    session1.Dispose();
                    session = null;
                    connectionState = OpcConnectionStatus.Disconnected;
                    ConnectionStatusChanged?.Invoke(this, new OpcConnectionStatusChangedEventArgs(OpcConnectionStatus.Disconnected, null, "Session Closing"));
                    ConnectionUpdated?.Invoke(this, null);
                }
            }
            ClearNodeEntries?.Invoke(this, null);
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
                    var status = session.Close(10000,true);
                    if (session != null)
                    {
                        session.KeepAlive -= Notification_KeepAlive;
                    }
                    nodeIdCache.Clear();
                    connectionState = OpcConnectionStatus.Disconnected;
                    //ClearNodeEntries?.Invoke(this, null);
                    ConnectionUpdated?.Invoke(this, null);
                    closing = false;
                    timer?.Dispose();
                }
            }
            catch (Exception e)
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

                //Creat a session name
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

                ApplicationDescription = FindServers(session.ConfiguredEndpoint.EndpointUrl.AbsoluteUri)[0];
                session.KeepAlive += Notification_KeepAlive;
                ConnectionUpdated?.Invoke(this, null);
                session.SessionClosing += Session_SessionClosing;
                ReInstateNodeEntries?.Invoke(this, null);
                wasConnected = true;
                subscriptions.Clear();

                UpdateTypeData();
            }
            catch (Exception e)
            {
                //handle Exception here
                throw;
            }
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
        private bool reconnecting = false;
        private uint reconnectFailCounter = 0;
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
            Console.WriteLine("KeepAlivePing");
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
            Console.WriteLine("KeepStrillAlivePing");
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
                reconnectFailCounter = 1;
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
            reconnectFailCounter = 0;
        }


        #endregion EventHandling

        #region Browse

        /// <summary>
        /// Browses the root folder of an OPC UA server.
        /// </summary>
        /// <returns>ReferenceDescriptionCollection of found nodes</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public ReferenceDescriptionCollection BrowseRoot() => BrowseNode(ObjectIds.RootFolder);

        /// <summary>
        /// Browses a node ID provided by a ReferenceDescription
        /// </summary>
        /// <param name="refDesc">The ReferenceDescription</param>
        /// <returns>ReferenceDescriptionCollection of found nodes</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public ReferenceDescriptionCollection BrowseNode(ReferenceDescription refDesc) =>
            session != null
            ? BrowseNode(ExpandedNodeId.ToNodeId(refDesc.NodeId, session.NamespaceUris))
            : throw new Exception("BadNotConnected");

        public ReferenceDescriptionCollection BrowseNode(NodeId node) => BrowseNode(
            new BrowseDescriptionCollection() { GetNodeHierarchalBrowseDescription(node) });

        public ReferenceDescriptionCollection BrowseNode(BrowseDescriptionCollection nodesToBrowse)
        {
            try
            {
                ReferenceDescriptionCollection references = new ReferenceDescriptionCollection();

                while (nodesToBrowse.Count > 0)
                {
                    session.Browse(
                        null,
                        null,
                        0,
                        nodesToBrowse,
                        out BrowseResultCollection results,
                        out DiagnosticInfoCollection diagnosticInfos);

                    ClientBase.ValidateResponse(results, nodesToBrowse);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);
                    references.AddRange(GetDescriptions(results));

                    var (unprocessedOperations, continuationPoints) = GetNewContinuationPoints(nodesToBrowse, results);

                    while (continuationPoints.Count > 0)
                    {
                        // continue browse operation.
                        session.BrowseNext(null, false, continuationPoints, out results, out diagnosticInfos);

                        ClientBase.ValidateResponse(results, continuationPoints);
                        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);
                        references.AddRange(GetDescriptions(results));
                        continuationPoints = GetNewContinuationPoints(continuationPoints, results);
                    }

                    // check if unprocessed results exist.
                    nodesToBrowse = unprocessedOperations;
                }

                // return complete list.
                return references;
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                throw e;
            }
        }


        public async Task<ReferenceDescriptionCollection> AsyncBrowseNode(NodeId node, CancellationToken token)
        {
            BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection()
            {
                GetNodeHierarchalBrowseDescription(node)
            };
            BrowseResultCollection results;
            try
            {
                ReferenceDescriptionCollection references = new ReferenceDescriptionCollection();

                while (nodesToBrowse.Count > 0)
                {
                    var response = await session.BrowseAsync(null, null, 0, nodesToBrowse, token);
                    results = response.Results;
                    ClientBase.ValidateResponse(response.Results, nodesToBrowse);
                    ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToBrowse);
                    references.AddRange(GetDescriptions(response));

                    var (unprocessedOperations, continuationPoints) = GetNewContinuationPoints(nodesToBrowse, results);

                    while (continuationPoints.Count > 0)
                    {
                        var nextresponse = await session.BrowseNextAsync(null, false, continuationPoints, token);
                        results = nextresponse.Results;
                        ClientBase.ValidateResponse(results, continuationPoints);
                        references.AddRange(GetDescriptions(results));
                        continuationPoints = GetNewContinuationPoints(continuationPoints, results);
                    }

                    // check if unprocessed results exist.
                    nodesToBrowse = unprocessedOperations;
                }

                // return complete list.
                return references;
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                throw e;
            }
        }

        public async Task<Dictionary<BrowseDescription, ReferenceDescriptionCollection>> AsyncBrowseNode(
            BrowseDescriptionCollection nodesToBrowse,
            CancellationToken token)
        {
            BrowseResultCollection results;
            try
            {
                Dictionary<BrowseDescription, ReferenceDescriptionCollection> references = new Dictionary<BrowseDescription, ReferenceDescriptionCollection>(
                    );

                while (nodesToBrowse.Count > 0)
                {
                    var response = await session.BrowseAsync(null, null, 0, nodesToBrowse, token);
                    results = response.Results;
                    ClientBase.ValidateResponse(response.Results, nodesToBrowse);
                    ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToBrowse);
                    //references.AddRange(GetDescriptions(response));

                    var unprocessedOperations = new BrowseDescriptionCollection();
                    var continued = new BrowseDescriptionCollection();
                    Dictionary<BrowseDescription, byte[]> continuationPoints = new Dictionary<BrowseDescription, byte[]>(
                        );
                    for (int ii = 0; ii < nodesToBrowse.Count; ii++)
                    {
                        // check for error.
                        if (StatusCode.IsBad(results[ii].StatusCode))
                        {
                            // this error indicates that the server does not have enough simultaneously active 
                            // continuation points. This request will need to be resent after the other operations
                            // have been completed and their continuation points released.
                            if (results[ii].StatusCode == StatusCodes.BadNoContinuationPoints)
                            {
                                unprocessedOperations.Add(nodesToBrowse[ii]);
                            }

                            continue;
                        }
                        references.Add(nodesToBrowse[ii], new ReferenceDescriptionCollection());
                        if (results[ii].References.Count == 0)
                            continue;
                        else
                            references[nodesToBrowse[ii]].AddRange(results[ii].References);

                        if (results[ii].ContinuationPoint != null)
                        {
                            continuationPoints.Add(
                                                                        nodesToBrowse[ii],
                                                                        results[ii].ContinuationPoint);
                        }
                    }

                    while (continuationPoints.Count > 0)
                    {
                        var scanpoints = new ByteStringCollection(continuationPoints.Values);

                        var nextresponse = await session.BrowseNextAsync(null, false, scanpoints, token);
                        results = nextresponse.Results;
                        ClientBase.ValidateResponse(results, scanpoints);
                        //references.AddRange(GetDescriptions(results));
                        Dictionary<BrowseDescription, byte[]> revisedContiuationPoints = new Dictionary<BrowseDescription, byte[]>(
                            );
                        var keyarray = continuationPoints.Keys.ToArray();
                        for (int ii = 0; ii < keyarray.Length; ii++)
                        {
                            if (StatusCode.IsBad(results[ii].StatusCode))
                            {
                                continue;
                            }
                            if (results[ii].References.Count == 0)
                            {
                                continue;
                            }
                            if (results[ii].ContinuationPoint != null)
                            {
                                revisedContiuationPoints.Add(
                                                                            keyarray[ii],
                                                                            results[ii].ContinuationPoint);
                            }
                        }

                        continuationPoints = revisedContiuationPoints;
                    }

                    // check if unprocessed results exist.
                    nodesToBrowse = unprocessedOperations;
                }

                // return complete list.
                return references;
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                throw e;
            }
        }

        public ReferenceDescription GetParent(NodeId nodeId)
        {
            try
            {
                //We need to browse for property (input and output arguments)
                //Create a collection for the browse results
                ReferenceDescriptionCollection referenceDescriptionCollection;
                ReferenceDescriptionCollection nextreferenceDescriptionCollection;
                //Create a continuationPoint
                byte[] continuationPoint;
                byte[] revisedContinuationPoint;

                session.Browse(
                    null,
                    null,
                    nodeId,
                    0u,
                    BrowseDirection.Inverse,
                    null,
                    true,
                    0,
                    out continuationPoint,
                    out referenceDescriptionCollection);

                while (continuationPoint != null)
                {
                    session.BrowseNext(
                        null,
                        false,
                        continuationPoint,
                        out revisedContinuationPoint,
                        out nextreferenceDescriptionCollection);
                    referenceDescriptionCollection.AddRange(nextreferenceDescriptionCollection);
                    continuationPoint = revisedContinuationPoint;
                }

                //Gaurd Clause
                if (referenceDescriptionCollection == null || referenceDescriptionCollection.Count <= 0)
                {
                    throw new Exception("No Parent Id Found");
                }
                if (referenceDescriptionCollection.Count > 1)
                {
                    throw new Exception("Multiple Parent Id Found");
                }
                return referenceDescriptionCollection[0];
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private static (BrowseDescriptionCollection, ByteStringCollection) GetNewContinuationPoints(
            BrowseDescriptionCollection nodesToBrowse,
            BrowseResultCollection results)
        {
            var unprocessedOperations = new BrowseDescriptionCollection();
            var continuationPoints = new ByteStringCollection();
            for (int ii = 0; ii < nodesToBrowse.Count; ii++)
            {
                // check for error.
                if (StatusCode.IsBad(results[ii].StatusCode))
                {
                    // this error indicates that the server does not have enough simultaneously active 
                    // continuation points. This request will need to be resent after the other operations
                    // have been completed and their continuation points released.
                    if (results[ii].StatusCode == StatusCodes.BadNoContinuationPoints)
                    {
                        unprocessedOperations.Add(nodesToBrowse[ii]);
                    }

                    continue;
                }
                if (results[ii].References.Count == 0)
                {
                    continue;
                }
                if (results[ii].ContinuationPoint != null) { continuationPoints.Add(results[ii].ContinuationPoint); }
            }
            return (unprocessedOperations, continuationPoints);
        }

        private static ByteStringCollection GetNewContinuationPoints(
            ByteStringCollection continuationpoints,
            BrowseResultCollection results)
        {
            var revisedContiuationPoints = new ByteStringCollection();
            for (int ii = 0; ii < continuationpoints.Count; ii++)
            {
                if (StatusCode.IsBad(results[ii].StatusCode))
                {
                    continue;
                }
                if (results[ii].References.Count == 0)
                {
                    continue;
                }
                if (results[ii].ContinuationPoint != null) { revisedContiuationPoints.Add(results[ii].ContinuationPoint); }
            }

            return revisedContiuationPoints;
        }

        private static IEnumerable<byte[]> GetContinuationPoints(BrowseResponse results) => GetContinuationPoints(
            results.Results);

        private static IEnumerable<byte[]> GetContinuationPoints(BrowseNextResponse results) => GetContinuationPoints(
            results.Results);

        private static IEnumerable<byte[]> GetContinuationPoints(BrowseResultCollection results) => results
                .Select(o => o.ContinuationPoint)
            .Where(o => o != null && o.Length > 1);

        private static ReferenceDescriptionCollection GetDescriptions(BrowseResponse results) => GetDescriptions(
            results.Results);

        private static ReferenceDescriptionCollection GetDescriptions(BrowseNextResponse results) => GetDescriptions(
            results.Results);

        private static ReferenceDescriptionCollection GetDescriptions(BrowseResultCollection results)
        {
            ReferenceDescriptionCollection temp = new ReferenceDescriptionCollection();
            foreach (ReferenceDescriptionCollection item in
                results.Where(o => StatusCode.IsNotBad(o.StatusCode))
                       .Select(o => o.References))
                temp.AddRange(item);

            return temp;
        }

        public BrowseDescription GetNodeHierarchalBrowseDescription(NodeId node) => new BrowseDescription()
        {
            NodeId = node,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
            NodeClassMask = 255u,
            ResultMask = (uint)BrowseResultMask.All
        };

        public async Task<ReferenceDescriptionCollection> AsyncBrowseNode(
            ReferenceDescription node,
            CancellationToken token) => await AsyncBrowseNode(
            ExpandedNodeId.ToNodeId(node.NodeId, session?.NamespaceUris),
            token);



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
            //NamespaceTable wellKnownNamespaceUris = new NamespaceTable();
            //if (uris != null)
            //{
            //    for (int i = 0; i < uris.Length; i++) wellKnownNamespaceUris.Append(uris[i]);
            //}
            //for (uint i = 0; i < WellKnownNameSpaces.Count; i++) wellKnownNamespaceUris.Append(WellKnownNameSpaces.GetString(i));

            //wellKnownNamespaceUris.Append(Namespaces.OpcUa);
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
            ValidateResponse(diagnostics);
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

            parseString = GetTypeDictionary(nodeIdString, session);
            PreparedNodeTypes.Add(nodeIdString, parseString);
            return GetTypeEncoding(parseString);
        }

        public NodeTypeData GetTypeEncoding(string parseString)
        {
            NodeTypeData value;
            if (PreparedTypes.TryGetValue(parseString, out value))
            {
                return value;
            }

            //trying updating the type data

            UpdateTypeData();

            if (PreparedTypes.TryGetValue(parseString, out value))
            {
                return value;
            }

            throw new Exception("Type not found");
        }

        private void UpdateTypeData()
        {
            Xmls.Clear();
            Xmls.AddRange(this.GetServerTypeSchema().Select(o => { var temp = new XmlDocument(); temp.LoadXml(o); return temp; }));
            var dict = UpdateNodeType(Xmls);
            foreach (var o in dict)
            {
                if (PreparedTypes.ContainsKey(o.Key)) PreparedTypes.Remove(o.Key);
                PreparedTypes.Add(o.Key, o.Value);
            }
        }

        private static string GetTypeDictionary(string nodeIdString, Session session)
        {
            


            //Read the desired node first and chekc if it's a variable
            Node node = session.ReadNode(nodeIdString);
            if (node.NodeClass != NodeClass.Variable)
            {
                Exception ex = new Exception("No variable data type found");
                throw ex;
            }
            //Get the node id of node's data type
            VariableNode variableNode = (VariableNode)node.DataLock;
            NodeId nodeId = (NodeId)variableNode.DataType;

            //Browse for HasEncoding
            ReferenceDescriptionCollection refDescCol;
            byte[] continuationPoint;
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
                //Read from node id of the found description to get a value to parse for later on
                nodeId = new NodeId(refDescCol2[0].NodeId.Identifier, refDescCol2[0].NodeId.NamespaceIndex);
                DataValue resultValue = session.ReadValue(nodeId);
                return resultValue.Value.ToString();
                //parseString;
                //Browse for ComponentOf from last browsing result inversly
                //session.Browse(null, null, nodeId, 0u, BrowseDirection.Inverse, ReferenceTypeIds.HasComponent, true, 0, out _, out refDescCol);

            }
            else
            {
                //var resultValue = session.ReadValue();
                return nodeId.ToString();
                //parseString;
                //Browse for ComponentOf from last browsing result inversly
                //session.Browse(null, null, nodeId, 0u, BrowseDirection.Inverse, ReferenceTypeIds.HasComponent, true, 0, out _, out refDescCol);
            }



            //
            //Check if reference was found
            //if (refDescCol.Count == 0)
            //{
            //  Exception ex = new Exception("Data type isn't a component of parent type in address space. Can't continue decoding.");
            //  throw ex;
            //}

            //Read from node id of the found HasCompoment reference to get a XML file (as HEX string) containing struct/UDT information

            //nodeId = new NodeId(refDescCol[0].NodeId.Identifier, refDescCol[0].NodeId.NamespaceIndex);
            //resultValue = session.ReadValue(nodeId);

            //Convert the HEX string to ASCII string
            //string xmlString = Encoding.ASCII.GetString((byte[])resultValue.Value);

            //Return the dictionary as ASCII string

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
                _ => throw new Exception("Unkown encoding"),
            };
        }

        private static Dictionary<string, NodeTypeData> UpdateNodeType(List<XmlDocument> xmls)
        {
            var dict = new Dictionary<string, NodeTypeData>();

            foreach (var item in xmls)
            {
                foreach (XmlNode child in item.GetElementsByTagName("opc:StructuredType"))
                {
                    var nodetype = new NodeTypeData();
                    nodetype.Name = GetCorrectedName(child);
                    nodetype.TypeName = GetCorrectedName(child);
                    nodetype.ChildData = new List<NodeTypeData>();
                    foreach (XmlNode child2 in child.ChildNodes)
                    {
                        if (child2.Name == "opc:Field")
                        {
                            var childNode = new NodeTypeData();
                            childNode.Name = GetCorrectedName(child2);
                            childNode.TypeName = GetCorrectedTypeName(child2);
                            childNode.ChildData = new List<NodeTypeData>();
                            nodetype.ChildData.Add(childNode);
                        }
                    }
                    dict.Add(nodetype.Name, nodetype);
                }
            }

            foreach (var item in dict)
            {
                var type = item.Value;
                AddChilderen(dict, type);
            }
            return dict;
        }

        private static void AddChilderen(Dictionary<string, NodeTypeData> dict, NodeTypeData type)
        {
            for (int i = 0; i < type.ChildData.Count; i++)
            {
                var child = type.ChildData[i];
                if (dict.ContainsKey(child.TypeName))
                {
                    type.ChildData[i].ChildData = dict[child.TypeName].ChildData;
                    foreach (var item in type.ChildData[i].ChildData)
                    {
                        AddChilderen(dict, item);
                    }
                }
            }
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
            var body = ReadValue(nodeEntry.GetNodeId());
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
            catch (Exception _)
            {
                throw AggrigateAllRecordCreationErrors(list, tempResult);
            }
        }
        public AggregateException AggrigateAllRecordCreationErrors(ValueNodeEntryCollection list, List<object> tempResult)
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
        public AggregateException AggrigateAllRecordCreationErrors(ValueNodeEntryCollection list, DataValueCollection tempResult)
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
            catch (Exception _)
            {
                throw AggrigateAllRecordCreationErrors(list, tempResult);
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
                ValidateResponse(serviceResults.Select(o => o.StatusCode));

                return values.Select(v => v.Value).ToList();
            };

            return (List<object>)await HandleTask(task);
        }

        public IEncodeable ConstructEncodable(ValueNodeEntry entry, byte[] encodedData)
        {
            IEncodeable objresult = (IEncodeable)entry.Constructor.Invoke(new object[0]);
            objresult.Decode(new BinaryDecoder(encodedData, session.MessageContext));
            return objresult;
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
            WriteValueCollection writeCollection = new WriteValueCollection();
            writeCollection.Add(CreateWriteValue(nodeEntry.GetNodeId(), value));
            WriteValues(writeCollection);
        }
        public void WriteValue(NodeId nodeId, object value)
        {
            WriteValueCollection writeCollection = new WriteValueCollection();
            writeCollection.Add(CreateWriteValue(nodeId, value));
            WriteValues(writeCollection);
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
            return new WriteValue()
            {
                NodeId = nodeId,
                Value = new DataValue(new Variant(value)),
                AttributeId = Attributes.Value
            };
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
                ValidateResponse(statusResults.Select(o => o.StatusCode));
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
                ValidateResponse(statusResults.Select(o => o.StatusCode));
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
                ValidateResponse(statusResults.Select(o => o.StatusCode));
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
                ValidateResponse(serviceResults);
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
                ValidateResponse(serviceResults);
                ValidateResponse(diag);
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
                ValidateResponse(response.Results);
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
        /// <returns>Argument informations as strings</returns>
        /// <exception cref="Exception">Throws and forwards any exception with short error description.</exception>
        public MethodArguments GetMethodArguments(string nodeIdString)
            => GetMethodArguments(new NodeId(nodeIdString));

        public MethodArguments GetMethodArguments(NodeId nodeId)
        {
            var methode = new MethodArguments();

            try
            {
                Node methodNode = ReadNode(nodeId);

                if (methodNode.NodeClass != NodeClass.Method) { throw new ServiceResultException(StatusCodes.BadNodeClassInvalid); }

                //We need to browse for property (input and output arguments)
                //Create a collection for the browse results
                ReferenceDescriptionCollection referenceDescriptionCollection;
                ReferenceDescriptionCollection nextreferenceDescriptionCollection;
                //Create a continuationPoint
                byte[] continuationPoint;
                byte[] revisedContinuationPoint;

                //Start browsing
                //Browse from starting point for properties (input and output)
                session.Browse(
                    null,
                    null,
                    nodeId,
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HasProperty,
                    true,
                    0,
                    out continuationPoint,
                    out referenceDescriptionCollection);

                while (continuationPoint != null)
                {
                    session.BrowseNext(
                        null,
                        false,
                        continuationPoint,
                        out revisedContinuationPoint,
                        out nextreferenceDescriptionCollection);
                    referenceDescriptionCollection.AddRange(nextreferenceDescriptionCollection);
                    continuationPoint = revisedContinuationPoint;
                }

                //Gaurd Clause
                if (referenceDescriptionCollection == null || referenceDescriptionCollection.Count <= 0)
                {
                    return methode;
                }

                foreach (ReferenceDescription refDesc in referenceDescriptionCollection)
                {
                    ArgumentCollection arguments;
                    //Get correct collection
                    if (refDesc.NodeClass != NodeClass.Variable)
                        continue;
                    if (refDesc.BrowseName.Name == "InputArguments")
                        arguments = methode.InputArguments;
                    else if (refDesc.BrowseName.Name == "OutputArguments")
                        arguments = methode.OutputArguments;
                    else
                        continue;

                    List<NodeId> nodeIds = new List<NodeId>()
                    {
                        ExpandedNodeId.ToNodeId(refDesc.NodeId, session.NamespaceUris)
                    };
                    List<Type> types = new List<Type>() { null };

                    //Read the input/output arguments
                    session.ReadValues(nodeIds, types, out List<object> values, out List<ServiceResult> serviceResults);

                    ServiceResult bad = serviceResults.FirstOrDefault(o => StatusCode.IsNotGood(o.StatusCode));
                    if (bad != null) throw new Exception(bad.ToString());

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
                        methode.InputArgumentTypes = new Type[arguments.Count];
                        argumentTypes = methode.InputArgumentTypes;
                    }
                    else
                    {
                        methode.OutputArgumentTypes = new Type[arguments.Count];
                        argumentTypes = methode.OutputArgumentTypes;
                    }
                    for (int i = 0; i < arguments.Count; i++)
                    {
                        argumentTypes[i] = Opc.Ua.TypeInfo.GetSystemType(
                                                                   arguments[i].DataType,
                                                                   session.Factory);
                    }
                }

                return methode;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public IList<object> CallMethod(NodeId objectNodeId, NodeId methodNodeId, params object[] inputArguments)
            => Session.Call(
            objectNodeId,
            methodNodeId,
            inputArguments ?? new object[0]);




        public IList<object> CallMethods(CallMethodRequestCollection methodRequests)
        {
            Func<IList<object>> task = delegate ()
            {
                RequestHeader requestHeader = new RequestHeader();
                var responceHeader = Session.Call(requestHeader, methodRequests, out var results, out var diagnotics);
                ValidateResponse(diagnotics);

                object[] output = new object[results.Count];

                for (int i = 0; i < results.Count; i++)
                {
                    if (StatusCode.IsBad(results[i].StatusCode))
                    {
                        output[i] = results[i].StatusCode;
                    }
                    else if (results[i].OutputArguments.Count > 1)
                    {
                        output[i] = results[i].OutputArguments
                            .Select(o => o.Value)
                            .ToArray();
                    }
                    else if (results[i].OutputArguments.Count == 1)
                    {
                        output[i] = results[i].OutputArguments[0].Value;
                    }
                    else
                    {
                        output[i] = null;
                    }
                }
                return output;
            };

            return HandleTask(task);
        }
        #endregion

        #region Subscription

        /// <summary>
        /// Creats a Subscription object to a server
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
            catch (Exception e)
            {
                throw e;
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
            catch (Exception e)
            {
                throw e;
            }
            return monitoredItem;
        }
        public MonitoredItem AddMonitoredItem(
                Subscription subscription,
                NodeId nodeEntry,
                MonitoredNodeValueEventHandler handler = null)
        {
            MonitoredItem monitoredItem = CreateMonitoredItem(nodeEntry, handler: handler);
            try
            {
                subscription.AddItem(monitoredItem);
                subscription.ApplyChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
            return monitoredItem;
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
            catch (Exception e)
            {
                throw e;
            }
        }

        public MonitoredItem AddMonitoredItem(
                TimeSpan publishingInterval,
                ValueNodeEntry nodeEntry,
                MonitoredNodeValueRecordEventHandler handler = null) => AddMonitoredItem(
                GetSubscription(publishingInterval),
                nodeEntry,
                handler);

        public MonitoredItem AddMonitoredItem(
                TimeSpan publishingInterval,
                NodeId nodeId,
                MonitoredNodeValueEventHandler handler = null) => AddMonitoredItem(
                GetSubscription(publishingInterval),
                nodeId,
                handler);
        public MonitoredItem AddMonitoredItem(
                int publishingIntervalMilliSec,
                ValueNodeEntry nodeEntry,
                MonitoredNodeValueRecordEventHandler handler = null) => AddMonitoredItem(
                GetSubscription(publishingIntervalMilliSec),
                nodeEntry,
                handler);
        public MonitoredItem AddMonitoredItem(
                int publishingIntervalMilliSec,
                NodeId nodeId,
                MonitoredNodeValueEventHandler handler = null) => AddMonitoredItem(
                GetSubscription(publishingIntervalMilliSec),
                nodeId,
                handler);

        public IEnumerable<MonitoredItem> AddMonitoredItems(TimeSpan publishingInterval, ValueNodeEntryCollection nodeEntrys)
            => AddMonitoredItems(
                GetSubscription(publishingInterval),
                nodeEntrys);

        public IEnumerable<MonitoredItem> AddMonitoredItems(int publishingIntervalMilliSec, ValueNodeEntryCollection nodeEntrys)
            => AddMonitoredItems(
                GetSubscription(publishingIntervalMilliSec),
                nodeEntrys);
        public Subscription GetSubscription(TimeSpan publishingInterval) => GetSubscription(publishingInterval.Milliseconds);


        public Subscription GetSubscription(int publishingIntervalMilliSec)
        {
            var subscription = subscriptions.FirstOrDefault(o => o.PublishingInterval == publishingIntervalMilliSec);
            return subscription != null ? subscription : CreateSubscription(publishingIntervalMilliSec);
        }

        public IEnumerable<MonitoredItem> AddMonitoredItems(Subscription subscription, ValueNodeEntryCollection nodeEntrys)
        {
            IEnumerable<MonitoredItem> items = nodeEntrys.NodeEntries.Select(o => CreateMonitoredItem(o));
            try
            {
                subscription.AddItems(items);
                subscription.ApplyChanges();
            }
            catch (Exception e)
            {
                throw e;
            }
            return items;
        }

        public MonitoredItem CreateMonitoredItem(
            ValueNodeEntry nodeEntry,
            int samplingInterval = 1,
            uint queueSize = 1,
            bool discardOldest = true,
            bool globalCall = false,
            MonitoredNodeValueRecordEventHandler handler = null)
        {
            MonitoredItem monitoredItem = CreateMonitoredItem(nodeEntry, samplingInterval, queueSize, discardOldest);
            if (handler != null)
            {
                monitoredItem.Notification += (sender, arg) => MoniteredNode(nodeEntry, arg, handler);
            }
            if (globalCall)
            {
                monitoredItem.Notification += (sender, arg) => MoniteredNode(nodeEntry, arg, NodeChangedNotification);
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
            MonitoredItem monitoredItem = CreateMonitoredItem(nodeId, samplingInterval, queueSize, discardOldest);
            if (handler != null)
            {
                monitoredItem.Notification += (sender, arg) => MoniteredNode(sender, arg, handler);
            }
            if (globalCall)
            {
                monitoredItem.Notification += (sender, arg) => MoniteredNode(sender, arg, NodeChangedNotification);
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

        public void MoniteredNode(
                MonitoredItem monitorItem,
                MonitoredItemNotificationEventArgs arg,
                MonitoredNodeValueEventHandler handeler)
        {
            var value = GetCorrectValue(((MonitoredItemNotification)arg.NotificationValue).Value.Value);
            handeler.Invoke(monitorItem, value);
        }

        public void MoniteredNode(
                MonitoredItem monitorItem,
                MonitoredItemNotificationEventArgs arg,
                MonitoredNodeValueRecordEventHandler handeler)
        {
            var value = GetCorrectValue(((MonitoredItemNotification)arg.NotificationValue).Value.Value);
            handeler.Invoke(monitorItem, new NodeValueRecord(new ValueNodeEntry(monitorItem.StartNodeId, value.GetType()), value));
        }

        public void MoniteredNode(
            ValueNodeEntry entry,
            MonitoredItemNotificationEventArgs arg,
            MonitoredNodeValueRecordEventHandler handler)
        {
            if (!entry.IsUDT)
            {
                handler.Invoke(
                    this,
                    CreateNodeValue(entry, ((MonitoredItemNotification)arg.NotificationValue).Value.Value));
            }
            else
            {
                // item.Decode(arg.NotificationValue);
                ExtensionObject obj = ((ExtensionObject)((MonitoredItemNotification)arg.NotificationValue).Value.Value);
                if (obj.Encoding == ExtensionObjectEncoding.Binary)
                {
                    handler.Invoke(this, entry.CreateRecord(ConstructEncodable(entry, (byte[])obj.Body)));
                }
                else
                {
                    logger.Error($"No Decoding methode for monitored item {entry.NodeString}");
                }
            }
        }

        /// <summary>
        /// Removs a monitored item from an existing subscription
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
                catch (Exception e)
                {
                    throw e;
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
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
        }

        public void RemoveMonitoredItems(ValueNodeEntryCollection entrys)
        {
            foreach (var entry in entrys.NodeEntries) RemoveMonitoredItem(entry);
        }
        #endregion

        #region Validation
        private static void ValidateResponse(IList<ServiceResult> diagnostics)
            => ValidateResponse(diagnostics.Select(o => o.StatusCode));

        private static void ValidateResponse(DiagnosticInfoCollection diagnostics)
            => ValidateResponse(diagnostics.Select(o => o.InnerStatusCode));

        private static void ValidateResponse(StatusCodeCollection diagnostics)
            => ValidateResponse(diagnostics.Select(o => o));

        private static void ValidateResponse(IEnumerable<StatusCode> response)
        {
            string message = response.Where(o => StatusCode.IsNotGood(o.Code))
                .Aggregate(string.Empty, (accumulator, result) =>
                accumulator += $"{result} : {StatusCodes.GetBrowseName(result.Code)}" + Environment.NewLine);

            if (message.Length > 0) throw new ServiceResultException(message);
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

    }
}