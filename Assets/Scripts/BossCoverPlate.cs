using System.Collections;
using System.Collections.Generic;
using Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class BossCoverPlate : MonoBehaviour, IHealth
{
    public Boss boss;
    public ParticleSystemPooler sparkPooler;
    public ParticleSystemPooler explosionPooler;
    private Score score;
    private BlitzClock blitzClock;
    public GameObject obj {get => gameObject; set{}}
    public float maxHealth {get => _maxHP; set{}}
    [SerializeField] protected float _maxHP;
    [ReadOnly, ShowInInspector] public float currentHealth {get => _hp; set{_hp = value;}}
    protected float _hp;
    public bool isDead {get => _isDead; set => _isDead = value;}
    protected bool _isDead;
    public bool isDying {get => _isDying; set => _isDying = value;}
    protected bool _isDying;

    public event OnHealthChanged onHealthChanged;
    [SerializeField] protected AudioSource damagedSource;
    public List<AudioClip> damagedClips;
    private float lastAudioPlayedAt;
    Player player;
    [SerializeField] private BossWeakSpot weakSpot;
    private void Awake() {
        score = GameObject.FindObjectOfType<Score>();
        blitzClock = GameObject.FindObjectOfType<BlitzClock>();
        player = GameObject.FindObjectOfType<Player>();
        HealToFull();
    }
    private void Start() {
        weakSpot.gameObject.SetActive(false);
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

        weakSpot.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    public void HealToFull() => currentHealth = maxHealth;

    public void TakeDamage(float amount, Transform damgedBy)
    {
        if(isDead || isDying)
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
            Die();
    }
}