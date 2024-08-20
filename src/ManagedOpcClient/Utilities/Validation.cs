using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Autabee.Communication.ManagedOpcClient.Utilities
{
    public class OpcValidation
    {
        public static void ValidateResponse(IList<ServiceResult> diagnostics)
        {
            ValidateResponse(diagnostics.Select(o => o.StatusCode));
        }
        
        public static void ValidateResponse(DiagnosticInfoCollection diagnostics)
        {
            ValidateResponse(diagnostics.Select(o => o.InnerStatusCode));
        }
        
        public static void ValidateResponse(StatusCodeCollection diagnostics)
        {
            ValidateResponse(diagnostics.Select(o => o));
        }

        public static void ValidateResponse(CallMethodResultCollection diagnostics)
        {
            ValidateResponse(diagnostics.Select(o => o.StatusCode));
        }


        public static void ValidateResponse(IEnumerable<StatusCode> response)
        {
            var expections = new List<Exception>();
            for (int i = 0; i < response.Count(); i++)
            {
                if (StatusCode.IsBad(response.ElementAt(i)))
                {
                    expections.Add(new ServiceResultException(response.ElementAt(i).Code, $"{i}: {response.ElementAt(i)}", null));
                }
            }
            if (expections.Count > 0) throw new AggregateException(expections);
        }
        public static void ValidateResponse(BrowseDescriptionCollection browseDescriptions, BrowseResultCollection results, DiagnosticInfoCollection diagnostics)
        {
            ClientBase.ValidateResponse(results, browseDescriptions);
            ClientBase.ValidateDiagnosticInfos(diagnostics, browseDescriptions);
        }
        public static void ValidateResponse(BrowseDescriptionCollection browseDescriptions, BrowseResponse response)
        {
            ClientBase.ValidateResponse(response.Results, browseDescriptions);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, browseDescriptions);
        }
    }
}
