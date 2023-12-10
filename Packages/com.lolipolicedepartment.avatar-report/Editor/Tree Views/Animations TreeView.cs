using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class AnimationsTreeItem : TreeViewItem
    {
        public Animator AnimatorComponent;
        public AnimationClip clip;

        public string name = "";
        public int layerCount = 0;
        public int stateCount = 0;
        public int anystateTransitionCount = 0;
        public int stateBehaviourCount = 0;
        public int parameterCount = 0;
    }
    public class AnimationsTreeView : TreeView
    {
        private List<TreeViewItem> treeitems;

        public AnimationsTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            header.sortingChanged += OnSortingChanged;
        }

        private void OnSortingChanged(MultiColumnHeader multicolumnheader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<AnimationsTreeItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.name);
                    break;
                case 1:
                    items = items.OrderBy(x => x.layerCount);
                    break;
                case 2:
                    items = items.OrderBy(x => x.stateCount);
                    break;
                case 3:
                    items = items.OrderBy(x => x.anystateTransitionCount);
                    break;
                case 4:
                    items = items.OrderBy(x => x.stateBehaviourCount);
                    break;
                case 5:
                    items = items.OrderBy(x => x.parameterCount);
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
            var obj = (AnimationsTreeItem)FindItem(id, rootItem);
            if (obj.depth == 0)
            {
                if (obj.AnimatorComponent != null)
                {
                    EditorGUIUtility.PingObject(obj.AnimatorComponent);
                    Selection.activeGameObject = obj.AnimatorComponent.gameObject;
                }

                return;
            }

            if (obj.depth == 1)
            {
                string path = AssetDatabase.GetAssetPath(obj.clip);
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(path));
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            AnimationsTreeItem item = (AnimationsTreeItem)args.item;
            
            if (item.AnimatorComponent == null)
            {
                GUI.color = Color.red;
                Rect rect = args.GetCellRect(0);
                rect.xMin += 20 + (20f * args.item.depth);
                EditorGUI.LabelField(rect, "Item Missing!");
                GUI.color = Color.white;
                return;
            }

            if (args.item.depth == 1)
            {
                Rect rect = args.GetCellRect(0);
                rect.xMin += 32f;
                rect.width = Screen.width - 32f;
                GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), AssetPreview.GetMiniTypeThumbnail(typeof(AnimationClip)), ScaleMode.ScaleToFit);
                rect.x += 20;
                rect.width -= 20;
                GUI.Label(rect, item.name);
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
                        rect.xMin += 20f * args.item.depth;
                        GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), AssetPreview.GetMiniTypeThumbnail(typeof(Animator)), ScaleMode.ScaleToFit);
                        rect.x += 20;
                        rect.width -= 20;
                        GUI.Box(rect, item.name);
                        break;
                    }
                    case 1:
                    {
                        GUI.Box(rect, item.layerCount.ToString());
                        break;
                    }
                    case 2:
                    {
                        GUI.Box(rect, item.stateCount.ToString());
                        break;
                    }
                    case 3:
                    {
                        GUI.Box(rect, item.stateBehaviourCount.ToString());
                        break;
                    }
                    case 4:
                    {
                        GUI.Box(rect, item.anystateTransitionCount.ToString());
                        break;
                    }
                    case 5:
                    {
                        GUI.Box(rect, item.parameterCount.ToString());
                        break;
                    }
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            treeitems = new List<TreeViewItem>();
            
            int idnum = 1;
            var root = new MeshTreeItem()
            {
                id = idnum,
                displayName = "root",
                depth = -1
            };

            for (int i = 0; i < AnimatorsTab.Animators.Count; i++)
            {
                idnum++;
                var item = new AnimationsTreeItem
                {
                    id = idnum,
                    displayName = AnimatorsTab.Animators[i].AnimatorComponent.name,
                    depth = 0,
                    
                    AnimatorComponent = AnimatorsTab.Animators[i].AnimatorComponent,
                    name = AnimatorsTab.Animators[i].AnimatorComponent.name,
                    layerCount = AnimatorsTab.Animators[i].LayerCount,
                    stateCount = AnimatorsTab.Animators[i].StateCount,
                    anystateTransitionCount = AnimatorsTab.Animators[i].AnyStateTransitionCount,
                    stateBehaviourCount = AnimatorsTab.Animators[i].StateBehaviourCount,
                    parameterCount = AnimatorsTab.Animators[i].ParameterCount,
                };

                
                foreach (AnimationClip clip in AnimatorsTab.Animators[i].AnimationClips)
                {
                    idnum++;
                    var clipitem = new AnimationsTreeItem
                    {
                        id = idnum,
                        displayName = clip.name,
                        depth = 1,
                        
                        AnimatorComponent = AnimatorsTab.Animators[i].AnimatorComponent,
                        clip = clip,
                        name = clip.name,
                    };
                    item.AddChild(clipitem);
                }
                
                treeitems.Add(item);
            }
            
            SetupParentsAndChildrenFromDepths(root, treeitems);
            return root;
        }
    }
}
