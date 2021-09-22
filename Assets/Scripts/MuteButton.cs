using UnityEngine;
using UnityEngine.UI;

public class MuteButton : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private SlicedFilledImage slider;
    [SerializeField] private AdjustMixerGroupVolume mixerGroupVolume;
    public Sprite speaker;
    public Sprite mutedSpeaker;

    float volume = 1;
    private void Awake()
    {
        slider.onValueChanged += NewVolume;
        
        toggle.onValueChanged.AddListener(val => {
            slider.fillAmount = val ? 1 : 0;
            toggle.image.sprite = val ? speaker : mutedSpeaker;
            toggle.image.color = val ? Color.white : Color.gray;
        });

        float pref = PlayerPrefs.GetFloat("Volume", volume);
        slider.fillAmount = pref;
    }

    private void NewVolume(float fillAmount)
    {
        if(fillAmount <= 0 && toggle.image.sprite == speaker)
        {
            toggle.image.color = Color.gray;
            toggle.image.sprite = mutedSpeaker;
        }
        else if(fillAmount > 0 && toggle.image.sprite != speaker)
        {
            toggle.image.color = Color.white;
            toggle.image.sprite = speaker;
        }

        fillAmount = Mathf.Clamp01(fillAmount);
        fillAmount = fillAmount < 0.0001f ? 0.0001f : fillAmount;
        volume = fillAmount;
        mixerGroupVolume.SetVolume(volume);
    }

    public void SaveAudioSettings() => PlayerPrefs.SetFloat("Volume", volume);
}