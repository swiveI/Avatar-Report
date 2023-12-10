using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDKBase.Validation.Performance;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class DynamicsTab : AvatarReportTab
    {
        int selectedTab = 0;
        string[] tabs = new string[] { "Physbones", "Contacts" };
        
        //lists for contacts and physbones
        public static List<VRCPhysBone> PhysBones = new List<VRCPhysBone>();
        public static List<VRCPhysBoneCollider> PhysBoneColliders = new List<VRCPhysBoneCollider>();
        public static List<VRCContactReceiver> ContactReceivers = new List<VRCContactReceiver>();
        public static List<VRCContactSender> ContactSenders = new List<VRCContactSender>();
        
        int physboneTransforms = 0;
        int physboneCollisions = 0;

        //treeview stuff
        private TreeViewState physBoneTreeState;
        private PhysBoneTreeView physBoneTreeView;
        private MultiColumnHeader physBoneTreeHeader;
        private MultiColumnHeaderState physBoneTreeHeaderState;
        
        private TreeViewState physBoneColliderTreeState;
        private PhysBoneColliderTreeView physBoneColliderTreeView;
        private MultiColumnHeader physBoneColliderTreeHeader;
        private MultiColumnHeaderState physBoneColliderTreeHeaderState;
        
        private TreeViewState contactTreeState;
        private ContactsTreeView contactTreeView;
        private MultiColumnHeader contactTreeHeader;
        private MultiColumnHeaderState contactTreeHeaderState;
        
        //graph
        private ContactsGraphWindow _graphView;
        
        public override void OnTabOpen()
        {
            //clear lists
            PhysBones.Clear();
            PhysBoneColliders.Clear();
            ContactReceivers.Clear();
            ContactSenders.Clear();
            physboneTransforms = 0;
            physboneCollisions = 0;
            
            //setup treeview
            SetupTreeViews();
        }

        public override void OnTabClose()
        {

        }

        private void SetupTreeViews()
        {
            #region Physbones
            var physBoneColumns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Physbones", "The object with a Physbone component"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Grab", "Allows grabbing of this physbone"),
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
                    headerContent = new GUIContent("Pose", "Allows posing of this physbone"),
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
                    headerContent = new GUIContent("Collide", "Allows collision with colliders other than the ones specified on this component.  Currently the only other colliders are each player's hands as defined by their avatar."),
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
                    headerContent = new GUIContent("PAT", "The number of transforms this PhysBone Affects"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Check", "The number of Collision checks this PhysBone performs"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
            };
            
            physBoneTreeState = new TreeViewState();
            physBoneTreeHeaderState = new MultiColumnHeaderState(physBoneColumns);
            physBoneTreeHeader = new MultiColumnHeader(physBoneTreeHeaderState) { height = 30 };
            physBoneTreeHeader.ResizeToFit();
            physBoneTreeView = new PhysBoneTreeView(physBoneTreeState, physBoneTreeHeader);
            
            #endregion
            
            #region Physbone Colliders
            var colliderColumns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Colliders", "The object with a Physbone Collider component"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Enabled", "If this collider is enabled or not"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 60,
                    maxWidth = 60,
                    minWidth = 60,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Inside", "When enabled, this collider contains bones inside its bounds. Does nothing if the Shape is set to plane"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Sphere", "Bones As Spheres. When enabled, this collider treats bones as spheres instead of capsules. This may be advantageous in situations where bones are constantly resting on colliders.  It will also be easier for colliders to pass through bones unintentionally"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Shape", "Type of collision shape used by this collider"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 65,
                    maxWidth = 65,
                    minWidth = 65,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Radius", "Size of the collider extending from its origin. Does nothing if Shape is set to plane"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
            };
            
            physBoneColliderTreeState = new TreeViewState();
            physBoneColliderTreeHeaderState = new MultiColumnHeaderState(colliderColumns);
            physBoneColliderTreeHeader = new MultiColumnHeader(physBoneColliderTreeHeaderState) { height = 30 };
            physBoneColliderTreeHeader.ResizeToFit();
            physBoneColliderTreeView = new PhysBoneColliderTreeView(physBoneColliderTreeState, physBoneColliderTreeHeader);
            #endregion
            
            #region Contacts
            var contactsColumns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Conctacts", "The object with a Contact sender or receiver component"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Enabled", "If this contact is enabled or not"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 60,
                    maxWidth = 60,
                    minWidth = 60,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Shape", "The shape of the contact, either a sphere or a capsule"),
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
                    headerContent = new GUIContent("Radius", "Size of the collider extending from its origin."),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Tags", "The number of Collision Tags this Contact has"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 40,
                    maxWidth = 40,
                    minWidth = 40,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
            };
            
            contactTreeState = new TreeViewState();
            contactTreeHeaderState = new MultiColumnHeaderState(contactsColumns);
            contactTreeHeader = new MultiColumnHeader(contactTreeHeaderState) { height = 30 };
            contactTreeHeader.ResizeToFit();
            contactTreeView = new ContactsTreeView(contactTreeState, contactTreeHeader);
            #endregion
        }

        public override void OnTabGui(float offset)
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, EditorStyles.toolbarButton);
            GUILayout.Space(5f);
            
            //tabs for contacts and physbones
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("PB components:  " + PhysBones.Count, "The number of physbone components on this avatar"), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                    GUILayout.Label(new GUIContent("PB Affected Transforms:  " + physboneTransforms, "The number of physbone components on this avatar"), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("PB Collider components:  " + PhysBoneColliders.Count, "The number of physbone collider components on this avatar"), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                    GUILayout.Label(new GUIContent("PB Collision Check Count:  " + physboneCollisions, "The number of physbone collider components on this avatar"), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("Total Contact receivers:  " + ContactReceivers.Count, "The number of contact receivers on this avatar"), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                    GUILayout.Label(new GUIContent("Total Contact senders:  " + ContactSenders.Count, "The number of contact senders on this avatar"), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                }
            }

            switch (selectedTab)
            {
                case 0:
                    DrawPhysbonesTab(offset);
                    
                    break;
                case 1:
                    DrawContactsTab(offset);
                    break;
            }
        }

        private void DrawContactsTab(float baseOffset)
        {
            GUIStyle openGraphButtonStyle = new GUIStyle("LargeButton");
            openGraphButtonStyle.fontStyle = FontStyle.Bold;
            GUI.backgroundColor = new Color(.5f, .5f, .7f);
            if (GUILayout.Button("Open Contacts Visualizer", openGraphButtonStyle)) EnableGraph();
            GUI.backgroundColor = Color.white;
            
            //draw the treeview
            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (295 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            contactTreeHeader.ResizeToFit();
            contactTreeView.OnGUI(rect);
        }

        private void EnableGraph()
        {
            if (_graphView == null) _graphView = EditorWindow.GetWindow<ContactsGraphWindow>("Contact Tags Visualizer");
        }
        private void DrawPhysbonesTab(float baseOffset)
        {
            //draw the treeview
            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (255 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            
            rect.height /= 2;
            physBoneTreeHeader.ResizeToFit();
            physBoneTreeView.OnGUI(rect);
            
            rect.y += rect.height;
            physBoneColliderTreeHeader.ResizeToFit();
            physBoneColliderTreeView.OnGUI(rect);
            
            EditorGUILayout.EndVertical();
        }

        public override void OnAvatarChanged()
        {
            ProcessAvatarDynamics();
            //if (selectedTab == 1) _graphView.GenerateNodes();
        }

        private void ProcessAvatarDynamics()
        {
            //clear lists
            PhysBones.Clear();
            PhysBoneColliders.Clear();
            ContactReceivers.Clear();
            ContactSenders.Clear();
            
            //get all components
            PhysBones = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<VRCPhysBone>(true).ToList();
            PhysBoneColliders = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<VRCPhysBoneCollider>(true).ToList();
            ContactReceivers = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<VRCContactReceiver>(true).ToList();
            ContactSenders = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<VRCContactSender>(true).ToList();

            physboneTransforms = 0;
            physboneCollisions = 0;
            
            foreach (VRCPhysBone bone in PhysBones)
            {
                bone.InitTransforms(true);
                physboneTransforms += bone.bones.Count;
                physboneCollisions += GetPhysboneCollisionCheckCount(bone);
            }
            
            if (physBoneTreeView == null || physBoneColliderTreeView == null) SetupTreeViews();
            physBoneTreeView.Reload();
            physBoneColliderTreeView.Reload();
            contactTreeView.Reload();
        }

        public static int GetPhysboneCollisionCheckCount(VRCPhysBone bone)
        {
            int CollisionChecks = 0;
            int colliders = 0;
            bool flag = bone.endpointPosition != Vector3.zero;
            foreach (VRCPhysBoneColliderBase collider in bone.colliders)
            {
                if (collider == null) continue;
                colliders++;
            }
                
            if (colliders == 0) return 0;                
            foreach (VRCPhysBoneBase.Bone effectedTransform in bone.bones)
            {
                if (effectedTransform.childCount == 1 ||
                    effectedTransform.childCount > 1 && bone.multiChildType != VRCPhysBoneBase.MultiChildType.Ignore ||
                    effectedTransform.isEndBone & flag)
                    CollisionChecks += colliders;
            }
            return CollisionChecks;
        }
    }
}