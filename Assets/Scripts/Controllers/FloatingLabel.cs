using TMPro;
using UnityEngine;

// Label 3D TextMeshPro: nameplate melayang (billboard) dan teks statis yang menempel pada geometri.
public static class FloatingLabel
{
    // Skala nameplate melayang agar teks sampah tetap kecil (lebar ~<= 10cm).
    private const float WorldUnitsPerFontSize = 0.03f;
    private static readonly Color OutlineColor = new Color(0.05f, 0.05f, 0.07f);

    // Nameplate melayang di atas sampah; selalu menghadap kamera.
    public static GameObject Create(Transform parent, string text, Vector3 localOffset, float fontSize, Color color)
    {
        if (!CanRender(text))
        {
            return null;
        }

        float parentScale = ParentScale(parent);
        float worldHeight = fontSize * WorldUnitsPerFontSize;

        GameObject labelObject = NewChild(parent, $"Label_{text}", localOffset / parentScale, Quaternion.identity);
        labelObject.transform.localScale = Vector3.one * (worldHeight / (fontSize * parentScale));

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        ApplyTextStyle(label, text, fontSize, color, FontStyles.Bold);

        labelObject.AddComponent<Billboard>();
        return labelObject;
    }

    // Teks 3D statis menempel pada geometri (mis. di atas tutup tong). Tanpa Billboard agar tidak berputar.
    public static GameObject CreateStaticLabel(
        Transform parent,
        string text,
        Vector3 localOffset,
        Vector3 localEuler,
        float fontSize,
        float scaleFactor,
        Color color)
    {
        if (!CanRender(text))
        {
            return null;
        }

        float parentScale = ParentScale(parent);

        GameObject labelObject = NewChild(parent, $"Label_{text}", localOffset / parentScale, Quaternion.Euler(localEuler));
        labelObject.transform.localScale = Vector3.one * (scaleFactor / parentScale);

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        ApplyTextStyle(label, text, fontSize, color, FontStyles.Bold);

        return labelObject;
    }

    private static void ApplyTextStyle(TextMeshPro label, string content, float fontSize, Color color, FontStyles style)
    {
        label.font = TMP_Settings.defaultFontAsset;
        label.text = content;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.extraPadding = false;
        label.color = color;
        label.outlineWidth = 0.12f;
        label.outlineColor = OutlineColor;
    }

    private static GameObject NewChild(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        return go;
    }

    private static float ParentScale(Transform parent)
    {
        return Mathf.Max(Mathf.Abs(parent.lossyScale.y), 0.0001f);
    }

    private static bool CanRender(string text)
    {
        return !string.IsNullOrEmpty(text) && TMP_Settings.defaultFontAsset != null;
    }
}
