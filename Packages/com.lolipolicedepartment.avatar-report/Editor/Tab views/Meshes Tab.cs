using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class MeshesTab : AvatarReportTab
    {
        public static List<MeshAsset> MeshAssets;
        private MeshStats stats;

        //treeview stuff
        private MeshesTreeview meshTree;
        private TreeViewState meshTreeState;
        private MultiColumnHeader meshTreeHeader;
        private MultiColumnHeaderState meshTreeHeaderState;

        public override void OnTabOpen()
        {
            //clear lists
            MeshAssets = new List<MeshAsset>();

            //setup treeview
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Renderer", "The object with a renderer component that uses this mesh"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 75,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Triangles", "The number of triangles this mesh has"),
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
                    headerContent = new GUIContent("Type", "Whether this is a skinned mesh or not"),
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
                    headerContent = new GUIContent("Shapes", "If this is a skinnedmesh, how many blendshapes it has"),
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
                    headerContent = new GUIContent("Mats", "How many material slots this mesh has"),
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
                    headerContent = new GUIContent("Vram", "how much Vram this mesh takes up"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 64,
                    maxWidth = 64,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
            };
            
            meshTreeState = new TreeViewState();
            meshTreeHeaderState = new MultiColumnHeaderState(columns);
            meshTreeHeader = new MultiColumnHeader(meshTreeHeaderState) { height = 30 };
            meshTreeHeader.ResizeToFit();
            meshTree = new MeshesTreeview(meshTreeState, meshTreeHeader);
            
            //get all the meshes
            //ProcessMeshes();
        }

        public override void OnTabClose()
        {
            base.OnTabClose();
        }

        public override void OnTabGui(float baseOffset)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(new GUIContent("Total Mesh Renderers:  Static " + stats.MeshCount + " Skinned " + stats.SkinnedMeshCount, "All renderer components even if they have a duplicate or no mesh assigned"), EditorStyles.boldLabel);
                GUILayout.Label(new GUIContent("Triangles: " + stats.TriCount, "The sum of all independent meshes on the avatar. This includes duplicates"),  EditorStyles.boldLabel);
                GUILayout.Label(new GUIContent("MaterialSlots: " + stats.MaterialSlots, "All material slots across all mesh and skinned mesh renderers, even if they have a duplicate or no material assigned"), EditorStyles.boldLabel);
                GUILayout.Label(new GUIContent("Vram Size: " + AvatarBuildReportUtility.FormatSize(stats.VramSize), "How much Vram is used by meshes on your avatar. Not 100% accurate but should give you a good idea"), EditorStyles.boldLabel);
            }
            
            //draw the treeview
            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (255 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            meshTreeHeader.ResizeToFit();
            meshTree.OnGUI(rect);
            EditorGUILayout.EndVertical();
        }

        public override void OnAvatarChanged()
        {
            ProcessMeshes();
        }

        private void ProcessMeshes()
        {
            MeshAssets = new List<MeshAsset>();
            meshSizeCache = new Dictionary<Mesh, long>();
            stats = new MeshStats();
            
            //get the renderer components
            SkinnedMeshRenderer[] skinnedMeshRenderers = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            MeshFilter[] meshRenderers = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<MeshFilter>(true);
            ParticleSystemRenderer[] particleRenderers = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<ParticleSystemRenderer>(true);

            stats.MeshCount = meshRenderers.Length;
            stats.SkinnedMeshCount = skinnedMeshRenderers.Length;
            
            //icons
            Texture2D skinnedMeshIcon = AssetPreview.GetMiniTypeThumbnail(typeof(SkinnedMeshRenderer));
            Texture2D meshIcon = AssetPreview.GetMiniTypeThumbnail(typeof(MeshRenderer));
            Texture2D meshParticleIcon = AssetPreview.GetMiniTypeThumbnail(typeof(ParticleSystem));
            
            //a list of all sharedmeshes with no duplicates for calculating stats
            List<Mesh> meshes = new List<Mesh>();
            
            
            //create a mesh asset for each mesh
            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                if (renderer.sharedMesh != null)
                {
                    long vramSize = GetMeshVramSize(renderer.sharedMesh);
                    
                    if (!meshes.Contains(renderer.sharedMesh))
                    {
                        meshes.Add(renderer.sharedMesh);
                        stats.VramSize += vramSize;
                    }
                    
                    stats.TriCount += renderer.sharedMesh.triangles.Length / 3;
                    stats.MaterialSlots += renderer.sharedMesh.subMeshCount;
                    
                    MeshAsset asset = new MeshAsset()
                    {
                        BlendShapes = renderer.sharedMesh.blendShapeCount,
                        Name = renderer.sharedMesh.name,
                        MaterialSlots = renderer.sharedMesh.subMeshCount,
                        Type = "Skinned",
                        TriCount = renderer.sharedMesh.triangles.Length / 3,
                        VramSize = vramSize,
                        ObjectReference = renderer.gameObject,
                        Icon = skinnedMeshIcon,
                        Exists = true
                    };
                    MeshAssets.Add(asset);
                }
                else
                {
                    MeshAsset asset = new MeshAsset()
                    {
                        BlendShapes = 0,
                        Name = "Renderer has no mesh assigned",
                        MaterialSlots = 0,
                        Type = "Skinned",
                        TriCount = 0,
                        VramSize = 0,
                        ObjectReference = renderer.gameObject,
                        Icon = EditorGUIUtility.IconContent("d_winbtn_mac_close_a").image,
                        Exists = false
                    };
                    MeshAssets.Add(asset);
                }
            }

            foreach (MeshFilter filter in meshRenderers)
            {
                if (filter.sharedMesh != null)
                {
                    long vramSize = GetMeshVramSize(filter.sharedMesh);
                    
                    if (!meshes.Contains(filter.sharedMesh))
                    {
                        meshes.Add(filter.sharedMesh);
                        stats.VramSize += vramSize;
                    }
                    
                    stats.TriCount += filter.sharedMesh.triangles.Length / 3;
                    stats.MaterialSlots += filter.sharedMesh.subMeshCount;
                    
                    MeshAsset asset = new MeshAsset()
                    {
                        BlendShapes = filter.sharedMesh.blendShapeCount,
                        Name = filter.sharedMesh.name,
                        MaterialSlots = filter.sharedMesh.subMeshCount,
                        Type = "Static",
                        TriCount = filter.sharedMesh.triangles.Length / 3,
                        VramSize = vramSize,
                        ObjectReference = filter.gameObject,
                        Icon = meshIcon,
                        Exists = true
                    };
                    MeshAssets.Add(asset);
                }
                else
                {
                    MeshAsset asset = new MeshAsset()
                    {
                        BlendShapes = 0,
                        Name = "Renderer has no mesh assigned",
                        MaterialSlots = 0,
                        Type = "Static",
                        TriCount = 0,
                        VramSize = 0,
                        ObjectReference = filter.gameObject,
                        Icon = EditorGUIUtility.IconContent("d_winbtn_mac_close_a").image,
                        Exists = false
                    };
                    MeshAssets.Add(asset);
                }
            }
            
            foreach (ParticleSystemRenderer renderer in particleRenderers)
            {
                if (renderer.renderMode != ParticleSystemRenderMode.Mesh) continue;
                if (renderer.mesh != null)
                {
                    long vramSize = GetMeshVramSize(renderer.mesh);
                    
                    if (!meshes.Contains(renderer.mesh))
                    {
                        meshes.Add(renderer.mesh);
                        stats.VramSize += vramSize;
                    }
                    
                    //mesh particles dont count against vrchats tri limit
                    //also mesh particles only work with 1 material
                    stats.MaterialSlots += 1;
                    
                    MeshAsset asset = new MeshAsset()
                    {
                        BlendShapes = renderer.mesh.blendShapeCount,
                        Name = renderer.mesh.name,
                        MaterialSlots = renderer.mesh.subMeshCount,
                        Type = "Particle",
                        TriCount = renderer.mesh.triangles.Length / 3,
                        VramSize = vramSize,
                        ObjectReference = renderer.gameObject,
                        Icon = meshParticleIcon,
                        Exists = true
                    };
                    MeshAssets.Add(asset);
                }
                else
                {
                    MeshAsset asset = new MeshAsset()
                    {
                        BlendShapes = 0,
                        Name = "Renderer has no mesh assigned",
                        MaterialSlots = 0,
                        Type = "Particle",
                        TriCount = 0,
                        VramSize = 0,
                        ObjectReference = renderer.gameObject,
                        Icon = EditorGUIUtility.IconContent("d_winbtn_mac_close_a").image,
                        Exists = false
                    };
                    MeshAssets.Add(asset);
                }
            }
            
            meshTree.Reload();
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Processed Meshes");
        }

        //You cna thank Thry for this one https://github.com/Thryrallo/VRC-Avatar-Performance-Tools/tree/master
        private Dictionary<Mesh, long> meshSizeCache = new Dictionary<Mesh, long>();
        
        private Dictionary<VertexAttributeFormat, int> VertexAttributeByteSize = new Dictionary<VertexAttributeFormat, int>()
        {
            { VertexAttributeFormat.UNorm8, 1},
            { VertexAttributeFormat.SNorm8, 1},
            { VertexAttributeFormat.UInt8, 1},
            { VertexAttributeFormat.SInt8, 1},

            { VertexAttributeFormat.UNorm16, 2},
            { VertexAttributeFormat.SNorm16, 2},
            { VertexAttributeFormat.UInt16, 2},
            { VertexAttributeFormat.SInt16, 2},
            { VertexAttributeFormat.Float16, 2},

            { VertexAttributeFormat.Float32, 4},
            { VertexAttributeFormat.UInt32, 4},
            { VertexAttributeFormat.SInt32, 4},
        };
        private long GetMeshVramSize(Mesh mesh)
        {
            if (meshSizeCache.ContainsKey(mesh))
                return meshSizeCache[mesh];
            
            long bytes = 0;

            var vertexAttributes = mesh.GetVertexAttributes();
            long vertexAttributeVRAMSize = 0;
            foreach (var vertexAttribute in vertexAttributes)
            {
                int skinnedMeshPositionNormalTangentMultiplier = 1;
                // skinned meshes have 2x the amount of position, normal and tangent data since they store both the un-skinned and skinned data in VRAM
                if (mesh.HasVertexAttribute(VertexAttribute.BlendIndices) && mesh.HasVertexAttribute(VertexAttribute.BlendWeight) &&
                    (vertexAttribute.attribute == VertexAttribute.Position || vertexAttribute.attribute == VertexAttribute.Normal || vertexAttribute.attribute == VertexAttribute.Tangent))
                    skinnedMeshPositionNormalTangentMultiplier = 2;
                vertexAttributeVRAMSize += VertexAttributeByteSize[vertexAttribute.format] * vertexAttribute.dimension * skinnedMeshPositionNormalTangentMultiplier;
            }
            var deltaPositions = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            long blendShapeVRAMSize = 0;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                var blendShapeName = mesh.GetBlendShapeName(i);
                var blendShapeFrameCount = mesh.GetBlendShapeFrameCount(i);
                for (int j = 0; j < blendShapeFrameCount; j++)
                {
                    mesh.GetBlendShapeFrameVertices(i, j, deltaPositions, deltaNormals, deltaTangents);
                    for (int k = 0; k < deltaPositions.Length; k++)
                    {
                        if (deltaPositions[k] != Vector3.zero || deltaNormals[k] != Vector3.zero || deltaTangents[k] != Vector3.zero)
                        {
                            // every affected vertex has 1 uint for the index, 3 floats for the position, 3 floats for the normal and 3 floats for the tangent
                            // this is true even if all normals or tangents in all blend shapes are zero
                            blendShapeVRAMSize += 40;
                        }
                    }
                }
            }
            bytes = vertexAttributeVRAMSize * mesh.vertexCount + blendShapeVRAMSize;
            meshSizeCache[mesh] = bytes;
            return bytes;
        }
        
        private struct MeshStats
        {
            public int TriCount;
            public int MaterialSlots;
            public long VramSize;
            public int MeshCount;
            public int SkinnedMeshCount;
        }
    }
    
    public class MeshAsset
    {
        //data to display
        public string Name;
        public int TriCount;
        public string Type;
        public int BlendShapes;
        public int MaterialSlots;
        public long VramSize;


        //for internal use
        public GameObject ObjectReference;
        public Texture Icon;
        public bool Exists;
    }
}