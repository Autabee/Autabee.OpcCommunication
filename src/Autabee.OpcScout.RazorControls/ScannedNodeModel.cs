using Autabee.Communication.ManagedOpcClient;
using Autabee.Communication.ManagedOpcClient.Utilities;
using Autabee.Communication.ManagedOpcClient.ManagedNode;
using Autabee.OpcScout.RazorControl.Browse;
using Opc.Ua;
using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace Autabee.OpcScout.RazorControl
{
    public class ScannedNodeModel
    {
        public readonly ScannedNodeModel? parent;

        public ScannedNodeModel[] Children = new ScannedNodeModel[0];
        public bool open = false;
        public string NodeClassType { get; set; } = "Node";
        public AutabeeManagedOpcClient Client { get; }
        protected Node node;
        public Node Node
        {
            get => node; protected set
            {
                if (node.NodeId == value.NodeId)
                {
                    node = value;
                    StateUpdated?.Invoke(this, new EventArgs());
                }

            }
        }
        public NodeImageId NodeImage { get; private set; }
        public ReferenceDescription Reference { get; }
        public bool RetrievedChildren { get; private set; }
        public bool RetrievingChildren { get; private set; }

        public bool IsRootNode => parent == null;

        public ScannedNodeModel(AutabeeManagedOpcClient client, ReferenceDescription Reference, Node node, ScannedNodeModel? parent = null)
        {
            Client = client;
            this.node = node;
            this.Reference = Reference;
            if (node is MethodNode mnode)
            {
                arguments = client.GetMethodArguments(mnode.NodeId);
            }
            this.parent = parent;
            NodeImage = NodeImageId.Unknown;
        }


        public MethodArguments arguments;

        public event EventHandler DeSelectEvent;
        public event EventHandler DoneGettingChildren;
        public event EventHandler StateUpdated;

        public void Close()
        {
            open = false;
        }
        public void DeSelect()
        {
            DeSelectEvent?.Invoke(this, new EventArgs());
        }

        public async void GetChildren()
        {
            NodeImage = NodeImageId.Loading;
            RetrievingChildren = true;
            try
            {
                NodeImage = NodeImageId.Loading;
                StateUpdated?.Invoke(this, new EventArgs());
                var token = CancellationToken.None;
                var results = await Client.AsyncBrowseNode(Node.NodeId, CancellationToken.None);

                var descriptions = Autabee.Communication.ManagedOpcClient.Utilities.Browse.GetDescriptions(results);

                NodeCollection childNodes = new NodeCollection(descriptions.Select(o => new Node(o)));
                
                var count = descriptions.Count();
                Children = new ScannedNodeModel[count];
                for (int i = 0; i < count; i++)
                {
                    Children[i] = new ScannedNodeModel(Client, descriptions[i], childNodes[i], this);
                }
                NodeImage = Reference.GetNodeImage();
                StateUpdated?.Invoke(this, new EventArgs());

                int index = 0;
                var splitted = SplitList(descriptions);
                foreach (ReferenceDescriptionCollection split in splitted)
                {
                    try
                    {
                        var nodes = await Client.AsyncReadNodes(split, CancellationToken.None);
                        foreach (var item in nodes)
                        {
                            Children[index++].Node = item;
                        }
                    }
                    // catch if node read has triggered and exception. 
                    catch (Exception ex) 
                    {
                        foreach (var desc in split)
                        {
                            try
                            {
                                Children[index].Node = Client.ReadNode(desc);
                            }
                            catch
                            {
                                //  could not read specific node
                            }
                            finally
                            {
                                index++;
                            }
                        }
                    }
                }


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

    }
}
