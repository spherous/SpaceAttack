using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AudioSlider : MonoBehaviour, IScrollHandler, IDragHandler
{
    [SerializeField] private SlicedFilledImage slider;

    public void OnDrag(PointerEventData eventData)
    {
        // Debug.Log(eventData.delta.y);
        slider.fillAmount += eventData.delta.y / 200f;
    }

    public void OnScroll(PointerEventData eventData) => slider.fillAmount += eventData.scrollDelta.y / 48f;
}
