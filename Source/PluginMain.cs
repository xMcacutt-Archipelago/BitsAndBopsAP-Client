using System;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;
using BepInEx.Configuration;

namespace BitsAndBops_AP_Client
{
    [BepInPlugin(Guid, Name, Version)]
    public class PluginMain : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableDebugLogging;
        public const string GameName = "Bits & Bops";
        private const string Guid = "bits_and_bops_ap_client";
        private const string Name = "BitsAndBopsAPClient";
        private const string Version = "1.0.3";
        public static ManualLogSource logger;
        private readonly Harmony _harmony = new(Guid);
        public static ArchipelagoHandler ArchipelagoHandler;
        public static SaveDataHandler SaveDataHandler;
        public static GameHandler GameHandler;
        public static ItemHandler ItemHandler;
        public static SlotData SlotData;
        public static Random random;
        
        private void Awake()
        {
            random = new Random();
            _harmony.PatchAll();
            logger = Logger;
            SaveDataHandler = new SaveDataHandler();
            ArchipelagoHandler = gameObject.AddComponent<ArchipelagoHandler>();
            GameHandler = gameObject.AddComponent<GameHandler>();
            ItemHandler = gameObject.AddComponent<ItemHandler>();

            SceneManager.sceneLoaded += (newScene, mode) =>
            {
                if (newScene.name != "StageSelectShop") 
                    return;
                ItemHandler.FlushQueue();
                GameHandler.JudgementDictionary_Patch.deathChance = 0;
                GameHandler.JudgementDictionary_Patch.troublemakerFlag = true;
            };
            
            enableDebugLogging = Config.Bind(
                "Logging",
                "EnableDebugLogging",
                false,
                "Enables or disables debug logging in the Archipelago Console."
            );
        }

    
    }
}