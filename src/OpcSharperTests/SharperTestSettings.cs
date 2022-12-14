using Autabee.OpcToClass;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public class SharperTestSettings
    {
        public GeneratorSettings GeneratorSettings;
        public string server;
        public string username;
        public string password;
        public string[] nodes;

        public SharperTestSettings()
        {
            server = string.Empty;
            username = string.Empty;
            password = string.Empty;
            nodes = new string[0];
        }

        public UserIdentity GetUserIdentity()
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;
            return new UserIdentity(username, password);
        }
    }
}