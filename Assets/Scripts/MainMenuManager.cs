using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Optional Manual References")]
    [SerializeField] private LevelSystemManager levelSystemManager;

    [Header("Scene UI References")]
    [SerializeField] private RectTransform mainMenuPanel;
    [SerializeField] private Image mainMenuBackgroundImage;
    [SerializeField] private Button classicModeButton;
    [SerializeField] private Button levelModeButton;
    [SerializeField] private TMP_Text classicModeHighScoreText;

    private void Awake()
    {
        AutoResolveReferences();
        CacheSceneUiReferences();
        SetMenuButtonsInteractable(false);
        if (mainMenuPanel != null)
        {
            mainMenuPanel.gameObject.SetActive(true);
            mainMenuPanel.SetAsLastSibling();
        }

        RefreshMainMenuHighScore();
    }

    private IEnumerator Start()
    {
        CacheSceneUiReferences();
        SetMenuButtonsInteractable(false);

        while (levelSystemManager != null && levelSystemManager.IsInitialized == false)
        {
            yield return null;
        }

        ShowMainMenu();
        SetMenuButtonsInteractable(true);
    }

    public void ShowMainMenu()
    {
        AutoResolveReferences();
        CacheSceneUiReferences();

        if (levelSystemManager != null)
        {
            levelSystemManager.PrepareForMainMenu();
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.gameObject.SetActive(true);
            mainMenuPanel.SetAsLastSibling();
        }

        RefreshMainMenuHighScore();
    }

    private void StartLevelMode()
    {
        if (levelSystemManager != null && levelSystemManager.IsInitialized == false)
        {
            return;
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.gameObject.SetActive(false);
        }

        if (levelSystemManager != null)
        {
            levelSystemManager.EnterLevelMode();
        }
    }

    private void StartClassicMode()
    {
        if (levelSystemManager != null && levelSystemManager.IsInitialized == false)
        {
            return;
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.gameObject.SetActive(false);
        }

        if (levelSystemManager != null)
        {
            levelSystemManager.EnterClassicMode();
        }
    }

    private void AutoResolveReferences()
    {
        if (levelSystemManager == null)
        {
            levelSystemManager = GetComponent<LevelSystemManager>();
        }
    }

    private void CacheSceneUiReferences()
    {
        if (mainMenuPanel == null)
        {
            mainMenuPanel = FindChildRect(transform, "MainMenuPanel");
        }

        if (mainMenuPanel == null)
        {
            return;
        }

        if (mainMenuBackgroundImage == null)
        {
            mainMenuBackgroundImage = mainMenuPanel.GetComponent<Image>();
        }

        if (levelModeButton == null)
        {
            levelModeButton = FindChildComponent<Button>(mainMenuPanel, "LevelModeMenuButton");
        }

        if (classicModeButton == null)
        {
            classicModeButton = FindChildComponent<Button>(mainMenuPanel, "ClassicModeMenuButton");
        }

        if (classicModeHighScoreText == null && classicModeButton != null)
        {
            classicModeHighScoreText = FindChildComponent<TMP_Text>(classicModeButton.transform, "HighScoreText");
        }

        AddButtonListener(levelModeButton, StartLevelMode);
        AddButtonListener(classicModeButton, StartClassicMode);
        RefreshMainMenuHighScore();
    }

    private void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void SetMenuButtonsInteractable(bool value)
    {
        if (levelModeButton != null)
        {
            levelModeButton.interactable = value;
        }

        if (classicModeButton != null)
        {
            classicModeButton.interactable = value;
        }
    }

    private void RefreshMainMenuHighScore()
    {
        if (classicModeHighScoreText == null)
        {
            return;
        }

        var highScore = ScoreManager.Instance != null
            ? ScoreManager.Instance.HighScore
            : ScoreManager.GetStoredHighScore();

        classicModeHighScoreText.text = highScore.ToString();
    }

    private RectTransform FindChildRect(Transform parent, string name)
    {
        var child = FindNamedTransformRecursive(parent, name);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private T FindChildComponent<T>(Transform parent, string name) where T : Component
    {
        var child = FindNamedTransformRecursive(parent, name);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindNamedTransformRecursive(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (var child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child != parent && child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}
