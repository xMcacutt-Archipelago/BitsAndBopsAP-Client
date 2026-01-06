using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using SettingsAnimationStates;
using UnityEngine;

namespace BitsAndBops_AP_Client
{
    public class ConnectionScreenHandler
    {
        [HarmonyPatch(typeof(FileSelectScript))]
        private static class FileSelectScript_Patch
        {
            [HarmonyPatch(nameof(FileSelectScript.Open))]
            [HarmonyPrefix]
            public static bool Open_Prefix(FileSelectScript __instance, ref IEnumerator __result)
            {
                __result = CustomOpen(__instance);
                return false;
            }

            private static string ap_IP = "archipelago.gg:38281";
            private static string ap_Slot = "Player1";
            private static string ap_Pass = "";

            private static float backspaceTimer = 0f;
            private const float INITIAL_DELAY = 0.5f;
            private const float REPEAT_RATE = 0.05f;
            
            private static char VKeyToChar(uint vkey, bool shift)
            {
                if (vkey >= 65 && vkey <= 90) 
                    return shift ? (char)vkey : char.ToLower((char)vkey);
    
                if (vkey >= 96 && vkey <= 105) 
                    return (char)(vkey - 48);

                switch (vkey)
                {
                    case 48: return shift ? ')' : '0';
                    case 49: return shift ? '!' : '1';
                    case 50: return shift ? '@' : '2';
                    case 51: return shift ? '#' : '3';
                    case 52: return shift ? '$' : '4';
                    case 53: return shift ? '%' : '5';
                    case 54: return shift ? '^' : '6';
                    case 55: return shift ? '&' : '7';
                    case 56: return shift ? '*' : '8';
                    case 57: return shift ? '(' : '9';
                    case 32: return ' ';
                    case 186: return shift ? ':' : ';';
                    case 187: return shift ? '+' : '=';
                    case 188: return shift ? '<' : ',';
                    case 189: return shift ? '_' : '-';
                    case 190: return shift ? '>' : '.';
                    case 191: return shift ? '?' : '/';
                    case 192: return shift ? '~' : '`';
                    case 219: return shift ? '{' : '[';
                    case 220: return shift ? '|' : '\\';
                    case 221: return shift ? '}' : ']';
                    case 222: return shift ? '"' : '\'';
                    case 106: return '*';
                    case 107: return '+';
                    case 109: return '-';
                    case 110: return '.';
                    case 111: return '/';
                }

                return '\0';
            }
            
            class ConnectionStatus { 
                public bool Finished = false; 
                public bool Success = false; 
            }

