using Autabee.Utility.Logger;
using Autabee.Utility.Messaging;
using Windows.UI.Notifications;

namespace Autabee.OpcScoutApp
{
  public class InMemoryLog : IAutabeeLogger
  {
    public List<LogMessage> Messages { get; set; } = new List<LogMessage>();
    public event EventHandler<LogMessage> MessageUpdate;

    public void Error(string message, Exception e = null, params object[] values)
    {
      AddMessage(MessageLevel.Error, message, e, values);
    }

    public void Error(string message, params object[] values)
    {
      AddMessage(MessageLevel.Error, message, null, values);
    }

    public void Fatal(string message, Exception e = null, params object[] values)
    {
      AddMessage(MessageLevel.Fatal, message, e, values);
    }

    public void Fatal(string message, params object[] values)
    {
      AddMessage(MessageLevel.Fatal, message, null, values);
    }

    public void Information(string message, Exception e = null, params object[] values)
    {
      AddMessage(MessageLevel.Info, message, e, values);
    }

    public void Information(string message, params object[] values)
    {
      AddMessage(MessageLevel.Info, message, null, values);
    }

    public void Log(Utility.Messaging.Message message)
    {
      AddMessage(message.Level, message.Text, null, message.Parameters);
    }

    public void Log(Utility.Messaging.Message message, Exception e)
    {
      AddMessage(message.Level, message.Text, e, message.Parameters);
    }

    public void Warning(string message, Exception e = null, params object[] values)
    {
      AddMessage(MessageLevel.Warning, message, e, values);
    }

    public void Warning(string message, params object[] values)
    {
      throw new NotImplementedException();
    }

    private void AddMessage(MessageLevel Code, string message, Exception e = null, params object[] values)
    {
      var messagestruct = new LogMessage(new Message(Code, message, values)) { ShowFullMessage = true };
      Messages.Add(messagestruct);
      MessageUpdate?.Invoke(this, messagestruct);
    }
  }

  public class LogMessage
  {
    public readonly Message message;
    public LogMessage()
    {

    }
    public LogMessage(Message message)
    {
      this.message = message;
      MultiLine = message.Text.Count(c => c == '\n') >= 1;
    }
    public bool MultiLine;
    public bool ShowFullMessage;
    public string MessageClass => ShowFullMessage ? "log-message" : "log-message-short";
  }

}
