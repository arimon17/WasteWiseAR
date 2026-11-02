using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button catalogButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button catalogCloseButton;
    [SerializeField] private Button infoCloseButton;
    [SerializeField] private GameObject catalogPanel;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private string arSceneName = "ARScene";

    private void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(StartGame);
        if (catalogButton != null) catalogButton.onClick.AddListener(OpenCatalog);
        if (infoButton != null) infoButton.onClick.AddListener(OpenInfo);
        if (catalogCloseButton != null) catalogCloseButton.onClick.AddListener(ClosePanels);
        if (infoCloseButton != null) infoCloseButton.onClick.AddListener(ClosePanels);
    }

    private void OnEnable()
    {
        ClosePanels();
        RefreshHighScore();
    }

    public void StartGame()
    {
        if (string.IsNullOrEmpty(arSceneName))
        {
            Debug.LogError("[WasteWise] Nama scene AR kosong; tidak bisa berpindah scene.");
            return;
        }

        SceneManager.LoadScene(arSceneName);
    }

    public void OpenCatalog()
    {
        SetPanel(catalogPanel, true);
        SetPanel(infoPanel, false);
    }

    public void OpenInfo()
    {
        SetPanel(infoPanel, true);
        SetPanel(catalogPanel, false);
    }

    public void ClosePanels()
    {
        SetPanel(catalogPanel, false);
        SetPanel(infoPanel, false);
    }

    private static void SetPanel(GameObject panel, bool visible)
    {
        if (panel == null)
        {
            return;
        }

        UIFadePanel fader = panel.GetComponent<UIFadePanel>();

        if (fader == null)
        {
            panel.SetActive(visible);
            return;
        }

        if (visible)
        {
            fader.Show();
        }
        else
        {
            fader.Hide();
        }
    }

    public void RefreshHighScore()
    {
        if (highScoreText == null)
        {
            return;
        }

        int best = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = best.ToString();
    }
}
