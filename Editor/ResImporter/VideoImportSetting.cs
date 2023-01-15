using UnityEditor;
namespace EMgr
{
    // https://docs.unity3d.com/Manual/class-VideoClip.html
    //GetTargetSettings()--https://docs.unity3d.com/cn/current/ScriptReference/VideoClipImporter.GetTargetSettings.html
    //平台字符串的选项为 "Default"、"Standalone"、"Android"、"iOS"、"Lumin"、"PS4"、"Switch"、"tvOS"、"WebGL"、"WSA"、"WebGL" 或 "XboxOne"。
    public class VideoImportSetting : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            VideoClipImporter videoImpoter = this.assetImporter as VideoClipImporter;
            if (videoImpoter != null && IsVideoPath(videoImpoter.assetPath))
            {
                videoImpoter.sRGBClip = true;
                videoImpoter.deinterlaceMode = VideoDeinterlaceMode.Off;
                videoImpoter.flipHorizontal = false;
                videoImpoter.flipVertical = false;
                videoImpoter.importAudio = true;
                var videoSetting = videoImpoter.GetTargetSettings("Default");
                var defaultTargetSettings = videoImpoter.defaultTargetSettings;

                //VideoImporterTargetSettings videoSetting = new VideoImporterTargetSettings();
                videoSetting.enableTranscoding = true;
                videoSetting.resizeMode = VideoResizeMode.ThreeQuarterRes;
                videoSetting.aspectRatio = VideoEncodeAspectRatio.NoScaling;
                videoSetting.customWidth = defaultTargetSettings.customWidth;
                videoSetting.customHeight = defaultTargetSettings.customHeight;
                videoSetting.bitrateMode = defaultTargetSettings.bitrateMode;
                videoSetting.spatialQuality = defaultTargetSettings.spatialQuality;

                //videoImpoter.SetTargetSettings("Android", videoSetting);
                //videoImpoter.SetTargetSettings("iOS", videoSetting);
                videoImpoter.SetTargetSettings("Default", videoSetting);
                //videoImpoter.SetTargetSettings("Standalone", videoSetting);
            }
        }
        static bool IsVideoPath(string path)
        {
            return path.Replace("\\", "/").Contains(@"_Resources/Video");
        }

    }
}