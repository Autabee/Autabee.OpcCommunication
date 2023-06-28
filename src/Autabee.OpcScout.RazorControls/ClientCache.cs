using Autabee.Communication.ManagedOpcClient;
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
}
