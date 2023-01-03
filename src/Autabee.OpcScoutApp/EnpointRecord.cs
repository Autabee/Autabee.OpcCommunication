namespace Autabee.OpcScoutApp
{
    public class EndpointRecord
    {
        public Opc.Ua.EndpointDescription EndpointDescription { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EndpointRecord()
        {
        }
    }
}
