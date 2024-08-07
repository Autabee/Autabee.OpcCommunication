using Autabee.Communication.ManagedOpcClient;
using Opc.Ua;
using System;

namespace AutabeeTestFixtures
{
    public class OpcUaTypeSampleFixture : IDisposable
    {
        protected readonly ApplicationDescription server;
        public bool SkipServerNotFound { get; private set; }
        public AutabeeManagedOpcClient Communicator { get; private set; }

        //private readonly bool UseAnonymous = false;
        protected readonly EndpointDescriptionCollection endpoints;

        protected string endpointString = "opc.tcp://localhost:62555/DataTypesServer";
        protected TestUser plcUser;

        public OpcUaTypeSampleFixture()
        {
            InitializeDefaultCommunicator();
        }

        private void InitializeDefaultCommunicator(bool force = false)
        {
            if (Communicator == null || force)
            {
                try
                {
                    Communicator = new AutabeeManagedOpcClient("Autabee", "UnitX.ApiTester", "Autabee.Tester");
                    var task = Communicator.Connect(endpointString, userIdentity: new UserIdentity());
                    task.Wait(10000);
                    SkipServerNotFound = task.IsCompleted && !Communicator.Connected;
                }
                catch (Exception)
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