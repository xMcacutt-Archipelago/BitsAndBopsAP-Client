using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using Random = System.Random;
using BepInEx.Configuration;

namespace BitsAndBops_AP_Client
{
    [BepInPlugin(Guid, Name, Version)]
    public class PluginMain : BaseUnityPlugin
    {
        public static ConfigEntry<bool>? EnableDebugLogging;
        public const string GameName = "Bits & Bops";
        private const string Guid = "bits_and_bops_ap_client";
        private const string Name = "BitsAndBopsAPClient";
        private const string Version = "1.0.4";
        
        public static ManualLogSource logger = null!;
        private readonly Harmony _harmony = new(Guid);
        public static ArchipelagoHandler ArchipelagoHandler = null!;
        public static SaveDataHandler SaveDataHandler = null!;
        public static GameHandler GameHandler = null!;
        public static ItemHandler ItemHandler = null!;
        public static SlotData SlotData = null!;
        public static Random Random = new();
        
        private void Awake()
        { 
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
                GameHandler.JudgementDictionaryPatch.DeathChance = 0;
                GameHandler.JudgementDictionaryPatch.TroublemakerFlag = true;
            };
            
            EnableDebugLogging = Config.Bind(
                "Logging",
                "EnableDebugLogging",
                false,
                "Enables or disables debug logging in the Archipelago Console."
            );
        }

    
    }
}