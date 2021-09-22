using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserProjectile : MonoBehaviour, IProjectile, IPoolable
{
    [SerializeField] public TrailRenderer trail;
    [SerializeField] private AudioSource audioSource;
    public Color playerColor;
    public Color enemyColor;
    private float dieAtTime;
    public float lifetime {get => _lifetime; set{_lifetime = value;}}
    [SerializeField] private float _lifetime;

    public float speed {get => _speed; set{_speed = value;}}
    private float _speed;

    public float defaultSpeed {get => _defaultSpeed; set => _defaultSpeed = value;}
    [SerializeField] private float _defaultSpeed;

    public Rigidbody2D body {get => _body; set{}}
    [SerializeField] private Rigidbody2D _body;

    public bool inPool {get => _inPool; set{_inPool = value;}}
    bool _inPool = false;


    public event OnReturnToPool onReturnToPool;
    private Transform owner;
    float damage;
    public float defaultWidthMulti {get; private set;}
    public BoxCollider2D col;
    private bool isPlayer = false;
    private void Awake() {
        
        
        defaultWidthMulti = trail.widthMultiplier;
    }

    private void Update()
    {
        if(Time.timeSinceLevelLoad >= dieAtTime)
            Collide();
    }

    public void Collide()
    {
        trail.Clear();
        onReturnToPool?.Invoke();
    }

    public void Fire(Transform owner, float damage, bool isPlayer = false)
    {
        trail.Clear();
        this.owner = owner;
        this.damage = damage;
        this.isPlayer = isPlayer;
        dieAtTime = Time.timeSinceLevelLoad + lifetime;
        body.velocity = transform.up * speed;
        Color color = isPlayer ? playerColor : enemyColor;
        trail.startColor = color;
        trail.endColor = color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other == null || other.transform == owner)
            return;

        if(other.TryGetComponent<IHealth>(out IHealth otherHealth))
        {
            // enemies can't shoot their teammates (mainly needed so the melee enemies to all instantly kill eachother)
            if(!isPlayer && otherHealth is Enemy)
                return;

            otherHealth.TakeDamage(damage, owner);
            Collide();
        }
    }
}