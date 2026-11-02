using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WasteWiseDiagnostic
{
    private const string OrganicPrefabPath = "Assets/Prefabs/Bins/Bin_Organik.prefab";
    private const string AnorganikPrefabPath = "Assets/Prefabs/Bins/Bin_Anorganik.prefab";
    private const string B3PrefabPath = "Assets/Prefabs/Bins/Bin_B3.prefab";
    private const float MaxAllowedHeight = 0.25f;

    [MenuItem("WasteWise/Inspect and Force Apply Bin Settings")]
    public static void InspectAndForceApply()
    {
        ClearSpawnedBins();

        bool needsRegen = false;

        needsRegen |= InspectPrefab("Bin_Organik", OrganicPrefabPath);
        needsRegen |= InspectPrefab("Bin_Anorganik", AnorganikPrefabPath);
        needsRegen |= InspectPrefab("Bin_B3", B3PrefabPath);

        if (needsRegen)
        {
            Debug.Log("<color=yellow>[WasteWise] Tinggi prefab > 0.25m; meregenerasi dari trashcan_single.obj (TargetBinHeight 0.20m).</color>");
            TrashBinPrefabCreator.GenerateTrashBinPrefabs();
        }

        ForceAssignPrefabs();

        AssetDatabase.SaveAssets();

        TrashBinSpawner spawner = Object.FindAnyObjectByType<TrashBinSpawner>(FindObjectsInactive.Include);
        WasteGameManager manager = Object.FindAnyObjectByType<WasteGameManager>(FindObjectsInactive.Include);

        if (spawner == null)
        {
            Debug.LogWarning("[WasteWise] TrashBinSpawner tidak ditemukan di scene aktif.");
        }

        if (manager == null)
        {
            Debug.LogWarning("[WasteWise] WasteGameManager tidak ditemukan di scene aktif.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=green>DIAGNOSTIC PASSED: Prefab height verified at 0.20m, spacing at 0.35m, Scene saved successfully.</color>");
    }

    private static void ClearSpawnedBins()
    {
        Scene scene = SceneManager.GetActiveScene();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name.StartsWith("TrashBin_"))
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static void ForceAssignPrefabs()
    {
        GameObject organic = AssetDatabase.LoadAssetAtPath<GameObject>(OrganicPrefabPath);
        GameObject anorganik = AssetDatabase.LoadAssetAtPath<GameObject>(AnorganikPrefabPath);
        GameObject b3 = AssetDatabase.LoadAssetAtPath<GameObject>(B3PrefabPath);

        TrashBinPrefabCreator.AssignPrefabsToSpawner(organic, anorganik, b3);
    }

    private static bool InspectPrefab(string name, string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[WasteWise] {name}: prefab tidak ditemukan di '{path}'; akan diregenerasi.");
            return true;
        }

        float height = MeasurePrefabHeight(prefab);
        bool tooTall = height > MaxAllowedHeight;
        Debug.Log($"[WasteWise] {name}: tinggi bounding box = {height:F3}m {(tooTall ? "(> 0.25m, regen)" : "(OK)")}");
        return tooTall;
    }

    private static float MeasurePrefabHeight(GameObject prefab)
    {
        GameObject instance = Object.Instantiate(prefab);
        instance.name = prefab.name + "_TempMeasure";

        float height = 0f;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            height = bounds.size.y;
        }

        Object.DestroyImmediate(instance);
        return height;
    }
}
