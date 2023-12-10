
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class MeshTreeItem : TreeViewItem
    {
        //data to display
        public string Name;
        public int TriCount;
        public string Type;
        public int BlendShapes;
        public int MaterialSlots;
        public long VramSize;


        ///for internal use
        public GameObject ObjectReference;
        public Texture Icon;
        public bool Exists;
    }
    
    public class MeshesTreeview : TreeView
    {
        private List<TreeViewItem> treeItems;
        
        private enum column
        {
            name,
            tricount,
            type,
            blendshapes,
            materialslots,
            vramsize
        }
        
        public MeshesTreeview(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            header.sortingChanged += OnSortingChanged;
        }

        private void OnSortingChanged(MultiColumnHeader multicolumnheader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<MeshTreeItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.Name);
                    break;
                case 1:
                    items = items.OrderBy(x => x.TriCount);
                    break;
                case 2:
                    items = items.OrderBy(x => x.Type);
                    break;
                case 3:
                    items = items.OrderBy(x => x.BlendShapes);
                    break;
                case 4:
                    items = items.OrderBy(x => x.MaterialSlots);
                    break;
                case 5:
                    items = items.OrderBy(x => x.VramSize);
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
            var obj = (MeshTreeItem)FindItem(id, rootItem);
            if (obj.ObjectReference != null)
            {
                EditorGUIUtility.PingObject(obj.ObjectReference);
                Selection.activeGameObject = obj.ObjectReference;
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            MeshTreeItem item = (MeshTreeItem)args.item;
            
            if (item.ObjectReference == null)
            {
                GUI.color = Color.red;
                Rect rect = args.GetCellRect(0);
                EditorGUI.LabelField(rect, "Item Missing!");
                GUI.color = Color.white;
                return;
            }

            for (int visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                Rect rect = args.GetCellRect(visibleColumnIndex);
                column column = (column)args.GetColumn(visibleColumnIndex);

                switch (column)
                {
                    case column.name:
                    {

                        rect.xMin += 20f * args.item.depth;
                        GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), item.Icon, ScaleMode.ScaleToFit);
                        rect.x += 20;
                        rect.width -= 20;
                        GUI.Box(rect, (new GUIContent(item.Name, item.Exists? item.ObjectReference.name : "Mesh does not exist")));
                        break;
                    }
                    case column.tricount:
                    {
                        GUI.Box(rect, item.TriCount.ToString());
                        break;
                    }
                    case column.type:
                    {
                        GUI.Box(rect, item.Type);
                        break;
                    }
                    case column.blendshapes:
                    {
                        GUI.Box(rect, item.BlendShapes.ToString());
                        break;
                    }
                    case column.materialslots:
                    {
                        GUI.Box(rect, item.MaterialSlots.ToString());
                        break;
                    }
                    case column.vramsize:
                    {
                        GUI.Box(rect, AvatarBuildReportUtility.FormatSize(item.VramSize));
                        break;
                    }
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            treeItems = new List<TreeViewItem>();

            int idnum = 1;
            var root = new MeshTreeItem()
            {
                id = idnum,
                displayName = "root",
                depth = -1
            };
            
            for (int i = 0; i < MeshesTab.MeshAssets.Count; i++)
            {
                idnum++;
                var item = new MeshTreeItem()
                {
                    id = idnum,
                    displayName = MeshesTab.MeshAssets[i].Name,
                    depth = 0,
                    
                    Name = MeshesTab.MeshAssets[i].Name,
                    TriCount = MeshesTab.MeshAssets[i].TriCount,
                    Type = MeshesTab.MeshAssets[i].Type,
                    BlendShapes = MeshesTab.MeshAssets[i].BlendShapes,
                    MaterialSlots = MeshesTab.MeshAssets[i].MaterialSlots,
                    VramSize = MeshesTab.MeshAssets[i].VramSize,
                    Icon = MeshesTab.MeshAssets[i].Icon,
                    ObjectReference = MeshesTab.MeshAssets[i].ObjectReference,
                    Exists = MeshesTab.MeshAssets[i].Exists
                };
                treeItems.Add(item);
            }

            SetupParentsAndChildrenFromDepths(root, treeItems);
            return root;
        }
    }
}