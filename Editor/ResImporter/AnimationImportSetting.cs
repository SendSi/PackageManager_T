using System.IO;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System;
using System.Text;

namespace EMgr
{

    public class AnimationImportSetting : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            if (Regex.IsMatch(assetPath, @"Assets/_Resources/Model/Map/Build/[^/\\]+/Animation/Build.anim"))
                ChangeAnimLoopToFalse(assetPath);
        }

        [MenuItem("GameTools/检查建造动画Loop", priority = 3010)]
        public static void CheckAllBuildAnimation()
        {
            var files = Directory.GetFiles("Assets/_Resources/Model/Map/Build", "Build.anim", SearchOption.AllDirectories);
            foreach (var filePath in files)
            {
                var clip = ChangeAnimLoopToFalse(filePath);
                if (clip)
                    EditorUtility.SetDirty(clip);
            }
            AssetDatabase.SaveAssets();
        }

        private static AnimationClip ChangeAnimLoopToFalse(string assetPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (clip.isLooping)
            {
                AnimationClipSettings setting = AnimationUtility.GetAnimationClipSettings(clip);
                setting.loopTime = false;
                AnimationUtility.SetAnimationClipSettings(clip, setting);
                Debug.Log("change build animation loop = false : " + assetPath);
                return clip;
            }
            return null;
        }


        [MenuItem("GameTools/检查建造动画_anim_Float精度", priority = 3010)]
        public static void CheckAllBuildAnimationFloat()
        {
            var files = Directory.GetFiles(@"Assets\_Resources", "*.anim", SearchOption.AllDirectories);
            Debug.LogError(files.Length);
            foreach (var filePath in files)
            {
                //Debug.Log(filePath);
                //if (filePath.Contains("JLTieJiangPu_01"))
                //{
                SetFloat5(filePath);
                //}
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        static void SetFloat5(string assetPath)
        {
            var tPath = Application.dataPath.Replace(@"Assets", "") + assetPath;
            string[] fileContent = File.ReadAllLines(tPath);
            bool isLoading = false;
            StringBuilder sb = new StringBuilder();
            string pattern = @"\d+(.\d+)";
            string replacement = "{0:0.####}";
            string result;
            bool isHasLongFloat = false;

            for (int i = 0; i < fileContent.Length; i++)
            {
                if (isLoading == false && fileContent[i].Contains("m_EditorCurves"))
                {
                    isLoading = true;
                }

                if (isLoading && fileContent[i].Contains("/") == false && fileContent[i].Contains(@"\") == false && fileContent[i].Contains(@"_") == false)
                {
                    //Debug.Log(fileContent[i]);
                    result = Regex.Replace(fileContent[i], pattern, m => string.Format(replacement, Convert.ToDouble(m.Value)));
                    isHasLongFloat = true;
                }
                else
                {
                    result = fileContent[i];
                }
                sb.AppendLine(result);
            }
            if (isHasLongFloat)
            {
                File.WriteAllText(tPath, sb.ToString());
            }
        }
    }
}