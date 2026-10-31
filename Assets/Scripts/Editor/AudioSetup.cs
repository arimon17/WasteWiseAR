using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AudioSetup
{
    private const string MenuPath = "WasteWise/Attach Audio Manager to Scenes";
    private const string ArScenePath = "Assets/Scenes/ARScene.unity";
    private const string FallbackArScenePath = "Assets/Scenes/SampleScene.unity";
    private const string AudioManagerName = "AudioManager";

    [MenuItem(MenuPath)]
    public static void Attach()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WasteWise] Hentikan Play Mode sebelum menjalankan Attach Audio Manager to Scenes.");
            return;
        }

        string scenePath = File.Exists(ArScenePath)
            ? ArScenePath
            : (File.Exists(FallbackArScenePath) ? FallbackArScenePath : null);

        if (scenePath == null)
        {
            Debug.LogError("[WasteWise] Scene AR tidak ditemukan (ARScene.unity atau SampleScene.unity).");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include) == null)
        {
            new GameObject(AudioManagerName, typeof(AudioManager));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[WasteWise] AudioManager terpasang di '{scenePath}'.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "WasteWise AR",
                $"AudioManager selesai dipasang di scene '{scene.name}'.\n\nAudioSource dan clip SFX dibuat otomatis saat runtime.",
                "OK");
        }
    }
}
