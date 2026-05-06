using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Communication.OpcCommunicator;
using AutabeeTestFixtures;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using Xunit;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcDaSampleTest : IClassFixture<OpcUaDataAccessSampleFixture>
    {
        private readonly AutabeeManagedOpcClient communicator;
        private readonly bool skipServerNotFound;

        public OpcDaSampleTest(OpcUaDataAccessSampleFixture testPlcTestsFixture, Xunit.ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
        }

        [Fact]
        public void ReadNodeWithValueEntry()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

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
        [Fact]
        public void ReadNodeWithValueCollections()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            ValueNodeEntryCollection collection = new ValueNodeEntryCollection();
            collection.AddRange(new ValueNodeEntry[]
            {
                new ValueNodeEntry<double>("ns=2;s=1:Pipe1001?Online/ValuePrecision")
                ,new ValueNodeEntry<float>("ns=2;s=1:FC1001?SetPoint")
            });
            collection.Add(new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/Definition"));

            communicator.ReadValues(collection);
        }
        [Fact]
        public void ReadNodeWithValueEntryException()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            Assert.Throws<ArgumentException>(
                () => communicator.ReadValue(new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/ValuePrecision")));
            Assert.Throws<ArgumentException>(
                () => communicator.ReadValue(new ValueNodeEntry<double>("ns=2;s=1:Pipe1001?Online/Definition")));
            Assert.Throws<ArgumentException>(
                () => communicator.ReadValue(new ValueNodeEntry<uint>("ns=2;s=1:FC1001?SetPoint")));
        }

        [Fact]
        public void ReadNodeWithValueCollectionsException()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            ValueNodeEntryCollection collection = new ValueNodeEntryCollection();
            collection.AddRange(new ValueNodeEntry[]
            {
                new ValueNodeEntry<string>("ns=2;s=1:Pipe1001?Online/ValuePrecision")
                ,new ValueNodeEntry<uint>("ns=2;s=1:FC1001?SetPoint")
            });
            collection.Add(new ValueNodeEntry<float>("ns=2;s=1:Pipe1001?Online/Definition"));

            Assert.Throws<AggregateException>(() => communicator.ReadValues(collection));
        }

        [Fact]
        public void ReadNodeValueRange()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var temp = communicator.ReadValues(new ValueNodeEntryCollection(){
                new ValueNodeEntry<object>("ns=2;s=1:Pipe1001?Measurement/EURange"),
                new ValueNodeEntry<object>("ns=2;s=1:Pipe1001?Measurement/EngineeringUnits")
            });

            Assert.True(((Opc.Ua.ExtensionObject)temp[0].Value).Body is Opc.Ua.Range);
            Assert.True(((Opc.Ua.ExtensionObject)temp[1].Value).Body is Opc.Ua.EUInformation);
        }


        [Fact]
        public void ReadNode()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var types = communicator.ReadValue("ns=2;s=1:FC1001?SetPoint/ValuePrecision");
            Assert.True(types.GetType() == typeof(double));
        }


        [Fact]
        public void ReadNode2()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var types = communicator.ReadValue<double>("ns=2;s=1:FC1001?SetPoint/ValuePrecision");
            Assert.True(types.GetType() == typeof(double));
        }


        [Fact]
        public void BrowseMultipleNodes()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var root  = communicator.BrowseRoot();
            communicator.BrowseNodes(root);
        }

        [Fact]
        public void ScanNodes()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var collection = new NodeIdCollection()
            {
                new NodeId("ns=2;s=1:FC1001?SetPoint/ValuePrecision"),
                new NodeId("ns=2;s=1:FC1001?SetPoint/ValuePr")
            };


            var types = communicator.ScanNodeExistences(collection);
            Assert.True(types[0]);
            Assert.False(types[1]);
        }
        [Fact]
        public void ScanNodes2()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var collection = new string[]
            {
                "ns=2;s=1:FC1001?SetPoint/ValuePrecision",
                "ns=2;s=1:FC1001?SetPoint/ValuePr"
            };


            var types = communicator.ScanNodeExistences(collection);
            Assert.True(types[0]);
            Assert.False(types[1]);
        }

        [Fact]
        public void ScanValueNodes()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var collection = new string[]
            {
                "ns=2;s=1:FC1001?SetPoint/ValuePrecision",
                "ns=2;s=1:FC1001?SetPoint/ValuePr"
            };


            var types = communicator.ScanValueNodeExistences(collection);
            Assert.True(types[0]);
            Assert.False(types[1]);
        }

        [Fact]
        public void ScanMethodNodes()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var collection = new string[]
            {
                "ns=2;s=1:FC1001?SetPoint/ValuePrecision",
                "ns=2;s=1:FC1001?SetPoint/ValuePr"
            };


            var types = communicator.ScanMethodNodeExistences(collection);
            Assert.False(types[0]);
            Assert.False(types[1]);
        }

        [Fact]
        public void ScanNode()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");
            Assert.True(communicator.ScanNodeExistance("ns=2;s=1:FC1001?SetPoint/ValuePrecision"));
            Assert.False(communicator.ScanNodeExistance("ns=2;s=1:FC1001?SetPoint/ValuePr"));
        }

        [Fact]
        public void ScanNode2()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");
            Assert.True(communicator.ScanNodeExistance("ns=2;s=1:FC1001?SetPoint/ValuePrecision"));
            Assert.False(communicator.ScanNodeExistance("ns=2;s=1:FC1001?SetPoint/ValuePr"));
        }
    }
}