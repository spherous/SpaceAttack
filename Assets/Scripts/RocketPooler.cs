using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketPooler : Pooler<Rocket>
{
    public class RocketReturn : ReturnToPool<Rocket>{}
    [SerializeField] protected ParticleSystemPooler explosionPooler;

    protected override void OnReturnToPool(Rocket item)
    {
        item.body.velocity = Vector2.zero;
        base.OnReturnToPool(item);
    }
    protected override Rocket CreatePooledItem()
    {
        Rocket rocket = base.CreatePooledItem();
        rocket.SetExplosionPooler(explosionPooler);
        RocketReturn rocketReturn = rocket.gameObject.AddComponent<RocketReturn>();
        rocketReturn.pool = pool;
        return rocket;
    }
}
