using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    internal class FileTreeItem : TreeViewItem
    {
        public string name;
        public int size;
        public float percentage;
        public string type;
        public string path;
        public bool exists;
    }
    
    public class FilesTreeview : TreeView
    {
        private List<TreeViewItem> treeItems;
        private enum Column
        {
            Name,
            Type,
            Size,
            Percentage,
        }
        public FilesTreeview(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            header.sortingChanged += OnSortingChanged;
        }
        
        protected override void RowGUI(RowGUIArgs args)
        {
            FileTreeItem item = (FileTreeItem)args.item;

            for (int visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                Rect rect = args.GetCellRect(visibleColumnIndex);
                Column column = (Column)args.GetColumn(visibleColumnIndex);

                switch (column)
                {
                    case Column.Name:
                        {
                            rect.xMin += 20f * args.item.depth;
                            GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20),item.exists? item.icon : EditorGUIUtility.IconContent("d_winbtn_mac_close_a").image, ScaleMode.ScaleToFit);
                            
                            rect.x += 20;
                            rect.width -= 20;
                            GUI.Box(rect, (new GUIContent(item.name, item.exists? item.path : "File has been moved since this build, or no longer exists")));
                            break;
                        }
                    case Column.Size:
                        {
                            GUI.Box(rect, (AvatarBuildReportUtility.FormatSize(item.size)));
                            break;
                        }
                    case Column.Percentage:
                        {
                            GUI.Box(rect, (item.percentage.ToString("#0.0") + "%"));
                            break;
                        }
                    case Column.Type:
                        {
                            GUI.Box(rect, (item.type));
                            break;
                        }
                }
            }
        }
        
        protected override TreeViewItem BuildRoot()
        {
            treeItems = new List<TreeViewItem>();
            
            int idnum = 1;
            var root = new FileTreeItem()
            {
                id = idnum,
                displayName = "root",
                depth = -1
            };
            idnum++;

            for (int i = 0; i < FilesTab.buildAssets.Count; i++)
            {
                FileTreeItem item = new FileTreeItem()
                {
                    name = FilesTab.buildAssets[i].name,
                    size = FilesTab.buildAssets[i].size,
                    percentage = FilesTab.buildAssets[i].percentage,
                    type = FilesTab.buildAssets[i].type,
                    icon = FilesTab.buildAssets[i].icon as Texture2D,
                    path = FilesTab.buildAssets[i].path,
                    id = idnum,
                    depth = 0,
                    exists = FilesTab.buildAssets[i].exists
                };
                treeItems.Add(item);
                idnum++;
            }
            
            SetupParentsAndChildrenFromDepths(root, treeItems);
            return root;
        }
        
        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<FileTreeItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.name);
                    break;
                case 1:
                    items = items.OrderBy(x => x.type);
                    break;
                case 2:
                    items = items.OrderBy(x => x.size);
                    break;
                case 3:
                    items = items.OrderBy(x => x.percentage);
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
            FileTreeItem item = (FileTreeItem)FindItem(id, rootItem);
            if (item != null)
            {
                if (File.Exists(item.path))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(item.path));
                }
            }
        }
    }
}
