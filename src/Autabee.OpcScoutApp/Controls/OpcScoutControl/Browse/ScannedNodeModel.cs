using Autabee.Communication.ManagedOpcClient;
using Opc.Ua;
using System;
using System.Linq;

namespace Autabee.OpcScoutApp.Controls.OpcScoutControl.Browse
{
    public class ScannedNodeModel
    {
        private readonly ScannedNodeModel parent;

        public ScannedNodeModel[] Children = new ScannedNodeModel[0];
        public bool open = false;

        public ScannedNodeModel(ReferenceDescription referenceDescription, OpcUaClientHelperApi client, Node node, ScannedNodeModel parent = null)
        {
            Reference = referenceDescription;
            Client = client;
            Node = node;
            this.parent = parent;
            NodeImage = NodeImageId.Unknown;
        }

        public event EventHandler deSelect;
        public event EventHandler DoneGettingChildren;

        public void Close()
        {
            open = false;
        }
        public void DeSelect()
        {
            deSelect?.Invoke(this, new EventArgs());
        }

        public void GetChildren()
        {
            NodeImage = NodeImageId.Loading;
            RetrievingChildren = true;
            try
            {
                var token = CancellationToken.None;
                var descriptions = Client.BrowseNode(Reference);

                var splitted = SplitList(descriptions);
                NodeCollection childNodes = new NodeCollection();
                foreach (ReferenceDescriptionCollection split in splitted)
                {
                    childNodes.AddRange(Client.ReadNodes(split));
                }
                var count = descriptions.Count();
                Children = new ScannedNodeModel[count];
                for (int i = 0; i < count; i++)
                {
                    Children[i] = new ScannedNodeModel(descriptions[i], Client, childNodes[i], this);
                }
                NodeImage = Reference.GetNodeImage();
                RetrievedChildren = true;
                DoneGettingChildren?.Invoke(this, new EventArgs());
            }
            catch (Exception e)
            {
                RetrievingChildren = false;
                NodeImage = NodeImageId.Unknown;
            }
        }

        public void Open()
        {
            if (RetrievedChildren && Children.Length > 0) open = true;
        }

        public List<ReferenceDescriptionCollection> SplitList(ReferenceDescriptionCollection me, int size = 50)
        {
            var list = new List<ReferenceDescriptionCollection>();
            for (int i = 0; i < me.Count; i += size)
            {
                var temp = new ReferenceDescriptionCollection();
                temp.AddRange(me.GetRange(i, Math.Min(size, me.Count - i)));
                list.Add(temp);
            }
            return list;
        }
        public NodeClass GetNodeClass() => Reference.NodeClass;

        public string NodeClassType { get; set; } = "Node";
        public OpcUaClientHelperApi Client { get; }
        public Node Node { get; }
        public NodeImageId NodeImage { get; private set; }
        public ReferenceDescription Reference { get; }
        public bool RetrievedChildren { get; private set; }
        public bool RetrievingChildren { get; private set; }
    }
}
