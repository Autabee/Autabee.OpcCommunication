using Autabee.Communication.ManagedOpcClient;
using Autabee.Utility.Messaging;
using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.OpcScout.RazorControl
{
    public record ClientCache
    {
        public ClientCache(AutabeeManagedOpcClient client, List<ScannedNodeModel> scannedItems)
        {
            this.client = client;
            if (scannedItems != null)
            {
                scannedNodes = new List<ScannedNodeModel>();
            }
        }

        public readonly AutabeeManagedOpcClient client;
        public readonly List<ScannedNodeModel> scannedNodes = new List<ScannedNodeModel>();
    }
    
    public class NodeBrowserModel
    {
        public NodeBrowserModel()
        {
            Clients = new List<ClientCache>();
        }

        public event EventHandler<ScannedNodeModel> OnSelectedChanged;
        public event EventHandler<ScannedNodeModel> OnAddSubscriptionRequest;
        public event EventHandler<ScannedNodeModel> OnOpenCall;
        public event EventHandler<ScannedNodeModel> OnOpenWrite;
        public event EventHandler<Message> OnNodeRead;
        public event EventHandler OnStateHasChanged;
        public event EventHandler<AutabeeManagedOpcClient> OnDisconnect;
        public event EventHandler<AutabeeManagedOpcClient> OnRemoveConnection;
        internal event EventHandler<AutabeeManagedOpcClient> OnStateUpdateRoot;

        public bool DirectCall { get; set; } = false;

        public List<ClientCache> Clients { get; set; } 
        
        public  ScannedNodeModel Selected { get; set; }
        public void UpdateSelectedNode(ScannedNodeModel selected)
        {
            Selected?.DeSelect();
            Selected = selected;
            OnSelectedChanged?.Invoke(this, selected);
        }
        public void StateHasChanged()
        {
            OnStateHasChanged?.Invoke(this, null);
        }
        public void ClearClientCash(AutabeeManagedOpcClient client)
        {
            OnStateUpdateRoot.Invoke(this,client);
        }
        public void SubscriptionRequest(ScannedNodeModel selected)
        {
            OnAddSubscriptionRequest?.Invoke(this, selected);
        }
        public void ReadNodeValue(Message selected)
        {
            OnNodeRead?.Invoke(this, selected);
        }
        public void OpenCall(ScannedNodeModel selected)
        {
            OnOpenCall?.Invoke(this, selected);
        }
        public void Disconnect(AutabeeManagedOpcClient client)
        {
            OnDisconnect?.Invoke(this, client);
        }
        public void RemoveConnection(AutabeeManagedOpcClient client)
        {
            OnRemoveConnection?.Invoke(this, client);
        }
        public void WriteNodeValue(ScannedNodeModel scannedNode)
        {
            OnOpenWrite?.Invoke(this, scannedNode);
        }
    }
}
