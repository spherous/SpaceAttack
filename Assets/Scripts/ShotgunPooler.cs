using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunPooler : Pooler<LaserProjectile>
{
    public class ShotgunReturn : ReturnToPool<LaserProjectile> {}
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
        ShotgunReturn laserReturn = proj.gameObject.AddComponent<ShotgunReturn>();
        laserReturn.pool = pool;
        return proj;
    }
}