using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class ARHUDSceneSetup
{
    private const string MenuPath = "WasteWise/Setup AR HUD Canvas in Scene";
    private const string ArScenePath = "Assets/Scenes/ARScene.unity";
    private const string FallbackArScenePath = "Assets/Scenes/SampleScene.unity";
    private const string CanvasName = "ARHUDCanvas";
    private const string EventSystemName = "EventSystem";

    // Token design.md. Skala 3x: nilai px design (baseline 360dp) -> canvas 1080x1920.
    private static readonly Color SurfaceCard = Hex("#1e293b");
    private static readonly Color SurfaceOverlay = Hex("#0f172ae6");
    private static readonly Color Primary = Hex("#1f8a5a");
    private static readonly Color TextMain = Hex("#f8fafc");
    private static readonly Color TextSub = Hex("#94a3b8");

    private const float ButtonHeight = 156f;   // 52px * 3
    private const float ButtonFontSize = 48f;  // 16pt * 3

    private static TMP_FontAsset font;

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    [MenuItem(MenuPath)]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WasteWise] Hentikan Play Mode sebelum menjalankan Setup AR HUD Canvas in Scene.");
            return;
        }

        string scenePath = File.Exists(ArScenePath) ? ArScenePath : (File.Exists(FallbackArScenePath) ? FallbackArScenePath : null);

        if (scenePath == null)
        {
            Debug.LogError("[WasteWise] Scene AR tidak ditemukan (ARScene.unity atau SampleScene.unity).");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CanvasName)
            {
                Object.DestroyImmediate(root);
            }
        }

        font = TMP_Settings.defaultFontAsset;

        if (font == null)
        {
            Debug.LogWarning("[WasteWise] Font TMP default tidak ditemukan. Import lewat Window > TextMeshPro > Import TMP Essential Resources.");
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        ARHUDController controller = canvasObject.AddComponent<ARHUDController>();
        Transform root2 = canvasObject.transform;

        Element part = BuildHud(root2);

        WasteGameManager gameManager = Object.FindAnyObjectByType<WasteGameManager>(FindObjectsInactive.Include);

        if (gameManager == null)
        {
            Debug.LogWarning("[WasteWise] WasteGameManager tidak ditemukan di scene. Tambahkan komponen WasteGameManager ke XR Origin, lalu jalankan menu ini lagi (HUD sudah mencoba menemukannya otomatis saat runtime).");
        }

        Wire(controller, gameManager, part);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[WasteWise] AR HUD Canvas terpasang di '{scenePath}'.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("WasteWise AR", $"AR HUD Canvas selesai dipasang di scene '{scene.name}'.\n\nTekan Ctrl+S untuk menyimpan scene.", "OK");
        }
    }

    private struct Element
    {
        public TMP_Text itemName;
        public TMP_Text score;
        public TMP_Text progress;
        public Button menuButton;
        public GameObject feedbackPanel;
        public UIFadePanel feedbackFader;
        public Image feedbackIndicator;
        public TMP_Text feedbackStatus;
        public TMP_Text feedbackExplanation;
        public GameObject resultPanel;
        public UIFadePanel resultFader;
        public TMP_Text resultScore;
        public TMP_Text resultRecord;
        public Button backToMenuButton;
        public Button replayButton;
    }

    private static Element BuildHud(Transform root)
    {
        Element element = new Element();

        // Top Bar: floating card (surface-card, sudut tumpul, padding 12px 20px)
        GameObject topBar = NewUi("TopBar", root);
        RectTransform topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = new Vector2(0f, -24f);
        topBarRect.sizeDelta = new Vector2(-48f, 192f);

        Image topBarImage = topBar.AddComponent<Image>();
        topBarImage.color = SurfaceCard;
        ApplyRounded(topBarImage, 16);

        element.itemName = CreateText("ItemName", topBar.transform, "Sampah: -", 42f, TextAlignmentOptions.TopLeft, TextMain);
        SetRect(element.itemName.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -30f), new Vector2(620f, 56f));

        element.score = CreateText("Score", topBar.transform, "Skor: 0", 36f, TextAlignmentOptions.TopLeft, TextSub);
        SetRect(element.score.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -100f), new Vector2(380f, 50f));

        element.progress = CreateText("Progress", topBar.transform, "1 / 8", 36f, TextAlignmentOptions.TopLeft, TextSub);
        SetRect(element.progress.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(460f, -100f), new Vector2(260f, 50f));

        element.menuButton = CreateButton("MenuButton", topBar.transform, "Menu", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(180f, 120f), 36f);

        // Feedback Banner: toast bottom-stretch (menempel rapi di bawah, aman dari notch/nav bar)
        element.feedbackPanel = NewUi("FeedbackBanner", root);
        RectTransform toastRect = element.feedbackPanel.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0f, 0f);
        toastRect.anchorMax = new Vector2(1f, 0f);
        toastRect.pivot = new Vector2(0.5f, 0f);
        toastRect.anchoredPosition = new Vector2(0f, 80f);
        toastRect.sizeDelta = new Vector2(-48f, 300f);

        Image toast = element.feedbackPanel.AddComponent<Image>();
        toast.color = SurfaceCard;
        ApplyRounded(toast, 16);

        element.feedbackPanel.AddComponent<CanvasGroup>();
        element.feedbackFader = element.feedbackPanel.AddComponent<UIFadePanel>();

        element.feedbackIndicator = NewUi("Indicator", element.feedbackPanel.transform).AddComponent<Image>();
        element.feedbackIndicator.color = Hex("#2e7d32");
        RectTransform indicatorRect = element.feedbackIndicator.rectTransform;
        indicatorRect.anchorMin = new Vector2(0f, 0f);
        indicatorRect.anchorMax = new Vector2(0f, 1f);
        indicatorRect.pivot = new Vector2(0f, 0.5f);
        indicatorRect.anchoredPosition = new Vector2(24f, 0f);
        indicatorRect.sizeDelta = new Vector2(36f, -48f);

        element.feedbackStatus = CreateText("Status", element.feedbackPanel.transform, "BENAR", 60f, TextAlignmentOptions.TopLeft, TextMain);
        element.feedbackStatus.fontStyle = FontStyles.Bold;
        StretchTop(element.feedbackStatus.rectTransform, 96f, 40f, 36f, 70f);

        element.feedbackExplanation = CreateText("Explanation", element.feedbackPanel.transform, "", 45f, TextAlignmentOptions.TopLeft, TextSub);
        Stretch(element.feedbackExplanation.rectTransform, 96f, 40f, 130f, 30f);
        element.feedbackPanel.SetActive(false);

        // Session Result: App Card
        element.resultPanel = NewUi("ResultPanel", root);
        Stretch(element.resultPanel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        element.resultPanel.AddComponent<Image>().color = SurfaceOverlay;
        element.resultPanel.AddComponent<CanvasGroup>();
        element.resultFader = element.resultPanel.AddComponent<UIFadePanel>();

        GameObject card = NewUi("Card", element.resultPanel.transform);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 900f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = SurfaceCard;
        ApplyRounded(cardImage, 24);

        TMP_Text cardTitle = CreateText("Title", card.transform, "Sesi Selesai", 84f, TextAlignmentOptions.Center, TextMain);
        cardTitle.fontStyle = FontStyles.Bold;
        StretchTop(cardTitle.rectTransform, 40f, 40f, 60f, 110f);

        element.resultScore = CreateText("ResultScore", card.transform, "Skor Akhir: 0", 60f, TextAlignmentOptions.Center, TextMain);
        element.resultScore.fontStyle = FontStyles.Bold;
        StretchTop(element.resultScore.rectTransform, 60f, 60f, 210f, 160f);

        element.resultRecord = CreateText("ResultRecord", card.transform, "Rekor terbaik: 0", 45f, TextAlignmentOptions.Center, TextSub);
        StretchTop(element.resultRecord.rectTransform, 60f, 60f, 380f, 90f);

        element.replayButton = CreateButton("ReplayButton", card.transform, "Latihan Lagi", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(720f, ButtonHeight), ButtonFontSize);
        element.backToMenuButton = CreateButton("BackToMenuButton", card.transform, "Kembali ke Menu", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(720f, ButtonHeight), ButtonFontSize);
        element.resultPanel.SetActive(false);

        return element;
    }

    private static void Wire(ARHUDController controller, WasteGameManager gameManager, Element e)
    {
        SerializedObject serialized = new SerializedObject(controller);

        SetObject(serialized, "gameManager", gameManager);
        SetObject(serialized, "itemNameText", e.itemName);
        SetObject(serialized, "scoreText", e.score);
        SetObject(serialized, "progressText", e.progress);
        SetObject(serialized, "menuButton", e.menuButton);
        SetObject(serialized, "feedbackPanel", e.feedbackPanel);
        SetObject(serialized, "feedbackFader", e.feedbackFader);
        SetObject(serialized, "feedbackIndicator", e.feedbackIndicator);
        SetObject(serialized, "feedbackStatusText", e.feedbackStatus);
        SetObject(serialized, "feedbackExplanationText", e.feedbackExplanation);
        SetObject(serialized, "resultPanel", e.resultPanel);
        SetObject(serialized, "resultFader", e.resultFader);
        SetObject(serialized, "resultScoreText", e.resultScore);
        SetObject(serialized, "resultRecordText", e.resultRecord);
        SetObject(serialized, "backToMenuButton", e.backToMenuButton);
        SetObject(serialized, "replayButton", e.replayButton);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(EventSystemName, typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void SetObject(SerializedObject serialized, string field, Object value)
    {
        SerializedProperty property = serialized.FindProperty(field);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject buttonObject = NewUi(name, parent);
        SetRect(buttonObject.GetComponent<RectTransform>(), anchor, pivot, position, size);

        Image image = buttonObject.AddComponent<Image>();
        image.color = Primary;
        ApplyRounded(image, 16);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.96f, 0.94f);
        colors.pressedColor = Hex("#156340");
        colors.selectedColor = Color.white;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, fontSize, TextAlignmentOptions.Center, Color.white);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = NewUi(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;

        if (font != null)
        {
            text.font = font;
        }

        return text;
    }

    private static GameObject NewUi(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        return rect;
    }

    private static RectTransform StretchTop(RectTransform rect, float left, float right, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2((left - right) * 0.5f, -top);
        rect.sizeDelta = new Vector2(-(left + right), height);
        return rect;
    }

    private static void ApplyRounded(Image image, int radius)
    {
        image.sprite = UISpriteGenerator.GetRounded(radius);
        image.type = Image.Type.Sliced;
    }
}
