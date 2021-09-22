using System.Collections;
using System.Collections.Generic;
using Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class LaserDrone : MonoBehaviour, IHealth
{
    Camera cam;
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
    [SerializeField] private LaserBeam laserBeam;
    public enum Routine {None = 0, LeftToRight = 1, RighToLeft = 2, TopToBottom = 3, BottomToTop = 4}
    private int sideID;
    private Routine routine;
    public float speed;
    public float beamLength;
    public Transform firstPosition;
    public Transform secondPosition;


    private void Awake() {
        cam = Camera.main;
        score = GameObject.FindObjectOfType<Score>();
        blitzClock = GameObject.FindObjectOfType<BlitzClock>();
        player = GameObject.FindObjectOfType<Player>();
        HealToFull();
    }
    private void Start() {
        laserBeam.Fade();
    }

    public void Release(int sideID, Routine routine)
    {
        this.sideID = sideID;
        this.routine = routine;

        StartCoroutine(ExecuteRoutine());

    }

    IEnumerator ExecuteRoutine()
    {
        Vector3 startPosition = transform.position;

        yield return StartCoroutine(GoToPos(firstPosition.position, speed / 2.5f));
        yield return new WaitForSeconds(2f);
        laserBeam.Fire(transform, laserBeam.transform, 8f, beamLength);
        // execute routine
        yield return StartCoroutine(GoToPos(secondPosition.position, speed));
        laserBeam.Fade();
        // return home
        yield return StartCoroutine(GoToPos(startPosition, speed / 2.5f));

    }

    IEnumerator GoToPos(Vector3 pos, float spd)
    {
        Debug.Log("Going...");
        Vector3 start = transform.position;
        float totalDist = (pos - start).magnitude;
        while(transform.position != pos)
        {
            yield return 0;
            float currentDist = (start - transform.position).magnitude;

            transform.position = Vector3.Lerp(start, pos, Mathf.Clamp01((currentDist + (spd * Time.deltaTime)) / totalDist));
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