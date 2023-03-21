using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcToClass;
using Opc.Ua;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autabee.OpcScout.RazorControl
{
    public class SharperViewModel
    {

        public event EventHandler OnStateHasChanged;
        public event EventHandler OnGenerateProcessStarted;
        public event EventHandler<string> OnGenerateProcessCompleted;
        public readonly Logger logger;
        public readonly List<ClientCache> Clients;
        public string BaseNamespace { get; set; } = "Autabee.Opc.Model";
        public string ProjectLocation { get; set; } = "./Generated/Model/Autabee.Opc.Model";
        public string NameSpacePrefix { get; set; } = "ns_";
        public string ZipSafeLocation { get; set; } = "./Generated/Store/";
        public bool ClearOnZip { get; set; } = false;
        public bool Generating { get; set; } = false;
        public bool ProjectLocationSelectable { get; set; } = true;
        public List<string> Nodes { get; set; } = new List<string>();
        public Dictionary<string, string> TypeOverrides { get; set; } = new Dictionary<string, string>();
        public ClientCache SelectedClient { get; set; } = null;


        public SharperViewModel(List<ClientCache> clients, Logger logger)
        {
            this.logger = logger;
            Clients = clients;
            SelectedClient = clients.FirstOrDefault();
        }


        public async void GenerateProject()
        {
            if (SelectedClient?.client == null || !SelectedClient.client.Connected )
            {
                logger.Error("Failed to generate project, Selected client not connected");
                return;
            }
            if (Generating)
            {
                logger.Error("Failed to generate project, one sharper process at the time allowed");
                return;
            }
            NodeIdCollection nodes = new NodeIdCollection();
            nodes.AddRange(Nodes.Select(o => new NodeId(o)));
            var settings = new GeneratorSettings(ProjectLocation, BaseNamespace)
            {
                nodes = nodes,
                typeOverrides = TypeOverrides,
                nameSpacePrefix = NameSpacePrefix,
                zipStoreLocation = ZipSafeLocation,
                clearOnZip = ClearOnZip
            };

            try
            {
                var task = new Task(() =>
                    OpcSharper.OpcSharper.GenerateProject(SelectedClient.client, settings, logger));
                task.Start();
                Generating = true;
                OnGenerateProcessStarted?.Invoke(this, null);
                OnStateHasChanged?.Invoke(this, null);
                await task;
                logger.Information("Project Genration Complete");
            }
            catch(Exception e)
            {
                logger?.Error(e, "Failed to generate project");
            }
            Generating = false;
            OnStateHasChanged?.Invoke(this, null);
            OnGenerateProcessCompleted?.Invoke(this, OpcSharper.OpcSharper.GetSharperZip(settings));
        }


        public void AddNode(object sender, NodeId nodeId)
        {
            Nodes.Add(nodeId.ToString());
            OnStateHasChanged?.Invoke(sender, null);
        }
    }
}
