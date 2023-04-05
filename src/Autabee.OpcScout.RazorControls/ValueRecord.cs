using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Autabee.OpcScout.RazorControl
{
    public class ValueRecord
    {
        public Dictionary<string, object> values = new Dictionary<string, object> ();
        public bool isArray { get; private set; } = false; 
        public ValueRecord(object value)
        {
            UpdateValue(value);
        }
        public void UpdateValue(object value)
        {

            if (value is Dictionary<string, object> dict)
            {
                values = dict;
            }
            else if (value.GetType().IsArray)
            {
                isArray = true;
                var avalue = ((Array)value);
                var tmp = new Dictionary<string, object>();
                for (int i = 0; i < avalue.Length; i++)
                {
                    if (avalue.GetValue(i) is IEncodeable encodeable)
                    {
                        var type = new NodeTypeData(avalue.GetValue(i).GetType());
                        tmp.Add($"[{i}]", type.Decode(encodeable));
                    }
                    else
                    {
                        tmp.Add($"[{i}]", avalue.GetValue(i));
                    }
                }
                values = tmp;
            }
            else if (value is IEncodeable encodeable)
            {
                var type = new NodeTypeData(value.GetType());
                UpdateValue(type.Decode(encodeable));
            }
            else
            {
                values = new Dictionary<string, object> { { "Value", value } };
            }
        }
    }
    public class ReadRecordSettings
    {
        public string NodeId { get; set; }
        public ValueRecord Record { get; set; }
        public ClientCache Client { get; set; }
    }
}
