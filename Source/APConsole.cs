using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BitsAndBops_AP_Client;

public class APConsole : MonoBehaviour
{
    // GAME SPECIFIC STUFF
    private static readonly Dictionary<string, string> KeywordColors = new()
    {
        { "badge", "#efbf04" },
        { "fire mixtape", "#f52011"},
        { "jungle mixtape", "#00ad06"},
        { "sky mixtape", "#02c9c6"},
        { "ocean mixtape", "#1b5bfa"},
        { "cartridge", "#717d85"},
        { "flipper snapper", "#3268a8"},
        { "sweet tooth", "#b830b1"},
        { "rock, paper, showdown!", "#a67d4e"},
        { "pantry parade", "#ff7869"},
        { "b-bot & the fly girls", "#f7e32a"},
        { "flow worms", "#aefa4b"},
        { "meet & tweet", "#26a7de"},
        { "steady bears", "#87f5f1"},
        { "pop up kitchen", "#ecaf73"},
        { "firework festival", "#e66b0e"},
        { "hammer time!", "#a17c60"},
        { "molecano", "#cf2200"},
        { "president bird", "#ff0505"},
        { "snakedown", "#d6d604"},
        { "octeaparty", "#ea8df2"},
        { "globe trotters", "#1cffec"},
        { "xmcacutt", "#ff2e4a"},
        { "dashieswag92", "#fa3ced"}
    };

    private const string FontName = "TempoCurse SDF";

    private const KeyCode LogToggleKey = KeyCode.F7; // Reassign in Create if configurable
    private const KeyCode HistoryToggleKey = KeyCode.F8; // Reassign in Create if configurable
    private const CursorLockMode DefaultCursorMode = CursorLockMode.Locked;
    private const bool DefaultCursorVisible = false;

    // CONSOLE PARAMS
    private const float MessageHeight = 28f;
    private const float ConsoleHeight = 280f;

    private const float SlideInTime = 0.25f;
    private const float HoldTime = 3.0f;
    private const float FadeOutTime = 0.5f;

    private const float SlideInOffset = -50f;
    private const float FadeUpOffset = 20f;

    private const float PaddingX = 25f;
    private const float PaddingY = 25f;

    private const float MessageSpacing = 6f;
    
    private bool _rebuildHistoryDirty;
    private int _historyBuiltCount;

    // COLLECTIONS
    private static TMP_FontAsset? _font;

    private readonly ConcurrentQueue<Image> _backgroundPool = new();

    private readonly ConcurrentQueue<LogEntry> _cachedEntries = new();

    private readonly ConcurrentQueue<TextMeshProUGUI> _textPool = new();
    private readonly List<LogEntry> _visibleEntries = [];
    private readonly List<LogEntry> _historyEntries = [];
    private GameObject? _historyPanel;
    private RectTransform? _historyContent;
    private bool _showHistory;
    private ScrollRect? _historyScrollRect;
    private RectTransform? _historyViewport;
    
    private Transform? _messageParent;
    private bool _showConsole = true;

    private static APConsole? _instance;
    public static APConsole Instance
    {
        get
        {
            if (_instance == null)
                Create();
            return _instance!;
        }
    }

    private static void Create()
    {
        if (_instance != null)
            return;
        PluginMain.logger.LogWarning($"{Resources.FindObjectsOfTypeAll<TMP_FontAsset>().Length} font assets exist.");
        foreach (var res in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            PluginMain.logger.LogWarning(res.name);
        _font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name == FontName);
        var consoleObject = new GameObject("ArchipelagoConsoleUI");
        DontDestroyOnLoad(consoleObject);
        _instance = consoleObject.AddComponent<APConsole>();
        _instance.BuildUI();
        _instance.Log($"Client by xMcacutt, apworld by DashieSwag92");
        _instance.Log(
            $"Press {LogToggleKey.ToString()} to Toggle log and {HistoryToggleKey.ToString()} to toggle history");
        _instance.DebugLog("Colour Test");
        foreach (var word in KeywordColors.Keys)
            _instance.DebugLog(word);
    }

