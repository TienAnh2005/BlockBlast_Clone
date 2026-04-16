using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";

    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TMP_Text currentScoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private int pointsPerClearedLine = 10;

    private int currentScore;
    private int highScore;

    public int PointsPerClearedLine => pointsPerClearedLine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        RefreshUI();
    }

    public void AddScore(int points)
    {
        if (points <= 0)
        {
            return;
        }

        currentScore += points;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (currentScoreText != null)
        {
            currentScoreText.text = currentScore.ToString();
        }

        if (highScoreText != null)
        {
            highScoreText.text = highScore.ToString();
        }
    }
}
