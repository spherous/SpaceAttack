using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScorePanel : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI scoreText;
    [SerializeField] public TextMeshProUGUI highscoreText;
    Score score;
    int highscore;
    private void Awake() {
        score = GameObject.FindObjectOfType<Score>();
        score.onScoreChanged += UpdateScore;
        scoreText.text = "0";
        highscore = PlayerPrefs.GetInt("Highscore", 0);
        highscoreText.text = $"{highscore}";
    }

    private void UpdateScore(int newScore)
    {
        scoreText.text = $"{newScore}";
        if(newScore > highscore)
        {
            highscore = newScore;
            highscoreText.text = $"{highscore}";
        }
    }
}
