using UnityEditor;
using UnityEngine;
using Boo.Lang;

namespace EMgr
{
    public class TextureImporterSetting : AssetPostprocessor
    {
        /// <summary>     Mipmap 不做强制的目录,即可手动修改的目录      </summary>
        static bool IgnoreMipmapEnable(string path)
        {
            var rePath = path.Replace("\\", "/");
            var ignoreList = new List<string>()
        {
            "_Resources/ArtResources/Environments/",

        };
            for (int i = 0; i < ignoreList.Count; i++)
            {
                if (rePath.Contains(ignoreList[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>     maxSize 不做强制的目录,即可手动修改的目录        </summary>
        static bool IgnoreMaxSize(string path)
        {
            var rePath = path.Replace("\\", "/");
            var ignoreList = new List<string>()
        {
            "_Resources/ArtResources/Environments/Map/WorldMap/Texture",
            "_Resources/UI/FguiRes",//fgui不能失真
        };
            for (int i = 0; i < ignoreList.Count; i++)
            {
                if (rePath.Contains(ignoreList[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>     SRGBTexture 不做强制的目录,即可手动修改的目录      </summary>
        static bool IgnoreSRGBTexture(string path)
        {
            var rePath = path.Replace("\\", "/");
            var ignoreList = new List<string>()
            {

            };

            for (int i = 0; i < ignoreList.Count; i++)
            {
                if (rePath.Contains(ignoreList[i]))
                {
                    return true;
                }
            }
            return false;
        }


        void OnPreprocessTexture()
        {
            TextureImporter teximporter = this.assetImporter as TextureImporter;

            if (teximporter.assetPath.Contains("/Editor/"))
            {
                return;//editor文件夹 可以忽略的图片    与真机性能无关
            }

            var ignoreMip = IgnoreMipmapEnable(teximporter.assetPath);//忽略的目录--即可手动修改的目录
            if (ignoreMip)
            {
            }
            else
            {
                MipmapSetting(teximporter);//必须        
            }

            bool isIgnoreSRGB = IgnoreSRGBTexture(teximporter.assetPath);//可手动修改的目录
            if (isIgnoreSRGB)
            {
            }
            else
            {
                SRGBSetting(teximporter);
            }

            var isDev = OtherDevReadable(teximporter);//开发时需要做特殊的处理的
            if (isDev)
            {
                return;
            }

            TextureImporterPlatformSettings windowSetting = teximporter.GetPlatformTextureSettings("Standalone");
            windowSetting.overridden = true;
            windowSetting.format = TextureImporterFormat.RGBA32;

            TextureImporterPlatformSettings androidSetting = teximporter.GetPlatformTextureSettings("Android");
            androidSetting.overridden = true;
            TextureImporterPlatformSettings iPhoneSetting = teximporter.GetPlatformTextureSettings("iPhone");
            iPhoneSetting.overridden = true;


            bool isFGUIRes = IsFGUIRes(teximporter.assetPath);
            if (isFGUIRes)
            {
                teximporter.textureType = TextureImporterType.Default;
                teximporter.textureShape = TextureImporterShape.Texture2D;
                teximporter.alphaIsTransparency = true;
                teximporter.wrapMode = TextureWrapMode.Clamp;
            }

            //法线贴图用ASTC5x5
            if (teximporter.textureType == TextureImporterType.NormalMap)
            {
                androidSetting.format = TextureImporterFormat.ASTC_RGB_5x5;
                iPhoneSetting.format = TextureImporterFormat.ASTC_RGB_5x5;
            }

            if (teximporter.DoesSourceTextureHaveAlpha())
            {
                androidSetting.format = TextureImporterFormat.ASTC_RGB_6x6;
                iPhoneSetting.format = TextureImporterFormat.ASTC_RGB_6x6;
                teximporter.alphaSource = TextureImporterAlphaSource.FromInput;
            }
            else
            {
                androidSetting.format = TextureImporterFormat.ASTC_RGB_8x8;
                iPhoneSetting.format = TextureImporterFormat.ASTC_RGB_8x8;
                teximporter.alphaSource = TextureImporterAlphaSource.None;
            }

            if (IgnoreMaxSize(teximporter.assetPath))
            {
                //可手动修改的目录
            }
            else
            {
                MaxTextureSizeSetting(teximporter, androidSetting, iPhoneSetting, windowSetting);
            }

            teximporter.SetPlatformTextureSettings(windowSetting);
            teximporter.SetPlatformTextureSettings(androidSetting);
            teximporter.SetPlatformTextureSettings(iPhoneSetting);
        }


        static bool IsFGUIRes(string path)
        {
            return path.Replace("\\", "/").Contains(@"_Resources/UI/FguiRes");
        }

        /// <summary>   开发时需要做特殊的处理的 </summary>
        static bool OtherDevReadable(TextureImporter teximporter)
        {
            var path = teximporter.assetPath;
            if (path.Replace("\\", "/").Contains(@"_Resources/UI/HUD") && !path.EndsWith("_HUDAtlas.png"))
            {
                teximporter.textureType = TextureImporterType.Sprite;
                teximporter.isReadable = true;//  要把碎图 删掉的  
                return true;
            }
            return false;
        }

        /// <summary>   maxTextureSize的设置      </summary>
        static void MaxTextureSizeSetting(TextureImporter teximporter, TextureImporterPlatformSettings androidSetting, TextureImporterPlatformSettings iPhoneSetting, TextureImporterPlatformSettings windowSetting)
        {
            var path = teximporter.assetPath;
            if (path.Replace("\\", "/").Contains(@"_Resources/UI/HUD") && path.EndsWith("_HUDAtlas.png"))//hud的 atlas 
            {
                androidSetting.maxTextureSize = 1024;
                iPhoneSetting.maxTextureSize = 1024;
                windowSetting.maxTextureSize = 1024;
            }
            else
            {
                androidSetting.maxTextureSize = 512;
                iPhoneSetting.maxTextureSize = 512;
                windowSetting.maxTextureSize = 512;
            }
        }

        /// <summary>   sRGBTexture的设置      </summary>
        static void SRGBSetting(TextureImporter teximporter)
        {
            var path = teximporter.assetPath;
            if (path.Replace("\\", "/").Contains(@"_Resources/ArtResources/Environments/Terrain/TerrainMaskTexture"))
            {
                teximporter.sRGBTexture = false;
            }
            else
            {
                teximporter.sRGBTexture = true;
            }
        }

        /// <summary>   mipmapEnabled的设置      </summary>
        static void MipmapSetting(TextureImporter teximporter)
        {
            var path = teximporter.assetPath;

            if (path.Replace("\\", "/").Contains(@"_Resources/Effect/UI") || path.Replace("\\", "/").Contains(@"_Resources/UI"))
            {
                teximporter.mipmapEnabled = false;
            }
            else if (path.Replace("\\", "/").Contains(@"_Resources/Terrain/3DTexture"))
            {
                teximporter.mipmapEnabled = true;
            }
            else
            {
                UnityEngine.Debug.Log($"此文件 {path} 不在TextureImporterSetting-mipmapEnabled,请定好规则哦");
            }
        }
    }
}