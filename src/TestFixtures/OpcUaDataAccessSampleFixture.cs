using Autabee.Communication.ManagedOpcClient;
using Microsoft.Extensions.Configuration;
using Opc.Ua;
using System;

namespace AutabeeTestFixtures
{
    public class OpcUaDataAccessSampleFixture : IDisposable
    {
        protected readonly ApplicationDescription server;
        public bool SkipServerNotFound { get; private set; }
        public OpcUaClientHelperApi Communicator { get; private set; }

        //private readonly bool UseAnonymous = false;
        protected readonly EndpointDescriptionCollection endpoints;

        protected string endpointString = "opc.tcp://localhost:62548/Quickstarts/DataAccessServer";
        protected TestUser plcUser;

        public OpcUaDataAccessSampleFixture()
        { 
            InitializeDefaultCommunicator();
        }

        private void InitializeDefaultCommunicator(bool force = false)
        {
            if (Communicator == null || force)
            {
                try
                {
                    Communicator = new OpcUaClientHelperApi("Autabee", "xUnitApiTester", "Autabee.Tester");
                    var task = Communicator.Connect(endpointString, userIdentity: new UserIdentity());
                    task.Wait(10000);
                    SkipServerNotFound = task.IsCompleted && !Communicator.Connected;
                }
                catch (Exception e)
                {
                    //Logger.Error("Failed to connect", e);
                    SkipServerNotFound = true;
                }
            }
        }

        public void Dispose()
        {
            Communicator?.Disconnect();
        }
    }
}