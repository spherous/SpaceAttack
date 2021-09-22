using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeamPooler : Pooler<LaserBeam>
{
    public class LaserBeamReturn : ReturnToPool<LaserBeam>{}
    protected override void OnReturnToPool(LaserBeam item)
    {
        base.OnReturnToPool(item);
        item.SetLength(1);
        item.ResetWidth();
    }
    protected override LaserBeam CreatePooledItem()
    {
        LaserBeam laserBeam = base.CreatePooledItem();
        LaserBeamReturn laserBeamReturn = laserBeam.gameObject.AddComponent<LaserBeamReturn>();
        laserBeamReturn.pool = pool;
        return laserBeam;
    }
}
