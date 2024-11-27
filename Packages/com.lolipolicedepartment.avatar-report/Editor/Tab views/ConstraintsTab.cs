using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Animations;

namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    public class ConstraintsTab : AvatarReportTab
    {
        private TreeViewState constraintTreeState;
        private ConstraintTree constraintTree;
        private MultiColumnHeader constraintTreeHeader;
        private MultiColumnHeaderState constraintTreeHeaderState;
        
        //constraints
        private int constraintCount = 0;
        public static List<AimConstraint> AvatarConstraintsAim = new List<AimConstraint>();
        public static List<LookAtConstraint> AvatarConstraintsLookAt = new List<LookAtConstraint>();
        public static List<ParentConstraint> AvatarConstraintsParent = new List<ParentConstraint>();
        public static List<PositionConstraint> AvatarConstraintsPosition = new List<PositionConstraint>();
        public static List<RotationConstraint> AvatarConstraintsRotation = new List<RotationConstraint>();
        public static List<ScaleConstraint> AvatarConstraintsScale = new List<ScaleConstraint>();
        
        public override void OnTabOpen()
        {
            SetupTreeViewItems();
            //ProcessAvatarConstraints();
        }

        private void SetupTreeViewItems()
        {
            //setup treeview
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name", "The object with a Constraint component"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 100,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type", "The type of constraint"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 64,
                    maxWidth = 64,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Active", "Is the constraint active? When set, the constraint is being evaluated and applied at runtime."),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = false,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Lock", "Toggle to let the Constraint move the GameObject. Uncheck this property to edit the position of this GameObject. You can also edit the Position At Rest and Position Offset properties. If Is Active is checked, the Constraint updates the At Rest or Offset properties for you as you move the GameObject or its Source GameObjects. When you are satisfied with your changes, check Lock to let the Constraint control this GameObject. This property has no effect in Play Mode."),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = false,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Weight", "A weight of 1 causes the Constraint to update a GameObject at the same rate as its source GameObjects. A weight of 0 removes the effect of the Constraint completely. Each source GameObject also has an individual weight."),
                    headerTextAlignment = TextAlignment.Center,
                    width = 120,
                    maxWidth = 120,
                    minWidth = 120,
                    canSort = false,
                    sortedAscending = true,
                    autoResize = false,
                },
            };
            
            constraintTreeState = new TreeViewState();
            constraintTreeHeaderState = new MultiColumnHeaderState(columns);
            constraintTreeHeader = new MultiColumnHeader(constraintTreeHeaderState) { height = 30 };
            constraintTreeHeader.ResizeToFit();
            constraintTree = new ConstraintTree(constraintTreeState, constraintTreeHeader);
        }
        
        public override void OnTabGui(float baseOffset)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Constraints are not allowed on quest avatars and will be removed", EditorStyles.boldLabel);
                GUILayout.Label("Total Constraints on avatar: " + constraintCount, EditorStyles.boldLabel);
                GUILayout.EndVertical();
            }
            GUILayout.Space(5f);
            
            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (230 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            constraintTree.OnGUI(rect);
            EditorGUILayout.EndVertical();
        }
        public override void OnAvatarChanged()
        {
            ProcessAvatarConstraints();
        }
        
        public void ProcessAvatarConstraints()
        {
            AvatarConstraintsAim = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<AimConstraint>(true).ToList();
            AvatarConstraintsLookAt = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<LookAtConstraint>(true).ToList();
            AvatarConstraintsParent = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<ParentConstraint>(true).ToList();
            AvatarConstraintsPosition = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<PositionConstraint>(true).ToList();
            AvatarConstraintsRotation = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<RotationConstraint>(true).ToList();
            AvatarConstraintsScale = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<ScaleConstraint>(true).ToList();

            constraintCount = 
                AvatarConstraintsAim.Count + 
                AvatarConstraintsLookAt.Count + 
                AvatarConstraintsParent.Count + 
                AvatarConstraintsPosition.Count + 
                AvatarConstraintsRotation.Count +
                AvatarConstraintsScale.Count;

            if (constraintTree == null) SetupTreeViewItems();
            
            constraintTree.Reload();
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Processsed Constraints");
        }
    }
}