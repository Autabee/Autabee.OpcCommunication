﻿using Autabee.Communication.ManagedOpcClient.Utilities;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public Type systemType = null;
        public PropertyInfo propertyInfo = null;
        private string typeName = "";

        public NodeTypeData()
        {
        }

        public NodeTypeData(Type type)
        {
            Name = type.Name;
            TypeName = type.FullName;
            systemType = type;
            ChildData = ChilderenFromProperties(type.GetProperties(), new Type[] { type });

        }


        public NodeTypeData(PropertyInfo property, Type[] types)
        {
            propertyInfo = property;
            Name = property.Name;
            TypeName = property.PropertyType.Name;
            systemType = property.PropertyType;


            var tmpTypes = new Type[types.Length + 1];
            types.CopyTo(tmpTypes, 0);
            tmpTypes[types.Length] = property.PropertyType;
            ChildData = ChilderenFromProperties(property.PropertyType.GetProperties(), tmpTypes);
        }

        private List<NodeTypeData> ChilderenFromProperties(PropertyInfo[] propertyInfos, Type[] types)
        {
            var result = new List<NodeTypeData>();
            foreach (var item in propertyInfos)
            {
                if (item.Name == "TypeId") continue;
                if (item.Name == "BinaryEncodingId") continue;
                if (item.Name == "XmlEncodingId") continue;
                if (item.Name == "JsonEncodingId") continue;
                if (types.Contains(item.PropertyType)) continue;

                result.Add(new NodeTypeData(item, types));
            }
            return result;
        }

        public NodeTypeData(NodeTypeData nodeTypeData)
        {
            Name = nodeTypeData.Name;
            TypeName = nodeTypeData.TypeName;
            ChildData = nodeTypeData.ChildData;
        }

        public string Name { get; set; } = string.Empty;
        public string ArrayLengthField { get; set; } = string.Empty;
        public bool ArraySizeField { get; set; } = false;
        public string TypeName
        {
            get => typeName; set
            {
                typeName = value;
            }
        }
        public bool IsArray { get; set; }
        public bool Primitive { get => ChildData.Count() == 0; }
        //public int index = 0;
        public List<NodeTypeData> ChildData { get; set; } = new List<NodeTypeData>();

        public NodeTypeData Copy()
        {
            return new NodeTypeData(this);
        }

        public Dictionary<string, object> Decode(IDecoder decoder, DecodeFactory factory)
        {
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < ChildData.Count; i++)
            {
                var child = ChildData[i];
                if (!string.IsNullOrWhiteSpace(child.ArrayLengthField))
                {
                    var size_field = (int)dict[child.ArrayLengthField];
                    for (int j = 0; j < size_field; j++)
                    {
                        dict.Add($"{child.Name}[{j}]", AddChildItem(decoder, child));
                    }
                }

                else
                {
                    dict.Add(child.Name, AddChildItem(decoder, child));
                }
            }

            return dict;

            object AddChildItem(IDecoder decoder, NodeTypeData child)
            {
                if (child.Primitive)
                {
                    if (factory.TryGetValue(child.TypeName, out var decodeFunc))
                    {
                        return decodeFunc(decoder, child.Name);
                    }
                    else
                    {
                        return decoder.Read(child.TypeName, child.Name);
                    }
                }
                else
                {
                    return child.Decode(decoder, factory);
                }
            }
        }


        public Dictionary<string, object> Decode(IEncodeable encodedItem)
        {
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < ChildData.Count; i++)
            {
                var child = ChildData[i];
                dict.Add(child.Name, ChildData[i].propertyInfo.GetValue(encodedItem));
            }

            return dict;
        }
    }

    public static class NodeTypeDataExtension
    {
        public static List<NodeTypeData> ToFlattened(this NodeTypeData nodeDataRecord)
        {
            var list = new List<NodeTypeData>();
            if (nodeDataRecord.Primitive) list.Add(nodeDataRecord);
            if (nodeDataRecord.ChildData != null)
            {
                foreach (var item in nodeDataRecord.ChildData)
                {
                    list.AddRange(ToFlattened(item));
                }
            }
            return list;
        }
    }
}