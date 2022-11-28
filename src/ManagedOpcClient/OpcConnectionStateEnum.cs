using System;

namespace Autabee.Communication.ManagedOpcClient
{
    public enum OpcConnectionStatus
    {
        Unkown = 0,
        Disconnected = 1,
        Reconnecting = 2,
        Connected = 3,
        
    }
}
