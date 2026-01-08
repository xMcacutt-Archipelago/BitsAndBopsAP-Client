using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using CollectionExtensions = System.Collections.Generic.CollectionExtensions;
using Random = System.Random;

namespace BitsAndBops_AP_Client
{
    public class GameHandler : MonoBehaviour
    {
        public void Kill()
        {
            var pause = PauseScript.Instance;
            if (pause == null || !pause.isGame)
            {
                APConsole.Instance.DebugLog("Pause is null probably");
                return;
            }
            var quitScene = new SceneKey?(SceneKey.StageSelect);
            pause.unpauseSound.Play();
            pause.quitter.PreventQuit = false;
            pause.quitter.Quit(audioDuration: 0.25f, quitScene: quitScene);
        }
        
        [HarmonyPatch(typeof(JudgementDictionary))]
        public static class JudgementDictionary_Patch
        {
            private static Random random = new Random();
            public static double deathChance = 0;
            public static bool troublemakerFlag = true;
            
            [HarmonyPatch(nameof(JudgementDictionary.IncrementAtomic))]
            [HarmonyPostfix]
            [HarmonyPatch([typeof(Judgement), typeof(string), typeof(Action), typeof(float)])]
            public static void IncrementAtomic(Judgement judgement, string type, Action action, float target)
            {
                if (judgement is Judgement.Perfect or Judgement.Hit)
                    troublemakerFlag = false;
                if (troublemakerFlag)
                    return;
                if (judgement == Judgement.Miss)
                {
                    var rng = random.NextDouble();
                    if (rng < deathChance)
                    {
                        PluginMain.ArchipelagoHandler.SendDeath();
                        deathChance = 0;
                    }
                    else
                        deathChance += 0.01d;
                }
            }
        }

        [HarmonyPatch(typeof(ShopSelectScript))]
        public class ShopSelectNavigationPatch
        {
            [HarmonyPatch(nameof(ShopScript.Start))]
            [HarmonyPostfix]
            static void OnStart(ShopSelectScript __instance)
            {
                if (!__instance.GetCurrentCard().Selectable)
                {
                    if (__instance.isArcade)
                    {
                        __instance.SavePosition(Stage.Clock);
                        (__instance.currentX, __instance.currentY) = __instance.LoadPosition();
                    }
                }
            }
            
            [HarmonyPatch(nameof(ShopSelectScript.SwitchIn))]
            [HarmonyPostfix]
            public static void OnSwitchIn(ShopSelectScript __instance, int direction, bool initial = false)
            {
                if (__instance.isArcade && __instance.GetCurrentCard().stage == Stage.Clock)
                {
                    __instance.cursor.SetActive(true);
                    __instance.cursor.transform.position = __instance.GetCurrentCard().transform.position + __instance.cursorOffset;
                    __instance.GetCurrentCard().Select();
                }
            }
            
            [HarmonyPatch(nameof(ShopSelectScript.OnDirection))]
            [HarmonyPrefix]
            static bool OnDirection(ShopSelectScript __instance, int x, int y, ref IEnumerator __result)
            {
                if (__instance.isArcade)
                    __result = OnDirectionMultiplayerCustom(__instance, x, y);
                else
                    __result = OnDirectionCustom(__instance, x, y);
                return false;
            }

            static IEnumerator OnDirectionMultiplayerCustom(ShopSelectScript instance, int x, int y)
            {
                if (!instance.acceptInput)
                    yield break;

                int bestX = instance.currentX;
                int bestY = instance.currentY;
                float minDistance = float.MaxValue;
                bool found = false;

                Vector3 currentPos = instance.GetCurrentCard().transform.position;

                for (int cy = 0; cy < instance.maxY; cy++)
                {
                    for (int cx = 0; cx < instance.maxX; cx++)
                    {
                        if (cx == instance.currentX && cy == instance.currentY)
                            continue;

                        LevelCardScript card = instance.GetCard(cx, cy);
                        
                        if (card == null || !card.gameObject.activeSelf || !card.Selectable)
                            continue;

                        Vector3 targetPos = card.transform.position;
                        Vector3 directionToTarget = targetPos - currentPos;

                        float dot = directionToTarget.x * x + directionToTarget.y * -y; 

                        if (dot > 0.1f) 
                        {
                            float dist = directionToTarget.sqrMagnitude;
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                bestX = cx;
                                bestY = cy;
                                found = true;
                            }
                        }
                    }
                }

