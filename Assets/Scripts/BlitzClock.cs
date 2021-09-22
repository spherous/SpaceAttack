using System;
using System.Collections;
using System.Collections.Generic;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlitzClock : MonoBehaviour
{
    [SerializeField] private Image foreground;
    [SerializeField] private TextMeshProUGUI timeText;
    public AudioSource audioSource;
    public float startSeconds;
    public float remainingSeconds;
    Player player;
    
    private void Awake() {
        player = GameObject.FindObjectOfType<Player>();
        remainingSeconds = startSeconds;
        timeText.text = TimeSpan.FromSeconds(startSeconds).ToString(GetFormat(startSeconds));
        foreground.material.SetFloat("_FillPercent", 1);
        foreground.material.SetFloat("_Flashing", 0);
        foreground.material.SetFloat("_FlashingSpeed", 8);
    }

    private void Update() {
        if(remainingSeconds > 0 && !(player.isDead || player.isDying || !player.gameObject.activeSelf))
            remainingSeconds -= Time.deltaTime;
        
        if(remainingSeconds > 0)
            RefreshTimer();
        else
        {
            audioSource.Stop();
            player.TakeDamage(player.currentHealth, transform);
        }
    }

    public void RefreshTimer()
    {
        audioSource.pitch = 1.333f - (remainingSeconds * 0.025f);
        if(remainingSeconds < 10f)
        {
            foreground.material.SetFloat("_FlashingSpeed", remainingSeconds.Remap(0, 10, 12, 8));
        }

        if(remainingSeconds < 10f && !audioSource.isPlaying)
        {
            foreground.material.SetFloat("_Flashing", 1);
            audioSource.Play();
        }
        else if(remainingSeconds > 10f && audioSource.isPlaying)
        {
            foreground.material.SetFloat("_Flashing", 0);
            audioSource.Stop();
        }

        timeText.text = TimeSpan.FromSeconds(remainingSeconds).ToString(GetFormat(remainingSeconds));
        foreground.fillAmount = Mathf.Clamp(remainingSeconds/startSeconds, 0, 1);
        foreground.material.SetFloat("_FillPercent", foreground.fillAmount);
    }

    public void AddSeconds(float seconds)
    {
        if(player.isDead || player.isDying || !player.gameObject.activeSelf)
            return;

        remainingSeconds = remainingSeconds + seconds <= startSeconds ? remainingSeconds + seconds : startSeconds;
        RefreshTimer();
    }

    string GetFormat(float seconds) => seconds < 60 
        ? @"%s\.f" 
        : seconds < 3600
            ? @"%m\:%s\.f"
            : @"%h\:%m\:%s\.f";
}
