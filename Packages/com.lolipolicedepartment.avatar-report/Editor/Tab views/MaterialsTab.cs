using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class MaterialsTab : AvatarReportTab
    {
        List<Material> Materials = new List<Material>();
        List<Shader> shaders = new List<Shader>();

        private Dictionary<Shader, string> warningsDictionary = new Dictionary<Shader, string>();

        List<Editor> materialPreviews = new List<Editor>();
        PrimitiveType previewObject = PrimitiveType.Sphere;
        GameObject previewContainer;
        private int previewPrim = 1;
        Vector2 matScrollPos;
        Texture warningIcon;
        
        private string[] PrimitiveObjects = new string[]
        {
            "Cube",
            "Sphere",
            "Capsule",
            "Cylinder",
            "Plane"
        };
        
        public override void OnTabOpen()
        {
            warningIcon = EditorGUIUtility.IconContent("console.erroricon.sml").image;
            //ProcessAvatarMaterials();
        }

        public override void OnTabClose()
        {
            /*Debug.Log(materialPreviews.Count);
            foreach (Editor editor in materialPreviews)
            {
                if (editor != null) DestroyImmediate(editor);
            }
            if (previewContainer != null) DestroyImmediate(previewContainer);*/
        }

        public override void OnTabGui(float offset)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                //unique material and shader count here
                GUILayout.Label("Total Materials on avatar: " + Materials.Count, EditorStyles.boldLabel);
                GUILayout.Label("Total Shaders on avatar: " + shaders.Count, EditorStyles.boldLabel);
            }
            
            int cachedPrim = previewPrim;
            previewPrim = GUILayout.Toolbar(previewPrim, PrimitiveObjects, EditorStyles.toolbarButton);
            if (cachedPrim != previewPrim)
            {
                switch (previewPrim)
                {
                    case 0:
                        previewObject = PrimitiveType.Cube;
                        break;
                    case 1:
                        previewObject = PrimitiveType.Sphere;
                        break;
                    case 2:
                        previewObject = PrimitiveType.Capsule;
                        break;
                    case 3:
                        previewObject = PrimitiveType.Cylinder;
                        break;
                    case 4:
                        previewObject = PrimitiveType.Plane;
                        break;
                }
                CreateMaterialEditors(previewObject);
            }
            
            float materilOffset = ((Screen.height - GUILayoutUtility.GetLastRect().y) - (240 + (Screen.width / 4)))  - offset;
            using (var scrollview = new GUILayout.ScrollViewScope(matScrollPos, GUILayout.Height(materilOffset)))
            {
                matScrollPos = scrollview.scrollPosition;
                GUILayout.BeginHorizontal();
                int rows = 1;
                float previewSize = Screen.width / 3 - 21f;
                
                GUIStyle pingObjButton = new GUIStyle("minibutton");
                pingObjButton.alignment = TextAnchor.MiddleCenter;
                pingObjButton.fixedWidth = previewSize;
                
                bool shadersChanged = false;
                
                for (int i = 0; i < materialPreviews.Count; i++)
                {
                    if (materialPreviews[i].target == null) continue;
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(previewSize)))
                    {
                        materialPreviews[i].OnInteractivePreviewGUI(GUILayoutUtility.GetRect(previewSize, previewSize), "window");
                        if (GUILayout.Button(Materials[i].name, pingObjButton, GUILayout.Width(previewSize)))
                        {
                            EditorGUIUtility.PingObject(Materials[i]); 
                        }

                        GUILayout.BeginHorizontal();
                        EditorGUI.BeginChangeCheck();
                        Materials[i].shader = (Shader)EditorGUILayout.ObjectField(Materials[i].shader, typeof(Shader), false);
                        if (EditorGUI.EndChangeCheck()) shadersChanged = true;

                        if (warningsDictionary.ContainsKey(Materials[i].shader))
                        {
                            //draw warining icon
                            GUILayout.Label(new GUIContent("", warningIcon, warningsDictionary[Materials[i].shader]));
                        }
                        GUILayout.EndHorizontal();
                    }

                    //check if we need to start a new row
                    if (i + 1 == (3 * rows))
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        rows++;
                    }
                }
                
                if (shadersChanged) ProcessAvatarMaterials();
            }
        }

        public override void OnAvatarChanged()
        {
            ProcessAvatarMaterials();
        }
        
        private void ProcessAvatarMaterials()
        {
            Materials.Clear();
            shaders.Clear();
            warningsDictionary.Clear();

            //find all materials on the selected gameobject
            foreach (var renderer in AvatarBuildReportUtility.SelectedAvatar.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }
                    if (!Materials.Contains(material))
                    {
                        Materials.Add(material);
                    }
                    
                    //skip if shader is null
                    if (material.shader == null)
                    {
                        continue;
                    }
                    if (!shaders.Contains(material.shader))
                    {
                        shaders.Add(material.shader);
                    }
                }

                foreach (Shader shader in shaders)
                {
                    if (warningsDictionary.ContainsKey(shader)) continue;
                    
                    //check if the shader used by the material has a grab pass, if so add it to the dictionary
                    string shaderPath = AssetDatabase.GetAssetPath(shader);
                    if (shaderPath.Contains("unity_builtin_extra") || shaderPath.Contains("unity default resources")) continue;
                    string shaderCode = File.ReadAllText(shaderPath);
                    string warning = "";
                    
                    if (shaderCode.Contains("GrabPass"))
                    {
                        warning += "Warning! This shader contains a GrabPass, this is bad for performance!" + '\n';
                    }
                    
                    //check if build target is android
                    if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                    {
                        //check if shader path is not one of the VRChat shaders
                        if (!shaderPath.Contains("VRChat"))
                        {
                            warning += "Warning! This shader is not a built in VRChat shader and cannot be used on Quest avatars!" + '\n';
                        }
                    }
                    
                    if (warning != "") warningsDictionary.Add(shader, warning);
                }
            }

            //create material previews
            CreateMaterialEditors(previewObject);
        }

        private void CreateMaterialEditors(PrimitiveType prim)
        {
            //foreach (Editor editor in materialPreviews) DestroyImmediate(editor);
            materialPreviews.Clear();
            
            //if (previewContainer != null) DestroyImmediate(previewContainer);
            previewContainer = new GameObject("LPD Avatar Report Material Previews");
            previewContainer.hideFlags = HideFlags.HideAndDontSave;
            previewContainer.transform.position = new Vector3(0, 1000, 0);
            previewContainer.transform.localScale = new Vector3(.01f, .01f, .01f);
            
            foreach (Material mat in Materials)
            {
                //create an instance of the primitive and assign the material to it
                GameObject preview = GameObject.CreatePrimitive(prim);
                preview.transform.SetParent(previewContainer.transform, false);
                preview.GetComponent<MeshRenderer>().sharedMaterial = mat;

                materialPreviews.Add(Editor.CreateEditor(preview));
            }
            //DestroyImmediate(previewContainer); //this happens before the editors are ready and throws an error ArgumentException: The Prefab you want to instantiate is null
            
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Processed Materials");
        }
    }   
}