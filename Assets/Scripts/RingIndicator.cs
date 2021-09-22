using System.Collections;
using UnityEngine;

public class RingIndicator : MonoBehaviour
{
    public Transform targetToFollow;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer timerRing;
    [SerializeField] private Collider2D col;
    public bool showOnAwake = false;
    public Color redColor;
    public Color blueColor;
    public RingMode mode {get; private set;}
    public float pow {get; private set;}
    // float? disableAtTime;
    private void Awake()
    {
        meshRenderer.enabled = showOnAwake;
        col.enabled = showOnAwake;
        timerRing.enabled = showOnAwake;
    }
    private void Update()
    {
        transform.position = targetToFollow.transform.position;
        // if(timerRing.enabled && disableAtTime.HasValue)
        // {
        //     disableAtTime.Value
        // }
    } 
    public void SetColor(Color color) => meshRenderer.material.SetColor("_BaseColor", color);
    public void SetMode(RingMode mode, float pow = 0)
    {
        this.mode = mode;
        this.pow = pow;
        Color color = mode switch{
            RingMode.Speed => blueColor,
            RingMode.Power => redColor,
            _ => Color.black
        };
        SetColor(color);
    }
    public void Show(float duration)
    {
        meshRenderer.enabled = true;
        timerRing.enabled = true;
        col.enabled = true;
        StopCoroutine("TickTimer");
        StartCoroutine("TickTimer", duration);
        // disableAtTime = duration + Time.timeSinceLevelLoad;
    }
    public void Hide()
    {
        meshRenderer.enabled = false;
        timerRing.enabled = false;
        col.enabled = false;
        StopCoroutine("TickTimer");
        // disableAtTime = null;
    }

    IEnumerator TickTimer(float duration)
    {
        float startTime = Time.timeSinceLevelLoad;
        float ellapsed = 0;
        float val = 0;
        while(ellapsed < duration)
        {
            ellapsed += Time.deltaTime;
            val = Mathf.Lerp(1,0, Mathf.Clamp01(ellapsed/duration));
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetFloat("_FilledAmount", val);
            timerRing.SetPropertyBlock(block);
            yield return 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(mode == RingMode.None)
            return;

        float factor = 1 - pow;
        
        if(other.gameObject.TryGetComponent<Enemy>(out Enemy enemy))
        {
            if(mode == RingMode.Speed)
                enemy.speed = enemy.defaultSpeed * factor;
            else if(mode == RingMode.Power)
            {
                enemy.damage = enemy.defaultDamage * factor;
                enemy.attackDelay = enemy.attackDelay + enemy.attackDelay * (pow * 2);
            }
        }
        else if(other.gameObject.TryGetComponent<Rock>(out Rock rock))
        {
            if(mode == RingMode.Speed)
                rock.SlowDown(factor);
            else if(mode == RingMode.Power)
                rock.damageMod = factor;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.gameObject.TryGetComponent<Enemy>(out Enemy enemy))        
        {
            enemy.speed = enemy.defaultSpeed;
            enemy.damage = enemy.defaultDamage;
            enemy.attackDelay = enemy.defaultAttackDelay;
        }
        else if(other.gameObject.TryGetComponent<Rock>(out Rock rock))
        {
            rock.RestoreSpeed();
            rock.damageMod = 1;
        }
    }
}
public enum RingMode {None = 0, Speed = 1, Power = 2}