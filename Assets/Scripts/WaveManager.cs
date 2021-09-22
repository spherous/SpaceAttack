using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using static Extensions.Extensions;
using static UnityEngine.Camera;
using Extensions;

public class WaveManager : SerializedMonoBehaviour
{
    Camera cam;
    Player player;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private RangedEnemyPooler rangedEnemyPooler;
    [SerializeField] private MeleeEnemyPooler meleeEnemyPooler;
    [SerializeField] private BossPooler bossPooler;
    [SerializeField] private RockSpawner rockSpawner;
    [OdinSerialize] public Dictionary<int, List<Wave>> wavePresetsDict = new Dictionary<int, List<Wave>>();
    [ShowInInspector, ReadOnly] private int currentWaveDifficulty;
    private float nextWaveSpawnTime;
    public float betweenWaveDelay;
    [ShowInInspector] Dictionary<IHealth, int> spawnedWaveUnits = new Dictionary<IHealth, int>();
    BlitzClock blitzClock;
    Score score;
    WaveComplete waveCompleteNotification;
    public float waveCompletionAdditionalTime;

    [ShowInInspector] private Wave? currentlySpawningWave;
    [ShowInInspector] private float? waveStartAtTime;
    [ShowInInspector] private int? meleeCycle;
    [ShowInInspector] private int? rangedCycle;
    [ShowInInspector] private int? rockCycle;
    public int powerupOfferedAfterWaves;
    public int wavesDefeated {get; private set;}


    public List<Wave> tempWaves = new List<Wave>();
    
    private void Awake()
    {
        cam = Camera.main;
        blitzClock = GameObject.FindObjectOfType<BlitzClock>();
        score = GameObject.FindObjectOfType<Score>();
        waveCompleteNotification = GameObject.FindObjectOfType<WaveComplete>();
        player = GameObject.FindObjectOfType<Player>();
    }
    private void Start() {
        SpawnWave(GetRandomWaveFromPower(0));
    }
    private Wave GetRandomWaveFromPower(int power)
    {
        if(wavePresetsDict.ContainsKey(power))
            return wavePresetsDict[power].ChooseRandom();
        else
        {
            // this should return a scaled dynamic wave
            return default;
        }
    }

    private void Update()
    {
        if(pauseMenu.paused)
            return;

        if(Time.timeSinceLevelLoad >= nextWaveSpawnTime)
            SpawnWave(GetRandomWaveFromPower(currentWaveDifficulty));
        
        if(currentlySpawningWave.HasValue && waveStartAtTime.HasValue)
        {
            float waveStartTime = waveStartAtTime.Value;
            Wave spawningWave = currentlySpawningWave.Value;
            int waveID = wavesDefeated;

            // Melee
            if(meleeCycle.HasValue && meleeCycle.Value < spawningWave.meleeBurst.cycles)
            {
                if(Time.timeSinceLevelLoad >= waveStartTime + spawningWave.meleeBurst.time + (spawningWave.meleeBurst.interval * meleeCycle.Value))
                {
                    // spawn next melee count
                    GameObject[] inactiveUnits = spawnedWaveUnits.Where(kvp => kvp.Value == waveID && !kvp.Key.obj.activeSelf).Select(kvp => kvp.Key.obj).ToArray();
                    int count = 0;
                    for(int i = 0; i < inactiveUnits.Length; i++)
                    {
                        if(count >= spawningWave.meleeBurst.count)
                            break;
                        if(inactiveUnits[i].TryGetComponent<MeleeEnemy>(out MeleeEnemy unit))
                        {
                            unit.gameObject.SetActive(true);
                            count++;
                        }
                    }
                    meleeCycle = meleeCycle.Value + 1 < spawningWave.meleeBurst.cycles ? meleeCycle.Value + 1 : (int?)null;
                }
            }

            // Ranged
            if(rangedCycle.HasValue && rangedCycle.Value < spawningWave.rangedBurst.cycles)
            {
                if(Time.timeSinceLevelLoad >= waveStartTime + spawningWave.rangedBurst.time + (spawningWave.rangedBurst.interval * rangedCycle.Value))
                {
                    // spawn next ranged count
                    GameObject[] inactiveUnits = spawnedWaveUnits.Where(kvp => kvp.Value == waveID && !kvp.Key.obj.activeSelf).Select(kvp => kvp.Key.obj).ToArray();
                    int count = 0;
                    for(int i = 0; i < inactiveUnits.Length; i++)
                    {
                        if(count >= spawningWave.rangedBurst.count)
                            break;
                        if(inactiveUnits[i].TryGetComponent<RangedEnemy>(out RangedEnemy unit))
                        {
                            unit.gameObject.SetActive(true);
                            count++;
                        }
                    }
                    rangedCycle = rangedCycle.Value + 1 < spawningWave.rangedBurst.cycles ? rangedCycle.Value + 1 : (int?)null;
                }
            }

            // Rocks
            if(rockCycle.HasValue && rockCycle.Value < spawningWave.rockBurst.cycles)
            {
                if(Time.timeSinceLevelLoad >= waveStartTime + spawningWave.rockBurst.time + (spawningWave.rockBurst.interval * rockCycle.Value))
                {
                    // spawn nex rock count
                    GameObject[] inactiveUnits = spawnedWaveUnits.Where(kvp => kvp.Value == waveID && !kvp.Key.obj.activeSelf).Select(kvp => kvp.Key.obj).ToArray();
                    int count = 0;
                    for(int i = 0; i < inactiveUnits.Length; i++)
                    {
                        if(count >= spawningWave.rockBurst.count)
                            break;
                        if(inactiveUnits[i].TryGetComponent<Rock>(out Rock newRock))
                        {
                            newRock.gameObject.SetActive(true);
                            // velocity
                            newRock.body.angularVelocity = UnityEngine.Random.Range(-120f, 120f);
                            (float x, float y) screenTarget = (UnityEngine.Random.Range(100f, Screen.width - 100), UnityEngine.Random.Range(100f, Screen.height - 100));
                            Vector3 worldTarget = cam.ScreenToWorldPoint(new Vector3(screenTarget.x, screenTarget.y, 10));
                            newRock.body.velocity = (worldTarget - newRock.transform.position).normalized * UnityEngine.Random.Range(0.8f, newRock.maxSpeed);
                            count++;
                        }
                    }
                    rockCycle = rockCycle.Value + 1 < spawningWave.rockBurst.cycles ? rockCycle.Value + 1 : (int?)null;
                }
            }

            int remainingToSpawn = spawnedWaveUnits.Count(kvp => !kvp.Key.obj.activeSelf && kvp.Value == waveID);
            if(remainingToSpawn == 0)
            {
                currentlySpawningWave = null;
                waveStartAtTime = null;
            }
        }
    }

