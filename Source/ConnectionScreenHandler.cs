using System;
using System.Collections;
using HarmonyLib;
using SettingsAnimationStates;
using UnityEngine;

namespace BitsAndBops_AP_Client
{
    public static class ConnectionScreenHandler
    {
        [HarmonyPatch(typeof(FileSelectScript))]
        private static class FileSelectScriptPatch
        {
            [HarmonyPatch(nameof(FileSelectScript.Open))]
            [HarmonyPrefix]
            public static bool Open_Prefix(FileSelectScript __instance, ref IEnumerator __result)
            {
                __result = CustomOpen(__instance);
                return false;
            }

            private static string _apIP = "archipelago.gg:38281";
            private static string _apSlot = "Player1";
            private static string _apPass = "";

            private static float _backspaceTimer;
            private const float InitialDelay = 0.5f;
            private const float RepeatRate = 0.05f;
            
            private static char VKeyToChar(uint vkey, bool shift)
            {
                return vkey switch
                {
                    >= 65 and <= 90 => shift ? (char)vkey : char.ToLower((char)vkey),
                    >= 96 and <= 105 => (char)(vkey - 48),
                    _ => vkey switch
                    {
                        48 => shift ? ')' : '0',
                        49 => shift ? '!' : '1',
                        50 => shift ? '@' : '2',
                        51 => shift ? '#' : '3',
                        52 => shift ? '$' : '4',
                        53 => shift ? '%' : '5',
                        54 => shift ? '^' : '6',
                        55 => shift ? '&' : '7',
                        56 => shift ? '*' : '8',
                        57 => shift ? '(' : '9',
                        32 => ' ',
                        186 => shift ? ':' : ';',
                        187 => shift ? '+' : '=',
                        188 => shift ? '<' : ',',
                        189 => shift ? '_' : '-',
                        190 => shift ? '>' : '.',
                        191 => shift ? '?' : '/',
                        192 => shift ? '~' : '`',
                        219 => shift ? '{' : '[',
                        220 => shift ? '|' : '\\',
                        221 => shift ? '}' : ']',
                        222 => shift ? '"' : '\'',
                        106 => '*',
                        107 => '+',
                        109 => '-',
                        110 => '.',
                        111 => '/',
                        _ => '\0'
                    }
                };
            }

            private static Vector2? _leftAnchor;
            private static IEnumerator CustomOpen(FileSelectScript __instance)
            {
                APConsole.Instance.Log($"Welcome to Bits & Bops Archipelago!");
                ConnectionInfoHandler.Load(ref _apIP, ref _apSlot, ref _apPass);
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
                _leftAnchor ??= new Vector2(pos.x + 400, pos.y);
                __instance.options[0].rectTransform.anchoredPosition = (Vector2)_leftAnchor;           
                __instance.options[1].rectTransform.anchoredPosition = (Vector2)_leftAnchor; 
                __instance.options[2].rectTransform.anchoredPosition = (Vector2)_leftAnchor; 
                
                for (var i = 0; i < 3; i++) 
                    __instance.HideDisplay(i);
                var selection = 0;

                yield return null;

                while (true)
                {
                    __instance.options[0].text = $"Host: {_apIP}";
                    __instance.options[1].text = $"Slot: {_apSlot}";
                    __instance.options[2].text = $"Pass: {new string('*', _apPass.Length)}";
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
                            _backspaceTimer -= Time.deltaTime;
                            if (TempoInput.GetKeyDown(TempoInput.VK_BACK) || _backspaceTimer <= 0)
                            {
                                if (selection == 0 && _apIP.Length > 0) 
                                    _apIP = _apIP.Substring(0, _apIP.Length - 1);
                                else if (selection == 1 && _apSlot.Length > 0) 
                                    _apSlot = _apSlot.Substring(0, _apSlot.Length - 1);
                                else if (selection == 2 && _apPass.Length > 0) 
                                    _apPass = _apPass.Substring(0, _apPass.Length - 1);
            
                                __instance.moveSound.Play();
                                _backspaceTimer = TempoInput.GetKeyDown(TempoInput.VK_BACK) ? InitialDelay : RepeatRate;
                            }
                        }
                        else
                        {
                            _backspaceTimer = 0f; // Reset when not holding
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
                                        if (selection == 0) _apIP += c;
                                        else if (selection == 1) _apSlot += c;
                                        else if (selection == 2) _apPass += c;
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

                        PluginMain.ArchipelagoHandler.connectionSucceeded = false;
                        PluginMain.ArchipelagoHandler.connectionFinished = false;

                        try
                        {
                            PluginMain.ArchipelagoHandler.CreateSession(_apIP, _apSlot, _apPass);
                            PluginMain.ArchipelagoHandler.Connect();
                        }
                        catch (Exception ex)
                        {
                            APConsole.Instance.Log(ex.Message);
                        }
                        while (!PluginMain.ArchipelagoHandler.connectionFinished)
                            yield return null;

                        if (PluginMain.ArchipelagoHandler.connectionSucceeded)
                        {
                            APConsole.Instance.DebugLog("Connection succeeded, loading");
                            ConnectionInfoHandler.Save(_apIP, _apSlot, _apPass);
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