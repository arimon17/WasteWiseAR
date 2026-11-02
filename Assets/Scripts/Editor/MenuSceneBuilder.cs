using System.Collections.Generic;
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

public static class MenuSceneBuilder
{
    private const string MenuPath = "WasteWise/Build MenuScene and Setup Navigation";
    private const string ScenesFolder = "Assets/Scenes";
    private const string MenuScenePath = "Assets/Scenes/MenuScene.unity";
    private const string ArScenePath = "Assets/Scenes/ARScene.unity";
    private const string FallbackArScenePath = "Assets/Scenes/SampleScene.unity";

    private const string CanvasName = "MenuCanvas";
    private const string EventSystemName = "EventSystem";
    private const string CameraName = "MenuCamera";

    // Token design.md. Skala 3x: nilai px design (baseline 360dp) -> canvas 1080x1920.
    private static readonly Color BgApp = Hex("#0f172a");
    private static readonly Color SurfaceCard = Hex("#1e293b");
    private static readonly Color SurfaceOverlay = Hex("#0f172a");
    private static readonly Color Primary = Hex("#10b981");
    private static readonly Color AccentGold = Hex("#fbbf24");
    private static readonly Color TextMain = Hex("#f8fafc");
    private static readonly Color TextSub = Hex("#94a3b8");
    private static readonly Color BorderColor = Hex("#334155");

    private const float ButtonHeight = 156f;   // 52px * 3
    private const float ButtonFontSize = 48f;  // 16pt * 3

    // Padding internal kartu katalog (design.md 3x): design 24/32px -> canvas 72/96.
    private const float CardPadTop = 72f;
    private const float CardPadBottom = 72f;
    private const float CardPadLeft = 96f;
    private const float CardPadRight = 96f;
    private const float CatalogCardHeight = 360f;

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    private struct WasteEntry
    {
        public string name;
        public string category;
        public string description;
    }

    private static readonly WasteEntry[] Catalog =
    {
        new WasteEntry { name = "Kulit Pisang", category = "Organik", description = "Sisa buah yang mudah membusuk; dapat dijadikan kompos." },
        new WasteEntry { name = "Sisa Sayur", category = "Organik", description = "Sisa sayuran dapur; bahan kompos yang baik." },
        new WasteEntry { name = "Botol Plastik", category = "Anorganik", description = "Botol PET; bilas lalu kumpulkan untuk didaur ulang." },
        new WasteEntry { name = "Bungkus Mi Instan", category = "Anorganik", description = "Plastik kemasan sulit terurai; masukkan wadah anorganik." },
        new WasteEntry { name = "Kardus", category = "Anorganik", description = "Kertas tebal; dapat didaur ulang menjadi produk baru." },
        new WasteEntry { name = "Kaleng", category = "Anorganik", description = "Kaleng aluminium bernilai jual untuk daur ulang." },
        new WasteEntry { name = "Baterai Bekas", category = "B3", description = "Mengandung logam berat berbahaya; buang ke wadah B3." },
        new WasteEntry { name = "Lampu Rusak", category = "B3", description = "Lampu mengandung merkuri sehingga termasuk limbah B3." }
    };

    private static TMP_FontAsset font;

