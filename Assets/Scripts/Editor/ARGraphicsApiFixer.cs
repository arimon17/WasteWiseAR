using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ARGraphicsApiFixer
{
    private const BuildTarget AndroidTarget = BuildTarget.Android;

    [MenuItem("WasteWise/Fix Graphics API to OpenGLES3")]
    public static void FixAndroidGraphicsApi()
    {
        GraphicsDeviceType[] previousApis = PlayerSettings.GetGraphicsAPIs(AndroidTarget);
        bool wasAutomatic = PlayerSettings.GetUseDefaultGraphicsAPIs(AndroidTarget);

        PlayerSettings.SetUseDefaultGraphicsAPIs(AndroidTarget, false);
        PlayerSettings.SetGraphicsAPIs(AndroidTarget, new[] { GraphicsDeviceType.OpenGLES3 });

        AssetDatabase.SaveAssets();

        GraphicsDeviceType[] currentApis = PlayerSettings.GetGraphicsAPIs(AndroidTarget);

        Debug.Log($"[WasteWise] Graphics API Android diubah dari [{FormatApis(previousApis)}] (automatic: {wasAutomatic}) menjadi [{FormatApis(currentApis)}]. Vulkan dihapus dan OpenGLES3 menjadi API utama.");
    }

    private static string FormatApis(GraphicsDeviceType[] apis)
    {
        return apis == null || apis.Length == 0 ? "kosong" : string.Join(", ", apis);
    }
}
