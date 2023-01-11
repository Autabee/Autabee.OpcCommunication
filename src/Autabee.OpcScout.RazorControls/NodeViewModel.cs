using Autabee.Utility.Messaging;
using System;
using System.Linq;

namespace Autabee.OpcScout.RazorControl
{
    public class NodeViewModel
    {
        public event EventHandler<ScannedNodeModel> OnSelectedChanged;

        private ScannedNodeModel Selected { get; set; }
        public void UpdateSelected(object sender, ScannedNodeModel selected)
        {
            Selected?.DeSelect();
            Selected = selected;
            OnSelectedChanged?.Invoke(this, selected);
        }
    }
}
