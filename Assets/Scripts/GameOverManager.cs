using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private Blocks blocks;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text restartButtonText;

    private FieldInfo boardField;
    private FieldInfo blocksField;
    private FieldInfo polyominoIndexesField;

    private Board board;
    private Block[] blockEntries;
    private int[] polyominoIndexes;

    private bool gameOverShown;

    private void Awake()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        boardField = typeof(Blocks).GetField("board", flags);
        blocksField = typeof(Blocks).GetField("blocks", flags);
        polyominoIndexesField = typeof(Blocks).GetField("polyominoIndexes", flags);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (titleText != null)
        {
            titleText.text = "NO SPACE LEFT!";
        }

        if (restartButtonText != null)
        {
            restartButtonText.text = "PLAY";
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    private void Start()
    {
        RefreshState();
    }

    private void Update()
    {
        if (gameOverShown)
        {
            return;
        }

        RefreshState();
        if (HasLoseState())
        {
            ShowGameOver();
        }
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }
    }

    private void RefreshState()
    {
        if (blocks == null || boardField == null || blocksField == null || polyominoIndexesField == null)
        {
            return;
        }

        board = boardField.GetValue(blocks) as Board;
        blockEntries = blocksField.GetValue(blocks) as Block[];
        polyominoIndexes = polyominoIndexesField.GetValue(blocks) as int[];
    }

    private bool HasLoseState()
    {
        if (board == null || blockEntries == null || polyominoIndexes == null)
        {
            return false;
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

    private void ShowGameOver()
    {
        gameOverShown = true;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (blocks != null)
        {
            blocks.enabled = false;
        }

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

            blockEntry.enabled = false;

            var collider2D = blockEntry.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.enabled = false;
            }
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
