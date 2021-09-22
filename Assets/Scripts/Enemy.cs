using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using static UnityEngine.Camera;

public class Enemy : MonoBehaviour, IHealth, IPoolable
{
    Camera cam;
    public GameObject obj {get => gameObject; set{}}
    [SerializeField] protected Collider2D col;
    public bool inPool {get => _inPool; set => _inPool = value;}
    protected bool _inPool = false;
    public event OnReturnToPool onReturnToPool;

    public float maxHealth {get => _maxHP; set{}}
    [SerializeField] protected float _maxHP;
    [ReadOnly, ShowInInspector] public float currentHealth {get => _hp; set{_hp = value;}}
    protected float _hp;
    public bool isDead {get => _isDead; set => _isDead = value;}
    protected bool _isDead;
    public bool isDying {get => _isDying; set => _isDying = value;}
    protected bool _isDying;

    public event OnHealthChanged onHealthChanged;

    public float attackingDistance;
    public float movementFarDistance;
    public float movementCloseDistance;

    public float attackDelay;
    public float defaultAttackDelay;
    protected float attackAtTime;

    protected Player player;
    public ContactFilter2D colliderCastFilter;

    public float damage;
    public float defaultDamage {get; protected set;}
    public float rotationSpeed;
    public float speed;
    public bool careAboutFacingPlayer;

    protected BlitzClock blitzClock;
    protected Score score;
    public float defaultSpeed {get; protected set;}
    public AudioClip attackClip;
    [SerializeField] protected AudioSource source;
    bool enteredScreen = false;
    public ParticleSystemPooler explosionPooler;
    public ParticleSystemPooler sparkPooler;
    [SerializeField] protected AudioSource damagedSource;
    public List<AudioClip> damagedClips;
    private float lastAudioPlayedAt;
    ScreenWrapIndicator screenWrapIndicator;
    protected virtual void Awake()
    {
        cam = Camera.main;
        player = GameObject.FindObjectOfType<Player>();
        blitzClock = GameObject.FindObjectOfType<BlitzClock>();
        score = GameObject.FindObjectOfType<Score>();
        screenWrapIndicator = GameObject.FindObjectOfType<ScreenWrapIndicator>();
        currentHealth = maxHealth;
        defaultSpeed = speed;
        defaultDamage = damage;
        defaultAttackDelay = attackDelay;
    }

    protected virtual void Update()
    {
        if(player == null || !player.gameObject.activeSelf)
            return;

        Vector3 playerPositionInLocalSpace = player.transform.position - transform.position;

        if(playerPositionInLocalSpace.magnitude <= attackingDistance && Time.timeSinceLevelLoad >= attackAtTime)
            Attack(playerPositionInLocalSpace.normalized);
    }

    protected void FixedUpdate()
    {
        if(isDying || isDead)
            return;

        if(player != null && player.gameObject.activeSelf)
        {
            Move();
            ScreenWrap();
        } 
    }

    private void ScreenWrap()
    {
        if(!enteredScreen)
            return;
        
        Vector3 screenPos = cam.WorldToScreenPoint(transform.position, MonoOrStereoscopicEye.Mono);
        Vector3 wrappedPos = screenPos.GetScreenWrapPosition(screenWrapIndicator);
        if(screenPos == wrappedPos)
            return;

        transform.position = cam.ScreenToWorldPoint(wrappedPos, MonoOrStereoscopicEye.Mono);
    }

    protected virtual void Move()
    {
        Vector3 playerPositionInLocalSpace = player.transform.position - transform.position;
        Vector3 direction = playerPositionInLocalSpace.normalized;
        float playerDistance = playerPositionInLocalSpace.magnitude;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(transform.forward, direction), rotationSpeed * Time.deltaTime);

        // too close, move away
        Vector3 targetPos;
        if(playerDistance <= movementCloseDistance)
            targetPos = transform.position - (direction * speed * Time.fixedDeltaTime);
        // within far distance and close distance, do nothing
        else if(playerPositionInLocalSpace.magnitude <= movementFarDistance)
            return;
        // too far, move closer
        else
            targetPos = transform.position + (direction * speed * Time.fixedDeltaTime);

        Vector3 screenPos = cam.WorldToScreenPoint(targetPos);

        if(screenPos.x < 0 && screenWrapIndicator.leftLocked && direction.Dot(Vector3.right) <= 0)
            return;
        else if(screenPos.x > Screen.width && screenWrapIndicator.rightLocked && direction.Dot(Vector3.left) >= 0)
            return;
        else if(screenPos.y < 0 && screenWrapIndicator.topLocked && direction.Dot(Vector3.up) <= 0)
            return;
        else if(screenPos.y > Screen.height && screenWrapIndicator.bottomLocked && direction.Dot(Vector3.down) <= 0)
            return;

        transform.position = targetPos;
        
        if(!enteredScreen && !cam.WorldToScreenPoint(transform.position).IsOffScreen())
            enteredScreen = true;
    }

    protected virtual bool Attack(Vector3 normalizedDirection)
    {
        attackAtTime = Time.timeSinceLevelLoad + attackDelay;
        
        if(player.isDead || player.isDying)
            return false;

        // Enemies shouldn't be able to attack if they are off the screen
        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
        if(screenPos.IsOffScreen())
            return false;
        
        // Enemy must be roughly facing the player to attack
        if(careAboutFacingPlayer && Vector3.Dot(transform.up, normalizedDirection) <= 0.75f)
            return false;
        
        // Enemies should not attack other enemies
        List<RaycastHit2D> results = new List<RaycastHit2D>();
        int amountHit = col.Cast(normalizedDirection, colliderCastFilter, results, 50f);
        if(amountHit > 0)
        {
            RaycastHit2D firstHit = results.First();
            if(firstHit.collider.TryGetComponent<Enemy>(out Enemy otherEnemy))
                return false;
        }

        source.PlayOneShot(attackClip);
        return true;
    }

    public virtual void Die()
    {
        // on death vfx/audio
        PooledParticleSystem explosion = explosionPooler.pool.Get();
        explosion.transform.position = transform.position;
        explosion.SetSize(1);
        explosion.Play();

        Debug.Log($"{name} has died");
        _isDead = true;
        onReturnToPool?.Invoke();
        enteredScreen = false;
    }

    public virtual void HealToFull()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(this, currentHealth);
    }

    public virtual void TakeDamage(float amount, Transform damgedBy)
    {
        if(isDying || isDead)
            return;
        
        amount = Mathf.Abs(amount);

        float oldHP = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        if(currentHealth != oldHP)
        {
            // play damaged audio/vfx
            AudioClip chosenClip = damagedClips.ChooseRandom();
            if(Time.timeSinceLevelLoad >= lastAudioPlayedAt + chosenClip.length)
            {
                damagedSource.PlayOneShot(chosenClip);
                lastAudioPlayedAt = Time.timeSinceLevelLoad;
            }

            PooledParticleSystem sparks = sparkPooler.pool.Get();
            sparks.transform.position = transform.position;
            sparks.Play();
            onHealthChanged?.Invoke(this, currentHealth);
        }

        if(currentHealth == 0)
        {
            // increase player's score
            score.Add(2);
            blitzClock.AddSeconds(0.75f);
            Die();
        }
    }
}
