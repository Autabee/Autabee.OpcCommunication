using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.Utilities;
using AutabeeTestFixtures;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Serilog;
using Serilog.Sinks.XUnit3;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcMethodSampleTests : IClassFixture<OpcUaMethodSampleFixture>
    {
        private readonly AutabeeManagedOpcClient communicator;
        private readonly bool skipServerNotFound;
        private readonly ILogger logger;

        public OpcMethodSampleTests(OpcUaMethodSampleFixture testPlcTestsFixture, ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
            logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}")
                .WriteTo.XUnit3TestOutput()
                .CreateLogger()
                .ForContext<OpcMethodSampleTests>();
        }

        [Fact]
        public void ConnectWithTestServer()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            logger.Information("Connected with Sample Method Server");
            var root = communicator.BrowseRoot();
            BrowseDescriptionCollection browseDescriptions = new BrowseDescriptionCollection();
            foreach (var item in root)
            {
                logger.Information(item.NodeId.ToString());
                logger.Information(item.BrowseName.Name);
                browseDescriptions.Add(Browse.GetChildrenBrowseDescription(ExpandedNodeId.ToNodeId(item.NodeId, null)));
            }

            var roota = communicator.BrowseNodes(browseDescriptions);
            browseDescriptions.Clear();

            Process(browseDescriptions, roota);
            roota = communicator.BrowseNodes(browseDescriptions);
            browseDescriptions.Clear();

            Process(browseDescriptions, roota);
        }

        private void Process(BrowseDescriptionCollection browseDescriptions, BrowseResultCollection root)
        {
            foreach (var item in root)
            {
                foreach (var child in item.References)
                {
                    logger.Information(child.NodeId.ToString());
                    logger.Information(child.BrowseName.Name);
                    browseDescriptions.Add(Browse.GetChildrenBrowseDescription(ExpandedNodeId.ToNodeId(child.NodeId, null)));
                }
            }
        }

        [Fact]
        public void GetMethodArgumentStructure()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var arguments = communicator.GetMethodArguments("ns=2;i=3");
            for (int i = 0; i < arguments.InputArguments.Count; i++)
            {
                logger.Information($"input: {arguments.InputArguments[i].Name}  type: {arguments.InputArgumentTypes[i].FullName}");
            }
            for (int i = 0; i < arguments.OutputArguments.Count; i++)
            {
                logger.Information($"output: {arguments.OutputArguments[i].Name}  type: {arguments.OutputArgumentTypes[i].FullName}");
            }
        }

        [Fact]
        public void GetNode()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");

            var node1 = communicator.ReadNode("ns=2;i=1");

            var node2 = communicator.TranslateBrowsePathsToNodeId(ObjectIds.ObjectsFolder, "2:My Process/2:Start");
            Assert.Equal("ns=2;i=3", node2?.ToString());
        }

        [Fact]
        public void CallOpcFunction()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");
            var arguments = communicator.CallMethod("ns=2;i=1", "ns=2;i=3", new object[2]
                    {
                        (UInt32) 50,
                        (UInt32) 100,
                    });
            Assert.Equal((uint)50, arguments[0]);
            Assert.Equal((uint)100, arguments[1]);
        }

        [Fact]
        public void CallOpcFunctions()
        {
            Assert.SkipWhen(skipServerNotFound, "Server not Found");
            var nodes = communicator.TranslateBrowsePathsToNodeIds(ObjectIds.ObjectsFolder, new string[] { "2:My Process", "2:My Process/2:Start" }
                );


            List<(NodeEntry, MethodNodeEntry, object[])> values = new List<(NodeEntry, MethodNodeEntry, object[])>();
            var argument = communicator.GetMethodArguments(nodes[1]);

            

            values.Add(new ValueTuple<NodeEntry, MethodNodeEntry, object[]>(
                 new NodeEntry(nodes[0]),
                 new MethodNodeEntry(nodes[1], argument),
                 new object[2]
                 {
                    (UInt32) 50,
                    (UInt32) 100,
                 })
            );

            values.Add(new ValueTuple<NodeEntry, MethodNodeEntry, object[]>(
                 new NodeEntry(nodes[0]),
                 new MethodNodeEntry(nodes[1], argument),
                 new object[1]
                 {
                    (UInt32) 2
                 })
            );

            values.Add(new ValueTuple<NodeEntry, MethodNodeEntry, object[]>(
                 new NodeEntry(nodes[0]),
                 new MethodNodeEntry(nodes[1], argument),
                 new object[2]
                 {
                    (int) 2,
                    (UInt32) 100
                 })
            );
            values.Add(new ValueTuple<NodeEntry, MethodNodeEntry, object[]>(
                 new NodeEntry(nodes[0]),
                 new MethodNodeEntry(nodes[1], argument),
                 new object[3]
                 {
                    (int) 2,
                    (UInt32) 100,
                    (UInt32) 10
                 })
            );


            var results = communicator.CallMethods(values);

            Assert.Equal((uint)50, results[0].OutputArguments[0].Value);
            Assert.Equal((uint)100,results[0].OutputArguments[1].Value);
            Assert.Equal(StatusCodes.BadArgumentsMissing, results[1].StatusCode);
            Assert.Equal(StatusCodes.BadInvalidArgument, results[2].StatusCode);
            Assert.Equal(StatusCodes.BadTooManyArguments, results[3].StatusCode);
        }
    }
}