using System.Collections;
using System.Collections.Generic;
using Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class Turret : MonoBehaviour, IHealth
{
    Player player;
    public Boss boss;
    private Score score;
    private BlitzClock blitzClock;
    public Transform firePoint;
    public LaserPooler laserPooler;
    public ParticleSystemPooler sparkPooler;
    public ParticleSystemPooler explosionPooler;
    public float fireDelay;
    private float fireAtTime;

    public event OnHealthChanged onHealthChanged;

    public GameObject obj {get => gameObject; set{}}
    public float maxHealth {get => _maxHP; set{}}
    [SerializeField] protected float _maxHP;
    [ReadOnly, ShowInInspector] public float currentHealth {get => _hp; set{_hp = value;}}
    protected float _hp;
    public bool isDead {get => _isDead; set => _isDead = value;}
    protected bool _isDead;
    public bool isDying {get => _isDying; set => _isDying = value;}
    protected bool _isDying;

    [SerializeField] protected AudioSource damagedSource;
    public List<AudioClip> damagedClips;
    private float lastAudioPlayedAt;

    private void Awake() {
        player = GameObject.FindObjectOfType<Player>();
        score = GameObject.FindObjectOfType<Score>();
        blitzClock = GameObject.FindObjectOfType<BlitzClock>();
        HealToFull();
    }
    private void Update() {
        transform.rotation = Quaternion.LookRotation(transform.forward, (player.transform.position - transform.position).normalized);

        if(Time.timeSinceLevelLoad >= fireAtTime)
        {
            if(boss.onSide)
            {
                // shoot at player
                LaserProjectile laser = laserPooler.pool.Get();
                laser.transform.position = firePoint.position;
                laser.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0,0, UnityEngine.Random.Range(-15f, 15f)));
                laser.Fire(transform, boss.turretDamage, false);
            }
            fireAtTime = Time.timeSinceLevelLoad + fireDelay;
        }
    }

    public void HealToFull()
    {
        float oldHP = currentHealth;
        currentHealth = maxHealth;
        if(oldHP != currentHealth)
            onHealthChanged?.Invoke(this, currentHealth);
    }

    public void TakeDamage(float amount, Transform damgedBy)
    {
        if(isDead || isDying)
            return;
        
        amount = Mathf.Abs(amount);

        float oldHP = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        if(currentHealth != oldHP)
        {
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
            // deal damage to boss
            Die();
        }
    }

    public void Die()
    {
        PooledParticleSystem explosion = explosionPooler.pool.Get();
        explosionPooler.transform.position = transform.position;
        explosion.SetSize(0.6f);
        explosion.Play();

        score.Add(100);
        blitzClock.AddSeconds(10);

        boss.TakeDamage(boss.maxHealth * .1f, player.transform);
        
        gameObject.SetActive(false);
    }
}
