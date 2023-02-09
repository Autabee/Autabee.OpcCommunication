using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Autabee.OpcScoutApp
{
    public class EndpointRecord
    {
        public int Id { get; set; }
        [Required]
        public string Server { get; set; }
        [Required]
        public string EndpointUrl { get; set; }
        [Required]
        public Opc.Ua.MessageSecurityMode SecurityMode { get; set; }
        public string SecurityPolicyUri { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EndpointRecord()
        {
        }

        public EndpointRecord Copy()
        {
            return new EndpointRecord
            {
                Server = Server,
                EndpointUrl = EndpointUrl,
                SecurityMode = SecurityMode,
                SecurityPolicyUri = SecurityPolicyUri,
                Name = Name,
                Description = Description
            };
        }
    }
}
