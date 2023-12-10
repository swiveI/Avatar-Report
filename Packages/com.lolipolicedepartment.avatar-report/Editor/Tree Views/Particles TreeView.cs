using System.Collections.Generic;
using System.Linq;
using LoliPoliceDepartment.Utilities.AvatarReport;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace  LoliPoliceDepartment.Utilities
{
    public class ParticleTreeItem : TreeViewItem
    {
        //internal
        public GameObject ObjectReference;
        public ParticleSystem.MainModule SystemMain;
        public ParticleSystem.CollisionModule SystemCollision;
        public ParticleSystem.TrailModule SystemTrails;
        public ParticleSystemRenderer RendererReference;
    }
    
    public class ParticlesTreeView : TreeView
    {
        private List<TreeViewItem> treeItems = new List<TreeViewItem>();
        
        private enum column
        {
            Name,
            ParticleCount,
            ParticleType,
            LoopEnabled,
            CollisionEnabled,
            TrailsEnabled,
            Material,
        }

        public ParticlesTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            header.sortingChanged += OnSortingChanged;
        }

        private void OnSortingChanged(MultiColumnHeader multicolumnheader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<ParticleTreeItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.displayName);
                    break;
                case 1:
                    items = items.OrderBy(x => x.SystemMain.maxParticles);
                    break;
                case 2:
                    items = items.OrderBy(x => x.RendererReference.renderMode);
                    break;
                case 3:
                    items = items.OrderBy(x => x.SystemMain.loop);
                    break;
                case 4:
                    items = items.OrderBy(x => x.SystemCollision.enabled);
                    break;
                case 5:
                    items = items.OrderBy(x => x.SystemTrails.enabled);
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
            var obj = (ParticleTreeItem)FindItem(id, rootItem); 
            if (obj.ObjectReference != null)
            {
                EditorGUIUtility.PingObject(obj.ObjectReference);
                Selection.activeGameObject = obj.ObjectReference;
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            ParticleTreeItem item = (ParticleTreeItem)args.item;
            
            if (item.RendererReference == null)
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
                    case column.Name:
                    {
                        
                        GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), item.icon, ScaleMode.ScaleToFit);
                        rect.x += 20;
                        rect.width -= 20;
                        GUI.Box(rect, new GUIContent(item.displayName));
                        break;
                    }
                    case column.ParticleCount:
                    {
                        item.SystemMain.maxParticles = EditorGUI.IntField(rect, item.SystemMain.maxParticles);
                        break;
                    }
                    case column.ParticleType:
                    {
                        item.RendererReference.renderMode = (ParticleSystemRenderMode)EditorGUI.EnumPopup(rect, item.RendererReference.renderMode);
                        break;
                    }
                    case column.LoopEnabled:
                    {
                        rect.x += 10;
                        item.SystemMain.loop = EditorGUI.Toggle(rect, item.SystemMain.loop);
                        break;
                    }
                    case column.CollisionEnabled:
                    {
                        rect.x += 10;
                        item.SystemCollision.enabled = EditorGUI.Toggle(rect, item.SystemCollision.enabled);
                        break;
                    }
                    case column.TrailsEnabled:
                    {
                        rect.x += 10;
                        item.SystemTrails.enabled = EditorGUI.Toggle(rect, item.SystemTrails.enabled);
                        break;
                    }
                    case column.Material:
                    {
                        item.RendererReference.sharedMaterial = (Material)EditorGUI.ObjectField(rect, item.RendererReference.sharedMaterial, typeof(Material), false);
                        break;
                    }
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            treeItems.Clear();
            Texture2D icon = AssetPreview.GetMiniTypeThumbnail(typeof(ParticleSystem));
            
            int idnum = 1;
            var root = new ParticleTreeItem()
            {
                id = idnum,
                displayName = "root",
                depth = -1
            };

            for (int i = 0; i < ParticlesTab.ParticleAssets.Count; i++)
            {
                idnum++;
                var item = new ParticleTreeItem()
                {
                    id = idnum,
                    displayName = ParticlesTab.ParticleAssets[i].name,
                    depth = 0,
                    icon = icon,
                    
                    ObjectReference = ParticlesTab.ParticleAssets[i].ObjectReference,
                    SystemMain = ParticlesTab.ParticleAssets[i].SystemMain,
                    SystemCollision = ParticlesTab.ParticleAssets[i].SystemCollision,
                    SystemTrails = ParticlesTab.ParticleAssets[i].SystemTrails,
                    RendererReference = ParticlesTab.ParticleAssets[i].RendererReference,
                };
                treeItems.Add(item);
            }

            SetupParentsAndChildrenFromDepths(root, treeItems);
            return root;
        }
    }
}
