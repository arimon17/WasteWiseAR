using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ARHUDController : MonoBehaviour
{
    [Header("Referensi Game")]
    [SerializeField] private WasteGameManager gameManager;
    [SerializeField] private string menuSceneName = "MenuScene";
    [SerializeField, Min(0.5f)] private float feedbackDuration = 2.5f;

    [Header("Top Bar")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button menuButton;

    [Header("Feedback Banner")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private UIFadePanel feedbackFader;
    [SerializeField] private Image feedbackIndicator;
    [SerializeField] private TMP_Text feedbackStatusText;
    [SerializeField] private TMP_Text feedbackExplanationText;

    [Header("Session Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private UIFadePanel resultFader;
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private TMP_Text resultRecordText;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button replayButton;

    // cat-organik / cat-b3 dari design.md
    private static readonly Color CorrectColor = new Color(0.1804f, 0.4902f, 0.1961f); // #2e7d32
    private static readonly Color WrongColor = new Color(0.7765f, 0.1569f, 0.1569f);   // #c62828

    private int correctCount;
    private int wrongCount;
    private string lastItemName;
    private Coroutine feedbackRoutine;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<WasteGameManager>();
        }

        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(GoToMenu);
        if (replayButton != null) replayButton.onClick.AddListener(Replay);

        if (gameManager != null)
        {
            gameManager.OnAnswerEvaluated += HandleAnswer;
            gameManager.OnSessionCompleted += HandleSessionCompleted;
        }
        else
        {
            Debug.LogWarning("[WasteWise] ARHUDController: WasteGameManager tidak ditemukan; HUD tidak akan menerima event.");
        }
    }

    private void OnDestroy()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnAnswerEvaluated -= HandleAnswer;
        gameManager.OnSessionCompleted -= HandleSessionCompleted;
    }

    private void OnEnable()
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        RefreshTopBar();
    }

    private void Update()
    {
        string activeName = gameManager != null ? gameManager.ActiveItemName : string.Empty;

        if (activeName != lastItemName)
        {
            lastItemName = activeName;
            if (itemNameText != null)
            {
                itemNameText.text = string.IsNullOrEmpty(activeName) ? "Sampah: -" : $"Sampah: {activeName}";
            }
        }
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    public void Replay()
    {
        // Muat ulang scene AR agar sesi, tempat sampah, dan skor kembali ke awal.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void HandleAnswer(WasteItem item, bool correct, string explanation)
    {
        if (correct)
        {
            correctCount++;
        }
        else
        {
            wrongCount++;
        }

        RefreshTopBar();
        ShowFeedback(correct, explanation);
    }

    private void HandleSessionCompleted(int correct, int total)
    {
        int score = correct - wrongCount;
        int previous = PlayerPrefs.GetInt("HighScore", 0);
        bool newRecord = score > previous;

        if (newRecord)
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();
        }

        if (resultScoreText != null)
        {
            resultScoreText.text = $"Skor Akhir: {score}\nBenar {correct}, Salah {wrongCount}";
        }

        if (resultRecordText != null)
        {
            resultRecordText.text = newRecord ? "Rekor Baru!" : $"Rekor terbaik: {Mathf.Max(previous, score)}";
        }

        ShowPanel(resultPanel, resultFader);
    }

    private void ShowFeedback(bool correct, string explanation)
    {
        if (feedbackPanel == null)
        {
            return;
        }

        if (feedbackIndicator != null)
        {
            feedbackIndicator.color = correct ? CorrectColor : WrongColor;
        }

        if (feedbackStatusText != null)
        {
            feedbackStatusText.text = correct ? "BENAR" : "SALAH";
        }

        if (feedbackExplanationText != null)
        {
            feedbackExplanationText.text = explanation;
        }

        ShowPanel(feedbackPanel, feedbackFader);

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);

        HidePanel(feedbackPanel, feedbackFader);
        feedbackRoutine = null;
    }

    private static void ShowPanel(GameObject panel, UIFadePanel fader)
    {
        if (fader != null)
        {
            fader.Show();
        }
        else if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private static void HidePanel(GameObject panel, UIFadePanel fader)
    {
        if (fader != null)
        {
            fader.Hide();
        }
        else if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void RefreshTopBar()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Skor: {correctCount - wrongCount}";
        }

        if (progressText != null)
        {
            int quota = gameManager != null ? gameManager.SessionQuota : 0;
            progressText.text = quota > 0 ? $"{Mathf.Min(correctCount + 1, quota)} / {quota}" : "- / -";
        }
    }
}
