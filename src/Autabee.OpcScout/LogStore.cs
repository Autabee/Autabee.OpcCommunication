using MudBlazor;
using Newtonsoft.Json.Serialization;
using Serilog.Core;
using Serilog.Events;

namespace Autabee.OpcScout
{
    public class InMemoryLog : ILogEventSink
    {
        ISnackbar snackbar;
        public List<LogMessage> Messages { get; set; } = new List<LogMessage>();
        public event EventHandler<LogMessage> MessageUpdate;
        public InMemoryLog(ISnackbar snackbar)
        {
            this.snackbar = snackbar;
        }
        public void Emit(LogEvent logEvent)
        {
            var log = new LogMessage(logEvent);
            Messages.Add(log);
            MessageUpdate?.Invoke(this,log);
            if (logEvent.Level >= LogEventLevel.Warning)
            {
                snackbar.Add(log.ToString(), (Severity)(int)logEvent.Level);
            }
        }
    }

    public class LogMessage
    {
        public readonly LogEvent message;
        public LogMessage()
        {

        }
        public LogMessage(LogEvent message)
        {
            this.message = message;
            MultiLine = message.RenderMessage().Count(c => c == '\n') >= 1;
        }
        public bool MultiLine;
        public bool ShowFullMessage;
        public string MessageClass => ShowFullMessage ? "log-message" : "log-message-short";
    }

}
