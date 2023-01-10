using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScoutApp.Controls.OpcScoutControl.Browse;
using Autabee.Utility.Messaging;
using System;
using System.Linq;

namespace Autabee.OpcScoutApp.Controls.OpcScoutControl
{
    public class NodeBrowserModel
    {
        public event EventHandler<ScannedNodeModel> OnSelectedChanged;
        public event EventHandler<ScannedNodeModel> OnAddSubscriptionRequest;
        public event EventHandler<Message> OnNodeRead;

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
    }
}
