using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;

public static class ARPipelineFixer
{
    private const string QualitySettingsAssetPath = "ProjectSettings/QualitySettings.asset";

    [MenuItem("WasteWise/Enforce URP AR Settings Across All Quality Levels")]
    public static void EnforceUrpArSettings()
    {
        string[] qualityNames = QualitySettings.names;

        if (qualityNames.Length == 0)
        {
            Debug.LogError("[WasteWise] Tidak ada Quality Level yang dapat diperiksa.");
            return;
        }

        UnityEngine.Object[] qualitySettingsAssets =
            AssetDatabase.LoadAllAssetsAtPath(QualitySettingsAssetPath);

        SerializedObject qualitySettings = null;
        SerializedProperty qualityLevels = null;

        if (qualitySettingsAssets != null && qualitySettingsAssets.Length > 0)
        {
            qualitySettings = new SerializedObject(qualitySettingsAssets[0]);
            qualityLevels = qualitySettings.FindProperty("m_QualitySettings");
        }
        else
        {
            Debug.LogWarning("[WasteWise] ProjectSettings/QualitySettings.asset tidak dapat dibuka. Penggantian URP Asset otomatis tidak tersedia.");
        }

        UniversalRenderPipelineAsset fallbackUrpAsset = null;
        bool qualitySettingsChanged = false;
        bool anyAssetChanged = false;
        int addedFeatureCount = 0;

        HashSet<ScriptableRendererData> processedRenderers = new HashSet<ScriptableRendererData>();
        List<string> fixedQualityLevels = new List<string>();

        for (int i = 0; i < qualityNames.Length; i++)
        {
            string qualityName = qualityNames[i];
            UniversalRenderPipelineAsset urpAsset =
                QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;

            if (!IsValidUrpAsset(urpAsset))
            {
                if (fallbackUrpAsset == null)
                {
                    fallbackUrpAsset = FindFallbackUrpAsset();
                }

                if (fallbackUrpAsset == null)
                {
                    Debug.LogError($"[WasteWise] Quality '{qualityName}' tidak menggunakan URP Asset yang valid dan tidak ada URP Asset cadangan yang bisa dipakai.");
                    continue;
                }

                if (!TryAssignQualityRenderPipeline(qualityLevels, i, fallbackUrpAsset))
                {
                    Debug.LogError($"[WasteWise] Quality '{qualityName}' tidak valid, tetapi customRenderPipeline tidak dapat diubah otomatis.");
                    continue;
                }

                urpAsset = fallbackUrpAsset;
                qualitySettingsChanged = true;
                anyAssetChanged = true;

                Debug.Log($"[WasteWise] Quality '{qualityName}' diarahkan ke URP Asset '{fallbackUrpAsset.name}'.");
            }

            int rendererCount = urpAsset.rendererDataList.Length;
            Debug.Log($"[WasteWise] Memeriksa Quality '{qualityName}' dengan URP Asset '{urpAsset.name}' ({rendererCount} Renderer Data).");

            for (int rendererIndex = 0; rendererIndex < rendererCount; rendererIndex++)
            {
                ScriptableRendererData rendererData = urpAsset.rendererDataList[rendererIndex];

                if (rendererData == null)
                {
                    Debug.LogWarning($"[WasteWise] Quality '{qualityName}' | URP Asset '{urpAsset.name}' | Renderer index {rendererIndex} kosong.");
                    continue;
                }

                if (rendererData.TryGetRendererFeature<ARBackgroundRendererFeature>(out _))
                {
                    Debug.Log($"[WasteWise] Quality '{qualityName}' | Renderer '{rendererData.name}': AR Background Renderer Feature sudah ada.");
                    continue;
                }

                if (processedRenderers.Contains(rendererData))
                {
                    continue;
                }

                if (!AddArBackgroundRendererFeature(rendererData))
                {
                    Debug.LogError($"[WasteWise] Quality '{qualityName}' | Renderer '{rendererData.name}': gagal menambahkan AR Background Renderer Feature.");
                    continue;
                }

                processedRenderers.Add(rendererData);
                addedFeatureCount++;
                anyAssetChanged = true;

                if (!fixedQualityLevels.Contains(qualityName))
                {
                    fixedQualityLevels.Add(qualityName);
                }

                Debug.Log($"[WasteWise] Quality '{qualityName}' | URP Asset '{urpAsset.name}' | Renderer '{rendererData.name}': AR Background Renderer Feature ditambahkan.");
            }
        }

        if (qualitySettingsChanged && qualitySettings != null)
        {
            qualitySettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(qualitySettings.targetObject);
            anyAssetChanged = true;
        }

        if (anyAssetChanged)
        {
            AssetDatabase.SaveAssets();
        }

        string fixedLevelsText = fixedQualityLevels.Count > 0
            ? string.Join(", ", fixedQualityLevels)
            : "tidak ada";

        Debug.Log($"[WasteWise] Selesai. Quality Level diperiksa: {qualityNames.Length}. Feature ditambahkan: {addedFeatureCount}. Quality Level diperbaiki: {fixedLevelsText}.");
    }

