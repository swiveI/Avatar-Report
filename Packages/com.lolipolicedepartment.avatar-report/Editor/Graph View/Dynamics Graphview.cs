using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Dynamics;
using Button = UnityEngine.UIElements.Button;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class DynamicsGraphview : GraphView
    {
        public class uxmlFactory : UxmlFactory<DynamicsGraphview, UxmlTraits> { }
        
        public const string stylesPath = "Packages/com.lolipolicedepartment.avatar-report/Editor/Graph View/ContactNodeStyles.uss";
        private List<ContactNode> senderNodes = new List<ContactNode>();
        private List<ContactNode> receiverNodes = new List<ContactNode>();
        public DynamicsGraphview()
        {
            StyleSheet styles = (StyleSheet)AssetDatabase.LoadAssetAtPath(stylesPath, typeof(StyleSheet));
            styleSheets.Add(styles);
            
            // Add a grid background
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            
            //manipulators
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            
            GenerateNodes();
            ConnectPortsOfSameName();
            FrameAll();
        }

        public void Refresh()
        {
            //todo come up with a better way to refresh the graph so we dont lose all the node positions
            GenerateNodes();
            ConnectPortsOfSameName();
        }
        public void GenerateNodes()
        {
            //clear all nodes in the current graph
            DeleteElements(graphElements.ToList());
            
            //create all nodes
            int senders = 0;
            Rect rect = new Rect(100, -20, 200, 150);
            foreach (ContactBase contact in DynamicsTab.ContactSenders)
            {
                if (contact == null) continue;

                rect.y += 120;
                senders++;
                ContactNode node = CreateContactNode(contact, rect);
                rect.y += 24 * contact.collisionTags.Count;
                
                node.RefreshExpandedState();
                node.RefreshPorts();
                senderNodes.Add(node);
                AddElement(node);
            }
            
            int recievers = 0;
            rect = new Rect(700, -20, 200, 150);
            foreach (ContactBase contact in DynamicsTab.ContactReceivers)
            {
                if (contact == null) continue;
                
                rect.y += 120;
                recievers++;
                ContactNode node = CreateContactNode(contact, rect);
                rect.y += 24 * contact.collisionTags.Count;

                node.RefreshExpandedState();
                node.RefreshPorts();
                receiverNodes.Add(node);
                AddElement(node);
            }
        }

        public void ConnectPortsOfSameName()
        {
            List<Port> outputports = new List<Port>();
            List<Port> inputports = new List<Port>();
            
            //clear all edges
            DeleteElements(edges.ToList());
            
            
            //find all ports
            ports.ForEach((port) =>
            {
                if (port.direction == Direction.Input)
                {
                    inputports.Add(port);
                }
                else
                {
                    outputports.Add(port);
                }
            });
            //Debug.Log("Found " + outputports.Count + " output ports and " + inputports.Count + " input ports");

            foreach (Port output in outputports)
            { 
                if (output.portName == "Connect New Tag") continue;
                //compare name against all input ports names
                foreach (Port input in inputports)
                {
                    if (output.portName == input.portName)
                    {
                        Edge edge = output.ConnectTo(input);
                        
                        // Prevent user from disconnecting the edge
                        edge.SetEnabled(false);
                        AddElement(edge);
                    }
                }
            }
        }
        
        private ContactNode CreateContactNode(ContactBase contact, Rect rect)
        {
            ContactNode node = new ContactNode(contact)
            {
                IsReceiver = contact is ContactReceiver,
            };
            //make node not deletable
            node.capabilities &= ~Capabilities.Deletable;
            VisualElement title = node.titleContainer.ElementAt(0);
            title.style.alignItems = Align.Center;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            node.title = contact.gameObject.name;
            Color nodeColor = node.IsReceiver ? new Color(.3f, .3f, .5f) : new Color(.1f, .4f, .3f);
            node.titleContainer.style.backgroundColor = nodeColor;
            
            //callback for when node is double clicked to select the gameobject
            node.RegisterCallback<MouseDownEvent>((evt) =>
            {
                if (evt.clickCount == 2)
                {
                    Selection.activeObject = contact.gameObject;
                }
            });
            
            Direction direction = node.IsReceiver ? Direction.Input : Direction.Output;
            
            //add port for creating new tags when another port is connected to it
            Port newTagPort = node.InstantiatePort(Orientation.Horizontal, direction, Port.Capacity.Multi, typeof(string));
            newTagPort.portName = "Connect New Tag";
            
            //callback when port is connected
            newTagPort.AddManipulator(new EdgeConnector<Edge>(new EdgeConnectedListener()));
            node.inputContainer.Add(newTagPort);
            
            
            //add port for each tag
            foreach (string tag in contact.collisionTags)
            {
                AddPort(node, tag);
            }

            //add small label under the name to indicate if the node is sender or reciever
            VisualElement labelContainer = node.contentContainer.ElementAt(0);
            var label = new Label(node.IsReceiver ? "Contact Receiver" : "Contact Sender");
            label.style.fontSize = 10;
            label.style.backgroundColor = nodeColor * .8f;
            label.style.unityTextAlign = TextAnchor.LowerCenter;
            labelContainer.style.flexDirection = FlexDirection.Column;
            labelContainer.Insert(1,label);

            VisualElement newTagContainer = new VisualElement();
            newTagContainer.style.backgroundColor = nodeColor * .8f;
            newTagContainer.style.flexDirection = node.IsReceiver? FlexDirection.Row : FlexDirection.RowReverse;
            newTagContainer.style.alignItems = Align.Center;
            
            //new port name
            string newPortName = "New Tag";
            var textField = new TextField(){value = newPortName};
            textField.RegisterValueChangedCallback((evt) =>
            {
                newPortName = evt.newValue;
                ConnectPortsOfSameName();
            });
            textField.style.minWidth = 120;
            textField.style.maxHeight = 25;
            textField.contentContainer.style.alignSelf = Align.Center;
            newTagContainer.Add(textField);
            
            //add port button
            var button = new Button(() =>
            {
                AddPort(node, newPortName);
                contact.collisionTags.Add(newPortName);
                node.RefreshPorts();
                ConnectPortsOfSameName();
            });
            button.text = "Add Tag";
            button.style.maxHeight = 25;
            button.contentContainer.style.alignSelf = Align.Center;
            newTagContainer.Add(button);

            labelContainer.Insert(2, newTagContainer);
            node.SetPosition(rect);
            
            return node;
        }
        
        public void AddPort(ContactNode node, string name)
        {
            Port port = node.InstantiatePort(Orientation.Horizontal, node.IsReceiver ? Direction.Input : Direction.Output, Port.Capacity.Multi, typeof(string));
            port.portName = name;
            
            //callback when port is connected
            port.AddManipulator(new EdgeConnector<Edge>(new EdgeConnectedListener()));
            
            //remove the port label in favor of our own
            //var label = port.contentContainer.Q<Label>("type");
            //var label = port.contentContainer.ElementAt(1); //cant grab input port nodes now?
            //label.style.display = DisplayStyle.None;
            
            //make our own editable label to set the contact tag and port name
            var textField = new TextField()
            {
                value = name,
                isDelayed = true,
            };
            textField.RegisterValueChangedCallback((evt) =>
            {
                node.VrcContact.collisionTags.Remove(name);
                name = evt.newValue;
                port.portName = name;
                node.VrcContact.collisionTags.Add(name);
                ConnectPortsOfSameName();
            });
            textField.style.minWidth = 100;
            textField.style.maxHeight = 20;
            port.contentContainer.Insert(1, textField);
            
            var deleteButton = new Button(() =>{RemovePort(node, port);})
            {
                text = "Remove Tag"
            };
            port.contentContainer.Insert(2, deleteButton);
            
            node.inputContainer.Add(port);
        }

        private void RemovePort(ContactNode node, Port port)
        {
            var edgeList = edges.ToList().Where(x => x.output == port || x.input == port).ToList();
            foreach (Edge edge in edgeList)
            {
                edge.input.Disconnect(edge);
                edge.output.Disconnect(edge);
                RemoveElement(edge);
            }
            
            node.VrcContact.collisionTags.Remove(port.portName);
            RemoveElement(port);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }
        
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();
            ports.ForEach((port) =>
            {
                if (startPort != port && startPort.node != port.node)
                {
                    if (startPort.direction != port.direction)
                    {
                        if (startPort.portName == "Connect New Tag" && port.portName != "Connect New Tag")
                        {
                            compatiblePorts.Add(port); //Debug.Log("case 1");
                            return;
                        }
                        if (startPort.portName != "Connect New Tag" && port.portName == "Connect New Tag")
                        {
                            compatiblePorts.Add(port); //Debug.Log("case 2");
                            return;
                        }
                    }
                }
            });
            return compatiblePorts;
        }
    }
}
