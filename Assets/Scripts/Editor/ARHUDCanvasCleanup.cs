using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ARHUDCanvasCleanup
{
    private const string ArScenePath = "Assets/Scenes/SampleScene.unity";
    private const string CanvasName = "ARHUDCanvas";

    [MenuItem("WasteWise/Remove AR HUD Canvas from AR Scene")]
    public static void RemoveFromArScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WasteWise] Hentikan Play Mode sebelum membersihkan AR HUD.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ArScenePath, OpenSceneMode.Single);

        if (EnsureRemoved(scene))
        {
            Debug.Log("[WasteWise] ARHUDCanvas dihapus dari SampleScene.");
        }
        else
        {
            Debug.Log("[WasteWise] ARHUDCanvas tidak ditemukan di SampleScene.");
        }
    }

    [InitializeOnLoadMethod]
    private static void ScheduleAutoCleanup()
    {
        EditorApplication.delayCall += AutoCleanupOpenScenes;
    }

    public static bool EnsureRemoved(Scene scene)
    {
        if (!RemoveCanvas(scene))
        {
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static void AutoCleanupOpenScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded && scene.path == ArScenePath && RemoveCanvas(scene))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ArScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("[WasteWise] ARHUDCanvas otomatis dihapus dari SampleScene.");
            }
        }
    }

    private static bool RemoveCanvas(Scene scene)
    {
        bool removed = false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CanvasName)
            {
                Object.DestroyImmediate(root);
                removed = true;
            }
        }

        return removed;
    }
}
