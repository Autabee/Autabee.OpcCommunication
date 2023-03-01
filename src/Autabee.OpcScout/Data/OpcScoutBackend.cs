using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScout.RazorControl;
using Autabee.OpcScout.RazorControl.Subscription;
using System;
using System.Linq;

namespace Autabee.OpcScout.Data
{
    public class OpcScoutPersistentData
    {
        public OpcScoutPersistentData()
        {
        }
        public readonly List<ClientCache> clients = new List<ClientCache>();
        public ScannedNodeModel observing = null;
        public readonly List<SubscriptionNodeModel> subscriptionNodes = new List<SubscriptionNodeModel>();

    }
}
