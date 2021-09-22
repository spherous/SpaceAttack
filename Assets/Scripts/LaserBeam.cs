using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeam : MonoBehaviour, IPoolable
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private BoxCollider2D col;
    [SerializeField] private ParticleSystem system;
    public bool inPool {get => _inPool; set{_inPool = value;}}
    bool _inPool = false;
    public event OnReturnToPool onReturnToPool;
    private Transform owner;
    public float damage;
    Transform firePoint;
    private float defaultWidthMulti;
    public float damageRate;
    public float defaultDamageRate {get; private set;}
    Dictionary<IHealth, float> damagedUnits = new Dictionary<IHealth, float>();
    private void Awake()
    {
        defaultDamageRate = damageRate;
        defaultWidthMulti = lineRenderer.widthMultiplier;
    }

    public void Fire(Transform owner, Transform firePoint, float damage, float length = 1, float thicknessMultiplier = 1f)
    {
        this.owner = owner;
        this.damage = damage;
        this.firePoint = firePoint;
        lineRenderer.enabled = true;
        SetLength(length);
        SetWidth(thicknessMultiplier);
        system.Play();
    }

    public void SetLength(float length)
    {
        float posInc = length / (lineRenderer.positionCount - 1);
        for(int i = 0; i < lineRenderer.positionCount; i++)
            lineRenderer.SetPosition(i, Vector3.up * i * posInc);
        
        col.size = new Vector2(col.size.x, length);
        col.offset = new Vector2(col.offset.x, length / 2f);
    }
    public void SetWidth(float multiplier)
    {
        float width = defaultWidthMulti * multiplier;
        col.size = new Vector2(width, col.size.y);
        lineRenderer.widthMultiplier = width;
    }
    public void ResetWidth()
    {
        lineRenderer.widthMultiplier = defaultWidthMulti;
        col.size = new Vector2(defaultWidthMulti, col.size.y);
    } 

    public void Fade()
    {
        system.Stop();
        damageRate = defaultDamageRate;
        damagedUnits.Clear();
        owner = null;
        damage = 0;
        firePoint = null;
        lineRenderer.enabled = false;
        onReturnToPool?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.transform == owner)
            return;
        
        if(other.gameObject.TryGetComponent<IHealth>(out IHealth hitHealth))
        {
            if(!damagedUnits.ContainsKey(hitHealth))
            {
                hitHealth.TakeDamage(damage, owner);
                damagedUnits.Add(hitHealth, Time.timeSinceLevelLoad);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.transform == owner)
            return;

        if(other.TryGetComponent<IHealth>(out IHealth extingHealth))
        {
            if(damagedUnits.ContainsKey(extingHealth))
                damagedUnits.Remove(extingHealth);
        }
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if(other.transform == owner)
            return;
        
        if(other.TryGetComponent<IHealth>(out IHealth hitHealth))
        {
            if(damagedUnits.ContainsKey(hitHealth))
            {
                float damagedAtTime = damagedUnits[hitHealth];
                if(Time.timeSinceLevelLoad >= damagedAtTime + damageRate)
                {
                    hitHealth.TakeDamage(damage, owner);
                    if(hitHealth.currentHealth <= 0)
                        damagedUnits.Remove(hitHealth);
                }
            }
            else
                damagedUnits.Add(hitHealth, Time.timeSinceLevelLoad);
        }
    }
}