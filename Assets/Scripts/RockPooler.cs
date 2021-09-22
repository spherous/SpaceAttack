using UnityEngine;

public class RockPooler : Pooler<Rock>
{
    public class RockReturn : ReturnToPool<Rock> {}
    [SerializeField] ParticleSystemPooler dustPooler;
    protected override void OnReturnToPool(Rock item)
    {
        item.body.velocity = Vector2.zero;
        base.OnReturnToPool(item);
    }
    protected override Rock CreatePooledItem()
    {
        Rock rock = base.CreatePooledItem();
        rock.SetDustPooler(dustPooler);
        RockReturn rockReturn = rock.gameObject.AddComponent<RockReturn>();
        rockReturn.pool = pool;
        return rock;
    }
    protected override void OnTakeFromPool(Rock item)
    {
        item.inPool = false;
    }
}