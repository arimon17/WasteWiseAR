using UnityEngine;

// Model 3D prosedural dari mesh primitive Unity (tanpa aset eksternal) + nameplate melayang.
// Catatan: bentuk disusun dari mesh primitive bawaan (bukan Mesh buatan tangan) agar
// winding/normal dijamin benar di perangkat tanpa bisa dirender-uji di sini.
public static class WasteModelBuilder
{
    private const float LabelWorldOffset = 0.28f; // di atas mesh sampah agar nameplate tegak & jelas
    private const float NameplateFontSize = 1.5f;

    // Dipanggil WasteGameManager: pasang visual + nameplate di bawah root item (root diskala 0.15).
    public static void Build(Transform item, string itemName, TrashCategory category)
    {
        GameObject visual = NewChild("Visual", item);
        BuildShape(visual.transform, itemName, category);
        BuildLabel(item, itemName);
    }

    private static void BuildShape(Transform parent, string itemName, TrashCategory category)
    {
        string key = (itemName ?? string.Empty).ToLowerInvariant();

        if (key.Contains("pisang")) Banana(parent);
        else if (key.Contains("botol")) Bottle(parent);
        else if (key.Contains("kaleng")) Can(parent);
        else if (key.Contains("kardus") || key.Contains("kertas")) Cardboard(parent);
        else if (key.Contains("mi")) Wrapper(parent);
        else if (key.Contains("baterai")) Battery(parent);
        else if (key.Contains("lampu")) Bulb(parent);
        else if (key.Contains("apel")) Apple(parent);
        else if (key.Contains("sayur") || key.Contains("nasi") || key.Contains("buah")) Vegetable(parent);
        else Fallback(parent, category);
    }

    private static void Fallback(Transform parent, TrashCategory category)
    {
        switch (category)
        {
            case TrashCategory.Anorganik:
                Can(parent);
                break;
            case TrashCategory.B3:
                Battery(parent);
                break;
            default:
                Apple(parent);
                break;
        }
    }

    private static void Banana(Transform parent)
    {
        GameObject banana = Part(PrimitiveType.Capsule, parent, new Vector3(0f, 0.05f, 0f), new Vector3(0.45f, 0.4f, 0.45f), "Pisang");
        banana.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
    }

    private static void Apple(Transform parent)
    {
        Part(PrimitiveType.Sphere, parent, Vector3.zero, Vector3.one * 0.7f, "Apel");
        Part(PrimitiveType.Cube, parent, new Vector3(0f, 0.36f, 0f), new Vector3(0.06f, 0.14f, 0.06f), "Gelap");
    }

    private static void Vegetable(Transform parent)
    {
        Part(PrimitiveType.Sphere, parent, Vector3.zero, new Vector3(0.7f, 0.6f, 0.7f), "Sayur");
        Part(PrimitiveType.Cube, parent, new Vector3(0f, 0.32f, 0f), new Vector3(0.16f, 0.08f, 0.16f), "Gelap");
    }

    private static void Bottle(Transform parent)
    {
        Part(PrimitiveType.Cylinder, parent, new Vector3(0f, -0.1f, 0f), new Vector3(0.4f, 0.35f, 0.4f), "Botol");
        Part(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.25f, 0f), new Vector3(0.15f, 0.15f, 0.15f), "Botol");
        Part(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.42f, 0f), new Vector3(0.18f, 0.05f, 0.18f), "Detail");
    }

    private static void Can(Transform parent)
    {
        Part(PrimitiveType.Cylinder, parent, Vector3.zero, new Vector3(0.4f, 0.35f, 0.4f), "Kaleng");
        Part(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.36f, 0f), new Vector3(0.42f, 0.02f, 0.42f), "Gelap");
    }

    private static void Cardboard(Transform parent)
    {
        Part(PrimitiveType.Cube, parent, Vector3.zero, new Vector3(0.8f, 0.5f, 0.25f), "Kardus");
        Part(PrimitiveType.Cube, parent, new Vector3(0f, 0.26f, 0f), new Vector3(0.82f, 0.05f, 0.27f), "Gelap");
    }

    private static void Wrapper(Transform parent)
    {
        Part(PrimitiveType.Cube, parent, Vector3.zero, new Vector3(0.35f, 0.9f, 0.12f), "Bungkus");
    }

    private static void Battery(Transform parent)
    {
        Part(PrimitiveType.Cylinder, parent, Vector3.zero, new Vector3(0.3f, 0.35f, 0.3f), "Baterai");
        Part(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.22f, 0f), new Vector3(0.31f, 0.08f, 0.31f), "Detail");
        Part(PrimitiveType.Cylinder, parent, new Vector3(0f, 0.38f, 0f), new Vector3(0.1f, 0.03f, 0.1f), "Gelap");
    }

    private static void Bulb(Transform parent)
    {
        Part(PrimitiveType.Sphere, parent, new Vector3(0f, 0.12f, 0f), Vector3.one * 0.6f, "Lampu");
        Part(PrimitiveType.Cylinder, parent, new Vector3(0f, -0.28f, 0f), new Vector3(0.22f, 0.12f, 0.22f), "Kaleng");
        Part(PrimitiveType.Cylinder, parent, new Vector3(0f, -0.42f, 0f), new Vector3(0.12f, 0.04f, 0.12f), "Gelap");
    }

    private static void BuildLabel(Transform item, string itemName)
    {
        FloatingLabel.Create(item, itemName, new Vector3(0f, LabelWorldOffset, 0f), NameplateFontSize, Color.white);
    }

    private static GameObject Part(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, string materialKey)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = materialKey;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = WasteMaterialLibrary.Get(materialKey);
        }

        return part;
    }

    private static GameObject NewChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }
}
