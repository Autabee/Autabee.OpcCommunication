using Autabee.OpcScoutApp.Controls.OpcScoutControl.Browse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autabee.OpcScoutApp.Controls.OpcScoutControl
{
    public class NodeBrowserModel
    {
        public event EventHandler<ScannedNodeModel> OnSelectedChanged;

        private ScannedNodeModel Selected { get; set; }
        public void UpdateSelected(ScannedNodeModel selected)
        {
            Selected?.DeSelect();
            Selected = selected;
            OnSelectedChanged?.Invoke(this, selected);
        }
    }
}
