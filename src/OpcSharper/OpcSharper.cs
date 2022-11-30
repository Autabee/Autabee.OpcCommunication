// See https://aka.ms/new-console-template for more information
using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcToClass;
using Autabee.Utility.Logger;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

public static class OpcSharper
{
    public static void GenerateProject(OpcUaClientHelperApi service, NodeIdCollection collection, GeneratorSettings settings, IAutabeeLogger logger = null)
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
        if (exceptions.Count > 0)
            throw new AggregateException(exceptions);
        if (collection == null)
            collection = new NodeIdCollection();

        logger?.Information("Generate Project");
        OpcToCSharpGenerator.GenerateCsharpProject(settings);


        logger?.Information("Start Retrieving Type Definitions");
        var schemas = service.GetServerTypeSchema();
        XmlDocument[] xmls = new XmlDocument[schemas.Length];
        logger?.Information("Type Definitions Retrieved");
        logger?.Information("Start Generating Type Schema");


        for (int i = 0; i < schemas.Length; i++)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(schemas[i]);
            xmls[i] = xmlDocument;
            OpcToCSharpGenerator.GenerateTypes(xmlDocument,settings);
        }
        logger?.Information("Type Schema updated");
        ReferenceDescriptionCollection refdesc = new ReferenceDescriptionCollection();
        ReferenceDescriptionCollection found = new ReferenceDescriptionCollection();
        foreach (var item in collection)
            refdesc.AddRange(service.BrowseNode(item));
        found.AddRange(refdesc);


        while (refdesc.Count > 1)
        {
            var refdisc = new ReferenceDescriptionCollection();
            foreach (var item in refdesc)
                refdisc.AddRange(service.BrowseNode(item));
            refdesc.Clear();
            refdesc.AddRange(refdisc);
            found.AddRange(refdesc);
            logger?.Information($"New Range {refdisc.Count}:");
            logger?.Information(string.Join(Environment.NewLine, refdesc.Select(o => o.NodeId.ToString())));
        }
        found.OrderBy(o => o);
        OpcToCSharpGenerator.GenerateAddressSpace(found, settings);
        OpcToCSharpGenerator.GenerateNodeEntryAddressSpace(found, xmls, settings);
    }
}

