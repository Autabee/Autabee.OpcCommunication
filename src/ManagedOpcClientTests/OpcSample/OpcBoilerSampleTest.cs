using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.OpcCommunicator;
using Autabee.Utility.Logger;
using Autabee.Utility.Logger.xUnit;
using AutabeeTestFixtures;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcBoilerSampleTest : IClassFixture<OpcBoilerSampleFixture>
    {
        private readonly OpcUaClientHelperApi communicator;
        private readonly bool skipServerNotFound;
        private readonly IAutabeeLogger logger;

        public OpcBoilerSampleTest(OpcBoilerSampleFixture testPlcTestsFixture, ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
            logger = new AutabeeXunitLogger(outputHelper);
        }

        [SkippableFact]
        public void ConnectWithTestServer()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            logger.Information("Connected with Sample Alarm Server");
            var root = communicator.BrowseRoot();
            foreach (var item in root)
            {
                logger.Information(item.NodeId.ToString());
            }
        }

        [SkippableFact]
        public void GetServerStructure()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var types = communicator.GetServerTypeSchema();
            foreach (var item in types)
            {
                logger.Information(item);
            }
        }


        [SkippableFact]
        public async void ReconnectRegisteredNodes()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var communicator2 = new OpcUaClientHelperApi("Autabee","scoutclient", "autabeeopcscout", logger);
            var endpoints = OpcUaClientHelperApi.GetEndpoints("opc.tcp://laptop131:62567/Quickstarts/BoilerServer");
            await communicator2.Connect(endpoints.First());
        }
    }
}