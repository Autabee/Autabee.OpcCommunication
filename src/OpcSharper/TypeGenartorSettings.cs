using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Autabee.OpcToClass
{
    public class GeneratorSettings
    {
        public string baseNamespace { get; set; }
        public string baseLocation { get; set; }
        public NodeIdCollection nodes { get; set; } = new NodeIdCollection();
        public string zipStoreLocation { get; set; }
        public bool clearOnZip { get; set; }
        public bool FlipTypeAndEncoding { get; set; }
        public Dictionary<string, string> typeOverrides { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// Namespace Prefix
        /// </summary>
        public string nameSpacePrefix = "ns_";

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

        public GeneratorSettings(GeneratorSettings generatorSettings)
        {
            baseLocation = generatorSettings.baseLocation;
            baseNamespace = generatorSettings.baseNamespace;
            nodes = generatorSettings.nodes;
            zipStoreLocation = generatorSettings.zipStoreLocation;
            clearOnZip = generatorSettings.clearOnZip;
            typeOverrides = generatorSettings.typeOverrides;
            nameSpacePrefix = generatorSettings.nameSpacePrefix;
            FlipTypeAndEncoding = generatorSettings.FlipTypeAndEncoding;
        }

        public GeneratorSettings() { }
    }

    public class GeneratorDataSet : GeneratorSettings
    {
        public Dictionary<string, OpcEnumTemplate> enums = new Dictionary<string, OpcEnumTemplate>();
        public Dictionary<string, OpcStructTemplate> structs = new Dictionary<string, OpcStructTemplate>();

        public GeneratorDataSet(string baseLocation, string baseNamespace) : base(baseLocation, baseNamespace)
        {
        }

        public GeneratorDataSet(GeneratorSettings generatorSettings) : base(generatorSettings)
        {
        }
    }
}
