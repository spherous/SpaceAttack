using TMPro;
using UnityEngine;

public class WaveComplete : MonoBehaviour
{
    [SerializeField] private GroupFader fader;
    [SerializeField] private TextMeshProUGUI waveCompleteText;
    public float duration;
    public float? fadeAtTime;

    public void Show(int waveID)
    {
        fader.FadeIn();
        fadeAtTime = Time.timeSinceLevelLoad + duration;
        waveCompleteText.text = $"Wave {waveID} Complete!";
    }

    private void Update() {
        if(fadeAtTime.HasValue && Time.timeSinceLevelLoad >= fadeAtTime.Value)
        {
            fader.FadeOut();
            fadeAtTime = null;
        }
    }
}