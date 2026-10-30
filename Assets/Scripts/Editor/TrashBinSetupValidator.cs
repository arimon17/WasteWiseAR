using System;
using System.Collections.Generic;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public static class TrashBinSetupValidator
{
    private const string MenuPath = "WasteWise/Validate and Fix TrashBin AR Scene";
    private const string UndoName = "Validate and Fix TrashBin AR Scene";
    private const string OrganicPrefabPath = "Assets/Prefabs/Bins/OrganicBin.prefab";
    private const string InorganicPrefabPath = "Assets/Prefabs/Bins/InorganicBin.prefab";
    private const string B3PrefabPath = "Assets/Prefabs/Bins/B3Bin.prefab";

    [MenuItem(MenuPath)]
    public static void ValidateAndFixTrashBinScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WasteWise] Hentikan Play Mode sebelum menjalankan Validate and Fix TrashBin AR Scene.");
            return;
        }

        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("[WasteWise] Tutup Prefab Mode sebelum menjalankan Validate and Fix TrashBin AR Scene.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid())
        {
            ShowFatalDialog("Scene aktif tidak valid. Buka scene AR terlebih dahulu.");
            return;
        }

        XROrigin xrOrigin = FindXROrigin(activeScene);

        if (xrOrigin == null)
        {
            ShowFatalDialog(
                "GameObject XR Origin dengan komponen XROrigin tidak ditemukan di scene aktif. Tambahkan XR Origin (Mobile AR) terlebih dahulu.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);

        List<string> changes = new List<string>();
        List<string> warnings = new List<string>();
        List<string> errors = new List<string>();

        GameObject originObject = xrOrigin.gameObject;

        EnsureActiveHierarchy(originObject.transform, null, changes);
        EnsureEnabled(xrOrigin, "XROrigin", changes);

        ARPlaneManager planeManager =
            GetOrAddComponent<ARPlaneManager>(originObject, "ARPlaneManager", changes, errors);
        EnsureEnabled(planeManager, "ARPlaneManager", changes);

        ARRaycastManager raycastManager =
            GetOrAddComponent<ARRaycastManager>(originObject, "ARRaycastManager", changes, errors);
        EnsureEnabled(raycastManager, "ARRaycastManager", changes);

        TrashBinSpawner spawner =
            GetOrAddComponent<TrashBinSpawner>(originObject, "TrashBinSpawner", changes, errors);
        EnsureEnabled(spawner, "TrashBinSpawner", changes);

        DisablePlacementControllers(activeScene, changes);

        GameObject cameraObject = FindMainCameraObject(xrOrigin);

        if (cameraObject == null)
        {
            errors.Add(
                "Main Camera tidak ditemukan sebagai anak XR Origin. Tambahkan Main Camera AR di bawah Camera Offset.");
        }
        else
        {
            EnsureActiveHierarchy(cameraObject.transform, originObject.transform, changes);

            Camera mainCamera = GetOrAddComponent<Camera>(cameraObject, "Camera", changes, errors);
            EnsureEnabled(mainCamera, "Camera", changes);

            if (mainCamera != null && xrOrigin.Camera != mainCamera)
            {
                Undo.RecordObject(xrOrigin, UndoName);
                xrOrigin.Camera = mainCamera;
                EditorUtility.SetDirty(xrOrigin);
                changes.Add($"Menghubungkan field Camera pada XROrigin ke '{cameraObject.name}'.");
            }

            ARCameraManager cameraManager =
                GetOrAddComponent<ARCameraManager>(cameraObject, "ARCameraManager", changes, errors);
            EnsureEnabled(cameraManager, "ARCameraManager", changes);

            ARCameraBackground cameraBackground =
                GetOrAddComponent<ARCameraBackground>(cameraObject, "ARCameraBackground", changes, errors);
            EnsureEnabled(cameraBackground, "ARCameraBackground", changes);

            EnsureTrackedPoseDriver(cameraObject, changes, warnings, errors);
        }

        if (spawner != null)
        {
            AssignSpawnerReferences(spawner, raycastManager, changes, errors);
            EditorUtility.SetDirty(spawner);
        }

        EditorUtility.SetDirty(originObject);
        EditorSceneManager.MarkSceneDirty(activeScene);
        AssetDatabase.SaveAssets();

        Undo.CollapseUndoOperations(undoGroup);

        if (!Application.isBatchMode)
        {
            Selection.activeGameObject = originObject;
            EditorGUIUtility.PingObject(originObject);
        }

        LogAndShowSummary(xrOrigin, changes, warnings, errors);
    }

    private static XROrigin FindXROrigin(Scene scene)
    {
        XROrigin bestOrigin = null;
        int bestScore = int.MinValue;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (XROrigin origin in root.GetComponentsInChildren<XROrigin>(true))
            {
                int score = ScoreOrigin(origin);

                if (bestOrigin == null || score > bestScore)
                {
                    bestOrigin = origin;
                    bestScore = score;
                }
            }
        }

        return bestOrigin;
    }

    private static int ScoreOrigin(XROrigin origin)
    {
        int score = 0;

        if (origin.gameObject.name.IndexOf("XR Origin", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            score += 100;
        }

        if (origin.GetComponent<TrashBinSpawner>() != null)
        {
            score += 20;
        }

        if (origin.GetComponent<ARRaycastManager>() != null)
        {
            score += 10;
        }

        if (origin.Camera != null)
        {
            score += 5;
        }

        if (origin.transform.parent == null)
        {
            score += 2;
        }

        if (origin.gameObject.activeInHierarchy)
        {
            score += 1;
        }

        return score;
    }

    private static GameObject FindMainCameraObject(XROrigin origin)
    {
        if (origin.Camera != null && origin.Camera.transform.IsChildOf(origin.transform))
        {
            return origin.Camera.gameObject;
        }

        GameObject namedCamera = null;
        GameObject cameraManagerObject = null;

        foreach (Transform child in origin.GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("MainCamera"))
            {
                return child.gameObject;
            }

            if (namedCamera == null &&
                string.Equals(child.gameObject.name, "Main Camera", StringComparison.OrdinalIgnoreCase))
            {
                namedCamera = child.gameObject;
            }

            if (cameraManagerObject == null && child.GetComponent<ARCameraManager>() != null)
            {
                cameraManagerObject = child.gameObject;
            }
        }

        if (namedCamera != null)
        {
            return namedCamera;
        }

        if (cameraManagerObject != null)
        {
            return cameraManagerObject;
        }

        Camera fallbackCamera = origin.GetComponentInChildren<Camera>(true);
        return fallbackCamera != null ? fallbackCamera.gameObject : null;
    }

    private static void DisablePlacementControllers(Scene scene, List<string> changes)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (ARPlacementController controller in root.GetComponentsInChildren<ARPlacementController>(true))
            {
                if (!controller.enabled)
                {
                    continue;
                }

                Undo.RecordObject(controller, UndoName);
                controller.enabled = false;
                EditorUtility.SetDirty(controller);
                changes.Add(
                    $"Menonaktifkan ARPlacementController pada '{GetHierarchyPath(controller.transform)}'.");
            }
        }
    }

    private static void EnsureTrackedPoseDriver(
        GameObject cameraObject,
        List<string> changes,
        List<string> warnings,
        List<string> errors)
    {
        int foundCount = 0;

        foreach (Component component in cameraObject.GetComponents<Component>())
        {
            if (component == null ||
                !string.Equals(component.GetType().Name, "TrackedPoseDriver", StringComparison.Ordinal))
            {
                continue;
            }

            foundCount++;

            if (component is Behaviour behaviour)
            {
                EnsureEnabled(behaviour, "TrackedPoseDriver", changes);
            }
        }

        if (foundCount > 0)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.XR.TrackedPoseDriver trackedPoseDriver =
            Undo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>(cameraObject);

        if (trackedPoseDriver == null)
        {
            errors.Add("Gagal menambahkan TrackedPoseDriver pada Main Camera.");
            return;
        }

        EnsureEnabled(trackedPoseDriver, "TrackedPoseDriver", changes);
        warnings.Add(
            "TrackedPoseDriver baru ditambahkan. Periksa binding Position, Rotation, dan Tracking State pada Main Camera.");
#else
        Type legacyTrackedPoseDriverType =
            Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver, Unity.XR.LegacyInputHelpers");

        if (legacyTrackedPoseDriverType == null)
        {
            errors.Add(
                "TrackedPoseDriver tidak ditemukan. Tambahkan komponen Tracked Pose Driver secara manual pada Main Camera.");
            return;
        }

        Component legacyTrackedPoseDriver = Undo.AddComponent(cameraObject, legacyTrackedPoseDriverType);

        if (!(legacyTrackedPoseDriver is Behaviour trackedPoseDriverBehaviour))
        {
            errors.Add("Gagal menambahkan TrackedPoseDriver pada Main Camera.");
            return;
        }

        EnsureEnabled(trackedPoseDriverBehaviour, "TrackedPoseDriver", changes);
        warnings.Add(
            "TrackedPoseDriver baru ditambahkan. Periksa binding Position, Rotation, dan Tracking State pada Main Camera.");
#endif
    }

    private static void AssignSpawnerReferences(
        TrashBinSpawner spawner,
        ARRaycastManager raycastManager,
        List<string> changes,
        List<string> errors)
    {
        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.Update();

        bool changed = false;

        changed |= AssignPrefab(serializedSpawner, "organicBinPrefab", OrganicPrefabPath, changes, errors);
        changed |= AssignPrefab(serializedSpawner, "inorganicBinPrefab", InorganicPrefabPath, changes, errors);
        changed |= AssignPrefab(serializedSpawner, "b3BinPrefab", B3PrefabPath, changes, errors);

        if (raycastManager == null)
        {
            errors.Add("ARRaycastManager pada XR Origin tidak tersedia sehingga field raycastManager tidak dapat diisi.");
        }
        else
        {
            changed |= AssignObjectReference(
                serializedSpawner,
                "raycastManager",
                raycastManager,
                "ARRaycastManager pada XR Origin",
                changes,
                errors);
        }

        if (!changed)
        {
            return;
        }

        Undo.RecordObject(spawner, UndoName);
        serializedSpawner.ApplyModifiedProperties();
        EditorUtility.SetDirty(spawner);
    }

    private static bool AssignPrefab(
        SerializedObject serializedObject,
        string propertyName,
        string assetPath,
        List<string> changes,
        List<string> errors)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (prefab == null)
        {
            errors.Add(
                $"Asset prefab '{assetPath}' tidak ditemukan. Jalankan menu WasteWise/Generate Trash Bin Prefabs terlebih dahulu.");
            return false;
        }

        return AssignObjectReference(
            serializedObject,
            propertyName,
            prefab,
            $"asset '{assetPath}'",
            changes,
            errors);
    }

    private static bool AssignObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        UnityEngine.Object value,
        string valueDescription,
        List<string> changes,
        List<string> errors)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            errors.Add($"Field '{propertyName}' tidak ditemukan pada TrashBinSpawner.");
            return false;
        }

        if (property.objectReferenceValue == value)
        {
            return false;
        }

        property.objectReferenceValue = value;
        changes.Add($"Menghubungkan field '{propertyName}' ke {valueDescription}.");
        return true;
    }

    private static T GetOrAddComponent<T>(
        GameObject target,
        string componentName,
        List<string> changes,
        List<string> errors) where T : Component
    {
        if (target.TryGetComponent(out T component))
        {
            return component;
        }

        component = Undo.AddComponent<T>(target);

        if (component == null)
        {
            errors.Add($"Gagal menambahkan komponen {componentName} pada '{GetHierarchyPath(target.transform)}'.");
            return null;
        }

        changes.Add($"Menambahkan komponen {componentName} pada '{GetHierarchyPath(target.transform)}'.");
        return component;
    }

    private static void EnsureEnabled(Behaviour behaviour, string label, List<string> changes)
    {
        if (behaviour == null || behaviour.enabled)
        {
            return;
        }

        Undo.RecordObject(behaviour, UndoName);
        behaviour.enabled = true;
        EditorUtility.SetDirty(behaviour);
        changes.Add($"Mengaktifkan komponen {label} pada '{GetHierarchyPath(behaviour.transform)}'.");
    }

    private static void EnsureActiveHierarchy(
        Transform target,
        Transform stopAt,
        List<string> changes)
    {
        Transform current = target;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                Undo.RecordObject(current.gameObject, UndoName);
                current.gameObject.SetActive(true);
                changes.Add($"Mengaktifkan GameObject '{GetHierarchyPath(current)}'.");
            }

            if (current == stopAt)
            {
                break;
            }

            current = current.parent;
        }
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;

        while (parent != null)
        {
            path = $"{parent.name}/{path}";
            parent = parent.parent;
        }

        return path;
    }

    private static void LogAndShowSummary(
        XROrigin xrOrigin,
        List<string> changes,
        List<string> warnings,
        List<string> errors)
    {
        StringBuilder message = new StringBuilder();
        message.AppendLine("[WasteWise] Validasi dan perbaikan TrashBin AR selesai.");
        message.AppendLine($"XR Origin: {GetHierarchyPath(xrOrigin.transform)}");
        AppendSection(message, "Perbaikan", changes);
        AppendSection(message, "Peringatan", warnings);
        AppendSection(message, "Error", errors);
        message.AppendLine("Scene sudah ditandai dirty, tetapi belum disimpan. Tekan Ctrl+S untuk menyimpan scene.");

        string logMessage = message.ToString();

        if (errors.Count > 0)
        {
            Debug.LogError(logMessage, xrOrigin);
        }
        else if (warnings.Count > 0)
        {
            Debug.LogWarning(logMessage, xrOrigin);
        }
        else
        {
            Debug.Log(logMessage, xrOrigin);
        }

        if (Application.isBatchMode)
        {
            return;
        }

        string dialogMessage =
            $"Validasi selesai.\n\nPerbaikan: {changes.Count}\nPeringatan: {warnings.Count}\nError: {errors.Count}\n\n" +
            "Detail lengkap ada di Console. Tekan Ctrl+S untuk menyimpan scene.";

        EditorUtility.DisplayDialog(
            errors.Count > 0 ? "WasteWise AR - Perlu Perhatian" : "WasteWise AR",
            dialogMessage,
            "OK");
    }

    private static void AppendSection(StringBuilder builder, string title, List<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        builder.AppendLine($"{title}:");

        foreach (string item in items)
        {
            builder.AppendLine($"- {item}");
        }
    }

    private static void ShowFatalDialog(string message)
    {
        Debug.LogError($"[WasteWise] {message}");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("WasteWise AR", message, "OK");
        }
    }
}
