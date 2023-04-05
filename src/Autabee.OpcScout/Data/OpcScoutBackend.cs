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
        public ReadRecordSettings ReadRecord = null;
        public ScannedNodeModel ModelReadRecord = null;
        public ScannedNodeModel observing = null;
        public readonly List<SubscriptionNodeModel> subscriptionNodes = new List<SubscriptionNodeModel>();
        public SharperViewModel sharperViewModel = null;
    }
}
