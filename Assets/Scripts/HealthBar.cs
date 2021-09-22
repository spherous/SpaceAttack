using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private GameObject objectToMonitor;
    [SerializeField] private SlicedFilledImage bar;
    public IHealth healthToMonitor {get; private set;}
    public Vector2 offset;

    public bool surpressFollow;

    private void Awake() {
        cam = Camera.main;
        if(objectToMonitor != null)
            healthToMonitor = objectToMonitor.GetComponent<IHealth>();
        if(healthToMonitor != null)
            healthToMonitor.onHealthChanged += UpdateHealthBar;
    }

    public void SetTarget(GameObject target, IHealth targetsHealth)
    {
        if(healthToMonitor != null)
            healthToMonitor.onHealthChanged -= UpdateHealthBar;

        objectToMonitor = target;
        healthToMonitor = targetsHealth;
        healthToMonitor.onHealthChanged += UpdateHealthBar;
    }

    private void UpdateHealthBar(IHealth changed, float newHP)
    {
        if(healthToMonitor.maxHealth == 0)
            return;

        bar.fillAmount = Mathf.Clamp01(newHP/healthToMonitor.maxHealth);
        if(healthToMonitor is Player && bar.fillAmount == 0)
            gameObject.SetActive(false);
    }

    private void Update()
    {
        if(objectToMonitor == null || healthToMonitor == null)
            return;
        
        if(!surpressFollow)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(objectToMonitor.transform.position);
            ((RectTransform)transform).position = screenPos + (Vector3)offset;
        }
    }
}
