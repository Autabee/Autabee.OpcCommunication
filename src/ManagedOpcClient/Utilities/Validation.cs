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
            string message = response.Where(o => StatusCode.IsNotGood(o.Code))
                .Aggregate(string.Empty, (accumulator, result) =>
                accumulator += $"{result} : {StatusCodes.GetBrowseName(result.Code)}" + Environment.NewLine);

            if (message.Length > 0) throw new ServiceResultException(message);
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
