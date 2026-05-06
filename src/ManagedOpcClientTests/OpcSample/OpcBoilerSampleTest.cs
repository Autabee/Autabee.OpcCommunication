using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.Utilities;
using AutabeeTestFixtures;
using Serilog;
using Serilog.Sinks.XUnit3;
using System.Linq;
using Xunit;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcBoilerSampleTest : IClassFixture<OpcUaBoilerSampleFixture>
    {
        private readonly AutabeeManagedOpcClient communicator;
        private readonly bool skipServerNotFound;


        public OpcBoilerSampleTest(OpcUaBoilerSampleFixture testPlcTestsFixture, ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
            
        }

        [Fact]
        public void BrowseRoot()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var root = communicator.BrowseRoot();
            var roota = communicator.BrowseNodes(root);
            foreach (var item in root)
            {
                //logger.Information(item.NodeId.ToString());
            }
        }

        [Fact]
        public void GetServerStructure()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var types = communicator.GetServerTypeSchema();
            foreach (var item in types)
            {
                //logger.Information(item);
            }
        }
    }
}