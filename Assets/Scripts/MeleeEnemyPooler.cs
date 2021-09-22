using UnityEngine;
using static UnityEngine.Camera;

public class MeleeEnemyPooler : Pooler<MeleeEnemy>
{
    [SerializeField] private Camera cam;
    [SerializeField] private ParticleSystemPooler burstPooler;
    [SerializeField] private ParticleSystemPooler explosionPooler;
    [SerializeField] private ParticleSystemPooler sparkPooler;
    [SerializeField] private ShotgunPooler spikePooler;
    [SerializeField] private Transform healthBarContainer;
    [SerializeField] private HealthBar healthBarPrefab;
    public class MeleeEnemyReturn : ReturnToPool<MeleeEnemy>{}

    protected override void OnReturnToPool(MeleeEnemy item)
    {
        item.gameObject.SetActive(false);
        item.transform.position = cam.ScreenToWorldPoint(new Vector3(-50, -50, 10), MonoOrStereoscopicEye.Mono);
    }

    protected override void OnTakeFromPool(MeleeEnemy item)
    {
        base.OnTakeFromPool(item);
        item.isDead = false;
        item.isDying = false;
        item.HealToFull();
    }

    protected override MeleeEnemy CreatePooledItem()
    {
        MeleeEnemy enemy = base.CreatePooledItem();
        enemy.burstPooler = burstPooler;
        enemy.explosionPooler = explosionPooler;
        enemy.sparkPooler = sparkPooler;
        enemy.spikePooler = spikePooler;

        HealthBar newBar = Instantiate(healthBarPrefab, healthBarContainer);
        newBar.SetTarget(enemy.gameObject, enemy);

        MeleeEnemyReturn enemyReturn = enemy.gameObject.AddComponent<MeleeEnemyReturn>();
        enemyReturn.pool = pool;
        return enemy;
    }
}