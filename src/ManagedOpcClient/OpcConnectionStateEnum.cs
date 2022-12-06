using System;

namespace Autabee.Communication.ManagedOpcClient
{
    public enum OpcConnectionStatus
    {
        Unknown = 0,
        Disconnected = 1,
        Reconnecting = 2,
        Connected = 3,
    }
}