    [MenuItem(MenuPath)]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WasteWise] Hentikan Play Mode sebelum menjalankan Build MenuScene and Setup Navigation.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(ScenesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        Scene scene = File.Exists(MenuScenePath)
            ? EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Bangun ulang dari nol: buang objek hasil build sebelumnya agar tidak menumpuk.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CanvasName || root.name == EventSystemName || root.name == CameraName)
            {
                Object.DestroyImmediate(root);
            }
        }

        font = TMP_Settings.defaultFontAsset;

        if (font == null)
        {
            Debug.LogWarning("[WasteWise] Font TMP default tidak ditemukan. Import lewat Window > TextMeshPro > Import TMP Essential Resources.");
        }

        string arPath = ResolveArScenePath();
        string arSceneName = arPath != null ? Path.GetFileNameWithoutExtension(arPath) : "ARScene";

        BuildMenuUi(arSceneName);
        SetupBuildSettings(arPath);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        AssetDatabase.SaveAssets();

        if (arPath == FallbackArScenePath)
        {
            Debug.LogWarning("[WasteWise] 'Assets/Scenes/ARScene.unity' belum ada; scene AR yang didaftarkan adalah SampleScene.unity. Ganti nama file itu menjadi ARScene.unity lalu jalankan menu ini lagi.");
        }

        Debug.Log($"[WasteWise] MenuScene selesai dibangun. Tombol 'Mulai Latihan' menuju scene '{arSceneName}'.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "WasteWise AR",
                $"MenuScene selesai dibangun.\n\nTombol 'Mulai Latihan' menuju scene '{arSceneName}'.\nMenuScene & scene AR sudah didaftarkan ke Build Settings.",
                "OK");
        }
    }

    private static string ResolveArScenePath()
    {
        if (File.Exists(ArScenePath))
        {
            return ArScenePath;
        }

        return File.Exists(FallbackArScenePath) ? FallbackArScenePath : null;
    }

    private static void SetupBuildSettings(string arPath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true)
        };

        if (arPath != null)
        {
            scenes.Add(new EditorBuildSettingsScene(arPath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void BuildMenuUi(string arSceneName)
    {
        GameObject cameraObject = new GameObject(CameraName);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BgApp;
        cameraObject.tag = "MainCamera";

        GameObject eventSystemObject = new GameObject(EventSystemName, typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif

        GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        Transform root = canvasObject.transform;

        BuildMainMenu(root, out Button playButton, out Button catalogButton, out Button infoButton, out TextMeshProUGUI highScoreText);

        Button catalogClose = BuildCatalogPanel(root);
        Button infoClose = BuildInfoPanel(root);

        GameObject controllerObject = NewUi("MainMenuController", root);
        Stretch(controllerObject.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        MainMenuController controller = controllerObject.AddComponent<MainMenuController>();

        WireController(controller, playButton, catalogButton, infoButton, catalogClose, infoClose, highScoreText, arSceneName);
    }

    private static void BuildMainMenu(Transform root, out Button playButton, out Button catalogButton, out Button infoButton, out TextMeshProUGUI highScoreText)
    {
        GameObject panel = CreatePanel("MainMenuPanel", root, BgApp);

        TextMeshProUGUI title = CreateText("Title", panel.transform, "WasteWise AR", 100f, TextAlignmentOptions.Center, TextMain);
        title.fontStyle = FontStyles.Bold;
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(960f, 140f));

        TextMeshProUGUI subtitle = CreateText("Subtitle", panel.transform, "Aplikasi Edukasi Pemilahan Sampah Interaktif", 42f, TextAlignmentOptions.Center, TextSub);
        SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(960f, 80f));

        highScoreText = CreateStatCard(panel.transform);

        playButton = CreateButton("PlayButton", panel.transform, "Mulai Latihan AR", new Vector2(0f, 160f));
        catalogButton = CreateOutlinedButton("CatalogButton", panel.transform, "Katalog Sampah", new Vector2(0f, -40f));
        infoButton = CreateOutlinedButton("InfoButton", panel.transform, "Info Kategori", new Vector2(0f, -240f));
    }

    private static Button BuildCatalogPanel(Transform root)
    {
        GameObject panel = CreatePanel("CatalogPanel", root, SurfaceOverlay);

        Button close = CreateBackButton("BackButton", panel.transform, "← Katalog Sampah");

        CreateScrollView(panel.transform);

        panel.AddComponent<CanvasGroup>();
        panel.AddComponent<UIFadePanel>();
        panel.SetActive(false);
        return close;
    }

    private static Button BuildInfoPanel(Transform root)
    {
        GameObject panel = CreatePanel("InfoPanel", root, SurfaceOverlay);

        Button close = CreateBackButton("BackButton", panel.transform, "← Panduan Kategori");

        BuildCategoryCard(panel.transform, "ORGANIK", "Sampah mudah terurai dari sisa makhluk hidup: sisa makanan, daun, dan kulit buah. Dapat dijadikan kompos.", Hex("#2e7d32"), -180f);
        BuildCategoryCard(panel.transform, "ANORGANIK", "Sampah sulit terurai namun dapat didaur ulang: plastik, kertas, kaleng, kardus, dan botol.", Hex("#fbc02d"), -560f);
        BuildCategoryCard(panel.transform, "B3", "Bahan Berbahaya dan Beracun: limbah kimia seperti baterai, lampu, dan obat kedaluwarsa.", Hex("#c62828"), -940f);

        panel.AddComponent<CanvasGroup>();
        panel.AddComponent<UIFadePanel>();
        panel.SetActive(false);
        return close;
    }

    private static void CreateScrollView(Transform parent)
    {
        GameObject scrollObject = NewUi("ScrollView", parent);
        Stretch(scrollObject.GetComponent<RectTransform>(), 60f, 60f, 200f, 200f);

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.13f;
        scrollRect.scrollSensitivity = 30f;

        GameObject viewport = NewUi("Viewport", scrollObject.transform);
        Stretch(viewport.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = NewUi("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 36f;
        layout.padding = new RectOffset(24, 24, 24, 24);

        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        foreach (WasteEntry entry in Catalog)
        {
            CreateCatalogRow(content.transform, entry);
        }
    }

    private static void CreateCatalogRow(Transform parent, WasteEntry entry)
    {
        GameObject row = NewUi("Item_" + entry.name, parent);

        LayoutElement element = row.AddComponent<LayoutElement>();
        element.preferredHeight = CatalogCardHeight;
        element.flexibleWidth = 1f;

        Image card = row.AddComponent<Image>();
        card.color = SurfaceCard;
        card.sprite = UISpriteGenerator.GetRounded(16);
        card.type = Image.Type.Sliced;

        Outline border = row.AddComponent<Outline>();
        border.effectColor = BorderColor;
        border.effectDistance = new Vector2(2f, 2f);

        TextMeshProUGUI title = CreateText("Name", row.transform, entry.name, 48f, TextAlignmentOptions.TopLeft, TextMain);
        // Judul panjang dipaksa satu baris (auto-size lalu ellipsis) agar tidak menimpa chip kategori.
        title.enableAutoSizing = true;
        title.fontSizeMin = 32f;
        title.fontSizeMax = 48f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(CardPadLeft, -CardPadTop);
        titleRect.sizeDelta = new Vector2(500f, 60f);

        CreateCategoryChip(row.transform, entry.category);

        TextMeshProUGUI description = CreateText("Description", row.transform, entry.description, 45f, TextAlignmentOptions.TopLeft, TextSub);
        description.lineSpacing = -10f;
        float descriptionTop = CardPadTop + 84f;
        StretchTop(description.rectTransform, CardPadLeft, CardPadRight, descriptionTop, CatalogCardHeight - descriptionTop - CardPadBottom);
    }

    private static void WireController(MainMenuController controller, Button play, Button catalog, Button info, Button catalogClose, Button infoClose, TMP_Text highScore, string arSceneName)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SetObject(serialized, "playButton", play);
        SetObject(serialized, "catalogButton", catalog);
        SetObject(serialized, "infoButton", info);
        SetObject(serialized, "catalogCloseButton", catalogClose);
        SetObject(serialized, "infoCloseButton", infoClose);
        SetObject(serialized, "highScoreText", highScore);
        SetObject(serialized, "catalogPanel", FindByName("CatalogPanel"));
        SetObject(serialized, "infoPanel", FindByName("InfoPanel"));

        SerializedProperty nameProperty = serialized.FindProperty("arSceneName");
        if (nameProperty != null)
        {
            nameProperty.stringValue = arSceneName;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindByName(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    private static void SetObject(SerializedObject serialized, string field, Object value)
    {
        SerializedProperty property = serialized.FindProperty(field);

        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static GameObject CreatePanel(string name, Transform parent, Color background)
    {
        GameObject panel = NewUi(name, parent);
        Stretch(panel.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        Image image = panel.AddComponent<Image>();
        image.color = background;
        image.sprite = UISpriteGenerator.GetRounded(24);
        image.type = Image.Type.Sliced;

        return panel;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position)
    {
        return CreateButton(name, parent, label, position, new Vector2(0.5f, 0.5f), new Vector2(720f, ButtonHeight));
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 anchor, Vector2 size)
    {
        GameObject buttonObject = NewUi(name, parent);
        SetRect(buttonObject.GetComponent<RectTransform>(), anchor, anchor, position, size);

        Image image = buttonObject.AddComponent<Image>();
        image.color = Primary;
        image.sprite = UISpriteGenerator.GetRounded(36);
        image.type = Image.Type.Sliced;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.96f, 0.94f);
        colors.pressedColor = Hex("#059669");
        colors.selectedColor = Color.white;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, ButtonFontSize, TextAlignmentOptions.Center, Color.white);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    private static Button CreateOutlinedButton(string name, Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = NewUi(name, parent);
        SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(720f, ButtonHeight));

        Image image = buttonObject.AddComponent<Image>();
        image.color = SurfaceCard;
        image.sprite = UISpriteGenerator.GetRounded(36);
        image.type = Image.Type.Sliced;

        Outline border = buttonObject.AddComponent<Outline>();
        border.effectColor = Primary;
        border.effectDistance = new Vector2(2f, 2f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.96f, 0.94f);
        colors.pressedColor = Hex("#134e4a");
        colors.selectedColor = Color.white;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, ButtonFontSize, TextAlignmentOptions.Center, TextMain);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    private static Button CreateBackButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = NewUi(name, parent);
        SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -32f), new Vector2(560f, 100f));

        Image image = buttonObject.AddComponent<Image>();
        image.color = SurfaceCard;
        image.sprite = UISpriteGenerator.GetRounded(16);
        image.type = Image.Type.Sliced;

        Outline border = buttonObject.AddComponent<Outline>();
        border.effectColor = BorderColor;
        border.effectDistance = new Vector2(2f, 2f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.96f, 0.94f);
        colors.pressedColor = Hex("#334155");
        colors.selectedColor = Color.white;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, label, 45f, TextAlignmentOptions.Center, TextMain);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    private static TextMeshProUGUI CreateStatCard(Transform parent)
    {
        GameObject card = NewUi("StatCard", parent);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -400f), new Vector2(960f, 240f));

        Image background = card.AddComponent<Image>();
        background.color = SurfaceCard;
        background.sprite = UISpriteGenerator.GetRounded(16);
        background.type = Image.Type.Sliced;

        Outline border = card.AddComponent<Outline>();
        border.effectColor = BorderColor;
        border.effectDistance = new Vector2(2f, 2f);

        TextMeshProUGUI label = CreateText("Label", card.transform, "Skor Terbaik", 42f, TextAlignmentOptions.Center, TextSub);
        SetRect(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 60f));

        TextMeshProUGUI value = CreateText("Value", card.transform, "0", 80f, TextAlignmentOptions.Center, AccentGold);
        value.fontStyle = FontStyles.Bold;
        SetRect(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(800f, 110f));

        return value;
    }

    private static void BuildCategoryCard(Transform parent, string title, string description, Color accent, float top)
    {
        GameObject card = NewUi("Card_" + title, parent);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(920f, 340f));

        Image background = card.AddComponent<Image>();
        background.color = SurfaceCard;
        background.sprite = UISpriteGenerator.GetRounded(16);
        background.type = Image.Type.Sliced;

        Outline border = card.AddComponent<Outline>();
        border.effectColor = BorderColor;
        border.effectDistance = new Vector2(2f, 2f);

        GameObject accentBar = NewUi("Accent", card.transform);
        RectTransform accentRect = accentBar.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(24f, 0f);
        Image accentImage = accentBar.AddComponent<Image>();
        accentImage.color = accent;
        accentImage.raycastTarget = false;

        TextMeshProUGUI titleText = CreateText("Title", card.transform, title, 60f, TextAlignmentOptions.TopLeft, TextMain);
        titleText.fontStyle = FontStyles.Bold;
        StretchTop(titleText.rectTransform, 96f, 60f, 36f, 80f);

        TextMeshProUGUI descriptionText = CreateText("Description", card.transform, description, 45f, TextAlignmentOptions.TopLeft, TextSub);
        descriptionText.lineSpacing = -10f;
        StretchTop(descriptionText.rectTransform, 96f, 60f, 140f, 160f);
    }

    private static void CreateCategoryChip(Transform parent, string category)
    {
        GameObject chip = NewUi("CategoryChip", parent);
        RectTransform chipRect = chip.GetComponent<RectTransform>();
        chipRect.anchorMin = new Vector2(1f, 1f);
        chipRect.anchorMax = new Vector2(1f, 1f);
        chipRect.pivot = new Vector2(1f, 1f);
        chipRect.anchoredPosition = new Vector2(-CardPadRight, -CardPadTop);
        chipRect.sizeDelta = new Vector2(220f, 60f);

        Image chipImage = chip.AddComponent<Image>();
        chipImage.color = CategoryColor(category);
        chipImage.sprite = UISpriteGenerator.GetRounded(14);
        chipImage.type = Image.Type.Sliced;

        TextMeshProUGUI label = CreateText("Label", chip.transform, category, 36f, TextAlignmentOptions.Center, CategoryTextColor(category));
        label.fontStyle = FontStyles.Bold;
        Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
    }

    private static Color CategoryColor(string category)
    {
        switch (category)
        {
            case "Organik": return Hex("#22c55e");
            case "Anorganik": return Hex("#facc15");
            case "B3": return Hex("#ef4444");
            default: return TextSub;
        }
    }

    private static Color CategoryTextColor(string category)
    {
        return category == "Anorganik" ? SurfaceCard : Color.white;
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
        text.extraPadding = true;

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
}
