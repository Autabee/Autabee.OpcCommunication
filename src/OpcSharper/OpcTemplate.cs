using System;
using System.Linq;

namespace Autabee.OpcToClass
{
    public abstract class OpcTemplate : IOpcSharperTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get { return Name.Split('.').Last().Replace("\"", ""); } }
        public string nsPrefix { get;set; } = "ns_";
        public string NameSpace
        {
            get
            {
                var split = Name.Split('.');
                var ns = string.Join($".{nsPrefix}", split.Take(split.Length - 1)).Replace("\"", "");
                if (split.Length > 1)
                    return nsPrefix + ns;
                return ns;
            }
        }

        public abstract string GetScript(GeneratorDataSet settings);
        public abstract string GetScriptAsFile(GeneratorDataSet settings);
        public abstract string[] GetScriptNameSpaces();
    }
}
