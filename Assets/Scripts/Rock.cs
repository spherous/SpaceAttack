using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Camera;
using Extensions;

public class Rock : MonoBehaviour, IHealth, IPoolable
{
    Camera cam;
    public Rigidbody2D body {get => _body; set{}}
    [SerializeField] private Rigidbody2D _body;
    public Collider2D col {get => _col; set{}}
    [SerializeField] private Collider2D _col;
    public float maxSpeed;
    public int size {get; private set;}
    public float maxHealth {get => 1; set{}}
    public float currentHealth {get => _hp; set{_hp = value;}}
    float _hp;
    public bool isDead {get => _isDead; set{}}
    bool _isDead;
    public bool isDying {get => _isDying; set{}}
    bool _isDying;
    public bool inPool {get => _inPool; set{_inPool = value;}}

    public GameObject obj {get => gameObject; set{}}

    bool _inPool = false;

    public event OnReturnToPool onReturnToPool;
    public event OnHealthChanged onHealthChanged;

    private RockSpawner spawner;
    private ParticleSystemPooler dustPooler;

    public float sizeMod = 1.5f;
    public float lifetime;
    float dieAtTime;
    
    Score score;

    public bool surpressSmallerRocks = false;

    RockPlayer audioPlayer;

    Vector3 cachedVelocity;
    float cachedAngularVelocity;

    public float damageMod = 1f;
    private bool slowed = false;
    private BlitzClock blitzClock;

    private void Awake() {
        cam = Camera.main;
        score = GameObject.FindObjectOfType<Score>();
        audioPlayer = GameObject.FindObjectOfType<RockPlayer>();
        blitzClock = GameObject.FindObjectOfType<BlitzClock>();
    }

    private void Update() {
        Vector3 screenLoc = cam.WorldToScreenPoint(transform.position, MonoOrStereoscopicEye.Mono);
        if(Time.timeSinceLevelLoad >= dieAtTime && screenLoc.IsOffScreen())
        {
            _hp = 0;
            onHealthChanged?.Invoke(this, currentHealth);
            onReturnToPool?.Invoke();
        }
    }

    public void SetDustPooler(ParticleSystemPooler dustPooler) => this.dustPooler = dustPooler;

    public void Init(int size, RockSpawner spawner)
    {
        dieAtTime = Time.timeSinceLevelLoad + lifetime;

        _isDead = false;
        _isDying = false;
        this.spawner = spawner;
        this.size = size;
        float scale = size / sizeMod;
        // transform.localScale = new Vector3(scale, scale, scale);
        _hp = maxHealth;
        onHealthChanged?.Invoke(this, currentHealth);
        
        // This resets the ignored collisions
        col.enabled = false;
        col.enabled = true;
        surpressSmallerRocks = false;
    }

    public void Die()
    {
        PooledParticleSystem dust = dustPooler.pool.Get();
        dust.transform.position = transform.position;
        dust.transform.localScale = Vector3.one * ((float)size + 1)/5f;
        dust.Play();

        audioPlayer?.Play(size);

        if(size > 0 && !surpressSmallerRocks)
        {
            int nextSize = size - 1;
            int amountOfNewRocks = UnityEngine.Random.Range(2, 5);
            List<Rock> newRocks = new List<Rock>();

            for(int i = 0; i < amountOfNewRocks; i++)
            {
                // Rock newRock = spawner.pooler.pool.Get();
                Rock newRock = spawner.poolers[nextSize].pool.Get();
                newRock.Init(nextSize, spawner);
                newRocks.Add(newRock);
                newRock.gameObject.SetActive(true);

                Vector3 posOffset = new Vector3(
                    x: UnityEngine.Random.Range(-sizeMod / 2, sizeMod / 2), 
                    y: UnityEngine.Random.Range(-sizeMod / 2, sizeMod / 2), 
                    z: 10
                );
                Quaternion rot = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-180f, 180f));

                int attempts = 0;
                while(Physics2D.OverlapCircleAll(transform.position + posOffset, size / sizeMod).Length > 0 && attempts < 20)
                {
                    // This will attempt to spread out the spawned rocks, but it may be impossible depending on where the first couple are spawned, so we limit the amount of attempts
                    posOffset = new Vector3(
                        x: UnityEngine.Random.Range(-sizeMod / 2, sizeMod / 2), 
                        y: UnityEngine.Random.Range(-sizeMod / 2, sizeMod / 2), 
                        z: 10
                    );
                    rot = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-180f, 180f));
                    attempts++;
                }

                newRock.transform.SetPositionAndRotation(transform.position + posOffset, rot);
                newRock.body.angularVelocity = UnityEngine.Random.Range(-120f, 120f);
                newRock.body.velocity = body.velocity + ((Vector2)(newRock.transform.position - transform.position).normalized * UnityEngine.Random.Range(0.33f, maxSpeed));
            }

            List<(Rock, Rock)> pairs = new List<(Rock, Rock)>();

            // Ignore colliding with the other freshly spawned rocks
            foreach(Rock rock in newRocks)
            {
                foreach(Rock r in newRocks)
                {
                    if(r == rock)
                        continue;
                    
                    if(pairs == null || rock == null || r == null)
                    {
                        Debug.LogWarning("Something was null");
                        continue;
                    }
                    if(pairs.Contains((rock, r)) || pairs.Contains((r, rock)))
                        continue;
                    
                    pairs.Add((rock, r));
                    Physics2D.IgnoreCollision(rock.col, r.col);
                }
            }
        }
        onReturnToPool?.Invoke();
    }

    public void TakeDamage(float amount, Transform damagedBy)
    {
        if(this == null)
            return;
        
        if(damagedBy != null && damagedBy.gameObject.name == "Player")
        {
            blitzClock.AddSeconds(0.05f * (size + 1));
            score.Add(1 * (size + 1));
        }
        Break();
        onHealthChanged?.Invoke(this, 0);
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if(other.collider == null)
            return;
        
        if(other.collider.TryGetComponent<IHealth>(out IHealth otherHealth))
        {
            // Rocks running into the player still count towards the score, even though they deal damage
            if(other.collider.gameObject.name == "Player")
            {
                blitzClock.AddSeconds(0.05f * (size + 1));
                score.Add(1 * (size + 1));
            }

            otherHealth.TakeDamage((size + 0.5f) * damageMod, transform);
            Break();
            onHealthChanged?.Invoke(this, 0);
        }
    }

    public void Break() => StartCoroutine(DieAtEndOfFrame());

    IEnumerator DieAtEndOfFrame()
    {
        if(isDying || _isDead)
            yield break;

        isDying = true;
        yield return new WaitForEndOfFrame();
        Die();
    }

    public void HealToFull()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(this, currentHealth);
    }

    public void SlowDown(float factor)
    {
        slowed = true;
        cachedVelocity = body.velocity;
        cachedAngularVelocity = body.angularVelocity;
        body.velocity = cachedVelocity * factor;
        body.angularVelocity = cachedAngularVelocity * factor;
    }
    public void RestoreSpeed()
    {
        if(!slowed)
            return;
        slowed = false;
        body.velocity = cachedVelocity;
        body.angularVelocity = cachedAngularVelocity;
    }
}
