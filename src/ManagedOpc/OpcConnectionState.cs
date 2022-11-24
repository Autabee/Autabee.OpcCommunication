using System;
using Opc.Ua;

namespace Autabee.Communication.ManagedOpc
{
    public class OpcConnectionStatusChangedEventArgs : EventArgs
    {

        public OpcConnectionStatus ConnectionState { get; set; } = OpcConnectionStatus.Disconnected;
        public ServiceResult StatusCode { get; set; }
        public string Message { get; set; }
        public OpcConnectionStatusChangedEventArgs(OpcConnectionStatus connectionStatus, ServiceResult statusCode, string message)
        {
            ConnectionState = connectionStatus;
            StatusCode = statusCode;
            Message = message;
        }
    }
}
