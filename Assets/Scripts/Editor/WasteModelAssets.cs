using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Menyiapkan folder model prosedural dan memeriksa keberadaan model .fbx kustom.
public static class WasteModelAssets
{
    private const string ModelsFolder = "Assets/Models";
    private const string GeneratedFolder = "Assets/Models/Generated";
    private const string MenuPath = "WasteWise/Prepare Model Folders and Check Custom Models";

    private static readonly string[] ModelExtensions = { ".fbx", ".obj", ".blend", ".gltf", ".glb", ".dae" };

    [MenuItem(MenuPath)]
    public static void Prepare()
    {
        EnsureFolders();
        int exported = ExportMaterials();
        int custom = CountCustomModels();

        if (custom > 0)
        {
            Debug.LogWarning($"[WasteWise] Folder model siap; {exported} material prosedural diekspor ke '{GeneratedFolder}'. " +
                             $"Ditemukan {custom} file model kustom di '{ModelsFolder}' — runtime saat ini tetap memakai mesh prosedural, " +
                             "hubungkan model kustom secara manual bila ingin dipakai.");
        }
        else
        {
            Debug.Log($"[WasteWise] Folder model siap; {exported} material prosedural diekspor ke '{GeneratedFolder}'. " +
                      $"Belum ada model kustom di '{ModelsFolder}'.");
        }
    }

    // Cari model tong kustom (mis. 'Bin_Organik') di folder Assets/Models; null bila tidak ada.
    public static GameObject FindCustomBinModel(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            return null;
        }

        foreach (string guid in AssetDatabase.FindAssets($"{modelName} t:GameObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.StartsWith(ModelsFolder + "/", StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(Path.GetFileNameWithoutExtension(path), modelName, StringComparison.Ordinal))
            {
                continue;
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (model != null)
            {
                return model;
            }
        }

        return null;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(ModelsFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Models");
        }

        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder(ModelsFolder, "Generated");
        }
    }

    private static int ExportMaterials()
    {
        int count = 0;

        foreach (WasteMaterialLibrary.Style style in WasteMaterialLibrary.Styles)
        {
            string path = $"{GeneratedFolder}/Mat_{style.key}.mat";

            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                continue;
            }

            Material material = WasteMaterialLibrary.Get(style.key);

            if (material == null)
            {
                continue;
            }

            AssetDatabase.CreateAsset(material, path);
            count++;
        }

        AssetDatabase.SaveAssets();
        return count;
    }

    private static int CountCustomModels()
    {
        int count = 0;

        foreach (string file in Directory.GetFiles(ModelsFolder, "*", SearchOption.AllDirectories))
        {
            string normalized = file.Replace('\\', '/');

            if (normalized.StartsWith(GeneratedFolder))
            {
                continue;
            }

            if (Array.IndexOf(ModelExtensions, Path.GetExtension(normalized).ToLowerInvariant()) >= 0)
            {
                count++;
            }
        }

        return count;
    }
}
