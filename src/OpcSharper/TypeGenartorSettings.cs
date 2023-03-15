using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Autabee.OpcToClass
{
    public class GeneratorSettings
    {
        public string baseNamespace;
        public string baseLocation;
        public NodeIdCollection nodes = new NodeIdCollection();
        public Dictionary<string, string> typeOverrides = new Dictionary<string, string>();
        /// <summary>
        /// Namespace Prefix
        /// </summary>
        public string nsPrefix = "ns_";

        public GeneratorSettings(string baseLocation, string baseNamespace)
        {
            List<Exception> exceptions = new List<Exception>();
            if (string.IsNullOrEmpty(baseLocation))
                exceptions.Add(new ArgumentNullException(nameof(baseLocation)));
            if (string.IsNullOrEmpty(baseNamespace))
                exceptions.Add(new ArgumentNullException(nameof(baseNamespace)));
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
            
            this.baseLocation = baseLocation;
            this.baseNamespace = baseNamespace;
        }
    }
}
