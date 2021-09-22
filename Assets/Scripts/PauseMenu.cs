using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public class PauseMenu : SerializedMonoBehaviour
{
    [SerializeField] private GroupFader fader;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject powerupPanel;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Player player;
    public bool paused = false;
    public bool swappedToContinue = false;

    [OdinSerialize] public Dictionary<WeaponType, Sprite> weaponsSpriteDict = new Dictionary<WeaponType, Sprite>();
    [OdinSerialize] public Dictionary<Powerups, Sprite> powerupsSpriteDict = new Dictionary<Powerups, Sprite>();

    public Sprite healSprite;
    public PowerupButton upgradeButton1;
    public PowerupButton upgradeButton2;
    public PowerupButton upgradeButton3;
    IEnumerable<PowerupButton> PowerupButtons()
    {
        yield return upgradeButton1;
        yield return upgradeButton2;
        yield return upgradeButton3;
    }
    public Enum upgradeSlot1;
    public Enum upgradeSlot2;
    public Enum upgradeSlot3;

    public int? selectedButton = null;
    public bool? hasUpgradeChoice = null;

    private void Awake()
    {
        Enable(true);
    }

    private void Start()
    {
        List<WeaponType> types = weaponsSpriteDict.Select(kvp => kvp.Key).ToList();
        List<WeaponType> randomChosen = new List<WeaponType>();
        for(int i = 0; i < 3; i++)
        {
            int index = UnityEngine.Random.Range(0, types.Count);
            randomChosen.Add(types[index]);
            types.RemoveAt(index);
        }

        upgradeSlot1 = randomChosen[0];
        upgradeButton1.SetSprite(weaponsSpriteDict[(WeaponType)upgradeSlot1]);

        upgradeSlot2 = randomChosen[1];
        upgradeButton2.SetSprite(weaponsSpriteDict[(WeaponType)upgradeSlot2]);

        upgradeSlot3 = randomChosen[2];
        upgradeButton3.SetSprite(weaponsSpriteDict[(WeaponType)upgradeSlot3]);
    }

    public void OfferPowerups()
    {
        List<Powerups> types = powerupsSpriteDict.Select(kvp => kvp.Key).ToList();
        List<Powerups> chosen = new List<Powerups>();
        for(int i = 0; i < 2; i++)
        {
            int index = UnityEngine.Random.Range(0, types.Count);
            chosen.Add(types[index]);
            types.RemoveAt(index);
        }
        upgradeSlot1 = chosen[0];
        upgradeButton1.SetSprite(powerupsSpriteDict[(Powerups)upgradeSlot1]);
        
        upgradeSlot2 = chosen[1];
        upgradeButton2.SetSprite(powerupsSpriteDict[(Powerups)upgradeSlot2]);

        // The 3rd power offered is always a heal
        upgradeSlot3 = Powerups.Heal;
        upgradeButton3.SetSprite(healSprite);
    }

    public void Enable(bool hasUpgradeChoice = false)
    {
        this.hasUpgradeChoice = hasUpgradeChoice;
        rectTransform.sizeDelta = hasUpgradeChoice ? new Vector2(rectTransform.sizeDelta.x, 300) : new Vector2(rectTransform.sizeDelta.x, 155);
        powerupPanel.SetActive(hasUpgradeChoice);
        continueButton.interactable = !hasUpgradeChoice;
        selectedButton = null;
        paused = true;
        player.InputInterrupt();
        Time.timeScale = 0;
        fader.FadeIn();
    }
    public void Disable()
    {
        if(hasUpgradeChoice.Value)
        {
            Enum toSet = selectedButton.Value switch{
                0 => upgradeSlot1,
                1 => upgradeSlot2,
                2 => upgradeSlot3,
                _ => null
            };

            if(toSet is WeaponType weapon)
                player.weapon = weapon;
            else if(toSet is Powerups powerup)
            {
                if(powerup == Powerups.Heal)
                    player.HealToFull();
                else
                    player.AddPowerup(powerup);
            }
            foreach(PowerupButton powerupButton in PowerupButtons())
            {
                if(powerupButton.buttonID == selectedButton)
                    powerupButton.Deselect();
            }
        }
        hasUpgradeChoice = null;
        selectedButton = null;
        paused = false;
        Time.timeScale = 1;
        fader.FadeOut();
    }
    public void Toggle(CallbackContext context)
    {
        if(!context.performed || !continueButton.interactable)
            return;

        if(paused)
            Disable();
        else
            Enable();
    }

    public void SetPowerupSelection(int ID)
    {
        selectedButton = ID;
        foreach(PowerupButton powerupButton in PowerupButtons())
        {
            if(powerupButton.buttonID == selectedButton)
                continue;
            
            powerupButton.Deselect();
        }
        if(!continueButton.interactable)
            continueButton.interactable = true;
    }
    
    public void SetContinue()
    {
        if(!continueButton.interactable)
            return;

        if(!swappedToContinue)
            StartCoroutine(SetContinueAfterDelay());
    }

    IEnumerator SetContinueAfterDelay()
    {
        yield return new WaitForSeconds(0.25f);
        swappedToContinue = true;
        buttonText.text = "Continue";
    }
}