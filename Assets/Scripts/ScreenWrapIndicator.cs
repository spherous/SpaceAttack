using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Camera;

public class ScreenWrapIndicator : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private Player player;

    [SerializeField] private GroupFader top;
    [SerializeField] private GroupFader bottom;
    [SerializeField] private GroupFader left;
    [SerializeField] private GroupFader right;
    [SerializeField] private Image topBar;
    [SerializeField] private Image topIcon;
    [SerializeField] private Image bottomBar;
    [SerializeField] private Image bottomIcon;
    [SerializeField] private Image leftBar;
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image rightBar;
    [SerializeField] private Image rightIcon;

    private GroupFader activeFader;
    public float edgeRange;

    public bool topLocked = false;
    public bool rightLocked = false;
    public bool bottomLocked = false;
    public bool leftLocked = false;

    public Color unlockedColor;
    public Color lockedColor;
    public Color unlockedIconColor;
    public Color lockedIconColor;

    private void Update()
    {
        Vector3 playerScreenPos = cam.WorldToScreenPoint(player.transform.position, MonoOrStereoscopicEye.Mono);
        GroupFader newFader = null;

        if(playerScreenPos.y <= edgeRange && player.canScreenWrap && !bottomLocked)
        {
            if(activeFader != bottom)
                newFader = bottom;
        }
        else if((Screen.height - playerScreenPos.y) <= edgeRange && player.canScreenWrap && !topLocked)
        {
            if(activeFader != top)
                newFader = top;
        }
        else if(playerScreenPos.x <= edgeRange && player.canScreenWrap && !leftLocked)
        {
            if(activeFader != left)
                newFader = left;
        }
        else if((Screen.width - playerScreenPos.x) <= edgeRange && player.canScreenWrap && !rightLocked)
        {
            if(activeFader != right)
                newFader = right;
        }        
        else
        {
            activeFader?.FadeOut();
            activeFader = null;
            newFader = null;
        }

        if(newFader != null)
        {
            activeFader?.FadeOut();
            newFader.FadeIn();
            activeFader = newFader;
        }
    }

    public void LockSide(int sideID, bool fadeIn = true)
    {
        switch(sideID)
        {
            case 0:
                Debug.Log("top locked");
                topLocked = true;
                Lock(topBar, topIcon);
                if(fadeIn)
                    top.FadeIn();
                break;
            case 1:
                Debug.Log("right locked");
                rightLocked = true;
                Lock(rightBar, rightIcon);
                if(fadeIn)
                    right.FadeIn();
                break;
            case 2:
                Debug.Log("bottom locked");
                bottomLocked = true;
                Lock(bottomBar, bottomIcon);
                if(fadeIn)
                    bottom.FadeIn();
                break;
            case 3:
                Debug.Log("left locked");
                leftLocked = true;
                Lock(leftBar, leftIcon);
                if(fadeIn)
                    left.FadeIn();
                break;
            default:
                Debug.Log($"none locked invalid sideID: {sideID}");
                return;
        }
    }
    public void ReleaseAllLocks()
    {
        if(topLocked)
        {
            topLocked = false;
            Unlock(topBar, topIcon);
            if(top.visible)
                top.FadeOut();
        }
        if(rightLocked)
        {
            rightLocked = false;
            Unlock(rightBar, rightIcon);
            if(right.visible)
                right.FadeOut();
        }
        if(bottomLocked)
        {
            bottomLocked = false;
            Unlock(bottomBar, bottomIcon);
            if(bottom.visible)
                bottom.FadeOut();
        }
        if(leftLocked)
        {
            leftLocked = false;
            Unlock(leftBar, leftIcon);
            if(left.visible)
                left.FadeOut();
        }
    }

    private void Lock(Image bar, Image icon)
    {
        bar.color = lockedColor;
        icon.color = lockedIconColor;
    }
    private void Unlock(Image bar, Image icon)
    {
        bar.color = unlockedColor;
        icon.color = unlockedIconColor;
    }
}