namespace Autabee.OpcScoutApp
{
    public class EndpointRecord
    {
        public string Server { get; set; }
        public string EndpointUrl { get; set; }
        public Opc.Ua.MessageSecurityMode SecurityMode { get; set; }
        public string SecurityPolicyUri { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EndpointRecord()
        {
        }
    }
}
