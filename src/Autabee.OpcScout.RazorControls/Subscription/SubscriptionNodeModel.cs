using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autabee.OpcScout.RazorControl.Subscription
{
    public class SubscriptionNodeModel
    {
        public TimeSpan timeSpan = TimeSpan.FromMilliseconds(500);
        private Opc.Ua.Client.Subscription subscription;
        public ScannedNodeModel nodeItem;
        public NodeId NodeTypeId => (((VariableNode)nodeItem.Node).DataType);
        public readonly MonitoredItem monitoredItem;
        public event EventHandler<object> ValueUpdated;
        public event EventHandler RemoveMonitoredItem;
        public event EventHandler UpdateView;
        public event EventHandler<Opc.Ua.Client.Subscription> RemoveSubscription;

        public Dictionary<string, object> DictMonitoredValue => (Dictionary<string, object>)MonitoredValue;
        public bool complex { get; set; }
        public bool show { get; set; }
        public bool editing { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime updateTime { get; set; }

        public string error { get; set; } = string.Empty;
        public object lockItem { get; set; } = new object();
        public object MonitoredValue { get; set; }
        public NodeTypeData NodeType { get; set; }
        public string MonitoredValueString { get; set; } = string.Empty;
        public object BackendMonitoredValue { get; set; }
        public string BackendMonitoredValueString { get; set; }
        public SubscriptionNodeModel(ScannedNodeModel nodeItem)
        {
            this.nodeItem = nodeItem;
            var client = nodeItem.Client;
            NodeType = client.GetNodeTypeEncoding(nodeItem.Node);
            subscription = client.GetSubscription(timeSpan);
            monitoredItem = client.CreateMonitoredItem((NodeId)nodeItem.Reference.NodeId, handler: MonitoredNodeValueEventHandler);
            client.AddMonitoredItem(subscription, monitoredItem);
        }
        public async void MonitoredNodeValueEventHandler(object sender, object record)
        {
            UpdateMonitoredValue(record);
        }

        public void UpdateValue(string value)
        {
            try
            {
                var convertedValue = ConvertOpc.StringToObject(NodeTypeId, value);
                var node = nodeItem;
                var converter = TypeDescriptor.GetConverter(MonitoredValue.GetType());
                node.Client.WriteValue((NodeId)node.Reference.NodeId, convertedValue);
                error = string.Empty;
                UpdateView?.Invoke(this, null);
            }
            catch (Exception e)
            {
                error = e.Message;
                UpdateView?.Invoke(this, null);
            }
        }

        public void Remove()
        {
            try
            {
                nodeItem.Client.RemoveMonitoredItem(subscription, monitoredItem);
                RemoveMonitoredItem?.Invoke(this, null);
            }
            catch (ServiceResultException e)
            {
                //system disconnected
                if (e.StatusCode == StatusCodes.BadSubscriptionIdInvalid)
                {
                    RemoveMonitoredItem?.Invoke(this, null);
                }
            }

        }

        public void UpdateMonitoredValue(object value)
        {
            if (value is Dictionary<string, object> dict)
            {
                UpdateTime = DateTime.Now;
                complex = true;
                MonitoredValue = dict;
            }
            else
            {
                UpdateTime = DateTime.Now;
                lock (lockItem)
                {
                    if (!editing)
                    {
                        MonitoredValue = value;
                        MonitoredValueString = value == null ? "NULL" : value.ToString();
                    }
                    else
                    {
                        BackendMonitoredValue = value;
                        BackendMonitoredValueString = value == null ? "NULL" : value.ToString();
                    }
                }
                complex = false;
            }

            UpdateView?.Invoke(this, null);

        }
        public async void ShowSubscription()
        {
            show = !show;
        }

        public async void FocusIn(EventArgs args)
        {
            lock (lockItem)
            {
                editing = true;
                BackendMonitoredValue = MonitoredValue;
                BackendMonitoredValueString = MonitoredValueString;
            }
        }

        public async void FocusOut()
        {
            lock (lockItem)
            {
                editing = false;
                MonitoredValue = BackendMonitoredValue;
                MonitoredValueString = BackendMonitoredValueString;
                UpdateView?.Invoke(this, null);
            }
        }
    }

}
