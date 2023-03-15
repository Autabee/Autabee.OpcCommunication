using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScout.RazorControl.Subscription;
using Serilog.Core;
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
        readonly Logger logger;
        public SubscriptionViewModel(List<SubscriptionNodeModel> subscriptionNodeModels, Logger logger)
        {
            this.logger = logger;
            this.subscriptionNodeModels = subscriptionNodeModels;
            foreach (var model in subscriptionNodeModels)
            {
                model.RemoveMonitoredItem += RemoveSubscription;
            }
        }

        public event EventHandler<List<SubscriptionNodeModel>> OnListChanged;
        public void AddSubscription(object sender, ScannedNodeModel selected)
        {
            try
            {
                SubscriptionNodeModel model = new SubscriptionNodeModel(selected);
                subscriptionNodeModels.Add(model);
                model.RemoveMonitoredItem += RemoveSubscription;
                OnListChanged?.Invoke(sender, subscriptionNodeModels);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to subcribe");
            }
        }

        private void RemoveSubscription(object sender, EventArgs obj)
        {
            subscriptionNodeModels.Remove((SubscriptionNodeModel)sender);
            OnListChanged?.Invoke(sender, subscriptionNodeModels);
        }
        public IEnumerable<string> RemoveSubscriptions(AutabeeManagedOpcClient client)
        {
            var items = subscriptionNodeModels.Where(o => o.nodeItem.Client == client).ToList();
            foreach (var item in items)
            {
                subscriptionNodeModels.Remove(item);
            }
            OnListChanged?.Invoke(this, subscriptionNodeModels);
            return items.Select(o => o.nodeItem.Node.NodeId.ToString());
        }
    }
}
