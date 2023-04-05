using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.OpcCommunicator;
using Autabee.Communication.ManagedOpcClient.Utilities;
using Autabee.Utility.Logger;
using Autabee.Utility.Logger.xUnit;
using AutabeeTestFixtures;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcBoilerSampleTest : IClassFixture<OpcUaBoilerSampleFixture>
    {
        private readonly AutabeeManagedOpcClient communicator;
        private readonly bool skipServerNotFound;
        private readonly IAutabeeLogger logger;

        public OpcBoilerSampleTest(OpcUaBoilerSampleFixture testPlcTestsFixture, ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
            logger = new AutabeeXunitLogger(outputHelper);
        }

        [SkippableFact]
        public void BrowseRoot()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var root = communicator.BrowseRoot();
            var roota = communicator.BrowseNodes(root);
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
    }
}