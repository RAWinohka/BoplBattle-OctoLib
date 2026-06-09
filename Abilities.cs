using BoplFixedMath;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OctoLib
{
    public static class Abilities
    {
        private static readonly List<BaseAbilityRegistration> _abilitywithbase = new List<BaseAbilityRegistration>();
        private static readonly List<AbilityCooldown> _cooldowns = new List<AbilityCooldown>();
        private static readonly List<AbilityOffensive> _offensive = new List<AbilityOffensive>();
        private static readonly List<AbilityBackground> _backgrounds = new List<AbilityBackground>();
        private static readonly List<CustomAbilityRegistration> _ability = new List<CustomAbilityRegistration>();
        private static bool _hasInjected = false;

        public class BaseAbilityRegistration
        {
            public string BaseAbilityName { get; set; }
            public string NewAbilityName { get; set; }
            public Texture2D IconTexture { get; set; }
            public Assembly OwnerAssembly { get; set; }
        }

        public class CustomAbilityRegistration
        {
            public string NewAbilityName { get; set; }
            public Texture2D IconTexture { get; set; }
            public GameObject GameObjectPrefab { get; set; }
            public Assembly OwnerAssembly { get; set; }
        }

        public class AbilityCooldown
        {
            public string AbilityName { get; set; }
            public Fix Cooldown { get; set; }
        }

        public class AbilityOffensive
        {
            public string AbilityName { get; set; }
            public bool Offensive { get; set; }
        }

        public class AbilityBackground
        {
            public string AbilityName { get; set; }
            public Texture2D Background { get; set; }

            public Assembly OwnerAssembly { get; set; }
        }

        public static void NewAbilityWithBase(
            string baseAbilityName,
            string newAbilityName,
            Texture2D iconTexture)
        {
            if (string.IsNullOrEmpty(baseAbilityName) || string.IsNullOrEmpty(newAbilityName))
            {
                Plugin.Logger.LogWarning("[OctoLib] NewAbilityWithBase: base or new name is empty!");
                return;
            }

            var ownerAssembly = GetCallingAssembly();

            _abilitywithbase.Add(new BaseAbilityRegistration
            {
                BaseAbilityName = baseAbilityName,
                NewAbilityName = newAbilityName,
                IconTexture = iconTexture,
                OwnerAssembly = ownerAssembly
            });

            Plugin.Logger.LogInfo($"[OctoLib] Registered: {newAbilityName} (based on '{baseAbilityName}') from {ownerAssembly.GetName().Name}");
        }

        public static void NewAbility(
            string newAbilityName,
            Texture2D iconTexture,
            GameObject GameObjectPrefab)
        {
            if (string.IsNullOrEmpty(newAbilityName))
            {
                Plugin.Logger.LogWarning("[OctoLib] NewAbilityWithBase: base or new name is empty!");
                return;
            }

            var ownerAssembly = GetCallingAssembly();

            _ability.Add(new CustomAbilityRegistration
            {
                NewAbilityName = newAbilityName,
                IconTexture = iconTexture,
                GameObjectPrefab = GameObjectPrefab,
                OwnerAssembly = ownerAssembly
            });

            Plugin.Logger.LogInfo($"[OctoLib] Registered: {newAbilityName} from {ownerAssembly.GetName().Name}");
        }

        public static void AbilitySetCooldown(string AbilityName, Fix Cooldown)
        {
            if (string.IsNullOrEmpty(AbilityName))
            {
                Plugin.Logger.LogWarning("[OctoLib] AbilitySetCooldown: name is empty!");
                return;
            }

            _cooldowns.Add(new AbilityCooldown
            {
                AbilityName = AbilityName,
                Cooldown = Cooldown
            });
        }

        public static void AbilitySetOffensive(string AbilityName, bool Offensive)
        {
            if (string.IsNullOrEmpty(AbilityName))
            {
                Plugin.Logger.LogWarning("[OctoLib] AbilitySetCooldown: name is empty!");
                return;
            }

            _offensive.Add(new AbilityOffensive
            {
                AbilityName = AbilityName,
                Offensive = Offensive
            });
        }

        public static void AbilitySetBackground(string AbilityName, Texture2D Background)
        {
            if (string.IsNullOrEmpty(AbilityName))
            {
                Plugin.Logger.LogWarning("[OctoLib] AbilitySetCooldown: name is empty!");
                return;
            }

            var ownerAssembly = GetCallingAssembly();

            _backgrounds.Add(new AbilityBackground
            {
                AbilityName = AbilityName,
                Background = Background,
                OwnerAssembly = ownerAssembly
            });
        }

        [HarmonyPatch(typeof(SteamManager), "Awake")]
        public static class SteamManagerPatch
        {
            [HarmonyPostfix]
            public static void Postfix(SteamManager __instance, ref NamedSpriteList ___abilityIconsFull)
            {
                if (___abilityIconsFull.sprites.Count == 30)
                {
                    foreach (var reg in _ability)
                    {
                        GameObject.DontDestroyOnLoad(reg.GameObjectPrefab);

                        Texture2D AbilityTexture = reg.IconTexture;
                        Texture2D AbilityBackground = GetBackgroundTexture(reg.NewAbilityName);

                        Sprite newSprite;

                        Texture2D AbilityTextureWithBackground = CreateAbilityTexture(AbilityTexture, AbilityBackground);
                        newSprite = Sprite.Create(
                            AbilityTextureWithBackground,
                            new Rect(0f, 0f, (float)AbilityTexture.width, (float)AbilityTexture.height),
                            new Vector2(0.5f, 0.5f),
                            100u,
                            0u,
                            SpriteMeshType.FullRect,
                            Vector2.zero,
                            false
                        );

                        bool IsOffensive = GetIsOffensive(reg.NewAbilityName);

                        NamedSprite NewNamedSprite = new NamedSprite(reg.NewAbilityName, newSprite, reg.GameObjectPrefab, IsOffensive);
                        ___abilityIconsFull.sprites.Add(NewNamedSprite);
                    }
                }
            }
        }



        [HarmonyPatch(typeof(AbilityGrid), "Awake")]
        private static class AbilityGridAwakePatch
        {
            [HarmonyPrefix]
            public static void Prefix(AbilityGrid __instance)
            {
                if (_hasInjected || _abilitywithbase.Count == 0) return;

                var traverse = Traverse.Create(__instance);
                var abilityIcons = traverse.Field("abilityIcons").GetValue<NamedSpriteList>();

                if (abilityIcons?.sprites == null)
                {
                    Plugin.Logger.LogWarning("[OctoLib] Could not find abilityIcons list!");
                    return;
                }

                if (Plugin.debugLogged.Value)
                {
                    foreach (var sprite in abilityIcons.sprites)
                    {
                        Plugin.Logger.LogWarning($"[OctoLib] Ability Names: '{sprite.name}'");
                    }
                }

                foreach (var reg in _abilitywithbase)
                {
                    InjectAbility(abilityIcons, reg);
                }

                foreach (var reg in _ability)
                {
                    InjectCustomAbility(abilityIcons, reg);
                }

                _hasInjected = true;
            }
        }

        private static void InjectAbility(NamedSpriteList list, BaseAbilityRegistration reg)
        {
            Plugin.Logger.LogWarning($"[OctoLib] Start adding ability: '{reg.NewAbilityName}'");
            NamedSprite baseSprite = default;
            NamedSprite RandomSprite = default;
            bool found = false;

            foreach (var sprite in list.sprites)
            {
                if (sprite.name.Equals("Random"))
                {
                    RandomSprite = sprite;
                    Plugin.Logger.LogWarning($"[OctoLib] Random Sprite found!");
                }


                if (sprite.name.Equals(reg.BaseAbilityName))
                {
                    baseSprite = sprite;
                    found = true;
                }
            }

            if (!found)
            {
                Plugin.Logger.LogWarning($"[OctoLib] Base ability '{reg.BaseAbilityName}' not found!");
                return;
            }

            GameObject newGO = UnityEngine.Object.Instantiate(baseSprite.associatedGameObject);
            UnityEngine.Object.DontDestroyOnLoad(newGO);
            newGO.name = reg.NewAbilityName;
            Fix Cooldown = GetCooldown(reg.NewAbilityName);
            if (Cooldown != (Fix)(-1f))
            {
                Ability component = newGO.GetComponent<Ability>();
                if (component != null)
                {
                    component.Cooldown = Cooldown;
                }
            }

            Texture2D AbilityTexture = reg.IconTexture;
            Texture2D AbilityBackground = GetBackgroundTexture(reg.NewAbilityName);

            Sprite newSprite;
            if (AbilityTexture != null)
            {
                Texture2D AbilityTextureWithBackground = CreateAbilityTexture(AbilityTexture, AbilityBackground);
                newSprite = Sprite.Create(
                    AbilityTextureWithBackground,
                    new Rect(0f, 0f, (float)AbilityTexture.width, (float)AbilityTexture.height),
                    new Vector2(0.5f, 0.5f),
                    RandomSprite.sprite.pixelsPerUnit,
                    0u,
                    SpriteMeshType.FullRect,
                    Vector2.zero,
                    false
                );
                newSprite.texture.filterMode = RandomSprite.sprite.texture.filterMode;
                newSprite.texture.wrapMode = RandomSprite.sprite.texture.wrapMode;
            }
            else
            {
                //Plugin.Logger.LogWarning($"[OctoLib] Icon not found: {reg.IconResourceName}. Using base icon.");
                newSprite = RandomSprite.sprite;
            }

            bool IsOffensive = GetIsOffensive(reg.NewAbilityName);

            NamedSprite newNamedSprite = new NamedSprite(reg.NewAbilityName, newSprite, newGO, IsOffensive);
            list.sprites.Add(newNamedSprite);

            Plugin.Logger.LogInfo($"[OctoLib] Injected: {reg.NewAbilityName}");
        }

        private static void InjectCustomAbility(NamedSpriteList list, CustomAbilityRegistration reg)
        {
            Plugin.Logger.LogWarning($"[OctoLib] Start adding ability: '{reg.NewAbilityName}'");
            NamedSprite RandomSprite = default;
            GameObject.DontDestroyOnLoad(reg.GameObjectPrefab);

            foreach (var sprite in list.sprites)
            {
                if (sprite.name.Equals("Random"))
                {
                    RandomSprite = sprite;
                    Plugin.Logger.LogWarning($"[OctoLib] Random sprite found!");
                    break;
                }
            }

            Fix Cooldown = GetCooldown(reg.NewAbilityName);
            if (Cooldown != (Fix)(-1f))
            {
                Ability component = reg.GameObjectPrefab.GetComponent<Ability>();
                if (component != null)
                {
                    component.Cooldown = Cooldown;
                }
            }

            Texture2D AbilityTexture = reg.IconTexture;
            Texture2D AbilityBackground = GetBackgroundTexture(reg.NewAbilityName);

            Sprite newSprite;
            if (AbilityTexture != null)
            {
                Texture2D AbilityTextureWithBackground = CreateAbilityTexture(AbilityTexture, AbilityBackground);
                newSprite = Sprite.Create(
                    AbilityTextureWithBackground,
                    new Rect(0f, 0f, (float)AbilityTexture.width, (float)AbilityTexture.height),
                    new Vector2(0.5f, 0.5f),
                    RandomSprite.sprite.pixelsPerUnit,
                    0u,
                    SpriteMeshType.FullRect,
                    Vector2.zero,
                    false
                );
                newSprite.texture.filterMode = RandomSprite.sprite.texture.filterMode;
                newSprite.texture.wrapMode = RandomSprite.sprite.texture.wrapMode;
            }
            else
            {
                //Plugin.Logger.LogWarning($"[OctoLib] Icon not found: {reg.IconResourceName}. Using base icon.");
                newSprite = RandomSprite.sprite;
            }

            bool IsOffensive = GetIsOffensive(reg.NewAbilityName);

            NamedSprite newNamedSprite = new NamedSprite(reg.NewAbilityName, newSprite, reg.GameObjectPrefab, IsOffensive);
            list.sprites.Add(newNamedSprite);

            Plugin.Logger.LogInfo($"[OctoLib] Injected: {reg.NewAbilityName}");
        }

        private static Assembly GetCallingAssembly()
        {
            var stack = new System.Diagnostics.StackTrace(1, false);
            for (int i = 0; i < stack.FrameCount; i++)
            {
                var asm = stack.GetFrame(i).GetMethod()?.DeclaringType?.Assembly;
                if (asm != null && asm != typeof(Abilities).Assembly && asm != typeof(Textures).Assembly)
                    return asm;
            }
            return Assembly.GetExecutingAssembly();
        }

        private static bool GetIsOffensive(string abilityName)
        {
            foreach (var off in _offensive)
                if (off.AbilityName == abilityName)
                    return off.Offensive;
            return false;
        }

        private static Fix GetCooldown(string abilityName)
        {          
            foreach (var coldwn in _cooldowns)
                if (abilityName == coldwn.AbilityName)
                    return coldwn.Cooldown;
            return (Fix)(-1);
        }

        private static Texture2D GetBackgroundTexture(string abilityName)
        {
            foreach (var bg in _backgrounds)
                if (bg.AbilityName == abilityName)
                    return bg.Background;
            return null;
        }

        private static Texture2D CreateAbilityTexture(Texture2D AbilityTexture, Texture2D AbilityBGTexture = null)
        {
            if (AbilityBGTexture == null)
                AbilityBGTexture = Textures.LoadFromAssembly("AbilityBg", Plugin.OctoLibAssembly);

            Texture2D NewTexture = new Texture2D(2048, 2048, TextureFormat.RGBA32, true);

            Color[] blankPixels = new Color[2048 * 2048];
            for (int i = 0; i < blankPixels.Length; i++)
                blankPixels[i] = Color.clear;
            NewTexture.SetPixels(blankPixels);

            if (AbilityBGTexture != null)
            {
                Color[] bgPixels = AbilityBGTexture.GetPixels();
                int bgWidth = AbilityBGTexture.width;
                int bgHeight = AbilityBGTexture.height;

                int startX = 0;
                int startY = 2048 - bgHeight;

                for (int y = 0; y < bgHeight; y++)
                {
                    for (int x = 0; x < bgWidth; x++)
                    {
                        if (startX + x < 2048 && startY + y < 2048)
                        {
                            NewTexture.SetPixel(startX + x, startY + y, bgPixels[y * bgWidth + x]);
                        }
                    }
                }
            }
            if (AbilityTexture != null)
            {
                Color[] abilityPixels = AbilityTexture.GetPixels();
                int abilityWidth = AbilityTexture.width;
                int abilityHeight = AbilityTexture.height;

                int startX = 0;
                int startY = 0;

                for (int y = 0; y < abilityHeight; y++)
                {
                    for (int x = 0; x < abilityWidth; x++)
                    {
                        if (startX + x < 2048 && startY + y < 2048)
                        {
                            NewTexture.SetPixel(startX + x, startY + y, abilityPixels[y * abilityWidth + x]);
                        }
                    }
                }
            }

            NewTexture.Apply();
            return NewTexture;
        }
    }
}
