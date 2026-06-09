using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OctoLib
{
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<Language, string>> _translations
            = new Dictionary<string, Dictionary<Language, string>>(StringComparer.OrdinalIgnoreCase);

        private static bool _hasInjected = false;

        public static void AddTranslation(string englishText, Language language, string translatedText)
        {
            if (string.IsNullOrEmpty(englishText) || string.IsNullOrEmpty(translatedText))
                return;

            if (!_translations.ContainsKey(englishText))
                _translations[englishText] = new Dictionary<Language, string>();

            _translations[englishText][language] = translatedText;

            Plugin.Logger.LogInfo($"[OctoLib] Translation added: '{englishText}' → {language}");
        }

        public static void AddTranslations(string englishText, params (Language lang, string text)[] translations)
        {
            foreach (var (lang, text) in translations)
            {
                AddTranslation(englishText, lang, text);
            }
        }

        [HarmonyPatch(typeof(LocalizationTable), "getText")]
        private static class LocalizationTablePatch
        {
            [HarmonyPrefix]
            public static bool Prefix(string enText, string[] languageArray, ref string __result)
            {
                if (_translations.TryGetValue(enText, out var dict))
                {
                    Language currentLang = Settings.Get().Language;

                    if (dict.TryGetValue(currentLang, out string translation))
                    {
                        __result = translation.ToUpper();
                        return false;
                    }
                }

                return true;
            }
        }

        /*[HarmonyPatch(typeof(LocalizedText), "UpdateText")]
        private static class LocalizedTextPatch
        {
            [HarmonyPostfix]
            public static void Postfix(LocalizedText __instance)
            {
                Plugin.Logger.LogInfo($"[OctoLib] Start postfix of LocalizedText.UpdateText");
                if (__instance == null) 
                {
                    Plugin.Logger.LogInfo($"[OctoLib] __instance is null");
                    return; 
                }

                string enText = __instance.GetType().GetField("enText",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(__instance) as string;

                if (string.IsNullOrEmpty(enText)) 
                {
                    Plugin.Logger.LogInfo($"[OctoLib] enText is Null");
                    return; 
                }

                if (_translations.TryGetValue(enText, out var dict))
                {
                    Language lang = Settings.Get().Language;
                    if (dict.TryGetValue(lang, out string translation))
                    {
                        if (__instance.GetComponent<TMPro.TextMeshProUGUI>() is var tmp && tmp != null)
                        {
                            tmp.text = translation;
                            if (!__instance.useFontWithStroke && (lang == Language.ZHCN || lang == Language.ZHTW || lang == Language.JP))
                                tmp.fontStyle = FontStyles.Normal;
                            else
                                tmp.fontStyle = FontStyles.Bold;
                            TMP_FontAsset font = LocalizedText.localizationTable.GetFont(Settings.Get().Language, __instance.useFontWithStroke);
                            Plugin.Logger.LogInfo($"[OctoLib] Translation added: '{tmp.font}' → {font}");
                            if (!__instance.ignoreFontChange && tmp.font != font)
                            {
                                tmp.font = font; 
                            }
                        }
                        else if (__instance.GetComponent<UnityEngine.UI.Text>() is var legacy && legacy != null)
                            legacy.text = translation;
                        Plugin.Logger.LogInfo($"[OctoLib] tmp is Null");
                    }
                }
                else
                    Plugin.Logger.LogInfo($"[OctoLib] cant get translation");
            }
        }*/
    }
}