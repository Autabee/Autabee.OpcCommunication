using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Utility.Logger;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Autabee.Communication.ManagedOpcClient
{
    public delegate void MonitoredNodeValueEventHandler(OpcUaClientHelperApi sender, NodeValue e);

    public class OpcUaClientHelperApi
    {
        private bool closing;
        private readonly IAutabeeLogger logger;
        private ApplicationConfiguration mApplicationConfig;
        private ConfiguredEndpoint mEndpoint;

        private Session session;
        private string sessionName;
        private List<Subscription> Subscriptions = new List<Subscription>();

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
        public event MonitoredNodeValueEventHandler NodeChangedNotification;
        internal event EventHandler ReInstateNodeEntries;

        #region xml
        public string[] GetServerTypeSchema(bool all = false)
        {
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

            //ReferenceDescriptionCollection refDescCol = new ReferenceDescriptionCollection();
            //refDescCol.AddRange(refDescColBin);
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
            var values = ReadValues(nodeIds, null);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] != null) { result.Add(Encoding.ASCII.GetString((byte[])values[i].Value)); }
            }
            //result.RemoveAll(o => string.IsNullOrEmpty(o));

            return result.ToArray();
        }
        #endregion xml

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
        public OpcUaClientHelperApi(string company, string product, string directory, IAutabeeLogger logger = null)
        {
            this.logger = logger;
            // Create's the application configuration (containing the certificate) on construction
            mApplicationConfig = GetClientConfiguration(company, product,directory,logger);
        }
        public OpcUaClientHelperApi(Stream stream, IAutabeeLogger logger = null)
        {
            this.logger = logger;
            // Create's the application configuration (containing the certificate) on construction
            mApplicationConfig = CreateDefaultClientConfiguration(stream);
        }

        public OpcUaClientHelperApi(ApplicationConfiguration opcAppConfig, IAutabeeLogger logger = null)
        {
            this.logger = logger;
            mApplicationConfig = opcAppConfig;
        }
        #endregion Construction

        #region Registration

        public void RegisterNodeIds(NodeEntryCollection preparedCollection, bool AutoReregister = true)
        {
            var nodelist = preparedCollection.PreparedNodes;
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
            NodeIdCollection registeredNodes;
            try
            {
                if (NoSession()) return new NodeIdCollection();
                //Register nodes
                var responce = session.RegisterNodes(null, nodesToRegister, out registeredNodes);
                bool failRegister = false;
                string failRegisterMessage = "";
                foreach (var item in registeredNodes.Where(o => o.IdType == IdType.String))
                {
                    failRegister = true;
                    logger?.Error("Failed to register node: " + item.ToString());
                    failRegisterMessage += item.ToString() + Environment.NewLine;
                }
                if (failRegister)
                {
                    throw new Exception("Failed to register nodes" + Environment.NewLine + failRegisterMessage);
                }
                //responce.ServiceResult;
                return registeredNodes;
            }
            catch (Exception e)
            {
                //handle Exception here
                throw;
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
        {
            var unregistered = new NodeIdCollection() { nodeToRegister };
            return RegisterNodeIds(unregistered)[0];
        }

        public void UnregisterNodeIds(NodeIdCollection nodesToUnregister)
        {
            try
            {
                //Register nodes
                session.UnregisterNodes(null, nodesToUnregister);
            }
            catch (Exception e)
            {
                //handle Exception here
                throw e;
            }
        }

        # endregion Registration

        #region Discovery
        public ApplicationDescription GetConnectedServer()
        {
            if (session == null) return null;

            return FindServers(session.ConfiguredEndpoint.EndpointUrl.AbsoluteUri)[0];
        }

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
            var endpoints = await client.GetEndpointsAsync(null, "", null, null, token);

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
        public async Task Connect(EndpointDescription endpointDescription, UserIdentity userIdentity = null)
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
                Subscriptions.Clear();
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
                    var status = session.Close(10000);
                    if (session != null)
                    {
                        session.KeepAlive -= Notification_KeepAlive;
                    }
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

        
        #endregion Connect/Disconnect

        #region Defaults
        public static ApplicationConfiguration GetClientConfiguration(string company, string product, string directory, IAutabeeLogger logger = null)
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

        private static void CreateDefaultConfiguration(string company, string product, string directory, IAutabeeLogger logger, string combined)
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

#if NET48_OR_GREATER || NET5_0_OR_GREATER
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
        public ReferenceDescriptionCollection BrowseNode(ReferenceDescription refDesc) => BrowseNode(
            ExpandedNodeId.ToNodeId(refDesc.NodeId, session.NamespaceUris));

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

        //public object GetNodeTypeEncoding(string nodeIdString)
        //{
        //    return GetNodeTypeEncoding(new NodeId(nodeIdString));
        //}

        //public object GetNodeTypeEncoding(NodeId nodeIdString)
        //{
        //    // string xmlString
        //    return GetTypeDictionary(nodeIdString, session, out string parseString);
        //    //PreparedNodeTypes.Add(nodeIdString, parseString);
        //    //return GetTypeEncoding(parseString, xmlString);
        //}

        //public KeyValuePair<string, List<NodeTypeData>> GetTypeEncoding(string parseString, string xmlString)
        //{
        //    if (PreparedTypes.ContainsKey(parseString))
        //    {
        //        return new KeyValuePair<string, List<NodeTypeData>>(parseString, PreparedTypes[parseString]);
        //    }
        //    else
        //    {
        //        var varList = ParseTypeDictionary(xmlString, parseString);
        //        var item = new KeyValuePair<string, List<NodeTypeData>>(parseString, varList);
        //        PreparedTypes.Add(item.Key, item.Value);
        //        return item;
        //    }
        //}

        //private static string GetTypeDictionary(NodeId nodeIdString, Session theSessionToBrowseIn, out string parseString)
        //{
        //    //Read the desired node first and chekc if it's a variable
        //    Node node = theSessionToBrowseIn.ReadNode(nodeIdString);
        //    if (node.NodeClass == NodeClass.Variable)
        //    {
        //        //Get the node id of node's data type
        //        VariableNode variableNode = (VariableNode)node.DataLock;
        //        NodeId nodeId = new NodeId(variableNode.DataType.Identifier, variableNode.DataType.NamespaceIndex);

        //        //Browse for HasEncoding
        //        ReferenceDescriptionCollection refDescCol;
        //        byte[] continuationPoint;
        //        theSessionToBrowseIn.Browse(null, null, nodeId, 0u, BrowseDirection.Forward, ReferenceTypeIds.HasEncoding, true, 0, out continuationPoint, out refDescCol);

        //        //Check For found reference
        //        if (refDescCol.Count == 0)
        //        {
        //            Exception ex = new Exception("No data type to encode. Could be a build-in data type you want to read.");
        //            throw ex;
        //        }

        //        //Check for HasEncoding reference with name "Default Binary"
        //        foreach (ReferenceDescription refDesc in refDescCol)
        //        {
        //            if (refDesc.DisplayName.Text == "Default Binary")
        //            {
        //                nodeId = new NodeId(refDesc.NodeId.Identifier, refDesc.NodeId.NamespaceIndex);
        //            }
        //            else
        //            {
        //                Exception ex = new Exception("No default binary data type found.");
        //                throw ex;
        //            }
        //        }

        //        //Browse for HasDescription
        //        refDescCol = null;
        //        theSessionToBrowseIn.Browse(null, null, nodeId, 0u, BrowseDirection.Forward, ReferenceTypeIds.HasDescription, true, 0, out continuationPoint, out refDescCol);

        //        //Check For found reference
        //        if (refDescCol.Count == 0)
        //        {
        //            Exception ex = new Exception("No data type description found in address space.");
        //            throw ex;
        //        }

        //        //Read from node id of the found description to get a value to parse for later on
        //        nodeId = new NodeId(refDescCol[0].NodeId.Identifier, refDescCol[0].NodeId.NamespaceIndex);
        //        DataValue resultValue = theSessionToBrowseIn.ReadValue(nodeId);
        //        parseString = resultValue.Value.ToString();

        //        //Browse for ComponentOf from last browsing result inversly
        //        refDescCol = null;
        //        theSessionToBrowseIn.Browse(null, null, nodeId, 0u, BrowseDirection.Inverse, ReferenceTypeIds.HasComponent, true, 0, out continuationPoint, out refDescCol);

        //        //Check if reference was found
        //        if (refDescCol.Count == 0)
        //        {
        //            Exception ex = new Exception("Data type isn't a component of parent type in address space. Can't continue decoding.");
        //            throw ex;
        //        }

        //        //Read from node id of the found HasCompoment reference to get a XML file (as HEX string) containing struct/UDT information

        //        nodeId = new NodeId(refDescCol[0].NodeId.Identifier, refDescCol[0].NodeId.NamespaceIndex);
        //        resultValue = theSessionToBrowseIn.ReadValue(nodeId);

        //        //Convert the HEX string to ASCII string
        //        string xmlString = Encoding.ASCII.GetString((byte[])resultValue.Value);

        //        //Return the dictionary as ASCII string
        //        return xmlString;
        //    }
        //    {
        //        Exception ex = new Exception("No variable data type found");
        //        throw ex;
        //    }
        //}
        #endregion Typing

        #region Entry Read


        public NodeValue ReadNodeValue(ValueNodeEntry nodeEntry)
        {
            if (nodeEntry == null) throw new ArgumentNullException(nameof(nodeEntry));
            var body = ReadNodeValue(nodeEntry.GetNodeId());
            return CreateNodeValue(nodeEntry, body);
        }

        public NodeValue[] ReadValues(NodeEntryCollection list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return new NodeValue[0];
            var tempResult = ReadValues(list.GetNodeIds(), list.Types);
            return CreateNodeValueArray(list, tempResult);
        }

        public async Task<NodeValue[]> ReadValuesAsync(NodeEntryCollection list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return new NodeValue[0];
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

        private NodeValue[] CreateNodeValueArray(NodeEntryCollection list, List<object> tempResult)
        {
            var nodeValues = new NodeValue[tempResult.Count];
            for (int i = 0; i < tempResult.Count; i++) { nodeValues[i] = CreateNodeValue(list[i], tempResult[i]); }

            return nodeValues;
        }

        private NodeValue[] CreateNodeValueArray(NodeEntryCollection list, DataValueCollection tempResult)
        {
            var nodeValues = new NodeValue[tempResult.Count];
            for (int i = 0; i < tempResult.Count; i++) { nodeValues[i] = CreateNodeValue(list[i], tempResult[i]); }

            return nodeValues;
        }

        private NodeValue CreateNodeValue(ValueNodeEntry entry, object tempResult)
        {
            if (entry.IsUDT)
            {
                if (tempResult is Opc.Ua.DataValue dvValue)
                {
                    return entry.CreateRecord(ConstructEncodable(entry, (byte[])((ExtensionObject)dvValue.Value).Body));
                }
                else if (tempResult is ExtensionObject eoValue)
                {
                    return entry.CreateRecord(ConstructEncodable(entry, (byte[])eoValue.Body));
                }
                throw new Exception("Unknown type");
            }
            else return entry.CreateRecord(tempResult);
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
        public void WriteValue(NodeValue nodeEntry)
        {
            if (nodeEntry == null) throw new ArgumentNullException(nameof(nodeEntry));
            WriteValue(nodeEntry.NodeEntry.GetNodeId(), nodeEntry.Value);
        }

        public void WriteValue(NodeId nodeId, object value)
        {
            WriteValueCollection writeCollection = new WriteValueCollection();
            writeCollection.Add(CreateWriteValue(nodeId, value));
            WriteValues(writeCollection);
        }

        public void WriteValues(NodeValueCollection list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (list.Count == 0) return;
            WriteValues(CreateWriteCollection(list));
        }

        public async Task WriteValuesAsync(NodeValueCollection list, CancellationToken ct = default)
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

        private static WriteValueCollection CreateWriteCollection(NodeValueCollection list)
        {
            NodeIdCollection nodeCollection = list.GetNodeIds();
            var collection = new WriteValueCollection();

            for (int i = 0; i < list.Count; i++) { collection.Add(CreateWriteValue(nodeCollection[i], list.Values[i])); }

            return collection;
        }
        #endregion Entry Read

        #region Read
        public object ReadNodeValue(string nodeIdString) => ReadNodeValue(new NodeId(nodeIdString));
        public object ReadNodeValue(ExpandedNodeId nodeId, Type type = null) => ReadNodeValue((NodeId)nodeId,null);
        public object ReadNodeValue(NodeId nodeId, Type type = null) => type == null
            ? session.ReadValue(nodeId) : session.ReadValue(nodeId, type);

        public T ReadNodeValue<T>(string nodeIdString) => ReadNodeValue<T>(new NodeId(nodeIdString));

        public T ReadNodeValue<T>(NodeId nodeId) => (T)session.ReadValue(nodeId, typeof(T));

        public Node ReadNode(string nodeIdString) => ReadNode(new NodeId(nodeIdString));
        public Node ReadNode(ExpandedNodeId nodeId) => ReadNode(((NodeId)nodeId));
        public Node ReadNode(NodeId nodeId) => session.ReadNode(nodeId);

        public Node ReadNode(ReferenceDescription reference) => session.ReadNode(
            ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris));

        public NodeCollection ReadNodes(ReferenceDescriptionCollection referenceDescriptions)
        {
            if (referenceDescriptions is null || referenceDescriptions.Count == 0) return new NodeCollection();
            Func<NodeCollection> task = delegate ()
            {
                NodeIdCollection nodeIds = new NodeIdCollection();
                nodeIds.AddRange(
                    referenceDescriptions.Select(o => ExpandedNodeId.ToNodeId(o.NodeId, session.NamespaceUris)));
                session.ReadNodes(nodeIds, out var nodes, out IList<ServiceResult> statusResults);
                ValidateResponse(statusResults.Select(o => o.StatusCode));
                var nodes2 = new NodeCollection();
                nodes2.AddRange(nodes);
                return nodes2;
            };

            return (NodeCollection)HandleTask(task);
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
            return (NodeCollection)await HandleTask(task);
        }
        #endregion Read

        #region UDT Read/Write
        private static T CreateDefaultInstance<T>()
        {
            //Get the Parameterless Constructor
            var constructor = typeof(T).GetConstructors().FirstOrDefault(o => o.GetParameters().Length == 0);
            if (constructor == null) throw new TypeInitializationException(
                                          typeof(T).FullName,
                                          new Exception("No parameterless constructor"));

            return (T)constructor.Invoke(new object[0]);
        }

        public T ReadStructUdt<T>(string nodeIdString) where T : IEncodeable => ReadStructUdt<T>(
            new NodeId(nodeIdString));

        public T ReadStructUdt<T>(ValueNodeEntry nodeId) where T : IEncodeable => (T)ReadValues(new NodeEntryCollection() { nodeId })[0].Value;

        public T ReadStructUdt<T>(NodeId nodeId) where T : IEncodeable
        {
            try
            {
                T result = CreateDefaultInstance<T>();
                var buffer = (byte[])session.ReadValue(nodeId, typeof(byte[]));
                result.Decode(new BinaryDecoder(buffer, session.MessageContext));
                return result;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public ExtensionObject ReadStructUdt(string nodeId) => ReadStructUdt(new NodeId(nodeId));

        public ExtensionObject ReadStructUdt(NodeId nodeId) => (ExtensionObject)session.ReadValue(nodeId).Value;

        public ExtensionObject[] ReadArrayStructUdt(string nodeId) => ReadArrayStructUdt(new NodeId(nodeId));

        public ExtensionObject[] ReadArrayStructUdt(NodeId nodeId) => (ExtensionObject[])session.ReadValue(nodeId).Value;

        public async Task<object> ReadStructUdtAsync(NodeId nodeId) => (await session.ReadValueAsync(nodeId)).Value;

        public void WriteStructUdt(NodeId nodeId, ExtensionObject dataToWrite) { WriteValue(nodeId, dataToWrite); }
        #endregion UDT Read/Write

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

        public IList<object> CallMethod(NodeId objectNodeId, NodeId methodNodeId, object[] inputArguments)
            => Session.Call(
            objectNodeId,
            methodNodeId,
            inputArguments ?? new object[0]);

        public IList<object> CallMethod(string objectNodeString, string methodNodeString, object[] inputArguments)
            => CallMethod(
           new NodeId(objectNodeString),
           new NodeId(methodNodeString),
           inputArguments);

        public IList<object> CallMethod(NodeEntry objectEntry, MethodEntry methodEntry, object[] inputArguments)
            => CallMethod(
            objectEntry.GetNodeId(),
            methodEntry.GetNodeId(),
            inputArguments);

        public IList<object> CallMethods(IEnumerable<(NodeEntry, MethodEntry, object[])> data)
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
            return CallMethods(methodRequests);
        }

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
                Subscriptions.Add(subscription);
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
        public void AddMonitoredItem(
            Subscription subscription,
            ValueNodeEntry nodeEntry,
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
        }

        public void AddMonitoredItem(
            int publishingInterval,
            ValueNodeEntry nodeEntry,
            MonitoredNodeValueEventHandler handler = null) => AddMonitoredItem(
            GetSubscription(publishingInterval),
            nodeEntry,
            handler);

        public void AddMonitoredItems(int publishingInterval, NodeEntryCollection nodeEntrys)
            => AddMonitoredItems(
                GetSubscription(publishingInterval),
                nodeEntrys);

        private Subscription GetSubscription(int publishingInterval)
        {
            var subscription = Subscriptions.FirstOrDefault(o => o.PublishingInterval == publishingInterval);
            return subscription != null ? subscription : CreateSubscription(publishingInterval);
        }

        public void AddMonitoredItems(Subscription subscription, NodeEntryCollection nodeEntrys)
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
        }

        private MonitoredItem CreateMonitoredItem(
            ValueNodeEntry nodeEntry,
            int samplingInterval = 1,
            uint queueSize = 1,
            bool discardOldest = true,
            MonitoredNodeValueEventHandler handler = null)
        {
            MonitoredItem monitoredItem = new MonitoredItem();
            monitoredItem.DisplayName = nodeEntry.NodeString;
            monitoredItem.StartNodeId = nodeEntry.UnregisteredNodeId;
            monitoredItem.AttributeId = Attributes.Value;
            monitoredItem.MonitoringMode = MonitoringMode.Reporting;
            monitoredItem.SamplingInterval = samplingInterval;
            monitoredItem.QueueSize = queueSize;
            monitoredItem.DiscardOldest = discardOldest;
            if (handler == null)
                monitoredItem.Notification += (sender, arg) => MoniteredNode(nodeEntry, arg, NodeChangedNotification);
            else
                monitoredItem.Notification += (sender, arg) => MoniteredNode(nodeEntry, arg, handler);
            return monitoredItem;
        }

        private void MoniteredNode(
            ValueNodeEntry entry,
            MonitoredItemNotificationEventArgs arg,
            MonitoredNodeValueEventHandler handler)
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
            foreach (var subscription in this.Subscriptions)
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

        public void RemoveMonitoredItems(NodeEntryCollection entrys)
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
                logger?.Error("Failed reading values", se, null);
                throw;
            }
            catch (Exception e)
            {
                if (e is ServiceResultException se) HandleServiceResultException(se);
                logger?.Error("Failed reading values", e, null);
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
                logger?.Error("Failed reading values", se, null);
                throw;
            }
            catch (Exception e)
            {
                if (e is ServiceResultException se) HandleServiceResultException(se);
                logger?.Error("Failed reading values", e, null);
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
                logger?.Error("Failed reading values", se, null);
                throw;
            }
            catch (Exception e)
            {
                if (e is ServiceResultException se) HandleServiceResultException(se);
                logger?.Error("Failed reading values", e, null);
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