    private static bool IsValidUrpAsset(UniversalRenderPipelineAsset asset)
    {
        if (asset == null)
        {
            return false;
        }

        for (int i = 0; i < asset.rendererDataList.Length; i++)
        {
            if (asset.rendererDataList[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private static UniversalRenderPipelineAsset FindFallbackUrpAsset()
    {
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset defaultAsset &&
            IsValidUrpAsset(defaultAsset))
        {
            return defaultAsset;
        }

        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset currentAsset &&
            IsValidUrpAsset(currentAsset))
        {
            return currentAsset;
        }

        string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                continue;
            }

            UniversalRenderPipelineAsset asset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);

            if (IsValidUrpAsset(asset))
            {
                return asset;
            }
        }

        return null;
    }

    private static bool TryAssignQualityRenderPipeline(
        SerializedProperty qualityLevels,
        int qualityIndex,
        RenderPipelineAsset pipelineAsset)
    {
        if (qualityLevels == null || qualityIndex < 0 || qualityIndex >= qualityLevels.arraySize)
        {
            return false;
        }

        SerializedProperty qualityLevel = qualityLevels.GetArrayElementAtIndex(qualityIndex);
        SerializedProperty customRenderPipeline = qualityLevel.FindPropertyRelative("customRenderPipeline");

        if (customRenderPipeline == null)
        {
            return false;
        }

        customRenderPipeline.objectReferenceValue = pipelineAsset;
        return true;
    }

    private static bool AddArBackgroundRendererFeature(ScriptableRendererData rendererData)
    {
        SerializedObject serializedRendererData = new SerializedObject(rendererData);
        SerializedProperty rendererFeatures = serializedRendererData.FindProperty("m_RendererFeatures");
        SerializedProperty rendererFeatureMap = serializedRendererData.FindProperty("m_RendererFeatureMap");

        if (rendererFeatures == null)
        {
            return false;
        }

        ARBackgroundRendererFeature feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
        feature.name = "AR Background Renderer Feature";

        bool isPersistent = EditorUtility.IsPersistent(rendererData);

        if (isPersistent)
        {
            AssetDatabase.AddObjectToAsset(feature, rendererData);
        }

        int featureIndex = rendererFeatures.arraySize;
        rendererFeatures.arraySize++;
        rendererFeatures.GetArrayElementAtIndex(featureIndex).objectReferenceValue = feature;

        if (rendererFeatureMap != null)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localFileId);

            int mapIndex = rendererFeatureMap.arraySize;
            rendererFeatureMap.arraySize++;
            rendererFeatureMap.GetArrayElementAtIndex(mapIndex).longValue = localFileId;
        }

        serializedRendererData.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
        rendererData.SetDirty();

        if (isPersistent)
        {
            AssetDatabase.SaveAssetIfDirty(rendererData);
        }

        return true;
    }
}
