using System.IO;
using UnityEditor;
using UnityEngine;

public static class UISpriteGenerator
{
    private const string Folder = "Assets/Sprites";
    // PPU = 100/3: radius design (baseline 360dp) jadi 3x lipat di canvas 1080x1920.
    private const float PixelsPerUnit = 100f / 3f;

    public static Sprite GetRounded(int radius)
    {
        EnsureFolder();
        string path = $"{Folder}/ui_rounded_{radius}.png";

        if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
        {
            Create(path, radius);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets", "Sprites");
        }
    }

    private static void Create(string path, int radius)
    {
        int size = radius * 2 + 8;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float cx = Mathf.Clamp(px, radius, size - radius);
                float cy = Mathf.Clamp(py, radius, size - radius);
                float distance = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(radius, radius, radius, radius);
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }
}
