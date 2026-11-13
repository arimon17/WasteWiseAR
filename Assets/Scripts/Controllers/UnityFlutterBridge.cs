using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

[DisallowMultipleComponent]
[Preserve]
public class UnityFlutterBridge : MonoBehaviour
{
    private const string ResetCommand = "reset";
    private const string SyncCommand = "syncState";

    public static UnityFlutterBridge Instance { get; private set; }

    private WasteGameManager gameManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        RemoveLegacyUnityHud();

        if (FindAnyObjectByType<UnityFlutterBridge>() != null)
        {
            return;
        }

        GameObject go = new GameObject("UnityFlutterBridge");
        go.AddComponent<UnityFlutterBridge>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindGameManager();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindGameManager();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindGameManager();
    }

    private void BindGameManager()
    {
        UnbindGameManager();

        gameManager = FindAnyObjectByType<WasteGameManager>();

        if (gameManager == null)
        {
            return;
        }

        gameManager.OnAnswerEvaluated += HandleAnswerEvaluated;
        gameManager.OnSessionCompleted += HandleSessionCompleted;

        SendGameStateToFlutter(0, 1, gameManager.SessionQuota);
    }

    private void UnbindGameManager()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnAnswerEvaluated -= HandleAnswerEvaluated;
        gameManager.OnSessionCompleted -= HandleSessionCompleted;
        gameManager = null;
    }

    private void HandleAnswerEvaluated(WasteItem item, bool correct, string explanation)
    {
        SendFeedbackToFlutter(explanation, correct);
    }

    private void HandleSessionCompleted(int correct, int total)
    {
        int score = gameManager != null ? gameManager.Score : correct;

        if (gameManager != null)
        {
            SendGameStateToFlutter(score, gameManager.SessionQuota, gameManager.SessionQuota);
        }

        SendFeedbackToFlutter($"Sesi selesai! Skor akhir: {score}", score >= 0);
    }

    private static void RemoveLegacyUnityHud()
    {
        GameObject hud = GameObject.Find("ARHUDCanvas");

        if (hud == null)
        {
            return;
        }

        hud.SetActive(false);
        UnityEngine.Object.Destroy(hud);
    }

    // UnityMessageManager (flutter_unity_widget) dipanggil lewat refleksi supaya proyek
    // tetap 0 error sebelum package flutter_unity_widget diimport.
    private static Type s_managerType;
    private static PropertyInfo s_instanceProperty;
    private static MethodInfo s_sendMethod;
    private static bool s_managerResolved;
    private static bool s_warned;

    public void SendGameStateToFlutter(int score, int progress, int total)
    {
        if (total <= 0)
        {
            return;
        }

        SendToFlutter(JsonUtility.ToJson(new FlutterGameUpdateMessage
        {
            type = "gameUpdate",
            score = score,
            progress = Mathf.Clamp(progress, 0, total),
            total = total
        }));
    }

    private static void SendFeedbackToFlutter(string message, bool correct)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        SendToFlutter(JsonUtility.ToJson(new FlutterFeedbackMessage
        {
            type = "feedback",
            message = message,
            correct = correct
        }));
    }

    [Preserve]
    public void OnReceiveFlutterCommand(string jsonCommand)
    {
        if (string.IsNullOrWhiteSpace(jsonCommand))
        {
            return;
        }

        Debug.Log($"[WasteWise] Perintah dari Flutter: {jsonCommand}");

        string command = ParseCommand(jsonCommand);

        if (string.Equals(command, ResetCommand, StringComparison.OrdinalIgnoreCase))
        {
            Scene scene = SceneManager.GetActiveScene();

            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
            else
            {
                Debug.LogWarning("[WasteWise] Scene AR belum terdaftar di Build Settings; reset dilewati.");
            }
        }
        else if (string.Equals(command, SyncCommand, StringComparison.OrdinalIgnoreCase))
        {
            SendCurrentGameState();
        }
        // Perintah lain (mis. changeMode) ditambahkan di sini sesuai kebutuhan Flutter.
    }

    public void SendCurrentGameState()
    {
        if (gameManager == null)
        {
            BindGameManager();
        }

        if (gameManager == null)
        {
            return;
        }

        int progress = Mathf.Clamp(gameManager.SortedCount + 1, 1, gameManager.SessionQuota);
        SendGameStateToFlutter(gameManager.Score, progress, gameManager.SessionQuota);
    }

    private static void SendToFlutter(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        // Jalur normal: UnityMessageManager dari plugin flutter_unity_widget.
        if (TrySendViaMessageManager(json))
        {
            return;
        }

        // Fallback: proyek ini tidak mengimpor paket .unitypackage flutter_unity_widget,
        // jadi panggil langsung jembatan Android milik plugin Flutter.
        SendViaAndroidPlugin(json);
    }

    private static bool TrySendViaMessageManager(string json)
    {
        if (!s_managerResolved)
        {
            ResolveManager();
        }

        if (s_managerType == null || s_instanceProperty == null || s_sendMethod == null)
        {
            return false;
        }

        try
        {
            object manager = s_instanceProperty.GetValue(null);

            if (manager == null)
            {
                return false;
            }

            s_sendMethod.Invoke(manager, new object[] { json });
            return true;
        }
        catch (Exception exception)
        {
            WarnOnce("[WasteWise] UnityMessageManager gagal dipakai, beralih ke fallback Android: " + exception.Message);
            return false;
        }
    }

    private static void SendViaAndroidPlugin(string json)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass playerUtils = new AndroidJavaClass("com.xraph.plugin.flutter_unity_widget.UnityPlayerUtils"))
            {
                playerUtils.CallStatic("onUnityMessage", json);
            }
        }
        catch (Exception exception)
        {
            WarnOnce("[WasteWise] Gagal mengirim pesan ke Flutter via UnityPlayerUtils: " + exception.Message);
        }
#else
        WarnOnce("[WasteWise] Pengiriman pesan Unity -> Flutter hanya tersedia di build Android.");
#endif
    }

    private static void WarnOnce(string message)
    {
        if (s_warned)
        {
            return;
        }

        s_warned = true;
        Debug.LogWarning(message);
    }

    private static void ResolveManager()
    {
        s_managerResolved = true;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType("FlutterUnityIntegration.UnityMessageManager")
                     ?? assembly.GetType("UnityMessageManager");

            if (type == null)
            {
                continue;
            }

            s_managerType = type;
            s_instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            s_sendMethod = type.GetMethod("SendMessageToFlutter", BindingFlags.Public | BindingFlags.Instance);

            if (s_instanceProperty == null || s_sendMethod == null)
            {
                WarnOnce("[WasteWise] UnityMessageManager ditemukan tetapi API-nya tidak cocok (perlu property Instance dan method SendMessageToFlutter).");
            }

            return;
        }
    }

    private static string ParseCommand(string json)
    {
        try
        {
            return JsonUtility.FromJson<FlutterCommand>(json)?.command;
        }
        catch (Exception)
        {
            return null;
        }
    }

    [Serializable]
    private class FlutterCommand
    {
        public string command;
    }

    [Serializable]
    private class FlutterGameUpdateMessage
    {
        public string type;
        public int score;
        public int progress;
        public int total;
    }

    [Serializable]
    private class FlutterFeedbackMessage
    {
        public string type;
        public string message;
        public bool correct;
    }
}
