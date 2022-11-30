using Autabee.OpcToClass;
using Opc.Ua;
using System;
using System.Linq;

namespace Tests
{
    public class SharperTestSettings
    {
        public GeneratorSettings GeneratorSettings { get; set; }
        public string server { get; set; }
        public string username { get; set; }
        public string password { get; set; }

        public string[] nodes { get; set; }

        public UserIdentity GetUserIdentity()
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;
            return new UserIdentity(username, password);
        }
    }
}