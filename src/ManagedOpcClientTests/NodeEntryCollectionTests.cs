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
            ValueNodeEntryCollection collection = new ValueNodeEntryCollection();
            collection.Add(node);
        }

        [Fact()]
        public void CreateMultipleRecods()
        {
            ValueNodeEntryCollection collection = new ValueNodeEntryCollection();
            collection.Add(new ValueNodeEntry<float>("i=1"));
            collection.Add(new ValueNodeEntry<Guid>("i=2"));
            
            Guid g = Guid.NewGuid();
            var records = collection.CreateRecords(

                new object[]
                {
                    0.0f,
                    g
                });

            Assert.True(records.Count == 2);
            Assert.True((float)records[0].Value == 0.0 && records[0].ValueType == typeof(float));
            Assert.True((Guid)records[1].Value == g && records[1].ValueType == typeof(Guid));
        }

        [Fact()]
        public void AddFailTest()
        {
            ValueNodeEntryCollection collection = new ValueNodeEntryCollection();
            Assert.Throws<Exception>(delegate { collection.Add(new ValueNodeEntry<TestUser>("i=1")); });
        }
    }
}