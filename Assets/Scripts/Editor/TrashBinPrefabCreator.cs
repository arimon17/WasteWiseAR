using System.Collections.Generic;
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
    private const string SharedBinModelPath = "Assets/Models/trashcan_single.obj";
    private const float TargetBinHeight = 0.20f; // tinggi 1 tong tunggal dunia nyata (m)

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
            Hex("#2E7D32"),
            litShader);

        Material yellowMaterial = CreateOrUpdateMaterial(
            "Assets/Materials/Mat_Kuning.mat",
            "Mat_Kuning",
            Hex("#FBC02D"),
            litShader);

        Material redMaterial = CreateOrUpdateMaterial(
            "Assets/Materials/Mat_Merah.mat",
            "Mat_Merah",
            Hex("#C62828"),
            litShader);

        // Warna penutup/kaki netral (surface-card #1e293b) agar warna kategori tetap menonjol.
        Material lidMaterial = CreateOrUpdateMaterial(
            "Assets/Materials/Mat_BinLid.mat",
            "Mat_BinLid",
            new Color(0.12f, 0.16f, 0.23f),
            litShader);

        GameObject organicPrefab = ResolveBinPrefab("Bin_Organik", greenMaterial, lidMaterial);
        GameObject inorganicPrefab = ResolveBinPrefab("Bin_Anorganik", yellowMaterial, lidMaterial);
        GameObject b3Prefab = ResolveBinPrefab("Bin_B3", redMaterial, lidMaterial);

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

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
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

    // Prioritas model: trashcan_single.obj > model kustom per kategori > prosedural.
    private static GameObject ResolveBinPrefab(string prefabName, Material bodyMaterial, Material lidMaterial)
    {
        EnsureModelReadable();

        GameObject sharedModel = AssetDatabase.LoadAssetAtPath<GameObject>(SharedBinModelPath);

        if (sharedModel != null)
        {
            return BuildPrefabFromModel(sharedModel, prefabName, bodyMaterial, lidMaterial);
        }

        GameObject customModel = WasteModelAssets.FindCustomBinModel(prefabName);

        if (customModel != null)
        {
            Debug.Log($"[WasteWise] Model kustom '{prefabName}' dipakai menggantikan prefab prosedural.");
            return customModel;
        }

        return CreateBinPrefab(prefabName, $"{BinsFolder}/{prefabName}.prefab", bodyMaterial, lidMaterial);
    }

    // Model tunggal bersih: pisahkan sub-mesh penutup atas (Lid) agar bisa dibuka animasi, lalu beri warna accent.
    private static GameObject BuildPrefabFromModel(GameObject modelAsset, string prefabName, Material bodyMaterial, Material lidMaterial)
    {
        string prefabPath = $"{BinsFolder}/{prefabName}.prefab";
        Mesh mesh = GetSharedMesh(modelAsset);

        if (mesh == null)
        {
            Debug.LogWarning($"[WasteWise] Gagal memuat mesh dari '{SharedBinModelPath}'; memakai prefab prosedural.");
            return CreateBinPrefab(prefabName, prefabPath, bodyMaterial, lidMaterial);
        }

        int lidSubMesh = FindLidSubMesh(mesh);
        Mesh bodyMesh = ExtractBinMesh(mesh, lidSubMesh, false);
        Mesh lidMesh = ExtractBinMesh(mesh, lidSubMesh, true);

        if (bodyMesh == null)
        {
            Debug.LogWarning($"[WasteWise] Gagal memisahkan lid dari '{SharedBinModelPath}'; memakai prefab prosedural.");
            return CreateBinPrefab(prefabName, prefabPath, bodyMaterial, lidMaterial);
        }

        GameObject root = new GameObject(prefabName);
        root.transform.localRotation = Quaternion.identity; // kunci orientasi: tong tegak lurus

        Bounds fullBounds = mesh.bounds;
        float factor = fullBounds.size.y > 0.0001f ? TargetBinHeight / fullBounds.size.y : 1f;
        Vector3 normalOffset = new Vector3(
            -fullBounds.center.x * factor,
            -fullBounds.min.y * factor,
            -fullBounds.center.z * factor);

        // Badan tong (semua sub-mesh selain lid) memakai material accent kategori.
        string bodyMeshPath = $"{BinsFolder}/{prefabName}_Body.asset";
        SaveMeshAsset(bodyMesh, bodyMeshPath);

        GameObject model = new GameObject("Model");
        model.transform.SetParent(root.transform, false);
        model.AddComponent<MeshFilter>().sharedMesh = bodyMesh;
        MeshRenderer bodyRenderer = model.AddComponent<MeshRenderer>();
        bodyRenderer.sharedMaterials = new Material[bodyMesh.subMeshCount];

        for (int i = 0; i < bodyMesh.subMeshCount; i++)
        {
            bodyRenderer.sharedMaterials[i] = bodyMaterial;
        }

        model.transform.localScale = Vector3.one * factor;
        model.transform.localPosition = normalOffset;
        model.transform.localRotation = Quaternion.identity;

        // Lid sebagai child bernama "Lid" dengan poros di engsel belakang-atas agar bisa dibuka WasteGameManager.
        if (lidMesh != null)
        {
            string lidMeshPath = $"{BinsFolder}/{prefabName}_Lid.asset";
            SaveMeshAsset(lidMesh, lidMeshPath);

            Bounds lidBounds = lidMesh.bounds;
            Vector3 hingeRaw = new Vector3(lidBounds.center.x, lidBounds.max.y, lidBounds.min.z);

            GameObject lidPivot = new GameObject("Lid");
            lidPivot.transform.SetParent(root.transform, false);
            lidPivot.transform.localPosition = normalOffset + hingeRaw * factor;
            lidPivot.transform.localRotation = Quaternion.identity;

            GameObject lidVisual = new GameObject("LidMesh");
            lidVisual.transform.SetParent(lidPivot.transform, false);
            lidVisual.AddComponent<MeshFilter>().sharedMesh = lidMesh;
            MeshRenderer lidRenderer = lidVisual.AddComponent<MeshRenderer>();
            lidRenderer.sharedMaterial = bodyMaterial;
            lidVisual.transform.localScale = Vector3.one * factor;
            lidVisual.transform.localPosition = -hingeRaw * factor;
            lidVisual.transform.localRotation = Quaternion.identity;
        }

        float width = Mathf.Max(fullBounds.size.x * factor, 0.2f);
        float depth = Mathf.Max(fullBounds.size.z * factor, 0.2f);

        // Badan tong solid (bukan trigger) supaya sampah tidak menerobos mesh tong.
        BoxCollider bodyCollider = root.AddComponent<BoxCollider>();
        bodyCollider.center = new Vector3(0f, TargetBinHeight * 0.5f, 0f);
        bodyCollider.size = new Vector3(width, TargetBinHeight, depth);

        // Area deteksi masuk: trigger tipis HANYA di mulut atas tong.
        GameObject mouthTrigger = new GameObject("MouthTrigger");
        mouthTrigger.transform.SetParent(root.transform, false);
        BoxCollider mouthCollider = mouthTrigger.AddComponent<BoxCollider>();
        mouthCollider.isTrigger = true;
        mouthCollider.center = new Vector3(0f, Mathf.Max(TargetBinHeight - 0.04f, 0.02f), 0f);
        mouthCollider.size = new Vector3(width * 0.9f, 0.08f, depth * 0.9f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        if (prefab == null)
        {
            Debug.LogError($"[WasteWise] Gagal membuat prefab '{prefabPath}' dari '{SharedBinModelPath}'.");
        }
        else
        {
            Debug.Log($"[WasteWise] Prefab '{prefabName}' dibuat dari '{SharedBinModelPath}' di '{prefabPath}'.");
        }

        return prefab;
    }

    private static void EnsureModelReadable()
    {
        ModelImporter importer = AssetImporter.GetAtPath(SharedBinModelPath) as ModelImporter;

        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            Debug.Log($"[WasteWise] Mengaktifkan Read/Write pada '{SharedBinModelPath}' agar lid bisa dipisahkan.");
        }
    }

    private static Mesh GetSharedMesh(GameObject modelAsset)
    {
        foreach (MeshFilter filter in modelAsset.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh != null)
            {
                return filter.sharedMesh;
            }
        }

        return null;
    }

    // Sub-mesh penutup atas = sub-mesh dengan vertex tertinggi (y maksimum).
    private static int FindLidSubMesh(Mesh mesh)
    {
        if (mesh.subMeshCount <= 1)
        {
            return 0;
        }

        Vector3[] vertices = mesh.vertices;
        int lidIndex = 0;
        float lidMaxY = float.NegativeInfinity;

        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            int[] tris = mesh.GetTriangles(sub);
            float subMaxY = float.NegativeInfinity;

            foreach (int tri in tris)
            {
                if (vertices[tri].y > subMaxY)
                {
                    subMaxY = vertices[tri].y;
                }
            }

            if (subMaxY > lidMaxY)
            {
                lidMaxY = subMaxY;
                lidIndex = sub;
            }
        }

        return lidIndex;
    }

    // Ekstrak sub-mesh (lidOnly=true) atau gabungkan semua sub-mesh kecuali lid (lidOnly=false).
    private static Mesh ExtractBinMesh(Mesh source, int lidSubMesh, bool lidOnly)
    {
        Vector3[] srcVerts = source.vertices;
        Vector3[] srcNormals = source.normals;
        Vector2[] srcUvs = source.uv;

        Dictionary<int, int> remap = new Dictionary<int, int>();
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            bool include = lidOnly ? (sub == lidSubMesh) : (sub != lidSubMesh);

            if (!include)
            {
                continue;
            }

            int[] subTris = source.GetTriangles(sub);

            foreach (int tri in subTris)
            {
                if (!remap.TryGetValue(tri, out int newIndex))
                {
                    newIndex = verts.Count;
                    remap[tri] = newIndex;
                    verts.Add(srcVerts[tri]);
                    normals.Add(srcNormals.Length > tri ? srcNormals[tri] : Vector3.up);
                    uvs.Add(srcUvs.Length > tri ? srcUvs[tri] : Vector2.zero);
                }

                tris.Add(newIndex);
            }
        }

        if (verts.Count == 0 || tris.Count == 0)
        {
            return null;
        }

        Mesh mesh = new Mesh { name = lidOnly ? source.name + "_Lid" : source.name + "_Body" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 1;
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SaveMeshAsset(Mesh mesh, string path)
    {
        if (mesh == null)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        AssetDatabase.CreateAsset(mesh, path);
    }

    private static GameObject CreateBinPrefab(string prefabName, string prefabPath, Material bodyMaterial, Material lidMaterial)
    {
        GameObject root = new GameObject(prefabName);

        // Badan tong kotak solid (bukan trigger) supaya sampah tidak menerobos mesh tong.
        BoxCollider bodyCollider = root.AddComponent<BoxCollider>();
        bodyCollider.center = new Vector3(0f, 0.28f, 0f);
        bodyCollider.size = new Vector3(0.36f, 0.50f, 0.36f);

        // Area deteksi masuk: trigger tipis HANYA di mulut atas tong.
        GameObject mouthTrigger = new GameObject("MouthTrigger");
        mouthTrigger.transform.SetParent(root.transform, false);
        BoxCollider mouthCollider = mouthTrigger.AddComponent<BoxCollider>();
        mouthCollider.isTrigger = true;
        mouthCollider.center = new Vector3(0f, 0.53f, 0f);
        mouthCollider.size = new Vector3(0.38f, 0.06f, 0.38f);

        // Bentuk tong kotak publik: alas/penyangga, badan balok tumpul, bibir mulut, penutup berengsel.
        AddBoxPart(root.transform, "Base", new Vector3(0f, 0.03f, 0f), new Vector3(0.30f, 0.06f, 0.30f), lidMaterial);
        AddBoxPart(root.transform, "Body", new Vector3(0f, 0.27f, 0f), new Vector3(0.34f, 0.42f, 0.34f), bodyMaterial);
        AddBoxPart(root.transform, "Rim", new Vector3(0f, 0.505f, 0f), new Vector3(0.38f, 0.05f, 0.38f), lidMaterial);

        // Lid = poros engsel di tepi belakang; panel kotak sebagai anaknya agar bisa berputar terbuka.
        GameObject lidPivot = new GameObject("Lid");
        lidPivot.transform.SetParent(root.transform, false);
        lidPivot.transform.localPosition = new Vector3(0f, 0.53f, -0.17f);
        AddBoxPart(lidPivot.transform, "LidPanel", new Vector3(0f, 0f, 0.17f), new Vector3(0.36f, 0.04f, 0.36f), lidMaterial);

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

    // Cube primitive: 1x1x1 pada skala 1.
    private static void AddBoxPart(Transform parent, string name, Vector3 center, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = center;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = size;

        // Collider anak dibuang; root prefab punya BoxCollider solid sebagai badan tong.
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

    public static void AssignPrefabsToSpawner(
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
