using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autabee.OpcScoutApp.Controls.OpcScoutControl.Browse
{
  public class SubscriptionNodeModel
  {
    public TimeSpan timeSpan = TimeSpan.FromMilliseconds(500);
    private Subscription subscription;
    public readonly MonitoredItem monitoredItem;
    public event EventHandler<object> ValueUpdated;
    public SubscriptionNodeModel(ScannedNodeModel nodeItem)
    {
      var client = nodeItem.Client;
      subscription = client.GetSubscription(timeSpan);
      monitoredItem = client.CreateMonitoredItem((NodeId)nodeItem.Reference.NodeId, handler: MonitoredNodeValueEventHandler);
      client.AddMonitoredItem(subscription, monitoredItem);
    }
    public async void MonitoredNodeValueEventHandler(object sender, object record)
    {
      var dispatcher = Dispatcher.GetForCurrentThread();

      if (dispatcher!= null && dispatcher.IsDispatchRequired)
        await dispatcher.DispatchAsync(() => ValueUpdated?.Invoke(sender, record));
      else
        ValueUpdated?.Invoke(sender, record);
    }
  }
}
