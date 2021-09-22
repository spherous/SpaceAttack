using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPooler : Pooler<Boss>
{
    [SerializeField] private LaserPooler laserPooler;
    [SerializeField] private ParticleSystemPooler sparkPooler;
    [SerializeField] private ParticleSystemPooler explosionPooler;
    [SerializeField] private RangedEnemyPooler rangedEnemyPooler;
    [SerializeField] private MeleeEnemyPooler meleeEnemyPooler;
    [SerializeField] private RockSpawner rockSpawner;
    public class BossReturn : ReturnToPool<Boss>{}
    protected override void OnReturnToPool(Boss item)
    {
        base.OnReturnToPool(item);
    }

    protected override Boss CreatePooledItem()
    {
        Boss boss = base.CreatePooledItem();
        boss.laserPooler = laserPooler;
        boss.sparkPooler = sparkPooler;
        boss.explosionPooler = explosionPooler;
        boss.meleeEnemyPooler = meleeEnemyPooler;
        boss.rangedEnemyPooler = rangedEnemyPooler;
        boss.rockSpawner = rockSpawner;
        BossReturn bossReturn = boss.gameObject.AddComponent<BossReturn>();
        bossReturn.pool = pool;
        return boss;
    }
}
