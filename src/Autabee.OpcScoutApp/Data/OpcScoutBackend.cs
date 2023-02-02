using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScout.RazorControl;
using Autabee.OpcScout.RazorControl.Subscription;
using System;
using System.Linq;

namespace Autabee.OpcScoutApp.Data
{
    public class OpcScoutPersistentData
    {
        public OpcScoutPersistentData()
        {
        }
        public readonly List<OpcUaClientHelperApi> clients = new List<OpcUaClientHelperApi>();
        public readonly ScannedNodeModel observing = null;
        public readonly List<SubscriptionNodeModel> subscriptionNodes = new List<SubscriptionNodeModel>();

    }
}
