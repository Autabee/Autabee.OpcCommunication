using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.OpcScout.RazorControl
{
    public class NodeViewModel
    {
        public event EventHandler<ScannedNodeModel> OnSelectedChanged;
        
        public ScannedNodeModel Selected { get; set; }
        public void UpdateSelected(object sender, ScannedNodeModel selected)
        {
            Selected = selected;
            OnSelectedChanged?.Invoke(this, selected);
        }
    }

    public struct NodeDetail
    {
        public string Key;
        public string Value;
        public string Detail;
        public bool Active;

        public NodeDetail()
        {
            Key = string.Empty;
            Value = string.Empty;
            Detail = string.Empty;
            Active = false;
        }

        public NodeDetail(string key, string value, bool active = true, string detail = null)
        {
            Key = key;
            Value = value;
            Active = active;
            Detail = detail == null ? string.Empty : detail;
        }
        
    }
}
