using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    public struct BoneInfo
    {
        public bool isLeaf;
        public bool isPhysbone;
        public bool hasWeight;
        public Transform bone;
    }
    public class BonesTab : AvatarReportTab
    {
        TreeViewState BoneTreeState;
        BoneTree BoneTree;
        MultiColumnHeaderState BoneHeaderState;
        MultiColumnHeader BoneHeader;
        
        //textures
        private Texture2D LeafBone;
        private Texture2D PhysBone;
        private Texture2D WeightBone;

        public static List<BoneInfo> Bones = new List<BoneInfo>();
        public int NumberOfBones = 0;
        
        public override void OnTabOpen()
        {
            LeafBone = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Resources/Leaf icon.png", typeof(Texture2D));
            PhysBone = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Resources/Phys icon.png", typeof(Texture2D));
            WeightBone = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Resources/Weight icon.png", typeof(Texture2D));
            
            SetupTreeViewItems();
            //ProcessAvatarBones();
        }

        private void SetupTreeViewItems()
        {
            //plant trees
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Bone gameobject name"),
                    headerTextAlignment = TextAlignment.Left,
                    width = 200,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(WeightBone, "This bone has no weights, it wont affect the mesh"),
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    sortedAscending = true,
                    width = 30,
                    minWidth = 30,
                    maxWidth = 30,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(PhysBone, "This bone has a physbone component"),
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    sortedAscending = false,
                    width = 30,
                    minWidth = 30, maxWidth = 30,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(LeafBone, "This bone looks like a leafbone, if it has no weight it can probably be safely deleted"),
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    sortedAscending = false,
                    width = 30,
                    minWidth = 30,
                    maxWidth = 30,
                    autoResize = false,
                },
            };
            
            BoneTreeState = new TreeViewState();
            BoneHeaderState = new MultiColumnHeaderState(columns);
            BoneHeader = new MultiColumnHeader(BoneHeaderState)
            {
                height = 30,
            };
            BoneHeader.ResizeToFit();
            BoneTree = new BoneTree(BoneTreeState, BoneHeader);
        }

        public override void OnTabGui(float baseOffset)
        {
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Number of Bones: " + NumberOfBones, EditorStyles.boldLabel);                
            }
            GUILayout.Space(5f);

            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (210 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            BoneHeader.ResizeToFit();
            BoneTree.OnGUI(rect);
            EditorGUILayout.EndVertical();
        }
        
        public override void OnTabClose()
        {
            
        }

        public override void OnAvatarChanged()
        {
            ProcessAvatarBones();
        }
        
        public  void ProcessAvatarBones()
        {
            Bones = new List<BoneInfo>();
            NumberOfBones = 0;
            
            //get all skinned mesh renderers in the avatar
            SkinnedMeshRenderer[] skinnedMeshRenderers = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                foreach (Transform bone in renderer.bones)
                {
                    NumberOfBones++;
                    int boneIndex = GetBoneIndex(renderer, bone);
                    if (boneIndex == -1) continue;
                    
                    BoneInfo info = new BoneInfo()
                    {
                        hasWeight = CheckBone(boneIndex, renderer.sharedMesh.boneWeights),
                        bone = bone,
                        isPhysbone = bone.TryGetComponent(typeof(VRCPhysBone), out Component comp),
                        isLeaf = bone.name.Contains("_end"),
                    };
                    Bones.Add(info);
                }
            }
            BoneTree.Reload();
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Processsed bones");
        }
        public int GetBoneIndex(SkinnedMeshRenderer renderer, Transform bone)
        {
            for (int i = 0; i < renderer.bones.Length; i++)
            {
                if (renderer.bones[i].name.Equals(bone.name)) return i;
            }
            return -1;
        }
        public bool CheckBone(int BonetoCheck, BoneWeight[] weights)
        {
            foreach (BoneWeight weight in weights)
            {
                if (BonetoCheck == weight.boneIndex0) return true;
                if (BonetoCheck == weight.boneIndex1) return true;
                if (BonetoCheck == weight.boneIndex2) return true;
                if (BonetoCheck == weight.boneIndex3) return true;
            }
            return false;
        }
    }
    
}
