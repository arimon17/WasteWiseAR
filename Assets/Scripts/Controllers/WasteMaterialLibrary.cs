using System.Collections.Generic;
using UnityEngine;

// Material URP Lit matte prosedural (tanpa aset), di-cache per tipe objek.
public static class WasteMaterialLibrary
{
    private const string LitShaderName = "Universal Render Pipeline/Lit";
    private const float Smoothness = 0.08f;

    public struct Style
    {
        public string key;
        public Color color;
    }

    public static readonly Style[] Styles =
    {
        new Style { key = "Pisang", color = new Color(0.96f, 0.85f, 0.25f) },
        new Style { key = "Apel", color = new Color(0.85f, 0.26f, 0.2f) },
        new Style { key = "Sayur", color = new Color(0.45f, 0.75f, 0.3f) },
        new Style { key = "Botol", color = new Color(0.55f, 0.85f, 0.95f) },
        new Style { key = "Kaleng", color = new Color(0.75f, 0.78f, 0.82f) },
        new Style { key = "Kardus", color = new Color(0.72f, 0.55f, 0.33f) },
        new Style { key = "Bungkus", color = new Color(0.95f, 0.55f, 0.2f) },
        new Style { key = "Baterai", color = new Color(0.25f, 0.27f, 0.3f) },
        new Style { key = "Lampu", color = new Color(0.88f, 0.9f, 0.92f) },
        new Style { key = "Detail", color = new Color(0.2f, 0.5f, 0.9f) },
        new Style { key = "Gelap", color = new Color(0.15f, 0.17f, 0.2f) }
    };

    private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

    public static Material Get(string key)
    {
        if (Cache.TryGetValue(key, out Material cached) && cached != null)
        {
            return cached;
        }

        Shader shader = Shader.Find(LitShaderName);

        if (shader == null)
        {
            Debug.LogWarning($"[WasteWise] Shader '{LitShaderName}' tidak ditemukan; material '{key}' dilewati.");
            return null;
        }

        Material material = new Material(shader)
        {
            name = $"Mat_{key}"
        };
        material.SetColor("_BaseColor", ColorFor(key));
        material.SetFloat("_Smoothness", Smoothness); // matte finish
        material.SetFloat("_Metallic", 0f);

        Cache[key] = material;
        return material;
    }

    public static Color ColorFor(string key)
    {
        foreach (Style style in Styles)
        {
            if (style.key == key)
            {
                return style.color;
            }
        }

        return new Color(0.7f, 0.7f, 0.7f);
    }
}
