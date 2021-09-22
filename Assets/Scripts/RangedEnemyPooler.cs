using UnityEngine;
using static UnityEngine.Camera;

public class RangedEnemyPooler : Pooler<RangedEnemy>
{
    [SerializeField] private Camera cam;
    [SerializeField] private ParticleSystemPooler explosionPooler;
    [SerializeField] private ParticleSystemPooler sparkPooler;
    [SerializeField] private Transform healthBarContainer;
    [SerializeField] private HealthBar healthBarPrefab;
    public class RangedEnemyReturn : ReturnToPool<RangedEnemy>{}
    protected override void OnReturnToPool(RangedEnemy item)
    {
        item.gameObject.SetActive(false);
        item.transform.position = cam.ScreenToWorldPoint(new Vector3(-50, -50, 10), MonoOrStereoscopicEye.Mono);
    }

    protected override void OnTakeFromPool(RangedEnemy item)
    {
        base.OnTakeFromPool(item);
        item.isDead = false;
        item.isDying = false;
        item.HealToFull();
    }

    protected override RangedEnemy CreatePooledItem()
    {
        RangedEnemy enemy = base.CreatePooledItem();
        enemy.explosionPooler = explosionPooler;
        enemy.sparkPooler = sparkPooler;

        HealthBar newBar = Instantiate(healthBarPrefab, healthBarContainer);
        newBar.SetTarget(enemy.gameObject, enemy);

        RangedEnemyReturn enemyReturn = enemy.gameObject.AddComponent<RangedEnemyReturn>();
        enemyReturn.pool = pool;
        return enemy;
    }
}
