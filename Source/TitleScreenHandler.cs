using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace BitsAndBops_AP_Client
{
    public class TitleScreenHandler : MonoBehaviour
    {
        [HarmonyPatch(typeof(TitleScript))]
        private class TitleScript_Patch
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            private static void Awake_Postfix(TitleScript __instance)
            {
                __instance.optionStrings[0] = "Connect";
            }
            
            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            private static void Update_Postfix(TitleScript __instance)
            {
                __instance.optionStrings[0] = PluginMain.ArchipelagoHandler.IsConnected ? "Continue" : "Connect";
                if (__instance.settings == null)
                    __instance.settings = FindObjectOfType<SettingsScript>();
                if (__instance.credits == null)
                    __instance.credits = FindObjectOfType<CreditsScript>();
                if (__instance.settings == null || __instance.credits == null || __instance.settings.IsOpen ||
                    __instance.credits.IsOpen || !__instance.acceptInput)
                    return;
                if (__instance.menuInput && TempoInput.GetActionDown(Action.Confirm))
                {
                    if (__instance.optionStrings[__instance.selection] == "Connect")
                        __instance.StartCoroutine(Play(__instance));
                    if (__instance.optionStrings[__instance.selection] == "Continue")
                        __instance.StartCoroutine(Continue(__instance));
                }
            }

            private static IEnumerator Continue(TitleScript __instance)
            {
                yield return __instance.Play(SceneKey.StageSelect);
            }
            
            private static IEnumerator Play(TitleScript __instance)
            {
                __instance.acceptInput = false;
                yield return __instance.fileSelect.Open();
                ScreenReader.Stop();
                if (!__instance.fileSelect.Complete)
                {
                    __instance.RedrawText();
                    __instance.acceptInput = true;
                }
                else
                    yield return __instance.Play(SceneKey.StageSelect);
            }
        }
        
        


    }
}