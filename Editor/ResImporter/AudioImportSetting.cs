using UnityEditor;
using UnityEngine;
namespace EMgr
{
    // https://blog.csdn.net/nanyupeng/article/details/121577122
    public class AudioImportSetting : AssetPostprocessor
    {
        void OnPostprocessAudio(AudioClip audioClip)
        {
            AudioImporter audioImpoter = this.assetImporter as AudioImporter;
            audioImpoter.forceToMono = true;//单声道

            AudioImporterSampleSettings windowSetting = audioImpoter.GetOverrideSampleSettings("Standalone");
            AudioImporterSampleSettings androidSetting = audioImpoter.GetOverrideSampleSettings("Android");
            AudioImporterSampleSettings iPhoneSetting = audioImpoter.GetOverrideSampleSettings("iPhone");


            if (IsBackgroundMusic(audioImpoter.assetPath))
            {//长音频 播放时间>=5秒
                windowSetting.loadType = androidSetting.loadType = iPhoneSetting.loadType = AudioClipLoadType.Streaming;
            }
            else
            {
                if (audioClip.length <= 1.1f)//这时长根据UPR的AssetChecker测出来的
                {
                    windowSetting.loadType = androidSetting.loadType = iPhoneSetting.loadType = AudioClipLoadType.DecompressOnLoad;//压缩在内存中（适合较大文件音效） 用cpu换内存
                }
                else if (audioClip.length > 1.1f && audioClip.length <= 3.01f)
                {
                    windowSetting.loadType = androidSetting.loadType = iPhoneSetting.loadType = AudioClipLoadType.CompressedInMemory;//1-3秒
                }
                else
                {
                    windowSetting.loadType = androidSetting.loadType = iPhoneSetting.loadType = AudioClipLoadType.Streaming;  //其他时间        
                }
            }

            windowSetting.compressionFormat = androidSetting.compressionFormat = iPhoneSetting.compressionFormat = AudioCompressionFormat.Vorbis;

            //采样率
            windowSetting.sampleRateSetting = androidSetting.sampleRateSetting = iPhoneSetting.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            windowSetting.sampleRateOverride = androidSetting.sampleRateOverride = iPhoneSetting.sampleRateOverride = 22050;

            var isTalk = IsDialogTalkMusic(audioImpoter.assetPath);//对话没必要一直在内存中
            var isSkill = IsSkillMusic(audioImpoter.assetPath);//技能 每个英雄 技能都不不一样
            if (isTalk || isSkill)
            {
                audioImpoter.preloadAudioData = false;
            }
            //audioImpoter.loadInBackground = true;  //不去做设置   true=多线程加载   类似于UI异步加载的样子 怕不同步 阻塞   

            audioImpoter.SetOverrideSampleSettings("Standalone", windowSetting);
            audioImpoter.SetOverrideSampleSettings("Android", androidSetting);
            audioImpoter.SetOverrideSampleSettings("iPhone", iPhoneSetting);
        }

        static bool IsBackgroundMusic(string path)
        {
            return path.Replace("\\", "/").Contains(@"_Resources/Sound/Background");
        }

        static bool IsDialogTalkMusic(string path)
        {
            return path.Replace("\\", "/").Contains(@"_Resources/Sound/TalkingBubble");
        }
        static bool IsSkillMusic(string path)
        {
            return path.Replace("\\", "/").Contains(@"_Resources/Sound/SKill");
        }

    }
}