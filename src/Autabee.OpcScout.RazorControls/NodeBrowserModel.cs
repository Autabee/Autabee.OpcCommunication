using Autabee.Communication.ManagedOpcClient;
using Autabee.Utility.Messaging;
using System;
using System.Linq;

namespace Autabee.OpcScout.RazorControl
{
    public class NodeBrowserModel
    {
        public event EventHandler<ScannedNodeModel> OnSelectedChanged;
        public event EventHandler<ScannedNodeModel> OnAddSubscriptionRequest;
        public event EventHandler<Message> OnNodeRead;
        public event EventHandler<AutabeeManagedOpcClient> OnClientDisconnect;

        public List<(AutabeeManagedOpcClient, List<ScannedNodeModel>)> Clients { get; set; } = new List<(AutabeeManagedOpcClient, List<ScannedNodeModel>)>();


        private ScannedNodeModel Selected { get; set; }
        public void UpdateSelected(ScannedNodeModel selected)
        {
            Selected?.DeSelect();
            Selected = selected;
            OnSelectedChanged?.Invoke(this, selected);
        }

        public void SubscriptionRequest(ScannedNodeModel selected)
        {
          OnAddSubscriptionRequest?.Invoke(this, selected);
        }

        public void ReadNode(Message selected)
        {
            OnNodeRead?.Invoke(this, selected);
        }

        public void Disconnect(AutabeeManagedOpcClient client)
        {
            OnClientDisconnect?.Invoke(this, client);
        }
    }
}
