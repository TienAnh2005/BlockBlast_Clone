using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LevelSystemManager : MonoBehaviour
{
    private enum SessionMode
    {
        None,
        Level,
        Classic
    }

    private const string HighestUnlockedLevelKey = "HighestUnlockedLevel";
    private const int FallbackLevelCount = 5;
    private const string ResourcesLevelsPath = "Levels";

    [Header("Optional Manual References")]
    [SerializeField] private Board board;
    [SerializeField] private Blocks blocks;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private GameOverManager legacyGameOverManager;
    [SerializeField] private MainMenuManager mainMenuManager;

    [Header("Level Data")]
    [SerializeField] private LevelData[] levelDataAssets;

    [Header("Scene UI References")]
    [SerializeField] private RectTransform levelSelectPanel;
    [SerializeField] private RectTransform levelCompletePanel;
    [SerializeField] private RectTransform levelFailPanel;
    [SerializeField] private RectTransform settingPanel;
    [SerializeField] private UIPopupController levelCompletePopup;
    [SerializeField] private UIPopupController levelFailPopup;
    [SerializeField] private UIPopupController settingPopup;
    [SerializeField] private TMP_Text targetScoreText;
    [SerializeField] private TMP_Text levelCompleteBodyText;
    [SerializeField] private TMP_Text levelFailBodyText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private TMP_Text nextLevelButtonText;
    [FormerlySerializedAs("gameplayBackButton")]
    [SerializeField] private Button openSettingButton;
    [SerializeField] private Button settingRestartButton;
    [SerializeField] private Button settingHomeButton;
    [SerializeField] private GameObject highScorePanelObject;
    [SerializeField] private GameObject currentScoreObject;

    private readonly List<Button> levelButtons = new();
    private readonly List<TMP_Text> levelButtonLabels = new();
    private readonly List<Image> levelButtonImages = new();
    private readonly List<LevelData> orderedLevelData = new();

    private FieldInfo blocksArrayField;
    private FieldInfo polyominoIndexesField;
    private FieldInfo blockCountField;
    private MethodInfo generateMethod;

    private Block[] blockEntries;
    private int[] polyominoIndexes;

    private int currentLevel = -1;
    private int highestUnlockedLevel = 1;
    private bool levelRunning;
    private bool resultShown;
    private bool settingOpen;
    private SessionMode currentMode = SessionMode.None;

    public bool IsInitialized { get; private set; }

    private int TotalLevels => orderedLevelData.Count > 0 ? orderedLevelData.Count : FallbackLevelCount;

    private void Awake()
    {
        AutoResolveReferences();
        CacheReflection();
        CacheBlocks();
        LoadLevelDataAssets();
        DisableLegacyGameOver();
        CacheSceneUiReferences();
        CacheGameplayHudReferences();
        SetGameplayHudVisible(false);
        SetGameplayInteractable(false);
        SetBlockTrayVisible(false);
        IsInitialized = false;
    }

    private IEnumerator Start()
    {
        CacheSceneUiReferences();
        CacheGameplayHudReferences();

        // Wait one frame so Board.Start() and Blocks.Start() finish building
        // the board cells and the initial block batch before we reset them.
        yield return null;

        LoadLevelDataAssets();
        LoadSavedProgress();
        HookScoreEvents();

        if (board != null)
        {
            board.ClearBoardState();
        }

        if (scoreManager != null)
        {
            scoreManager.ResetCurrentScore();
        }

        RefreshLevelButtons();
        PrepareForMainMenu();
        IsInitialized = true;
    }

    private void Update()
    {
        if (levelRunning == false || resultShown == true)
        {
            return;
        }

        if (settingOpen)
        {
            return;
        }

        if (currentMode == SessionMode.Level && HasReachedTargetScore())
        {
            ResolveLevelCleared();
            return;
        }

        CacheBlocks();
        if (HasLoseState() == false)
        {
            return;
        }

        ResolveEndOfLevel();
    }

    private void OnDestroy()
    {
        UnhookScoreEvents();
    }

    private void AutoResolveReferences()
    {
        if (board == null)
        {
            board = FindObjectOfType<Board>();
        }

        if (blocks == null)
        {
            blocks = FindObjectOfType<Blocks>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        if (legacyGameOverManager == null)
        {
            legacyGameOverManager = GetComponent<GameOverManager>();
        }

        if (mainMenuManager == null)
        {
            mainMenuManager = GetComponent<MainMenuManager>();
        }
    }

    private void CacheReflection()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        blocksArrayField = typeof(Blocks).GetField("blocks", flags);
        polyominoIndexesField = typeof(Blocks).GetField("polyominoIndexes", flags);
        blockCountField = typeof(Blocks).GetField("blockCount", flags);
        generateMethod = typeof(Blocks).GetMethod("Generate", flags);
    }

    private void CacheBlocks()
    {
        if (blocks == null || blocksArrayField == null || polyominoIndexesField == null)
        {
            return;
        }

        blockEntries = blocksArrayField.GetValue(blocks) as Block[];
        polyominoIndexes = polyominoIndexesField.GetValue(blocks) as int[];
    }

    private void LoadLevelDataAssets()
    {
        orderedLevelData.Clear();

        if (levelDataAssets != null)
        {
            foreach (var levelData in levelDataAssets)
            {
                if (levelData != null)
                {
                    orderedLevelData.Add(levelData);
                }
            }
        }

        if (orderedLevelData.Count == 0)
        {
            var resourceLevels = Resources.LoadAll<LevelData>(ResourcesLevelsPath);
            foreach (var levelData in resourceLevels)
            {
                if (levelData != null)
                {
                    orderedLevelData.Add(levelData);
                }
            }
        }

        orderedLevelData.Sort((left, right) =>
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.levelId.CompareTo(right.levelId);
        });
    }

    private void DisableLegacyGameOver()
    {
        if (legacyGameOverManager != null)
        {
            legacyGameOverManager.enabled = false;
        }
    }

    private void CacheSceneUiReferences()
    {
        if (targetScoreText == null)
        {
            targetScoreText = FindChildComponent<TMP_Text>(transform, "TargetScoreText");
        }

        if (openSettingButton == null)
        {
            openSettingButton = FindChildComponent<Button>(transform, "OpenSettingButton");
        }

        if (openSettingButton == null)
        {
            openSettingButton = FindChildComponent<Button>(transform, "GameplayBackButton");
        }

        if (levelSelectPanel == null)
        {
            levelSelectPanel = FindChildRect(transform, "LevelSelectPanel");
        }

        if (levelCompletePanel == null)
        {
            levelCompletePanel = FindChildRect(transform, "LevelCompletePanel");
        }

        if (levelFailPanel == null)
        {
            levelFailPanel = FindChildRect(transform, "LevelFailPanel");
        }

        if (settingPanel == null)
        {
            settingPanel = FindChildRect(transform, "SettingPanel");
        }

        if (levelCompletePanel != null)
        {
            levelCompletePopup = EnsurePopupController(levelCompletePanel, levelCompletePopup);

            if (levelCompleteBodyText == null)
            {
                levelCompleteBodyText = FindChildComponent<TMP_Text>(levelCompletePanel, "LevelCompleteBodyText");
            }

            if (nextLevelButton == null)
            {
                nextLevelButton = FindChildComponent<Button>(levelCompletePanel, "NextLevelButton");
            }

            if (nextLevelButtonText == null)
            {
                nextLevelButtonText = FindChildComponent<TMP_Text>(levelCompletePanel, "NextLevelButtonText");
            }

            var levelCompleteBackButton = FindChildComponent<Button>(levelCompletePanel, "LevelCompleteBackButton");
            if (levelCompleteBackButton != null)
            {
                AddButtonListener(levelCompleteBackButton, HandleBackAction);
            }
        }

        if (levelFailPanel != null)
        {
            levelFailPopup = EnsurePopupController(levelFailPanel, levelFailPopup);

            if (levelFailBodyText == null)
            {
                levelFailBodyText = FindChildComponent<TMP_Text>(levelFailPanel, "LevelFailBodyText");
            }

            var retryButton = FindChildComponent<Button>(levelFailPanel, "RetryLevelButton");
            if (retryButton != null)
            {
                AddButtonListener(retryButton, RetryCurrentLevel);
            }

            var levelMenuButton = FindChildComponent<Button>(levelFailPanel, "BackToLevelsButton");
            if (levelMenuButton != null)
            {
                AddButtonListener(levelMenuButton, HandleBackAction);
            }
        }

        if (settingPanel != null)
        {
            settingPopup = EnsurePopupController(settingPanel, settingPopup);

            if (settingRestartButton == null)
            {
                settingRestartButton = FindChildComponent<Button>(settingPanel, "RestartButton");
            }

            if (settingHomeButton == null)
            {
                settingHomeButton = FindChildComponent<Button>(settingPanel, "HomeButton");
            }

            AddButtonListener(settingRestartButton, RestartFromSettings);
            AddButtonListener(settingHomeButton, ReturnHomeFromSettings);
        }

        if (openSettingButton != null)
        {
            AddButtonListener(openSettingButton, ToggleSettingPanel);
        }

        if (nextLevelButton != null)
        {
            AddButtonListener(nextLevelButton, GoToNextLevel);
        }

        CacheLevelButtonReferences();
    }

    private void CacheLevelButtonReferences()
    {
        levelButtons.Clear();
        levelButtonLabels.Clear();
        levelButtonImages.Clear();

        if (levelSelectPanel == null)
        {
            return;
        }

        var foundButtons = levelSelectPanel.GetComponentsInChildren<Button>(true);
        var cachedButtons = new List<Button>();
        foreach (var button in foundButtons)
        {
            if (button == null || TryGetLevelNumberFromName(button.name, out _) == false)
            {
                continue;
            }

            cachedButtons.Add(button);
        }

        cachedButtons.Sort((left, right) =>
        {
            TryGetLevelNumberFromName(left.name, out var leftNumber);
            TryGetLevelNumberFromName(right.name, out var rightNumber);
            return leftNumber.CompareTo(rightNumber);
        });

        foreach (var button in cachedButtons)
        {
            if (TryGetLevelNumberFromName(button.name, out var levelNumber) == false)
            {
                continue;
            }

            var label = FindChildComponent<TMP_Text>(button.transform, $"LevelButtonStatus_{levelNumber}");
            var image = button.GetComponent<Image>();
            if (label == null || image == null)
            {
                continue;
            }

            AddButtonListener(button, () => OnLevelSelected(levelNumber));
            levelButtons.Add(button);
            levelButtonLabels.Add(label);
            levelButtonImages.Add(image);
        }
    }

    private void OnLevelSelected(int level)
    {
        if (level > highestUnlockedLevel)
        {
            return;
        }

        StartLevel(level);
    }

    public void PrepareForMainMenu()
    {
        currentMode = SessionMode.None;
        currentLevel = -1;
        levelRunning = false;
        resultShown = false;
        settingOpen = false;

        if (board != null)
        {
            board.ClearBoardState();
        }

        if (scoreManager != null)
        {
            scoreManager.ResetCurrentScore();
        }

        ResetBlockWave();
        SetGameplayInteractable(false);
        SetBlockTrayVisible(false);
        SetGameplayHudVisible(false);

        if (levelSelectPanel != null)
        {
            levelSelectPanel.gameObject.SetActive(false);
        }

        if (levelCompletePanel != null)
        {
            HidePopupImmediately(levelCompletePanel, levelCompletePopup);
        }

        if (levelFailPanel != null)
        {
            HidePopupImmediately(levelFailPanel, levelFailPopup);
        }

        HideSettingPanelImmediately();
    }

    public void EnterLevelMode()
    {
        currentMode = SessionMode.Level;
        RefreshLevelButtons();
        ShowLevelSelection();
    }

    public void EnterClassicMode()
    {
        currentMode = SessionMode.Classic;
        StartClassicGame();
    }

    private void StartLevel(int level)
    {
        currentMode = SessionMode.Level;
        currentLevel = level;
        resultShown = false;
        levelRunning = true;
        settingOpen = false;

        if (levelSelectPanel != null)
        {
            levelSelectPanel.gameObject.SetActive(false);
        }

        if (levelCompletePanel != null)
        {
            HidePopupImmediately(levelCompletePanel, levelCompletePopup);
        }

        if (levelFailPanel != null)
        {
            HidePopupImmediately(levelFailPanel, levelFailPopup);
        }

        HideSettingPanelImmediately();

        if (scoreManager != null)
        {
            scoreManager.ResetCurrentScore();
        }

        if (board != null)
        {
            board.ClearBoardState();
        }

        SpawnPrePlacedBlocks(level);
        ResetBlockWave(GetOpeningBlockBatch(level));
        UpdateTargetText();
        SetGameplayHudVisible(true);
        SetBlockTrayVisible(true);
        SetGameplayInteractable(true);
    }

    private void StartClassicGame()
    {
        currentLevel = -1;
        resultShown = false;
        levelRunning = true;
        settingOpen = false;

        if (levelSelectPanel != null)
        {
            levelSelectPanel.gameObject.SetActive(false);
        }

        if (levelCompletePanel != null)
        {
            HidePopupImmediately(levelCompletePanel, levelCompletePopup);
        }

        if (levelFailPanel != null)
        {
            HidePopupImmediately(levelFailPanel, levelFailPopup);
        }

        HideSettingPanelImmediately();

        if (scoreManager != null)
        {
            scoreManager.ResetCurrentScore();
        }

        if (board != null)
        {
            board.ClearBoardState();
        }

        ResetBlockWave();
        SetGameplayHudVisible(true);
        SetBlockTrayVisible(true);
        SetGameplayInteractable(true);
    }

    private void ResolveEndOfLevel()
    {
        if (currentMode == SessionMode.Level && HasReachedTargetScore())
        {
            ResolveLevelCleared();
            return;
        }

        resultShown = true;
        levelRunning = false;
        settingOpen = false;
        SetGameplayInteractable(false);
        SetBlockTrayVisible(false);
        SetGameplayHudVisible(false);
        HideSettingPanelImmediately();
        var currentScore = scoreManager != null ? scoreManager.CurrentScore : 0;

        if (currentMode == SessionMode.Classic)
        {
            ShowClassicGameOver(currentScore);
            return;
        }

        var target = GetTargetScore(currentLevel);
        ShowLevelFail(currentScore, target);
    }

    private void ShowLevelComplete(int currentScore, int target)
    {
        if (levelCompleteBodyText != null)
        {
            levelCompleteBodyText.text = $"Level {currentLevel} cleared. Score {currentScore}/{target}.";
        }

        if (nextLevelButtonText != null)
        {
            nextLevelButtonText.text = currentLevel >= TotalLevels ? "LEVELS" : "NEXT LEVEL";
        }

        if (levelCompletePanel != null)
        {
            ShowPopup(levelCompletePanel, levelCompletePopup);
        }
    }

    private void ShowLevelFail(int currentScore, int target)
    {
        if (levelFailBodyText != null)
        {
            levelFailBodyText.text = $"Score {currentScore}/{target}. Reach the target score to unlock the next level.";
        }

        if (levelFailPanel != null)
        {
            ShowPopup(levelFailPanel, levelFailPopup);
        }
    }

    private void ShowClassicGameOver(int currentScore)
    {
        if (levelFailBodyText != null)
        {
            levelFailBodyText.text = $"Final score: {currentScore}.";
        }

        if (levelFailPanel != null)
        {
            ShowPopup(levelFailPanel, levelFailPopup);
        }
    }

    private void GoToNextLevel()
    {
        if (currentLevel >= TotalLevels)
        {
            ShowLevelSelection();
            return;
        }

        StartLevel(Mathf.Min(currentLevel + 1, highestUnlockedLevel));
    }

    private void RetryCurrentLevel()
    {
        if (currentMode == SessionMode.Classic)
        {
            StartClassicGame();
            return;
        }

        if (currentLevel <= 0)
        {
            ShowLevelSelection();
            return;
        }

        StartLevel(currentLevel);
    }

    private void ShowLevelSelection()
    {
        currentMode = SessionMode.Level;
        levelRunning = false;
        resultShown = false;
        settingOpen = false;

        if (board != null)
        {
            board.ClearBoardState();
        }

        if (scoreManager != null)
        {
            scoreManager.ResetCurrentScore();
        }

        ResetBlockWave();
        SetGameplayInteractable(false);
        SetBlockTrayVisible(false);
        SetGameplayHudVisible(false);
        RefreshLevelButtons();

        if (levelCompletePanel != null)
        {
            HidePopupImmediately(levelCompletePanel, levelCompletePopup);
        }

        if (levelFailPanel != null)
        {
            HidePopupImmediately(levelFailPanel, levelFailPopup);
        }

        HideSettingPanelImmediately();

        if (levelSelectPanel != null)
        {
            levelSelectPanel.gameObject.SetActive(true);
        }

        if (targetScoreText != null)
        {
            targetScoreText.text = "TARGET: SELECT A LEVEL";
        }
    }

    private void HandleBackAction()
    {
        if (currentMode == SessionMode.Classic)
        {
            PrepareForMainMenu();
            if (mainMenuManager != null)
            {
                mainMenuManager.ShowMainMenu();
            }

            return;
        }

        ShowLevelSelection();
    }

    private void RefreshLevelButtons()
    {
        for (var i = 0; i < levelButtons.Count; ++i)
        {
            var level = i + 1;
            var unlocked = level <= highestUnlockedLevel;

            levelButtons[i].interactable = unlocked;
            levelButtonImages[i].color = unlocked ? new Color(0.32f, 0.77f, 0.20f, 1f) : new Color(0.68f, 0.72f, 0.78f, 1f);
            var levelData = GetLevelData(level);
            levelButtonLabels[i].text = unlocked
                ? $"TARGET {GetTargetScore(level)}"
                : "LOCKED";

            if (levelData != null)
            {
                var numberLabel = FindChildComponent<TMP_Text>(levelButtons[i].transform, $"LevelButtonText_{level}");
                if (numberLabel != null)
                {
                    numberLabel.text = levelData.levelId.ToString();
                }
            }
        }
    }

    private void UpdateTargetText()
    {
        if (targetScoreText == null)
        {
            return;
        }

        targetScoreText.text = $"TARGET: {GetTargetScore(currentLevel)}";
    }

    private void CacheGameplayHudReferences()
    {
        if (highScorePanelObject == null)
        {
            highScorePanelObject = FindChildRect(transform, "HighScorePanel")?.gameObject;
        }

        if (currentScoreObject == null)
        {
            currentScoreObject = FindChildComponent<TMP_Text>(transform, "CurrentScoreText")?.gameObject;
        }
    }

    private int GetTargetScore(int level)
    {
        var levelData = GetLevelData(level);
        return levelData != null ? Mathf.Max(0, levelData.targetScore) : level * 10;
    }

    private LevelData GetLevelData(int level)
    {
        if (orderedLevelData.Count == 0)
        {
            return null;
        }

        var index = Mathf.Clamp(level - 1, 0, orderedLevelData.Count - 1);
        return orderedLevelData[index];
    }

    private void LoadSavedProgress()
    {
        highestUnlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt(HighestUnlockedLevelKey, 1), 1, TotalLevels);
        SaveUnlockedProgress();
    }

    private void SaveUnlockedProgress()
    {
        highestUnlockedLevel = Mathf.Clamp(highestUnlockedLevel, 1, TotalLevels);
        PlayerPrefs.SetInt(HighestUnlockedLevelKey, highestUnlockedLevel);
        PlayerPrefs.Save();
    }

    private void HookScoreEvents()
    {
        if (scoreManager == null)
        {
            return;
        }

        scoreManager.ScoreChanged -= HandleScoreChanged;
        scoreManager.ScoreChanged += HandleScoreChanged;
    }

    private void UnhookScoreEvents()
    {
        if (scoreManager == null)
        {
            return;
        }

        scoreManager.ScoreChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged(int currentScore)
    {
        if (currentMode != SessionMode.Level || levelRunning == false || resultShown == true || currentLevel <= 0)
        {
            return;
        }

        if (currentScore >= GetTargetScore(currentLevel))
        {
            ResolveLevelCleared();
        }
    }

    private bool HasReachedTargetScore()
    {
        if (currentMode != SessionMode.Level || scoreManager == null || currentLevel <= 0)
        {
            return false;
        }

        return scoreManager.CurrentScore >= GetTargetScore(currentLevel);
    }

    private void ResolveLevelCleared()
    {
        if (resultShown)
        {
            return;
        }

        resultShown = true;
        levelRunning = false;
        SetGameplayInteractable(false);
        SetBlockTrayVisible(false);
        SetGameplayHudVisible(false);

        highestUnlockedLevel = Mathf.Clamp(Mathf.Max(highestUnlockedLevel, currentLevel + 1), 1, TotalLevels);
        SaveUnlockedProgress();
        RefreshLevelButtons();

        var currentScore = scoreManager != null ? scoreManager.CurrentScore : 0;
        ShowLevelComplete(currentScore, GetTargetScore(currentLevel));
    }

    private int[] GetOpeningBlockBatch(int level)
    {
        var levelData = GetLevelData(level);
        if (levelData == null)
        {
            return null;
        }

        CacheBlocks();
        var blockSlotCount = blockEntries != null && blockEntries.Length > 0 ? blockEntries.Length : 3;
        var batch = PieceGenerator.Generate(board, blockSlotCount);
        var hasConfiguredBlock = false;

        for (var i = 0; i < Mathf.Min(LevelData.OpeningBlockCount, blockSlotCount); ++i)
        {
            var openingBlockIndex = levelData.GetOpeningBlockPolyominoIndex(i);
            if (openingBlockIndex < 0)
            {
                continue;
            }

            batch[i] = openingBlockIndex;
            hasConfiguredBlock = true;
        }

        return hasConfiguredBlock ? batch : null;
    }

    private void ResetBlockWave(int[] nextBatchOverride = null)
    {
        CacheBlocks();
        if (blocks == null || blockCountField == null || generateMethod == null)
        {
            return;
        }

        blocks.ClearPendingBatches();
        if (nextBatchOverride != null && nextBatchOverride.Length > 0)
        {
            blocks.SetNextBatchOverride(nextBatchOverride);
        }

        blockCountField.SetValue(blocks, 0);
        generateMethod.Invoke(blocks, null);
        CacheBlocks();
    }

    private void SetGameplayInteractable(bool value)
    {
        CacheBlocks();

        if (blockEntries == null)
        {
            return;
        }

        foreach (var blockEntry in blockEntries)
        {
            if (blockEntry == null)
            {
                continue;
            }

            blockEntry.enabled = value;

            var collider2D = blockEntry.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.enabled = value;
            }
        }
    }

    private void SetBlockTrayVisible(bool value)
    {
        CacheBlocks();

        if (blockEntries == null)
        {
            return;
        }

        foreach (var blockEntry in blockEntries)
        {
            if (blockEntry == null)
            {
                continue;
            }

            blockEntry.gameObject.SetActive(value);
        }
    }

    private void SetGameplayHudVisible(bool value)
    {
        CacheGameplayHudReferences();

        if (highScorePanelObject != null)
        {
            highScorePanelObject.SetActive(value);
        }

        if (currentScoreObject != null)
        {
            currentScoreObject.SetActive(value);
        }

        if (targetScoreText != null)
        {
            targetScoreText.gameObject.SetActive(value && currentMode == SessionMode.Level);
        }

        if (openSettingButton != null)
        {
            openSettingButton.gameObject.SetActive(value);
        }
    }

    private void ToggleSettingPanel()
    {
        if (settingPanel == null || levelRunning == false || resultShown)
        {
            return;
        }

        if (settingOpen)
        {
            CloseSettingPanel();
            return;
        }

        OpenSettingPanel();
    }

    private void OpenSettingPanel()
    {
        if (settingPanel == null)
        {
            return;
        }

        settingOpen = true;
        SetGameplayInteractable(false);
        ShowPopup(settingPanel, settingPopup);

        if (openSettingButton != null)
        {
            openSettingButton.transform.SetAsLastSibling();
        }
    }

    private void CloseSettingPanel()
    {
        if (settingPanel == null)
        {
            return;
        }

        settingOpen = false;

        if (settingPopup != null)
        {
            settingPopup.ClosePopup();
        }
        else
        {
            settingPanel.gameObject.SetActive(false);
        }

        if (levelRunning && resultShown == false)
        {
            SetGameplayInteractable(true);
        }
    }

    private void HideSettingPanelImmediately()
    {
        settingOpen = false;

        if (settingPanel == null)
        {
            return;
        }

        HidePopupImmediately(settingPanel, settingPopup);
    }

    private void RestartFromSettings()
    {
        HideSettingPanelImmediately();
        RetryCurrentLevel();
    }

    private void ReturnHomeFromSettings()
    {
        HideSettingPanelImmediately();

        if (mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
            return;
        }

        PrepareForMainMenu();
    }

    private bool HasLoseState()
    {
        if (board == null || blockEntries == null || polyominoIndexes == null)
        {
            return false;
        }

        if (board.HasForcedGameOver)
        {
            return true;
        }

        if (polyominoIndexes.Length != blockEntries.Length)
        {
            return false;
        }

        var hasActiveBlock = false;
        for (var i = 0; i < blockEntries.Length; ++i)
        {
            var blockEntry = blockEntries[i];
            if (blockEntry == null || blockEntry.gameObject.activeSelf == false)
            {
                continue;
            }

            hasActiveBlock = true;
            if (board.CheckPlace(polyominoIndexes[i]))
            {
                return false;
            }
        }

        return hasActiveBlock;
    }

    private void SpawnPrePlacedBlocks(int levelID)
    {
        if (board == null)
        {
            return;
        }

        var levelData = GetLevelData(levelID);
        if (levelData == null)
        {
            return;
        }

        foreach (var configuredCell in levelData.GetPrePlacedCells())
        {
            SpawnLevelCell(configuredCell);
        }
    }

    private void SpawnLevelCell(LevelConfiguredCell configuredCell)
    {
        if (board.TryPrePlaceCell(configuredCell.point, configuredCell.blockType) == false)
        {
            Debug.LogWarning($"Failed to pre-place cell at {configuredCell.point}. Check overlap / bounds.");
        }
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

    private UIPopupController EnsurePopupController(RectTransform panel, UIPopupController existingPopup)
    {
        if (panel == null)
        {
            return existingPopup;
        }

        if (existingPopup == null)
        {
            existingPopup = panel.GetComponent<UIPopupController>();
        }

        if (existingPopup == null)
        {
            existingPopup = panel.gameObject.AddComponent<UIPopupController>();
        }

        existingPopup.Configure();
        return existingPopup;
    }

    private void ShowPopup(RectTransform panel, UIPopupController popupController)
    {
        if (panel == null)
        {
            return;
        }

        if (popupController != null)
        {
            popupController.OpenPopup();
            return;
        }

        panel.gameObject.SetActive(true);
    }

    private void HidePopupImmediately(RectTransform panel, UIPopupController popupController)
    {
        if (panel == null)
        {
            return;
        }

        if (popupController != null)
        {
            popupController.HideImmediately();
            return;
        }

        panel.gameObject.SetActive(false);
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

    private bool TryGetLevelNumberFromName(string objectName, out int levelNumber)
    {
        levelNumber = 0;
        const string prefix = "LevelButton_";
        if (string.IsNullOrEmpty(objectName) || objectName.StartsWith(prefix) == false)
        {
            return false;
        }

        return int.TryParse(objectName.Substring(prefix.Length), out levelNumber);
    }
}
