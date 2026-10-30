using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public static class TrashBinPrefabCreator
{
    private const string MaterialsFolder = "Assets/Materials";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string BinsFolder = "Assets/Prefabs/Bins";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    [MenuItem("WasteWise/Generate Trash Bin Prefabs")]
    public static void GenerateTrashBinPrefabs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WasteWise] Hentikan Play Mode sebelum menjalankan Generate Trash Bin Prefabs.");
            return;
        }

        Shader litShader = Shader.Find(UrpLitShaderName);

        if (litShader == null)
        {
            Debug.LogError($"[WasteWise] Shader '{UrpLitShaderName}' tidak ditemukan. Pastikan URP terpasang.");
            return;
        }

        EnsureFolders();

        Material greenMaterial = CreateOrUpdateMaterial(
            "Assets/Materials/Mat_Hijau.mat",
            "Mat_Hijau",
            new Color(0f, 0.8f, 0.2f),
            litShader);

        Material yellowMaterial = CreateOrUpdateMaterial(
            "Assets/Materials/Mat_Kuning.mat",
            "Mat_Kuning",
            new Color(0.9f, 0.8f, 0.1f),
            litShader);

        Material redMaterial = CreateOrUpdateMaterial(
            "Assets/Materials/Mat_Merah.mat",
            "Mat_Merah",
            new Color(0.8f, 0.1f, 0.1f),
            litShader);

        GameObject organicPrefab = CreateBinPrefab(
            "OrganicBin",
            "Assets/Prefabs/Bins/OrganicBin.prefab",
            greenMaterial);

        GameObject inorganicPrefab = CreateBinPrefab(
            "InorganicBin",
            "Assets/Prefabs/Bins/InorganicBin.prefab",
            yellowMaterial);

        GameObject b3Prefab = CreateBinPrefab(
            "B3Bin",
            "Assets/Prefabs/Bins/B3Bin.prefab",
            redMaterial);

        AssetDatabase.SaveAssets();

        AssignPrefabsToSpawner(organicPrefab, inorganicPrefab, b3Prefab);

        Debug.Log("[WasteWise] Generate Trash Bin Prefabs selesai.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        if (!AssetDatabase.IsValidFolder(PrefabsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(BinsFolder))
        {
            AssetDatabase.CreateFolder(PrefabsFolder, "Bins");
        }
    }

    private static Material CreateOrUpdateMaterial(
        string assetPath,
        string materialName,
        Color color,
        Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        if (material == null)
        {
            material = new Material(shader);
            material.name = materialName;
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);

        return material;
    }

    private static GameObject CreateBinPrefab(string prefabName, string prefabPath, Material material)
    {
        GameObject root = new GameObject(prefabName);

        CapsuleCollider capsuleCollider = root.AddComponent<CapsuleCollider>();
        capsuleCollider.center = new Vector3(0f, 0.2f, 0f);
        capsuleCollider.height = 0.4f;
        capsuleCollider.radius = 0.125f;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);

        Collider visualCollider = visual.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Object.DestroyImmediate(visualCollider);
        }

        MeshRenderer meshRenderer = visual.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            meshRenderer.sharedMaterial = material;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        if (prefab == null)
        {
            Debug.LogError($"[WasteWise] Gagal membuat prefab '{prefabPath}'.");
        }
        else
        {
            Debug.Log($"[WasteWise] Prefab '{prefabName}' dibuat di '{prefabPath}'.");
        }

        return prefab;
    }

    private static void AssignPrefabsToSpawner(
        GameObject organicPrefab,
        GameObject inorganicPrefab,
        GameObject b3Prefab)
    {
        TrashBinSpawner spawner = FindSpawnerInActiveScene();

        if (spawner == null)
        {
            Debug.LogWarning("[WasteWise] TrashBinSpawner tidak ditemukan di scene aktif. Tambahkan komponen TrashBinSpawner ke XR Origin lalu jalankan menu lagi.");
            return;
        }

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        bool allAssigned = true;

        allAssigned &= AssignObjectReference(serializedSpawner, "organicBinPrefab", organicPrefab);
        allAssigned &= AssignObjectReference(serializedSpawner, "inorganicBinPrefab", inorganicPrefab);
        allAssigned &= AssignObjectReference(serializedSpawner, "b3BinPrefab", b3Prefab);

        ARRaycastManager raycastManager = FindRaycastManager(spawner);

        if (raycastManager != null)
        {
            allAssigned &= AssignObjectReference(serializedSpawner, "raycastManager", raycastManager);
        }
        else
        {
            Debug.LogWarning("[WasteWise] ARRaycastManager tidak ditemukan. Field raycastManager tidak diubah.");
            allAssigned = false;
        }

        serializedSpawner.ApplyModifiedProperties();
        EditorUtility.SetDirty(spawner);

        if (spawner.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }

        if (allAssigned)
        {
            Debug.Log($"[WasteWise] Prefab tempat sampah dan ARRaycastManager berhasil dihubungkan ke TrashBinSpawner pada '{spawner.gameObject.name}'.");
        }
        else
        {
            Debug.LogWarning("[WasteWise] Sebagian referensi TrashBinSpawner belum berhasil dihubungkan. Periksa Console.");
        }
    }

    private static bool AssignObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        Object value)
    {
        if (value == null)
        {
            Debug.LogWarning($"[WasteWise] Referensi untuk field '{propertyName}' kosong; field tidak diubah.");
            return false;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogError($"[WasteWise] Field '{propertyName}' tidak ditemukan pada TrashBinSpawner.");
            return false;
        }

        property.objectReferenceValue = value;
        return true;
    }

    private static TrashBinSpawner FindSpawnerInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid())
        {
            return null;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        foreach (GameObject rootObject in rootObjects)
        {
            XROrigin xrOrigin = rootObject.GetComponentInChildren<XROrigin>(true);

            if (xrOrigin == null)
            {
                continue;
            }

            TrashBinSpawner spawnerOnOrigin = xrOrigin.GetComponent<TrashBinSpawner>();

            if (spawnerOnOrigin != null)
            {
                return spawnerOnOrigin;
            }
        }

        foreach (GameObject rootObject in rootObjects)
        {
            TrashBinSpawner spawner = rootObject.GetComponentInChildren<TrashBinSpawner>(true);

            if (spawner != null)
            {
                return spawner;
            }
        }

        return null;
    }

    private static ARRaycastManager FindRaycastManager(TrashBinSpawner spawner)
    {
        ARRaycastManager raycastManager = spawner.GetComponent<ARRaycastManager>();

        if (raycastManager == null)
        {
            raycastManager = spawner.GetComponentInChildren<ARRaycastManager>(true);
        }

        if (raycastManager == null)
        {
            XROrigin xrOrigin = spawner.GetComponent<XROrigin>();

            if (xrOrigin != null)
            {
                raycastManager = xrOrigin.GetComponentInChildren<ARRaycastManager>(true);
            }
        }

        if (raycastManager == null)
        {
            raycastManager = Object.FindAnyObjectByType<ARRaycastManager>(FindObjectsInactive.Include);
        }

        return raycastManager;
    }
}
