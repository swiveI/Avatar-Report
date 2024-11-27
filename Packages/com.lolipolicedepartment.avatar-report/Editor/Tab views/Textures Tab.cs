using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace LocalPoliceDepartment.Utilities.AvatarReport
{
    //looked at a lot of Thry's code for this one
    public class TexturesTab : AvatarReportTab
    {
        private List<Material> materials = new List<Material>();
        private List<Texture> textures = new List<Texture>();
        private List<TextureInfo> textureInfos = new List<TextureInfo>();
        private long totalVRamUsage = 0;
        private string buildTarget;
        private Color platformColor = Color.white;
        private bool refreshTextures = false;

        int[] textureSizes = new int[]
        {
            32,
            64,
            128,
            256,
            512,
            1024,
            2048,
            4096,
            8192
        };

        private TextureImporterFormat[] windowsFormats = new TextureImporterFormat[]
        {
            TextureImporterFormat.Automatic,
            TextureImporterFormat.Automatic,
            TextureImporterFormat.BC4,
            TextureImporterFormat.BC5,
            TextureImporterFormat.BC7,
            TextureImporterFormat.DXT1,
            TextureImporterFormat.DXT5,
        };
        
        private TextureImporterFormat[] androidFormats = new TextureImporterFormat[]
        {
            TextureImporterFormat.Automatic,
            TextureImporterFormat.Automatic,
            TextureImporterFormat.ASTC_4x4,
            TextureImporterFormat.ASTC_6x6,
            TextureImporterFormat.ASTC_8x8,
        };

        Vector2 scrollPos;
        public override void OnTabOpen()
        {
            base.OnTabOpen();
            buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            switch (buildTarget)
            {
                case "StandaloneWindows64":
                    platformColor = new Color(0.2f, 0.5f, 1f);
                    break;
                case "Android":
                    platformColor = new Color(0.2f, 1f, 0.5f);
                    break;
                default:
                    platformColor = Color.white;
                    break;
            }
        }

        public override void OnTabClose()
        {
            base.OnTabClose();
        }

        public override void OnTabGui(float offset)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Total Textures on avatar: " + textures.Count, EditorStyles.boldLabel);
                GUILayout.Label("Texture VRAM usage: " + AvatarBuildReportUtility.FormatSize(totalVRamUsage), EditorStyles.boldLabel);
            }
            
            float textureOffset = ((Screen.height - GUILayoutUtility.GetLastRect().y) - (227 + (Screen.width / 4)))  - offset;
            using (var scrollview = new GUILayout.ScrollViewScope(scrollPos, GUILayout.Height(textureOffset)))
            {
                scrollPos = scrollview.scrollPosition;
                foreach (var textureInfo in textureInfos)
                {
                    DrawTextureButton(textureInfo);
                }

                GUI.backgroundColor = Color.white;
                if (refreshTextures)
                {
                    refreshTextures = false;
                    ProcessAvatarTextures();
                }
            }
        }

        public override void OnAvatarChanged()
        {
            ProcessAvatarTextures();
        }

        private void DrawTextureButton(TextureInfo info)
        {
            float previewSize = Screen.width /3 - 20f;
            float optionsSize = (Screen.width /3) * 2 - 25f;

            //font style
            GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel);
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.fontSize = Screen.width / 35;
            textStyle.wordWrap = false;
            textStyle.clipping = TextClipping.Clip;
            
            GUI.backgroundColor = Color.gray;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(info.texture, info.name), GUILayout.Height(previewSize), GUILayout.Width(previewSize)))
            {
                EditorGUIUtility.PingObject(info.texture);
            }
            
            DrawImporterSettings(info, textStyle, previewSize, optionsSize);
            GUILayout.EndHorizontal();
            DrawMaterialsUsingTexture(info, textStyle);
            
            GUILayout.Space(5);
            GUI.backgroundColor = Color.black;
            GUILayout.Box(new GUIContent(EditorGUIUtility.whiteTexture), GUILayout.ExpandWidth(true), GUILayout.Height(2));
            GUI.backgroundColor = Color.gray;
            GUILayout.Space(5);
        }
        
        private void DrawImporterSettings(TextureInfo info, GUIStyle textStyle, float previewSize, float optionsSize)
        {
            GUI.backgroundColor = Color.white;
            using (new GUILayout.VerticalScope(new GUIStyle("Box"), GUILayout.Width(optionsSize +20), GUILayout.Height(previewSize)))
            {
                
                GUILayout.Label(info.name + info.filetype, textStyle, GUILayout.Width(optionsSize));
                textStyle.fontStyle = FontStyle.Normal;
                
                //if the importer is null its probably a builtin unity asset
                if (info.importer == null)
                {
                    GUILayout.Label("Built in Unity asset", textStyle, GUILayout.Width(optionsSize));
                    return;
                }
                
                GUILayout.Label( "Native: " + info.pixelSize.width + "x" + info.pixelSize.height, textStyle, GUILayout.Width(optionsSize));
                GUILayout.Label("Vram usage: " + AvatarBuildReportUtility.FormatSize(info.vRamSize), textStyle, GUILayout.Width(optionsSize));
                
                //importer settings
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = platformColor;
                using (new GUILayout.VerticalScope(new GUIStyle("Box"), GUILayout.Width(optionsSize)))
                {
                    textStyle.fontStyle = FontStyle.Bold;
                    GUILayout.Label(buildTarget + " Import Settings", textStyle, GUILayout.Width(optionsSize));
                    
                    textStyle.fontStyle = FontStyle.Normal;
                    textStyle.alignment = TextAnchor.MiddleLeft;
                    GUI.backgroundColor = Color.white;
                    
                    //resolution
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Max size: ", textStyle, GUILayout.Width(optionsSize/2));

                        EditorGUI.BeginChangeCheck();
                        info.importedSize = EditorGUILayout.IntPopup(info.importedSize, new string[]
                        {
                            "32",
                            "64",
                            "128",
                            "256",
                            "512",
                            "1024",
                            "2048",
                            "4096",
                            "Bruh"
                        }, textureSizes, GUILayout.Width(optionsSize/2));
                        if (EditorGUI.EndChangeCheck())
                        {
                            TextureImporterPlatformSettings oldSettings = info.importer.GetPlatformTextureSettings(buildTarget);
                            
                            info.importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                            {
                                name = buildTarget,
                                overridden = true,
                                format = oldSettings.format,
                                maxTextureSize = info.importedSize,
                                compressionQuality = 100,
                            });
                            info.importer.SaveAndReimport();
                            refreshTextures = true;
                        }
                    }
                    
                    //texture format
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Format: ", textStyle, GUILayout.Width(optionsSize/2));
                        TextureImporterPlatformSettings importsSettings = info.importer.GetPlatformTextureSettings(buildTarget);
                        
                        TextureImporterFormat[] formats = buildTarget == "StandaloneWindows64" ? windowsFormats : androidFormats;
                        formats[0] = importsSettings.format;
                        
                        EditorGUI.BeginChangeCheck();
                        int newFormat = EditorGUILayout.Popup(0, formats.Select(x => x.ToString()).ToArray(), GUILayout.Width(optionsSize/2));
                        if (EditorGUI.EndChangeCheck())
                        {
                            TextureImporterPlatformSettings oldSettings = info.importer.GetPlatformTextureSettings(buildTarget);
                            
                            info.importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings()
                            {
                                name = buildTarget,
                                overridden = true,
                                format = formats[newFormat],
                                maxTextureSize = oldSettings.maxTextureSize,
                                compressionQuality = 100,
                            });
                            info.importer.SaveAndReimport();
                            refreshTextures = true;
                        }
                    }
                }
            }
        }
        private void DrawMaterialsUsingTexture(TextureInfo info, GUIStyle textStyle)
        {
            GUI.backgroundColor = new Color(.5f, .5f, .7f);
            using (new GUILayout.VerticalScope(new GUIStyle("Box"), GUILayout.Width(Screen.width - 25)))
            {
                textStyle.fontStyle = FontStyle.Bold;
                textStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label(new GUIContent(info.materials.Count + " Materials using " + info.name), textStyle);
                GUILayout.Space(3);
            
                textStyle.fontStyle = FontStyle.Normal;
                textStyle.alignment = TextAnchor.MiddleLeft;
                
                //draw a small preview of each material and its name
                foreach (var material in info.materials)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.backgroundColor = Color.white;
                        if (GUILayout.Button(new GUIContent(AssetPreview.GetAssetPreview(material), AssetDatabase.GetAssetPath(material)), GUILayout.Width(30), GUILayout.Height(30)))
                        {
                            EditorGUIUtility.PingObject(material);
                        }
                        GUI.backgroundColor = new Color(.5f, .5f, .7f);
                        GUILayout.Label(material.name, textStyle, GUILayout.Height(30));
                    }
                }
            }
            GUI.backgroundColor = Color.grey;
        }
        
        private void ProcessAvatarTextures()
        {
            totalVRamUsage = 0;
            materials.Clear();
            textures.Clear();
            textureInfos.Clear();

            //get all materials first
            foreach (var renderer in AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    if (!materials.Contains(material)) materials.Add(material);
                }
            }
            
            //get all textures from materials
            foreach (var material in materials)
            {
                int[] textureIDs = material.GetTexturePropertyNameIDs();
                foreach (var textureID in textureIDs)
                {
                    Texture texture = material.GetTexture(textureID);
                    if (texture == null) continue;
                    if (!textures.Contains(texture)) textures.Add(texture);
                    else
                    {
                        //we have seen this texture before, add this material to its textureinfo
                        foreach (var info in textureInfos)
                        {
                            if (info.texture == texture)
                            {
                                if (info.materials.Contains(material)) break;
                                info.materials.Add(material);
                                break;
                            }
                        }
                        continue;
                    }
                    
                    //get resolution from the actual texture file before import
                    string path = AssetDatabase.GetAssetPath(texture);
                    if (path.Contains("unity_builtin_extra"))
                    {
                        var defaultResourceInfo = new TextureInfo
                        {
                            texture = texture,
                            name = texture.name.Length <= 20 ? texture.name : texture.name.Substring(0, 20) + "...",
                            filetype = "",
                            pixelSize = new Size(){height = 0, width = 0},
                            importedSize = 0,
                            vRamSize = Profiler.GetRuntimeMemorySizeLong(texture),
                            importer = null,
                            materials = new List<Material>()
                            {
                                material
                            }
                        };
                        textureInfos.Add(defaultResourceInfo);
                        continue;
                    }
                    
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    TextureImporterFormat format = importer.GetPlatformTextureSettings(buildTarget).format;
                    TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(buildTarget);
                    
                    if (format == TextureImporterFormat.Automatic)
                    {
                        format = importer.GetAutomaticFormat(buildTarget);
                    }

                    var textureInfo = new TextureInfo
                    {
                        texture = texture,
                        name = texture.name.Length <= 20 ? texture.name : texture.name.Substring(0, 20) + "...",
                        filetype = path.Substring(path.LastIndexOf('.')),
                        pixelSize = GetOriginalTextureSize(importer),
                        importedSize = settings.maxTextureSize,
                        vRamSize = CalculateTextureVram(texture, format),
                        importer = importer,
                        materials = new List<Material>()
                        {
                            material
                        }
                    };
                    textureInfos.Add(textureInfo);
                }
            }
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Processed Textures");
        }
        
        struct TextureInfo
        {
            public Texture texture;
            public string name;
            public string filetype;
            public Size pixelSize;
            public int importedSize;
            public long vRamSize;
            public TextureImporter importer;
            public List<Material> materials;
        }
        
        //yoinked from the internet
        private delegate void GetWidthAndHeight(TextureImporter importer, ref int width, ref int height);
        private static GetWidthAndHeight getWidthAndHeightDelegate;
 
        public struct Size {
            public int width;
            public int height;
        }
        
        public static Size GetOriginalTextureSize(TextureImporter importer)
        {
            if (importer == null) return new Size(){width = 0, height = 0};
            
            if (getWidthAndHeightDelegate == null) {
                var method = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
                getWidthAndHeightDelegate = Delegate.CreateDelegate(typeof(GetWidthAndHeight), null, method) as GetWidthAndHeight;
            }
 
            var size = new Size();
            getWidthAndHeightDelegate(importer, ref size.width, ref size.height);
 
            return size;
        }
        
        //thry's vram stuff, from my understanding it takes the number of pixels * the bits per pixel
        static Dictionary<TextureImporterFormat, int> BPP = new Dictionary<TextureImporterFormat, int>()
        {
            { TextureImporterFormat.BC7 , 8 },
            { TextureImporterFormat.DXT5 , 8 },
            { TextureImporterFormat.DXT5Crunched , 8 },
            { TextureImporterFormat.RGBA64 , 64 },
            { TextureImporterFormat.RGBA32 , 32 },
            { TextureImporterFormat.RGBA16 , 16 },
            { TextureImporterFormat.DXT1 , 4 },
            { TextureImporterFormat.DXT1Crunched , 4 },
            { TextureImporterFormat.RGB48 , 64 },
            { TextureImporterFormat.RGB24 , 32 },
            { TextureImporterFormat.RGB16 , 16 },
            { TextureImporterFormat.BC5 , 8 },
            { TextureImporterFormat.RG32 , 32 },
            { TextureImporterFormat.BC4 , 4 },
            { TextureImporterFormat.R8 , 8 },
            { TextureImporterFormat.R16 , 16 },
            { TextureImporterFormat.Alpha8 , 8 },
            { TextureImporterFormat.RGBAHalf , 64 },
            { TextureImporterFormat.BC6H , 8 },
            { TextureImporterFormat.RGB9E5 , 32 },
            { TextureImporterFormat.ETC2_RGBA8Crunched , 8 },
            { TextureImporterFormat.ETC2_RGB4 , 4 },
            { TextureImporterFormat.ETC2_RGBA8 , 8 },
            { TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA , 4 },
            { TextureImporterFormat.PVRTC_RGB2 , 2 },
            { TextureImporterFormat.PVRTC_RGB4 , 4 },
            { TextureImporterFormat.ARGB32 , 32 },
            { TextureImporterFormat.ARGB16 , 16 }
        };

        private long CalculateTextureVram(Texture texture, TextureImporterFormat format)
        {
            if (!BPP.ContainsKey(format)) return Profiler.GetRuntimeMemorySizeLong(texture);
            
            int bpp = BPP[format];
            long bytes = 0;
            double mipmaps = 1;
            for (int i = 0; i < texture.mipmapCount; i++) mipmaps += Math.Pow(0.25, i + 1);
            bytes = (long)(bpp * texture.width * texture.height * (texture.mipmapCount > 1 ? mipmaps : 1) / 8);
            totalVRamUsage += bytes;
            return bytes;
        }
    }
}