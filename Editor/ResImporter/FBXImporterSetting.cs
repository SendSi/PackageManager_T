using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace EMgr
{
    public class FBXImporterSetting : AssetPostprocessor
    {
        private string dir;
        private string fileName;

        void OnPreprocessModel()
        {
            var importer = assetImporter as ModelImporter;
            importer.importMaterials = false;  // 默认不带材质球
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            CommonMeshSetting(importer);
            var assetPath = importer.assetPath;
            if (IsFormationMeshModel(assetPath))
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.generateAnimations = ModelImporterGenerateAnimations.None;
                importer.isReadable = false;//dev工程为true,因dev工程的模型要生成AnimMap/AnimTexture          CN工程 En工程特有的false
            }
            else if (IsFormationAnimationModel(assetPath))
            {
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.generateAnimations = ModelImporterGenerateAnimations.GenerateAnimations;
                importer.isReadable = false;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.isReadable = false;
            }
            if (Regex.IsMatch(assetPath, "^Assets/_Resources/Model/Map/Build"))
            {
                ParsePath();
                //GenerateSubPath();
            }
        }

        static bool IsFormationMeshModel(string resPath)
        {
            return Regex.IsMatch(resPath, @"Assets/_Resources/ArtResources/Actors/(BingZhen|Hero)/\w+/Model");
        }

        static bool IsFormationAnimationModel(string resPath)
        {
            return Regex.IsMatch(resPath, @"Assets/_Resources/ArtResources/Actors/(BingZhen|Hero)/\w+/Animations");
        }

        void ParsePath()
        {
            var match = Regex.Match(assetPath, @"^(.*)/Model/([^/]+).fbx$", RegexOptions.IgnoreCase);
            if (match == Match.Empty)
                return;
            dir = match.Groups[1].Value;
            fileName = match.Groups[2].Value;
        }

        private void ClearDefaultMaterial()
        {
            // 去掉fbx默认的材质球，否则会造成装载缓慢以及内存爆炸
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Renderer[] renders = fbx.GetComponentsInChildren<Renderer>();
            foreach (Renderer render in renders)
            {
                render.sharedMaterials = new Material[render.sharedMaterials.Length];
            }
        }

        void CommonMeshSetting(ModelImporter modelImporter)
        {
            modelImporter.globalScale = 1.0f;
            modelImporter.useFileScale = true;  // 为ture，曾3dmax的坐标比例会跟unity一直，否则是1:100的关系，上面的globalScale就要设置成0.01
            modelImporter.optimizeGameObjects = modelImporter.optimizeGameObjects;
            //modelImporter.meshCompression = ModelImporterMeshCompression.Low;
            //modelImporter.importTangents = ModelImporterTangents.None;
            modelImporter.isReadable = false;
            modelImporter.importMaterials = false;
            modelImporter.importLights = false;
            modelImporter.importCameras = false;
            modelImporter.weldVertices = true;
            modelImporter.preserveHierarchy = false;
        }

        // 需要暴露出来的挂点列表，不同类型模型挂点列表不一样
        private void TrySetExposedTransform(ModelImporter modelImporter)
        {
            // 实际项目中，请重写这个判断规则
            if (modelImporter.assetPath.Contains("_Resource/TestAnim") == false)
                return;

            // 然后获得对应的挂点词典
            List<string> exposeTransformsList = new List<string>
        {
            "Root_001"
        };

            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(modelImporter.assetPath);

            // 检测当前资源是否挂点完备，不完备的输出log
            Transform[] mBones = go.GetComponentsInChildren<Transform>(true);
            foreach (string boneName in exposeTransformsList)
            {
                bool isOk = false;
                foreach (Transform containBone in mBones)
                {
                    if (containBone.name == boneName)
                    {
                        isOk = true;
                        break;
                    }
                }

                if (isOk == false)
                    Debug.LogError($"缺乏挂点！ 资源路径：{modelImporter.assetPath} 挂点名字：{boneName}");
            }

            modelImporter.extraExposedTransformPaths = exposeTransformsList.ToArray();
        }

        private void OnPostprocessModel(GameObject go)
        {
            //ClearDefaultMaterial();
            //if (Regex.IsMatch(assetPath, "^Assets/_Resources/Model/Map/Build"))
            //    GeneratePrefab(dir, fileName, assetPath);
        }

        private void GenerateSubPath()
        {
            var subPaths = new[] { "Animation", "Materials", "Prefab", "Texture" };
            foreach (var subPathName in subPaths)
            {
                if (!AssetDatabase.IsValidFolder($"{dir}/{subPathName}"))
                    AssetDatabase.CreateFolder(dir, subPathName);
            }
        }


        public static void GeneratePrefab(string dir, string fileName, string assetPath)
        {
            var keepPoints = new[]
            {
            "Workers",
        };

            var points = new List<Transform>();

            var prefabPath = $"{dir}/Prefab/{fileName}.prefab";
            if (File.Exists(prefabPath))
            {
                var oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                foreach (var pointPath in keepPoints)
                {
                    var finded = oldPrefab.transform.Find(pointPath);
                    if (finded)
                    {
                        var pointClone = Object.Instantiate(finded.gameObject);
                        pointClone.name = finded.name;
                        points.Add(pointClone.transform);
                    }
                }
                AssetDatabase.DeleteAsset(prefabPath);
            }


            var go = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (go == null)
            {
                Debug.LogError("load " + assetPath + " get null");
                return;
            }
            var clone = Object.Instantiate(go) as GameObject;
            if (points.Count > 0)
            {
                foreach (var point in points)
                {
                    point.SetParent(clone.transform);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);
            Object.DestroyImmediate(clone);
        }
    }
}