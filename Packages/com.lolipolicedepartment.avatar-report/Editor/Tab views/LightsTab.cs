using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class LightsTab : AvatarReportTab
    {
        public static List<Light> Lights = new List<Light>();
        
        //treeview stuff
        private LightsTreeview lightTree;
        private TreeViewState lightTreeState;
        private MultiColumnHeader lightTreeHeader;
        private MultiColumnHeaderState lightTreeHeaderState;

        public override void OnTabOpen()
        {
            //clear list
            Lights.Clear();

            //setup treeview
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name", "The object with a Light component"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type", "The type of light"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 64,
                    maxWidth = 64,
                    minWidth = 64,
                    canSort = false,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Range", "The range of the light"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 64,
                    maxWidth = 64,
                    minWidth = 64,
                    canSort = false,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Intensity", "The intensity of the light"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 64,
                    maxWidth = 64,
                    minWidth = 64,
                    canSort = false,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Shadows", "The lights shadow type. Note: Lights are already expensive, turing on shadows will make them even more expensive!"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 64,
                    maxWidth = 64,
                    minWidth = 64,
                    canSort = false,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Color", "The color of the light"),
                    headerTextAlignment = TextAlignment.Center,
                    minWidth = 64,
                    canSort = false,
                    sortedAscending = true,
                    autoResize = true,
                },
            };
            lightTreeState = new TreeViewState();
            lightTreeHeaderState = new MultiColumnHeaderState(columns);
            lightTreeHeader = new MultiColumnHeader(lightTreeHeaderState) { height = 30 };
            lightTreeHeader.ResizeToFit();
            lightTree = new LightsTreeview(lightTreeState, lightTreeHeader);
        }

        public override void OnTabClose()
        {
            base.OnTabClose();
        }

        public override void OnTabGui(float baseOffset)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(new GUIContent("Total Lights on avatar: " + Lights.Count, "Realtime Lights are performance intensive, try to use shader effects instead where possible"), EditorStyles.boldLabel);
                GUILayout.Label(new GUIContent("Note: Lights do not work on quest"), EditorStyles.boldLabel);
            }
            
            //draw the treeview
            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (220 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            lightTreeHeader.ResizeToFit();
            lightTree.OnGUI(rect);
            EditorGUILayout.EndVertical();
        }

        public override void OnAvatarChanged()
        {
            ProcessAvatarLight();
        }

        private void ProcessAvatarLight()
        {
            //get all lights
            Lights.Clear();
            Lights = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<Light>(true).ToList();
            lightTree.Reload();
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Processed Lights");
        }
    }
}