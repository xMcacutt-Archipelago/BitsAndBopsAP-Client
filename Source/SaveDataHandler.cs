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
        public CustomSaveData APSaveData = null!;
        private string _saveDataPath = "";
        private string _customDataPath = "";
        private string _tmpPath = "";
        private string _bakPath = "";
        
        public void GetSave(string seed, string slot)
        {
            _saveDataPath = $"./ArchipelagoSaves/{slot}_{seed}.dat";
            _tmpPath = $"./ArchipelagoSaves/{slot}_{seed}.tmp";
            _bakPath = $"./ArchipelagoSaves/{slot}_{seed}.bak";
            _customDataPath = $"./ArchipelagoSaves/{slot}_{seed}.json";
            if (File.Exists(_customDataPath))
            {
                var data = JsonConvert.DeserializeObject<CustomSaveData>(File.ReadAllText(_customDataPath));
                APSaveData = data ?? new CustomSaveData();
            }
            else
                APSaveData = new CustomSaveData();
            SaveDataManager.Instance.savePath = _saveDataPath;
            SaveDataManager.Instance.bakPath = _bakPath;
            SaveDataManager.Instance.tmpPath = _tmpPath;
            SaveDataManager.Instance.Load();
            SaveDataManager.saveData.shopBestOverride = true;
            SaveDataManager.saveData.gameEvents[GameEvent.MultiplayerOpen] = EventState.Complete;
            SaveDataManager.saveData.gameEvents[GameEvent.MultiplayerIntro] = EventState.Complete;
            SaveDataManager.saveData.unlockEvents[Stage.Clock] = EventState.Complete;
            SaveGame();
        }
        
        public void SaveGame()
        {
            if (_saveDataPath == "" || _customDataPath == "")
            {
                APConsole.Instance.DebugLog($"Failed to save game! SaveDataPath: {_saveDataPath}, CustomDataPath: {_customDataPath}");
                return;
            }
            File.WriteAllText(_customDataPath, JsonConvert.SerializeObject(APSaveData));
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