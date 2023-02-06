using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Autabee.Communication.ManagedOpcClient.ManagedNode
{
#if NET5_0_OR_GREATER
    public record NodeDataRecord<T> : NodeTypeData
#else
    public class NodeDataRecord<T> : NodeTypeData
#endif

    {
        public NodeDataRecord(T Value) : base()
        {
            this.Value = Value;
        }
        public NodeDataRecord(NodeTypeData nodeTypeData, T Value) : base(nodeTypeData)
        {
            this.Value = Value;
        }

        public NodeDataRecord(NodeDataRecord<T> nodeTypeData) : base(nodeTypeData)
        {
            Value = nodeTypeData.Value;
            //ValueData = nodeTypeData.ValueData;
        }

        public T Value { get; set; }
        //public List<NodeDataRecord<object>> ValueData { get; set; } = new List<NodeDataRecord<object>>();
    }
#if NET5_0_OR_GREATER
    public record NodeTypeData
#else
    public class NodeTypeData
#endif
    {
        private string typeName = "";

        public NodeTypeData()
        {
        }

        public NodeTypeData(NodeTypeData nodeTypeData)
        {
            Name = nodeTypeData.Name;
            TypeName = nodeTypeData.TypeName;
            ChildData = nodeTypeData.ChildData;
        }

        public string Name { get; set; } = "";
        public string TypeName
        {
            get => typeName; set
            {
                typeName = value;
                Primitive = value.Contains("opc:");
            }
        }
        public bool Primitive { get; private set; }
        //public int index = 0;
        public List<NodeTypeData> ChildData { get; set; } = new List<NodeTypeData>();

        public NodeTypeData Copy()
        {
            return new NodeTypeData(this);
        }

        public Dictionary<string, object> Decode(IDecoder decoder)
        {
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < ChildData.Count; i++)
            {
                var child = ChildData[i];
                if (ChildData[i].Primitive)
                    dict.Add(child.Name, new NodeDataRecord<object>(ChildData[i], decoder.Read(child.TypeName, child.Name)));
                else
                    dict.Add(child.Name,  child.Decode(decoder));
            }
            return dict;
        }
    }
}