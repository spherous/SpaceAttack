using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Rocket : MonoBehaviour, IPoolable, IProjectile
{
    [SerializeField] private BoxCollider2D col;
    [SerializeField] private AudioSource fireSource;
    public bool inPool {get => _inPool; set => _inPool = value;}
    private float dieAtTime;
    public float lifetime {get => _lifetime; set{_lifetime = value;}}
    [SerializeField] private float _lifetime;
    public float speed {get => _speed; set{_speed = value;}}
    private float _speed;
    public float defaultSpeed {get => _defaultSpeed; set => _defaultSpeed = value;}
    [SerializeField] private float _defaultSpeed;
    public Rigidbody2D body {get => _body; set{}}
    [SerializeField] private Rigidbody2D _body;

    private bool _inPool = false;
    public event OnReturnToPool onReturnToPool;

    private Transform owner;
    float damage;

    public float radius;

    ParticleSystemPooler explosionPooler;
    Vector2 defaultColliderSize;

    [SerializeField] private LineRenderer trail;
    float defaultTrailWidthMod;
    // [SerializeField] private ParticleSystem enginePlumeSystem;
    // float defaultPlumeSizeMult;
    // [SerializeField] private ParticleSystem enginePlumeBulbSystem;
    // float defaultBulbSizeMult;

    float size = 1;

    private void Awake() {
        speed = defaultSpeed;
        defaultColliderSize = col.size;
        defaultTrailWidthMod = trail.widthMultiplier;
        // defaultPlumeSizeMult = enginePlumeSystem.main.startSizeMultiplier;
        // defaultBulbSizeMult = enginePlumeBulbSystem.main.startSizeMultiplier;
    }

    private void Update()
    {
        if(Time.timeSinceLevelLoad >= dieAtTime)
            onReturnToPool?.Invoke();
    }

    public void SetExplosionPooler(ParticleSystemPooler pooler)
    {
        explosionPooler = pooler;
    }

    public void Fire(Transform owner, float damage, bool isPlayer = false)
    {
        this.owner = owner;
        this.damage = damage;
        dieAtTime = Time.timeSinceLevelLoad + lifetime;
        body.velocity = transform.up * speed;
    }

    public void Collide()
    {
        PooledParticleSystem explosion = explosionPooler.pool.Get();
        explosion.transform.position = transform.position;
        Vector3 scale = Vector3.one * radius * size;
        explosion.transform.localScale = scale;
        for(int i = 0; i < explosion.transform.childCount; i++)
        {
            Transform child = explosion.transform.GetChild(i);
            child.localScale = scale;
        }
        explosion.SetSize(size);
        explosion.Play();
        onReturnToPool?.Invoke();
    }

    public void SetScale(float scale)
    {
        size = scale;
        transform.localScale = Vector3.one * scale;
        col.size = defaultColliderSize * scale;
        trail.widthMultiplier = defaultTrailWidthMod * scale;

        // var plumeMain = enginePlumeSystem.main;
        // plumeMain.startSizeMultiplier = defaultPlumeSizeMult * scale;
        // var bulbMain = enginePlumeBulbSystem.main;
        // bulbMain.startSizeMultiplier = defaultBulbSizeMult * scale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other == null || other.transform == owner)
            return;
        
        if(other.TryGetComponent<IHealth>(out IHealth otherHealth))
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius * size);
            // Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius * (isJuiced ? 1.5f : 1f));
            foreach(Collider2D hitCollider in hitColliders)
            {
                if(hitCollider.transform == owner)
                    continue;
                if(hitCollider.TryGetComponent<IHealth>(out IHealth hitHealth))
                    hitHealth.TakeDamage(damage, owner);
            }
            Collide();
        }
    }
}
