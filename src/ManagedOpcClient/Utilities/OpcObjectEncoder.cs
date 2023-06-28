using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    public static class OpcObjectEncoder
    {
        public static byte[] Binary(IServiceMessageContext messageContext, Dictionary<string, object> dict)
        {
            var flattened = dict.Flatten();
            var count = -1;
            var lenght = 0;
            List<byte[]> bytes = new List<byte[]>();

            void AddByte(byte value)
            {
                bytes.Add(new byte[1] { value });
                lenght += 1;
            }
            void AddByteArray(byte[]value )
            {
                bytes.Add( value);
                lenght += value.Length;
            }

            foreach(var value in flattened.Select(o => o.Value))
            {
                count++;
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Boolean:
                        AddByte(Convert.ToByte(value));
                        break; 
                    case TypeCode.Byte:
                        AddByte((byte)value);
                        break;

                    case TypeCode.Int16:
                        AddByteArray(BitConverter.GetBytes((short)value));
                        break;
                    case TypeCode.Int32:
                        AddByteArray(BitConverter.GetBytes((int)value));
                        break;
                    case TypeCode.Int64:
                        AddByteArray(BitConverter.GetBytes((long)value));
                        break;

                    case TypeCode.UInt16:
                        AddByteArray(BitConverter.GetBytes((short)value));
                        break;
                    case TypeCode.UInt32:
                        AddByteArray(BitConverter.GetBytes((int)value));
                        break;
                    case TypeCode.UInt64:
                        AddByteArray(BitConverter.GetBytes((ulong)value));
                        break;

                    case TypeCode.Single:
                        AddByteArray(BitConverter.GetBytes((float)value));
                        break;
                    case TypeCode.Double:
                        AddByteArray(BitConverter.GetBytes((double)value));
                        break;

                    case TypeCode.Char:
                        AddByte(Convert.ToByte((char)value));
                        break;
            

                    case TypeCode.String:
                        AddByteArray(BitConverter.GetBytes(((string)value).Length));
                        foreach (var item in (string)value)
                        {
                            AddByte(Convert.ToByte(item) );
                        }
                        break;



                    default:
                        if(value is IEncodeable obj)
                        {
                            var encoder = new BinaryEncoder(messageContext);
                            obj.Encode(encoder);
                            AddByteArray(encoder.CloseAndReturnBuffer());
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                }
            }
            var encoded = new byte[lenght];
            int index = 0;
            for (int i = 0; i < bytes.Count; i++)
            {
                Array.Copy(bytes[i], 0, encoded, index, bytes[i].Length);
                index += bytes[i].Length;
            }

            return encoded;
        }
    }
    public static class DictionaryFlatten
    {
        public static List<KeyValuePair<string, object>> Flatten(this Dictionary<string, object> dict, string baseValue = null)
        {
            if (baseValue ==  null) { baseValue = string.Empty; }
            List<KeyValuePair<string, object>> keyValuePairs = new List<KeyValuePair<string, object>>();
            foreach (var kvp  in dict) 
            {
                if (kvp.Value is Dictionary<string, object>newDict)
                    keyValuePairs.AddRange(newDict.Flatten(kvp.Key.ToString()));
                else
                {
                    keyValuePairs.Add(new KeyValuePair<string, object>(baseValue == string.Empty ?
                        kvp.Key.ToString() :
                        $"{baseValue}.{kvp.Key}", kvp.Value));
                }
            }
            return keyValuePairs;
        }
        }


    
}
