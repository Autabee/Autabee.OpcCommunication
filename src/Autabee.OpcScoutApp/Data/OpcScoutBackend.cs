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
        public readonly List<(AutabeeManagedOpcClient, List<ScannedNodeModel>)> clients = new List<(AutabeeManagedOpcClient, List<ScannedNodeModel>)>();
        public ScannedNodeModel observing = null;
        public readonly List<SubscriptionNodeModel> subscriptionNodes = new List<SubscriptionNodeModel>();

    }
}
