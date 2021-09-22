using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AudioButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private AudioSource source;
    [SerializeField] private Button button;
    public AudioClip hoverClip;
    public AudioClip pressClip;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(button != null && !button.interactable)
            return;
        source.PlayOneShot(hoverClip);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(button != null && !button.interactable)
            return;
        source.PlayOneShot(pressClip);
    } 
}