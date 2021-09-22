using System.Collections;
using System.Collections.Generic;
using static Extensions.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class Boss : MonoBehaviour, IHealth, IPoolable
{
    Camera cam;
    enum AttackTypes {EnemyWave = 0, LaserDrones = 1}
    [SerializeField] private Turret turret1;
    [SerializeField] private Turret turret2;
    public LaserPooler laserPooler;
    public ParticleSystemPooler sparkPooler;
    public ParticleSystemPooler explosionPooler;
    public RangedEnemyPooler rangedEnemyPooler;
    public MeleeEnemyPooler meleeEnemyPooler;
    public RockSpawner rockSpawner;
    private Player player;
    private Score score;
    private BlitzClock blitzClock;
    private ScreenWrapIndicator screenWrapIndicator;

    public float speed;
    public float turretDamage;

    public Vector3 topPos;
    public Vector3 bottomPos;
    public Vector3 rightPos;
    public Vector3 leftPos;
    public Vector3 topEntryPos;
    public Vector3 bottomEntryPos;
    public Vector3 rightEntryPos;
    public Vector3 leftEntryPos;

    public float relocateAfterTime;
    private float relocateAtTime;

    public event OnHealthChanged onHealthChanged;
    public event OnReturnToPool onReturnToPool;

    public GameObject obj {get => gameObject; set{}}
    public float maxHealth {get => _maxHP; set{}}
    [SerializeField] protected float _maxHP;
    [ReadOnly, ShowInInspector] public float currentHealth {get => _hp; set{_hp = value;}}
    protected float _hp;
    public bool isDead {get => _isDead; set => _isDead = value;}
    protected bool _isDead;
    public bool isDying {get => _isDying; set => _isDying = value;}
    protected bool _isDying;
    public bool inPool {get => _inPool; set{_inPool = value;}}
    bool _inPool = false;

    int onSideID;
    public bool onSide {get; set;} = false;

    private float attackAtTime;
    public float timeBetweenAttacks;
    public UnitBurst enemyBurst;
    public UnitBurst rockBurst;
    public Transform meleePoint;
    public Transform rangedPoint;
    bool attacking = false;

    public List<BossCoverPlate> coverPlates = new List<BossCoverPlate>();
    public List<BossWeakSpot> weakSpots = new List<BossWeakSpot>();
    public List<LaserDrone> laserDrones = new List<LaserDrone>();

    private void Awake() {
        cam = Camera.main;
        player = GameObject.FindObjectOfType<Player>();
        score = GameObject.FindObjectOfType<Score>();
        blitzClock = GameObject.FindObjectOfType<BlitzClock>();
        screenWrapIndicator = GameObject.FindObjectOfType<ScreenWrapIndicator>();
        HealToFull();
        
    }
    private void Start() {
        GoToSide(UnityEngine.Random.Range(0, 4));
        relocateAtTime = Time.timeSinceLevelLoad + relocateAfterTime;
        turret1.laserPooler = laserPooler;
        turret2.laserPooler = laserPooler;
        turret1.sparkPooler = sparkPooler;
        turret2.sparkPooler = sparkPooler;
        turret1.explosionPooler = explosionPooler;
        turret2.explosionPooler = explosionPooler;
        turret1.boss = this;
        turret2.boss = this;
        foreach(BossCoverPlate plate in coverPlates)
        {
            plate.sparkPooler = sparkPooler;
            plate.explosionPooler = explosionPooler;
        }
        foreach(BossWeakSpot weakSpot in weakSpots)
        {
            weakSpot.sparkPooler = sparkPooler;
            weakSpot.explosionPooler = explosionPooler;
        }
        foreach(LaserDrone laserDrone in laserDrones)
        {
            laserDrone.sparkPooler = sparkPooler;
            laserDrone.explosionPooler = explosionPooler;
        }
    }

    private void Update() {
        if(Time.timeSinceLevelLoad >= relocateAtTime && onSide)
        {
            StartCoroutine(LeaveSide(GetEntryPoint(onSideID)));
            relocateAtTime = Time.timeSinceLevelLoad + relocateAfterTime;
        }
        if(onSide && Time.timeSinceLevelLoad >= attackAtTime && !attacking)
        {
            AttackTypes type = EnumArray<AttackTypes>.Values.ChooseRandom();

            if(type == AttackTypes.EnemyWave)
                StartCoroutine(SpawnEnemies());
            else if(type == AttackTypes.LaserDrones)
                StartCoroutine(ReleaseLaserDrones());

            attackAtTime = Time.timeSinceLevelLoad + timeBetweenAttacks;
        }
    }

    IEnumerator ReleaseLaserDrones()
    {
        attacking = true;
        (LaserDrone.Routine routineA, LaserDrone.Routine routineB) = onSideID switch{
            0 => (LaserDrone.Routine.LeftToRight, LaserDrone.Routine.RighToLeft),
            1 => (LaserDrone.Routine.TopToBottom, LaserDrone.Routine.BottomToTop),
            2 => (LaserDrone.Routine.LeftToRight, LaserDrone.Routine.RighToLeft),
            3 => (LaserDrone.Routine.TopToBottom, LaserDrone.Routine.BottomToTop),
            _ => (LaserDrone.Routine.None, LaserDrone.Routine.None)
        };
        bool routineChanged = false;
        foreach(LaserDrone laserDrone in laserDrones)
        {
            laserDrone.Release(onSideID, routineChanged ? routineA : routineB);
            routineChanged = true;
            yield return new WaitForSeconds(2.5f);
        }
        yield return new WaitForSeconds(20f);
        attacking = false;
    }

    IEnumerator SpawnEnemies()
    {
        attacking = true;
        
        for(int i = 0; i < enemyBurst.cycles; i++)
        {
            if(!gameObject.activeSelf)
                break;
            
            if(!onSide)
                break;
            
            for(int j = 0; j < enemyBurst.count; j++)
            {
                RangedEnemy ranged = rangedEnemyPooler.pool.Get();
                MeleeEnemy melee = meleeEnemyPooler.pool.Get();

                ranged.HealToFull();
                melee.HealToFull();

                ranged.isDead = false;
                ranged.isDying = false;
                melee.isDying = false;
                melee.isDying = false;

                ranged.transform.position = rangedPoint.position;
                melee.transform.position = meleePoint.position;
            }
            yield return new WaitForSeconds(enemyBurst.interval);
        }

        attacking = false;
    }

    private void GoToSide(int sideID)
    {
        screenWrapIndicator.ReleaseAllLocks();
        screenWrapIndicator.LockSide(sideID, false);
        screenWrapIndicator.LockSide((sideID + 2) % 4);
        onSideID = sideID;
        var goal = GetSideGoal(sideID);
        Vector3 entryPoint = GetEntryPoint(sideID);
        transform.position = entryPoint;
        StartCoroutine(GetInPosition(goal.loc));
        transform.rotation = Quaternion.Euler(0, 0, goal.zrot);
    }

    IEnumerator GetInPosition(Vector3 goal)
    {
        Vector3 start = transform.position;
        float totalDist = (goal - start).magnitude;
        while(transform.position != goal)
        {
            yield return 0;
            float currentDist = (start - transform.position).magnitude;

            transform.position = Vector3.Lerp(start, goal, Mathf.Clamp01((currentDist + (speed * Time.deltaTime)) / totalDist));
        }
        onSide = true;
        relocateAtTime = Time.timeSinceLevelLoad + relocateAfterTime;
    }
    IEnumerator LeaveSide(Vector3 goal)
    {
        onSide = false;
        Vector3 start = transform.position;
        float totalDist = (goal - start).magnitude;
        while(transform.position != goal)
        {
            yield return 0;
            float currentDist = (start - transform.position).magnitude;

            transform.position = Vector3.Lerp(start, goal, Mathf.Clamp01((currentDist + (speed * Time.deltaTime)) / totalDist));
        }
        bool spawnRocks = RandomBool();
        if(spawnRocks)
            yield return StartCoroutine(SpawnRocks());
        else
            yield return new WaitForSeconds(3f);
        GoToSide(UnityEngine.Random.Range(0, 4));
    }

    IEnumerator SpawnRocks()
    {
        for(int i = 0; i < rockBurst.cycles; i++)
        {
            if(!gameObject.activeSelf)
                break;
            
            if(onSide)
                break;

            for(int j = 0; j < rockBurst.count; j++)
            {
                int startRockSize = UnityEngine.Random.Range(1, 5);
                Rock newRock = rockSpawner.poolers[startRockSize].pool.Get();
                // Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
                // choose a random position on this side of the screen, off screen
                bool shouldSpawnVert = (onSideID + 1) % 2 == 0;                
                int rockSideID = shouldSpawnVert 
                    ? UnityEngine.Random.Range(0,2) == 0 ? 0 : 2 // top or bottom
                    : UnityEngine.Random.Range(0,2) == 0 ? 1 : 3; // left or right
                Vector3 targetScreenPos = GetRandomOffScreenLocationOnSide(rockSideID);
                newRock.transform.position = cam.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, 10));
                newRock.Init(startRockSize, rockSpawner);

                Vector3 directionToPlayer = (player.transform.position - newRock.transform.position).normalized;
                newRock.body.velocity = (directionToPlayer + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f), 0)) * UnityEngine.Random.Range(newRock.maxSpeed, newRock.maxSpeed * 3f);
            }
            yield return new WaitForSeconds(rockBurst.interval);
        }
    }

    private (Vector3 loc, float zrot) GetSideGoal(int sideID) => sideID switch
    {
        0 => (topPos, 0f),
        1 => (rightPos, -90f),
        2 => (bottomPos, 180f),
        3 => (leftPos, 90f),
        _ => (Vector3.zero, 0f)
    };

    private Vector3 GetEntryPoint(int sideID) => sideID switch
    {
        0 => topEntryPos,
        1 => rightEntryPos,
        2 => bottomEntryPos,
        3 => leftEntryPos,
        _ => Vector3.zero,
    };

    public void HealToFull()
    {
        float health = currentHealth;
        currentHealth = maxHealth;
        if(currentHealth != health)
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
            // play damaged audio/vfx

            onHealthChanged?.Invoke(this, currentHealth);
        }

        if(currentHealth == 0)
            Die();
    }

    public void Die()
    {
        Debug.Log("Boss died");
        score.Add(100);
        blitzClock.AddSeconds(30);
        PooledParticleSystem explosion = explosionPooler.pool.Get();
        explosionPooler.transform.position = transform.position;
        explosion.SetSize(4f);
        explosion.Play();
        _isDead = true;
        onReturnToPool?.Invoke();
    }
}