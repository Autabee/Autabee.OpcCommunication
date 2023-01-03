using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScoutApp.Controls.OpcScoutControl.Browse;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
