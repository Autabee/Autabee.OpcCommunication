// See https://aka.ms/new-console-template for more information
using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.Utilities;
using Autabee.OpcToClass;
using Opc.Ua;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace Autabee.OpcSharper
{
    public static class OpcSharper
    {
        public static void GenerateProject(AutabeeManagedOpcClient service, GeneratorSettings settings, Logger logger = null)
        {
            List<Exception> exceptions = new List<Exception>();
            if (settings == null)
                exceptions.Add(new ArgumentNullException(nameof(settings)));
            if (service == null)
            {
                exceptions.Add(new ArgumentNullException(nameof(service)));
                exceptions.Add(new InvalidOperationException("Client is not connected"));
            }
            if (service != null && service.Connected == false)
                exceptions.Add(new InvalidOperationException("Client is not connected"));
            if (settings.nodes == null)
                exceptions.Add(new ArgumentNullException(nameof(settings.nodes)));
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);


            logger?.Information("Generate Project at, {0}", Path.Combine(Directory.GetCurrentDirectory(), settings.baseLocation) );
#if skip
            OpcToCSharpGenerator.GenerateCsharpProject(settings);


            logger?.Information("Start Retrieving Type Definitions");
            var schemas = service.Session.GetServerTypeSchema();
            XmlDocument[] xmls = new XmlDocument[schemas.Length];
            logger?.Information("Type Definitions Retrieved");
            logger?.Information("Start Generating Type Schema");


            for (int i = 0; i < schemas.Length; i++)
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(schemas[i]);
                xmls[i] = xmlDocument;
                OpcToCSharpGenerator.GenerateTypeFiles(xmlDocument, settings);
            }
            logger?.Information("Type Schema updated");
            ReferenceDescriptionCollection refdesc = new ReferenceDescriptionCollection();
            ReferenceDescriptionCollection found = new ReferenceDescriptionCollection();
            NodeCollection foundTypes = new NodeCollection();
            foreach (var item in settings.nodes)
                refdesc.AddRange(service.BrowseNode(item));
            found.AddRange(refdesc);


            while (refdesc.Count > 1)
            {
                var refdisc = new ReferenceDescriptionCollection();

                foreach (var item in refdesc)
                {
                    refdisc.AddRange(service.BrowseNode(item));
                }

                refdesc.Clear();
                refdesc.AddRange(refdisc);
                found.AddRange(refdesc);
                logger?.Information($"New Range {refdisc.Count}:");
                logger?.Information(string.Join(Environment.NewLine, refdesc.Select(o => o.NodeId.ToString())));
            }
            found.OrderBy(o => o);
#if NET6_0_OR_GREATER
            foreach (var chunk in found.ToList().Chunk(50))
            {
                var nodes = service.ReadNodes(new ReferenceDescriptionCollection(chunk));
                foundTypes.AddRange(nodes);
            }
#else
            foreach (var chunk in found)
            {
                var nodes = service.ReadNode(chunk);
                foundTypes.Add(nodes);
            }
#endif

            OpcToCSharpGenerator.GenerateAddressSpace(found, settings);
            OpcToCSharpGenerator.GenerateNodeEntryAddressSpace(service, found, foundTypes, xmls, settings);
#endif
            try
            {
                string path = GetSharperZip(settings);
                logger.Information("create zip at {0}", path);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                ZipFile.CreateFromDirectory(settings.baseLocation, path);
                if (settings.clearOnZip)
                {
                    Directory.Delete(settings.baseLocation,true);
                }
            }
            catch
            {
                
            }
        }

        public static string GetSharperZip(GeneratorSettings settings)
        {
            Directory.CreateDirectory(settings.zipStoreLocation);
            var path = Path.Combine(settings.zipStoreLocation, settings.baseNamespace + ".zip");
            return path;
        }
    }
}