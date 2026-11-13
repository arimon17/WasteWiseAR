using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WasteItemPrefabBuilder
{
    private const string ModelsFolder = "Assets/Models/Quaternius";
    private const string PrefabsFolder = "Assets/Prefabs/WasteItems";
    private const string MaterialsFolder = "Assets/Materials/WasteItems";
    private const string MenuPath = "WasteWise/Generate Waste Prefabs";
    private const float TargetMaxSize = 0.12f;
    private const float ColliderPadding = 1.08f;

    private static readonly Dictionary<string, Color> FallbackColors =
        new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "Apple", Hex("#C0392B") },
            { "Banana", Hex("#F4D03F") },
            { "Bread_Slice", Hex("#C99A5B") },
            { "Carrot", Hex("#E67E22") },
            { "FishBone", Hex("#D5C6A5") },
            { "Bottle1", Hex("#7DD3FC") },
            { "bottle-ketchup", Hex("#DC2626") },
            { "can", Hex("#CBD5E1") },
            { "candy-bar-wrapper", Hex("#F97316") },
            { "carton", Hex("#FDE68A") },
            { "battery", Hex("#334155") },
            { "bottle-oil", Hex("#B45309") },
            { "lightbulb", Hex("#FEF3C7") },
            { "paint", Hex("#0F766E") },
            { "Poison", Hex("#15803D") }
        };

    [MenuItem(MenuPath)]
    public static void GeneratePrefabs()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[WasteWise] Hentikan Play Mode sebelum generate prefab sampah.");
            return;
        }

        EnsureFolder(PrefabsFolder);
        EnsureFolder(MaterialsFolder);

        List<WasteItem> generated = new List<WasteItem>();

        foreach (WasteCatalogEntry entry in WasteCatalog.Entries)
        {
            string modelPath = $"{ModelsFolder}/{entry.modelFile}.obj";
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

            if (modelAsset == null)
            {
                Debug.LogWarning($"[WasteWise] Model '{modelPath}' tidak ditemukan; dilewati.");
                continue;
            }

            WasteItem prefab = BuildPrefab(modelAsset, entry);

            if (prefab != null)
            {
                generated.Add(prefab);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AssignToGameManager(generated);

        Debug.Log($"[WasteWise] {generated.Count} prefab sampah 3D dibuat di '{PrefabsFolder}' dan didaftarkan ke WasteGameManager.wasteItems.");
    }

    private static WasteItem BuildPrefab(GameObject modelAsset, WasteCatalogEntry entry)
    {
        string prefabPath = $"{PrefabsFolder}/Waste_{entry.modelFile}.prefab";
        GameObject root = new GameObject($"Waste_{entry.modelFile}");

        GameObject model = UnityEngine.Object.Instantiate(modelAsset);
        model.name = "Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;

        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        Bounds bounds = CalculateBounds(model);

        if (bounds.size.sqrMagnitude <= 0.000001f)
        {
            Debug.LogWarning($"[WasteWise] Model '{entry.modelFile}' tidak memiliki mesh yang bisa dirender; dilewati.");
            UnityEngine.Object.DestroyImmediate(root);
            return null;
        }

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float scale = maxSize > 0.0001f ? TargetMaxSize / maxSize : 1f;

        model.transform.localScale *= scale;
        model.transform.localPosition = new Vector3(
            -bounds.center.x * scale,
            -bounds.min.y * scale,
            -bounds.center.z * scale);

        Vector3 size = bounds.size * scale;

        EnsureMaterials(model, entry);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, size.y * 0.5f, 0f);
        box.size = new Vector3(
            Mathf.Max(size.x * ColliderPadding, 0.08f),
            Mathf.Max(size.y * ColliderPadding, 0.08f),
            Mathf.Max(size.z * ColliderPadding, 0.08f));

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        WasteItem item = root.AddComponent<WasteItem>();
        item.Configure(entry.itemName, entry.category, entry.explanation);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        if (saved == null)
        {
            Debug.LogError($"[WasteWise] Gagal membuat prefab '{prefabPath}'.");
            return null;
        }

        return saved.GetComponent<WasteItem>();
    }

    private static Bounds CalculateBounds(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void EnsureMaterials(GameObject model, WasteCatalogEntry entry)
    {
        Material fallback = null;

        foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (!NeedsFallback(materials[i]))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = GetOrCreateFallbackMaterial(entry);
                }

                if (fallback != null)
                {
                    materials[i] = fallback;
                }
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static bool NeedsFallback(Material material)
    {
        if (material == null || material.shader == null)
        {
            return true;
        }

        if (material.shader.name == "Hidden/InternalErrorShader")
        {
            return true;
        }

        return material.name.IndexOf("colormap", StringComparison.OrdinalIgnoreCase) >= 0
            && material.mainTexture == null;
    }

    private static Material GetOrCreateFallbackMaterial(WasteCatalogEntry entry)
    {
        string path = $"{MaterialsFolder}/Mat_{entry.modelFile}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                Debug.LogWarning("[WasteWise] Shader Lit tidak ditemukan; material fallback dilewati.");
                return null;
            }

            material = new Material(shader)
            {
                name = $"Mat_{entry.modelFile}"
            };
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", FallbackColorFor(entry.modelFile));
        material.SetFloat("_Smoothness", 0.12f);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);

        return material;
    }

    private static Color FallbackColorFor(string modelFile)
    {
        return FallbackColors.TryGetValue(modelFile, out Color color)
            ? color
            : new Color(0.7f, 0.7f, 0.7f);
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    private static void AssignToGameManager(List<WasteItem> prefabs)
    {
        if (prefabs.Count == 0)
        {
            return;
        }

        WasteGameManager manager = UnityEngine.Object.FindAnyObjectByType<WasteGameManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            Debug.LogWarning("[WasteWise] WasteGameManager tidak ditemukan. Prefab sudah dibuat; buka SampleScene lalu jalankan menu ini lagi untuk mendaftarkannya.");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty property = serializedManager.FindProperty("wasteItems");

        if (property == null)
        {
            Debug.LogError("[WasteWise] Field 'wasteItems' tidak ditemukan pada WasteGameManager.");
            return;
        }

        property.arraySize = prefabs.Count;

        for (int i = 0; i < prefabs.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        }

        serializedManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        Scene scene = manager.gameObject.scene;

        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"[WasteWise] {prefabs.Count} prefab sampah didaftarkan ke WasteGameManager.wasteItems.");
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "Assets" || AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);

        EnsureFolder(parent);

        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