                if (found)
                {
                    instance.moveSound.Play();
                    LevelCardScript previousCard = instance.GetCurrentCard();
                    LevelCardScript nextCard = instance.GetCard(bestX, bestY);

                    instance.currentX = bestX;
                    instance.currentY = bestY;

                    yield return null;

                    instance.cursor.SetActive(true);
                    instance.cursor.transform.position = nextCard.transform.position + instance.cursorOffset;

                    if (instance.pillar != null)
                        instance.pillar.Hide();

                    previousCard.Deselect();
                    nextCard.Select();
                    instance.NarrateOption();
                }
                else
                {
                    if (x != 0)
                    {
                        instance.PageDelta = x;
                    }
                }
            }

            static IEnumerator OnDirectionCustom(ShopSelectScript instance, int x, int y)
            {
                if (!instance.acceptInput)
                    yield break;

                if ((x != 0 || y != 0) && instance.GetCurrentCard().stage == Stage.Mixtape5)
                {
                    x = 0;
                    y = -1;
                }

                int bestX = instance.currentX;
                int bestY = instance.currentY;
                int bestScore = int.MaxValue;
                bool found = false;

                for (int cy = 0; cy < instance.maxY; cy++)
                {
                    for (int cx = 0; cx < instance.maxX; cx++)
                    {
                        LevelCardScript card = instance.GetCard(cx, cy);
                        if (!card.Selectable)
                            continue;

                        int dx = cx - instance.currentX;
                        int dy = cy - instance.currentY;

                        if (x != 0 && Math.Sign(dx) != Math.Sign(x))
                            continue;
                        if (y != 0 && Math.Sign(dy) != Math.Sign(y))
                            continue;

                        int score = Math.Abs(dy) * 1000 + Math.Abs(dx);
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestX = cx;
                            bestY = cy;
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    if (x != 0)
                        instance.PageDelta = x;
                    yield break;
                }

                LevelCardScript nextCard = instance.GetCard(bestX, bestY);

                instance.moveSound.Play();
                LevelCardScript previousCard = instance.GetCurrentCard();

                instance.currentX = bestX;
                instance.currentY = bestY;

                yield return null;

                instance.cursor.SetActive(true);
                instance.cursor.transform.position = nextCard.transform.position + instance.cursorOffset;

                if (nextCard.stage == Stage.Mixtape5)
                {
                    instance.cursor.SetActive(false);
                    instance.pillar.Show();
                }
                else if (instance.pillar != null)
                {
                    instance.pillar.Hide();
                }

                previousCard.Deselect();
                nextCard.Select();
                instance.NarrateOption();
            }
        }

        [HarmonyPatch(typeof(GameManager))]
        public class GameManager_Patch
        {
            [HarmonyTargetMethod]
            static MethodBase Target()
            {
                return typeof(GameManager).GetMethod(nameof(GameManager.TryCompleteEvent))!
                    .MakeGenericMethod(typeof(Achievement));
            }

            [HarmonyPostfix]
            public static void Postfix(Achievement key, bool __result)
            {
                if (__result)
                    PluginMain.ArchipelagoHandler.CheckLocation(0x300 + (int)key);
            }
        }

