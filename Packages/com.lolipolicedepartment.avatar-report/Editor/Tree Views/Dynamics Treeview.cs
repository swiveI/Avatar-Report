using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    #region Physbones
    public class PhysBoneTreeViewItem : TreeViewItem
    {
        public VRCPhysBone Bone;
        public VRCPhysBoneBase.Bone ChildBone;
        public int AffectedTransforms = 0;
        public int CollisionCheckCount = 0;
    }

    enum physboneInteractionTypes
    {
        False,
        True,
        SelfOnly,
        OthersOnly,
    }
    
    public class PhysBoneTreeView : TreeView
    {
        List<TreeViewItem> treeitems = new List<TreeViewItem>();
        public PhysBoneTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            header.sortingChanged += OnSortingChanged;
        }

        private void OnSortingChanged(MultiColumnHeader multicolumnheader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<PhysBoneTreeViewItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.displayName);
                    break;
                case 1:
                    items = items.OrderBy(x => x.Bone.allowGrabbing);
                    break;
                case 2:
                    items = items.OrderBy(x => x.Bone.allowPosing);
                    break;
                case 3:
                    items = items.OrderBy(x => x.Bone.allowCollision);
                    break;
                case 4:
                    items = items.OrderBy(x => x.AffectedTransforms);
                    break;
                case 5:
                    items = items.OrderBy(x => x.CollisionCheckCount);
                    break;
                default: break;
            }

            if (!multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex))
            {
                items = items.Reverse();
            }
            rootItem.children = items.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }
        
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var obj = (PhysBoneTreeViewItem)FindItem(id, rootItem);
            if (obj.depth == 0)
            {
                if (obj.Bone != null)
                {
                    EditorGUIUtility.PingObject(obj.Bone);
                    Selection.activeGameObject = obj.Bone.gameObject;
                }

                return;
            }

            if (obj.depth == 1)
            {
                string path = AssetDatabase.GetAssetPath(obj.ChildBone.transform);
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(path));
                Selection.activeGameObject = obj.ChildBone.transform.gameObject;
            }
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            PhysBoneTreeViewItem item = (PhysBoneTreeViewItem)args.item;
            
            if (item.Bone == null)
            {
                GUI.color = Color.red;
                Rect rect = args.GetCellRect(0);
                rect.xMin += 16 + (16f * args.item.depth);
                EditorGUI.LabelField(rect, "Item Missing!");
                GUI.color = Color.white;
                return;
            }
            
            if (args.item.depth == 1)
            {
                Rect rect = args.GetCellRect(0);
                rect.xMin += 32f;
                rect.width = Screen.width - 32f;
                GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), item.icon, ScaleMode.ScaleToFit);
                rect.x += 20;
                rect.width -= 20;
                GUI.Label(rect, item.displayName);
                return;
            }

            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                Rect rect = args.GetCellRect(i);
                int columnIndex = args.GetColumn(i);
                
                switch (columnIndex)
                {
                    case 0:
                    {
                        rect.x += GetContentIndent(args.item);
                        rect.width -= GetContentIndent(args.item);
                        rect.xMin += 16f * args.item.depth;
                        GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), item.icon, ScaleMode.ScaleToFit);
                        rect.x += 20;
                        rect.width -= 20;
                        GUI.Box(rect, item.displayName);
                        break;
                    }
                    case 1:
                    {
                        physboneInteractionTypes grabType = physboneInteractionTypes.False;
                        switch (item.Bone.allowGrabbing)
                        {
                            case VRCPhysBoneBase.AdvancedBool.False:
                                grabType = physboneInteractionTypes.False;
                                break;
                            case VRCPhysBoneBase.AdvancedBool.True:
                                grabType = physboneInteractionTypes.True;
                                break;
                            case VRCPhysBoneBase.AdvancedBool.Other:
                                //value is other, need to check the bools
                                if (item.Bone.grabFilter.allowSelf) grabType = physboneInteractionTypes.SelfOnly;
                                else grabType = physboneInteractionTypes.OthersOnly;
                                break;
                        }
                        
                        grabType = (physboneInteractionTypes)EditorGUI.EnumPopup(rect, grabType);

                        //convert back to advanced bool and set the filter bools
                        switch (grabType)
                        {
                            case physboneInteractionTypes.False:
                                item.Bone.allowGrabbing = VRCPhysBoneBase.AdvancedBool.False;
                                break;
                            case physboneInteractionTypes.True:
                                item.Bone.allowGrabbing = VRCPhysBoneBase.AdvancedBool.True;
                                break;
                            case physboneInteractionTypes.SelfOnly:
                                item.Bone.allowGrabbing = VRCPhysBoneBase.AdvancedBool.Other;
                                item.Bone.grabFilter.allowSelf = true;
                                item.Bone.grabFilter.allowOthers = false;
                                break;
                            case physboneInteractionTypes.OthersOnly:
                                item.Bone.allowGrabbing = VRCPhysBoneBase.AdvancedBool.Other;
                                item.Bone.grabFilter.allowSelf = false;
                                item.Bone.grabFilter.allowOthers = true;
                                break;
                        }
                        break;
                    }
                    case 2:
                    {
                        physboneInteractionTypes poseType = physboneInteractionTypes.False;
                        switch (item.Bone.allowPosing)
                        {
                            case VRCPhysBoneBase.AdvancedBool.False:
                                poseType = physboneInteractionTypes.False;
                                break;
                            case VRCPhysBoneBase.AdvancedBool.True:
                                poseType = physboneInteractionTypes.True;
                                break;
                            case VRCPhysBoneBase.AdvancedBool.Other:
                                //value is other, need to check the bools
                                if (item.Bone.poseFilter.allowSelf) poseType = physboneInteractionTypes.SelfOnly;
                                else poseType = physboneInteractionTypes.OthersOnly;
                                break;
                        }
                        
                        poseType = (physboneInteractionTypes)EditorGUI.EnumPopup(rect, poseType);

                        //convert back to advanced bool and set the filter bools
                        switch (poseType)
                        {
                            case physboneInteractionTypes.False:
                                item.Bone.allowPosing = VRCPhysBoneBase.AdvancedBool.False;
                                break;
                            case physboneInteractionTypes.True:
                                item.Bone.allowPosing = VRCPhysBoneBase.AdvancedBool.True;
                                break;
                            case physboneInteractionTypes.SelfOnly:
                                item.Bone.allowPosing = VRCPhysBoneBase.AdvancedBool.Other;
                                item.Bone.poseFilter.allowSelf = true;
                                item.Bone.poseFilter.allowOthers = false;
                                break;
                            case physboneInteractionTypes.OthersOnly:
                                item.Bone.allowPosing = VRCPhysBoneBase.AdvancedBool.Other;
                                item.Bone.poseFilter.allowSelf = false;
                                item.Bone.poseFilter.allowOthers = true;
                                break;
                        }
                        break;
                    }
                    case 3:
                    {
                        physboneInteractionTypes collideType = physboneInteractionTypes.False;
                        switch (item.Bone.allowCollision)
                        {
                            case VRCPhysBoneBase.AdvancedBool.False:
                                collideType = physboneInteractionTypes.False;
                                break;
                            case VRCPhysBoneBase.AdvancedBool.True:
                                collideType = physboneInteractionTypes.True;
                                break;
                            case VRCPhysBoneBase.AdvancedBool.Other:
                                //value is other, need to check the bools
                                if (item.Bone.collisionFilter.allowSelf) collideType = physboneInteractionTypes.SelfOnly;
                                else collideType = physboneInteractionTypes.OthersOnly;
                                break;
                        }
                        
                        collideType = (physboneInteractionTypes)EditorGUI.EnumPopup(rect, collideType);

                        //convert back to advanced bool and set the filter bools
                        switch (collideType)
                        {
                            case physboneInteractionTypes.False:
                                item.Bone.allowCollision = VRCPhysBoneBase.AdvancedBool.False;
                                break;
                            case physboneInteractionTypes.True:
                                item.Bone.allowCollision = VRCPhysBoneBase.AdvancedBool.True;
                                break;
                            case physboneInteractionTypes.SelfOnly:
                                item.Bone.allowCollision = VRCPhysBoneBase.AdvancedBool.Other;
                                item.Bone.collisionFilter.allowSelf = true;
                                item.Bone.collisionFilter.allowOthers = false;
                                break;
                            case physboneInteractionTypes.OthersOnly:
                                item.Bone.allowCollision = VRCPhysBoneBase.AdvancedBool.Other;
                                item.Bone.collisionFilter.allowSelf = false;
                                item.Bone.collisionFilter.allowOthers = true;
                                break;
                        }
                        break;
                    }
                    case 4:
                    {
                        GUI.Box(rect, item.AffectedTransforms.ToString());
                        break;
                    }
                    case 5:
                    {
                        GUI.Box(rect, item.CollisionCheckCount.ToString());
                        break;
                    }
                }
            }
        }
        
        protected override TreeViewItem BuildRoot()
        {
            treeitems = new List<TreeViewItem>();
            Texture2D transformIcon = EditorGUIUtility.IconContent("Transform Icon").image as Texture2D;
            Texture2D physBoneIcon = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/LPD/AvatarReport/Resources/Phys icon.png", typeof(Texture2D));
            int idnum = 1;
            var root = new PhysBoneTreeViewItem()
            {
                id = idnum,
                displayName = "root",
                depth = -1
            };

            foreach (var bone in DynamicsTab.PhysBones)
            {
                idnum++;
                var boneitem = new PhysBoneTreeViewItem()
                {
                    id = idnum,
                    displayName = bone.name,
                    depth = 0,
                    icon = physBoneIcon,
                    Bone = bone,
                    AffectedTransforms = bone.bones.Count,
                    CollisionCheckCount = DynamicsTab.GetPhysboneCollisionCheckCount(bone)
                };

                
                foreach (var child in bone.bones)
                {
                    idnum++;
                    var AffectedTransform = new PhysBoneTreeViewItem()
                    {
                        id = idnum,
                        displayName = child.transform.name,
                        depth = 1,
                        icon = transformIcon,
                        Bone = bone,
                        ChildBone = child
                    };
                    boneitem.AddChild(AffectedTransform);
                }
                treeitems.Add(boneitem);
            }

            SetupParentsAndChildrenFromDepths(root, treeitems);
            return root;
        }
    }
    #endregion
    #region PhysboneColliders
    public class PhysBoneColliderTreeViewItem : TreeViewItem
    {
        public VRCPhysBoneCollider Collider;
    }

    public class PhysBoneColliderTreeView : TreeView
    {
        List<TreeViewItem> treeitems = new List<TreeViewItem>();
        public PhysBoneColliderTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            header.sortingChanged += OnSortingChanged;
        }

        private void OnSortingChanged(MultiColumnHeader multicolumnheader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<PhysBoneColliderTreeViewItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.displayName);
                    break;
                case 1:
                    items = items.OrderBy(x => x.Collider.enabled);
                    break;
                case 2:
                    items = items.OrderBy(x => x.Collider.bonesAsSpheres);
                    break;
                case 3:
                    items = items.OrderBy(x => x.Collider.shapeType);
                    break;
                default: break;
            }

            if (!multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex))
            {
                items = items.Reverse();
            }
            rootItem.children = items.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }
        
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var obj = (PhysBoneColliderTreeViewItem)FindItem(id, rootItem);
            if (obj.Collider != null)
            {
                EditorGUIUtility.PingObject(obj.Collider);
                Selection.activeGameObject = obj.Collider.gameObject;
            }
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            PhysBoneColliderTreeViewItem item = (PhysBoneColliderTreeViewItem)args.item;
            
            if (item.Collider == null)
            {
                GUI.color = Color.red;
                Rect rect = args.GetCellRect(0);
                rect.xMin += 16 + (16f * args.item.depth);
                EditorGUI.LabelField(rect, "Item Missing!");
                GUI.color = Color.white;
                return;
            }

            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                Rect rect = args.GetCellRect(i);
                int columnIndex = args.GetColumn(i);

                switch (columnIndex)
                {
                    case 0:
                    {
                        GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), item.icon, ScaleMode.ScaleToFit);
                        rect.x += 20;
                        rect.width -= 20;
                        GUI.Box(rect, item.displayName);
                        break;
                    }
                    case 1:
                    {
                        rect.x += 15;
                        item.Collider.enabled = EditorGUI.Toggle(rect, item.Collider.enabled);
                        break;
                    }
                    case 2:
                    {
                        rect.x += 15;
                        if (item.Collider.shapeType == VRCPhysBoneColliderBase.ShapeType.Plane)
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUI.Toggle(rect, item.Collider.insideBounds);
                            EditorGUI.EndDisabledGroup();
                            break;
                        }
                        item.Collider.insideBounds = EditorGUI.Toggle(rect, item.Collider.insideBounds);
                        break;
                    }
                    case 3:
                    {
                        rect.x += 15;
                        if (item.Collider.insideBounds)
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUI.Toggle(rect, item.Collider.insideBounds);
                            EditorGUI.EndDisabledGroup();
                            break;
                        }
                        item.Collider.bonesAsSpheres = EditorGUI.Toggle(rect, item.Collider.bonesAsSpheres);
                        break;
                    }
                    case 4:
                    {
                        item.Collider.shapeType = (VRCPhysBoneColliderBase.ShapeType)EditorGUI.EnumPopup(rect, item.Collider.shapeType);
                        break;
                    }
                    case 5:
                    {
                        item.Collider.radius = EditorGUI.FloatField(rect, item.Collider.radius);
                        break;
                    }
                }
            }
        }
        
        protected override TreeViewItem BuildRoot()
        {
            treeitems = new List<TreeViewItem>();
            Texture2D icon = EditorGUIUtility.IconContent("d_RaycastCollider Icon").image as Texture2D;

            int idnum = 1;
            var root = new PhysBoneColliderTreeViewItem()
            {
                id = idnum,
                displayName = "root",
                depth = -1
            };

            foreach (var collider in DynamicsTab.PhysBoneColliders)
            {
                idnum++;
                var colliderItem = new PhysBoneColliderTreeViewItem()
                {
                    id = idnum,
                    displayName = collider.name,
                    depth = 0,
                    icon = icon,
                    Collider = collider,
                };

                treeitems.Add(colliderItem);
            }

            SetupParentsAndChildrenFromDepths(root, treeitems);
            return root;
        }
    }
    #endregion
    #region Contacts
    public class ContactsTreeViewItem : TreeViewItem
    {
        public ContactBase Contact;
        public string ContactType;
    }

    public class ContactsTreeView : TreeView
    {
        List<TreeViewItem> treeitems = new List<TreeViewItem>();
        public ContactsTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            header.sortingChanged += OnSortingChanged;
        }

        private void OnSortingChanged(MultiColumnHeader multicolumnheader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<ContactsTreeViewItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.displayName);
                    break;
                case 1:
                    items = items.OrderBy(x => x.Contact.enabled);
                    break;
                case 2:
                    items = items.OrderBy(x => x.Contact.shapeType);
                    break;
                case 3:
                    items = items.OrderBy(x => x.Contact.radius);
                    break;
                case 4:
                    items = items.OrderBy(x => x.Contact.collisionTags.Count);
                    break;
                default: break;
            }

            if (!multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex))
            {
                items = items.Reverse();
            }
            rootItem.children = items.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }
        
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var obj = (ContactsTreeViewItem)FindItem(id, rootItem);
            if (obj.Contact != null)
            {
                EditorGUIUtility.PingObject(obj.Contact);
                Selection.activeGameObject = obj.Contact.gameObject;
            }
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            ContactsTreeViewItem item = (ContactsTreeViewItem)args.item;
            
            if (item.Contact == null)
            {
                GUI.color = Color.red;
                Rect rect = args.GetCellRect(0);
                rect.xMin += 16 + (16f * args.item.depth);
                EditorGUI.LabelField(rect, "Item Missing!");
                GUI.color = Color.white;
                return;
            }

            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                Rect rect = args.GetCellRect(i);
                int columnIndex = args.GetColumn(i);

                switch (columnIndex)
                {
                    case 0:
                    {
                        GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), item.icon, ScaleMode.ScaleToFit);
                        EditorGUI.LabelField(new Rect(rect.x, rect.y, 20, 20), new GUIContent("", item.ContactType));
                        rect.x += 20;
                        rect.width -= 20;
                        GUI.Box(rect, item.displayName);
                        break;
                    }
                    case 1:
                    {
                        rect.x += 20;
                        item.Contact.enabled = EditorGUI.Toggle(rect, item.Contact.enabled);
                        break;
                    }
                    case 2:
                    {
                        item.Contact.shapeType = (ContactBase.ShapeType)EditorGUI.EnumPopup(rect, item.Contact.shapeType);
                        break;
                    }
                    case 3:
                    {
                        item.Contact.radius = EditorGUI.FloatField(rect, item.Contact.radius);
                        break;
                    }
                    case 4:
                    {
                        rect.x += 15;
                        EditorGUI.LabelField(rect, item.Contact.collisionTags.Count.ToString());
                        break;
                    }
                }
            }
        }
        
        protected override TreeViewItem BuildRoot()
        {
            treeitems = new List<TreeViewItem>();
            Texture2D sendIcon = EditorGUIUtility.IconContent("NetworkAnimator Icon").image as Texture2D;
            Texture2D receiveIcon = EditorGUIUtility.IconContent("Animator Icon").image as Texture2D;

            int idnum = 1;
            var root = new ContactsTreeViewItem()
            {
                id = idnum,
                displayName = "root",
                depth = -1
            };

            foreach (var contact in DynamicsTab.ContactSenders)
            {
                idnum++;
                var contactItem = new ContactsTreeViewItem()
                {
                    id = idnum,
                    displayName = contact.name,
                    depth = 0,
                    icon = sendIcon,
                    Contact = contact,
                    ContactType = "Sender"
                };

                treeitems.Add(contactItem);
            }
            foreach (var contact in DynamicsTab.ContactReceivers)
            {
                idnum++;
                var contactItem = new ContactsTreeViewItem()
                {
                    id = idnum,
                    displayName = contact.name,
                    depth = 0,
                    icon = receiveIcon,
                    Contact = contact,
                    ContactType = "Receiver"
                };

                treeitems.Add(contactItem);
            }

            SetupParentsAndChildrenFromDepths(root, treeitems);
            return root;
        }
    }
    #endregion
}
