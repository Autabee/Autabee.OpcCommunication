using Autabee.Communication.ManagedOpcClient;
using System;
using System.Linq;

namespace Autabee.OpcScoutApp.Controller
{
    public class OpcScoutBackend
    {
        public OpcScoutBackend()
        {
        }
        public readonly List<OpcUaClientHelperApi> clients = new List<OpcUaClientHelperApi>();

    }
}
