using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneCleanerAndLabeler
{
    private const string MenuPath = "WasteWise/Clean Scene and Add Bin World Labels";
    private const string BinsFolder = "Assets/Prefabs/Bins";
    private const string LabelName = "Label";
    private const float LabelHeight = 0.55f;

    private static readonly string[] DummyNames = { "OrganicBin", "InorganicBin", "B3Bin", "Cube", "Cylinder" };

    private struct BinLabel
    {
        public string prefabName;
        public string text;
        public Color color;
    }

    private static readonly BinLabel[] Bins =
    {
        new BinLabel { prefabName = "OrganicBin", text = "ORGANIK", color = new Color(0f, 0.8f, 0.2f) },
        new BinLabel { prefabName = "InorganicBin", text = "ANORGANIK", color = new Color(0.9f, 0.8f, 0.1f) },
        new BinLabel { prefabName = "B3Bin", text = "B3", color = new Color(0.8f, 0.1f, 0.1f) }
    };

    [MenuItem(MenuPath)]
    public static void CleanAndLabel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WasteWise] Hentikan Play Mode sebelum menjalankan Clean Scene and Add Bin World Labels.");
            return;
        }

        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("[WasteWise] Tutup Prefab Mode sebelum menjalankan Clean Scene and Add Bin World Labels.");
            return;
        }

        int removed = CleanDummyObjects();
        int labeled = 0;

        foreach (BinLabel bin in Bins)
        {
            if (AddLabelToPrefab(bin))
            {
                labeled++;
            }
        }

        Scene scene = SceneManager.GetActiveScene();

        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"[WasteWise] Clean & Label selesai. Objek dummy dihapus: {removed}. Prefab dilabeli: {labeled}/{Bins.Length}. Tekan Ctrl+S untuk menyimpan scene.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "WasteWise AR",
                $"Selesai.\n\nObjek dummy dihapus: {removed}\nPrefab dilabeli: {labeled}/{Bins.Length}\n\nTekan Ctrl+S untuk menyimpan scene.",
                "OK");
        }
    }

    private static int CleanDummyObjects()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
        {
            return 0;
        }

        List<GameObject> matches = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (IsDummyName(child.name))
                {
                    matches.Add(child.gameObject);
                }
            }
        }

        if (matches.Count == 0)
        {
            return 0;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clean WasteWise Dummy Objects");

        int removed = 0;

        foreach (GameObject go in matches)
        {
            if (go == null)
            {
                // Sudah ikut terhapus sebagai anak dari objek match sebelumnya.
                continue;
            }

            Undo.DestroyObjectImmediate(go);
            removed++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        return removed;
    }

    private static bool IsDummyName(string objectName)
    {
        foreach (string dummy in DummyNames)
        {
            if (objectName == dummy ||
                objectName.StartsWith(dummy + " ") ||
                objectName.StartsWith(dummy + "("))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AddLabelToPrefab(BinLabel bin)
    {
        string path = $"{BinsFolder}/{bin.prefabName}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning($"[WasteWise] Prefab '{path}' tidak ditemukan. Jalankan 'WasteWise/Generate Trash Bin Prefabs' terlebih dahulu.");
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform existing = root.transform.Find(LabelName);
        GameObject label = existing != null ? existing.gameObject : CreateLabel(root.transform);

        ConfigureLabel(label, bin);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return true;
    }

    private static GameObject CreateLabel(Transform parent)
    {
        GameObject label = new GameObject(LabelName, typeof(RectTransform), typeof(Canvas));
        label.transform.SetParent(parent, false);

        label.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        label.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 14f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(label.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        textObject.AddComponent<TextMeshProUGUI>();
        return label;
    }

    private static void ConfigureLabel(GameObject label, BinLabel bin)
    {
        label.transform.localPosition = new Vector3(0f, LabelHeight, 0f);
        label.transform.localRotation = Quaternion.identity;
        label.transform.localScale = Vector3.one * 0.01f;

        TextMeshProUGUI text = label.GetComponentInChildren<TextMeshProUGUI>(true);
        text.text = bin.text;
        text.color = bin.color;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = 6f;
        text.fontSizeMax = 24f;

        if (text.font == null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        if (text.font == null)
        {
            Debug.LogWarning("[WasteWise] Font TMP default tidak ditemukan. Import lewat Window > TextMeshPro > Import TMP Essential Resources.");
        }

        if (label.GetComponent<Billboard>() == null)
        {
            label.AddComponent<Billboard>();
        }
    }
}
