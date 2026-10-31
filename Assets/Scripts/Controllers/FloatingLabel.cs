using TMPro;
using UnityEngine;

// Label 3D TextMeshPro yang melayang & selalu menghadap kamera (dipakai sampah & tempat sampah).
public static class FloatingLabel
{
    public static GameObject Create(Transform parent, string text, float worldOffsetY, float worldHeight, Color color)
    {
        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;

        if (fontAsset == null || string.IsNullOrEmpty(text))
        {
            return null;
        }

        GameObject labelObject = new GameObject($"Label_{text}");
        labelObject.transform.SetParent(parent, false);

        // Kompensasi skala induk agar offset & ukuran tetap dalam satuan dunia nyata.
        float parentScale = Mathf.Max(parent.lossyScale.y, 0.0001f);
        labelObject.transform.localPosition = new Vector3(0f, worldOffsetY / parentScale, 0f);

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.font = fontAsset;
        label.text = text;
        label.fontSize = 36f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.color = color;
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color(0.08f, 0.08f, 0.1f);
        label.ForceMeshUpdate();

        float boundsHeight = label.textBounds.size.y;

        if (boundsHeight > 0.0001f)
        {
            float scale = worldHeight / (boundsHeight * parentScale);
            Debug.Assert(!float.IsNaN(scale) && !float.IsInfinity(scale), "Skala label tidak valid.");
            labelObject.transform.localScale = Vector3.one * scale;
        }

        labelObject.AddComponent<Billboard>();
        return labelObject;
    }
}
