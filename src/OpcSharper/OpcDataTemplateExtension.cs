using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace Autabee.OpcToClass
{
    public static class OpcDataTemplateExtension
    {

        public static void GenerateTypeFile(this IOpcSharperTemplate data, GeneratorDataSet settings)
        {
            Directory.CreateDirectory(settings.baseLocation);

            var (name, scriptContent) = (data.Name, data.GetScriptAsFile(settings));

            var namespaceName = data.NameSpace.Split('.');
            var className = data.ClassName;
            var fileName = className + ".cs";

            string filePath;
            if (namespaceName.Length == 0)
            {
                filePath = Path.Combine(settings.baseLocation, fileName);
            }
            else
            {
                var dir = Path.Combine(settings.baseLocation,string.Join($"\\", namespaceName));
                Directory.CreateDirectory(dir);
                filePath = Path.Combine(dir, fileName);
            }

            OpcToCSharpGenerator.CreateFile(filePath, scriptContent);
        }

        public static Field[] GetFields(this OpcStructTemplate template)
        {
            Field[] fields = template.Fields;
            if (template.BaseType != null)
            {
                fields = fields.Concat(template.BaseType.GetFields()).ToArray();
            }
            return fields;
        }

        private static Field[] GetClassOnlyFields(this OpcStructTemplate template)
        {
            var fields = template.Fields;
            if (template.BaseType != null)
            {
                // remove fields that are already in the base type
                foreach (var field in template.BaseType.GetFields())
                {
                    fields = fields.Where(o => o.Name != field.Name).ToArray();
                }
            }

            return fields;
        }

        public static OpcEnumTemplate ToEnumTemplate(this XmlNode node, string nsPrefix)
        {
            OpcEnumTemplate template = new OpcEnumTemplate();
            template.Name = OpcToCSharpGenerator.GetCorrectedName(node);
            template.nsPrefix = nsPrefix;
            //template.TypeName = OpcToCSharpGenerator.GetCorrectedTypeName(node, nsPrefix);
            template.NamespaceURI = node.NamespaceURI;

            foreach (XmlNode field in node.ChildNodes)
            {
                if (field.Name == "opc:EnumeratedValue")
                {
                    template.Fields = template.Fields.Append(new Field()
                    {
                        Name = OpcToCSharpGenerator.GetCorrectedName(field),
                        Type = string.Empty,
                        TypeName = string.Empty,
                        Value = field.Attributes["Value"].Value
                    }).ToArray();
                }
            }

            return template;
        }

        public static OpcStructTemplate ToStructTemplate(this XmlNode node, string nsPrefix)
        {
            OpcStructTemplate template = new OpcStructTemplate();

            template.nodes = new XmlNode[] { node };
            template.nsPrefix = nsPrefix;
            template.Name = OpcToCSharpGenerator.GetCorrectedName(node);
            template.TypeName = OpcToCSharpGenerator.GetCorrectedTypeName(node.Attributes["Name"].Value);
            template.NamespaceURI = node.NamespaceURI;

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode field = node.ChildNodes[i];
                if (field.Name == "opc:Field")
                {
                    var fieldName = OpcToCSharpGenerator.GetCorrectedName(field);
                    var fieldType = OpcToCSharpGenerator.GetCorrectedTypeName(field);
                    var classfieldName = fieldName;
                    // need to make sure that the class name is not the same as the field name, as some systems use the same name for both.
                    if (classfieldName == template.ClassName)
                    {
                        classfieldName += "Field";
                    }

                    Field newField = new Field()
                    {
                        Name = fieldName,
                        Type = fieldType,
                        TypeName = field.Attributes["TypeName"]?.Value,
                        Value = field.Attributes["Value"]?.Value,
                        ClassFieldName = classfieldName
                    };

                    template.Fields = template.Fields.Append(newField).ToArray();
                }
            }



            return template;
        }

        private static string GetStringOfIdentifier(object id)
        {
            if (id is string sid)
            {
                return $"\"{sid.Replace("\"", "\\\"")}\"" ;
            }
            else
            {
                return id.ToString();
            }
        }


        public static string GenerateClassScript(this OpcStructTemplate template, GeneratorDataSet dataSet)
        {
            var nsPrefix = dataSet.nameSpacePrefix;
            string[] enums = dataSet.enums.Keys.ToArray();
            if (enums == null) enums = new string[0];

            string ns = string.IsNullOrWhiteSpace(template.NameSpace) ? dataSet.baseNamespace : $"{dataSet.baseNamespace}.{template.NameSpace}";


            string classData = $"\nnamespace {ns}\n{{";
            if (template.BaseType == null)
                classData += $"\n\tpublic class {template.ClassName.Replace("\"", "")} : EncodeableObject\n\t{{";
            else
                classData += $"\n\tpublic class {template.ClassName.Replace("\"", "")} : {template.BaseType.ClassName.Replace("\"", "")}\n\t{{";

            Field[] fields = template.GetClassOnlyFields();

            foreach (var field in fields)
            { 
                var fieldtypename = OpcToCSharpGenerator.GetNamespacedType(field.Type, nsPrefix);
                classData += $"\n\t\tpublic {fieldtypename} {field.ClassFieldName} {{ get; set; }}";
            }

            // To Do This does not work always as systems somtimes use struct name with or without qoutations.
            // example: `"value"` vs `value`.
            // might need to read the node itself to get the correct name.


            classData += template.TypeId != null
                ? $"\n\t\tprivate static ExpandedNodeId typeId = new ExpandedNodeId({GetStringOfIdentifier(template.TypeId.Identifier)},\"{template.TypeId.NamespaceUri}\");"
                + $"\n\t\tpublic override ExpandedNodeId TypeId => typeId;"
                : $"\n\t\tpublic override ExpandedNodeId TypeId => NodeId.Null;";

            classData += template.BinaryEncoding != null
                ? $"\n\t\tprivate static ExpandedNodeId binaryEncodingId = new ExpandedNodeId({GetStringOfIdentifier(template.BinaryEncoding.Identifier)},\"{template.BinaryEncoding.NamespaceUri}\");"
                + $"\n\t\tpublic override ExpandedNodeId BinaryEncodingId => binaryEncodingId;"
                : $"\n\t\tpublic override ExpandedNodeId BinaryEncodingId => NodeId.Null;";


            classData += template.XmlEncoding != null
                ? $"\n\t\tprivate static ExpandedNodeId xmlNodeId = new ExpandedNodeId({GetStringOfIdentifier(template.XmlEncoding.Identifier)},\"{template.XmlEncoding.NamespaceUri}\");"
                + $"\n\t\tpublic override ExpandedNodeId  XmlEncodingId => xmlNodeId;"
                : $"\n\t\tpublic override ExpandedNodeId XmlEncodingId => NodeId.Null;";

            //Encoder function
            string encoder = "encoder";
            classData += GetEncoderFunction(template.BaseType, enums, fields,nsPrefix, encoder);


            //decoder function
            string decoder = "decoder";
            classData += GetDecoderFunction(template.BaseType, enums, fields,nsPrefix, decoder);

            classData += "\n\t}\n}";

            return classData;
        }



        private static string GetDecoderFunction(this OpcStructTemplate template, string[] enums, string decoder)
            => GetDecoderFunction(template.BaseType, enums, template.GetClassOnlyFields(), template.nsPrefix, decoder);


        private static string GetDecoderFunction(OpcStructTemplate baseTemplate, string[] enums, Field[] fields, string nsPrefix, string decoder)
        {
            string classData = string.Empty;
            classData += $"\n\n\tpublic override void Decode(IDecoder {decoder})\n\t{{";

            if (baseTemplate != null)
            {
                classData += $"\n\t\tbase.Decode({decoder});";
            }

            foreach (var field in fields)
            {
                classData += $"\n\t\tthis.{field.ClassFieldName} = {OpcToCSharpGenerator.GetDecoder(field, nsPrefix, enums, decoder)};";
            }

            classData += "\n\t}";
            return classData;
        }

        private static string GetEncoderFunction(this OpcStructTemplate template, string[] enums, string encoder)
            => GetEncoderFunction(template.BaseType, enums, template.GetClassOnlyFields(), template.nsPrefix, encoder);

        private static string GetEncoderFunction(OpcStructTemplate baseTemplate, string[] enums, Field[] fields, string nsPrefix, string encoder)
        {
            string classData = string.Empty;
            classData += $"\n\n\tpublic override void Encode(IEncoder {encoder})\n\t{{";

            if (baseTemplate != null)
            {
                classData += $"\n\t\tbase.Encode({encoder});";
            }

            foreach (var field in fields)
            {
                classData += $"\n\t\t{OpcToCSharpGenerator.GetEncoder(field, enums, nsPrefix, encoder )};";
            }
            classData += "\n\t}";
            return classData;
        }
    }
}