            private static Vector2? leftAnchor;
            public static IEnumerator CustomOpen(FileSelectScript __instance)
            {
                APConsole.Create();
                var temp_ip = "";
                ushort temp_port = 0;
                ConnectionInfoHandler.Load(ref temp_ip, ref temp_port, ref ap_Slot, ref ap_Pass);
                ap_IP = temp_ip + ":" + temp_port; 
                __instance.boardDown.Play();
                __instance.board.SetState(Board.Enter);
                __instance.titleText.text = "Archipelago Connection!";
                __instance.transform.Find("Table").gameObject.SetActive(false);
                __instance.transform.Find("Copy").gameObject.SetActive(false);
                __instance.transform.Find("Erase").gameObject.SetActive(false);
                foreach (var icon in __instance.icons) icon.gameObject.SetActive(false);
                __instance.options[4].gameObject.SetActive(false);

                __instance.options[0].rectTransform.sizeDelta = new Vector2(1000, __instance.options[0].rectTransform.sizeDelta.y);
                __instance.options[1].rectTransform.sizeDelta = new Vector2(1000, __instance.options[1].rectTransform.sizeDelta.y);
                __instance.options[2].rectTransform.sizeDelta = new Vector2(1000, __instance.options[2].rectTransform.sizeDelta.y);
                var pos = __instance.options[0].rectTransform.anchoredPosition;
                if (leftAnchor == null)
                    leftAnchor = new Vector2(pos.x + 400, pos.y);
                __instance.options[0].rectTransform.anchoredPosition = (Vector2)leftAnchor;           
                __instance.options[1].rectTransform.anchoredPosition = (Vector2)leftAnchor; 
                __instance.options[2].rectTransform.anchoredPosition = (Vector2)leftAnchor; 
                
                for (int i = 0; i < 3; i++) 
                    __instance.HideDisplay(i);
                
                string[] labels = { "Host:", "Slot:", "Pass:", "Connect" };
                int selection = 0;

                yield return null;

                while (true)
                {
                    __instance.options[0].text = $"Host: {ap_IP}";
                    __instance.options[1].text = $"Slot: {ap_Slot}";
                    __instance.options[2].text = $"Pass: {new string('*', ap_Pass.Length)}";
                    __instance.options[3].text = "Connect!";

                    for (int i = 0; i < 4; i++)
                        __instance.options[i].color =
                            (selection == i) ? __instance.highlightColor : __instance.defaultColors[i];
                    
                    if (Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        selection = (selection + 1) % 4;
                        __instance.moveSound.Play();
                    }

                    if (Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        selection = (selection + 3) % 4;
                        __instance.moveSound.Play();
                    }
                    
                    if (selection < 3)
                    {
                        if (TempoInput.GetKey(TempoInput.VK_BACK))
                        {
                            backspaceTimer -= Time.deltaTime;
                            if (TempoInput.GetKeyDown(TempoInput.VK_BACK) || backspaceTimer <= 0)
                            {
                                if (selection == 0 && ap_IP.Length > 0) 
                                    ap_IP = ap_IP.Substring(0, ap_IP.Length - 1);
                                else if (selection == 1 && ap_Slot.Length > 0) 
                                    ap_Slot = ap_Slot.Substring(0, ap_Slot.Length - 1);
                                else if (selection == 2 && ap_Pass.Length > 0) 
                                    ap_Pass = ap_Pass.Substring(0, ap_Pass.Length - 1);
            
                                __instance.moveSound.Play();
                                backspaceTimer = TempoInput.GetKeyDown(TempoInput.VK_BACK) ? INITIAL_DELAY : REPEAT_RATE;
                            }
                        }
                        else
                        {
                            backspaceTimer = 0f; // Reset when not holding
                        }
                        
                        if (TempoInput.GetKeyDown(out uint vkey))
                        {
                            if (vkey != TempoInput.VK_BACK)
                            {
                                bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            
                                if (vkey == TempoInput.VK_RETURN)
                                {
                                    selection = (selection + 1) % 4;
                                    __instance.confirmSound.Play();
                                }
                                else
                                {
                                    char c = VKeyToChar(vkey, isShiftHeld);
                                    if (c != '\0')
                                    {
                                        if (selection == 0) ap_IP += c;
                                        else if (selection == 1) ap_Slot += c;
                                        else if (selection == 2) ap_Pass += c;
                                    }
                                }
                            }
                            TempoInput.DropKeyDown(vkey);
                        }
                    }

                    if (TempoInput.GetActionDown(Action.Cancel))
                    {
                        __instance.cancelSound.Play();
                        break;
                    }

                    if (TempoInput.GetActionDown(Action.Confirm) && selection == 3)
                    {
                        __instance.confirmSound.Play();
                        __instance.titleText.text = "Connecting...";

                        PluginMain.ArchipelagoHandler.ConnectionSucceeded = false;
                        PluginMain.ArchipelagoHandler.ConnectionFinished = false;
                        
                        string[] hostParts = ap_IP.Split(':');
                        string host = hostParts[0];
                        int port = hostParts.Length > 1 ? int.Parse(hostParts[1]) : 38281;

                        PluginMain.ArchipelagoHandler.CreateSession(host, port, ap_Slot, ap_Pass);
                        PluginMain.ArchipelagoHandler.Connect();
                        while (!PluginMain.ArchipelagoHandler.ConnectionFinished)
                            yield return null;

                        if (PluginMain.ArchipelagoHandler.ConnectionSucceeded)
                        {
                            APConsole.Instance.DebugLog("Connection succeeded, loading");
                            ConnectionInfoHandler.Save(host, (ushort)port, ap_Slot, ap_Pass);
                            __instance.titleText.text = "Connected!";
                            yield return new WaitForSeconds(1.0f);
                            SaveDataManager.SetSaveSlot(0);
                            __instance.Complete = true;
                            yield break;
                        }
                        __instance.titleText.text = "Archipelago Connection!";
                    }

                    yield return null;
                }

                __instance.boardUp.Play();
                __instance.board.SetState(Board.Exit);
            }
        }
    }
}