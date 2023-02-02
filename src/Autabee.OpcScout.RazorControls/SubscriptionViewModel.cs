using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScout.RazorControl.Subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autabee.OpcScout.RazorControl
{
    public class SubscriptionViewModel
    {
        public readonly List<SubscriptionNodeModel> subscriptionNodeModels = new List<SubscriptionNodeModel>();
        public SubscriptionViewModel(List<SubscriptionNodeModel> subscriptionNodeModels)
        {
            this.subscriptionNodeModels = subscriptionNodeModels;
        }

        public event EventHandler OnListChanged;
        public void AddSubscription(object sender, ScannedNodeModel selected)
        {
            SubscriptionNodeModel model = new SubscriptionNodeModel(selected);
            subscriptionNodeModels.Add(model);
            model.RemoveMonitoredItem += RemoveSubscription;
            OnListChanged?.Invoke(sender, null);
        }

        private void RemoveSubscription(object sender, EventArgs obj)
        {
            subscriptionNodeModels.Remove((SubscriptionNodeModel)sender);
            OnListChanged?.Invoke(sender, null);
        }
        public IEnumerable<string> RemoveSubscriptions(OpcUaClientHelperApi client)
        {
            var items = subscriptionNodeModels.Where(o => o.nodeItem.Client == client).ToList();
            foreach (var item in items)
            {
                subscriptionNodeModels.Remove(item);
            }
            OnListChanged?.Invoke(this, null);
            return items.Select(o => o.nodeItem.Reference.NodeId.ToString());
        }
    }
}
