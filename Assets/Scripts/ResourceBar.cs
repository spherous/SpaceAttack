using System;
using System.Collections;
using System.Collections.Generic;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceBar : MonoBehaviour
{
    [SerializeField] private Image redBar;
    [SerializeField] private Image blueBar;
    [SerializeField] private TextMeshProUGUI spacebarText;
    Player player;

    bool redFlashing = false;
    bool blueFlashing = false;

    public float spacebarFlashSpeed;

    private void Awake() {
        player = GameObject.FindObjectOfType<Player>();
        player.onResourceChanged += UpdateBars;
        redBar.fillAmount = 0;
        blueBar.fillAmount = 0;
        redBar.material.SetFloat("_Flashing", 0);
        blueBar.material.SetFloat("_Flashing", 0);
        spacebarText.gameObject.SetActive(false);
    }
    
    private void Update()
    {
        if(spacebarText.gameObject.activeSelf)
            spacebarText.transform.localScale = Vector3.one * Mathf.Sin(Time.timeSinceLevelLoad * spacebarFlashSpeed).Remap(-1, 1, .8f, 1.2f);
    }

    private void UpdateBars(float val)
    {
        if(val < 0f)
        {
            redBar.fillAmount = Mathf.Clamp01(-val);
            blueBar.fillAmount = 0;
            if(val < -1 && !redFlashing)
            {
                redBar.material.SetFloat("_Flashing", 1);
                redFlashing = true;
                spacebarText.gameObject.SetActive(true);
            }
            else if(val > -1 && redFlashing)
            {
                redBar.material.SetFloat("_Flashing", 0);
                redFlashing = false;
                spacebarText.gameObject.SetActive(false);
                spacebarText.transform.localScale = Vector3.one;
            }
        }
        else if(val > 0f)
        {
            blueBar.fillAmount = Mathf.Clamp01(val);
            redBar.fillAmount = 0;
            if(val > 1 && !blueFlashing)
            {
                blueBar.material.SetFloat("_Flashing", 1);
                blueFlashing = true;
                spacebarText.gameObject.SetActive(true);
            }
            else if(val < 1 && blueFlashing)
            {
                blueBar.material.SetFloat("_Flashing", 0);
                blueFlashing = false;
                spacebarText.gameObject.SetActive(false);
                spacebarText.transform.localScale = Vector3.one;
            }
        }
        else
        {
            blueBar.fillAmount = 0;
            redBar.fillAmount = 0;
        }

        if(val < 1 && val > -1)
        {
            if(spacebarText.gameObject.activeSelf)
            {
                spacebarText.gameObject.SetActive(false);
                spacebarText.transform.localScale = Vector3.one;
            }

            if(blueFlashing)
            {
                blueBar.material.SetFloat("_Flashing", 0);
                blueFlashing = false;
            }
            if(redFlashing)
            {
                redBar.material.SetFloat("_Flashing", 0);
                redFlashing = false;
            }
        }
    }
}