        public static void CheckGoal()
        {
            var shouldEnable = true;
            var highScores16 =  SaveDataManager.saveData.highScores16;
            var highScores = SaveDataManager.saveData.highScores;
            var highScores45 = SaveDataManager.saveData.highScores45;
            var highScores78 =  SaveDataManager.saveData.highScores78;
            var log = "Goal Requirements: ";
            if (highScores16 == null || PluginMain.SlotData.Required16RPMCompletions > highScores16.Count)
            {
                shouldEnable = false;
            }
            if (highScores == null || PluginMain.SlotData.RequiredLevelCompletions > highScores.Count)
            {
                shouldEnable = false;
            }
            if (highScores45 == null || PluginMain.SlotData.Required45RPMCompletions > highScores45.Count)
            {
                shouldEnable = false;
            }
            if (highScores78 == null || PluginMain.SlotData.Required78RPMCompletions > highScores78.Count)
            {
                shouldEnable = false;
            }
            
            if (PluginMain.SlotData.Required16RPMCompletions > 0)
                log += $"   {Math.Min(highScores16?.Count ?? 0, PluginMain.SlotData.Required16RPMCompletions)}/{PluginMain.SlotData.Required16RPMCompletions} 16RPM   ";
            if (PluginMain.SlotData.RequiredLevelCompletions > 0)
                log += $"   {Math.Min(highScores?.Count ?? 0, PluginMain.SlotData.RequiredLevelCompletions)}/{PluginMain.SlotData.RequiredLevelCompletions} 33RPM   ";
            if (PluginMain.SlotData.Required45RPMCompletions > 0)
                log += $"   {Math.Min(highScores45?.Count ?? 0, PluginMain.SlotData.Required45RPMCompletions)}/{PluginMain.SlotData.Required45RPMCompletions} 45RPM   ";
            if (PluginMain.SlotData.Required78RPMCompletions > 0)
                log += $"   {Math.Min(highScores78?.Count ?? 0, PluginMain.SlotData.Required78RPMCompletions)}/{PluginMain.SlotData.Required78RPMCompletions} 78RPM";
            
            if (!shouldEnable)
            {
                APConsole.Instance.Log(log);
                return;
            }
            
            if (!GameManager.UnlockEvents.TryGetValue(Stage.Mixtape5, out var mix5) || mix5 != EventState.Complete)
            {
                GameManager.UnlockEvents[Stage.Mixtape5] = EventState.Available;
                APConsole.Instance.Log("The Final Mixtape is Available...");
            }
        }

        [HarmonyPatch(typeof(RecordPlayerScript))]
        public class RecordPlayerScript_Patch
        {
            [HarmonyPatch(nameof(RecordPlayerScript.SetGlobalSpeedInternal))]
            [HarmonyPrefix]
            private static bool OnSetGlobalSpeedInternal(RecordPlayerScript __instance)
            {
                RecordPlayerScript.GlobalSpeed = __instance.CurrentSpeed;
                return false;
            }
        }

        [HarmonyPatch(typeof(ShopScript))]
        public class ShopScriptPatch
        {
            [HarmonyPatch(nameof(ShopScript.GetLowestVerdict))]
            [HarmonyPrefix]
            public static bool GetLowestVerdict(ShopScript __instance, ref Verdict __result)
            {
                Dictionary<string, Verdict> highScores = JudgementScript.GetHighScores();
                __result = __instance.mainStageOrder.Where(x => x != Stage.Mixtape5)
                    .Select(x => CollectionExtensions.GetValueOrDefault(highScores, x.ToString())).Min();
                return false;
            }
            
            [HarmonyPatch(nameof(ShopScript.MaybeUnlockInternal))]
            [HarmonyPrefix]
            private static bool OnMaybeUnlockInternal(ShopScript __instance)
            {
                if (JudgementScript.stageCompleted &&
                    JudgementScript.verdict > (Verdict)PluginMain.SlotData.RequiredRank)
                {
                    __instance.counter.TryEnableGameEvent(GameEvent.StageCleared);
                    Stage? clearedStage = JudgementScript.clearedStage;
                    if (clearedStage.HasValue)
                    {
                        switch (clearedStage)
                        {
                            case Stage.Mixtape1:
                                __instance.counter.TryEnableAchievement(Achievement.JungleMixtape);
                                break;
                            case Stage.Mixtape2:
                                __instance.counter.TryEnableAchievement(Achievement.SkyMixtape);
                                break;
                            case Stage.Mixtape3:
                                __instance.counter.TryEnableAchievement(Achievement.OceanMixtape);
                                break;
                            case Stage.Mixtape4:
                                __instance.counter.TryEnableAchievement(Achievement.FireMixtape);
                                break;
                            case Stage.Mixtape5:
                                PluginMain.ArchipelagoHandler.Release();
                                break;
                        }
                    }
                }
                return false;
            }
        }
        
