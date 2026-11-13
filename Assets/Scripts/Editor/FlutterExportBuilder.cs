using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FlutterExportBuilder
{
    private const string MenuPath = "WasteWise/Export to Flutter Android Project";
    private const string ArScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem(MenuPath)]
    public static void ExportToFlutterAndroidProject()
    {
        string targetPath = Path.GetFullPath(Path.Combine(
            Application.dataPath, "../wastewise_flutter/android/unityLibrary"));

        if (!CleanArScene())
        {
            return;
        }

        ConfigureAndroidSettings();
        ConfigureBuildScenes();
        BuildUnityLibrary(targetPath);
        RemoveUnityLauncherIntentFilters(targetPath);
        DisableUnitySplashInManifest(targetPath);
    }

    private static bool CleanArScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[WasteWise] Hentikan Play Mode sebelum mengekspor ke Flutter.");
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return false;
        }

        Scene scene = EditorSceneManager.OpenScene(ArScenePath, OpenSceneMode.Single);

        if (ARHUDCanvasCleanup.EnsureRemoved(scene))
        {
            Debug.Log("[WasteWise] ARHUDCanvas dibersihkan sebelum ekspor.");
        }

        return true;
    }

    private static void RemoveUnityLauncherIntentFilters(string targetPath)
    {
        string manifestPath = Path.Combine(targetPath, "unityLibrary", "src", "main", "AndroidManifest.xml");

        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("[WasteWise] Manifest unityLibrary tidak ditemukan; patch launcher dilewati.");
            return;
        }

        XNamespace android = "http://schemas.android.com/apk/res/android";
        XDocument document = XDocument.Load(manifestPath);
        bool changed = false;

        foreach (XElement activity in document.Descendants("activity").ToList())
        {
            foreach (XElement intentFilter in activity.Elements("intent-filter").ToList())
            {
                bool isLauncher = intentFilter.Elements("category")
                    .Any(category => (string)category.Attribute(android + "name") == "android.intent.category.LAUNCHER");

                if (!isLauncher)
                {
                    continue;
                }

                intentFilter.Remove();
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        document.Save(manifestPath, SaveOptions.DisableFormatting);
        Debug.Log("[WasteWise] Intent-filter LAUNCHER Unity dihapus dari manifest unityLibrary.");
    }

    private static void ConfigureAndroidSettings()
    {
        EnsureXrSettingsPreloaded();
        DisableUnitySplash();
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        ARGraphicsApiFixer.FixAndroidGraphicsApi(); // flutter_unity_widget butuh OpenGLES3, bukan Vulkan
    }

    private static void DisableUnitySplash()
    {
        try
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            Debug.Log("[WasteWise] Splash screen Unity dinonaktifkan lewat PlayerSettings.");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("[WasteWise] PlayerSettings splash tidak bisa diubah (lisensi?), manifest akan dipatch setelah build. " + exception.Message);
        }
    }

    private static void DisableUnitySplashInManifest(string targetPath)
    {
        string manifestPath = Path.Combine(targetPath, "unityLibrary", "src", "main", "AndroidManifest.xml");

        if (!File.Exists(manifestPath))
        {
            return;
        }

        XNamespace android = "http://schemas.android.com/apk/res/android";
        XDocument document = XDocument.Load(manifestPath);
        bool changed = false;

        foreach (XElement metaData in document.Descendants("meta-data"))
        {
            if ((string)metaData.Attribute(android + "name") != "unity.splash-enable")
            {
                continue;
            }

            metaData.SetAttributeValue(android + "value", "False");
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        document.Save(manifestPath, SaveOptions.DisableFormatting);
        Debug.Log("[WasteWise] unity.splash-enable di manifest unityLibrary dipaksa False.");
    }

    private static void EnsureXrSettingsPreloaded()
    {
        UnityEngine.Object xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);

        if (xrSettings == null)
        {
            Debug.LogWarning("[WasteWise] XR settings Android tidak ditemukan; preloaded asset XR dilewati.");
            return;
        }

        UnityEngine.Object[] preloaded = PlayerSettings.GetPreloadedAssets();

        if (preloaded.Contains(xrSettings))
        {
            return;
        }

        PlayerSettings.SetPreloadedAssets(preloaded.Concat(new[] { xrSettings }).ToArray());
        Debug.Log("[WasteWise] XR settings Android ditambahkan ke preloaded assets.");
    }

    private static void ConfigureBuildScenes()
    {
        // UI Menu/Katalog dipindah ke Flutter; hanya scene AR yang dikirim ke Build Settings.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ArScenePath, true)
        };
    }

    private static void BuildUnityLibrary(string targetPath)
    {
        // Fresh export: bersihkan folder target agar BuildPlayer tidak mencoba append
        // ke Gradle project lama (penyebab "The build cannot be appended").
        ClearReadOnly(targetPath);
        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, true);
        }
        Directory.CreateDirectory(targetPath);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { ArScenePath };
        buildPlayerOptions.locationPathName = targetPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.AcceptExternalModificationsToPlayer;

        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("<color=green>EXPORT SUCCESS: unityLibrary successfully generated at " + targetPath + "</color>");
        }
        else
        {
            Debug.LogError("EXPORT FAILED: " + summary.totalErrors + " errors.");
        }
    }

    private static void ClearReadOnly(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        DirectoryInfo root = new DirectoryInfo(path);
        root.Attributes &= ~FileAttributes.ReadOnly;

        foreach (FileSystemInfo entry in root.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
        {
            entry.Attributes &= ~FileAttributes.ReadOnly;
        }
    }
}
