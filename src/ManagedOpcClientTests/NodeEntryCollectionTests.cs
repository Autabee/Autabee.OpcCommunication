using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using AutabeeTestFixtures;
using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Autabee.Communication.OpcCommunicator.Tests
{
    public class NodeEntryGenerator : IEnumerable<object[]>
    {
        //All default opc data types
        private readonly List<object[]> _data = new List<object[]>
        {
            new object[]{ new ValueNodeEntry<float>("i=1") },
            new object[]{ new ValueNodeEntry<double>("i=1") },
            new object[]{ new ValueNodeEntry<bool>("i=1") },
            new object[]{ new ValueNodeEntry<byte>("i=1") },
            new object[]{ new ValueNodeEntry<sbyte>("i=1") },
            new object[]{ new ValueNodeEntry<short>("i=1") },
            new object[]{ new ValueNodeEntry<ushort>("i=1") },
            new object[]{ new ValueNodeEntry<int>("i=1") },
            new object[]{ new ValueNodeEntry<uint>("i=1") },
            new object[]{ new ValueNodeEntry<long>("i=1") },
            new object[]{ new ValueNodeEntry<ulong>("i=1") },
            new object[]{ new ValueNodeEntry<decimal>("i=1") },
            new object[]{ new ValueNodeEntry<char>("i=1") },
            new object[]{ new ValueNodeEntry<DateTime>("i=1") },
            new object[]{ new ValueNodeEntry<TimeSpan>("i=1") },
            new object[]{ new ValueNodeEntry<Guid>("i=1") },
            new object[]{ new ValueNodeEntry<string>("i=1") },
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
    }

    public class NodeEntryCollectionTests
    {
        [Theory()]
        [ClassData(typeof(NodeEntryGenerator))]
        public void AddTest(ValueNodeEntry node)
        {
            NodeEntryCollection collection = new NodeEntryCollection();
            collection.Add(node);
        }

        [Fact()]
        public void AddFailTest()
        {
            NodeEntryCollection collection = new NodeEntryCollection();
            Assert.Throws<Exception>(delegate { collection.Add(new ValueNodeEntry<TestUser>("i=1")); });
        }
    }
}