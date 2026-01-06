using System.IO;
using HarmonyLib;
using Newtonsoft.Json;

namespace BitsAndBops_AP_Client
{
    public class CustomSaveData
    {
        public int ItemIndex;
    }
    
    public class SaveDataHandler
    {
        public CustomSaveData apSaveData;
        private string saveDataPath;
        private string customDataPath;
        private string tmpPath;
        private string bakPath;
        
        public void GetSave(string seed, string slot)
        {
            saveDataPath = $"./ArchipelagoSaves/{slot}_{seed}.dat";
            tmpPath = $"./ArchipelagoSaves/{slot}_{seed}.tmp";
            bakPath = $"./ArchipelagoSaves/{slot}_{seed}.bak";
            customDataPath = $"./ArchipelagoSaves/{slot}_{seed}.json";
            if (File.Exists(customDataPath))
                apSaveData = JsonConvert.DeserializeObject<CustomSaveData>(File.ReadAllText(customDataPath));
            else
                apSaveData = new CustomSaveData();
            SaveDataManager.Instance.savePath = saveDataPath;
            SaveDataManager.Instance.bakPath = bakPath;
            SaveDataManager.Instance.tmpPath = tmpPath;
            SaveDataManager.Instance.Load();
            SaveDataManager.saveData.shopBestOverride = true;
            SaveDataManager.saveData.gameEvents[GameEvent.MultiplayerOpen] = EventState.Complete;
            SaveDataManager.saveData.gameEvents[GameEvent.MultiplayerIntro] = EventState.Complete;
            SaveDataManager.saveData.unlockEvents[Stage.Clock] = EventState.Complete;
            SaveGame();
        }
        
        public void SaveGame()
        {
            if (saveDataPath == "" || customDataPath == "")
            {
                APConsole.Instance.DebugLog($"Failed to save game! SaveDataPath: {saveDataPath}, CustomDataPath: {customDataPath}");
                return;
            }
            File.WriteAllText(customDataPath, JsonConvert.SerializeObject(apSaveData));
            SaveDataManager.Instance.Save();
        }
        
        [HarmonyPatch(typeof(SaveDataManager))]
        public class SaveDataManager_Patch
        {
            [HarmonyPatch(nameof(SaveDataManager.InitPaths))]
            [HarmonyPrefix]
            private static bool InitPaths(SaveDataManager __instance, int slotIndex)
            {
                SaveDataManager.ValidateSlotIndex(slotIndex);
                if (!Directory.Exists("./ArchipelagoSaves"))
                    Directory.CreateDirectory("./ArchipelagoSaves");
                __instance.savePath = $"./ArchipelagoSaves/{$"{"save"}{slotIndex + 1}"}.dat";
                __instance.tmpPath = __instance.savePath + ".tmp";
                __instance.bakPath = __instance.savePath + ".bak";
                __instance.freezerPath = "./ArchipelagoSaves/freezer";
                return false;
            }
        }
    }
}