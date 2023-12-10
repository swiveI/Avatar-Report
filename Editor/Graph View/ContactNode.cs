using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using VRC.Dynamics;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class ContactNode : Node
    {
        public ContactBase VrcContact;
        public bool IsReceiver;

        public ContactNode(ContactBase contact)
        {
            VrcContact = contact;
        }
    }

    public class EdgeConnectedListener : IEdgeConnectorListener
    {
        public void OnDropOutsidePort(Edge edge, Vector2 position) { }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            //Debug.Log("edge connected");
            
            DynamicsGraphview view = graphView as DynamicsGraphview;
            
            ContactNode outputNode = edge.output.node as ContactNode;
            ContactNode inputNode = edge.input.node as ContactNode;
            if (outputNode == null || inputNode == null) return;

            if (edge.input.portName == "Connect New Tag")
            {
                //Debug.Log("Connected to output port " + edge.output.portName);
                
                //check if the tag is already in the list
                if (inputNode.VrcContact.collisionTags.Contains(edge.output.portName))
                {
                    view.ConnectPortsOfSameName();
                    return;
                }
                
                //add the tag to the vrcontact
                inputNode.VrcContact.collisionTags.Add(edge.output.portName);
                view.AddPort(inputNode, edge.output.portName);
                view.ConnectPortsOfSameName();

                return;
            }

            if (edge.output.portName == "Connect New Tag")
            {
                //Debug.Log("Connected to input port " + edge.input.portName);

                //check if the tag is already in the list
                if (outputNode.VrcContact.collisionTags.Contains(edge.input.portName))
                {
                    view.ConnectPortsOfSameName();
                    return;
                }

                //add the tag to the vrcontact
                outputNode.VrcContact.collisionTags.Add(edge.input.portName);
                view.AddPort(outputNode, edge.input.portName);
                view.ConnectPortsOfSameName();
            }
        }
    }
}