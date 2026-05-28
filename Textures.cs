using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OctoLib
{
    public static class Textures
    {
        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        public static Texture2D Load(string name, bool usePointFilter = true)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_cache.TryGetValue(name, out var cached))
                return cached;

            byte[] data = LoadRawBytes(name);
            if (data == null)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (usePointFilter)
                texture.filterMode = FilterMode.Point;

            texture.LoadImage(data, false);
            texture.name = name;

            _cache[name] = texture;
            return texture;
        }

        public static byte[] LoadRawBytes(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            Assembly callerAssembly = GetCallingAssembly();

            string[] allResources = callerAssembly.GetManifestResourceNames();

            if (Plugin.debugLogged.Value)
            {
                Plugin.Logger.LogInfo("=== Embedded Resources in " + callerAssembly.GetName().Name + " ===");
                foreach (var res in allResources)
                    Plugin.Logger.LogInfo("  - " + res);
                Plugin.Logger.LogInfo("=====================================");
            }

            string resourceName = FindResourceName(allResources, name);
            if (resourceName != null)
            {
                return LoadFromResource(callerAssembly, resourceName);
            }

            string dir = Path.GetDirectoryName(callerAssembly.Location);
            string[] possiblePaths =
            {
            Path.Combine(dir, name),
            Path.Combine(dir, name + ".png"),
            Path.Combine(dir, name.ToLower() + ".png")
        };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Plugin.Logger.LogInfo($"[OctoLib] Loaded from file: {path}");
                    return File.ReadAllBytes(path);
                }
            }

            Plugin.Logger.LogWarning($"[OctoLib] Texture not found: {name}");
            return null;
        }

        private static Assembly GetCallingAssembly()
        {
            var stack = new System.Diagnostics.StackTrace(1, false);
            for (int i = 0; i < stack.FrameCount; i++)
            {
                var frame = stack.GetFrame(i);
                var asm = frame.GetMethod()?.DeclaringType?.Assembly;
                if (asm != null && asm != typeof(Textures).Assembly)
                    return asm;
            }
            return Assembly.GetExecutingAssembly();
        }

        private static string FindResourceName(string[] allResources, string name)
        {
            string lowerName = name.ToLower();

            foreach (var res in allResources)
            {
                if (res.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    res.Equals(name + ".png", StringComparison.OrdinalIgnoreCase) ||
                    res.ToLower().Contains(lowerName))
                {
                    return res;
                }
            }
            return null;
        }

        private static byte[] LoadFromResource(Assembly asm, string resourceName)
        {
            Stream stream = null;
            try
            {
                stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null) return null;

                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                Plugin.Logger.LogInfo($"[OctoLib] Loaded embedded: {resourceName}");
                return bytes;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        public static Texture2D LoadFromAssembly(string name, Assembly assembly, bool usePointFilter = true)
        {
            if (string.IsNullOrEmpty(name) || assembly == null)
                return null;

            byte[] data = LoadRawBytesFromAssembly(name, assembly);
            if (data == null)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (usePointFilter)
                texture.filterMode = FilterMode.Point;

            texture.LoadImage(data, false);
            texture.name = name;

            _cache[name] = texture;
            return texture;
        }

        private static byte[] LoadRawBytesFromAssembly(string name, Assembly assembly)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string[] allResources = assembly.GetManifestResourceNames();

            string resourceName = FindResourceName(allResources, name);
            if (resourceName == null)
                return null;

            return LoadFromResource(assembly, resourceName);
        }

        public static Texture2D ExtractTextureFromSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                Debug.LogError("[SpriteExtractor] Sprite is null!");
                return null;
            }

            Texture2D originalTexture = sprite.texture;

            if (originalTexture == null)
            {
                Debug.LogError("[SpriteExtractor] Sprite texture is null!");
                return null;
            }

            Rect rect = sprite.rect;

            Texture2D extractedTexture = new Texture2D(
                (int)rect.width,
                (int)rect.height,
                originalTexture.format,
                false
            );

            Color[] pixels = originalTexture.GetPixels(
                (int)rect.x,
                (int)rect.y,
                (int)rect.width,
                (int)rect.height
            );

            extractedTexture.SetPixels(pixels);
            extractedTexture.Apply();

            return extractedTexture;
        }

        public static void ClearCache() => _cache.Clear();
    }
}
