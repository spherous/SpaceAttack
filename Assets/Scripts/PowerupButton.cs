using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PowerupButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private Image BG;
    [SerializeField] private Image FG;
    public Color hoverBGColor;
    public Color clickedBGColor;
    public Color defaultBGColor {get; private set;}
    public int buttonID;
    bool isSelected = false;
    private void Awake()
    {
        defaultBGColor = BG.color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isSelected = true;
        pauseMenu.SetPowerupSelection(buttonID);
        BG.color = clickedBGColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        BG.color = hoverBGColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        BG.color = isSelected ? clickedBGColor : defaultBGColor;
    }

    public void Deselect()
    {
        isSelected = false;
        BG.color = defaultBGColor;
    }
    public void SetSprite(Sprite sprite)
    {
        FG.sprite = sprite;
    }
}