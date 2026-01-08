using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BitsAndBops_AP_Client
{
    public enum BaBItem
    {
        FlipperSnapper = 0x1,
        SweetTooth = 0x2,
        RockPaperShowdown = 0x3,
        PantryParade = 0x4,
        JungleMixtape = 0x5,
        BBotAndTheFlyGirls = 0x6,
        FlowWorms = 0x7,
        MeetAndTweet = 0x8,
        SteadyBears = 0x9,
        SkyMixtape = 0xA,
        PopUpKitchen = 0xB,
        FireworkFestival = 0xC,
        HammerTime = 0xD,
        Molecano = 0xE,
        OceanMixtape = 0xF,
        PresidentBird = 0x10,
        Snakedown = 0x11,
        Octeaparty = 0x12,
        GlobeTrotters = 0x13,
        FireMixtape = 0x14,
        SymphonyCartridge = 0x15,
        ThreeLeggedRaceCartridge = 0x16,
        BlacksmithCartridge = 0x17,
        EncoreCartridge = 0x18,
        FlipperSnapperRecord = 0x19,
        SweetToothRecord = 0x1A,
        RockPaperShowdownRecord = 0x1B,
        PantryParadeRecord = 0x1C,
        JungleMixtapeRecord = 0x1D,
        BBotAndTheFlyGirlsRecord = 0x1E,
        FlowWormsRecord = 0x1F,
        MeetAndTweetRecord = 0x20,
        SteadyBearsRecord = 0x21,
        SkyMixtapeRecord = 0x22,
        PopUpKitchenRecord = 0x23,
        FireworkFestivalRecord = 0x24,
        HammerTimeRecord = 0x25,
        MolecanoRecord = 0x26,
        OceanMixtapeRecord = 0x27,
        PresidentBirdRecord = 0x28,
        SnakedownRecord = 0x29,
        OcteapartyRecord = 0x2A,
        GlobeTrottersRecord = 0x2B,
        FireMixtapeRecord = 0x2C,
        RandomSouvenir = 0x100,
        RandomVideotape = 0x101,
    }

    public class ItemHandler : MonoBehaviour
    {
        private Queue<(int, ItemInfo)> cachedItems = new Queue<(int, ItemInfo)>();

        private bool IsGameReady()
        {
            return SceneManager.GetActiveScene().name == "StageSelectShop";
        }

        public void HandleItem(int index, ItemInfo item, bool save = true)
        {
            try
            {
                if (!IsGameReady())
                {
                    APConsole.Instance.DebugLog($"Game not ready, caching item: {item.ItemName} (index {index})");
                    cachedItems.Enqueue((index, item));
                    return;
                }

                if (cachedItems.Count > 0)
                {
                    APConsole.Instance.DebugLog($"Processing {cachedItems.Count} cached items...");
                    FlushQueue();
                }

                ProcessItem(index, item);
            }
            catch (Exception ex)
            {
                APConsole.Instance.DebugLog($"HandleItem Error: {ex}");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                FlushQueue();
                GameHandler.CheckGoal();
            }
        }
        
        public void FlushQueue()
        {
            if (!IsGameReady())
            {
                APConsole.Instance.DebugLog("Attempted to flush queue but game is not ready");
                return;
            }

            int processedCount = 0;
            while (cachedItems.Count > 0)
            {
                var (index, item) = cachedItems.Dequeue();
                ProcessItem(index, item);
                processedCount++;
            }

            APConsole.Instance.DebugLog($"Flushed {processedCount} cached items");
            if (processedCount > 0)
                PluginMain.SaveDataHandler.SaveGame();
        }

        private void ProcessItem(int index, ItemInfo item)
        {
            var apSaveData = PluginMain.SaveDataHandler.apSaveData;
            if (index < apSaveData.ItemIndex)
            {
                APConsole.Instance.DebugLog($"Item {index} already processed (current: {apSaveData.ItemIndex})");
                return;
            }

            apSaveData.ItemIndex++;

            switch ((BaBItem)item.ItemId)
            {
                case BaBItem.FlipperSnapper:
                    GameManager.UnlockEvents[Stage.FlipperSnapper] = EventState.Available;
                    break;
                case BaBItem.SweetTooth:
                    GameManager.UnlockEvents[Stage.SweetTooth] = EventState.Available;
                    break;
                case BaBItem.RockPaperShowdown:
                    GameManager.UnlockEvents[Stage.RockPaperShowdown] = EventState.Available;
                    break;
                case BaBItem.PantryParade:
                    GameManager.UnlockEvents[Stage.PantryParade] = EventState.Available;
                    break;
                case BaBItem.JungleMixtape:
                    GameManager.UnlockEvents[Stage.Mixtape1] = EventState.Available;
                    break;
                case BaBItem.BBotAndTheFlyGirls:
                    GameManager.UnlockEvents[Stage.BBot] = EventState.Available;
                    break;
                case BaBItem.FlowWorms:
                    GameManager.UnlockEvents[Stage.FlowWorms] = EventState.Available;
                    break;
                case BaBItem.MeetAndTweet:
                    GameManager.UnlockEvents[Stage.MeetAndTweet] = EventState.Available;
                    break;
                case BaBItem.SteadyBears:
                    GameManager.UnlockEvents[Stage.SteadyBears] = EventState.Available;
                    break;
                case BaBItem.SkyMixtape:
                    GameManager.UnlockEvents[Stage.Mixtape2] = EventState.Available;
                    break;
                case BaBItem.PopUpKitchen:
                    GameManager.UnlockEvents[Stage.PopUpKitchen] = EventState.Available;
                    break;
                case BaBItem.FireworkFestival:
                    GameManager.UnlockEvents[Stage.FireworkFestival] = EventState.Available;
                    break;
                case BaBItem.HammerTime:
                    GameManager.UnlockEvents[Stage.HammerTime] = EventState.Available;
                    break;
                case BaBItem.Molecano:
                    GameManager.UnlockEvents[Stage.Molecano] = EventState.Available;
                    break;
                case BaBItem.OceanMixtape:
                    GameManager.UnlockEvents[Stage.Mixtape3] = EventState.Available;
                    break;
                case BaBItem.PresidentBird:
                    GameManager.UnlockEvents[Stage.PresidentBird] = EventState.Available;
                    break;
                case BaBItem.Snakedown:
                    GameManager.UnlockEvents[Stage.Snakedown] = EventState.Available;
                    break;
                case BaBItem.Octeaparty:
                    GameManager.UnlockEvents[Stage.Octeaparty] = EventState.Available;
                    break;
                case BaBItem.GlobeTrotters:
                    GameManager.UnlockEvents[Stage.GlobeTrotters] = EventState.Available;
                    break;
                case BaBItem.FireMixtape:
                    GameManager.UnlockEvents[Stage.Mixtape4] = EventState.Available;
                    break;
                case BaBItem.SymphonyCartridge:
                    GameManager.UnlockEvents[Stage.Symphony] = EventState.Available;
                    break;
                case BaBItem.ThreeLeggedRaceCartridge:
                    GameManager.UnlockEvents[Stage.ThreeLeggedRace] = EventState.Available;
                    break;
                case BaBItem.BlacksmithCartridge:
                    GameManager.UnlockEvents[Stage.Blacksmith] = EventState.Available;
                    break;
                case BaBItem.EncoreCartridge:
                    GameManager.UnlockEvents[Stage.Encore] = EventState.Available;
                    break;
                case BaBItem.FlipperSnapperRecord:
                    GameManager.TryEnableEvent(RecordUnlock.FlipperSnapper, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.SweetToothRecord:
                    GameManager.TryEnableEvent(RecordUnlock.SweetTooth, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.RockPaperShowdownRecord:
                    GameManager.TryEnableEvent(RecordUnlock.RockPaperShowdown, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.PantryParadeRecord:
                    GameManager.TryEnableEvent(RecordUnlock.PantryParade, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.JungleMixtapeRecord:
                    GameManager.TryEnableEvent(RecordUnlock.Mixtape1, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.BBotAndTheFlyGirlsRecord:
                    GameManager.TryEnableEvent(RecordUnlock.BBot, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.FlowWormsRecord:
                    GameManager.TryEnableEvent(RecordUnlock.FlowWorms, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.MeetAndTweetRecord:
                    GameManager.TryEnableEvent(RecordUnlock.MeetAndTweet, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.SteadyBearsRecord:
                    GameManager.TryEnableEvent(RecordUnlock.SteadyBears, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.SkyMixtapeRecord:
                    GameManager.TryEnableEvent(RecordUnlock.Mixtape2, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.PopUpKitchenRecord:
                    GameManager.TryEnableEvent(RecordUnlock.PopUpKitchen, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.FireworkFestivalRecord:
                    GameManager.TryEnableEvent(RecordUnlock.FireworkFestival, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.HammerTimeRecord:
                    GameManager.TryEnableEvent(RecordUnlock.HammerTime, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.MolecanoRecord:
                    GameManager.TryEnableEvent(RecordUnlock.Molecano, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.OceanMixtapeRecord:
                    GameManager.TryEnableEvent(RecordUnlock.Mixtape3, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.PresidentBirdRecord:
                    GameManager.TryEnableEvent(RecordUnlock.PresidentBird, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.SnakedownRecord:
                    GameManager.TryEnableEvent(RecordUnlock.Snakedown, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.OcteapartyRecord:
                    GameManager.TryEnableEvent(RecordUnlock.Octeaparty, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.GlobeTrottersRecord:
                    GameManager.TryEnableEvent(RecordUnlock.GlobeTrotters, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.FireMixtapeRecord:
                    GameManager.TryEnableEvent(RecordUnlock.Mixtape4, GameManager.RecordUnlockEvents);
                    break;
                case BaBItem.RandomSouvenir:
                    var trinkets = Enum.GetValues(typeof(TrinketUnlock)).Cast<TrinketUnlock>().ToList();
                    var available = trinkets.Where(x =>
                        !GameManager.TrinketUnlockEvents.TryGetValue(x, out var trinket) ||
                        trinket == EventState.Unavailable);
                    var trinketUnlocks = available as TrinketUnlock[] ?? available.ToArray();
                    if (!trinketUnlocks.Any())
                        break;
                    GameManager.TryEnableEvent(trinketUnlocks.OrderBy(x => PluginMain.random.Next()).First(), GameManager.TrinketUnlockEvents);
                    break;
                case BaBItem.RandomVideotape:                   
                    var tapes = Enum.GetValues(typeof(VideoUnlock)).Cast<VideoUnlock>().ToList();
                    var availableTapes = tapes.Where(x =>
                        !GameManager.VideoUnlockEvents.TryGetValue(x, out var trinket) ||
                        trinket == EventState.Unavailable);
                    var tapeUnlocks = availableTapes as VideoUnlock[] ?? availableTapes.ToArray();
                    if (!tapeUnlocks.Any())
                        break;
                    GameManager.TryEnableEvent(tapeUnlocks.OrderBy(x => PluginMain.random.Next()).First(), GameManager.VideoUnlockEvents);
                    break;
                default:
                    Log.Warning($"Unknown item: {item.ItemId} ({item.ItemName})");
                    break;
            }

            FindObjectOfType<CounterScript>()?.SetMailActive();
            PluginMain.SaveDataHandler.SaveGame();
        }
    }
}