        [HarmonyPatch(typeof(ShopScript), nameof(ShopScript.Awake))]
        public static class ShopScript_Awake_Transpiler
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    var instr = codes[i];
                    if (instr.opcode == OpCodes.Call &&
                        instr.operand is MethodInfo mi &&
                        mi.Name == nameof(ShopScript.TryEnableNextUnlockEvent))
                    {
                        codes[i].opcode = OpCodes.Nop;
                        codes[i].operand = null;
                    }
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch]
        public class PostcardJudgementCheckSendPatch
        {
            static MethodBase TargetMethod()
            {
                var nestedTypes = typeof(PostcardJudgementScript)
                    .GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance);

                var stateMachine = nestedTypes.Single(t =>
                    typeof(IEnumerator).IsAssignableFrom(t) &&
                    t.Name.Contains("Play"));

                return AccessTools.Method(stateMachine, "MoveNext");
            }
            
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                var hook = AccessTools.Method(
                    typeof(PostcardJudgementCheckSendPatch),
                    nameof(OnJudgementCheck)
                );

                var thisField = codes
                    .Select(c => c.operand)
                    .OfType<FieldInfo>()
                    .First(f => f.Name.Contains("4__this") &&
                                f.FieldType == typeof(PostcardJudgementScript));

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt &&
                        codes[i].operand is MethodInfo mi &&
                        mi.Name == "Play" &&
                        mi.DeclaringType == typeof(TempoSound))
                    {
                        if (codes[i - 1].opcode == OpCodes.Ldfld &&
                            codes[i - 1].operand is FieldInfo f &&
                            f.Name.Contains("verdictSound"))
                        {
                            codes.InsertRange(i, new[]
                            {
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Ldfld, thisField),
                                new CodeInstruction(OpCodes.Call, hook)
                            });

                            break;
                        }
                    }
                }

                return codes;
            }
            
            public static void OnJudgementCheck(PostcardJudgementScript instance)
            {
                if (instance.stage == Stage.Mixtape5)
                    return;
                if ((int)JudgementScript.verdict <= (int)PluginMain.SlotData.RequiredRank)
                {
                    APConsole.Instance.Log("Insufficient score to send check!");
                    return;
                }
                var speedLevel = Data.SpeedToId[RecordPlayerScript.GlobalSpeed];
                var locId = Data.StageToId[instance.stage];
                PluginMain.ArchipelagoHandler.CheckLocation(0x100 + locId * 4 + speedLevel);
                CheckGoal();
            }
        }
        
        
        [HarmonyPatch(typeof(JudgementScript), nameof(JudgementScript.GetHighScores))]
        public class JudgementGetHighScoresPatch
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call &&
                        codes[i].operand is MethodInfo { Name: "GetValueOrDefault" }
                        && codes[i + 1].opcode == OpCodes.Ldc_I4_2
                        && codes[i + 2].opcode == OpCodes.Bne_Un)
                    {
                        codes[i + 1] = new CodeInstruction(OpCodes.Pop);
                        codes[i + 2] = new CodeInstruction(OpCodes.Nop);
                        break;
                    }
                }
                return codes;
            }
        }


        [HarmonyPatch]
        public class CounterScript_Patch
        {
            static MethodBase TargetMethod()
            {
                var nestedTypes = typeof(CounterScript).GetNestedTypes(
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                var stateMachine = nestedTypes.Single(t =>
                    typeof(IEnumerator).IsAssignableFrom(t) &&
                    t.Name.Contains("HandleEvents")
                );
                return AccessTools.Method(stateMachine, "MoveNext");
            }
            
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count - 1; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo { Name: "get_UnlockEvents" } &&
                        codes[i + 1].opcode == OpCodes.Ldc_I4 && (int)codes[i + 1].operand == 4096)
                    {
                        for (int j = 0; j < 4 && i + j < codes.Count; j++)
                        {
                            codes[i + j].opcode = OpCodes.Nop;
                            codes[i + j].operand = null;
                        }

                        break;
                    }
                }
                return codes.AsEnumerable();
            }
        }
    }
}