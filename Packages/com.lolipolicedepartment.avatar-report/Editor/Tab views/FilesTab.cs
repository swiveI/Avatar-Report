using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using UnityEditor.IMGUI.Controls;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class FilesTab : AvatarReportTab
    {
        public static List<AssetFile> buildAssets = new List<AssetFile>();
        private BuildReport selectedReport = null;
        private SerializedObject ReportSO;
        private int selectedReportNumber = 0;
        private string selectedPlatform = "StandaloneWindows64";
        private  string buildSize;
        private string avatarBuildReportLocations = "Assets/LPD/AvatarReport/Build Reports/";
        Vector2 scroll;
        
        int platformTab;
        string[] platformNames = new string[]
        {
            "StandaloneWindows64",
            "Android"
        };
        
        //treeview stuff
        private TreeViewState fileTreeState;
        private FilesTreeview fileTree;
        private MultiColumnHeader fileTreeHeader;
        private MultiColumnHeaderState fileTreeHeaderState;

        public override void OnTabOpen()
        {
            //create the directory where we will store the build reports if it doesnt exist already
            if (!Directory.Exists(avatarBuildReportLocations))
            {
                Directory.CreateDirectory(avatarBuildReportLocations);
            }
            
            SetupTreeViewItems();
            
            if (AvatarBuildReportUtility.SelectedAvatar == null) return;
            string latestReportPath = GetMostRecentBuildReport(AvatarBuildReportUtility.SelectedAvatar.blueprintId, selectedPlatform, out int reportNumber);
            selectedReport = AssetDatabase.LoadAssetAtPath<BuildReport>(latestReportPath);
            selectedReportNumber = reportNumber;
            ParseBuildReport(selectedReport);
        }
        
        public override void OnTabClose()
        {
            //clear the build assets list
            buildAssets.Clear();
        }

        private void SetupTreeViewItems()
        {
            //create columns for the treeview header
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Filename", "The name of the file"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type", "The file type"),
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
                    headerContent = new GUIContent("Size", "The size of the uncompressed file"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 78,
                    maxWidth = 78,
                    minWidth = 78,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("%", "The percentage of the total build size this file takes up"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
            };
            
            //setup the treeview state
            fileTreeState = new TreeViewState();
            fileTreeHeaderState = new MultiColumnHeaderState(columns);
            fileTreeHeader = new MultiColumnHeader(fileTreeHeaderState) { height = 30 };
            fileTreeHeader.ResizeToFit();
            fileTree = new FilesTreeview(fileTreeState, fileTreeHeader);
        }
        
        public override void OnAvatarChanged()
        {
            
            CheckForNewReport();
            
            string latestReportPath = GetMostRecentBuildReport(AvatarBuildReportUtility.SelectedAvatar.blueprintId, selectedPlatform, out int reportNumber);
            selectedReport = AssetDatabase.LoadAssetAtPath<BuildReport>(latestReportPath);
            selectedReportNumber = reportNumber;
            ParseBuildReport(selectedReport);
        }
        
        public override void OnTabGui(float baseOffset)
        {
            //check for build platform tab switched
            using (new GUILayout.HorizontalScope())
            {
                int cachedTab = platformTab;
                platformTab = GUILayout.Toolbar(platformTab, platformNames, EditorStyles.toolbarButton);
                selectedPlatform = platformNames[platformTab];
                
                if (platformTab != cachedTab)
                {
                    selectedReport = AssetDatabase.LoadAssetAtPath<BuildReport>(GetMostRecentBuildReport(AvatarBuildReportUtility.SelectedAvatar.blueprintId, selectedPlatform, out int reportnumber));
                    ParseBuildReport(selectedReport);
                }
            }
            GUILayout.Space(5f);

            //check for valid report
            if (selectedReport == null)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Upload your avatar to generate a report", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                }
                return;
            }

            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label("Report for build# " + selectedReportNumber, EditorStyles.boldLabel);
                GUILayout.Label(selectedReport.summary.buildEndedAt.ToString(), EditorStyles.boldLabel);
                GUILayout.Label("Package size " + buildSize, EditorStyles.boldLabel);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Prev"))
                {
                    string[] reports = GetAllBuildReports(AvatarBuildReportUtility.SelectedAvatar.blueprintId, selectedPlatform);
                    selectedReportNumber = Mathf.Clamp(selectedReportNumber - 1, 0, reports.Length - 1);
                    selectedReport = AssetDatabase.LoadAssetAtPath<BuildReport>(reports[selectedReportNumber]);
                    ParseBuildReport(selectedReport);
                }
                if (GUILayout.Button("Next"))
                {
                    string[] reports = GetAllBuildReports(AvatarBuildReportUtility.SelectedAvatar.blueprintId, selectedPlatform);
                    selectedReportNumber = Mathf.Clamp(selectedReportNumber + 1, 0, reports.Length - 1);
                    selectedReport = AssetDatabase.LoadAssetAtPath<BuildReport>(reports[selectedReportNumber]);
                    ParseBuildReport(selectedReport);
                }
                GUILayout.EndHorizontal();
            }

            //draw the treeview
            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (263 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            if (fileTreeHeader == null || fileTree == null) SetupTreeViewItems();
            fileTreeHeader.ResizeToFit();
            fileTree.OnGUI(rect);
            EditorGUILayout.EndVertical();
        }
        
        #region Build Report
        //stuff for sorting the Build Report
        public void CheckForNewReport()
        {
            if (AvatarBuildReportUtility.SelectedAvatar == null) return;
            if (NewReportFound(out int reportNumber, out string avatarID, out string platform))
            {
                
                if (!Directory.Exists(avatarBuildReportLocations + avatarID + "/" + platform))
                {
                    Directory.CreateDirectory(avatarBuildReportLocations + avatarID + "/" + platform);
                }
                string newfilePath = avatarBuildReportLocations + avatarID + "/" + platform + "/Build Report #" + reportNumber + ".buildreport";

                File.Copy("Library/LastBuild.buildreport", newfilePath, true);
                AssetDatabase.ImportAsset(newfilePath);
            }
        }
        bool NewReportFound(out int reportNumber, out string avatarID, out string platform)
        {
            avatarID = null;
            platform = null;

            reportNumber = 0;
            /*
            if (File.Exists("Library/LastBuild.buildreport"))
            {
                //copy the report to assets temporarally so we can read it
                string newfilePath = avatarBuildReportLocations + "Build Report Temp.buildreport";
                File.Copy("Library/LastBuild.buildreport", newfilePath, true);
                AssetDatabase.ImportAsset(newfilePath);

                //load the new report
                BuildReport newReport = (BuildReport)AssetDatabase.LoadAssetAtPath(newfilePath, typeof(BuildReport));

                //get report build platform
                platform = newReport.summary.platform.ToString();

                //try getting the avatar ID
                for (int i = 0; i < newReport.steps.Length; i++)
                {
                    if (newReport.steps[i].name.Contains("prefab-id-v1_avtr"))
                    {
                        avatarID = newReport.steps[i].name.Substring(27, 41);
                        break;
                    }
                }
                if (avatarID == null)
                {
                    Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Last build report wasnt for an avatar");
                }

                string lastReport = GetMostRecentBuildReport(avatarID, platform, out int latestReportNumber);
                if (lastReport == null)
                {
                    Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Found new report for Avatar: " + avatarID);
                    return true;
                }
                if (File.GetLastWriteTime("Library/LastBuild.buildreport") > File.GetLastWriteTime(lastReport))
                {
                    reportNumber = latestReportNumber + 1;
                    Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Found new report for Avatar: " + avatarID);
                    return true;
                }
            }*/
            return false;
        }

        public string GetMostRecentBuildReport(string avatarID, string platform, out int latestReportNumber)
        {
            string[] filenames = GetAllBuildReports(avatarID, platform);
            latestReportNumber = filenames.Length - 1;
            if (filenames.Length == 0) return null;
            Array.Sort(filenames);
            return filenames[filenames.Length - 1];
        }
        public string[] GetAllBuildReports(string avatarID, string platform)
        {
            if (Directory.Exists(avatarBuildReportLocations + avatarID + "/" + platform))
            {
                string[] filenames = Directory.GetFiles(avatarBuildReportLocations + avatarID + "/" + platform, "*.buildreport?");
                return filenames;
            }
            return new string[0];
        }

        //This is pretty much taken straight out of 1s VR World Toolkit, go say thanks https://github.com/oneVR/VRWorldToolkit
        public void ParseBuildReport(BuildReport report)
        {
            if (report == null) return;
            ReportSO = new SerializedObject(report);
            var appendices = ReportSO.FindProperty("m_Appendices");
            if (appendices != null)
            {
                buildAssets = new List<AssetFile>();
                int totalSize = 0;
                for (int i = 0; i < appendices.arraySize; i++)
                {
                    var appendix = appendices.GetArrayElementAtIndex(i);
                    if (appendix.objectReferenceValue.GetType() != typeof(PackedAssets)) continue;

                    var appendixSO = new SerializedObject(appendix.objectReferenceValue);
                    if (appendixSO.FindProperty("m_ShortPath") == null) continue;

                    var contents = appendixSO.FindProperty("m_Contents");

                    for (int j = 0; j < contents.arraySize; j++)
                    {
                        var entry = contents.GetArrayElementAtIndex(j);
                        string filePath = entry.FindPropertyRelative("buildTimeAssetPath").stringValue;
                        string fileType = Path.GetExtension(filePath);
                        bool fileExists = File.Exists(filePath);
                        
                        //remvoe invalid files
                        if (fileType == ".dll" || fileType == ".pdb" || fileType == ".xml" || fileType == ".mdb") continue;
                        if (string.IsNullOrEmpty(filePath) || filePath.Contains("prefab-id-v1")) continue;

                        string fileName = filePath.Split('/').Last();
                        fileName = fileName.Split('.')[0];

                        int filesize = entry.FindPropertyRelative("packedSize").intValue;
                        AssetFile file = new AssetFile()
                        {
                            name = fileName,
                            path = filePath,
                            size = filesize,
                            type = fileType,
                            icon = AssetDatabase.GetCachedIcon(filePath),
                            exists = fileExists,
                        };
                        buildAssets.Add(file);
                        totalSize += filesize;
                    }
                }

                for (int k = 0; k < buildAssets.Count; k++)
                {
                    buildAssets[k].percentage = ((float)buildAssets[k].size / totalSize) * 100;
                }
                buildSize = AvatarBuildReportUtility.FormatSize(report.summary.totalSize);
            }
            fileTree.Reload();
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Processed build report");
        }
        
        public class AssetFile
        {
            public string name;
            public string path;
            public int size;
            public float percentage;
            public string type;
            public Texture icon;
            public bool exists = true;
        }
        #endregion
    }
}