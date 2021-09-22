using UnityEngine;

public class LaserPooler : Pooler<LaserProjectile>
{
    public class LaserReturn : ReturnToPool<LaserProjectile> {}
    protected override void OnReturnToPool(LaserProjectile item)
    {
        item.body.velocity = Vector2.zero;
        base.OnReturnToPool(item);

        item.col.size = new Vector2(item.defaultWidthMulti, item.col.size.y);
        item.trail.widthMultiplier = item.defaultWidthMulti;
        item.speed = item.defaultSpeed;

    }
    protected override LaserProjectile CreatePooledItem()
    {
        LaserProjectile proj = base.CreatePooledItem();
        LaserReturn laserReturn = proj.gameObject.AddComponent<LaserReturn>();
        laserReturn.pool = pool;
        return proj;
    }
}