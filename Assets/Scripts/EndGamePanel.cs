using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndGamePanel : MonoBehaviour
{
    [SerializeField] private GroupFader fader;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void GameOver(int score)
    {
        scoreText.text = $"Score: {score}";

        int highscore = PlayerPrefs.GetInt("Highscore", 0);
        if(score > highscore)
            PlayerPrefs.SetInt("Highscore", score);

        fader.FadeIn();
    }
}