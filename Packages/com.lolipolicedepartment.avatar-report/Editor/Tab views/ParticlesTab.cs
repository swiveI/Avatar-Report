using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    public class ParticlesTab : AvatarReportTab
    {
        public static List<ParticleAsset> ParticleAssets = new List<ParticleAsset>();
        private ParticleStats stats;
        
        //treeview stuff
        private ParticlesTreeView particleTree;
        private TreeViewState particleTreeState;
        private MultiColumnHeader particleTreeHeader;
        private MultiColumnHeaderState particleTreeHeaderState;
        public override void OnTabOpen()
        {
            //clear list
            ParticleAssets.Clear();
            
            //setup treeview
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name", "The object with a ParticleSystem component"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Max", "The maximum number of particles that can be alive at the same time"),
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
                    headerContent = new GUIContent("Type", "The rendermode of the ParticleSystem"),
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
                    headerContent = new GUIContent("Loop", "Does this ParticleSystem loop?"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 40,
                    maxWidth = 40,
                    minWidth = 40,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Coll", "Does this ParticleSystem have collision enabled?"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 40,
                    maxWidth = 40,
                    minWidth = 40,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Trails", "Does this ParticleSystem have trails enabled?"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 40,
                    maxWidth = 40,
                    minWidth = 40,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Material", "The material used by the ParticleSystem's renderer"),
                    headerTextAlignment = TextAlignment.Center,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
            };
            
            particleTreeState = new TreeViewState();
            particleTreeHeaderState = new MultiColumnHeaderState(columns);
            particleTreeHeader = new MultiColumnHeader(particleTreeHeaderState) { height = 30 };
            particleTreeHeader.ResizeToFit();
            particleTree = new ParticlesTreeView(particleTreeState, particleTreeHeader);
            
            //ProcessAvatarParticles();
        }

        public override void OnTabClose()
        {
            base.OnTabClose();
        }

        public override void OnTabGui(float baseOffset)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(new GUIContent("ParticleSystems on avatar: " + stats.ParticleSystemCount, "The number of ParticleSystem Components on this avatar"), EditorStyles.boldLabel);
                GUILayout.Label(new GUIContent("Max Particle Count: " + stats.MaxParticleCount, "The sum of all Particle systems Max Particles count"),  EditorStyles.boldLabel);
                GUILayout.Label(new GUIContent("Max mesh Particle triangle count : " + stats.MaxMeshParticleTriangles, "The sum of all triangles in meshes used by particle systems"), EditorStyles.boldLabel);
            }
            
            //draw the treeview
            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (235 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            particleTreeHeader.ResizeToFit();
            particleTree.OnGUI(rect);
            EditorGUILayout.EndVertical();
        }

        public override void OnAvatarChanged()
        {
            ProcessAvatarParticles();
        }

        public void ProcessAvatarParticles()
        {
            stats = new ParticleStats();
            ParticleAssets.Clear();

            //get all particle systems on the avatar
            ParticleSystem[] particleSystems = AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<ParticleSystem>(true);
            stats.ParticleSystemCount = particleSystems.Length;
            
            //create a particle asset for each particle system and add to the list
            foreach (var particleSystem in particleSystems)
            {
                ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                string renderMode = renderer.renderMode.ToString();
                GameObject particleObject = particleSystem.gameObject;
                
                ParticleAsset particleAsset = new ParticleAsset();
                particleAsset.name = particleObject.name;
                particleAsset.particleCount = particleSystem.main.maxParticles;
                particleAsset.particleType = renderMode;
                particleAsset.loopEnabled = particleSystem.main.loop;
                particleAsset.collisionEnabled = particleSystem.collision.enabled;
                particleAsset.trailsEnabled = particleSystem.trails.enabled;
                
                particleAsset.ObjectReference = particleObject;
                particleAsset.SystemMain = particleSystem.main;
                particleAsset.SystemCollision = particleSystem.collision;
                particleAsset.SystemTrails = particleSystem.trails;
                particleAsset.RendererReference = renderer;
                
                stats.MaxParticleCount += particleSystem.main.maxParticles;
                if (renderMode == "Mesh")
                {
                    Mesh mesh = renderer.mesh;
                    if (mesh != null)
                    {
                        stats.MaxMeshParticleTriangles += (mesh.triangles.Length / 3 * particleSystem.main.maxParticles);
                    }
                }
                
                ParticleAssets.Add(particleAsset);
            }
            
            particleTree.Reload();
        }
    }
    
    public class ParticleAsset
    {
        //data to display
        public string name;
        public int particleCount;
        public string particleType;
        public bool loopEnabled;
        public bool collisionEnabled;
        public bool trailsEnabled;

        //internal
        public GameObject ObjectReference;
        public ParticleSystem.MainModule SystemMain;
        public ParticleSystem.CollisionModule SystemCollision;
        public ParticleSystem.TrailModule SystemTrails;
        public ParticleSystemRenderer RendererReference;
    }

    public struct ParticleStats
    {
        public int ParticleSystemCount;
        public int MaxParticleCount;
        public int MaxMeshParticleTriangles;
    }
}