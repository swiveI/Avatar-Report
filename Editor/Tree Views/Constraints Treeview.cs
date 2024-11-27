using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using Debug = System.Diagnostics.Debug;

namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    public enum ConstraintType
    {
        Aim,
        Parent,
        Position,
        Rotation,
        Scale,
        LookAt,
        Source
    }
    public class ConstraintsTreeItem : TreeViewItem
    {
         public GameObject ObjectRefrence;
         public ConstraintType Type;
         public Behaviour Constraint;
         public Texture Icon;
         public int Index = - 1;
         public ConstraintsTreeItem(GameObject obj)
        {
            ObjectRefrence = obj;
        }
    }

    public class ConstraintTree : TreeView
    {
        List<TreeViewItem> Constraints;
        Texture transformIcon = EditorGUIUtility.IconContent("d_Transform Icon").image;

        public ConstraintTree(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
            showBorder = true;
            header.sortingChanged += OnSortingChanged;
        }
        
        private void OnSortingChanged(MultiColumnHeader multicolumnheader)
        {
            // This is just code from 1's VR World Toolkit, Go say thanks
            var items = rootItem.children.Cast<ConstraintsTreeItem>();
            switch (multiColumnHeader.sortedColumnIndex)
            {
                case 0:
                    items = items.OrderBy(x => x.displayName);
                    break;
                case 1:
                    items = items.OrderBy(x => x.Type);
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
            var obj = (ConstraintsTreeItem) FindItem(id, rootItem);
            if (obj.ObjectRefrence != null)
            {
                EditorGUIUtility.PingObject(obj.ObjectRefrence);
                Selection.activeGameObject = obj.ObjectRefrence;
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            ConstraintsTreeItem item = (ConstraintsTreeItem)args.item;
            
            if (item.ObjectRefrence == null)
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
                rect.xMin += 32;
                rect.width = (Screen.width / 2) - 32f;
                GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), item.Icon, ScaleMode.ScaleToFit);
                rect.x += 20;
                rect.width -= 20;
                GUI.Label(rect, item.displayName);
                
                rect.x += rect.width;
                rect.width = Screen.width / 2 - 20f;
                ConstraintsTreeItem parent = (ConstraintsTreeItem)args.item.parent;
                switch (parent.Type)
                {
                    case ConstraintType.Aim:
                        AimConstraint aimCon = (AimConstraint)parent.Constraint;
                        if (aimCon != null)
                        {
                            ConstraintSource source = aimCon.GetSource(item.Index);
                            source.weight = EditorGUI.Slider(rect, source.weight, 0f, 1f);
                            aimCon.SetSource(item.Index, source);
                        }
                        return;
                    case ConstraintType.Parent:
                        ParentConstraint parCon = (ParentConstraint)parent.Constraint;
                        if (parCon != null)
                        {
                            ConstraintSource source = parCon.GetSource(item.Index);
                            source.weight = EditorGUI.Slider(rect, source.weight, 0f, 1f);
                            parCon.SetSource(item.Index, source);
                        }
                        return;
                    case ConstraintType.Position:
                        PositionConstraint posCon = (PositionConstraint)parent.Constraint;
                        if (posCon != null)
                        {
                            ConstraintSource source = posCon.GetSource(item.Index);
                            source.weight = EditorGUI.Slider(rect, source.weight, 0f, 1f);
                            posCon.SetSource(item.Index, source);
                        }
                        return;
                    case ConstraintType.Rotation:
                        RotationConstraint rotCon = (RotationConstraint)parent.Constraint;
                        if (rotCon != null)
                        {
                            ConstraintSource source = rotCon.GetSource(item.Index);
                            source.weight = EditorGUI.Slider(rect, source.weight, 0f, 1f);
                            rotCon.SetSource(item.Index, source);
                        }
                        return;
                    case ConstraintType.Scale:
                        ScaleConstraint scaleCon = (ScaleConstraint)parent.Constraint;
                        if (scaleCon != null)
                        {
                            ConstraintSource source = scaleCon.GetSource(item.Index);
                            source.weight = EditorGUI.Slider(rect, source.weight, 0f, 1f);
                            scaleCon.SetSource(item.Index, source);
                        }
                        return;
                    case ConstraintType.LookAt:
                        LookAtConstraint lookCon = (LookAtConstraint)parent.Constraint;
                        if (lookCon != null)
                        {
                            ConstraintSource source = lookCon.GetSource(item.Index);
                            source.weight = EditorGUI.Slider(rect, source.weight, 0f, 1f);
                            lookCon.SetSource(item.Index, source);
                        }
                        return;
                }
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
                        GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), item.icon, ScaleMode.ScaleToFit);
                        rect.x += 20;
                        rect.width -= 20;
                        GUI.Box(rect, item.displayName);
                        break;
                    }
                    case 1:
                    {
                        GUI.Box(rect, item.Type.ToString());
                        break;
                    }
                    case 2:
                    {
                        rect.x += 15;
                        switch (item.Type)
                        {
                            case ConstraintType.Aim:
                                AimConstraint aimCon = (AimConstraint)item.Constraint;
                                if (aimCon == null) break;
                                aimCon.constraintActive = EditorGUI.Toggle(rect, aimCon.constraintActive);
                                break;
                            case ConstraintType.Parent:
                                ParentConstraint parCon = (ParentConstraint)item.Constraint;
                                if (parCon == null) break;
                                parCon.constraintActive = EditorGUI.Toggle(rect, parCon.constraintActive);
                                break;
                            case ConstraintType.Position:
                                PositionConstraint posCon = (PositionConstraint)item.Constraint;
                                if (posCon == null) break;
                                posCon.constraintActive = EditorGUI.Toggle(rect, posCon.constraintActive);
                                break;
                            case ConstraintType.Rotation:
                                RotationConstraint rotCon = (RotationConstraint)item.Constraint;
                                if (rotCon == null) break;
                                rotCon.constraintActive = EditorGUI.Toggle(rect, rotCon.constraintActive);
                                break;
                            case ConstraintType.Scale:
                                ScaleConstraint scaleCon = (ScaleConstraint)item.Constraint;
                                if (scaleCon == null) break;
                                scaleCon.constraintActive = EditorGUI.Toggle(rect, scaleCon.constraintActive);
                                break;
                            case ConstraintType.LookAt:
                                LookAtConstraint lookCon = (LookAtConstraint)item.Constraint;
                                if (lookCon == null) break;
                                lookCon.constraintActive = EditorGUI.Toggle(rect, lookCon.constraintActive);
                                break;
                        }
                        break;
                    }
                    case 3:
                    {
                        rect.x += 15;
                        switch (item.Type)
                        {
                            case ConstraintType.Aim:
                                AimConstraint aimCon = (AimConstraint)item.Constraint;
                                if (aimCon == null) break;
                                aimCon.locked = EditorGUI.Toggle(rect, aimCon.locked);
                                break;
                            case ConstraintType.Parent:
                                ParentConstraint parCon = (ParentConstraint)item.Constraint;
                                if (parCon == null) break;
                                parCon.locked = EditorGUI.Toggle(rect, parCon.locked);
                                break;
                            case ConstraintType.Position:
                                PositionConstraint posCon = (PositionConstraint)item.Constraint;
                                if (posCon == null) break;
                                posCon.locked = EditorGUI.Toggle(rect, posCon.locked);
                                break;
                            case ConstraintType.Rotation:
                                RotationConstraint rotCon = (RotationConstraint)item.Constraint;
                                if (rotCon == null) break;
                                rotCon.locked = EditorGUI.Toggle(rect, rotCon.locked);
                                break;
                            case ConstraintType.Scale:
                                ScaleConstraint scaleCon = (ScaleConstraint)item.Constraint;
                                if (scaleCon == null) break;
                                scaleCon.locked = EditorGUI.Toggle(rect, scaleCon.locked);
                                break;
                            case ConstraintType.LookAt:
                                LookAtConstraint lookCon = (LookAtConstraint)item.Constraint;
                                if (lookCon == null) break;
                                lookCon.locked = EditorGUI.Toggle(rect, lookCon.locked);
                                break;
                        }
                        break;
                    }
                    case 4:
                    {
                        switch (item.Type)
                        {
                            case ConstraintType.Aim:
                                AimConstraint aimCon = (AimConstraint)item.Constraint;
                                if (aimCon == null) break;
                                aimCon.weight = EditorGUI.Slider(rect, aimCon.weight, 0, 1);
                                break;
                            case ConstraintType.Parent:
                                ParentConstraint parCon = (ParentConstraint)item.Constraint;
                                if (parCon == null) break;
                                parCon.weight = EditorGUI.Slider(rect, parCon.weight, 0, 1);
                                break;
                            case ConstraintType.Position:
                                PositionConstraint posCon = (PositionConstraint)item.Constraint;
                                if (posCon == null) break;
                                posCon.weight = EditorGUI.Slider(rect, posCon.weight, 0, 1);
                                break;
                            case ConstraintType.Rotation:
                                RotationConstraint rotCon = (RotationConstraint)item.Constraint;
                                if (rotCon == null) break;
                                rotCon.weight = EditorGUI.Slider(rect, rotCon.weight, 0, 1);
                                break;
                            case ConstraintType.Scale:
                                ScaleConstraint scaleCon = (ScaleConstraint)item.Constraint;
                                if (scaleCon == null) break;
                                scaleCon.weight = EditorGUI.Slider(rect, scaleCon.weight, 0, 1);
                                break;
                            case ConstraintType.LookAt:
                                LookAtConstraint lookCon = (LookAtConstraint)item.Constraint;
                                if (lookCon == null) break;
                                lookCon.weight = EditorGUI.Slider(rect, lookCon.weight, 0, 1);
                                break;
                        }
                        break;
                    }
                }
            }
            
        }

        protected override TreeViewItem BuildRoot()
        {
            int idnum = 1;
            Constraints = new List<TreeViewItem>();
            
            var root = new ConstraintsTreeItem(null)
            {
                id = idnum,
                displayName = "Root",
                depth = -1
            };

            //aim constraints
            for (int i = 0; i < ConstraintsTab.AvatarConstraintsAim.Count; i++)
            {
                idnum++;
                ConstraintsTreeItem item = new ConstraintsTreeItem(ConstraintsTab.AvatarConstraintsAim[i].gameObject)
                {
                    displayName = ConstraintsTab.AvatarConstraintsAim[i].name,
                    id = idnum,
                    depth = 0,
                    icon = AssetPreview.GetMiniTypeThumbnail(typeof(AimConstraint)),
                    Type = ConstraintType.Aim,
                    Constraint = ConstraintsTab.AvatarConstraintsAim[i]
                };

                for (int j = 0; j < ConstraintsTab.AvatarConstraintsAim[i].sourceCount; j++)
                {
                    idnum++;
                    ConstraintSource source = ConstraintsTab.AvatarConstraintsAim[i].GetSource(j);
                    if (source.sourceTransform == null) continue;
                    GameObject obj = source.sourceTransform.gameObject;
                    
                    ConstraintsTreeItem child = new ConstraintsTreeItem(obj)
                    {
                        displayName = obj.name,
                        id = idnum,
                        depth = 1,
                        Icon = transformIcon,
                        Type = ConstraintType.Source,
                        Index = j
                    };
                    item.AddChild(child);
                }
                Constraints.Add(item);
            }
            
            //look at constraints
            for (int i = 0; i < ConstraintsTab.AvatarConstraintsLookAt.Count; i++)
            {
                idnum++;
                ConstraintsTreeItem item = new ConstraintsTreeItem(ConstraintsTab.AvatarConstraintsLookAt[i].gameObject)
                {
                    displayName = ConstraintsTab.AvatarConstraintsLookAt[i].name,
                    id = idnum,
                    depth = 0,
                    icon = AssetPreview.GetMiniTypeThumbnail(typeof(LookAtConstraint)),
                    Type = ConstraintType.LookAt,
                    Constraint = ConstraintsTab.AvatarConstraintsLookAt[i]
                };

                for (int j = 0; j < ConstraintsTab.AvatarConstraintsLookAt[i].sourceCount; j++)
                {
                    idnum++;
                    ConstraintSource source = ConstraintsTab.AvatarConstraintsLookAt[i].GetSource(j);
                    if (source.sourceTransform == null) continue;
                    GameObject obj = source.sourceTransform.gameObject;
                    
                    ConstraintsTreeItem child = new ConstraintsTreeItem(obj)
                    {
                        displayName = obj.name,
                        id = idnum,
                        depth = 1,
                        Icon = transformIcon,
                        Type = ConstraintType.Source,
                        Index = j
                    };
                    item.AddChild(child);
                }
                Constraints.Add(item);
            }

            //parent constraints
            for (int i = 0; i < ConstraintsTab.AvatarConstraintsParent.Count; i++)
            {
                idnum++;
                ConstraintsTreeItem item = new ConstraintsTreeItem(ConstraintsTab.AvatarConstraintsParent[i].gameObject)
                {
                    displayName = ConstraintsTab.AvatarConstraintsParent[i].name,
                    id = idnum,
                    depth = 0,
                    icon = AssetPreview.GetMiniTypeThumbnail(typeof(ParentConstraint)),
                    Type = ConstraintType.Parent,
                    Constraint = ConstraintsTab.AvatarConstraintsParent[i]
                };

                for (int j = 0; j < ConstraintsTab.AvatarConstraintsParent[i].sourceCount; j++)
                {
                    idnum++;
                    ConstraintSource source = ConstraintsTab.AvatarConstraintsParent[i].GetSource(j);
                    if (source.sourceTransform == null) continue;
                    GameObject obj = source.sourceTransform.gameObject;
                    
                    ConstraintsTreeItem child = new ConstraintsTreeItem(obj)
                    {
                        displayName = obj.name,
                        id = idnum,
                        depth = 1,
                        Icon = transformIcon,
                        Type = ConstraintType.Source,
                        Index = j
                    };
                    item.AddChild(child);
                }
                Constraints.Add(item);
            }

            //position constraints
            for (int i = 0; i < ConstraintsTab.AvatarConstraintsPosition.Count; i++)
            {
                idnum++;
                ConstraintsTreeItem item = new ConstraintsTreeItem(ConstraintsTab.AvatarConstraintsPosition[i].gameObject)
                {
                    displayName = ConstraintsTab.AvatarConstraintsPosition[i].name,
                    id = idnum,
                    depth = 0,
                    icon = AssetPreview.GetMiniTypeThumbnail(typeof(PositionConstraint)),
                    Type = ConstraintType.Position,
                    Constraint = ConstraintsTab.AvatarConstraintsPosition[i]
                };

                for (int j = 0; j < ConstraintsTab.AvatarConstraintsPosition[i].sourceCount; j++)
                {
                    idnum++;
                    ConstraintSource source = ConstraintsTab.AvatarConstraintsPosition[i].GetSource(j);
                    if (source.sourceTransform == null) continue;
                    GameObject obj = source.sourceTransform.gameObject;
                    
                    ConstraintsTreeItem child = new ConstraintsTreeItem(obj)
                    {
                        displayName = obj.name,
                        id = idnum,
                        depth = 1,
                        Icon = transformIcon,
                        Type = ConstraintType.Source,
                        Index = j
                    };
                    item.AddChild(child);
                }
                Constraints.Add(item);
            }

            //rotation constraints
            for (int i = 0; i < ConstraintsTab.AvatarConstraintsRotation.Count; i++)
            {
                idnum++;
                ConstraintsTreeItem item = new ConstraintsTreeItem(ConstraintsTab.AvatarConstraintsRotation[i].gameObject)
                {
                    displayName = ConstraintsTab.AvatarConstraintsRotation[i].name,
                    id = idnum,
                    depth = 0,
                    icon = AssetPreview.GetMiniTypeThumbnail(typeof(RotationConstraint)),
                    Type = ConstraintType.Rotation,
                    Constraint = ConstraintsTab.AvatarConstraintsRotation[i]
                };

                for (int j = 0; j < ConstraintsTab.AvatarConstraintsRotation[i].sourceCount; j++)
                {
                    idnum++;
                    ConstraintSource source = ConstraintsTab.AvatarConstraintsRotation[i].GetSource(j);
                    if (source.sourceTransform == null) continue;
                    GameObject obj = source.sourceTransform.gameObject;
                    
                    ConstraintsTreeItem child = new ConstraintsTreeItem(obj)
                    {
                        displayName = obj.name,
                        id = idnum,
                        depth = 1,
                        Icon = transformIcon,
                        Type = ConstraintType.Source,
                        Index = j
                    };
                    item.AddChild(child);
                }
                Constraints.Add(item);
            }

            //scale constraints
            for (int i = 0; i < ConstraintsTab.AvatarConstraintsScale.Count; i++)
            {
                idnum++;
                ConstraintsTreeItem item = new ConstraintsTreeItem(ConstraintsTab.AvatarConstraintsScale[i].gameObject)
                {
                    displayName = ConstraintsTab.AvatarConstraintsScale[i].name,
                    id = idnum,
                    depth = 0,
                    icon = AssetPreview.GetMiniTypeThumbnail(typeof(ScaleConstraint)),
                    Type = ConstraintType.Scale,
                    Constraint = ConstraintsTab.AvatarConstraintsScale[i]
                };

                for (int j = 0; j < ConstraintsTab.AvatarConstraintsScale[i].sourceCount; j++)
                {
                    idnum++;
                    ConstraintSource source = ConstraintsTab.AvatarConstraintsScale[i].GetSource(j);
                    if (source.sourceTransform == null) continue;
                    GameObject obj = source.sourceTransform.gameObject;
                    
                    ConstraintsTreeItem child = new ConstraintsTreeItem(obj)
                    {
                        displayName = obj.name,
                        id = idnum,
                        depth = 1,
                        Icon = transformIcon,
                        Type = ConstraintType.Source,
                        Index = j
                    };
                    item.AddChild(child);
                }
                Constraints.Add(item);
            }

            SetupParentsAndChildrenFromDepths(root, Constraints);
            return root;
        }
    }
}