using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public static class ARSceneFixer
{
    private const string UndoName = "Fix AR Camera Settings";

    [MenuItem("WasteWise/Fix AR Camera Settings")]
    public static void FixARCameraSettings()
    {
        GameObject mainCameraObject = FindMainCameraObject();

        if (mainCameraObject == null)
        {
            EditorUtility.DisplayDialog(
                "WasteWise AR",
                "Main Camera tidak ditemukan. Pastikan scene aktif memiliki XR Origin (Mobile AR) dengan kamera AR.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);

        Camera camera = GetOrAddComponent<Camera>(mainCameraObject);
        GetOrAddComponent<ARCameraManager>(mainCameraObject);
        ARCameraBackground cameraBackground = GetOrAddComponent<ARCameraBackground>(mainCameraObject);
        EnsureTrackedPoseDriver(mainCameraObject);

        UniversalAdditionalCameraData cameraData =
            GetOrAddComponent<UniversalAdditionalCameraData>(mainCameraObject);

        Undo.RecordObject(camera, UndoName);
        camera.clearFlags = CameraClearFlags.Nothing;
        camera.backgroundColor = Color.black;

        Undo.RecordObject(cameraData, UndoName);
        cameraData.renderType = CameraRenderType.Base;
        cameraData.renderPostProcessing = false;
        cameraData.allowXRRendering = true;

        Undo.RecordObject(cameraBackground, UndoName);
        cameraBackground.useCustomMaterial = false;
        cameraBackground.customMaterial = null;

        MarkCameraDirty(mainCameraObject, camera, cameraData, cameraBackground);

        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = mainCameraObject;
        EditorGUIUtility.PingObject(mainCameraObject);

        Debug.Log(
            $"[WasteWise] AR camera URP background berhasil dikonfigurasi pada '{mainCameraObject.name}' dan Custom Material dinonaktifkan. Scene aktif telah ditandai dirty.",
            mainCameraObject);
    }

    private static GameObject FindMainCameraObject()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject namedCamera = null;
        Camera fallbackCamera = null;

        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject candidate = child.gameObject;

                if (candidate.CompareTag("MainCamera"))
                {
                    return candidate;
                }

                if (namedCamera == null &&
                    string.Equals(candidate.name, "Main Camera", StringComparison.OrdinalIgnoreCase))
                {
                    namedCamera = candidate;
                }

                if (fallbackCamera == null &&
                    candidate.TryGetComponent(out Camera cameraComponent))
                {
                    fallbackCamera = cameraComponent;
                }
            }
        }

        if (namedCamera != null)
        {
            return namedCamera;
        }

        return fallbackCamera != null ? fallbackCamera.gameObject : null;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        if (target.TryGetComponent(out T component))
        {
            return component;
        }

        return Undo.AddComponent<T>(target);
    }

    private static void EnsureTrackedPoseDriver(GameObject target)
    {
        foreach (Component component in target.GetComponents<Component>())
        {
            if (component != null &&
                string.Equals(component.GetType().Name, "TrackedPoseDriver", StringComparison.Ordinal))
            {
                return;
            }
        }

#if ENABLE_INPUT_SYSTEM
        Undo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>(target);
#else
        Type legacyTrackedPoseDriverType =
            Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver, Unity.XR.LegacyInputHelpers");

        if (legacyTrackedPoseDriverType != null)
        {
            Undo.AddComponent(target, legacyTrackedPoseDriverType);
        }
        else
        {
            Debug.LogWarning(
                "[WasteWise] Tracked Pose Driver tidak ditemukan. Tambahkan komponen Tracked Pose Driver secara manual pada Main Camera.",
                target);
        }
#endif
    }

    private static void MarkCameraDirty(
        GameObject mainCameraObject,
        Camera camera,
        UniversalAdditionalCameraData cameraData,
        ARCameraBackground cameraBackground)
    {
        EditorUtility.SetDirty(mainCameraObject);
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(cameraData);
        EditorUtility.SetDirty(cameraBackground);

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }
}
