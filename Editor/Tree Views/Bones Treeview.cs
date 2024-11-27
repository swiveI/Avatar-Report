using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    public class BoneTreeItem : TreeViewItem
    {
        public GameObject objectRefrence;
        public BoneInfo boneInfo;
        public BoneTreeItem(GameObject obj)
        {
            objectRefrence = obj;
        }
    }
    public class BoneTree : TreeView
    {
        List<TreeViewItem> boneTreeItems;
        public Texture2D LeafBone;
        public Texture2D PhysBone;
        public Texture2D WeightBone;

        //add bone tree item 
        public BoneTree(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            showBorder = true;
            header.sortingChanged += OnSortingChanged;
            LeafBone = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Resources/Leaf icon.png", typeof(Texture2D));
            PhysBone = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Resources/Phys icon.png", typeof(Texture2D));
            WeightBone = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Resources/Weight icon.png", typeof(Texture2D));
            Reload();
        }
        protected override void RowGUI(RowGUIArgs args)
        {
            BoneTreeItem item = (BoneTreeItem)args.item;
            
            if (item.objectRefrence == null)
            {
                GUI.color = Color.red;
                Rect rect = args.GetCellRect(0);
                EditorGUI.LabelField(rect, "Item Missing!");
                GUI.color = Color.white;
                return;
            }
            
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                Rect rect = args.GetCellRect(i);
                Rect texRect = new Rect(rect.x, rect.y, rect.height, rect.height);
                int columnIndex = args.GetColumn(i);

                switch (columnIndex)
                {
                    case 0:
                        EditorGUI.LabelField(rect,item.displayName);
                        break;
                    case 1:
                        if (!item.boneInfo.hasWeight)
                        {
                            GUI.Box(texRect, new GUIContent(WeightBone, "This bone has no weights, it wont affect the mesh"));
                        }
                        break;
                    case 2:
                        if (item.boneInfo.isPhysbone)
                        {
                            GUI.Box(texRect, new GUIContent(PhysBone, "This bone has a physbone component"));
                        }
                        break;
                    case 3:
                        if (item.boneInfo.isLeaf)
                        {
                            GUI.Box(texRect, new GUIContent(LeafBone, "This bone looks like a leafbone, if it has no weight it can probably be safely deleted"));
                        }
                        break;
                }
            }
        }
        protected override TreeViewItem BuildRoot()
        {
            int idnum = 1;
            var root = new BoneTreeItem(null)
            {
                id = idnum,
                displayName = "Root",
                depth = -1
            };
            idnum++;

            boneTreeItems = new List<TreeViewItem>();

            for (int i = 0; i < BonesTab.Bones.Count; i++)
            {
                BoneTreeItem item = new BoneTreeItem(BonesTab.Bones[i].bone.gameObject)
                {
                    id = idnum,
                    displayName = BonesTab.Bones[i].bone.name,
                    depth = 0,
                    boneInfo = BonesTab.Bones[i],
                };
                boneTreeItems.Add(item);
                idnum++;
            }
            SetupParentsAndChildrenFromDepths(root, boneTreeItems);
            return root;
        }
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var obj = (BoneTreeItem)FindItem(id, rootItem);
            if (obj.objectRefrence != null)
            {
                EditorGUIUtility.PingObject(obj.objectRefrence);
                Selection.activeGameObject = obj.objectRefrence;
            }
        }
        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<BoneTreeItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.displayName);
                    break;
                case 1:
                    items = items.OrderBy(x => x.boneInfo.hasWeight);
                    break;
                case 2:
                    items = items.OrderBy(x => x.boneInfo.isPhysbone);
                    break;
                case 3:
                    items = items.OrderBy(x => x.boneInfo.isLeaf);
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
    }
}