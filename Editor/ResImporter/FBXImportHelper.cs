using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
namespace EMgr
{
    class FBXImportHelper
    {
        static readonly List<string> matchPathList = new List<string>
    {
        @"^(Assets/_Resources/ArtResources/Environments/Map/Build/[^/]+)/Model/([^/]+)\.fbx$",
        @"^(Assets/_Resources/ArtResources/Actors/Actors_UI/[^/]+)/Model/([^/]+)\.fbx$"
    };


        [MenuItem("Assets/选中资源目录并生成Aimator Controller", priority = 2020)]
        public static void GenerateForSelectedDir()
        {
            Object[] objs = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (objs == null || objs.Length == 0)
                return;

            Object obj = objs[0];
            string dir = AssetDatabase.GetAssetPath(obj);
            var fbxs = Directory.GetFiles(dir, "*.fbx", SearchOption.AllDirectories);
            if (fbxs.Length == 0)
                return;

            foreach (var path in fbxs)
            {
                var fbxPath = path.Replace("\\", "/").Replace("//", "/");
                foreach (var matchPath in matchPathList)
                {
                    var match = Regex.Match(fbxPath, matchPath, RegexOptions.IgnoreCase);
                    if (match != Match.Empty)
                        DestroyPrefabAnimatorOrGenerateController(match.Groups[1].Value, match.Groups[2].Value);
                }
            }
        }


        [MenuItem("Assets/检查全部AnimatorController", priority = 2021)]
        public static void CheckModelRuntimeAnimator()
        {
            var assetPaths = AssetDatabase.GetAllAssetPaths();

            Regex regex = new Regex(@"^(Assets/_Resources/Model/Map/Build/[^/]+)/Model/([^/]+)\.fbx$", RegexOptions.IgnoreCase);
            foreach (var assetPath in assetPaths)
            {
                var match = regex.Match(assetPath);
                if (match != Match.Empty)
                {
                    var dir = match.Groups[1].Value;
                    var file = match.Groups[2].Value;
                    DestroyPrefabAnimatorOrGenerateController(dir, file);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.LogError("检查完成");
        }

        public static void DestroyPrefabAnimatorOrGenerateController(string dir, string fbxName)
        {
            var prefabPath = $"{dir}/Prefab/{fbxName}.prefab";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (go == null)
                return;

            var anims = Directory.GetFiles(dir, "*.anim", SearchOption.AllDirectories);
            if (anims.Length > 0)
            {
                GenerateAnimatorControllerAndAssignToPrefab(dir);
            }
            else
            {
                var animators = go.GetComponentsInChildren<Animator>();
                foreach (var animator in animators)
                {
                    if (animator.runtimeAnimatorController == null)
                    {
                        Object.DestroyImmediate(animator, true);
                        EditorUtility.SetDirty(go);
                    }
                }
            }
        }

        private static void GenerateAnimatorControllerAndAssignToPrefab(string modelDir)
        {
            var controller = CreateAnimatorControllerFromSelect(modelDir);
            if (controller == null)
                return;

            var path = AssetDatabase.GetAssetPath(controller);
            var parentPath = path.Substring(0, path.LastIndexOf("/") + 1);
            var assetName = Regex.Match(parentPath, @"/([^/\\]+)/$").Groups[1].Value;
            var prefabPath = parentPath + "Prefab/" + assetName + ".prefab";

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;

            var animator = prefab.GetComponentInChildren<Animator>() ?? prefab.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            PrefabUtility.SavePrefabAsset(prefab);
        }

        public static AnimatorController CreateAnimatorControllerFromSelect(string animDir)
        {
            //string[] animFileList = FileHelper.FindFileBySuffix(animDir, ".anim");
            //if (animFileList.Length == 0)
            //    return null;

            //List<AnimationClip> animations = new List<AnimationClip>();
            //foreach (string path in animFileList)
            //{
            //    AnimationClip chip = AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip)) as AnimationClip;
            //    //AnimationCompressTool.CompressAnimationClip(chip);
            //    animations.Add(chip);
            //}

            //var controllerPath = animDir + "/animator.controller";
            //var animatorController = CreateAnimatorController(animDir, controllerPath, animations);
            //return animatorController;
            return null;
        }


        static readonly HashSet<string> loopClipNames = new HashSet<string> { "Idle" };
        static readonly List<AnimStateTransitonConfig> transitionConfig = new List<AnimStateTransitonConfig>
    {
        new AnimStateTransitonConfig("Build", new[] {"Work", "Idle"}, true),
        new AnimStateTransitonConfig("Exhibition", new[] {"Idle"}, true),
    };

        public static AnimatorController CreateAnimatorController(string animDir, string controllerPath, List<AnimationClip> animations)
        {
            bool needReimport = false;
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                needReimport = true;
            }

            AnimatorControllerLayer layer = controller.layers[0];
            AnimatorStateMachine sm = layer.stateMachine;
            sm.states = null;
            sm.anyStateTransitions = null;

            var name2state = new Dictionary<string, AnimatorState>();

            var idleState = sm.AddState("Idle");
            AnimationClip idleClip = animations.FirstOrDefault(s => s.name == "Idle");
            if (idleClip != null)
            {
                idleState.motion = idleClip;
                animations.Remove(idleClip);
                name2state.Add(idleClip.name, idleState);
            }

            foreach (var newClip in animations)
            {
                if (loopClipNames.Contains(newClip.name))
                {
                    AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(newClip);
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(newClip, settings);
                }
                //newClip.wrapMode = WrapMode.Loop;
                var state = sm.AddState(newClip.name);
                state.motion = newClip;

                name2state.Add(newClip.name, state);
            }

            foreach (var config in transitionConfig)
            {
                if (!name2state.TryGetValue(config.fromAnimName, out var fromState))
                    continue;
                AnimatorState toState = null;
                foreach (var toAnimName in config.toAnimName)
                {
                    if (name2state.TryGetValue(toAnimName, out toState))
                        break;
                }
                if (toState == null)
                    continue;
                fromState.AddTransition(toState, config.hasExitTime);
            }

            controller.layers[0] = layer;
            if (needReimport)
            {
                AssetImporter importer = AssetImporter.GetAtPath(controllerPath);
                importer.SaveAndReimport();
            }

            return controller;
        }


        struct AnimStateTransitonConfig
        {
            public string fromAnimName;
            public string[] toAnimName;
            public bool hasExitTime;

            public AnimStateTransitonConfig(string from, string[] to, bool hasExitTime)
            {
                this.fromAnimName = from;
                this.toAnimName = to;
                this.hasExitTime = hasExitTime;
            }
        }
    }
}