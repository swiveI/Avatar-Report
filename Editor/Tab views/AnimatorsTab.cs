using VRC.SDK3.Avatars.Components;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LoliPoliceDepartment.Utilities.AvatarReport
{
    public class AnimatorsTab : AvatarReportTab
    {
        public static List<AnimatorInformation> Animators = new List<AnimatorInformation>();
        public List<RuntimeAnimatorController> VRCPlayableLayers = new List<RuntimeAnimatorController>();
        private VRCAvatarDescriptor avatarDescriptor;
        
        private int totalLayers = 0;
        private int totalStates = 0;
        private int totalClips = 0;
        private int totalBehaviours = 0;
        private int totalAnyStateTransitions = 0;
        private int totalParameters = 0;
        
        //treeview stuff
        private TreeViewState animationTreeState;
        private AnimationsTreeView animationTree;
        private MultiColumnHeader animationTreeHeader;
        private MultiColumnHeaderState animationTreeHeaderState;

        private string[] LayerNames = new string[]
        {
            "Base",
            "Additive",
            "Gesture",
            "Action",
            "FX",
            "Sitting",
            "TPose",
            "IKPose",
        };

        private int selectedTab = 0;
        private string[] tabs = new[]
        {
            "VRC Playable layers",
            "Details"
        };
        
        public override void OnTabOpen()
        {
            //clear lists
            Animators.Clear();

            //setup treeview
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name", "The object with an animator component"),
                    headerTextAlignment = TextAlignment.Left,
                    minWidth = 64,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Layers", "The number of layers this animation controller has"),
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
                    headerContent = new GUIContent("States", "The number of AnimationStates this animation controller has"),
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
                    headerContent = new GUIContent("SB", "The number of StateMachineBehaviours this animation controller has"),
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
                    headerContent = new GUIContent("AST", "The number of AnyState Transitions this animation controller has"),
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
                    headerContent = new GUIContent("Params", "The number of parameters this animation controller has"),
                    headerTextAlignment = TextAlignment.Center,
                    width = 50,
                    maxWidth = 50,
                    minWidth = 50,
                    canSort = true,
                    sortedAscending = true,
                    autoResize = false,
                },
            };
            
            animationTreeState = new TreeViewState();
            animationTreeHeaderState = new MultiColumnHeaderState(columns);
            animationTreeHeader = new MultiColumnHeader(animationTreeHeaderState) { height = 30 };
            animationTreeHeader.ResizeToFit();
            animationTree = new AnimationsTreeView(animationTreeState, animationTreeHeader);
        }

        public override void OnTabClose()
        {
            base.OnTabClose();
        }

        public override void OnTabGui(float offset)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Total Animators on avatar: " + Animators.Count, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("Total Animation Layers: " + totalLayers, "Animation layers get more expensive the more you have."), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                    GUILayout.Label(new GUIContent("Total Animation States: " + totalStates), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("Total Animation Clips: " + totalClips, "This is calculated as unique clips per layer so may be slightly inaccurate"), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                    GUILayout.Label(new GUIContent("Total StateBehaviours: " + totalBehaviours), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("Total Any State Transitions: " + totalAnyStateTransitions, "These can get very expensive the more states you have"), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                    GUILayout.Label(new GUIContent("Total Animator Parameters: " + totalParameters), EditorStyles.boldLabel, GUILayout.Width(Screen.width / 2));
                }
            }

            selectedTab = GUILayout.Toolbar(selectedTab, tabs, EditorStyles.toolbarButton);
            switch (selectedTab)
            {
                case 0:
                    DrawPlayableLayers();
                    break;
                case 1:
                    DrawTreeView(offset);
                    break;
            }
        }

        private void DrawPlayableLayers()
        {
            GUI.backgroundColor = new Color(.5f, .5f, 1f);
            using (new GUILayout.VerticalScope(new GUIStyle("GroupBox")))
            {
                GUILayout.Label(new GUIContent("Playable Layers", "These are the animation controllers defined in your VRCAvatarDescriptor"), EditorStyles.boldLabel);
                for (int i = 0; i < avatarDescriptor.baseAnimationLayers.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(LayerNames[i]), GUILayout.Width(50));
                    if (avatarDescriptor.baseAnimationLayers[i].isDefault)
                    {
                        if (GUILayout.Button("Default " + LayerNames[i], new GUIStyle("minibuttonmid"), GUILayout.ExpandWidth(true)))
                        {
                            avatarDescriptor.baseAnimationLayers[i].isDefault = false;
                        }
                    }
                    else
                    {
                        avatarDescriptor.baseAnimationLayers[i].animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(avatarDescriptor.baseAnimationLayers[i].animatorController, typeof(RuntimeAnimatorController), false);
                        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_winbtn_mac_close_a").image, "Reset to Default"), new GUIStyle("minibuttonmid"), GUILayout.Width(20)))
                        {
                            avatarDescriptor.baseAnimationLayers[i].isDefault = true;
                            avatarDescriptor.baseAnimationLayers[i].animatorController = null;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUI.backgroundColor = new Color(.2f, 1f, .8f);
            using (new GUILayout.VerticalScope(new GUIStyle("GroupBox")))
            {
                GUILayout.Label(new GUIContent("Special Layers", "These are the animation controllers defined in your VRCAvatarDescriptor"), EditorStyles.boldLabel);
                for (int i = 0; i < avatarDescriptor.specialAnimationLayers.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(LayerNames[i + 5]), GUILayout.Width(50));
                    
                    if (avatarDescriptor.specialAnimationLayers[i].isDefault)
                    {
                        if (GUILayout.Button("Default " + LayerNames[i + 5], new GUIStyle("minibuttonmid"), GUILayout.ExpandWidth(true)))
                        {
                            avatarDescriptor.specialAnimationLayers[i].isDefault = false;
                        }
                    }
                    else
                    {
                        avatarDescriptor.specialAnimationLayers[i].animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(avatarDescriptor.specialAnimationLayers[i].animatorController, typeof(RuntimeAnimatorController), false);
                        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_winbtn_mac_close_a").image, "Reset to Default"), new GUIStyle("minibuttonmid"), GUILayout.Width(20)))
                        {
                            avatarDescriptor.specialAnimationLayers[i].isDefault = true;
                            avatarDescriptor.specialAnimationLayers[i].animatorController = null;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawTreeView(float baseOffset)
        {
            //draw the treeview
            Rect rect = EditorGUILayout.BeginVertical();
            float offset = ((Screen.height - rect.y) - (275 + (Screen.width / 4)))  - baseOffset;
            GUILayout.Space(offset);
            animationTreeHeader.ResizeToFit();
            animationTree.OnGUI(rect);
            EditorGUILayout.EndVertical();
        }
        
        public override void OnAvatarChanged()
        {
            ProcessAvatarAnimators();
        }
        
        private void ProcessAvatarAnimators()
        {
            //clear the lists
            Animators.Clear();
            VRCPlayableLayers.Clear();
            totalLayers = 0;
            totalStates = 0;
            totalClips = 0;
            totalBehaviours = 0;
            totalAnyStateTransitions = 0;
            totalParameters = 0;

            //get the avatar descriptor
            avatarDescriptor = AvatarBuildReportUtility.SelectedAvatar.GetComponent<VRCAvatarDescriptor>();
            Animator animator = avatarDescriptor.GetComponent<Animator>();
            if (animator == null)
            {
                animationTree.Reload();
                return;
            }

            //get the VRChat playable layers and special layers first
            foreach (var layer in avatarDescriptor.baseAnimationLayers)
            {
                VRCPlayableLayers.Add(layer.animatorController);
                if (layer.animatorController == null) continue;
                
                Animators.Add(GetAnimatiorInformation(animator, layer.animatorController));
            }
            foreach (var layer in avatarDescriptor.specialAnimationLayers)
            {
                VRCPlayableLayers.Add(layer.animatorController);
                if (layer.animatorController == null) continue;
                
                Animators.Add(GetAnimatiorInformation(animator, layer.animatorController));
            }
            
            //get all other animators on this avatar
            Animator[] animators = avatarDescriptor.GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                if (anim == animator) continue;
                if (anim.runtimeAnimatorController == null) continue;
                
                Animators.Add(GetAnimatiorInformation(anim, anim.runtimeAnimatorController));
            }
            
            //calculate the totals
            foreach (var animatorInformation in Animators)
            {
                totalLayers += animatorInformation.LayerCount;
                totalStates += animatorInformation.StateCount;
                totalClips += animatorInformation.AnimationClips.Count;
                totalBehaviours += animatorInformation.StateBehaviourCount;
                totalParameters += animatorInformation.ParameterCount;
                totalAnyStateTransitions += animatorInformation.AnyStateTransitionCount;
            }
            
            animationTree.Reload();
            Debug.Log("<color=#24bc41><b>Avatar Report:</b></color> Processed Animators");
        }

        private AnimatorInformation GetAnimatiorInformation(Animator animator, RuntimeAnimatorController controller)
        {
            AnimatorController controllerAsset = controller as AnimatorController;
            
            int states = 0;
            int behaviours = 0;
            int transitions = 0;
            List<AnimationClip> clips = new List<AnimationClip>();

            // get all animation states from evey layer
            foreach (AnimatorControllerLayer controllerLayer in controllerAsset.layers)
            {
                states += controllerLayer.stateMachine.states.Length;
                
                //get the number of statebehaviours
                foreach (ChildAnimatorState state in controllerLayer.stateMachine.states)
                {
                    behaviours += state.state.behaviours.Length;
                }
                
                //get the number of Any State Transitions
                transitions += controllerLayer.stateMachine.anyStateTransitions.Length;
            }

            foreach (AnimationClip clip in controllerAsset.animationClips)
            {
                if (clips.Contains(clip)) continue;
                clips.Add(clip);
            }

            var item = new AnimatorInformation()
            {
                AnimatorComponent = animator,
                AnimationClips = clips,
                LayerCount = controllerAsset.layers.Length,
                StateCount = states,
                ParameterCount = controllerAsset.parameters.Length,
                StateBehaviourCount = behaviours,
                AnyStateTransitionCount = transitions,
            };
            return item;
        }
    }

    public class AnimatorInformation
    {
        public Animator AnimatorComponent;
        public List<AnimationClip> AnimationClips;
        public int LayerCount;
        public int StateCount;
        public int ParameterCount;
        public int StateBehaviourCount;
        public int AnyStateTransitionCount;
        
    }
}