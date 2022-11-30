using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Utility.Logger;
using Autabee.Utility.Logger.xUnit;
using AutabeeTestFixtures;
using Opc.Ua;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Autabee.Communication.OpcCommunicatorTests.OpcSample
{
    public class OpcMethodSampleTests : IClassFixture<OpcMethodSampleFixture>
    {
        private readonly OpcUaClientHelperApi communicator;
        private readonly bool skipServerNotFound;
        private readonly IAutabeeLogger logger;

        public OpcMethodSampleTests(OpcMethodSampleFixture testPlcTestsFixture, ITestOutputHelper outputHelper)
        {
            communicator = testPlcTestsFixture.Communicator;
            skipServerNotFound = testPlcTestsFixture.SkipServerNotFound;
            logger = new AutabeeXunitLogger(outputHelper);
        }

        [SkippableFact]
        public void ConnectWithTestServer()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            logger.Information("Connected with Sample Method Server");
            var root = communicator.BrowseRoot();
            BrowseDescriptionCollection browseDescriptions = new BrowseDescriptionCollection();
            foreach (var item in root)
            {
                logger.Information(item.NodeId.ToString());
                logger.Information(item.BrowseName.Name);
                browseDescriptions.Add(communicator.GetNodeHierarchalBrowseDescription(ExpandedNodeId.ToNodeId(item.NodeId, null)));
            }

            root = communicator.BrowseNode(browseDescriptions);
            browseDescriptions.Clear();

            foreach (var item in root)
            {
                logger.Information(item.NodeId.ToString());
                logger.Information(item.BrowseName.Name);
                browseDescriptions.Add(communicator.GetNodeHierarchalBrowseDescription(ExpandedNodeId.ToNodeId(item.NodeId, null)));
            }
            root = communicator.BrowseNode(browseDescriptions);
            browseDescriptions.Clear();

            foreach (var item in root)
            {
                logger.Information(item.NodeId.ToString());
                logger.Information(item.BrowseName.Name);
                browseDescriptions.Add(communicator.GetNodeHierarchalBrowseDescription(ExpandedNodeId.ToNodeId(item.NodeId, null)));
            }
        }

        [SkippableFact]
        public void GetMethodArgumentStructure()
        {
            Skip.If(skipServerNotFound, "Server not Found");

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

        [SkippableFact]
        public void GetNode()
        {
            Skip.If(skipServerNotFound, "Server not Found");

            var node1 = communicator.ReadNode("ns=2;i=1");

            var node2 = communicator.TranslateBrowsePathsToNodeId(ObjectIds.ObjectsFolder, "2:My Process/2:Start"
                //, new string[1]{ "http://opcfoundation.org/Quickstarts/Methods" }
                );
            Assert.Equal(node2?.ToString(), "ns=2;i=3");

            //communicator.WellKnownNameSpaces.Append("http://opcfoundation.org/Quickstarts/Methods");
            //node2 = communicator.TranslateBrowsePathsToNodeId(ObjectIds.ObjectsFolder, 
            //    "2:My Process/2:Start"
            //    );
            //Assert.Equal(node2?.ToString(), "ns=2;i=3");
        }

        [SkippableFact]
        public void CallOpcFunction()
        {
            Skip.If(skipServerNotFound, "Server not Found");
            var arguments = communicator.CallMethod("ns=2;i=1", "ns=2;i=3", new object[2]
                    {
                        (UInt32) 1,
                        (UInt32) 100,
                    });

        }

        [SkippableFact]
        public void CallOpcFunctions()
        {
            Skip.If(skipServerNotFound, "Server not Found");
            var nodes = communicator.TranslateBrowsePathsToNodeIds(ObjectIds.ObjectsFolder, new string[] { "2:My Process", "2:My Process/2:Start" }
                );


            List<(NodeEntry, MethodEntry, object[])> values = new List<(NodeEntry, MethodEntry, object[])>();
            nodes = communicator.RegisterNodeIds(nodes);
            var argument = communicator.GetMethodArguments(nodes[1]);

            

            values.Add(new ValueTuple<NodeEntry, MethodEntry, object[]>(
                 new NodeEntry(nodes[0]),
                 new MethodEntry(nodes[1], argument),
                 new object[2]
                 {
                    (UInt32) 50,
                    (UInt32) 100,
                 })
            );

            values.Add(new ValueTuple<NodeEntry, MethodEntry, object[]>(
                 new NodeEntry(nodes[0]),
                 new MethodEntry(nodes[1], argument),
                 new object[1]
                 {
                    (UInt32) 2
                 })
            );

            values.Add(new ValueTuple<NodeEntry, MethodEntry, object[]>(
                 new NodeEntry(nodes[0]),
                 new MethodEntry(nodes[1], argument),
                 new object[2]
                 {
                    (int) 2,
                    (UInt32) 100
                 })
            );
            values.Add(new ValueTuple<NodeEntry, MethodEntry, object[]>(
                 new NodeEntry(nodes[0]),
                 new MethodEntry(nodes[1], argument),
                 new object[3]
                 {
                    (int) 2,
                    (UInt32) 100,
                    (UInt32) 10
                 })
            );


            var arguments = communicator.CallMethods(values);

            Assert.Equal((uint)50, (arguments[0] as Object[])[0]);
            Assert.Equal((uint)100, (arguments[0] as Object[])[1]);
            Assert.Equal(StatusCodes.BadArgumentsMissing, ((StatusCode)arguments[1]).Code);
            Assert.Equal(StatusCodes.BadInvalidArgument, ((StatusCode)arguments[2]).Code);
            Assert.Equal(StatusCodes.BadTooManyArguments, ((StatusCode)arguments[3]).Code);
        }
    }
}