    private void Update()
    {
        UpdateMessages(Time.deltaTime);
        TryAddNewMessages();
        if (Input.GetKeyDown(LogToggleKey))
            ToggleConsole();
        if (Input.GetKeyDown(HistoryToggleKey))
            ToggleHistory();

        if (_showHistory)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (_showHistory && _rebuildHistoryDirty)
        {
            _rebuildHistoryDirty = false;
            RebuildHistory();
        }
        
        if (_showHistory && _historyScrollRect != null)
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                float currentPos = _historyScrollRect.verticalNormalizedPosition;
                float newPos = currentPos + (scrollDelta * _historyScrollRect.scrollSensitivity * 0.1f);
                _historyScrollRect.verticalNormalizedPosition = Mathf.Clamp01(newPos);
            }
        }
    }

    private void UpdateMessages(float delta)
    {
        for (var i = _visibleEntries.Count - 1; i >= 0; i--)
        {
            var e = _visibleEntries[i];
            var done = AnimateEntry(e, delta);

            if (done)
            {
                RecycleEntry(e);
                _visibleEntries.RemoveAt(i);
                RecalculateBaseY();
            }
            else
            {
                UpdateEntryVisual(e);
            }
        }
    }

    private void RecalculateBaseY()
    {
        var y = 0f;
        for (var i = _visibleEntries.Count - 1; i >= 0; i--)
        {
            var e = _visibleEntries[i];
            e.baseY = y;
            y += e.height + MessageSpacing;
        }
    }

    private bool AnimateEntry(LogEntry entry, float delta)
    {
        entry.stateTimer += delta;

        switch (entry.state)
        {
            case LogEntry.State.SlideIn:
            {
                var t = Mathf.Clamp01(entry.stateTimer / SlideInTime);
                entry.offsetY = Mathf.Lerp(SlideInOffset, 0f, EaseOutQuad(t));

                if (t >= 1f)
                {
                    entry.state = LogEntry.State.Hold;
                    entry.stateTimer = 0f;
                }
            }
                break;

            case LogEntry.State.Hold:
            {
                entry.offsetY = 0f;
                if (entry.stateTimer >= HoldTime)
                {
                    entry.state = LogEntry.State.FadeOut;
                    entry.stateTimer = 0f;
                }
            }
                break;

            case LogEntry.State.FadeOut:
            {
                var t = Mathf.Clamp01(entry.stateTimer / FadeOutTime);
                entry.offsetY = Mathf.Lerp(0f, FadeUpOffset, t);
                var alpha = 1f - t;
                if (entry.text != null) 
                    entry.text.color = new Color(1f, 1f, 1f, alpha);
                if (entry.background != null) 
                    entry.background.color = new Color(0f, 0f, 0f, 0.8f * alpha);

                if (t >= 1f)
                    return true;
            }
                break;
        }

        return false;
    }

    private static float EaseOutQuad(float x)
    {
        return 1f - (1f - x) * (1f - x);
    }

    private void TryAddNewMessages()
    {
        if (_showHistory)
            return;

        if (!_cachedEntries.Any())
            return;

        var maxMessages = Mathf.FloorToInt(ConsoleHeight / MessageHeight);
        if (_visibleEntries.Count >= maxMessages)
            return;

        _cachedEntries.TryDequeue(out var entry);
        entry.state = LogEntry.State.SlideIn;
        entry.stateTimer = 0f;

        entry.offsetY = SlideInOffset;
        entry.animatedY = entry.baseY + entry.offsetY;

        CreateEntryVisual(entry);

        _visibleEntries.Add(entry);
        RecalculateBaseY();
        entry.animatedY = entry.baseY + entry.offsetY;
    }

    private void AddHistoryEntryVisual(LogEntry entry)
    {
        var bg = GetBackground();
        bg.transform.SetParent(_historyContent, false);

        var bgRect = bg.rectTransform;
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(1, 1);
        bgRect.pivot = new Vector2(0.5f, 1);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(600f, MessageHeight);

        var text = GetText();
        var tRect = text.rectTransform;
        tRect.SetParent(bg.transform, false);

        tRect.anchorMin = new Vector2(0, 0);
        tRect.anchorMax = new Vector2(1, 1);
        tRect.pivot = new Vector2(0, 0.5f);
        tRect.offsetMin = new Vector2(8f, 4f);
        tRect.offsetMax = new Vector2(-8f, -4f);

        entry.text = text;
        entry.background = bg;

        text.color = Color.white;
        bg.color = new Color(0, 0, 0, 0.8f);

        text.text = entry.colorizedMessage;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
        var height = Mathf.Max(MessageHeight, text.preferredHeight + 8f);
        var layoutElement = bg.GetComponent<LayoutElement>();
        if (!layoutElement)
            layoutElement = bg.gameObject.AddComponent<LayoutElement>();

        layoutElement.preferredHeight = height;
    }

    private void CreateEntryVisual(LogEntry entry)
    {
        var bg = GetBackground();
        bg.transform.SetParent(_messageParent, false);

        var bgRect = bg.rectTransform;
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(0, 0);
        bgRect.pivot = new Vector2(0, 0);
        bgRect.sizeDelta = new Vector2(600f, MessageHeight);

        var text = GetText();
        var tRect = text.rectTransform;
        tRect.SetParent(bg.transform, false);

        tRect.anchorMin = new Vector2(0, 0);
        tRect.anchorMax = new Vector2(1, 1);
        tRect.pivot = new Vector2(0, 0.5f);
        tRect.offsetMin = new Vector2(8f, 4f);
        tRect.offsetMax = new Vector2(-8f, -4f);

        entry.text = text;
        entry.background = bg;
        text.color = new Color(1, 1, 1, 1);
        bg.color = new Color(0, 0, 0, 0.8f);

        UpdateEntryVisual(entry);
    }

    private void UpdateEntryVisual(LogEntry entry)
    {
        if (entry.text != null)
        {
            entry.text.text = entry.colorizedMessage;

            var bgRect = entry.background?.rectTransform;
            var textHeight = entry.text.preferredHeight;
            entry.height = Mathf.Max(MessageHeight, textHeight + 8f);
            if (bgRect != null) 
                bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, entry.height);
        }

        var targetY = entry.baseY + entry.offsetY;
        entry.animatedY = Mathf.Lerp(entry.animatedY, targetY, Time.deltaTime * 12f);

        if (entry.background != null)
            entry.background.rectTransform.anchoredPosition =
                new Vector2(0f, entry.animatedY);
    }

    private TextMeshProUGUI GetText()
    {
        if (_textPool.Count > 0)
        {
            _textPool.TryDequeue(out var t);
            t.gameObject.SetActive(true);
            return t;
        }

        var go = new GameObject("LogText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var t2 = go.GetComponent<TextMeshProUGUI>();
        t2.fontSize = 19;
        t2.color = Color.white;
        t2.font = _font;
        t2.wordSpacing = 20f;
        t2.alignment = TextAlignmentOptions.MidlineLeft;
        return t2;
    }

    private Image GetBackground()
    {
        if (_backgroundPool.Count > 0)
        {
            _backgroundPool.TryDequeue(out var img);
            img.gameObject.SetActive(true);
            return img;
        }

        var go = new GameObject("LogBG");
        var imgNew = go.AddComponent<Image>();
        imgNew.color = new Color(0, 0, 0, 0.8f);
        imgNew.type = Image.Type.Sliced;

        return imgNew;
    }

    private void RecycleEntry(LogEntry entry)
    {
        if (entry.text != null)
        {
            entry.text.gameObject.SetActive(false);
            _textPool.Enqueue(entry.text);
            entry.text = null;
        }

        if (entry.background != null)
        {
            entry.background.gameObject.SetActive(false);
            _backgroundPool.Enqueue(entry.background);
            entry.background = null;
        }
    }

    private string Colorize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var tokens = Tokenize(input);
        ApplyMultiWordColoring(tokens);
        ApplySingleWordColoring(tokens);
        return string.Concat(tokens);
    }

    private List<string> Tokenize(string input)
    {
        return string.IsNullOrEmpty(input) ? [] : Regex.Split(input, @"(\s+)").ToList();
    }

    private void ApplySingleWordColoring(List<string> tokens)
    {
        var singleKeys = KeywordColors
            .Where(kvp => !kvp.Key.Contains(" "))
            .ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (!IsWord(token))
                continue;

            var cleanToken = Regex.Replace(token.ToLowerInvariant(), @"[^a-z0-9]", "");

            if (singleKeys.TryGetValue(cleanToken, out var color))
            {
                tokens[i] = $"<color={color}>{token}</color>";
            }
        }
    }

    private void ApplyMultiWordColoring(List<string> tokens)
    {
        var multiKeys = KeywordColors
            .Where(kvp => kvp.Key.Contains(" "))
            .OrderByDescending(kvp => kvp.Key.Length)
            .ToList();

        if (multiKeys.Count == 0 || tokens.Count == 0)
            return;

        for (int i = 0; i < tokens.Count; i++)
        {
            if (!IsWord(tokens[i])) continue;

            string remainingText = string.Concat(tokens.Skip(i));
            string lowerRemaining = remainingText.ToLowerInvariant();

            foreach (var key in multiKeys)
            {
                if (lowerRemaining.StartsWith(key.Key.ToLowerInvariant()))
                {
                    string accumulated = "";
                    int tokensToConsume = 0;
                    for (int j = i; j < tokens.Count; j++)
                    {
                        accumulated += tokens[j];
                        tokensToConsume++;
                        if (accumulated.Length >= key.Key.Length) break;
                    }

                    string originalPhrase = string.Concat(tokens.Skip(i).Take(tokensToConsume));
                    tokens[i] = $"<color={key.Value}>{originalPhrase}</color>";

                    for (int c = 1; c < tokensToConsume; c++)
                    {
                        tokens[i + c] = string.Empty;
                    }

                    i += tokensToConsume - 1;
                    break; 
                }
            }
        }
    }

    private bool IsWord(string token)
    {
        return !string.IsNullOrWhiteSpace(token);
    }


    public void Log(string text)
    {
        var entry = new LogEntry(text);
        entry.colorizedMessage = Colorize(text);

        if (_showHistory)
        {
            _historyEntries.Add(new LogEntry(text)
            {
                colorizedMessage = entry.colorizedMessage
            });

            _rebuildHistoryDirty = true;
            return;
        }

        _cachedEntries.Enqueue(entry);

        _historyEntries.Add(new LogEntry(text)
        {
            colorizedMessage = entry.colorizedMessage
        });
    }

    public void DebugLog(string text)
    {
        if (PluginMain.EnableDebugLogging != null && !PluginMain.EnableDebugLogging.Value)
            return;
        Log(text);
    }

    private void ToggleHistory()
    {
        _showHistory = !_showHistory;

        if (_messageParent == null || _historyPanel == null)
            return;
        
        _messageParent.gameObject.SetActive(!_showHistory);

        _historyPanel.SetActive(_showHistory);

        if (_showHistory)
        {
            foreach (var e in _visibleEntries)
            {
                if (e.text != null)
                {
                    e.text.gameObject.SetActive(false);
                    _textPool.Enqueue(e.text);
                }

                if (e.background != null)
                {
                    e.background.gameObject.SetActive(false);
                    _backgroundPool.Enqueue(e.background);
                }
            }

            _visibleEntries.Clear();
            _cachedEntries.Clear();

            RebuildHistory();
        }
        else
        {
            Cursor.lockState = DefaultCursorMode;
            Cursor.visible = DefaultCursorVisible;
            _messageParent.gameObject.SetActive(_showConsole);
        }
    }

    private void ToggleConsole()
    {
        _showConsole = !_showConsole;
        if (_messageParent == null || _historyPanel == null)
            return;

        foreach (var e in _visibleEntries)
        {
            if (e.background != null)
                e.background.gameObject.SetActive(_showConsole);
            if (e.text != null)
                e.text.gameObject.SetActive(_showConsole);
        }

        _messageParent.gameObject.SetActive(_showConsole);

        if (_showConsole)
            return;
        _showHistory = false;
        _historyPanel.SetActive(false);
    }

    private void BuildUI()
    {
        var canvasObject = new GameObject("APConsoleCanvas", typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var container = new GameObject("Messages", typeof(RectTransform));
        var rect = container.GetComponent<RectTransform>();
        rect.SetParent(canvasObject.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(PaddingX, PaddingY);
        _messageParent = container.transform;

        _historyPanel = new GameObject("HistoryPanel", typeof(RectTransform));
        var historyRect = _historyPanel.GetComponent<RectTransform>();
        historyRect.SetParent(canvasObject.transform, false);

        historyRect.anchorMin = new Vector2(0f, 0f);
        historyRect.anchorMax = new Vector2(0f, 0f);
        historyRect.pivot = new Vector2(0f, 0f);
        historyRect.anchoredPosition = new Vector2(PaddingX, PaddingY);
        historyRect.sizeDelta = new Vector2(600f, ConsoleHeight);

        _historyPanel.SetActive(false);

        _historyScrollRect = _historyPanel.AddComponent<ScrollRect>();
        _historyScrollRect.horizontal = false;
        _historyScrollRect.vertical = true;
        _historyScrollRect.scrollSensitivity = 10f;
        _historyScrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask));
        _historyViewport = viewport.GetComponent<RectTransform>();
        viewport.transform.SetParent(_historyPanel.transform, false);

        _historyViewport.anchorMin = Vector2.zero;
        _historyViewport.anchorMax = Vector2.one;
        _historyViewport.offsetMin = Vector2.zero;
        _historyViewport.offsetMax = Vector2.zero;

        var vpImage = viewport.GetComponent<Image>();
        vpImage.color = Color.white;
        vpImage.type = Image.Type.Simple;
        vpImage.raycastTarget = true;

        var mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        _historyScrollRect.viewport = _historyViewport;
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.SetParent(viewport.transform, false);

        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperLeft;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _historyScrollRect.content = contentRect;
        _historyContent = contentRect;
    }

    private void RebuildHistory()
    {
        if (_historyContent == null) return;
        for (int i = _historyBuiltCount; i < _historyEntries.Count; i++)
            AddHistoryEntryVisual(_historyEntries[i]);
        
        _historyBuiltCount = _historyEntries.Count;
        
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_historyContent);
        Canvas.ForceUpdateCanvases();
        if (_historyScrollRect != null)
            _historyScrollRect.verticalNormalizedPosition = 0f;
    }

    [Serializable]
    public class LogEntry(string msg)
    {
        public enum State
        {
            SlideIn,
            Hold,
            FadeOut
        }

        public State state = State.SlideIn;

        public float stateTimer;
        public float offsetY;
        public float baseY;
        public float animatedY;

        public TextMeshProUGUI? text;
        public Image? background;

        public string message = msg;
        public string colorizedMessage = msg;
        public float height = MessageHeight;
    }
}