using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Communication.OpcCommunicator;
using Autabee.Utility.Logger;
using Autabee.Utility.Logger.xUnit;
using AutabeeTestFixtures;
using System;
using System.Linq;
using System.Xml.XPath;
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

            if (!(communicator.ReadValue(new ValueNodeEntry<double>("ns=2;s=1:Pipe1001?Online/ValuePrecision")) is NodeValueRecord<double> _))
            {
                Assert.Fail("Return type incorrect at ns=2;s=1:Pipe1001?Online/ValuePrecision");
            }

            var entry = new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/Definition");
            if (communicator.ReadValue(entry) is NodeValueRecord<string> result1)
            {
                Assert.Null(result1.Value);
            }

            if (!(communicator.ReadValue(new ValueNodeEntry<float>("ns=2;s=1:FC1001?SetPoint")) is NodeValueRecord<float> _))
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
                () => communicator.ReadValue(new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/ValuePrecision")));
            Assert.Throws<ArgumentException>(
                () => communicator.ReadValue(new ValueNodeEntry<double>("ns=2;s=1:Pipe1001?Online/Definition")));
            Assert.Throws<ArgumentException>(
                () => communicator.ReadValue(new ValueNodeEntry<uint>("ns=2;s=1:FC1001?SetPoint")));
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

        [SkippableFact]
        public void ReadNodeValueRange()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var temp = communicator.ReadValues(new ValueNodeEntryCollection(){
                new ValueNodeEntry<object>("ns=2;s=1:Pipe1001?Measurement/EURange"),
                new ValueNodeEntry<object>("ns=2;s=1:Pipe1001?Measurement/EngineeringUnits")
            });

            Assert.True(((Opc.Ua.ExtensionObject)temp[0].Value).Body is Opc.Ua.Range);
            Assert.True(((Opc.Ua.ExtensionObject)temp[1].Value).Body is Opc.Ua.EUInformation);
        }


        [SkippableFact]
        public void ReadNode()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var types = communicator.ReadValue("ns=2;s=1:FC1001?SetPoint/ValuePrecision");
            Assert.True(types.GetType() == typeof(double));
        }
        [SkippableFact]
        public void ReadNode2()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var types = communicator.ReadValue<double>("ns=2;s=1:FC1001?SetPoint/ValuePrecision");
            Assert.True(types.GetType() == typeof(double));
        }
    }
}