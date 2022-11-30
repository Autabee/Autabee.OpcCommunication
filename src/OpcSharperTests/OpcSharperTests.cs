using Xunit;
using System;
using System.Linq;
using Autabee.OpcToClass;
using Opc.Ua;
using Xunit.Abstractions;
using Autabee.Utility.Logger.xUnit;
using Autabee.Communication.ManagedOpcClient;
using Newtonsoft.Json;
using System.IO;

namespace Tests
{
    public class OpcSharperTests
    {
        AutabeeXunitLogger logger;
        public OpcSharperTests(ITestOutputHelper outputHelper)
        {
            logger = new AutabeeXunitLogger(outputHelper);
        }
        [SkippableFact]
        public async void GenerateProjectTest()
        {
            Skip.If(!File.Exists("config/secret.json")
                , "secret.json not found");


            //read seceret 
            SharperTestSettings settings;
            using (StreamReader r = new StreamReader("config/secret.json"))
            {
                string json = r.ReadToEnd();
                settings = JsonConvert.DeserializeObject<SharperTestSettings>(json);
            }
            var service = new OpcUaClientHelperApi("Autabee", "UnitX.ApiTester", "Autabee.Tester");

            EndpointDescriptionCollection endpointDescriptionCollection = OpcUaClientHelperApi.GetEndpoints(settings.server);
            endpointDescriptionCollection.ForEach(endpoint => Console.WriteLine(endpoint.EndpointUrl));
            await service.Connect(endpointDescriptionCollection.First(), settings.GetUserIdentity());
            Skip.If(service.Connected, "Error connecting to server");


            var collection = new NodeIdCollection();
            collection.AddRange(settings.nodes.Select(o => new NodeId(o)));

            OpcSharper.GenerateProject(service, collection, settings.GeneratorSettings, logger);
        }

        [SkippableFact]
        public async void GenerateProjectTestBoiler()
        {
            Skip.If(!File.Exists("config/boiler.json")
                , "secret.json not found");


            //read seceret 
            SharperTestSettings settings;
            using (StreamReader r = new StreamReader("config/boiler.json"))
            {
                string json = r.ReadToEnd();
                settings = JsonConvert.DeserializeObject<SharperTestSettings>(json);
            }


            var service = new OpcUaClientHelperApi("Autabee", "UnitX.ApiTester", "Autabee.Tester");
            try
            {
                EndpointDescriptionCollection endpointDescriptionCollection = OpcUaClientHelperApi.GetEndpoints(settings.server);
                endpointDescriptionCollection.ForEach(endpoint => Console.WriteLine(endpoint.EndpointUrl));

                //always accept cert
                service.CertificateValidationNotification += Service_CertificateValidationNotification;

                await service.Connect(endpointDescriptionCollection.Where(o => o.SecurityMode == MessageSecurityMode.None).First(), settings.GetUserIdentity());
            }
            catch (Exception ex)
            {
                Skip.If(true, "Error connecting to server");
            }
            Skip.If(!service.Connected, "Error connecting to server");
            var collection = new NodeIdCollection();
            collection.AddRange(service.BrowseRoot().Select(o => (NodeId)o.NodeId));

            OpcSharper.GenerateProject(service, collection, settings.GeneratorSettings, logger);
        }

        private void Service_CertificateValidationNotification(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            e.Accept = true;
        }
    }
}