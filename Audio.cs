using BoplFixedMath;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static OctoLib.Abilities;

namespace OctoLib
{
    public static class Audio
    {
        private static readonly List<NewSoundRegistration> _sounds = new List<NewSoundRegistration>();
        private static bool _hasInjected = false;

        public class NewSoundRegistration
        {
            public string NewSoundName { get; set; }
            public string NewSoundFolder { get; set; }
            public bool IsLoop { get; set; }
        }

        public static void AddSound(string NewSoundName, string NewSoundFolder, bool IsLoop = false)
        {
            if (string.IsNullOrEmpty(NewSoundName))
            {
                Plugin.Logger.LogWarning("[OctoLib] NewSoundName: name is empty!");
                return;
            }

            if (string.IsNullOrEmpty(NewSoundFolder))
            {
                Plugin.Logger.LogWarning("[OctoLib] NewSoundFolder: name is empty!");
                return;
            }

            _sounds.Add(new NewSoundRegistration
            {
                NewSoundName = NewSoundName,
                NewSoundFolder = NewSoundFolder,
                IsLoop = IsLoop
            });
        }

        //I stole this from MoreSongs, sorry about that
        public static string GetAudioPath(string name, string folder)
        {
            var path = Path.Combine("BepInEx", "plugins", folder);
            Plugin.Logger.LogInfo($"AudioPath is {path}");
            List<string> audioFiles = new List<String>(Directory.GetFiles(path));

            foreach (var file in audioFiles)
            {
                if (file.EndsWith(name))
                {
                    return Directory.GetCurrentDirectory() + "\\" + file;
                }
            }

            return null;
        }

        public static string FilePathToFileUrl(string path)
        {
            return new UriBuilder("file", string.Empty)
            {
                Path = path
                        .Replace("%", $"%{(int)'%':X2}")
                        .Replace("[", $"%{(int)'[':X2}")
                        .Replace("]", $"%{(int)']':X2}"),
            }
                .Uri
                .AbsoluteUri;
        }


        public static AudioClip GetAudioClip(string filePath, AudioType fileType)
        {
            Plugin.Logger.LogInfo($"{FilePathToFileUrl(filePath)}");
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(FilePathToFileUrl(filePath), fileType))
            {
                var result = www.SendWebRequest();

                while (!result.isDone)
                {
                    Thread.Sleep(100);
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Logger.LogError($"[OctoLib] Failed to load audio {filePath}: {www.error}");
                    return null;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                clip.name = Path.GetFileNameWithoutExtension(filePath);
                return clip;
            }
        }

        private static AudioType GetAudioType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            AudioType type = AudioType.UNKNOWN;


            if (ext == ".ogg") type = AudioType.OGGVORBIS;
            if (ext == ".wav") type = AudioType.WAV;
            if (ext == ".mp3") type = AudioType.MPEG;


            return type;
        }

        [HarmonyPatch(typeof(AudioManager), "Awake")]
        public static class AudioManagerPatch
        {
            [HarmonyPostfix]
            public static void Awake(AudioManager __instance)
            {
                if (_hasInjected) return;
                var index = __instance.sounds.Length;
                var timesSoundPlayedField = typeof(AudioManager).GetField("timesSoundPlayed", BindingFlags.NonPublic | BindingFlags.Instance);
                var isSoundLoopingField = typeof(AudioManager).GetField("isSoundLooping", BindingFlags.NonPublic | BindingFlags.Instance);
                timesSoundPlayedField.SetValue(__instance, new int[__instance.sounds.Length + _sounds.Count]);
                isSoundLoopingField.SetValue(__instance, new bool[__instance.sounds.Length + _sounds.Count]);
                Sound[] newSounds = new Sound[__instance.sounds.Length + _sounds.Count];

                Array.Copy(__instance.sounds, newSounds, __instance.sounds.Length);

                foreach (var reg in _sounds)
                {
                    Sound NewSound = SoundInject(reg);
                    newSounds[index] = NewSound;
                    newSounds[index].source = __instance.gameObject.AddComponent<AudioSource>();
                    index++;
                }
                __instance.sounds = newSounds;
                __instance.ReInitializeSfx();
                if (Plugin.debugLogged.Value)
                {
                    for (var i = 0; i < __instance.sounds.Length; i++)
                    {
                        Plugin.Logger.LogInfo($"AudioManager sounds name: {__instance.sounds[i].name}");
                    }
                }
                _hasInjected = true;
            }
        }

        private static Sound SoundInject(NewSoundRegistration reg)
        {
            string AudioPath = GetAudioPath(reg.NewSoundName, reg.NewSoundFolder);
            AudioClip NewAudioClip = GetAudioClip(AudioPath, GetAudioType(AudioPath));
            if (NewAudioClip == null) Plugin.Logger.LogError($"AudioClip in path {GetAudioPath(reg.NewSoundName, reg.NewSoundFolder)} is null");

            string Soundname = Path.GetFileNameWithoutExtension(reg.NewSoundName);
            Plugin.Logger.LogInfo($"new sound name: {Soundname}");

            Sound NewSound = new Sound
            {
                name = Soundname,
                clip = NewAudioClip,
                volume = 1f,
                pitch = 1f,
                loop = reg.IsLoop
            };
            return NewSound; 
        }
    }
}
