// See https://aka.ms/new-console-template for more information
using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.Utilities;
using Autabee.OpcToClass;
using Opc.Ua;
using Serilog;
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
        public static void GenerateProject(AutabeeManagedOpcClient service, GeneratorSettings settings, ILogger logger = null)
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


            logger?.Information("Generate Project at, {0}", Path.Combine(Directory.GetCurrentDirectory(), settings.baseLocation));

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
            {
                var node = service.BrowseNode(item);
                refdesc.AddRange(node.References);
            };
            found.AddRange(refdesc);


            while (refdesc.Count >= 1)
            {
                var refdisc = new ReferenceDescriptionCollection();

                foreach (var item in refdesc)
                {
                    refdisc.AddRange(service.BrowseNode(item).References);
                }

                refdesc.Clear();
                refdesc.AddRange(refdisc);
                found.AddRange(refdesc);
                logger?.Information($"New Range {refdisc.Count}:");
                logger?.Information(string.Join(Environment.NewLine, refdesc.Select(o => o.NodeId.ToString())));
            }
            found.OrderBy(o => o);
            var nodes_to_remove = new List<ReferenceDescription>();
#if NET6_0_OR_GREATER
            foreach (var chunk in found.ToList().Chunk(50))
            {
                try
                {
                    var nodes = service.ReadNodes(new ReferenceDescriptionCollection(chunk));
                    foundTypes.AddRange(nodes);
                }
                catch (AggregateException ex)
                {
                    // example System.AggregateException: One or more errors occurred. (0: BadNotReadable) (3: BadNotReadable) (15: BadNotReadable)
                    // fiter out nodes that could not be read and log them and reread the rest
                    var readExceptions = ex.InnerExceptions.ToList();
                    if (readExceptions.Count > 0)
                    {
                        var indexFailed = readExceptions.Select(e => e.Message.Split(':')[0].Trim()).ToList();
                        var unfailedNodes = chunk.Where((o, i) => !indexFailed.Contains(i.ToString()));
                        logger?.Error(ex, $"Error reading nodes. Failed nodes: {string.Join(", ", chunk.Where((o, i) => indexFailed.Contains(i.ToString())))}. Retrying with unfailed nodes.");
                        try
                        {
                            var nodes = service.ReadNodes(new ReferenceDescriptionCollection(unfailedNodes));
                            nodes_to_remove.AddRange(chunk.Where((o, i) => indexFailed.Contains(i.ToString())));
                            foundTypes.AddRange(nodes);
                        }
                        catch (Exception ex2)
                        {
                            logger?.Error(ex2, $"Error reading nodes. trying individual node readings.");
                            foreach (var item in chunk)
                            {
                                try
                                {
                                    var nodes = service.ReadNode(item);
                                    foundTypes.Add(nodes);
                                }
                                catch (Exception ex3)
                                {
                                    nodes_to_remove.Add(item);
                                    logger?.Error(ex3, $"Error reading node {item.NodeId}. Skipping it for reading.");
                                }
                
                            }
                        }
                    }
                    else
                    {
                        logger?.Error(ex, $"Error reading nodes. Skipping chunk for reading.");
                    }
                }
                
            }
#else
            foreach (var chunk in found)
            {
                try
                {
                    var nodes = service.ReadNode(chunk);
                    foundTypes.Add(nodes);
                }
                catch (Exception ex)
                {
                    nodes_to_remove.Add(chunk);
                    logger?.Error(ex, $"Error reading node {chunk.NodeId}. Skipping it for reading.");
                }
                
            }
#endif
            foreach (var item in nodes_to_remove)
            {
                logger?.Warning($"Removing node {item.NodeId} from found nodes because it could not be read.");
                found.Remove(item);
            }


            OpcToCSharpGenerator.GenerateAddressSpace(found, settings);
            OpcToCSharpGenerator.GenerateNodeEntryAddressSpace(service, found, foundTypes, xmls, settings);

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
                    Directory.Delete(settings.baseLocation, true);
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