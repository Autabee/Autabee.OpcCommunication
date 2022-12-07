using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Communication.OpcCommunicator;
using Autabee.Utility.Logger;
using Autabee.Utility.Logger.xUnit;
using AutabeeTestFixtures;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcDaSampleTest : IClassFixture<OpcUaDataAccessSampleFixture>
    {
        private readonly OpcUaClientHelperApi communicator;
        private readonly bool skipServerNotFound;
        private readonly IAutabeeLogger logger;

        public OpcDaSampleTest(OpcUaDataAccessSampleFixture testPlcTestsFixture, ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
            logger = new AutabeeXunitLogger(outputHelper);
        }

        [SkippableFact]
        public void ReadNodeWithValueEntry()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            if (!(communicator.ReadNodeValue(new ValueNodeEntry<double>("ns=2;s=1:Pipe1001?Online/ValuePrecision")) is NodeValueRecord<double> _))
            {
                Assert.Fail("Return type incorrect at ns=2;s=1:Pipe1001?Online/ValuePrecision");
            }

            var entry = new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/Definition");
            if (communicator.ReadNodeValue(entry) is NodeValueRecord<string> result1)
            {
                Assert.Null(result1.Value);
            }

            if (!(communicator.ReadNodeValue(new ValueNodeEntry<float>("ns=2;s=1:FC1001?SetPoint")) is NodeValueRecord<float> _))
            {
                Assert.Fail("Return type incorrect at ns=2;s=1:FC1001?Setpoint");
            }
        }
        [SkippableFact]
        public void ReadNodeWithValueCollections()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            ValueNodeEntryCollection collection = new ValueNodeEntryCollection();
            collection.AddRange(new ValueNodeEntry[]
            {
                new ValueNodeEntry<double>("ns=2;s=1:Pipe1001?Online/ValuePrecision")
                ,new ValueNodeEntry<float>("ns=2;s=1:FC1001?SetPoint")
            });
            collection.Add(new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/Definition"));

            communicator.ReadValues(collection);
        }
        [SkippableFact]
        public void ReadNodeWithValueEntryException()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            Assert.Throws<ArgumentException>( 
                () => communicator.ReadNodeValue(new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/ValuePrecision")));
            Assert.Throws<ArgumentException>(
                () => communicator.ReadNodeValue(new ValueNodeEntry<double>("ns=2;s=1:Pipe1001?Online/Definition")));
            Assert.Throws<ArgumentException>(
                () => communicator.ReadNodeValue(new ValueNodeEntry<uint>("ns=2;s=1:FC1001?SetPoint")));
        }

        [SkippableFact]
        public void ReadNodeWithValueCollectionsException()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            ValueNodeEntryCollection collection = new ValueNodeEntryCollection();
            collection.AddRange(new ValueNodeEntry[]
            {
                new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/ValuePrecision")
                ,new ValueNodeEntry<uint>("ns=2;s=1:FC1001?SetPoint")
            });
            collection.Add(new ValueNodeEntry<float>("ns=2;s=1:Pipe1001?Online/Definition"));

            Assert.Throws<AggregateException>(() => communicator.ReadValues(collection));
        }
    }
}