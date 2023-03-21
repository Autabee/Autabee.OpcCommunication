using Autabee.Communication.ManagedOpcClient;
using Autabee.Utility.Messaging;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Linq;
using System.Text.Json;

namespace Autabee.OpcScout.RazorControl
{
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
        public event EventHandler<NodeId> OnAddSharperNode;
        public event EventHandler<Message> OnNodeRead;
        public event EventHandler OnStateHasChanged;
        public event EventHandler<AutabeeManagedOpcClient> OnDisconnect;
        public event EventHandler<AutabeeManagedOpcClient> OnRemoveConnection;
        internal event EventHandler<AutabeeManagedOpcClient> OnStateUpdateRoot;

        public bool DirectCall { get; set; } = false;

        public bool SharperActive { get; set; } = false;

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
        
        public void ReadValue(ScannedNodeModel model)
        {
            try
            {
                var data = model.Client.ReadValue(model.Node.NodeId);
                Message message = GetValueMessage(data);
                ReadNodeValue(message);
            }
            catch (Exception e)
            {
                ReadNodeValue(new Message(level: MessageLevel.Error, text: " {0} : {1} \n{2}", model.Node, e.Message, e));
            }
        }
        
        private Message GetValueMessage(object value)
        {
            if (value == null) return new Message(level: MessageLevel.Info, text: "NULL");
            else if (value is Dictionary<string, object> dic) return new Message(level: MessageLevel.Info, text: "{0}", JsonSerializer.Serialize(dic));
            else if (value is object[] list) return new Message(level: MessageLevel.Info, text: "{0}", string.Join("\n", list));
            else return new Message(level: MessageLevel.Info, text: "{0}", value?.ToString());
        }


        public void AddSharperNode(NodeId nodeId)
        {
            OnAddSharperNode?.Invoke(this, nodeId);
        }
    }
}
