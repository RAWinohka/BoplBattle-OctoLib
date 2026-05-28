using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OctoLib
{
    [BepInPlugin("com.OctoLab.OctoLib", "OctoLib", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        public static ConfigEntry<bool> debugLogged;

        public static Assembly OctoLibAssembly;


        private void Awake()
        {
            Logger = base.Logger;

            OctoLibAssembly = Assembly.GetExecutingAssembly();

            debugLogged = base.Config.Bind<bool>("Debug", "DebugLogger", false, "Show some debug logs");

            var harmony = new Harmony("com.OctoLab.OctoLib");
            harmony.PatchAll();

            Logger.LogInfo("OctoLib is working");
        }
    }
}