    public void SpawnWave(Wave toSpawn)
    {
        if(wavesDefeated % 5 == 0)
            currentWaveDifficulty++;
        nextWaveSpawnTime = Time.timeSinceLevelLoad + toSpawn.maxDuration + betweenWaveDelay;
        int ID = wavesDefeated;
        waveStartAtTime = Time.timeSinceLevelLoad;

        // Calculate and prespawn all units, release them by burst rules
        currentlySpawningWave = toSpawn;

        UnitBurst meleeBurst = toSpawn.meleeBurst;
        int totalMeleeUnits = meleeBurst.count * meleeBurst.cycles;
        meleeCycle = totalMeleeUnits > 0 ? 0 : (int?)null;
        for(int i = 0; i < totalMeleeUnits; i++)
        {
            MeleeEnemy newEnemy = meleeEnemyPooler.pool.Get();
            spawnedWaveUnits.Add(newEnemy, ID);
            ConfigureEnemy(newEnemy);
            newEnemy.gameObject.SetActive(false);

        }

        UnitBurst rangedBurst = toSpawn.rangedBurst;
        int totalRangedUnits = rangedBurst.count * rangedBurst.cycles;
        rangedCycle = totalRangedUnits > 0 ? 0 : (int?)null;
        for(int i = 0; i < totalRangedUnits; i++)
        {
            RangedEnemy newEnemy = rangedEnemyPooler.pool.Get();
            spawnedWaveUnits.Add(newEnemy, ID);
            ConfigureEnemy(newEnemy);
            newEnemy.gameObject.SetActive(false);
        }

        UnitBurst rockBurst = toSpawn.rockBurst;
        int totalRockUnits = rockBurst.count * rockBurst.cycles;
        rockCycle = totalRockUnits > 0 ? 0 : (int?)null;
        for(int i = 0; i < totalRockUnits; i++)
        {
            int startRockSize = UnityEngine.Random.Range(2, 5);
            Rock newRock = rockSpawner.poolers[startRockSize].pool.Get();
            newRock.onHealthChanged += WaveEnemyDamaged;
            newRock.Init(startRockSize, rockSpawner);
            spawnedWaveUnits.Add(newRock, ID);

            // position
            (float x, float y) loc = GetRandomOffScreenLocation();
            newRock.transform.position = cam.ScreenToWorldPoint(new Vector3(loc.x, loc.y, 10), MonoOrStereoscopicEye.Mono);

            // Randomize rotation
            newRock.transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-180f, 180f));

            newRock.gameObject.SetActive(false);
        }

        if(toSpawn.hasBoss)
        {
            Boss boss = bossPooler.pool.Get();
            boss.onHealthChanged += WaveEnemyDamaged;
            spawnedWaveUnits.Add(boss, ID);
        }
    }

    public void ConfigureEnemy(Enemy enemy)
    {
        enemy.HealToFull();
        enemy.onHealthChanged += WaveEnemyDamaged;
        enemy.isDead = false;
        enemy.isDying = false;

        (float x, float y) loc = GetRandomOffScreenLocation();
        Vector3 spawnAtLoc = new Vector3(loc.x, loc.y, 10);
        enemy.transform.position = cam.ScreenToWorldPoint(spawnAtLoc, MonoOrStereoscopicEye.Mono);
        enemy.transform.localScale = Vector3.one * UnityEngine.Random.Range(1f, 1.333f);
    }

    private void WaveEnemyDamaged(IHealth changed, float newHP)
    {
        if(newHP <= 0)
        {
            int waveID = spawnedWaveUnits[changed];
            spawnedWaveUnits.Remove(changed);
            changed.onHealthChanged -= WaveEnemyDamaged;

            if(!spawnedWaveUnits.Any(kvp => kvp.Value == waveID))
            {
                // WAVE COMPLETE
                if(!player.isDead && !player.isDying)
                {
                    wavesDefeated++;
                    waveCompleteNotification.Show(wavesDefeated);
                    score.Add(waveID);
                    blitzClock.AddSeconds(waveCompletionAdditionalTime);

                    // Every 5 waves, offer power choice
                    if((waveID + 1) % powerupOfferedAfterWaves == 0)
                    {
                        pauseMenu.Enable(true);
                        pauseMenu.OfferPowerups();
                    }
                }
            }

            if(spawnedWaveUnits.Count == 0)
                nextWaveSpawnTime = Time.timeSinceLevelLoad + betweenWaveDelay;
        }
    }
}