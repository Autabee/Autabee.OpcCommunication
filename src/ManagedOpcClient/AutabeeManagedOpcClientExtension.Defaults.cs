using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.Communication.ManagedOpcClient.ManagedNodeCollection;
using Autabee.Utility.Logger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua.Security.Certificates;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Autabee.Communication.ManagedOpcClient
{
    public static partial class AutabeeManagedOpcClientExtension
    {
        #region Defaults
        public static ApplicationConfiguration GetClientConfiguration(
            string company, string product, string directory, IAutabeeLogger logger = null)
        {
            var error = new System.Collections.Generic.List<Exception>();
            if (string.IsNullOrWhiteSpace(company)) error.Add(new ArgumentNullException(nameof(company)));
            if (string.IsNullOrWhiteSpace(product)) error.Add(new ArgumentNullException(nameof(product)));
            if (string.IsNullOrWhiteSpace(directory)) error.Add(new ArgumentNullException(nameof(directory)));
            if (error.Count() != 0) { throw new AggregateException(error); }

            ApplicationInstance configuration = new ApplicationInstance();
            configuration.ApplicationType = ApplicationType.Client;
            configuration.ConfigSectionName = product;

            var combined = Path.Combine(directory, product + ".Config.xml");
            
            if (!File.Exists(combined))
            {
                CreateDefaultConfiguration(company, product, directory, logger, combined);
            }

            configuration.LoadApplicationConfiguration(combined, false).Wait();
            configuration.CheckApplicationInstanceCertificate(false, 0).Wait();

            return configuration.ApplicationConfiguration;
        }

        private static void CreateDefaultConfiguration(string company, string product, string directory, IAutabeeLogger logger, string combined)
        {
            logger?.Warning("File {0} not found. recreating it using embedded default.", null, combined);
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Autabee.Communication.ManagedOpcClient.DefaultOpcClient.Config.xml"))
            {
                using (StreamReader reader = new StreamReader(resource))
                {
                    string result = reader.ReadToEnd();
                    result = result.Replace("productref", product);
                    result = result.Replace("companyref", company);
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(combined, result);
                    logger?.Warning("File {0} Created and updated with ({1}, {2}).", null, combined, product, company);
                    
                }
            }
        }

        public static ApplicationConfiguration CreateDefaultClientConfiguration(Stream configStream)
        {

            ApplicationInstance configuration = new ApplicationInstance();
            configuration.ApplicationType = ApplicationType.Client;
            configuration.LoadApplicationConfiguration(configStream, false).Wait();
            configuration.CheckApplicationInstanceCertificate(false, 2048).Wait();

            return configuration.ApplicationConfiguration;
        }

        public static X509Certificate2 CreateDefaultClientCertificate(ApplicationConfiguration configuration)
        {
            // X509Certificate2 clientCertificate;
            ICertificateBuilder builder = CertificateBuilder.Create($"cn={configuration.ApplicationName}");
            builder = builder.SetHashAlgorithm(System.Security.Cryptography.HashAlgorithmName.SHA256);
            builder = (ICertificateBuilder)builder.SetRSAKeySize(2048);
            builder = builder.SetLifeTime(24);
            builder = builder.CreateSerialNumber();
            builder = builder.SetNotBefore(DateTime.Now);

#if NET48_OR_GREATER || NET5_0_OR_GREATER
      builder = builder.AddExtension(GetLocalIpData(configuration.ApplicationUri));
#endif
            var clientCertificate = builder.CreateForRSA();

            clientCertificate.AddToStore(
                configuration.SecurityConfiguration.ApplicationCertificate.StoreType,
                configuration.SecurityConfiguration.ApplicationCertificate.StorePath);
            return clientCertificate;
        }
        #endregion Defaults

#if NET48_OR_GREATER || NET5_0_OR_GREATER
        private static X509Extension GetLocalIpData(string applicationUri)
        {
            var abuilder = new SubjectAlternativeNameBuilder();
            List<string> localIps = new List<string>();
            abuilder.AddUri(new Uri(applicationUri));
            abuilder.AddDnsName(Dns.GetHostName());
            var host = Dns.GetHostEntry(Dns.GetHostName());
            bool found = false;
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    abuilder.AddIpAddress(ip);
                    found = true;
                }
            }
            if (!found)
            {
                throw new Exception("Local IP Address Not Found!");
            }
            return abuilder.Build();
        }
#endif
    }
}