using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using VRC.Core;
using UnityEngine.Animations;
using UnityEngine.Tilemaps;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation.Performance.Stats;
using Debug = UnityEngine.Debug;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class AvatarBuildReportUtility : EditorWindow
    {
        public static PipelineManager SelectedAvatar = null;
        public static AvatarPerformanceStats SelectedAvatarStats;

        bool avatarFoldout = false;
        bool autoRefresh = false;
        bool statsFoldout = false;
        Vector2 avatarScroll;
        Vector2 statsScroll;

        PipelineManager[] avatarsInScene;
        
        TreeViewState constraintTreeState;
        ConstraintTree constraintTree;

        int tabNumber = 0;
        private GUIContent[] tabNames;
        private List<AvatarReportTab> tabs = new List<AvatarReportTab>();

        //textures
        private Texture2D headerTexture;
        private Texture2D excelentPerficon;
        private Texture2D goodPerficon;
        private Texture2D mediumPerficon;
        private Texture2D poorPerficon;
        private Texture2D veryPoorPerficon;
        private Texture2D unknownPerficon;
        private Texture2D twitterLogo;
        private Texture2D discordLogo;
        private Texture2D youtubeLogo;
        private Texture2D kofiLogo;
        
        [MenuItem("LPD/Avatar Report")]
        public static void ShowWindow()
        {
            AvatarBuildReportUtility window = GetWindow<AvatarBuildReportUtility>("Avatar Report");
            window.maxSize = new Vector2(1024f, 4000);
            window.minSize = new Vector2(400, 512);
            window.Show();
        }

        public void OnEnable()
        {
            //load all Textures Packages/com.lolipolicedepartment.avatar-report/Editor/SocialLogos/TwitterLogo.png
            headerTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Resources/TITLEBAR.png", typeof(Texture2D));
            excelentPerficon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.vrchat.base/Runtime/VRCSDK/Dependencies/VRChat/Resources/PerformanceIcons/Perf_Great_32.png");
            goodPerficon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.vrchat.base/Runtime/VRCSDK/Dependencies/VRChat/Resources/PerformanceIcons/Perf_Good_32.png");
            mediumPerficon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.vrchat.base/Runtime/VRCSDK/Dependencies/VRChat/Resources/PerformanceIcons/Perf_Medium_32.png");
            poorPerficon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.vrchat.base/Runtime/VRCSDK/Dependencies/VRChat/Resources/PerformanceIcons/Perf_Poor_32.png");
            veryPoorPerficon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.vrchat.base/Runtime/VRCSDK/Dependencies/VRChat/Resources/PerformanceIcons/Perf_Horrible_32.png");
            unknownPerficon = EditorGUIUtility.IconContent("d__Help@2x").image as Texture2D;
            twitterLogo = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Editor/SocialLogos/TwitterLogo.png", typeof(Texture2D));
            discordLogo = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Editor/SocialLogos/DiscordLogo.png", typeof(Texture2D));
            youtubeLogo = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Editor/SocialLogos/YoutubeLogo.png", typeof(Texture2D));
            kofiLogo = (Texture2D)AssetDatabase.LoadAssetAtPath("Packages/com.lolipolicedepartment.avatar-report/Editor/SocialLogos/KofiLogo.png", typeof(Texture2D));
            

            //guicontent for tabs
            tabNames = new GUIContent[12];
            tabNames[0] = new GUIContent(" Files",AssetPreview.GetMiniTypeThumbnail(typeof(Tilemap)),"All files uploaded to VRChat");
            tabNames[1] = new GUIContent(" Materials", AssetPreview.GetMiniTypeThumbnail(typeof(Material)),"All materials on the avatar, note that this is not material slots, nor does it account for materials used for material swap animations");
            tabNames[2] = new GUIContent(" Meshes", AssetPreview.GetMiniTypeThumbnail(typeof(Mesh)),"All meshes and skinned meshes used by the avatar");
            tabNames[3] = new GUIContent(" Bones", AssetPreview.GetMiniTypeThumbnail(typeof(Avatar)),"All bones on this avatar if it has any");
            tabNames[4] = new GUIContent(" Particles", AssetPreview.GetMiniTypeThumbnail(typeof(ParticleSystem)),"All Particle systems on this avatar");
            tabNames[5] = new GUIContent(" Animators", AssetPreview.GetMiniTypeThumbnail(typeof(Animator)),"All Animators used by the avatar");
            tabNames[6] = new GUIContent(" Lights", AssetPreview.GetMiniTypeThumbnail(typeof(Light)),"All Light components on the avatar");
            tabNames[7] = new GUIContent(" Trails/Lines", AssetPreview.GetMiniTypeThumbnail(typeof(TrailRenderer)), "All Trail Renderer and Line Renderer components on the avatar");
            tabNames[8] = new GUIContent(" Cloths", AssetPreview.GetMiniTypeThumbnail(typeof(Cloth)),"All Cloth components on the avatar");
            tabNames[9] = new GUIContent(" Constraints", AssetPreview.GetMiniTypeThumbnail(typeof(ParentConstraint)),"All Constraint components on the avatar");
            tabNames[10] = new GUIContent(" Dynamics", EditorGUIUtility.IconContent("NetworkAnimator Icon").image,"All Physbones, Contact Senders, and Receivers on the avatar");
            tabNames[11] = new GUIContent(" Textures", AssetPreview.GetMiniTypeThumbnail(typeof(Texture)),"All textures for this avatar");
            
            CreateTabs();
            
            //find avatars
            RefreshAvatars();
            if (avatarsInScene.Length == 0)
            {
                Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> No avatars found in the scene");
                return;
            }
            /*
            SelectedAvatar = avatarsInScene[0];
            
            // only process the stuff for the first tab
            tabs[tabNumber].OnAvatarChanged();*/
        }
        

        private void CreateTabs()
        {
            tabs.Clear();
            //Create instances of the tabs
            tabs.Add(new FilesTab());
            tabs.Add(new MaterialsTab());
            tabs.Add(new MeshesTab());
            tabs.Add(new BonesTab());
            tabs.Add(new ParticlesTab());
            tabs.Add(new AnimatorsTab());
            tabs.Add(new LightsTab());
            tabs.Add(new TrailsLinesTab());
            tabs.Add(new ClothsTab());
            tabs.Add(new ConstraintsTab());
            tabs.Add(new DynamicsTab());
            tabs.Add(new TexturesTab());
            
            //init tabs
            foreach (AvatarReportTab tab in tabs)
            {
                tab.OnTabOpen();
            }
        }

        private void OnDisable()
        {
            foreach (AvatarReportTab tab in tabs)
            {
                tab.OnTabClose();
            }
            tabs.Clear();
        }

        public void OnFocus()
        {
            RefreshAvatars();
            if (autoRefresh)
            {
                GetAvatarStats();
                tabs[tabNumber].OnAvatarChanged();
            }
        }

        public void RefreshAvatars()
        {
            avatarsInScene = FindObjectsOfType(typeof(PipelineManager)) as PipelineManager[];
        }

        public void GetAvatarStats()
        {
            SelectedAvatarStats = new AvatarPerformanceStats(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android);
            AvatarPerformance.CalculatePerformanceStats(SelectedAvatar.gameObject.name, SelectedAvatar.gameObject, SelectedAvatarStats, EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android);
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Avatar stats updated");
        }
        
        private void OnGUI()
        {
            GUI.contentColor = Color.white;
            float drawarea = Screen.width / 4;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, drawarea), headerTexture, ScaleMode.ScaleToFit);

            GUILayout.BeginArea(new Rect(0, drawarea, Screen.width, Screen.height));

            if (avatarsInScene.Length == 0)
            {
                GUILayout.Space(5f);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("No avatars found in the scene", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndArea();
                DrawSocialButtons();
                return;
            }
            
            
            GUILayout.Space(5f);

            //top buttons for selecting avatars and refreshing the avatar info
            GUIStyle AvatarSelectorButtonStyle = new GUIStyle("LargeButton");
            AvatarSelectorButtonStyle.fontStyle = FontStyle.Bold;
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUI.backgroundColor = new Color(.2f, .6f, .2f, 1f);
                    if (GUILayout.Button(new GUIContent("Select Avatar", "Click to Show/Hide Avatars \nNote: Disabled avatars are hidden"), AvatarSelectorButtonStyle,  GUILayout.Width(Screen.width /2)))
                    {
                        avatarFoldout = !avatarFoldout;
                    } 
                    GUI.backgroundColor = new Color(.2f, .2f, .6f, 1f);
                    if (GUILayout.Button(new GUIContent("Refresh Avatar Info", "Finds all files and components for the selected view. Note this can be slow if your avatar is complex"), AvatarSelectorButtonStyle, GUILayout.ExpandWidth(true)))
                    {
                        GetAvatarStats();
                        tabs[tabNumber].OnAvatarChanged();
                    }

                    if (autoRefresh) GUI.backgroundColor = Color.red;
                    else GUI.backgroundColor = Color.white;
                    if (GUILayout.Button(new GUIContent("Auto", "Automatically refresh the avatar info, this can be laggy"), AvatarSelectorButtonStyle))
                    {
                        autoRefresh = !autoRefresh;
                    }
                    GUI.backgroundColor = Color.white;
                }

                if (avatarFoldout)
                {
                    using (var scrollview = new GUILayout.ScrollViewScope(avatarScroll, GUILayout.Height(Screen.width/2)))
                    {
                        avatarScroll = scrollview.scrollPosition;
                        GUILayout.BeginHorizontal();
                        int rows = 1;
                        int buttonSize = (Screen.width / 4) - 11;

                        for (int i = 0; i < avatarsInScene.Length; i++)
                        {
                            //button
                            GUILayout.BeginVertical();
                            if (GUILayout.Button(new GUIContent(AssetPreview.GetAssetPreview(avatarsInScene[i].gameObject)), GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                            {
                                SelectedAvatar = avatarsInScene[i];
                                GetAvatarStats();
                                tabs[tabNumber].OnAvatarChanged();
                            }
                            string AvatarName = avatarsInScene[i].gameObject.name;

                            //label
                            GUILayout.Label(AvatarName, EditorStyles.boldLabel, GUILayout.Width((Screen.width / 4) - 11f));

                            GUILayout.EndVertical();

                            //check if we need to go to a new line
                            if (i + 1 == (4 * rows))
                            {
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                rows++;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            
            if (SelectedAvatar == null)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("No avatar selected", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndArea();
                DrawSocialButtons();
                return;
            }
            if (SelectedAvatarStats == null) GetAvatarStats();
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUIStyle avatarNameStyle = new GUIStyle(EditorStyles.boldLabel);
                    avatarNameStyle.fontSize = 16;
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(SelectedAvatar.gameObject.name, avatarNameStyle);
                    GUILayout.FlexibleSpace();
                }
                
                PerformanceRating overallRating = SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.Overall);
                Texture2D performanceIcon = GetPerformanceIcon(overallRating);
                
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("Overall Performance: " + overallRating, performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent(statsFoldout? "Hide Details" : "Show Details", "Show the performance ranking for each category"), AvatarSelectorButtonStyle))
                    {
                        statsFoldout = !statsFoldout;
                    }
                }
            }
            
            DrawCurrentTab();

            GUILayout.EndArea();
            DrawSocialButtons();
        }

        private void DrawCurrentTab()
        {
            int cachedTabNumber = tabNumber;
            GUIStyle tabButtonStyle = new GUIStyle("LargeButton");
            GUI.backgroundColor = Color.gray;
            tabNumber = GUILayout.SelectionGrid(tabNumber, tabNames, 4, tabButtonStyle);
            GUI.backgroundColor = Color.white;
            
            float offset = avatarFoldout ? (Screen.width / 2) + 23 : 23;
            if (cachedTabNumber != tabNumber) tabs[tabNumber].OnAvatarChanged();
            if (statsFoldout) DrawStats(offset);
            else tabs[tabNumber].OnTabGui(offset);
        }

        private void DrawStats(float baseOffset = 0)
        {
            Texture2D performanceIcon;
            
            GUILayout.Space(5);
            using (var scrollView = new GUILayout.ScrollViewScope(statsScroll, GUILayout.Height(Screen.height - (Screen.width / 4 + baseOffset + 178))))
            {
                statsScroll = scrollView.scrollPosition;
                EditorGUIUtility.SetIconSize(new Vector2(20, 20));

                using (new GUILayout.HorizontalScope())
                {
                    //left column
                    using (new GUILayout.VerticalScope( ))
                    {
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.DownloadSize));
                           int downloadSize = SelectedAvatarStats.downloadSizeBytes ?? 0;
                           GUILayout.Label(new GUIContent("Download size: " + FormatSize(downloadSize), performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.PolyCount));
                           GUILayout.Label(new GUIContent("Polygons: " + SelectedAvatarStats.polyCount + "/70,000", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.SkinnedMeshCount));
                           GUILayout.Label(new GUIContent("Skinned meshes: " + SelectedAvatarStats.skinnedMeshCount + "/16", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.MaterialCount));
                           GUILayout.Label(new GUIContent("Material slots: " + SelectedAvatarStats.materialCount + "/32", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.PhysBoneTransformCount));
                           int physBoneTransforms = SelectedAvatarStats.physBone?.transformCount ?? 0;
                           GUILayout.Label(new GUIContent("PhysBone transforms: " + physBoneTransforms + "/256", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.PhysBoneCollisionCheckCount));
                           int physBoneCollisionChecks = SelectedAvatarStats.physBone?.collisionCheckCount ?? 0;
                           GUILayout.Label(new GUIContent("PhysBone collision checks: " + physBoneCollisionChecks + "/256", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.AnimatorCount));
                           GUILayout.Label(new GUIContent("Animators: " + SelectedAvatarStats.animatorCount + "/32", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.LightCount));
                           GUILayout.Label(new GUIContent("Lights: " + SelectedAvatarStats.lightCount + "/1", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ParticleTotalCount));
                           GUILayout.Label(new GUIContent("Total max particles: " + SelectedAvatarStats.particleTotalCount + "/2,500", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ParticleTrailsEnabled));
                           GUILayout.Label(new GUIContent("Particle trails: " + SelectedAvatarStats.particleTrailsEnabled + "/True", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.TrailRendererCount));
                           GUILayout.Label(new GUIContent("Trail renderers: " + SelectedAvatarStats.trailRendererCount + "/8", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ClothMaxVertices));
                           GUILayout.Label(new GUIContent("Cloth max vertices: " + SelectedAvatarStats.clothMaxVertices + "/200", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.PhysicsRigidbodyCount));
                           GUILayout.Label(new GUIContent("Rigidbodies: " + SelectedAvatarStats.physicsRigidbodyCount + "/8", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ConstraintsCount));
                           GUILayout.Label(new GUIContent("Constraints: " + SelectedAvatarStats.constraintsCount + "/15", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                    }
                    
                    //right column
                    using (new GUILayout.VerticalScope())
                    {
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.TextureMegabytes));
                           float textureSize = SelectedAvatarStats.textureMegabytes ?? 0;
                           GUILayout.Label(new GUIContent("Texture memory: " + textureSize.ToString("##0.00") + "MB", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.AABB));
                           Vector3 bounds = SelectedAvatarStats.aabb?.max ?? Vector3.zero;
                           GUILayout.Label(new GUIContent("Bounds: " + bounds + "/(5.0, 6.0, 5.0)", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.MeshCount));
                           GUILayout.Label(new GUIContent("Basic meshes: " + SelectedAvatarStats.meshCount + "/24", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.PhysBoneComponentCount));
                           int physBoneComponentCount = SelectedAvatarStats.physBone?.componentCount ?? 0;
                           GUILayout.Label(new GUIContent("PhysBone Components: " + physBoneComponentCount + "/32", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.PhysBoneColliderCount));
                           int physBoneColliderCount = SelectedAvatarStats.physBone?.colliderCount ?? 0;
                           GUILayout.Label(new GUIContent("PhysBone colliders: " + physBoneColliderCount + "/8", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ContactCount));
                           GUILayout.Label(new GUIContent("Contact count: " + SelectedAvatarStats.contactCount + "/32", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.BoneCount));
                           GUILayout.Label(new GUIContent("Bones: " + SelectedAvatarStats.boneCount + "/400", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ParticleSystemCount));
                           GUILayout.Label(new GUIContent("Particle systems: " + SelectedAvatarStats.lightCount + "/16", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ParticleMaxMeshPolyCount));
                           GUILayout.Label(new GUIContent("Mesh particle polygons: " + SelectedAvatarStats.particleMaxMeshPolyCount + "/5000", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ParticleCollisionEnabled));
                           GUILayout.Label(new GUIContent("Particle collisions: " + SelectedAvatarStats.particleCollisionEnabled + "/True", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.ClothCount));
                           GUILayout.Label(new GUIContent("Cloth meshes: " + SelectedAvatarStats.clothCount + "/1", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.PhysicsColliderCount));
                           GUILayout.Label(new GUIContent("Collider count: " + SelectedAvatarStats.physicsColliderCount + "/8", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                       using (new GUILayout.HorizontalScope())
                       {
                           performanceIcon = GetPerformanceIcon(SelectedAvatarStats.GetPerformanceRatingForCategory(AvatarPerformanceCategory.AudioSourceCount));
                           GUILayout.Label(new GUIContent("Rigidbodies: " + SelectedAvatarStats.audioSourceCount + "/8", performanceIcon), EditorStyles.boldLabel, GUILayout.Height(20), GUILayout.MaxWidth(Screen.width / 2 - 5));
                       }
                    }
                }
                EditorGUIUtility.SetIconSize(Vector2.zero);
                if (GUILayout.Button(new GUIContent("Open Documentation", "https://creators.vrchat.com/avatars/avatar-performance-ranking-system/"), EditorStyles.miniButtonMid, GUILayout.Width(Screen.width), GUILayout.Height(20)))
                {
                    Application.OpenURL("https://creators.vrchat.com/avatars/avatar-performance-ranking-system/");
                }
            }
        }

        private void DrawSocialButtons()
        {
            GUILayout.BeginArea(new Rect(0, Screen.height - 40f, Screen.width, 60f));
            using (new GUILayout.HorizontalScope())
            {
                GUI.color = Color.white;
                GUI.backgroundColor = Color.white;
                
                GUI.backgroundColor = new Color(0.4509804f, 0.5411765f, 0.8588236f, 1f);
                if (GUILayout.Button(new GUIContent(discordLogo, "Discord"), EditorStyles.miniButtonMid, GUILayout.Width(Screen.width / 4), GUILayout.Height(60))) Application.OpenURL("https://discord.gg/lpd");
                GUI.backgroundColor = new Color(0.1137255f, .6313726f, 0.9490196f, 1f);
                if (GUILayout.Button(new GUIContent(twitterLogo, "Twitter"), EditorStyles.miniButtonMid, GUILayout.Width(Screen.width / 4), GUILayout.Height(60))) Application.OpenURL("https://twitter.com/LPD_vrchat");
                GUI.backgroundColor = new Color(0.8039216f, 0.1254902f, 0.1215686f, 1f);
                if (GUILayout.Button(new GUIContent(youtubeLogo, "Youtube"), EditorStyles.miniButtonMid, GUILayout.Width(Screen.width / 4), GUILayout.Height(60))) Application.OpenURL("https://www.youtube.com/c/LoliPoliceDepartment");
                GUI.backgroundColor = new Color(1f, 0.3137255f, 0.3137255f, 1f);
                if (GUILayout.Button(new GUIContent(kofiLogo, "Ko-fi"), EditorStyles.miniButtonMid, GUILayout.Width(Screen.width / 4), GUILayout.Height(60))) Application.OpenURL("https://ko-fi.com/lolipolicedepartment");
            }
            GUILayout.EndArea();
        }
        
        public static string FormatSize(float size)
        {
            if (size < 1024)
                return size + " B";
            if (size < 1024 * 1024)
                return (size / 1024.00).ToString("##0.0") + " KB";
            if (size < 1024 * 1024 * 1024)
                return (size / (1024.0 * 1024.0)).ToString("##0.0") + " MB";
            return (size / (1024.0 * 1024.0 * 1024.0)).ToString("##0.0") + " GB";
        }
        
        public static string FormatSize(long size)
        {
            if (size < 1024)
                return size + " B";
            if (size < 1024 * 1024)
                return (size / 1024.00).ToString("##0.0") + " KB";
            if (size < 1024 * 1024 * 1024)
                return (size / (1024.0 * 1024.0)).ToString("##0.0") + " MB";
            return (size / (1024.0 * 1024.0 * 1024.0)).ToString("##0.0") + " GB";
        }

        public Texture2D GetPerformanceIcon(PerformanceRating perfRating)
        {
            Texture2D performanceIcon;
            switch (perfRating)
            {
                case PerformanceRating.Excellent:
                    performanceIcon = excelentPerficon;
                    break;
                case PerformanceRating.Good:
                    performanceIcon = goodPerficon;
                    break;
                case PerformanceRating.Medium:
                    performanceIcon = mediumPerficon;
                    break;
                case PerformanceRating.Poor:
                    performanceIcon = poorPerficon;
                    break;
                case PerformanceRating.VeryPoor:
                    performanceIcon = veryPoorPerficon;
                    break;
                case PerformanceRating.None:
                    performanceIcon = excelentPerficon;
                    break;
                    
                default:
                    performanceIcon = unknownPerficon;
                    break;
            };
            return performanceIcon;
        }
    }
}