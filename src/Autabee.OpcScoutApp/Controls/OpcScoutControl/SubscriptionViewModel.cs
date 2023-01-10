using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScoutApp.Controls.OpcScoutControl.Browse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autabee.OpcScoutApp.Controls.OpcScoutControl
{
  public class SubscriptionViewModel
  {
    public readonly List<SubscriptionNodeModel> subscriptionNodeModels = new List<SubscriptionNodeModel>();
    public SubscriptionViewModel()
    {
    }

    public event EventHandler OnListChanged;
    public void AddSubscription(object sender, ScannedNodeModel selected)
    {
      SubscriptionNodeModel model = new SubscriptionNodeModel(selected);
      subscriptionNodeModels.Add(model);
      OnListChanged?.Invoke(sender, null);
    }
  }
}
