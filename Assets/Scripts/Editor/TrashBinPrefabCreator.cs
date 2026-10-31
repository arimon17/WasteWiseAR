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

        // Warna penutup/kaki netral (surface-card #1e293b) agar warna kategori tetap menonjol.
        Material lidMaterial = CreateOrUpdateMaterial(
            "Assets/Materials/Mat_BinLid.mat",
            "Mat_BinLid",
            new Color(0.12f, 0.16f, 0.23f),
            litShader);

        GameObject organicPrefab = CreateBinPrefab(
            "OrganicBin",
            "Assets/Prefabs/Bins/OrganicBin.prefab",
            greenMaterial,
            lidMaterial);

        GameObject inorganicPrefab = CreateBinPrefab(
            "InorganicBin",
            "Assets/Prefabs/Bins/InorganicBin.prefab",
            yellowMaterial,
            lidMaterial);

        GameObject b3Prefab = CreateBinPrefab(
            "B3Bin",
            "Assets/Prefabs/Bins/B3Bin.prefab",
            redMaterial,
            lidMaterial);

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
        material.SetFloat("_Smoothness", 0.1f); // matte finish
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);

        return material;
    }

    private static GameObject CreateBinPrefab(string prefabName, string prefabPath, Material bodyMaterial, Material lidMaterial)
    {
        GameObject root = new GameObject(prefabName);

        CapsuleCollider capsuleCollider = root.AddComponent<CapsuleCollider>();
        capsuleCollider.center = new Vector3(0f, 0.2f, 0f);
        capsuleCollider.height = 0.4f;
        capsuleCollider.radius = 0.125f;

        // Profil tempat sampah low-poly: lis/kaki bawah, badan, cekungan, lis mulut, penutup, pegangan.
        AddBinPart(root.transform, "Base", 0.15f, 0.03f, 0.015f, bodyMaterial);
        AddBinPart(root.transform, "Body", 0.115f, 0.30f, 0.18f, bodyMaterial);
        AddBinPart(root.transform, "Groove", 0.128f, 0.02f, 0.12f, lidMaterial);
        AddBinPart(root.transform, "Rim", 0.15f, 0.04f, 0.35f, bodyMaterial);
        AddBinPart(root.transform, "Lid", 0.155f, 0.03f, 0.385f, lidMaterial);
        AddBinPart(root.transform, "Handle", 0.05f, 0.03f, 0.415f, lidMaterial);

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

    // Cylinder primitive: radius 0.5 & tinggi 2 pada skala 1.
    private static void AddBinPart(Transform parent, string name, float radius, float height, float centerY, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = new Vector3(0f, centerY, 0f);
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

        // Collider anak dibuang; root prefab sudah punya CapsuleCollider sebagai target tap.
        Collider partCollider = part.GetComponent<Collider>();

        if (partCollider != null)
        {
            Object.DestroyImmediate(partCollider);
        }

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
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
