using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using Random = System.Random;
using BepInEx.Configuration;
using Steamworks;

namespace BitsAndBops_AP_Client
{
    [BepInPlugin(Guid, Name, Version)]
    public class PluginMain : BaseUnityPlugin
    {
        public static ConfigEntry<bool>? EnableDebugLogging;
        public static ConfigEntry<bool>? FilterLog;
        public static ConfigEntry<float>? MessageInTime;
        public static ConfigEntry<float>? MessageHoldTime;
        public static ConfigEntry<float>? MessageOutTime;
        public const string GameName = "Bits & Bops";
        private const string Guid = "bits_and_bops_ap_client";
        private const string Name = "BitsAndBopsAPClient";
        private const string Version = "1.0.7";
        
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
            ConnectionScreenHandler.OnKeyboardDismissed = Callback<GamepadTextInputDismissed_t>.Create(
                ConnectionScreenHandler.FileSelectScriptPatch.KeyboardDismissed
            );
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
            
            FilterLog = Config.Bind(
                "Logging",
                "FilterLog",
                false,
                "Filter the archipelago log to only show messages relevant to you."
            );
            
            MessageInTime = Config.Bind(
                "Logging",
                "MessageInTime",
                    0.25f,
                "How long messages take to animate in."
            );
            
            MessageHoldTime = Config.Bind(
                "Logging",
                "MessageHoldTime",
                3f,
                "How long messages stay in the log before animating out."
            );
            
            MessageOutTime = Config.Bind(
                "Logging",
                "MessageOutTime",
                0.5f,
                "How long messages stay in the log before animating out."
            );
        }
    }
}