using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    public class ContactsGraphWindow : EditorWindow
    {
        private DynamicsGraphview _graphView;

        private void OnEnable()
        {
            _graphView = new DynamicsGraphview()
            {
                name = "Contacts Graph"
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        private void OnFocus()
        {
            _graphView.Refresh();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }
    }
}