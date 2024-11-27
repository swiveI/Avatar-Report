using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;

namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    public class LightsTreeItem : TreeViewItem
    {
        public Light Light;
        public GameObject ObjectRefrence;
    }
    public class LightsTreeview : TreeView
    {
        List<TreeViewItem> treeItems = new List<TreeViewItem>();
        public LightsTreeview(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            showBorder = true;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            LightsTreeItem item = (LightsTreeItem)args.item;
            if (item.Light == null)
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
                int columnIndex = args.GetColumn(i);

                Texture lightTypeIcon;
                switch (item.Light.type)
                {
                    case LightType.Directional:
                        lightTypeIcon = EditorGUIUtility.IconContent("d_DirectionalLight Icon").image;
                        break;
                    case LightType.Point:
                        lightTypeIcon = EditorGUIUtility.IconContent("d_Light Icon").image;
                        break;
                    case LightType.Spot:
                        lightTypeIcon = EditorGUIUtility.IconContent("d_Spotlight Icon").image;
                        break;
                    case LightType.Area:
                        lightTypeIcon = EditorGUIUtility.IconContent("d_AreaLight Icon").image;
                        break;
                    case LightType.Disc:
                        lightTypeIcon = EditorGUIUtility.IconContent("d_AreaLight Icon").image;
                        break;
                    default:
                        lightTypeIcon = EditorGUIUtility.IconContent("d_Light Icon").image;
                        break;
                }
                
                switch (columnIndex)
                {
                    case 0:
                        rect.xMin += 20f * args.item.depth;
                        GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), lightTypeIcon, ScaleMode.ScaleToFit);
                        rect.x += 20;
                        rect.width -= 20;
                        EditorGUI.LabelField(rect, item.displayName);
                        break;
                    case 1:
                        item.Light.type = (LightType)EditorGUI.EnumPopup(rect, item.Light.type);
                        break;
                    case 2:
                        if (item.Light.type == LightType.Area || item.Light.type == LightType.Disc)
                        {
                            EditorGUI.LabelField(rect, item.Light.range.ToString());
                            break;
                        }
                        item.Light.range = EditorGUI.FloatField(rect, item.Light.range);
                        break;
                    case 3:
                        item.Light.intensity = EditorGUI.FloatField(rect, item.Light.intensity);
                        break;
                    case 4:
                        item.Light.shadows = (LightShadows)EditorGUI.EnumPopup(rect, item.Light.shadows);
                        break;
                    case 5:
                        item.Light.color = EditorGUI.ColorField(rect, item.Light.color);
                        break;
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            treeItems.Clear();
            
            int idnum = 1;
            var root = new TreeViewItem
            {
                id = idnum, 
                depth = -1, 
                displayName = "Root"
            };
            idnum++;

            foreach (Light light in LightsTab.Lights)
            {
                LightsTreeItem item = new LightsTreeItem
                {
                    id = idnum,
                    depth = 0,
                    displayName = light.name,
                    Light = light,
                    ObjectRefrence = light.gameObject
                };
                
                treeItems.Add(item);
                idnum++;
            }

            SetupParentsAndChildrenFromDepths(root, treeItems);
            return root;
        }
        
        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var obj = (LightsTreeItem)FindItem(id, rootItem);
            if (obj.ObjectRefrence != null)
            {
                EditorGUIUtility.PingObject(obj.ObjectRefrence);
                Selection.activeGameObject = obj.ObjectRefrence;
            }
        }
    }
}