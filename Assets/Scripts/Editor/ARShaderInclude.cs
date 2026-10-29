using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class ARShaderInclude : IPreprocessBuildWithReport
{
    private const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
    private const string AlwaysIncludedShadersProperty = "m_AlwaysIncludedShaders";

    private static readonly string[] ArBackgroundShaderNames =
    {
        "Universal Render Pipeline/AR/Background",
        "ARCore/Background",
        "Unlit/ARCoreBackground",
        "Unlit/ARCoreBackground/AfterOpaques"
    };

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Object[] graphicsSettingsAssets = AssetDatabase.LoadAllAssetsAtPath(GraphicsSettingsAssetPath);

        if (graphicsSettingsAssets == null || graphicsSettingsAssets.Length == 0)
        {
            Debug.LogWarning(
                "[WasteWise] GraphicsSettings.asset tidak ditemukan. Shader AR tidak dapat ditambahkan ke Always Included Shaders.");
            return;
        }

        SerializedObject graphicsSettings = new SerializedObject(graphicsSettingsAssets[0]);
        SerializedProperty alwaysIncludedShaders = graphicsSettings.FindProperty(AlwaysIncludedShadersProperty);

        if (alwaysIncludedShaders == null)
        {
            Debug.LogWarning(
                "[WasteWise] Properti Always Included Shaders tidak ditemukan pada GraphicsSettings.");
            return;
        }

        bool changed = false;

        foreach (string shaderName in ArBackgroundShaderNames)
        {
            Shader shader = Shader.Find(shaderName);

            if (shader == null || IsShaderAlreadyIncluded(alwaysIncludedShaders, shader))
            {
                continue;
            }

            int index = alwaysIncludedShaders.arraySize;
            alwaysIncludedShaders.InsertArrayElementAtIndex(index);
            alwaysIncludedShaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            changed = true;

            Debug.Log($"[WasteWise] Shader '{shaderName}' ditambahkan ke Always Included Shaders.");
        }

        if (!changed)
        {
            return;
        }

        graphicsSettings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    private static bool IsShaderAlreadyIncluded(SerializedProperty alwaysIncludedShaders, Shader shader)
    {
        for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
        {
            if (alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue == shader)
            {
                return true;
            }
        }

        return false;
    }
}
